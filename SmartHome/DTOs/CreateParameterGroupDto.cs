using System.ComponentModel.DataAnnotations;

namespace SmartHome.DTOs;

public class CreateParameterGroupDto
{
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = null!;

    [Required]
    [MaxLength(128)]
    public string ShortName { get; set; } = null!;

    public int SortOrder { get; set; }
}