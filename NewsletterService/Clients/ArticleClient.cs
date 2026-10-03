using System.Globalization;
using Monitoring;
using NewsletterService.Clients.Interfaces;
using NewsletterService.DTO;

namespace NewsletterService.Clients;

public class ArticleClient : IArticleClient
{
    private readonly HttpClient _httpClient;

    public ArticleClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ArticleResponse>> GetForDayAsync(
        string region,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity(
                "ArticleClient.GetForDay");

        var formattedDate =
            date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var url =
            $"api/{Uri.EscapeDataString(region)}/articles" +
            $"?date={formattedDate}";

        using var response = await _httpClient.GetAsync(
            url,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content
                   .ReadFromJsonAsync<List<ArticleResponse>>(
                       cancellationToken: cancellationToken)
               ?? throw new InvalidOperationException(
                   "ArticleService returned an empty response.");
    }
}