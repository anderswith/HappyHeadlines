using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Monitoring;
using PublisherService.BLL.Interfaces;
using PublisherService.DTO;

namespace PublisherService.Controllers;

[ApiController]
[Route("api/publisher")]
public class PublisherController : ControllerBase
{
    private readonly IPublisherLogic _logic;

    public PublisherController(IPublisherLogic logic)
    {
        _logic = logic;
    }

    [HttpPost("articles")]
    public async Task<IActionResult> Publish(
        PublishArticleRequest request,
        CancellationToken cancellationToken)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity(
                "PublisherController.Publish");

        var articleId = await _logic.PublishAsync(
            request.Region,
            request.Title,
            request.Content,
            cancellationToken);

        return Accepted(new
        {
            ArticleId = articleId,
            Region = request.Region,
            Message = "Article accepted for processing.",
            TraceId = Activity.Current?.TraceId.ToString()
        });
    }
}