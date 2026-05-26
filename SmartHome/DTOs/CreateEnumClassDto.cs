using System.ComponentModel.DataAnnotations;
using SmartHome.Models;

namespace SmartHome.DTOs;

public class CreateEnumClassDto
{
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = null!;

    [Required]
    [MaxLength(128)]
    public string ShortName { get; set; } = null!;

    [Required]
    public EnumValueType ValueType { get; set; }

    public int SortOrder { get; set; }

    public int? MeasureUnitId { get; set; }
}