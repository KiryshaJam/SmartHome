using System.ComponentModel.DataAnnotations;

namespace SmartHome.Models;

public class EnumValue
{
    public int Id { get; set; }

    [Required]
    public int EnumClassId { get; set; }

    public EnumClass EnumClass { get; set; } = null!;

    [MaxLength(512)]
    public string? StringValue { get; set; }

    public decimal? NumberValue { get; set; }

    [MaxLength(512)]
    public string? IconValue { get; set; }

    [MaxLength(512)]
    public string? DisplayName { get; set; }

    public int SortOrder { get; set; }
    public ICollection<ProductParameterValue> ProductParameterValues { get; set; } = new List<ProductParameterValue>();
}