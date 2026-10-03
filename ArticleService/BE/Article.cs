namespace ArticleService.BE;

public class Article
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public DateTime CreatedUtc  { get; set; }
    
}