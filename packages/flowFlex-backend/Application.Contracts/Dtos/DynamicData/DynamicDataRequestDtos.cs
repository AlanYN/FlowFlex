using FlowFlex.Domain.Shared.Enums.DynamicData;

namespace FlowFlex.Application.Contracts.Dtos.DynamicData;

/// <summary>
/// Create business data request
/// </summary>
public class CreateBusinessDataRequest
{
    /// <summary>
    /// Field values
    /// </summary>
    public List<FieldValueRequest>? Fields { get; set; }

    /// <summary>
    /// Internal extension data
    /// </summary>
    public Newtonsoft.Json.Linq.JObject? InternalData { get; set; }
}

/// <summary>
/// Update business data request
/// </summary>
public class UpdateBusinessDataRequest
{
    /// <summary>
    /// Field values
    /// </summary>
    public List<FieldValueRequest>? Fields { get; set; }

    /// <summary>
    /// Internal extension data
    /// </summary>
    public Newtonsoft.Json.Linq.JObject? InternalData { get; set; }
}

/// <summary>
/// Field value request
/// </summary>
public class FieldValueRequest
{
    /// <summary>
    /// Field name
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Field value
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Data type
    /// </summary>
    public DataType DataType { get; set; }
}

/// <summary>
/// Move to group request
/// </summary>
public class MoveToGroupRequest
{
    /// <summary>
    /// Property IDs to move
    /// </summary>
    public long[] PropertyIds { get; set; } = Array.Empty<long>();

    /// <summary>
    /// Target group ID
    /// </summary>
    public long GroupId { get; set; }
}

/// <summary>
/// Batch get properties request
/// </summary>
public class BatchGetPropertiesRequest
{
    /// <summary>
    /// Property IDs to query
    /// </summary>
    public long[] Ids { get; set; } = Array.Empty<long>();
}
