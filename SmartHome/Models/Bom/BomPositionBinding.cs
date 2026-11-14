using System.ComponentModel.DataAnnotations;

namespace SmartHome.Models.Bom;

public class BomPositionBinding
{
    public int RevisionId { get; set; }
    [MaxLength(64)] public string PositionKey { get; set; } = "";
    public int DefinitionId { get; set; }
    [MaxLength(1024)] public string Expression { get; set; } = "";
}
