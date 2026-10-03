using DraftService.BE;
using DraftService.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Monitoring;

namespace DraftService.DAL.Repositories;

public class DraftRepository : IDraftRepository
{
    private readonly DraftContext _context;

    public DraftRepository(DraftContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        Draft draft,
        CancellationToken cancellationToken)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity("DraftDatabase.Insert");

        activity?.SetTag("draft.id", draft.Id.ToString());

        await _context.Drafts.AddAsync(draft, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Draft?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity("DraftDatabase.Read");

        activity?.SetTag("draft.id", id.ToString());

        // Tracking beholdes, så BLL kan ændre entiteten og gemme den.
        return await _context.Drafts
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<List<Draft>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity("DraftDatabase.List");

        return await _context.Drafts
            .AsNoTracking()
            .OrderByDescending(d => d.UpdatedUtc)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity("DraftDatabase.Update");

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Draft draft,
        CancellationToken cancellationToken)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity("DraftDatabase.Delete");

        activity?.SetTag("draft.id", draft.Id.ToString());

        _context.Drafts.Remove(draft);

        await _context.SaveChangesAsync(cancellationToken);
    }
}