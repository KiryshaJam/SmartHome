namespace SmartHome.Models.Routes;

public class TechInput
{
    public int Id { get; set; }
    public int StepId { get; set; }
    public int SourceId { get; set; }
    public decimal Amount { get; set; }
    public decimal ForQuantity { get; set; }
    public int? ParameterId { get; set; }
}
