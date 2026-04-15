namespace SmartHome.DTOs;

public class TreeNodeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string ShortName { get; set; } = null!;
    public bool IsTerminal { get; set; }
    public int SortOrder { get; set; }
    public int? ParentId { get; set; }
    public List<TreeNodeDto> Children { get; set; } = new();
}