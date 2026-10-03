using ArticleService.BLL;
using ArticleService.BLL.Interfaces;
using ArticleService.DAL;
using ArticleService.DAL.Interfaces;
using ArticleService.DAL.Repositories;
using ArticleService.DAL.Repositories.Interfaces;
using ArticleService.Messaging;
using EasyNetQ;
using Monitoring;
using Serilog;
using StackExchange.Redis;
using ArticleService.Caching;
using ArticleService.Caching.Interfaces;

var builder = WebApplication.CreateBuilder(args);

MonitorService.Configure(builder, "ArticleService");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IArticleLogic, ArticleLogic>();
builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
builder.Services.AddScoped<IArticleContextFactory, ArticleContextFactory>();

builder.Services.AddSingleton<IArticleCache, RedisArticleCache>();

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(
        builder.Configuration.GetConnectionString("Redis")
        ?? throw new InvalidOperationException(
            "Missing Redis connection string.")));

builder.Services.AddSingleton<IBus>(_ =>
    RabbitHutch.CreateBus(
        builder.Configuration.GetConnectionString("RabbitMQ")
        ?? throw new InvalidOperationException(
            "Missing RabbitMQ connection string.")));

var isCacheWorker =
    builder.Configuration.GetValue<bool>("Caching:WorkerOnly");

if (isCacheWorker)
{
    // Cache-workeren indlæser globale artikler fra databasen
    // og erstatter indholdet i Redis hvert 30. sekund.
    builder.Services.AddHostedService<ArticleCacheRefreshService>();
}
else
{
    // De almindelige API-instanser modtager publicerede artikler
    // gennem RabbitMQ og gemmer dem i den relevante database.
    builder.Services.AddHostedService<ArticleConsumer>();
}
var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMonitoringExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI();

app.Use(async (context, next) =>
{
    var instance = Environment.GetEnvironmentVariable("HOSTNAME")
                   ?? Environment.MachineName;

    Console.WriteLine(
        $"Service {instance} bruges: " +
        $"{context.Request.Method} {context.Request.Path}");

    context.Response.Headers["X-ArticleService-Instance"] = instance;

    await next();
});

app.MapControllers();

app.MapGet("/api/cache/ping", async (
    IConnectionMultiplexer redis) =>
{
    var elapsed = await redis.GetDatabase().PingAsync();

    return Results.Ok(new
    {
        Message = "Redis connection works",
        ElapsedMilliseconds = elapsed.TotalMilliseconds
    });
});

app.MapGet("/api/cache/statistics", async (
    IConnectionMultiplexer redis) =>
{
    try
    {
        var database = redis.GetDatabase();

        // Hent begge tællere i ét Redis-kald.
        // Tallene er fælles for alle tre ArticleService-instanser.
        var values = await database.HashGetAsync(
            "cache:global-articles:statistics",
            new RedisValue[] { "hits", "misses" });


        // Manglende tællere behandles som nul.
        long hits = values[0].IsNull ? 0 : (long)values[0];
        long misses = values[1].IsNull ? 0 : (long)values[1];

        var totalLookups = hits + misses;

        var hitRatioPercent = totalLookups == 0
            ? 0
            : hits * 100.0 / totalLookups;
        
        return Results.Ok(new
        {
            Cache = "global-articles",
            Hits = hits,
            Misses = misses,
            TotalLookups = totalLookups,
            HitRatioPercent = Math.Round(hitRatioPercent, 2)
        });
    }
    catch (RedisException exception)
    {
        MonitorService.Log.Warning(
            exception,
            "Reading article cache statistics failed");

        return Results.Problem(
            title: "Cache statistics unavailable",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});
app.Run();