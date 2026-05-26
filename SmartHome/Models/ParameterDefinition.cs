using System.ComponentModel.DataAnnotations;

namespace SmartHome.Models;

public class ParameterDefinition
{
    public int Id { get; set; }

    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = null!;

    [Required]
    [MaxLength(128)]
    public string ShortName { get; set; } = null!;

    public ParameterValueType ValueType { get; set; }

    public int? MeasureUnitId { get; set; }
    public MeasureUnit? MeasureUnit { get; set; }

    public int? EnumClassId { get; set; }
    public EnumClass? EnumClass { get; set; }

    public ICollection<ClassParameter> ClassParameters { get; set; } = new List<ClassParameter>();
}