using System.ComponentModel.DataAnnotations;

namespace SmartHome.DTOs;

public class CreateMeasureUnitDto
{
    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = null!;

    [Required]
    [MaxLength(64)]
    public string ShortName { get; set; } = null!;
}
