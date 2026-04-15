namespace SmartHome.DTOs;

public class ClassNodeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string ShortName { get; set; } = null!;
    public bool IsTerminal { get; set; }
    public int SortOrder { get; set; }
    public int? ParentId { get; set; }
    public int? MeasureUnitId { get; set; }
    public string? MeasureUnitName { get; set; }
    public string? MeasureUnitShortName { get; set; }
}