using ArticleService.BE;
using ArticleService.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ArticleService.DAL;

public class ArticleContextFactory : IArticleContextFactory
{
    private readonly IConfiguration _configuration;

    public ArticleContextFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public ArticleServiceContext CreateContext(ArticleRegion region)
    {
        var connectionString =
            _configuration.GetConnectionString(region.ToString());

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Missing connection string for {region}."
            );
        }

        var options =
            new DbContextOptionsBuilder<ArticleServiceContext>()
                .UseNpgsql(connectionString)
                .Options;

        return new ArticleServiceContext(options);
    }
}