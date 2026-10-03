using Contracts.Messages;
using NewsletterService.BE;

namespace NewsletterService.BLL.Interfaces;

public interface INewsletterLogic
{
    Task<Subscriber> SubscribeAsync(
        string email,
        CancellationToken cancellationToken);

    Task<List<Subscriber>> GetSubscribersAsync(
        CancellationToken cancellationToken);

    Task<int> SendImmediateAsync(
        ArticlePublished article,
        CancellationToken cancellationToken);

    Task<int> SendDailyAsync(
        string region,
        DateOnly date,
        CancellationToken cancellationToken);
}