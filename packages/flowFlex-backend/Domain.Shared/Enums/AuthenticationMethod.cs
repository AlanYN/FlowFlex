namespace Domain.Shared.Enums;

/// <summary>
/// Authentication method for external system connection
/// </summary>
public enum AuthenticationMethod
{
    /// <summary>
    /// API Key authentication
    /// </summary>
    ApiKey = 0,
    
    /// <summary>
    /// Basic authentication (username + password)
    /// </summary>
    BasicAuth = 1,
    
    /// <summary>
    /// OAuth 2.0 authentication
    /// </summary>
    OAuth2 = 2,
    
    /// <summary>
    /// Bearer Token authentication
    /// </summary>
    BearerToken = 3
}

