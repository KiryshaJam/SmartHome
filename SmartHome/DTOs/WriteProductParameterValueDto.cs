namespace SmartHome.DTOs;

public class WriteProductParameterValueDto
{
    public int? IntegerValue { get; set; }

    public decimal? NumberValue { get; set; }

    public string? StringValue { get; set; }

    public DateTime? DateTimeValue { get; set; }

    public int? EnumValueId { get; set; }
}