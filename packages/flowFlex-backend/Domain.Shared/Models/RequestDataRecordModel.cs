using System;
using FlowFlex.Domain.Shared.Enums;

namespace FlowFlex.Domain.Shared.Models;

public class RequestDataRecordModel
{
    public long Id { get; set; }

    /// <summary>
    /// Source
    /// </summary>
    public RecordSource Source { get; set; }

    /// <summary>
    /// Request data
    /// </summary>
    public string RequestData { get; set; }

    /// <summary>
    /// Response data
    /// </summary>
    public string ResponseData { get; set; }

    /// <summary>
    /// Create time
    /// </summary>
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// Update time
    /// </summary>
    public DateTime? UpdateTime { get; set; }

    /// <summary>
    /// Associated primary key ID
    /// </summary>
    public long ReferenceId { get; set; }

    /// <summary>
    /// Category
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Tenant ID
    /// </summary>
    public string TenantId { get; set; }

    /// <summary>
    /// Request method
    /// </summary>
    public string Method { get; set; }

    /// <summary>
    /// Request path
    /// </summary>
    public string Url { get; set; }
}
