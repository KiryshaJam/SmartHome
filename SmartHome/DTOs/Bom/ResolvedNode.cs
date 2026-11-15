using System.Text.Json;

namespace SmartHome.DTOs.Bom;

public class ResolvedNode
{
    public int? ConfiguredRevisionId { get; set; }
    public int ProductId { get; set; }
    public int RevisionId { get; set; }
    public string Name { get; set; } = "";
    public int UnitId { get; set; }
    public string Unit { get; set; } = "";
    public int[] Classes { get; set; } = [];
    public string PositionId { get; set; } = "";
    public decimal Quantity { get; set; } = 1;
    public decimal Amount { get; set; } = 1;
    public decimal ForQuantity { get; set; } = 1;
    public Dictionary<string, JsonElement> Values { get; set; } = [];
    public SpecDocument Source { get; set; } = new();
    public List<ResolvedNode> Children { get; set; } = [];
}
