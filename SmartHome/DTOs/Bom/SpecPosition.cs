using System.Text.Json;

namespace SmartHome.DTOs.Bom;

public class SpecPosition
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int ComponentId { get; set; }
    public int RevisionId { get; set; }
    public decimal Amount { get; set; } = 1;
    public decimal ForQuantity { get; set; } = 1;
    public int UnitId { get; set; }
    public int SortOrder { get; set; }
    public string? Condition { get; set; }
    public string? Formula { get; set; }
    public int? ChildVariantId { get; set; }
    public Dictionary<string, string> Bindings { get; set; } = [];
    public bool Excluded { get; set; }
    public bool Inherited { get; set; }
}
