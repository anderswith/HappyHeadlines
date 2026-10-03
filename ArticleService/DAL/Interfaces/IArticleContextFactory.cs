using ArticleService.BE;

namespace ArticleService.DAL.Interfaces;

public interface IArticleContextFactory
{
    ArticleServiceContext CreateContext(ArticleRegion region);
}