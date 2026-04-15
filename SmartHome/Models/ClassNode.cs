using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartHome.Models;

public class ClassNode
{
    public int Id { get; set; }

    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = null!;

    [Required]
    [MaxLength(128)]
    public string ShortName { get; set; } = null!;

    public bool IsTerminal { get; set; }

    public int SortOrder { get; set; }

    public int? ParentId { get; set; }
    public ClassNode? Parent { get; set; }

    public ICollection<ClassNode> Children { get; set; } = new List<ClassNode>();

    public int? MeasureUnitId { get; set; }
    public MeasureUnit? MeasureUnit { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}