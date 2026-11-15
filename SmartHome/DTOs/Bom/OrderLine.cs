using System.Text.Json;

namespace SmartHome.DTOs.Bom;

public record OrderLine(int VariantId, decimal Quantity);
