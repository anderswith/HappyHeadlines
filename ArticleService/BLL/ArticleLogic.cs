using ArticleService.BE;
using ArticleService.BLL.Interfaces;
using ArticleService.DAL.Repositories.Interfaces;
using Contracts;
using Contracts.Messages;
using Monitoring;
using ArticleService.Caching.Interfaces;
using StackExchange.Redis;

namespace ArticleService.BLL;

public class ArticleLogic : IArticleLogic
{
    private readonly IArticleRepository _repository;
    private readonly IArticleCache _cache;

    public ArticleLogic(
        IArticleRepository repository,
        IArticleCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<Article> CreateAsync(
        ArticleRegion region,
        string title,
        string content)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity(
                "ArticleLogic.Create");

        ValidateArticle(title, content);

        var article = new Article
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Content = content.Trim(),
            CreatedUtc = DateTime.UtcNow
        };

        await _repository.AddAsync(region, article);

        return article;
    }

    public async Task ReceivePublishedAsync(
        ArticlePublished message,
        CancellationToken cancellationToken)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity(
                "ArticleLogic.ReceivePublished");

        var regionName = ArticleRegions.Normalize(message.Region);
        var region = Enum.Parse<ArticleRegion>(regionName);

        ValidateArticle(message.Title, message.Content);

        if (message.Id == Guid.Empty)
        {
            throw new ArgumentException("Article ID is required.");
        }

        if (message.CreatedUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Article timestamp must be UTC.");
        }

        var article = new Article
        {
            Id = message.Id,
            Title = message.Title.Trim(),
            Content = message.Content.Trim(),
            CreatedUtc = message.CreatedUtc
        };

        await _repository.AddPublishedAsync(
            region,
            article,
            cancellationToken);

        MonitorService.Log.Information(
            "Article {ArticleId} persisted in {Region}",
            article.Id,
            region);
    }

    public async Task<Article?> GetByIdAsync(
        ArticleRegion region,
        Guid id)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity(
                "ArticleLogic.Read");

        if (region == ArticleRegion.Global)
        {
            try
            {
                // Forsøg først at hente artiklen fra Redis.
                // Cacheklassen returnerer null, hvis artiklen ikke findes
                // eller er ældre end cachens grænse på 14 dage.
                var cachedArticle = await _cache.GetByIdAsync(id);

                if (cachedArticle is not null)
                {
                    // Et cache hit betyder, at artiklen findes i cachen.
                    activity?.SetTag("cache.result", "hit");

                    MonitorService.Log.Information(
                        "Article cache HIT for {ArticleId}",
                        id);
                    
                    // Returnér den cachede artikel med det samme.
                    // Dermed undgår vi et opslag i databasen.
                    return cachedArticle;
                }
                // Et cache miss betyder, at cachen ikke kunne levere artiklen.
                // Metoden fortsætter derfor til databaseopslaget.
                activity?.SetTag("cache.result", "miss");

                MonitorService.Log.Information(
                    "Article cache MISS for {ArticleId}",
                    id);
            }
            catch (RedisException exception)
            {
                activity?.SetTag("cache.result", "error");

                MonitorService.Log.Warning(
                    exception,
                    "Article cache unavailable; reading from database");
            }
        }

        return await _repository.GetByIdAsync(region, id);
    }

    public async Task<List<Article>> GetForDayAsync(
        ArticleRegion region,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity(
                "ArticleLogic.GetForDay");

        return await _repository.GetForDayAsync(
            region,
            date,
            cancellationToken);
    }

    public async Task<bool> UpdateAsync(
        ArticleRegion region,
        Guid id,
        string title,
        string content)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity(
                "ArticleLogic.Update");

        ValidateArticle(title, content);

        return await _repository.UpdateAsync(
            region,
            id,
            title.Trim(),
            content.Trim());
    }

    public async Task<bool> DeleteAsync(
        ArticleRegion region,
        Guid id)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity(
                "ArticleLogic.Delete");

        return await _repository.DeleteAsync(region, id);
    }

    private static void ValidateArticle(
        string title,
        string content)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length > 200)
        {
            throw new ArgumentException(
                "Title must contain between 1 and 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Content cannot be empty.");
        }
    }
}