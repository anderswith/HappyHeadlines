using Microsoft.AspNetCore.Mvc;
using ProfanityService.BLL.Interfaces;
using ProfanityService.DTO;

namespace ProfanityService.Controllers;

[ApiController]
[Route("api/profanity")]
public class ProfanityController : ControllerBase
{
    private readonly IProfanityLogic _logic;

    public ProfanityController(IProfanityLogic logic)
    {
        _logic = logic;
    }

    [HttpPost("filter")]
    public async Task<IActionResult> Filter(
        FilterRequest request,
        CancellationToken cancellationToken)
    {
        var filteredText = await _logic.FilterAsync(
            request.Text,
            cancellationToken
        );

        return Ok(new FilterResponse
        {
            FilteredText = filteredText
        });
    }
}