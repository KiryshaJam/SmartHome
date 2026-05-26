using SmartHome.Models;

namespace SmartHome.DTOs;

public class ParameterDefinitionDto
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string ShortName { get; set; } = null!;

    public ParameterValueType ValueType { get; set; }

    public int? MeasureUnitId { get; set; }

    public string? MeasureUnitName { get; set; }

    public string? MeasureUnitShortName { get; set; }

    public int? EnumClassId { get; set; }

    public string? EnumClassName { get; set; }
}