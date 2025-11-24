namespace Domain.Shared.Enums;

/// <summary>
/// Field data type for mapping
/// </summary>
public enum FieldType
{
    /// <summary>
    /// Text/String field
    /// </summary>
    Text = 0,
    
    /// <summary>
    /// Email field
    /// </summary>
    Email = 1,
    
    /// <summary>
    /// Phone number field
    /// </summary>
    Phone = 2,
    
    /// <summary>
    /// Numeric field
    /// </summary>
    Number = 3,
    
    /// <summary>
    /// Date field (without time)
    /// </summary>
    Date = 4,
    
    /// <summary>
    /// DateTime field (with time)
    /// </summary>
    DateTime = 5,
    
    /// <summary>
    /// Boolean field
    /// </summary>
    Boolean = 6,
    
    /// <summary>
    /// Dropdown/Select field
    /// </summary>
    Dropdown = 7,
    
    /// <summary>
    /// Multi-select field
    /// </summary>
    MultiSelect = 8,
    
    /// <summary>
    /// JSON object field
    /// </summary>
    Json = 9
}

