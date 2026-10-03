using System.ComponentModel.DataAnnotations;

namespace NewsletterService.DTO;

public class SubscribeRequest
{
    [Required]
    [EmailAddress]
    [StringLength(320)]
    public string Email { get; set; } = "";
}