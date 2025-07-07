using System;
using FlowFlex.Domain.Shared.Enums.Item;

namespace FlowFlex.Domain.Shared.Models;

public class ConnectDetailModel
{
    /// <summary>
    /// Primary key ID for deletion
    /// </summary>
    public string Id { get; set; }

    public string SourceId { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Time
    /// </summary>
    public DateTimeOffset? Time { get; set; }

    /// <summary>
    /// Associated ID
    /// </summary>
    public string TargetId { get; set; }

    /// <summary>
    /// Associated type
    /// </summary>
    public RelationalTypeEnum? TargetType { get; set; }

    public RelationalTypeEnum SourceType { get; set; }
}
