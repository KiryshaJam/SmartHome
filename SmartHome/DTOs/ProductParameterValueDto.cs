using SmartHome.Models;

namespace SmartHome.DTOs;

public class ProductParameterValueDto
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public int ClassParameterId { get; set; }

    public int ParameterDefinitionId { get; set; }

    public string ParameterName { get; set; } = null!;

    public string ParameterShortName { get; set; } = null!;

    public ParameterValueType ValueType { get; set; }

    public int? ParameterGroupId { get; set; }

    public string? ParameterGroupName { get; set; }

    public int SortOrder { get; set; }

    public bool IsRequired { get; set; }

    public decimal? MinNumberValue { get; set; }

    public decimal? MaxNumberValue { get; set; }

    public int? MeasureUnitId { get; set; }

    public string? MeasureUnitName { get; set; }

    public string? MeasureUnitShortName { get; set; }

    public int? EnumClassId { get; set; }

    public string? EnumClassName { get; set; }

    public int? IntegerValue { get; set; }

    public decimal? NumberValue { get; set; }

    public string? StringValue { get; set; }

    public DateTime? DateTimeValue { get; set; }

    public int? EnumValueId { get; set; }

    public string? EnumDisplayName { get; set; }

    public string? EnumStringValue { get; set; }

    public decimal? EnumNumberValue { get; set; }

    public string? EnumIconValue { get; set; }
}