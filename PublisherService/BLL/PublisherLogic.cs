using Contracts;
using Contracts.Messages;
using Monitoring;
using PublisherService.BLL.Interfaces;
using PublisherService.DAL.Interfaces;

namespace PublisherService.BLL;

public class PublisherLogic : IPublisherLogic
{
    private readonly IArticlePublisher _publisher;

    public PublisherLogic(IArticlePublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task<Guid> PublishAsync(
        string region,
        string title,
        string content,
        CancellationToken cancellationToken)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity(
                "PublisherLogic.Publish");

        var normalizedRegion = ArticleRegions.Normalize(region);

        if (string.IsNullOrWhiteSpace(title) || title.Length > 200)
        {
            throw new ArgumentException(
                "Title must contain between 1 and 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(content) || content.Length > 50000)
        {
            throw new ArgumentException(
                "Content must contain between 1 and 50000 characters.");
        }

        var article = new ArticlePublished
        {
            Id = Guid.NewGuid(),
            Region = normalizedRegion,
            Title = title.Trim(),
            Content = content.Trim(),
            CreatedUtc = DateTime.UtcNow
        };

        await _publisher.PublishAsync(article, cancellationToken);

        return article.Id;
    }
}