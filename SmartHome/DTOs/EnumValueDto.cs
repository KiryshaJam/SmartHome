namespace SmartHome.DTOs;

public class EnumValueDto
{
    public int Id { get; set; }

    public int EnumClassId { get; set; }

    public string? StringValue { get; set; }

    public decimal? NumberValue { get; set; }

    public string? IconValue { get; set; }

    public string? DisplayName { get; set; }

    public int SortOrder { get; set; }
}