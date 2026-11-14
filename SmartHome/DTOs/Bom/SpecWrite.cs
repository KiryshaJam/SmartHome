using System.Text.Json;

namespace SmartHome.DTOs.Bom;

public class SpecWrite
{
    public int? ExpectedRevisionId { get; set; }
    public int UnitId { get; set; }
    public string Reason { get; set; } = "";
    // Only differences for modifications. An excluded position retains its stable Id.
    public List<SpecParameter>? Parameters { get; set; }
    public List<SpecPosition> Positions { get; set; } = [];
    public List<SpecRule> Rules { get; set; } = [];
    public Dictionary<string, string> Calculations { get; set; } = [];
}
