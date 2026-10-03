namespace ProfanityService.DAL.Repositories.Interfaces;

public interface IProfanityRepository
{
    Task<List<string>> GetWordsAsync(
        CancellationToken cancellationToken);
}