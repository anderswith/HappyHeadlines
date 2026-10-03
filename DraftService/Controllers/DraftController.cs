using DraftService.BLL.Interfaces;
using DraftService.DTO;
using Microsoft.AspNetCore.Mvc;
using Monitoring;

namespace DraftService.Controllers;

[ApiController]
[Route("api/drafts")]
public class DraftController : ControllerBase
{
    private readonly IDraftLogic _logic;

    public DraftController(IDraftLogic logic)
    {
        _logic = logic;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        DraftRequest request,
        CancellationToken cancellationToken)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity("DraftController.Create");

        var draft = await _logic.CreateAsync(
            request.Author,
            request.Title,
            request.Content,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = draft.Id },
            draft);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity("DraftController.List");

        var drafts = await _logic.GetAllAsync(cancellationToken);

        return Ok(drafts);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity("DraftController.Read");

        activity?.SetTag("draft.id", id.ToString());

        var draft = await _logic.GetByIdAsync(id, cancellationToken);

        if (draft is null)
        {
            return NotFound();
        }

        return Ok(draft);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        DraftRequest request,
        CancellationToken cancellationToken)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity("DraftController.Update");

        var updated = await _logic.UpdateAsync(
            id,
            request.Author,
            request.Title,
            request.Content,
            cancellationToken);

        if (!updated)
        {
            MonitorService.Log.Warning(
                "Update rejected: draft {DraftId} does not exist",
                id);

            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity("DraftController.Delete");

        var deleted = await _logic.DeleteAsync(id, cancellationToken);

        if (!deleted)
        {
            MonitorService.Log.Warning(
                "Delete rejected: draft {DraftId} does not exist",
                id);

            return NotFound();
        }

        return NoContent();
    }
}