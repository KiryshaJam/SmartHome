namespace SmartHome.Models.Routes;

public class TechRouteSetting
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int ProductId { get; set; }
    public int? RevisionId { get; set; }
    public int FinalStepId { get; set; }
    public string Kind { get; set; } = "";
    public int? ParameterId { get; set; }
    public decimal? ParameterValue { get; set; }
}
