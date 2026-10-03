using System.ComponentModel.DataAnnotations;

namespace CommentService.DTO;

public class CommentRequest
{
    public Guid ArticleId { get; set; }

    [Required]
    [RegularExpression(
        "^(Europe|Asia|Africa|NorthAmerica|SouthAmerica|Oceania|Antarctica|Global)$")]
    public string ArticleRegion { get; set; } = "";

    [Required]
    [StringLength(100)]
    public string Author { get; set; } = "";

    [Required]
    [StringLength(10000)]
    public string Text { get; set; } = "";
}