namespace Domain.Shared.Enums;

/// <summary>
/// Quick link redirect type
/// </summary>
public enum RedirectType
{
    /// <summary>
    /// Direct redirect without confirmation
    /// </summary>
    Direct = 0,

    /// <summary>
    /// Show popup confirmation before redirect
    /// </summary>
    PopupConfirmation = 1
}

