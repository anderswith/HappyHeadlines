using CommentService.BLL;
using CommentService.BLL.Interfaces;
using CommentService.Clients;
using CommentService.Clients.Interfaces;
using CommentService.DAL;
using CommentService.DAL.Repositories;
using CommentService.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using StackExchange.Redis;
using CommentService.Caching;
using CommentService.Caching.Interfaces;
using System.Text.Json;
using System.Globalization;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<CommentContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("CommentDatabase"),
        postgres => postgres.CommandTimeout(5)
    );
});

builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<ICommentLogic, CommentLogic>();

builder.Services.AddSingleton<ICommentCache, RedisCommentCache>();
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(
        builder.Configuration.GetConnectionString("Redis")
        ?? throw new InvalidOperationException(
            "Missing Redis connection string.")));

builder.Services
    .AddHttpClient<IProfanityClient, ProfanityClient>(client =>
    {
        var baseUrl = builder.Configuration["ProfanityService:BaseUrl"]
            ?? throw new InvalidOperationException(
                "Missing ProfanityService base URL.");

        client.BaseAddress = new Uri(baseUrl);

        // Timeout håndteres af resilience-pipelinen nedenfor.
        client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
    })
    .AddResilienceHandler("profanity", pipeline =>
    {
        // Maksimalt 10 samtidige kald og ingen ventekø.
        pipeline.AddConcurrencyLimiter(10, 0);

        pipeline.AddCircuitBreaker(
            new HttpCircuitBreakerStrategyOptions
            {
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 3,
                FailureRatio = 0.5,
                BreakDuration = TimeSpan.FromSeconds(15),

                OnOpened = args =>
                {
                    Console.WriteLine(
                        "Circuit breaker ÅBEN: kald til ProfanityService stoppes.");

                    return ValueTask.CompletedTask;
                },

                OnHalfOpened = args =>
                {
                    Console.WriteLine(
                        "Circuit breaker HALF-OPEN: prøver ProfanityService igen.");

                    return ValueTask.CompletedTask;
                },

                OnClosed = args =>
                {
                    Console.WriteLine(
                        "Circuit breaker LUKKET: ProfanityService virker igen.");

                    return ValueTask.CompletedTask;
                }
            });

        // En langsom afhængighed må ikke holde kaldet åbent længe.
        pipeline.AddTimeout(TimeSpan.FromSeconds(3));
    });

builder.Services.AddHttpClient("ArticleStatistics", client =>
{
    var baseUrl = builder.Configuration["ArticleService:BaseUrl"]
                  ?? throw new InvalidOperationException(
                      "Missing ArticleService base URL.");

    client.BaseAddress = new Uri(baseUrl);

    // Dashboardet må ikke vente ubegrænset på ArticleService.
    client.Timeout = TimeSpan.FromSeconds(5);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.MapGet("/api/cache/ping", async (
    IConnectionMultiplexer redis) =>
{
    var elapsed = await redis.GetDatabase().PingAsync();

    return Results.Ok(new
    {
        Message = "CommentService Redis connection works",
        ElapsedMilliseconds = elapsed.TotalMilliseconds
    });
});

app.MapGet("/api/cache/statistics", async (
    IConnectionMultiplexer redis,
    ILogger<Program> logger) =>
{
    try
    {
        var database = redis.GetDatabase();

        // Hent begge tællere i ét Redis-kald.
        var values = await database.HashGetAsync(
            "cache:{comments}:statistics",
            new RedisValue[] { "hits", "misses" });

        // Felterne mangler, hvis der endnu ikke har været opslag.
        long hits = values[0].IsNull ? 0 : (long)values[0];
        long misses = values[1].IsNull ? 0 : (long)values[1];

        var totalLookups = hits + misses;

        // Undgå division med nul.
        // Brug decimaltal, så eksempelvis 1 af 3 bliver 33,33 %.
        var hitRatioPercent = totalLookups == 0
            ? 0
            : hits * 100.0 / totalLookups;

        // Hash'en indeholder én kommentarliste pr. cachet artikel.
        // En tom kommentarliste tæller også som én artikel.
        var cachedArticles = await database.HashLengthAsync(
            "cache:{comments}:data");

        return Results.Ok(new
        {
            Cache = "comments",
            Hits = hits,
            Misses = misses,
            TotalLookups = totalLookups,
            HitRatioPercent = Math.Round(hitRatioPercent, 2),
            CachedArticles = cachedArticles,
            Capacity = 30
        });
    }
    catch (RedisException exception)
    {
        logger.LogWarning(
            exception,
            "Reading comment cache statistics failed");

        return Results.Problem(
            title: "Cache statistics unavailable",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapGet("/api/cache/article-statistics", async (
    IHttpClientFactory clientFactory,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    try
    {
        var client = clientFactory.CreateClient("ArticleStatistics");

        // Hent statistikken gennem loadbalanceren.
        // Alle ArticleService-instanser læser de samme Redis-tællere.
        using var response = await client.GetAsync(
            "api/cache/statistics",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        // Læs JSON og send de samme statistikfelter til dashboardet.
        var statistics = await response.Content
            .ReadFromJsonAsync<JsonElement>(
                cancellationToken: cancellationToken);

        return Results.Json(statistics);
    }
    catch (HttpRequestException exception)
    {
        logger.LogWarning(
            exception,
            "Fetching article cache statistics failed");

        return Results.Problem(
            title: "Article cache statistics unavailable",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (OperationCanceledException exception)
        when (!cancellationToken.IsCancellationRequested)
    {
        // HTTP-klientens timeout er udløbet.
        // Hvis brugeren afbryder requestet, håndteres det ikke her.
        logger.LogWarning(
            exception,
            "Fetching article cache statistics timed out");

        return Results.Problem(
            title: "Article cache statistics timed out",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});
app.MapGet("/metrics", async (
    IConnectionMultiplexer redis,
    ILogger<Program> logger) =>
{
    try
    {
        var database = redis.GetDatabase();

        // Læs de eksisterende tællere direkte fra Redis.
        // Vi opretter ikke nye tællere og ændrer ikke cachelogikken.
        var articleValues = await database.HashGetAsync(
            "cache:global-articles:statistics",
            new RedisValue[] { "hits", "misses" });

        var commentValues = await database.HashGetAsync(
            "cache:{comments}:statistics",
            new RedisValue[] { "hits", "misses" });

        long articleHits =
            articleValues[0].IsNull ? 0 : (long)articleValues[0];

        long articleMisses =
            articleValues[1].IsNull ? 0 : (long)articleValues[1];

        long commentHits =
            commentValues[0].IsNull ? 0 : (long)commentValues[0];

        long commentMisses =
            commentValues[1].IsNull ? 0 : (long)commentValues[1];

        var cachedArticles = await database.HashLengthAsync(
            "cache:{comments}:data");

        // Prometheus læser tekst i et bestemt format frem for JSON.
        var text = new StringBuilder();

        // Counter: et samlet antal, der stiger ved hvert opslag.
        text.Append(
            "# HELP happyheadlines_cache_requests_total Cache lookups by cache and result.\n");
        text.Append(
            "# TYPE happyheadlines_cache_requests_total counter\n");

        // Labels gør det muligt at filtrere på cache og resultat.
        text.Append(
            "happyheadlines_cache_requests_total{cache=\"articles\",result=\"hit\"} ");
        text.Append(articleHits.ToString(CultureInfo.InvariantCulture));
        text.Append('\n');

        text.Append(
            "happyheadlines_cache_requests_total{cache=\"articles\",result=\"miss\"} ");
        text.Append(articleMisses.ToString(CultureInfo.InvariantCulture));
        text.Append('\n');

        text.Append(
            "happyheadlines_cache_requests_total{cache=\"comments\",result=\"hit\"} ");
        text.Append(commentHits.ToString(CultureInfo.InvariantCulture));
        text.Append('\n');

        text.Append(
            "happyheadlines_cache_requests_total{cache=\"comments\",result=\"miss\"} ");
        text.Append(commentMisses.ToString(CultureInfo.InvariantCulture));
        text.Append('\n');

        // Gauge: en aktuel værdi, der kan stige og falde.
        // Antallet inkluderer entries, som endnu ikke er fjernet
        // efter tidsudløb. Det er antallet af gemte kommentarlister.
        text.Append(
            "# HELP happyheadlines_cache_entries Stored article comment collections.\n");
        text.Append(
            "# TYPE happyheadlines_cache_entries gauge\n");
        text.Append(
            "happyheadlines_cache_entries{cache=\"comments\"} ");
        text.Append(cachedArticles.ToString(CultureInfo.InvariantCulture));
        text.Append('\n');

        text.Append(
            "# HELP happyheadlines_cache_capacity Maximum cached article comment collections.\n");
        text.Append(
            "# TYPE happyheadlines_cache_capacity gauge\n");
        text.Append(
            "happyheadlines_cache_capacity{cache=\"comments\"} 30\n");

        return Results.Text(
            text.ToString(),
            contentType: "text/plain; version=0.0.4; charset=utf-8");
    }
    catch (RedisException exception)
    {
        // Hvis Redis fejler, skal Prometheus se en mislykket indsamling.
        // Vi må ikke præsentere nul som en gyldig måling.
        logger.LogWarning(
            exception,
            "Reading cache metrics failed");

        return Results.StatusCode(
            StatusCodes.Status503ServiceUnavailable);
    }
});

app.Run();