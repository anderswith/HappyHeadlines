using System.Diagnostics;
using Contracts.Messages;
using EasyNetQ;
using Monitoring;
using PublisherService.DAL.Interfaces;

namespace PublisherService.DAL;

public class ArticlePublisher : IArticlePublisher
{
    private readonly IBus _bus;

    public ArticlePublisher(IBus bus)
    {
        _bus = bus;
    }

    public async Task PublishAsync(
        ArticlePublished article,
        CancellationToken cancellationToken)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity(
                "RabbitMQ.PublishArticle",
                ActivityKind.Producer);

        activity?.SetTag("article.id", article.Id.ToString());
        activity?.SetTag("article.region", article.Region);

        // Overfører den aktive producers trace-context til beskeden.
        TraceContextHelper.Inject(article.Header);

        try
        {
            await _bus.PubSub.PublishAsync(
                article,
                cancellationToken);

            MonitorService.Log.Information(
                "Article {ArticleId} published to RabbitMQ for {Region}",
                article.Id,
                article.Region);
        }
        catch (Exception exception)
        {
            activity?.SetStatus(
                ActivityStatusCode.Error,
                exception.Message);

            MonitorService.Log.Error(
                exception,
                "Publishing article {ArticleId} failed",
                article.Id);

            throw;
        }
    }
}