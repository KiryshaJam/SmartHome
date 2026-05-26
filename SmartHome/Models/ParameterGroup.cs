using System.ComponentModel.DataAnnotations;

namespace SmartHome.Models;

public class ParameterGroup
{
    public int Id { get; set; }

    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = null!;

    [Required]
    [MaxLength(128)]
    public string ShortName { get; set; } = null!;

    public int SortOrder { get; set; }

    public ICollection<ClassParameter> ClassParameters { get; set; } = new List<ClassParameter>();
}