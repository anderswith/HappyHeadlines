using DraftService.BE;

namespace DraftService.BLL.Interfaces;

public interface IDraftLogic
{
    Task<Draft> CreateAsync(
        string author,
        string title,
        string content,
        CancellationToken cancellationToken);

    Task<Draft?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<List<Draft>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<bool> UpdateAsync(
        Guid id,
        string author,
        string title,
        string content,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken);
}