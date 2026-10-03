using System.Net.Mail;
using Contracts;
using Contracts.Messages;
using Monitoring;
using NewsletterService.BE;
using NewsletterService.BLL.Interfaces;
using NewsletterService.Clients.Interfaces;
using NewsletterService.DAL.Repositories.Interfaces;

namespace NewsletterService.BLL;

public class NewsletterLogic : INewsletterLogic
{
    private readonly ISubscriberRepository _repository;
    private readonly IArticleClient _articleClient;
    private readonly IEmailSender _emailSender;

    public NewsletterLogic(
        ISubscriberRepository repository,
        IArticleClient articleClient,
        IEmailSender emailSender)
    {
        _repository = repository;
        _articleClient = articleClient;
        _emailSender = emailSender;
    }

    public async Task<Subscriber> SubscribeAsync(
        string email,
        CancellationToken cancellationToken)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity(
                "NewsletterLogic.Subscribe");

        if (string.IsNullOrWhiteSpace(email) || email.Length > 320)
        {
            throw new ArgumentException("Invalid email address.");
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (!MailAddress.TryCreate(normalizedEmail, out var address)
            || address.Address != normalizedEmail)
        {
            throw new ArgumentException("Invalid email address.");
        }

        return await _repository.AddAsync(
            normalizedEmail,
            cancellationToken);
    }

    public Task<List<Subscriber>> GetSubscribersAsync(
        CancellationToken cancellationToken)
    {
        return _repository.GetAllAsync(cancellationToken);
    }

    public async Task<int> SendImmediateAsync(
        ArticlePublished article,
        CancellationToken cancellationToken)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity(
                "NewsletterLogic.SendImmediate");

        var region = ArticleRegions.Normalize(article.Region);

        if (article.Id == Guid.Empty
            || string.IsNullOrWhiteSpace(article.Title)
            || string.IsNullOrWhiteSpace(article.Content))
        {
            throw new ArgumentException("Invalid article message.");
        }

        var subscribers = await _repository.GetAllAsync(
            cancellationToken);

        var body =
            $"Ny positiv nyhed fra {region}\n\n" +
            $"{article.Title}\n\n" +
            article.Content;

        foreach (var subscriber in subscribers)
        {
            await _emailSender.SendAsync(
                subscriber.Email,
                $"HappyHeadlines: {article.Title}",
                body,
                cancellationToken);
        }

        MonitorService.Log.Information(
            "Immediate newsletter for article {ArticleId} sent to {Count} subscribers",
            article.Id,
            subscribers.Count);

        return subscribers.Count;
    }

    public async Task<int> SendDailyAsync(
        string region,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity(
                "NewsletterLogic.SendDaily");

        var normalizedRegion = ArticleRegions.Normalize(region);

        if (date == default)
        {
            throw new ArgumentException("A date is required.");
        }

        // HTTP-kald gennem loadbalanceren til ArticleService.
        var articles = await _articleClient.GetForDayAsync(
            normalizedRegion,
            date,
            cancellationToken);

        if (articles.Count == 0)
        {
            MonitorService.Log.Information(
                "No articles found for {Region} on {Date}",
                normalizedRegion,
                date);

            return 0;
        }

        var subscribers = await _repository.GetAllAsync(
            cancellationToken);

        var body =
            $"HappyHeadlines — {normalizedRegion} — {date:yyyy-MM-dd}\n\n"
            + string.Join(
                "\n\n--------------------\n\n",
                articles.Select(article =>
                    $"{article.Title}\n\n{article.Content}"));

        foreach (var subscriber in subscribers)
        {
            await _emailSender.SendAsync(
                subscriber.Email,
                $"HappyHeadlines: dagens nyheder {date:yyyy-MM-dd}",
                body,
                cancellationToken);
        }

        MonitorService.Log.Information(
            "Daily newsletter with {ArticleCount} articles sent to {SubscriberCount} subscribers",
            articles.Count,
            subscribers.Count);

        return subscribers.Count;
    }
}