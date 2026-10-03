using System.Diagnostics;
using ArticleService.BLL.Interfaces;
using Contracts.Messages;
using EasyNetQ;
using Monitoring;

namespace ArticleService.Messaging;

public class ArticleConsumer : IHostedService
{
    private readonly IBus _bus;
    private readonly IServiceScopeFactory _scopeFactory;
    private IDisposable? _subscription;

    public ArticleConsumer(
        IBus bus,
        IServiceScopeFactory scopeFactory)
    {
        _bus = bus;
        _scopeFactory = scopeFactory;
    }

    public async Task StartAsync(
        CancellationToken cancellationToken)
    {
        _subscription =
            await _bus.PubSub.SubscribeAsync<ArticlePublished>(
                "article-storage",
                HandleAsync,
                configuration => configuration.WithPrefetchCount(1),
                cancellationToken);

        MonitorService.Log.Information(
            "Article consumer ready on {Instance}",
            Environment.GetEnvironmentVariable("HOSTNAME"));
    }

    private async Task HandleAsync(
        ArticlePublished message,
        CancellationToken cancellationToken)
    {
        // Læser contexten fra afsenderens besked.
        var parentContext =
            TraceContextHelper.Extract(message.Header);

        using var activity =
            MonitorService.ActivitySource.StartActivity(
                "ArticleService.ReceiveArticle",
                ActivityKind.Consumer,
                parentContext);

        activity?.SetTag("article.id", message.Id.ToString());
        activity?.SetTag("article.region", message.Region);

        try
        {
            await using var scope =
                _scopeFactory.CreateAsyncScope();

            var logic = scope.ServiceProvider
                .GetRequiredService<IArticleLogic>();

            await logic.ReceivePublishedAsync(
                message,
                cancellationToken);
        }
        catch (Exception exception)
        {
            activity?.SetStatus(
                ActivityStatusCode.Error,
                exception.Message);

            MonitorService.Log.Error(
                exception,
                "Processing article {ArticleId} failed",
                message.Id);

            // Fejlen skal også være synlig for EasyNetQ.
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _subscription?.Dispose();

        return Task.CompletedTask;
    }
}