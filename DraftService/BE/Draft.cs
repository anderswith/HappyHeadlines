namespace DraftService.BE;

public class Draft
{
    public Guid Id { get; set; }

    public string Author { get; set; } = "";

    public string Title { get; set; } = "";

    public string Content { get; set; } = "";

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }
}