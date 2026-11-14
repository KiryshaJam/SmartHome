using System.ComponentModel.DataAnnotations;

namespace SmartHome.Models.Bom;

public class BomRevision
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int UnitId { get; set; }
    public bool IsConfigured { get; set; }
    public bool IsFinalized { get; set; }
    public decimal RootAmount { get; set; } = 1;
    public decimal RootForQuantity { get; set; } = 1;
    public decimal RootQuantity { get; set; } = 1;
    public int? PreviousId { get; set; }
    public int? BaseRevisionId { get; set; }
    public int? SourceRevisionId { get; set; }
    [MaxLength(256)] public string? Name { get; set; }
    [MaxLength(512)] public string Reason { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
