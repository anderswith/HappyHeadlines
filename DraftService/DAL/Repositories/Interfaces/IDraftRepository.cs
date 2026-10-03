using DraftService.BE;

namespace DraftService.DAL.Repositories.Interfaces;

public interface IDraftRepository
{
    Task AddAsync(Draft draft, CancellationToken cancellationToken);

    Task<Draft?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<List<Draft>> GetAllAsync(
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task DeleteAsync(Draft draft, CancellationToken cancellationToken);
}