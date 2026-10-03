using ArticleService.BE;
using Contracts.Messages;

namespace ArticleService.BLL.Interfaces;

public interface IArticleLogic
{
    Task<Article> CreateAsync(
        ArticleRegion region,
        string title,
        string content);

    Task<Article?> GetByIdAsync(
        ArticleRegion region,
        Guid id);

    Task<List<Article>> GetForDayAsync(
        ArticleRegion region,
        DateOnly date,
        CancellationToken cancellationToken);

    Task ReceivePublishedAsync(
        ArticlePublished message,
        CancellationToken cancellationToken);

    Task<bool> UpdateAsync(
        ArticleRegion region,
        Guid id,
        string title,
        string content);

    Task<bool> DeleteAsync(
        ArticleRegion region,
        Guid id);
}