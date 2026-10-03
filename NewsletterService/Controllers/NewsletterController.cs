using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Monitoring;
using NewsletterService.BLL.Interfaces;
using NewsletterService.DTO;

namespace NewsletterService.Controllers;

[ApiController]
[Route("api/newsletters")]
public class NewsletterController : ControllerBase
{
    private readonly INewsletterLogic _logic;

    public NewsletterController(INewsletterLogic logic)
    {
        _logic = logic;
    }

    [HttpPost("subscribers")]
    public async Task<IActionResult> Subscribe(
        SubscribeRequest request,
        CancellationToken cancellationToken)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity(
                "NewsletterController.Subscribe");

        var subscriber = await _logic.SubscribeAsync(
            request.Email,
            cancellationToken);

        return Ok(subscriber);
    }

    [HttpGet("subscribers")]
    public async Task<IActionResult> GetSubscribers(
        CancellationToken cancellationToken)
    {
        return Ok(await _logic.GetSubscribersAsync(
            cancellationToken));
    }

    [HttpPost("daily/{region}")]
    public async Task<IActionResult> SendDaily(
        string region,
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity(
                "NewsletterController.SendDaily");

        var recipients = await _logic.SendDailyAsync(
            region,
            date,
            cancellationToken);

        return Ok(new
        {
            Recipients = recipients,
            TraceId = Activity.Current?.TraceId.ToString()
        });
    }
}