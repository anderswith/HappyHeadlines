using System.ComponentModel.DataAnnotations;

namespace ArticleService.DTO;

public class ArticleRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = "";

    [Required]
    public string Content { get; set; } = "";
}