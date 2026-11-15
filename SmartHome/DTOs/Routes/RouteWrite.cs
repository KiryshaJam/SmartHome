using SmartHome.Models.Routes;
namespace SmartHome.DTOs.Routes;

public class RouteWrite
{
    public string Name { get; set; } = "";
    public int ProductId { get; set; }
    public int? RevisionId { get; set; }
    public string Kind { get; set; } = "standalone";
    public int? ParameterId { get; set; }
    public decimal? ParameterValue { get; set; }
    public int FinalStepId { get; set; }
    public List<TechStep> Steps { get; set; } = [];
    public List<TechInput> Inputs { get; set; } = [];
}
public record ApplyRoute(string Name, int ProductId, int? RevisionId, decimal? ParameterValue);
public record PriceWrite(decimal? PurchasePrice);
