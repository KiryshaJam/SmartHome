using System.ComponentModel.DataAnnotations;

namespace SmartHome.Models.Bom;

public class BomParameter
{
    public int RevisionId { get; set; }
    public int DefinitionId { get; set; }
    public int SortOrder { get; set; }
    public int Type { get; set; }
    public bool Required { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    [MaxLength(1024)] public string? Formula { get; set; }
    public int? IntegerValue { get; set; }
    public decimal? NumberValue { get; set; }
    [MaxLength(1024)] public string? StringValue { get; set; }
    public long? DateTimeTicks { get; set; }
    public int? EnumValueId { get; set; }
}
