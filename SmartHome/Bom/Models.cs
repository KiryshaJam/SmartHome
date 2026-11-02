using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace SmartHome.Bom;

public class BomRevision
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int UnitId { get; set; }
    public int? PreviousId { get; set; }
    public int? BaseRevisionId { get; set; }
    [MaxLength(512)] public string Reason { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column(TypeName = "jsonb")] public string Document { get; set; } = "{}";
}
public class BomVariant
{
    public int Id { get; set; }
    public int RevisionId { get; set; }
    [MaxLength(256)] public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column(TypeName = "jsonb")] public string Snapshot { get; set; } = "{}";
}
public class BomOrder
{
    public int Id { get; set; }
    [MaxLength(128)] public string Number { get; set; } = "";
    public DateOnly Date { get; set; }
}
public class BomOrderRevision
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int? PreviousId { get; set; }
    [MaxLength(512)] public string Reason { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column(TypeName = "jsonb")] public string Lines { get; set; } = "[]";
}
public record SpecParameter(string Key, int DefinitionId, string Name, int Type, bool Required,
    decimal? Min, decimal? Max, int[] EnumIds, JsonElement? Default)
{
    public Dictionary<int, string> EnumLabels { get; init; } = [];
}
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
public record SpecRule(string Expression, string Message);
public class SpecDocument
{
    public List<SpecPosition> Positions { get; set; } = [];
    public List<SpecParameter> Parameters { get; set; } = [];
    public List<SpecRule> Rules { get; set; } = [];
    public Dictionary<string, string> Calculations { get; set; } = [];
    public string ProductName { get; set; } = "";
    public string UnitName { get; set; } = "";
    public int[] Classes { get; set; } = [];
}
public class SpecWrite
{
    public int? ExpectedRevisionId { get; set; }
    public int UnitId { get; set; }
    public string Reason { get; set; } = "";
    // Only differences for modifications. An excluded position retains its stable Id.
    public List<SpecPosition> Positions { get; set; } = [];
    public List<SpecRule> Rules { get; set; } = [];
    public Dictionary<string, string> Calculations { get; set; } = [];
}
public record VariantWrite(int RevisionId, string Name, Dictionary<string, JsonElement> Values);
public record ModificationWrite(int BaseRevisionId, string Name, string ShortName, string Reason);
public record OrderLine(int VariantId, decimal Quantity);
public record OrderWrite(string Number, DateOnly Date, int? ExpectedRevisionId, string Reason, List<OrderLine> Lines);
public class ResolvedNode
{
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
public static class BomJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    public static string Encode<T>(T value) => JsonSerializer.Serialize(value, Options);
    public static T Decode<T>(string value) => JsonSerializer.Deserialize<T>(value, Options)!;
}
