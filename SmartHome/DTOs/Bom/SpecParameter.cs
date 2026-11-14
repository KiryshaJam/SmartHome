using System.Text.Json;

namespace SmartHome.DTOs.Bom;

public record SpecParameter(string Key, int DefinitionId, string Name, int Type, bool Required,
    decimal? Min, decimal? Max, int[] EnumIds, JsonElement? Default)
{
    public Dictionary<int, string> EnumLabels { get; init; } = [];
}
