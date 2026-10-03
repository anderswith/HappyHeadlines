using CommentService.BE;
using CommentService.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CommentService.DAL.Repositories;

public class CommentRepository : ICommentRepository
{
    private readonly CommentContext _context;

    public CommentRepository(CommentContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        Comment comment,
        CancellationToken cancellationToken)
    {
        await _context.Comments.AddAsync(
            comment,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<List<Comment>> GetByArticleAsync(
        string region,
        Guid articleId,
        CancellationToken cancellationToken)
    {
        return _context.Comments
            .AsNoTracking()
            .Where(c =>
                c.ArticleRegion == region &&
                c.ArticleId == articleId)
            .OrderBy(c => c.CreatedUtc)
            .ToListAsync(cancellationToken);
    }
}