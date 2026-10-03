using CommentService.BE;

namespace CommentService.Caching.Interfaces;

public interface ICommentCache
{

    Task<List<Comment>?> GetAsync(string region, Guid articleId);

    // Læs tokenet, før vi starter databaseopslaget.
    Task<string> GetVersionAsync();

    // Gem kun resultatet, hvis tokenet ikke har ændret sig.
    Task SetAsync(
        string region,
        Guid articleId,
        IReadOnlyCollection<Comment> comments,
        string expectedVersion);

    Task RemoveAsync(string region, Guid articleId);
}