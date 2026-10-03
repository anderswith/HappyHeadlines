using System.Text.Json;
using ArticleService.BE;
using ArticleService.Caching.Interfaces;
using StackExchange.Redis;
using Monitoring;

namespace ArticleService.Caching;

// Håndterer læsning, statistik og udskiftning af artikelcachen i Redis.
public class RedisArticleCache : IArticleCache
{
    private const string CacheKey = "cache:{global-articles}:current";
    private const string StatisticsKey = "cache:global-articles:statistics";

    private readonly IDatabase _database;

    public RedisArticleCache(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    public async Task<Article?> GetByIdAsync(Guid id)
    {
        // Hent artiklen fra Redis.
        var json = await _database.HashGetAsync(
            CacheKey,
            id.ToString());

        if (json.IsNullOrEmpty)
        {
            await RecordLookupAsync("misses");
            return null;
        }

        var article = JsonSerializer.Deserialize<Article>(
            json.ToString());

        // Afvis artikler, der har passeret 14-dagesgrænsen.
        if (article is null ||
            article.CreatedUtc < DateTime.UtcNow.AddDays(-14))
        {
            await RecordLookupAsync("misses");
            return null;
        }

        await RecordLookupAsync("hits");

        return article;
    }

    private async Task RecordLookupAsync(string counter)
    {
        try
        {
            // Forøg den fælles hit- eller miss-tæller.
            await _database.HashIncrementAsync(
                StatisticsKey,
                counter,
                1);
        }
        catch (RedisException exception)
        {
            // Statistikfejl må ikke forhindre returnering af artiklen.
            MonitorService.Log.Warning(
                exception,
                "Recording article cache {Counter} failed",
                counter);
        }
    }

    public async Task ReplaceAsync(
        IReadOnlyCollection<Article> articles)
    {
        var temporaryKey =
            $"cache:{{global-articles}}:loading:{Guid.NewGuid():N}";

        var entries = new List<HashEntry>
        {
            // Opret også nøglen, når artikelsættet er tomt.
            new("_refreshedUtc", DateTime.UtcNow.ToString("O"))
        };

        foreach (var article in articles)
        {
            entries.Add(new HashEntry(
                article.Id.ToString(),
                JsonSerializer.Serialize(article)));
        }

        try
        {
            // Byg det nye sæt uden at ændre den aktive cache.
            await _database.HashSetAsync(
                temporaryKey,
                entries.ToArray());

            // Udløb efter to minutter uden en ny opdatering.
            await _database.KeyExpireAsync(
                temporaryKey,
                TimeSpan.FromMinutes(2));

            // Erstat den aktive cache med det nye sæt.
            await _database.KeyRenameAsync(
                temporaryKey,
                CacheKey);
        }
        finally
        {
            // Ryd en eventuel resterende midlertidig nøgle.
            await _database.KeyDeleteAsync(temporaryKey);
        }
    }
}