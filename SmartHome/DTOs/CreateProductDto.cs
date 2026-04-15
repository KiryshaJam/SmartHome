using System.ComponentModel.DataAnnotations;

namespace SmartHome.DTOs;

public class CreateProductDto
{
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = null!;

    [Required]
    [MaxLength(128)]
    public string ShortName { get; set; } = null!;

    [Required]
    public int ClassNodeId { get; set; }
}