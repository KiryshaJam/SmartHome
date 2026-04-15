namespace SmartHome.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string ShortName { get; set; } = null!;
    public int ClassNodeId { get; set; }
    public string ClassNodeName { get; set; } = null!;
}