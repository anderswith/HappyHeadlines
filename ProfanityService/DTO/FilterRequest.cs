using System.ComponentModel.DataAnnotations;

namespace ProfanityService.DTO;

public class FilterRequest
{
    [Required]
    [StringLength(10000)]
    public string Text { get; set; } = "";
}