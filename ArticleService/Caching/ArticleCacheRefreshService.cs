using ArticleService.Caching.Interfaces;
using ArticleService.DAL.Repositories.Interfaces;
using Monitoring;

namespace ArticleService.Caching;

public class ArticleCacheRefreshService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IArticleCache _cache;
    private readonly ILogger<ArticleCacheRefreshService> _logger;

    public ArticleCacheRefreshService(
        IServiceScopeFactory scopeFactory,
        IArticleCache cache,
        ILogger<ArticleCacheRefreshService> logger)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
        _logger = logger;
    }

    // Baggrundsprocessen kører uafhængigt af brugerrequests (offline process).
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        await Task.Yield();

        // Opdatér periodisk hvert 30. sekund.
        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(30));

        do
        {
            try
            {
                // Første opdatering sker ved opstart, før timeren afventes.
                await RefreshAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Refreshing the global article cache failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RefreshAsync(
        CancellationToken cancellationToken)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity(
                "ArticleCache.Refresh");

        await using var scope =
            _scopeFactory.CreateAsyncScope();

        var repository = scope.ServiceProvider
            .GetRequiredService<IArticleRepository>();

        var now = DateTime.UtcNow;

        // Hent globale artikler fra de seneste 14 dage.
        var articles = await repository.GetRecentGlobalAsync(
            now.AddDays(-14),
            now,
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        // Erstat hele artikelcachen i Redis med det nye sæt.
        await _cache.ReplaceAsync(articles);

        _logger.LogInformation(
            "Global article cache refreshed with {ArticleCount} articles",
            articles.Count);
    }
}