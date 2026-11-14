using System.ComponentModel.DataAnnotations;

namespace SmartHome.Models.Bom;

public class BomOrderLine
{
    public int OrderId { get; set; }
    public int SortOrder { get; set; }
    public int ConfiguredRevisionId { get; set; }
    public decimal Quantity { get; set; }
}
