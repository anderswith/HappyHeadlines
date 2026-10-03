namespace Contracts.Messages;

public class ArticlePublished
{
    public Guid Id { get; set; }

    public string Region { get; set; } = "";

    public string Title { get; set; } = "";

    public string Content { get; set; } = "";

    public DateTime CreatedUtc { get; set; }

    // Indeholder trace-context fra den service, der sender beskeden.
    public Dictionary<string, string> Header { get; set; } = new();
}