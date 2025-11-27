namespace Domain.Shared.Enums;

/// <summary>
/// Integration action type
/// </summary>
public enum ActionType
{
    /// <summary>
    /// System built-in action
    /// </summary>
    SystemAction = 0,

    /// <summary>
    /// Webhook action
    /// </summary>
    Webhook = 1,

    /// <summary>
    /// Custom script action
    /// </summary>
    CustomScript = 2,

    /// <summary>
    /// Data transformation action
    /// </summary>
    DataTransform = 3
}

