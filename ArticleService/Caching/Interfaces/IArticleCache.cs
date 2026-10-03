using ArticleService.BE;

namespace ArticleService.Caching.Interfaces;

public interface IArticleCache
{
    Task<Article?> GetByIdAsync(Guid id);

    Task ReplaceAsync(IReadOnlyCollection<Article> articles);
}