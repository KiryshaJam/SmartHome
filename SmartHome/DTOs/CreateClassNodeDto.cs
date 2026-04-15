using System.ComponentModel.DataAnnotations;

namespace SmartHome.DTOs;

public class CreateClassNodeDto
{
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = null!;

    [Required]
    [MaxLength(128)]
    public string ShortName { get; set; } = null!;

    public bool IsTerminal { get; set; }

    public int SortOrder { get; set; }

    public int? ParentId { get; set; }

    public int? MeasureUnitId { get; set; }
}