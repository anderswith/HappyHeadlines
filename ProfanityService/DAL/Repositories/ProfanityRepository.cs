using Microsoft.EntityFrameworkCore;
using ProfanityService.DAL.Repositories.Interfaces;

namespace ProfanityService.DAL.Repositories;

public class ProfanityRepository : IProfanityRepository
{
    private readonly ProfanityContext _context;

    public ProfanityRepository(ProfanityContext context)
    {
        _context = context;
    }

    public Task<List<string>> GetWordsAsync(
        CancellationToken cancellationToken)
    {
        return _context.ProfanityWords
            .AsNoTracking()
            .Select(w => w.Word)
            .ToListAsync(cancellationToken);
    }
}