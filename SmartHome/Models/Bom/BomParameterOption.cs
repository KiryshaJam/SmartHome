using System.ComponentModel.DataAnnotations;

namespace SmartHome.Models.Bom;

public class BomParameterOption
{
    public int RevisionId { get; set; }
    public int DefinitionId { get; set; }
    public int EnumValueId { get; set; }
}
