namespace Domain.Shared.Enums;

/// <summary>
/// Integration action status
/// </summary>
public enum ActionStatus
{
    /// <summary>
    /// Action is active and running
    /// </summary>
    Active = 0,

    /// <summary>
    /// Action is inactive/disabled
    /// </summary>
    Inactive = 1,

    /// <summary>
    /// Action has error
    /// </summary>
    Error = 2
}

