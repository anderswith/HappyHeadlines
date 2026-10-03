namespace ProfanityService.BLL.Interfaces;

public interface IProfanityLogic
{
    Task<string> FilterAsync(
        string text,
        CancellationToken cancellationToken);
}