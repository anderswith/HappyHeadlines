using System.Diagnostics;
using Contracts.Messages;
using EasyNetQ;
using Monitoring;
using NewsletterService.BLL.Interfaces;

namespace NewsletterService.Messaging;

public class NewsletterConsumer : IHostedService
{
    private readonly IBus _bus;
    private readonly IServiceScopeFactory _scopeFactory;
    private IDisposable? _subscription;

    public NewsletterConsumer(
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
                "newsletter-delivery",
                HandleAsync,
                configuration => configuration.WithPrefetchCount(1),
                cancellationToken);

        MonitorService.Log.Information(
            "Newsletter consumer ready");
    }

    private async Task HandleAsync(
        ArticlePublished message,
        CancellationToken cancellationToken)
    {
        var parentContext =
            TraceContextHelper.Extract(message.Header);

        using var activity =
            MonitorService.ActivitySource.StartActivity(
                "NewsletterService.ReceiveArticle",
                ActivityKind.Consumer,
                parentContext);

        activity?.SetTag("article.id", message.Id.ToString());

        try
        {
            await using var scope =
                _scopeFactory.CreateAsyncScope();

            var logic = scope.ServiceProvider
                .GetRequiredService<INewsletterLogic>();

            await logic.SendImmediateAsync(
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
                "Newsletter for article {ArticleId} failed",
                message.Id);

            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _subscription?.Dispose();

        return Task.CompletedTask;
    }
}