using System.ComponentModel.DataAnnotations;

namespace SmartHome.Models;

public class ClassParameter
{
    public int Id { get; set; }

    [Required]
    public int ClassNodeId { get; set; }
    public ClassNode ClassNode { get; set; } = null!;

    [Required]
    public int ParameterDefinitionId { get; set; }
    public ParameterDefinition ParameterDefinition { get; set; } = null!;

    public int? ParameterGroupId { get; set; }
    public ParameterGroup? ParameterGroup { get; set; }

    public int SortOrder { get; set; }

    public bool IsRequired { get; set; }

    public bool IsInherited { get; set; }

    public decimal? MinNumberValue { get; set; }

    public decimal? MaxNumberValue { get; set; }

    public ICollection<ProductParameterValue> ProductValues { get; set; } = new List<ProductParameterValue>();
}