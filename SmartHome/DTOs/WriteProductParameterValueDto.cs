using System.ComponentModel.DataAnnotations;

namespace SmartHome.DTOs;

public class WriteProductParameterValueDto
{
    public int? IntegerValue { get; set; }

    public decimal? NumberValue { get; set; }

    [MaxLength(1024)]
    public string? StringValue { get; set; }

    public DateTime? DateTimeValue { get; set; }

    public int? EnumValueId { get; set; }
}