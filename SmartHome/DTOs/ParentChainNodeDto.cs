namespace SmartHome.DTOs;

public class ParentChainNodeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string ShortName { get; set; } = null!;
    public int Level { get; set; }
}