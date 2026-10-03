namespace CommentService.BE;

public class Comment
{
    public Guid Id { get; set; }

    public Guid ArticleId { get; set; }

    public string ArticleRegion { get; set; } = "";

    public string Author { get; set; } = "";

    public string Text { get; set; } = "";

    public DateTime CreatedUtc { get; set; }
}