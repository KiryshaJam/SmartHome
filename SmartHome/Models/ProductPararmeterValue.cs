using System.ComponentModel.DataAnnotations;

namespace SmartHome.Models;

public class ProductParameterValue
{
    public int Id { get; set; }

    [Required]
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Required]
    public int ClassParameterId { get; set; }
    public ClassParameter ClassParameter { get; set; } = null!;

    public int? IntegerValue { get; set; }

    public decimal? NumberValue { get; set; }

    [MaxLength(1024)]
    public string? StringValue { get; set; }

    public DateTime? DateTimeValue { get; set; }

    public int? EnumValueId { get; set; }
    public EnumValue? EnumValue { get; set; }
}