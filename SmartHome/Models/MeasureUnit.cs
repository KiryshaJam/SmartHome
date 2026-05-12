using System.ComponentModel.DataAnnotations;

namespace SmartHome.Models;

public class MeasureUnit
{
    public int Id { get; set; }

    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = null!;

    [Required]
    [MaxLength(64)]
    public string ShortName { get; set; } = null!;

    public ICollection<ClassNode> ClassNodes { get; set; } = new List<ClassNode>();
    
    public ICollection<EnumClass> EnumClasses { get; set; } = new List<EnumClass>();
    public ICollection<ParameterDefinition> ParameterDefinitions { get; set; } = new List<ParameterDefinition>();
}