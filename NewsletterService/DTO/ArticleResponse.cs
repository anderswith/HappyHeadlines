namespace NewsletterService.DTO;

public class ArticleResponse
{
    public Guid Id { get; set; }

    public string Title { get; set; } = "";

    public string Content { get; set; } = "";

    public DateTime CreatedUtc { get; set; }
}