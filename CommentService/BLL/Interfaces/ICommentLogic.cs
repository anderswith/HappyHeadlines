using CommentService.BE;

namespace CommentService.BLL.Interfaces;

public interface ICommentLogic
{
    Task<Comment> CreateAsync(
        Guid articleId,
        string articleRegion,
        string author,
        string text,
        CancellationToken cancellationToken);

    Task<List<Comment>> GetByArticleAsync(
        string region,
        Guid articleId,
        CancellationToken cancellationToken);
}