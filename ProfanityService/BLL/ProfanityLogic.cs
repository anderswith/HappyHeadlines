using System.Text.RegularExpressions;
using ProfanityService.BLL.Interfaces;
using ProfanityService.DAL.Repositories.Interfaces;

namespace ProfanityService.BLL;

public class ProfanityLogic : IProfanityLogic
{
    private readonly IProfanityRepository _repository;

    public ProfanityLogic(IProfanityRepository repository)
    {
        _repository = repository;
    }

    public async Task<string> FilterAsync(
        string text,
        CancellationToken cancellationToken)
    {
        var words = await _repository.GetWordsAsync(
            cancellationToken);

        var filteredText = text;

        foreach (var word in words)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Match hele ord, så dele af andre ord ikke censureres.
            var pattern = $@"(?<!\w){Regex.Escape(word)}(?!\w)";

            filteredText = Regex.Replace(
                filteredText,
                pattern,
                match => new string('*', match.Length),
                RegexOptions.IgnoreCase |
                RegexOptions.CultureInvariant,
                TimeSpan.FromMilliseconds(100)
            );
        }

        return filteredText;
    }
}