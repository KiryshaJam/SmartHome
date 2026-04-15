using System.ComponentModel.DataAnnotations;

namespace SmartHome.Models;

public class Product
{
    public int Id { get; set; }

    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = null!;

    [Required]
    [MaxLength(128)]
    public string ShortName { get; set; } = null!;

    [Required]
    public int ClassNodeId { get; set; }

    public ClassNode ClassNode { get; set; } = null!;
}