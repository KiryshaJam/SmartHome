using System.Text.Json;

namespace SmartHome.DTOs.Bom;

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
