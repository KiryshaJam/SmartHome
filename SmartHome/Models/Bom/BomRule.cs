using System.ComponentModel.DataAnnotations;

namespace SmartHome.Models.Bom;

public class BomRule
{
    public int RevisionId { get; set; }
    public int SortOrder { get; set; }
    [MaxLength(1024)] public string Expression { get; set; } = "";
    [MaxLength(512)] public string Message { get; set; } = "";
}
