using SmartHome.Models;

namespace SmartHome.DTOs;

public class ClassParameterDto
{
    public int Id { get; set; }

    public int ClassNodeId { get; set; }

    public string ClassNodeName { get; set; } = null!;

    public int ParameterDefinitionId { get; set; }

    public string ParameterName { get; set; } = null!;

    public string ParameterShortName { get; set; } = null!;

    public ParameterValueType ValueType { get; set; }

    public int? MeasureUnitId { get; set; }

    public string? MeasureUnitName { get; set; }

    public string? MeasureUnitShortName { get; set; }

    public int? EnumClassId { get; set; }

    public string? EnumClassName { get; set; }

    public int? ParameterGroupId { get; set; }

    public string? ParameterGroupName { get; set; }

    public int SortOrder { get; set; }

    public bool IsRequired { get; set; }

    public bool IsInherited { get; set; }

    public decimal? MinNumberValue { get; set; }

    public decimal? MaxNumberValue { get; set; }
}