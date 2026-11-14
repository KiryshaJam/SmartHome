using System.ComponentModel.DataAnnotations;

namespace SmartHome.Models.Bom;

public class BomOrder
{
    public int Id { get; set; }
    [MaxLength(128)] public string Number { get; set; } = "";
    public DateOnly Date { get; set; }
    public int UpdateToken { get; set; } = 1;
    public List<BomOrderLine> Lines { get; set; } = [];
}
