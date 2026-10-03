namespace CommentService.Clients.Interfaces;

public interface IProfanityClient
{
    Task<string> FilterAsync(
        string text,
        CancellationToken cancellationToken);
}