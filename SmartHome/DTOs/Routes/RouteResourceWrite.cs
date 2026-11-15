using System.ComponentModel.DataAnnotations;

namespace SmartHome.DTOs.Routes;

public class DepartmentWrite
{
    [Required, MaxLength(256)]
    public string Name { get; set; } = "";
    [Range(1, int.MaxValue)]
    public int TypeId { get; set; }
}

public class WorkCenterWrite : DepartmentWrite
{
    [Range(1, int.MaxValue)]
    public int DepartmentId { get; set; }
}
