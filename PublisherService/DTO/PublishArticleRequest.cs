using System.ComponentModel.DataAnnotations;

namespace PublisherService.DTO;

public class PublishArticleRequest
{
    [Required]
    public string Region { get; set; } = "";

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = "";

    [Required]
    [StringLength(50000, MinimumLength = 1)]
    public string Content { get; set; } = "";
}