using ArticleService.BE;

namespace ArticleService.DAL.Repositories.Interfaces;

public interface IArticleRepository
{
    Task AddAsync(
        ArticleRegion region,
        Article article);

    Task AddPublishedAsync(
        ArticleRegion region,
        Article article,
        CancellationToken cancellationToken);

    Task<Article?> GetByIdAsync(
        ArticleRegion region,
        Guid id);

    Task<List<Article>> GetForDayAsync(
        ArticleRegion region,
        DateOnly date,
        CancellationToken cancellationToken);

    Task<bool> UpdateAsync(
        ArticleRegion region,
        Guid id,
        string title,
        string content);

    Task<bool> DeleteAsync(
        ArticleRegion region,
        Guid id);
    
    Task<List<Article>> GetRecentGlobalAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken);
}