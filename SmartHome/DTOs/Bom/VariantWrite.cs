using System.Text.Json;

namespace SmartHome.DTOs.Bom;

public record VariantWrite(int RevisionId, string Name, Dictionary<string, JsonElement> Values);
