namespace PublisherService.BLL.Interfaces;

public interface IPublisherLogic
{
    Task<Guid> PublishAsync(
        string region,
        string title,
        string content,
        CancellationToken cancellationToken);
}