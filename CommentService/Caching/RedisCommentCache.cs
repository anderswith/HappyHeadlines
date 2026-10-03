using System.Text.Json;
using CommentService.BE;
using CommentService.Caching.Interfaces;
using StackExchange.Redis;

namespace CommentService.Caching;

public class RedisCommentCache : ICommentCache
{
    private const int Capacity = 30;

    // Hash: artikelens nøgle -> kommentarliste som JSON.
    private const string DataKey = "cache:{comments}:data";

    // Sorted set: holder rækkefølgen for seneste brug.
    // Laveste score er den artikel, der længst ikke er blevet brugt.
    private const string LruKey = "cache:{comments}:lru";

    // En stigende tæller giver hvert opslag en entydig rækkefølge.
    // Dermed behøver vi ikke sammenligne API-instansernes ure.
    private const string SequenceKey = "cache:{comments}:sequence";
    private const string StatisticsKey = "cache:{comments}:statistics";
    
    // Én udløbstid pr. cachet artikels kommentarliste.
    // Redis-hash-felterne udløber ikke automatisk
    // så læsescriptet skal kontrollere tiderne.
    private const string ExpiryKey = "cache:{comments}:expiry";

    // En liste må bruges i højst to minutter efter indlæsning.
    private const int LifetimeSeconds = 120;
    
    // Bruges til at afvise cachefyldning fra ældre databaseopslag.
    private const string VersionKey = "cache:{comments}:version";

    private readonly IDatabase _database;

    public RedisCommentCache(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }
    
    public async Task<string> GetVersionAsync()
    {
        var version = await _database.StringGetAsync(VersionKey);

        // Før første invalidering findes nøglen ikke.
        return version.IsNull ? "" : version.ToString();
    }

    public async Task<List<Comment>?> GetAsync(
        string region,
        Guid articleId)
    {
        var entryKey = CreateEntryKey(region, articleId);

        const string script = """
          local value = redis.call('HGET', KEYS[1], ARGV[1])
          local expiresAt = redis.call('HGET', KEYS[5], ARGV[1])
          local now = tonumber(redis.call('TIME')[1])

          -- Listen skal både findes og have en gyldig udløbstid.
          -- Gamle entries fra før denne ændring mangler udløbstid
          -- og bliver derfor også behandlet som misses.
          if not value or not expiresAt or tonumber(expiresAt) <= now then
              redis.call('HDEL', KEYS[1], ARGV[1])
              redis.call('ZREM', KEYS[2], ARGV[1])
              redis.call('HDEL', KEYS[5], ARGV[1])
          
              redis.call('HINCRBY', KEYS[4], 'misses', 1)
              return false
          end

          redis.call('HINCRBY', KEYS[4], 'hits', 1)

          -- Opdatér kun LRU-rækkefølgen.
          -- Udløbstiden forlænges ikke, selvom listen læses ofte.
          local sequence = redis.call('INCR', KEYS[3])
          redis.call('ZADD', KEYS[2], sequence, ARGV[1])

          return value
          """;

        var result = await _database.ScriptEvaluateAsync(
            script,
            new RedisKey[]
            {
                DataKey,
                LruKey,
                SequenceKey,
                StatisticsKey,
                ExpiryKey
            },
            new RedisValue[] { entryKey });

        if (result.IsNull)
        {
            return null;
        }

        return JsonSerializer.Deserialize<List<Comment>>(
            result.ToString());
    }

    public async Task SetAsync(
        string region,
        Guid articleId,
        IReadOnlyCollection<Comment> comments,
        string expectedVersion)
    {
        var entryKey = CreateEntryKey(region, articleId);
        var json = JsonSerializer.Serialize(comments);

        const string script = """
          -- Kontroller tokenet før nogen cacheændringer.
          local currentVersion = redis.call('GET', KEYS[5]) or ''
          
          if currentVersion ~= ARGV[5] then
              -- Der er sket en invalidering under databaseopslaget.
              -- Undlad at cache resultatet.
              return 0
          end
          -- Gem hele kommentarsamlingen.
          redis.call('HSET', KEYS[1], ARGV[1], ARGV[2])

          -- Markér artiklen som senest brugt.
          local sequence = redis.call('INCR', KEYS[3])
          redis.call('ZADD', KEYS[2], sequence, ARGV[1])

          -- Brug Redis-serverens ur til at beregne udløbstiden.
          -- TIME returnerer sekunder og mikrosekunder.
          local now = tonumber(redis.call('TIME')[1])
          local expiresAt = now + tonumber(ARGV[4])
          redis.call('HSET', KEYS[4], ARGV[1], expiresAt)

          -- Hold fortsat kapaciteten på højst 30 artikler.
          local capacity = tonumber(ARGV[3])

          while redis.call('ZCARD', KEYS[2]) > capacity do
              local oldest = redis.call('ZRANGE', KEYS[2], 0, 0)[1]
          
              -- Fjern data, LRU-placering og udløbstid samlet.
              redis.call('HDEL', KEYS[1], oldest)
              redis.call('ZREM', KEYS[2], oldest)
              redis.call('HDEL', KEYS[4], oldest)
          end

          return 1
          """;

        await _database.ScriptEvaluateAsync(
            script,
            new RedisKey[]
            {
                DataKey,
                LruKey,
                SequenceKey,
                ExpiryKey,
                VersionKey
            },
            new RedisValue[]
            {
                entryKey,
                json,
                Capacity,
                LifetimeSeconds,
                expectedVersion
            });
    }

    public async Task RemoveAsync(
        string region,
        Guid articleId)
    {
        var entryKey = CreateEntryKey(region, articleId);

        // Et nyt token markerer, at kommentarer er blevet ændret.
        var newVersion = Guid.NewGuid().ToString("N");

        const string script = """
          -- Skift token og fjern den gamle liste samlet.
          redis.call('SET', KEYS[4], ARGV[2])

          redis.call('HDEL', KEYS[1], ARGV[1])
          redis.call('ZREM', KEYS[2], ARGV[1])
          redis.call('HDEL', KEYS[3], ARGV[1])

          return 1
          """;

        await _database.ScriptEvaluateAsync(
            script,
            new RedisKey[]
            {
                DataKey,
                LruKey,
                ExpiryKey,
                VersionKey
            },
            new RedisValue[] { entryKey, newVersion });
    }

    private static string CreateEntryKey(
        string region,
        Guid articleId)
    {
        // Hold kommentarsamlinger for forskellige regioner adskilt.
        return $"{region}:{articleId:N}";
    }
}