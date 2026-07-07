using Newtonsoft.Json;

namespace FlowFlex.Application.Contracts.Dtos.Integration;

/// <summary>
/// Flattened Change Log query request
/// </summary>
public class FlattenedChangeLogQueryRequest
{
    /// <summary>
    /// Entity ID stored in ff_onboarding.entity_id (e.g., Ticket ID "TK-12345")
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Incremental pull: only return records after this timestamp (ISO 8601)
    /// </summary>
    public DateTimeOffset? Since { get; set; }

    /// <summary>
    /// Page index (default 1)
    /// </summary>
    public int PageIndex { get; set; } = 1;

    /// <summary>
    /// Page size (default 20, max 100)
    /// </summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Flattened Change Log item - each record with field-level diffs
/// </summary>
public class FlattenedChangeLogItemDto
{
    /// <summary>
    /// Change log record ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the change (UTC)
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Operator information
    /// </summary>
    public ChangeLogOperatorDto Operator { get; set; } = new();

    /// <summary>
    /// Operation type (e.g., "CaseUpdate", "StageTransition", "StaticFieldValueChange")
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Flattened field-level changes
    /// </summary>
    public List<FieldChangeDto> Changes { get; set; } = new();
}

/// <summary>
/// Operator information
/// </summary>
public class ChangeLogOperatorDto
{
    /// <summary>
    /// Operator ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Operator display name
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Single field change (Old Value → New Value)
/// </summary>
public class FieldChangeDto
{
    /// <summary>
    /// Human-readable field name
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Value before the change (null if first-time set)
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public string? OldValue { get; set; }

    /// <summary>
    /// Value after the change
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public string? NewValue { get; set; }
}

/// <summary>
/// Paged response for flattened change logs
/// </summary>
public class FlattenedChangeLogPagedResponse
{
    /// <summary>
    /// Change log items
    /// </summary>
    public List<FlattenedChangeLogItemDto> Items { get; set; } = new();

    /// <summary>
    /// Total record count
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page index
    /// </summary>
    public int PageIndex { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }
}
