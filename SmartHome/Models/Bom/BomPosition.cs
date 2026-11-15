using System.ComponentModel.DataAnnotations;

namespace SmartHome.Models.Bom;

public class BomPosition
{
    public int RevisionId { get; set; }
    [MaxLength(64)] public string PositionKey { get; set; } = "";
    public int? ComponentRevisionId { get; set; }
    public decimal Amount { get; set; } = 1;
    public decimal ForQuantity { get; set; } = 1;
    public decimal? ResolvedQuantity { get; set; }
    public int SortOrder { get; set; }
    [MaxLength(1024)] public string? Condition { get; set; }
    [MaxLength(1024)] public string? Formula { get; set; }
    public bool Excluded { get; set; }
}
