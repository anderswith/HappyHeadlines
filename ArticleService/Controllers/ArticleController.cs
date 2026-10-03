using ArticleService.BE;
using ArticleService.BLL.Interfaces;
using ArticleService.DTO;
using Microsoft.AspNetCore.Mvc;

namespace ArticleService.Controllers;

[ApiController]
[Route("api/{region}/articles")]
public class ArticleController : ControllerBase
{
    private readonly IArticleLogic _articleLogic;

    public ArticleController(IArticleLogic articleLogic)
    {
        _articleLogic = articleLogic;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        string region,
        [FromBody] ArticleRequest request)
    {
        if (!TryParseRegion(region, out var articleRegion))
        {
            return BadRequest("Unknown region.");
        }

        try
        {
            var article = await _articleLogic.CreateAsync(
                articleRegion,
                request.Title,
                request.Content
            );

            return CreatedAtAction(
                nameof(Read),
                new { region, id = article.Id },
                article
            );
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
    
    [HttpGet]
    public async Task<IActionResult> ReadForDay(
        string region,
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken)
    {
        if (!TryParseRegion(region, out var articleRegion))
        {
            return BadRequest("Unknown region.");
        }

        if (date == default)
        {
            return BadRequest("A date is required.");
        }

        var articles = await _articleLogic.GetForDayAsync(
            articleRegion,
            date,
            cancellationToken);

        return Ok(articles);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Read(
        string region,
        Guid id)
    {
        if (!TryParseRegion(region, out var articleRegion))
        {
            return BadRequest("Unknown region.");
        }

        var article =
            await _articleLogic.GetByIdAsync(articleRegion, id);

        if (article is null)
        {
            return NotFound();
        }

        return Ok(article);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        string region,
        Guid id,
        [FromBody] ArticleRequest request)
    {
        if (!TryParseRegion(region, out var articleRegion))
        {
            return BadRequest("Unknown region.");
        }

        try
        {
            var updated = await _articleLogic.UpdateAsync(
                articleRegion,
                id,
                request.Title,
                request.Content
            );

            if (!updated)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        string region,
        Guid id)
    {
        if (!TryParseRegion(region, out var articleRegion))
        {
            return BadRequest("Unknown region.");
        }

        var deleted =
            await _articleLogic.DeleteAsync(articleRegion, id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    private static bool TryParseRegion(
        string value,
        out ArticleRegion region)
    {
        // Accepterer kun regionernes navne, ikke numeriske enum-værdier.
        var validName = Enum.GetNames<ArticleRegion>()
            .Any(name => string.Equals(
                name,
                value,
                StringComparison.OrdinalIgnoreCase
            ));

        if (!validName)
        {
            region = default;
            return false;
        }

        return Enum.TryParse(value, true, out region);
    }
}