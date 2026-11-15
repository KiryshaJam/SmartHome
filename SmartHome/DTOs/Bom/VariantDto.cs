namespace SmartHome.DTOs.Bom;

public record VariantDto(int Id, int RevisionId, string Name, DateTime CreatedAt, string Snapshot);
