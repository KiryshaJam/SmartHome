using System.ComponentModel.DataAnnotations;

namespace SmartHome.DTOs;

public class UpdateSortOrderDto
{
    [Required]
    public int SortOrder { get; set; }
}