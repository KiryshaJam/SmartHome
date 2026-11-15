namespace SmartHome.Models.Routes;

public class TechStep
{
    public int Id { get; set; }
    public string Kind { get; set; } = "";
    public int ProductId { get; set; }
    public string ResultName { get; set; } = "";
    public int UnitId { get; set; }
    public int Number { get; set; }
    public int? OperationId { get; set; }
    public int? WorkCenterId { get; set; }
    public int? ProfessionId { get; set; }
    public int? QualificationId { get; set; }
    public string Tooling { get; set; } = "";
    public decimal SetupMinutes { get; set; }
    public decimal UnitMinutes { get; set; }
    public decimal HourlyRate { get; set; }
}
