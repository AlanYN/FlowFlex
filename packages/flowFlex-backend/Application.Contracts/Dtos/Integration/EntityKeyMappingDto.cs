using System.ComponentModel.DataAnnotations;
using Domain.Shared.Enums;

namespace FlowFlex.Application.Contracts.Dtos.Integration;

/// <summary>
/// Entity key mapping DTO
/// </summary>
public class EntityKeyMappingDto
{
    public long Id { get; set; }
    public string KeyFieldName { get; set; } = string.Empty;
    public EntityKeyType KeyFieldType { get; set; }
    public string ExternalKeyField { get; set; } = string.Empty;
    public string WfeKeyField { get; set; } = string.Empty;
    public Dictionary<string, object> MappingRules { get; set; } = new();
}

/// <summary>
/// DTO for creating entity key mapping
/// </summary>
public class CreateEntityKeyMappingDto
{
    [Required(ErrorMessage = "Key field name is required")]
    public string KeyFieldName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Key field type is required")]
    public EntityKeyType KeyFieldType { get; set; }

    [Required(ErrorMessage = "External key field is required")]
    public string ExternalKeyField { get; set; } = string.Empty;

    [Required(ErrorMessage = "WFE key field is required")]
    public string WfeKeyField { get; set; } = string.Empty;

    public Dictionary<string, object>? MappingRules { get; set; }
}

