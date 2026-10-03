using CommentService.BE;

namespace CommentService.DAL.Repositories.Interfaces;

public interface ICommentRepository
{
    Task AddAsync(
        Comment comment,
        CancellationToken cancellationToken);

    Task<List<Comment>> GetByArticleAsync(
        string region,
        Guid articleId,
        CancellationToken cancellationToken);
}