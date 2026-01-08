using System.ComponentModel.DataAnnotations;
using FlowFlex.Domain.Entities.Base;
using SqlSugar;

namespace FlowFlex.Domain.Entities.Integration;

/// <summary>
/// Integration API Log - Records external API calls for statistics
/// </summary>
[SugarTable("ff_integration_api_log")]
public class IntegrationApiLog : EntityBaseCreateInfo
{
    /// <summary>
    /// Integration ID
    /// </summary>
    [Required]
    [SugarColumn(ColumnName = "integration_id")]
    public long IntegrationId { get; set; }

    /// <summary>
    /// System ID (from external system)
    /// </summary>
    [StringLength(100)]
    [SugarColumn(ColumnName = "system_id")]
    public string SystemId { get; set; } = string.Empty;

    /// <summary>
    /// API endpoint path (e.g., /workflows, /case-info, /attachments)
    /// </summary>
    [Required]
    [StringLength(200)]
    [SugarColumn(ColumnName = "endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// HTTP method (GET, POST, PUT, DELETE)
    /// </summary>
    [Required]
    [StringLength(10)]
    [SugarColumn(ColumnName = "http_method")]
    public string HttpMethod { get; set; } = "GET";

    /// <summary>
    /// Request start time
    /// </summary>
    [SugarColumn(ColumnName = "started_at")]
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// Request completion time
    /// </summary>
    [SugarColumn(ColumnName = "completed_at")]
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Duration in milliseconds
    /// </summary>
    [SugarColumn(ColumnName = "duration_ms")]
    public long? DurationMs { get; set; }

    /// <summary>
    /// HTTP status code
    /// </summary>
    [SugarColumn(ColumnName = "status_code")]
    public int StatusCode { get; set; }

    /// <summary>
    /// Whether the request was successful
    /// </summary>
    [SugarColumn(ColumnName = "is_success")]
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    [StringLength(2000)]
    [SugarColumn(ColumnName = "error_message")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Request parameters (JSON)
    /// </summary>
    [SugarColumn(ColumnName = "request_params", ColumnDataType = "jsonb", IsJson = true)]
    public string? RequestParams { get; set; }

    /// <summary>
    /// Caller user ID
    /// </summary>
    [SugarColumn(ColumnName = "caller_user_id")]
    public long? CallerUserId { get; set; }

    /// <summary>
    /// Caller IP address
    /// </summary>
    [StringLength(50)]
    [SugarColumn(ColumnName = "caller_ip")]
    public string? CallerIp { get; set; }
}
