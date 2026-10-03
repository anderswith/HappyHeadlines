using NewsletterService.DTO;

namespace NewsletterService.Clients.Interfaces;

public interface IArticleClient
{
    Task<List<ArticleResponse>> GetForDayAsync(
        string region,
        DateOnly date,
        CancellationToken cancellationToken);
}