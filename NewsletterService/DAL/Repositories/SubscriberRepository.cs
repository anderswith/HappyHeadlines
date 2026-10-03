using Microsoft.EntityFrameworkCore;
using Monitoring;
using NewsletterService.BE;
using NewsletterService.DAL.Repositories.Interfaces;

namespace NewsletterService.DAL.Repositories;

public class SubscriberRepository : ISubscriberRepository
{
    private readonly NewsletterContext _context;

    public SubscriberRepository(NewsletterContext context)
    {
        _context = context;
    }

    public async Task<Subscriber> AddAsync(
        string email,
        CancellationToken cancellationToken)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity(
                "NewsletterDatabase.Subscribe");

        var id = Guid.NewGuid();
        var createdUtc = DateTime.UtcNow;

        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO subscribers (id, email, created_utc)
             VALUES ({id}, {email}, {createdUtc})
             ON CONFLICT (email) DO NOTHING
             """,
            cancellationToken);

        return await _context.Subscribers
            .AsNoTracking()
            .SingleAsync(
                subscriber => subscriber.Email == email,
                cancellationToken);
    }

    public async Task<List<Subscriber>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity(
                "NewsletterDatabase.ReadSubscribers");

        return await _context.Subscribers
            .AsNoTracking()
            .OrderBy(subscriber => subscriber.CreatedUtc)
            .ToListAsync(cancellationToken);
    }
}