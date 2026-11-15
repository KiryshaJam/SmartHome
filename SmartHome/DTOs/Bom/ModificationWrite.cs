using System.Text.Json;

namespace SmartHome.DTOs.Bom;

public record ModificationWrite(int BaseRevisionId, string Name, string ShortName, string Reason);
