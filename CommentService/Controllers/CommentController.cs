using CommentService.BLL.Interfaces;
using CommentService.DTO;
using Microsoft.AspNetCore.Mvc;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Polly.Timeout;

namespace CommentService.Controllers;

[ApiController]
[Route("api/comments")]
public class CommentController : ControllerBase
{
    private readonly ICommentLogic _logic;

    public CommentController(ICommentLogic logic)
    {
        _logic = logic;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CommentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var comment = await _logic.CreateAsync(
                request.ArticleId,
                request.ArticleRegion,
                request.Author,
                request.Text,
                cancellationToken
            );

            return StatusCode(StatusCodes.Status201Created, comment);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (BrokenCircuitException)
        {
            return Unavailable(
                "Circuit breaker is open. Try again shortly.");
        }
        catch (TimeoutRejectedException)
        {
            return Unavailable("ProfanityService took too long.");
        }
        catch (RateLimiterRejectedException)
        {
            return Unavailable("The profanity filter is busy.");
        }
        catch (HttpRequestException)
        {
            return Unavailable("ProfanityService is unavailable.");
        }
    }

    [HttpGet("article/{region}/{articleId:guid}")]
    public async Task<IActionResult> GetByArticle(
        string region,
        Guid articleId,
        CancellationToken cancellationToken)
    {
        try
        {
            var comments = await _logic.GetByArticleAsync(
                region,
                articleId,
                cancellationToken
            );

            return Ok(comments);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    private ObjectResult Unavailable(string message)
    {
        return StatusCode(
            StatusCodes.Status503ServiceUnavailable,
            new
            {
                Message = message,
                CommentSaved = false
            });
    }
}