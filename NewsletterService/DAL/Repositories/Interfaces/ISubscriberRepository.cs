using NewsletterService.BE;

namespace NewsletterService.DAL.Repositories.Interfaces;

public interface ISubscriberRepository
{
    Task<Subscriber> AddAsync(
        string email,
        CancellationToken cancellationToken);

    Task<List<Subscriber>> GetAllAsync(
        CancellationToken cancellationToken);
}