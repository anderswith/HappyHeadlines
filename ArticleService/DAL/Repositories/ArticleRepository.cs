using ArticleService.BE;
using ArticleService.DAL.Interfaces;
using ArticleService.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Monitoring;

namespace ArticleService.DAL.Repositories;

public class ArticleRepository : IArticleRepository
{
    private readonly IArticleContextFactory _contextFactory;

    public ArticleRepository(IArticleContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task AddAsync(
        ArticleRegion region,
        Article article)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity(
                "ArticleDatabase.Insert");

        await using var context =
            _contextFactory.CreateContext(region);

        await context.Articles.AddAsync(article);
        await context.SaveChangesAsync();
    }

    public async Task AddPublishedAsync(
        ArticleRegion region,
        Article article,
        CancellationToken cancellationToken)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity(
                "ArticleDatabase.InsertPublished");

        activity?.SetTag("article.region", region.ToString());

        await using var context =
            _contextFactory.CreateContext(region);

        // RabbitMQ kan levere samme besked mere end én gang.
        // Et eksisterende artikel-ID indsættes derfor ikke igen.
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO articles (id, title, content, created_utc)
             VALUES (
                 {article.Id},
                 {article.Title},
                 {article.Content},
                 {article.CreatedUtc}
             )
             ON CONFLICT (id) DO NOTHING
             """,
            cancellationToken);
    }

    public async Task<Article?> GetByIdAsync(
        ArticleRegion region,
        Guid id)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity(
                "ArticleDatabase.Read");

        await using var context =
            _contextFactory.CreateContext(region);

        return await context.Articles
            .AsNoTracking()
            .FirstOrDefaultAsync(article => article.Id == id);
    }

    public async Task<List<Article>> GetForDayAsync(
        ArticleRegion region,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity(
                "ArticleDatabase.GetForDay");

        var start = DateTime.SpecifyKind(
            date.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc);

        var end = start.AddDays(1);

        await using var context =
            _contextFactory.CreateContext(region);

        return await context.Articles
            .AsNoTracking()
            .Where(article =>
                article.CreatedUtc >= start &&
                article.CreatedUtc < end)
            .OrderBy(article => article.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(
        ArticleRegion region,
        Guid id,
        string title,
        string content)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity(
                "ArticleDatabase.Update");

        await using var context =
            _contextFactory.CreateContext(region);

        var article = await context.Articles
            .FirstOrDefaultAsync(article => article.Id == id);

        if (article is null)
        {
            return false;
        }

        article.Title = title;
        article.Content = content;

        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(
        ArticleRegion region,
        Guid id)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity(
                "ArticleDatabase.Delete");

        await using var context =
            _contextFactory.CreateContext(region);

        var article = await context.Articles
            .FirstOrDefaultAsync(article => article.Id == id);

        if (article is null)
        {
            return false;
        }

        context.Articles.Remove(article);

        await context.SaveChangesAsync();

        return true;
    }
    
    public async Task<List<Article>> GetRecentGlobalAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        using var activity =
            MonitorService.ActivitySource.StartActivity(
                "ArticleDatabase.ReadRecentGlobal");

        await using var context =
            _contextFactory.CreateContext(ArticleRegion.Global);

        return await context.Articles
            .AsNoTracking()
            .Where(article =>
                article.CreatedUtc >= fromUtc &&
                article.CreatedUtc <= toUtc)
            .OrderByDescending(article => article.CreatedUtc)
            .ToListAsync(cancellationToken);
    }
}