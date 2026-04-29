using System.ComponentModel.DataAnnotations;

namespace SmartHome.DTOs;

public class CreateEnumValueDto
{
    [MaxLength(512)]
    public string? StringValue { get; set; }

    public decimal? NumberValue { get; set; }

    [MaxLength(512)]
    public string? IconValue { get; set; }

    [MaxLength(512)]
    public string? DisplayName { get; set; }

    public int SortOrder { get; set; }
}