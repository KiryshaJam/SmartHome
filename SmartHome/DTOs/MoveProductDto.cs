using System.ComponentModel.DataAnnotations;

namespace SmartHome.DTOs;

public class MoveProductDto
{
    [Required]
    public int NewClassNodeId { get; set; }
}