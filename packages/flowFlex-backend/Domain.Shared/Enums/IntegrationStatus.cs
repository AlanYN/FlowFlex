namespace Domain.Shared.Enums;

/// <summary>
/// Integration connection status
/// </summary>
public enum IntegrationStatus
{
    /// <summary>
    /// Not connected or connection lost
    /// </summary>
    Disconnected = 0,
    
    /// <summary>
    /// Successfully connected
    /// </summary>
    Connected = 1,
    
    /// <summary>
    /// Connection error
    /// </summary>
    Error = 2,
    
    /// <summary>
    /// Testing connection
    /// </summary>
    Testing = 3
}

