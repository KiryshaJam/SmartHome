using System.ComponentModel.DataAnnotations;
using SmartHome.Models;

namespace SmartHome.DTOs;

public class CreateParameterDefinitionDto
{
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = null!;

    [Required]
    [MaxLength(128)]
    public string ShortName { get; set; } = null!;

    [Required]
    public ParameterValueType ValueType { get; set; }

    public int? MeasureUnitId { get; set; }

    public int? EnumClassId { get; set; }
}