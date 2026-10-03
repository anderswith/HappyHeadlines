using System.Net.Http.Json;
using CommentService.Clients.Interfaces;

namespace CommentService.Clients;

public class ProfanityClient : IProfanityClient
{
    private readonly HttpClient _httpClient;

    public ProfanityClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> FilterAsync(
        string text,
        CancellationToken cancellationToken)
    {
        Console.WriteLine("Kalder ProfanityService.");

        using var response = await _httpClient.PostAsJsonAsync(
            "api/profanity/filter",
            new { Text = text },
            cancellationToken
        );

        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<FilterResult>(
                cancellationToken: cancellationToken);

        if (result?.FilteredText is null)
        {
            throw new HttpRequestException(
                "ProfanityService returned an invalid response.");
        }

        return result.FilteredText;
    }

    public class FilterResult
    {
        public string? FilteredText { get; set; }
    }
}