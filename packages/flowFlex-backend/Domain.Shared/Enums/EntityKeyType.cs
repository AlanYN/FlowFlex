namespace Domain.Shared.Enums;

/// <summary>
/// Entity key type for matching external and internal records
/// </summary>
public enum EntityKeyType
{
    /// <summary>
    /// Use external system's unique ID
    /// </summary>
    ExternalId = 0,

    /// <summary>
    /// Use email as unique identifier
    /// </summary>
    Email = 1,

    /// <summary>
    /// Use custom field as unique identifier
    /// </summary>
    CustomField = 2,

    /// <summary>
    /// Use combination of multiple fields
    /// </summary>
    CompositeKey = 3
}

