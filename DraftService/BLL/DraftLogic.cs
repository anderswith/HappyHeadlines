using DraftService.BE;
using DraftService.BLL.Interfaces;
using DraftService.DAL.Repositories.Interfaces;
using Monitoring;

namespace DraftService.BLL;

public class DraftLogic : IDraftLogic
{
    private readonly IDraftRepository _repository;

    public DraftLogic(IDraftRepository repository)
    {
        _repository = repository;
    }

    public async Task<Draft> CreateAsync(
        string author,
        string title,
        string content,
        CancellationToken cancellationToken)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity("DraftLogic.Create");

        Validate(author, title, content);

        var now = DateTime.UtcNow;

        var draft = new Draft
        {
            Id = Guid.NewGuid(),
            Author = author.Trim(),
            Title = title.Trim(),
            Content = content,
            CreatedUtc = now,
            UpdatedUtc = now
        };

        activity?.SetTag("draft.id", draft.Id.ToString());

        await _repository.AddAsync(draft, cancellationToken);

        MonitorService.Log.Information(
            "Draft {DraftId} created",
            draft.Id);

        return draft;
    }

    public Task<Draft?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return _repository.GetByIdAsync(id, cancellationToken);
    }

    public Task<List<Draft>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return _repository.GetAllAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(
        Guid id,
        string author,
        string title,
        string content,
        CancellationToken cancellationToken)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity("DraftLogic.Update");

        activity?.SetTag("draft.id", id.ToString());

        Validate(author, title, content);

        var draft = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (draft is null)
        {
            return false;
        }

        draft.Author = author.Trim();
        draft.Title = title.Trim();
        draft.Content = content;
        draft.UpdatedUtc = DateTime.UtcNow;

        await _repository.SaveChangesAsync(cancellationToken);

        MonitorService.Log.Information(
            "Draft {DraftId} updated",
            id);

        return true;
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity("DraftLogic.Delete");

        activity?.SetTag("draft.id", id.ToString());

        var draft = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (draft is null)
        {
            return false;
        }

        await _repository.DeleteAsync(draft, cancellationToken);

        MonitorService.Log.Information(
            "Draft {DraftId} deleted",
            id);

        return true;
    }

    private static void Validate(
        string author,
        string title,
        string content)
    {
        if (string.IsNullOrWhiteSpace(author) || author.Length > 100)
        {
            throw new ArgumentException(
                "Author must contain between 1 and 100 characters.");
        }

        if (title is null || title.Length > 200)
        {
            throw new ArgumentException(
                "Title cannot be null or exceed 200 characters.");
        }

        if (content is null || content.Length > 50000)
        {
            throw new ArgumentException(
                "Content cannot be null or exceed 50000 characters.");
        }
    }
}