namespace SmartHome.DTOs;

public class ProductFilterDto
{
    public int ClassNodeId { get; set; }

    public int? ClassParameterId { get; set; }

    public int? IntegerValue { get; set; }

    public decimal? NumberFrom { get; set; }

    public decimal? NumberTo { get; set; }

    public string? StringContains { get; set; }

    public DateTime? DateFrom { get; set; }

    public DateTime? DateTo { get; set; }

    public int? EnumValueId { get; set; }
}