using FlowFlex.Application.Contracts.Dtos.OW.Message;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.OW;

/// <summary>
/// Outlook integration service interface
/// </summary>
public interface IOutlookService : IScopedService
{
    #region OAuth Authentication

    /// <summary>
    /// Get Microsoft OAuth authorization URL
    /// </summary>
    string GetAuthorizationUrl(string state);

    /// <summary>
    /// Exchange authorization code for access token
    /// </summary>
    Task<OutlookTokenResult?> GetTokenFromAuthorizationCodeAsync(string authorizationCode);

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    Task<OutlookTokenResult?> RefreshTokenAsync(string refreshToken);

    #endregion

    #region Email Operations

    /// <summary>
    /// Get emails from Outlook
    /// </summary>
    Task<List<OutlookEmailDto>> GetEmailsAsync(
        string accessToken,
        string folderId = "inbox",
        int top = 50,
        int skip = 0,
        bool? onlyUnread = null);

    /// <summary>
    /// Get email detail by ID
    /// </summary>
    Task<OutlookEmailDto?> GetEmailByIdAsync(string accessToken, string messageId);

    /// <summary>
    /// Send email via Outlook
    /// </summary>
    Task<bool> SendEmailAsync(string accessToken, OutlookSendEmailDto input);

    /// <summary>
    /// Mark email as read
    /// </summary>
    Task<bool> MarkAsReadAsync(string accessToken, string messageId);

    /// <summary>
    /// Mark email as unread
    /// </summary>
    Task<bool> MarkAsUnreadAsync(string accessToken, string messageId);

    /// <summary>
    /// Delete email (move to deleted items)
    /// </summary>
    Task<bool> DeleteEmailAsync(string accessToken, string messageId);

    /// <summary>
    /// Move email to folder
    /// </summary>
    Task<bool> MoveEmailAsync(string accessToken, string messageId, string destinationFolderId);

    /// <summary>
    /// Get folder statistics
    /// </summary>
    Task<OutlookFolderStats?> GetFolderStatsAsync(string accessToken, string folderId);

    /// <summary>
    /// Process cid: references in HTML body by replacing them with base64 data URIs
    /// Used for emails that were previously synced without inline image processing
    /// </summary>
    /// <param name="accessToken">Outlook access token</param>
    /// <param name="externalMessageId">External message ID in Outlook</param>
    /// <param name="htmlBody">HTML body containing cid: references</param>
    /// <returns>HTML body with cid: references replaced by base64 data URIs</returns>
    Task<string> ProcessCidReferencesAsync(string accessToken, string externalMessageId, string htmlBody);

    #endregion

    #region Sync Operations

    /// <summary>
    /// Sync emails from Outlook to local database
    /// </summary>
    Task<int> SyncEmailsToLocalAsync(string accessToken, long ownerId, string folderId = "inbox", int maxCount = 100);

    #endregion
}

#region DTOs

/// <summary>
/// Outlook OAuth token result
/// </summary>
public class OutlookTokenResult
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

/// <summary>
/// Outlook email DTO
/// </summary>
public class OutlookEmailDto
{
    public string Id { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string BodyPreview { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public List<RecipientDto> ToRecipients { get; set; } = new();
    public List<RecipientDto>? CcRecipients { get; set; }
    public List<RecipientDto>? BccRecipients { get; set; }
    public DateTimeOffset? SentDateTime { get; set; }
    public DateTimeOffset? ReceivedDateTime { get; set; }
    public bool IsRead { get; set; }
    public bool IsDraft { get; set; }
    public bool HasAttachments { get; set; }
    public string? ParentFolderId { get; set; }
}

/// <summary>
/// Outlook send email DTO
/// </summary>
public class OutlookSendEmailDto
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
    public List<RecipientDto> ToRecipients { get; set; } = new();
    public List<RecipientDto>? CcRecipients { get; set; }
    public List<RecipientDto>? BccRecipients { get; set; }
    public List<OutlookAttachmentDto>? Attachments { get; set; }
}

/// <summary>
/// Outlook attachment DTO for sending emails
/// </summary>
public class OutlookAttachmentDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public byte[] ContentBytes { get; set; } = Array.Empty<byte>();
    public bool IsInline { get; set; } = false;
    public string? ContentId { get; set; }
}

/// <summary>
/// Outlook folder statistics
/// </summary>
public class OutlookFolderStats
{
    public string FolderId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
}

#endregion
