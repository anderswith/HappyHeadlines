using Contracts.Messages;

namespace PublisherService.DAL.Interfaces;

public interface IArticlePublisher
{
    Task PublishAsync(
        ArticlePublished article,
        CancellationToken cancellationToken);
}