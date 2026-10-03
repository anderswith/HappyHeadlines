using System.ComponentModel.DataAnnotations;

namespace DraftService.DTO;

public class DraftRequest
{
    [Required]
    [StringLength(100)]
    public string Author { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    [StringLength(200)]
    public string Title { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    [StringLength(50000)]
    public string Content { get; set; } = "";
}