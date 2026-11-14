namespace SmartHome.DTOs.Bom;

public record RevisionDto(int Id, int ProductId, int UnitId, int? PreviousId, int? BaseRevisionId, string Reason, DateTime CreatedAt, string Document);
