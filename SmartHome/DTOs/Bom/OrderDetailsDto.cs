namespace SmartHome.DTOs.Bom;

public record OrderSummaryDto(int Id, string Number, DateOnly Date, int UpdateToken);
public record OrderDetailsDto(OrderSummaryDto Order, List<OrderLine> Lines);
