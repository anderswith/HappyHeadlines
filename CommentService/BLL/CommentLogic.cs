using CommentService.BE;
using CommentService.BLL.Interfaces;
using CommentService.Clients.Interfaces;
using CommentService.DAL.Repositories.Interfaces;
using CommentService.Caching.Interfaces;
using StackExchange.Redis;

namespace CommentService.BLL;

public class CommentLogic : ICommentLogic
{
    private readonly ICommentRepository _repository;
    private readonly IProfanityClient _profanityClient;
    private readonly ICommentCache _cache;
    private readonly ILogger<CommentLogic> _logger;

    public CommentLogic(
        ICommentRepository repository,
        IProfanityClient profanityClient,
        ICommentCache cache,
        ILogger<CommentLogic> logger)
    {
        _repository = repository;
        _profanityClient = profanityClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Comment> CreateAsync(
        Guid articleId,
        string articleRegion,
        string author,
        string text,
        CancellationToken cancellationToken)
    {
        if (articleId == Guid.Empty)
        {
            throw new ArgumentException("ArticleId is required.");
        }

        if (string.IsNullOrWhiteSpace(author) || author.Length > 100)
        {
            throw new ArgumentException(
                "Author must contain between 1 and 100 characters.");
        }

        if (string.IsNullOrWhiteSpace(text) || text.Length > 10000)
        {
            throw new ArgumentException(
                "Text must contain between 1 and 10000 characters.");
        }

        ValidateRegion(articleRegion);

        // Kommentaren gemmes først, når filtreringen er lykkedes.
        var filteredText = await _profanityClient.FilterAsync(
            text.Trim(),
            cancellationToken
        );

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            ArticleId = articleId,
            ArticleRegion = articleRegion,
            Author = author.Trim(),
            Text = filteredText,
            CreatedUtc = DateTime.UtcNow
        };
        // Gem først kommentaren permanent.
        // Hvis databaseoprettelsen fejler, fortsætter vi ikke til cachefjernelsen.
        await _repository.AddAsync(comment, cancellationToken);

        try
        {
            // Den gamle cachede liste indeholder ikke den nye kommentar.
            // fjernes så næste læsning henter den opdaterede liste.
            await _cache.RemoveAsync(
                articleRegion,
                articleId);
        }
        catch (RedisException exception)
        {
            // Kommentaren er allerede gemt i databasen.
            // Log cachefejlen uden at melde oprettelsen som mislykket.
            _logger.LogWarning(
                exception,
                "Invalidating comment cache failed for {Region}/{ArticleId}",
                articleRegion,
                articleId);
        }

        return comment;
    }

    public async Task<List<Comment>> GetByArticleAsync(
    string region,
    Guid articleId,
    CancellationToken cancellationToken)
    {
        ValidateRegion(region);

        if (articleId == Guid.Empty)
        {
            throw new ArgumentException("ArticleId is required.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        // null betyder, at vi ikke har et gyldigt token
        // og derfor ikke må forsøge at cache databaseresultatet.
        string? version = null;

        try
        {
            var cachedComments = await _cache.GetAsync(
                region,
                articleId);

            if (cachedComments is not null)
            {
                _logger.LogInformation(
                    "Comment cache HIT for {Region}/{ArticleId}",
                    region,
                    articleId);

                return cachedComments;
            }

            _logger.LogInformation(
                "Comment cache MISS for {Region}/{ArticleId}",
                region,
                articleId);

            // Tokenet skal læses før databaseopslaget.
            version = await _cache.GetVersionAsync();
        }
        catch (RedisException exception)
        {
            _logger.LogWarning(
                exception,
                "Comment cache unavailable; reading from database");
        }

        var comments = await _repository.GetByArticleAsync(
            region,
            articleId,
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        if (version is not null)
        {
            try
            {
                // Redis gemmer kun listen, hvis ingen invalidering
                // har ændret tokenet siden før databaseopslaget.
                await _cache.SetAsync(
                    region,
                    articleId,
                    comments,
                    version);
            }
            catch (RedisException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Caching comments failed for {Region}/{ArticleId}",
                    region,
                    articleId);
            }
        }

        // Det aktuelle request får stadig databaseresultatet.
        // Tokenet beskytter mod at gemme en gammel liste til senere requests.
        return comments;
    }

    private static void ValidateRegion(string region)
    {
        string[] regions =
        [
            "Europe", "Asia", "Africa", "NorthAmerica",
            "SouthAmerica", "Oceania", "Antarctica", "Global"
        ];

        if (!regions.Contains(region))
        {
            throw new ArgumentException("Unknown article region.");
        }
    }
}