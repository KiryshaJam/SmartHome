namespace SmartHome.DTOs;

public class UpdateClassParameterDto
{
    public int? ParameterGroupId { get; set; }

    public int SortOrder { get; set; }

    public bool IsRequired { get; set; }

    public decimal? MinNumberValue { get; set; }

    public decimal? MaxNumberValue { get; set; }
}