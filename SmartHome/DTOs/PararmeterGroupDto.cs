namespace SmartHome.DTOs;

public class ParameterGroupDto
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string ShortName { get; set; } = null!;

    public int SortOrder { get; set; }
}