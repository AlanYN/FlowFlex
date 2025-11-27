namespace Domain.Shared.Enums;

/// <summary>
/// Field mapping sync direction
/// </summary>
public enum SyncDirection
{
    /// <summary>
    /// Inbound only - View only in WFE, cannot edit
    /// </summary>
    ViewOnly = 0,

    /// <summary>
    /// Bidirectional sync - Can edit in WFE and sync back to external system
    /// </summary>
    Editable = 1,

    /// <summary>
    /// Outbound only - Only sync from WFE to external system
    /// </summary>
    OutboundOnly = 2
}

