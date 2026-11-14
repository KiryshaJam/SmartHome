namespace SmartHome.DTOs.Bom;

public record OrderWrite(string Number, DateOnly Date, int? ExpectedUpdateToken, List<OrderLine> Lines);
