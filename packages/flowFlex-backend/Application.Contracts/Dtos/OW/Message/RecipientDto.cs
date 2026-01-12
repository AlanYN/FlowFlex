namespace FlowFlex.Application.Contracts.Dtos.OW.Message;

/// <summary>
/// Recipient DTO
/// </summary>
public class RecipientDto
{
    /// <summary>
    /// User ID (for internal users)
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// Recipient Name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Recipient Email Address
    /// </summary>
    public string Email { get; set; } = string.Empty;
}
