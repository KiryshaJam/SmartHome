using System.ComponentModel.DataAnnotations;

namespace SmartHome.Models;

public class EnumClass
{
    public int Id { get; set; }

    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = null!;

    [Required]
    [MaxLength(128)]
    public string ShortName { get; set; } = null!;

    public EnumValueType ValueType { get; set; }

    public int SortOrder { get; set; }

    public int? MeasureUnitId { get; set; }
    public MeasureUnit? MeasureUnit { get; set; }

    public ICollection<EnumValue> Values { get; set; } = new List<EnumValue>();
}