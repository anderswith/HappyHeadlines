namespace NewsletterService.BE;

public class Subscriber
{
    public Guid Id { get; set; }

    public string Email { get; set; } = "";

    public DateTime CreatedUtc { get; set; }
}