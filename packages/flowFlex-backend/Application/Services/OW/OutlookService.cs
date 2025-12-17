using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Application.Contracts.Dtos.OW.Message;
using FlowFlex.Application.Contracts.IServices.OW;

namespace FlowFlex.Application.Services.OW;

/// <summary>
/// Outlook integration service using Microsoft Graph API
/// </summary>
public class OutlookService : IOutlookService, IScopedService
{
    private readonly HttpClient _httpClient;
    private readonly OutlookOptions _options;
    private readonly IMessageRepository _messageRepository;
    private readonly ILogger<OutlookService> _logger;

    public OutlookService(
        HttpClient httpClient,
        IOptions<OutlookOptions> options,
        IMessageRepository messageRepository,
        ILogger<OutlookService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _messageRepository = messageRepository;
        _logger = logger;
    }

    #region OAuth Authentication

    /// <summary>
    /// Get Microsoft OAuth authorization URL
    /// </summary>
    public string GetAuthorizationUrl(string state)
    {
        var scopes = "User.Read Mail.Read Mail.ReadWrite Mail.Send offline_access";
        //// 优先使用本地开发环境的 RedirectUri
        //var redirectUri = !string.IsNullOrEmpty(_options.RedirectUriLocal) 
        //    ? _options.RedirectUriLocal 
        //    : _options.RedirectUri;

        var redirectUri = _options.RedirectUri;

        // Use "common" for multi-tenant apps that support both organizational and personal accounts
        // Use "consumers" for personal Microsoft accounts only
        // Use "organizations" for organizational accounts only
        // Use specific tenant ID for single-tenant apps
        var tenant = GetOAuthTenant();

        var authUrl = $"{_options.Instance}/{tenant}/oauth2/v2.0/authorize" +
            $"?client_id={_options.ClientId}" +
            $"&response_type=code" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&scope={Uri.EscapeDataString(scopes)}" +
            $"&state={state}" +
            $"&response_mode=query";

        return authUrl;
    }

    /// <summary>
    /// Get OAuth tenant identifier based on configuration
    /// Returns "common" for multi-tenant apps, or specific tenant ID for single-tenant
    /// </summary>
    private string GetOAuthTenant()
    {
        // If TenantId is empty, "common", "consumers", or "organizations", use it directly
        // Otherwise use "common" to support both organizational and personal accounts
        if (string.IsNullOrEmpty(_options.TenantId) ||
            _options.TenantId.Equals("common", StringComparison.OrdinalIgnoreCase) ||
            _options.TenantId.Equals("consumers", StringComparison.OrdinalIgnoreCase) ||
            _options.TenantId.Equals("organizations", StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrEmpty(_options.TenantId) ? "common" : _options.TenantId;
        }

        // For multi-tenant apps supporting personal accounts, use "common"
        // This allows both Azure AD accounts and personal Microsoft accounts (outlook.com, live.com, etc.)
        return "common";
    }

    /// <summary>
    /// Exchange authorization code for access token
    /// </summary>
    public async Task<OutlookTokenResult?> GetTokenFromAuthorizationCodeAsync(string authorizationCode)
    {
        try
        {
            // Use the same redirect URI as authorization
            //var redirectUri = !string.IsNullOrEmpty(_options.RedirectUriLocal) 
            //    ? _options.RedirectUriLocal 
            //    : _options.RedirectUri;
            var redirectUri = _options.RedirectUri;
            var tokenRequestParams = new Dictionary<string, string>
            {
                { "client_id", _options.ClientId },
                { "client_secret", _options.ClientSecret },
                { "code", authorizationCode },
                { "redirect_uri", redirectUri },
                { "grant_type", "authorization_code" },
                { "scope", "User.Read Mail.Read Mail.ReadWrite Mail.Send offline_access" }
            };

            var content = new FormUrlEncodedContent(tokenRequestParams);
            var tenant = GetOAuthTenant();
            var tokenEndpoint = $"{_options.Instance}/{tenant}{_options.TokenEndpoint}";

            var response = await _httpClient.PostAsync(tokenEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get token: {Error}", errorContent);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<OAuthTokenResponse>(responseContent);

            return tokenResponse?.ToOutlookTokenResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting token from authorization code");
            return null;
        }
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    public async Task<OutlookTokenResult?> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var tokenRequestParams = new Dictionary<string, string>
            {
                { "client_id", _options.ClientId },
                { "client_secret", _options.ClientSecret },
                { "refresh_token", refreshToken },
                { "grant_type", "refresh_token" },
                { "scope", "User.Read Mail.Read Mail.ReadWrite Mail.Send offline_access" }
            };

            var content = new FormUrlEncodedContent(tokenRequestParams);
            var tenant = GetOAuthTenant();
            var tokenEndpoint = $"{_options.Instance}/{tenant}{_options.TokenEndpoint}";

            var response = await _httpClient.PostAsync(tokenEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to refresh token: {Error}", errorContent);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<OAuthTokenResponse>(responseContent);

            return tokenResponse?.ToOutlookTokenResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return null;
        }
    }

    #endregion

    #region Email Operations

    /// <summary>
    /// Get emails from Outlook
    /// </summary>
    public async Task<List<OutlookEmailDto>> GetEmailsAsync(
        string accessToken,
        string folderId = "inbox",
        int top = 50,
        int skip = 0,
        bool? onlyUnread = null)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var url = $"{_options.BaseUrl}/me/mailFolders/{folderId}/messages" +
                $"?$top={top}&$skip={skip}" +
                $"&$orderby=receivedDateTime desc" +
                $"&$select=id,subject,bodyPreview,from,toRecipients,receivedDateTime,sentDateTime,isRead,hasAttachments,isDraft";

            if (onlyUnread == true)
            {
                url += "&$filter=isRead eq false";
            }

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get emails: {Error}", errorContent);
                return new List<OutlookEmailDto>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<GraphEmailListResponse>(content);

            return result?.Value?.Select(MapToOutlookEmailDto).ToList() ?? new List<OutlookEmailDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting emails from Outlook");
            return new List<OutlookEmailDto>();
        }
    }

    /// <summary>
    /// Get email detail by ID
    /// </summary>
    public async Task<OutlookEmailDto?> GetEmailByIdAsync(string accessToken, string messageId)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var url = $"{_options.BaseUrl}/me/messages/{messageId}" +
                $"?$select=id,subject,body,bodyPreview,from,toRecipients,ccRecipients,bccRecipients," +
                $"receivedDateTime,sentDateTime,isRead,hasAttachments,isDraft,parentFolderId";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var email = JsonSerializer.Deserialize<GraphEmailMessage>(content);

            if (email == null) return null;

            var dto = MapToOutlookEmailDto(email);

            // Process inline attachments (cid: references) if email has attachments
            if (email.HasAttachments == true && !string.IsNullOrEmpty(dto.Body) && dto.Body.Contains("cid:"))
            {
                dto.Body = await ReplaceCidWithBase64Async(accessToken, messageId, dto.Body);
            }

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email by ID: {MessageId}", messageId);
            return null;
        }
    }

    /// <summary>
    /// Send email via Outlook
    /// </summary>
    public async Task<bool> SendEmailAsync(string accessToken, OutlookSendEmailDto input)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            // Build message object
            var messageObj = new Dictionary<string, object>
            {
                ["subject"] = input.Subject,
                ["body"] = new Dictionary<string, object>
                {
                    ["contentType"] = input.IsHtml ? "HTML" : "Text",
                    ["content"] = input.Body
                },
                ["toRecipients"] = input.ToRecipients.Select(r => new
                {
                    emailAddress = new { address = r.Email, name = r.Name }
                }).ToList()
            };

            // Add CC recipients if present
            if (input.CcRecipients?.Any() == true)
            {
                messageObj["ccRecipients"] = input.CcRecipients.Select(r => new
                {
                    emailAddress = new { address = r.Email, name = r.Name }
                }).ToList();
            }

            // Add BCC recipients if present
            if (input.BccRecipients?.Any() == true)
            {
                messageObj["bccRecipients"] = input.BccRecipients.Select(r => new
                {
                    emailAddress = new { address = r.Email, name = r.Name }
                }).ToList();
            }

            // Add attachments if present
            if (input.Attachments?.Any() == true)
            {
                messageObj["attachments"] = input.Attachments.Select(a => new Dictionary<string, object>
                {
                    ["@odata.type"] = "#microsoft.graph.fileAttachment",
                    ["name"] = a.FileName,
                    ["contentType"] = a.ContentType,
                    ["contentBytes"] = Convert.ToBase64String(a.ContentBytes),
                    ["isInline"] = a.IsInline,
                    ["contentId"] = a.ContentId ?? ""
                }).ToList();
            }

            var requestBody = new Dictionary<string, object>
            {
                ["message"] = messageObj,
                ["saveToSentItems"] = true
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_options.BaseUrl}/me/sendMail", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send email: {Error}", errorContent);
                return false;
            }

            _logger.LogInformation("Email sent successfully with {AttachmentCount} attachments",
                input.Attachments?.Count ?? 0);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email via Outlook");
            return false;
        }
    }

    /// <summary>
    /// Mark email as read
    /// </summary>
    public async Task<bool> MarkAsReadAsync(string accessToken, string messageId)
    {
        return await UpdateEmailPropertyAsync(accessToken, messageId, new { isRead = true });
    }

    /// <summary>
    /// Mark email as unread
    /// </summary>
    public async Task<bool> MarkAsUnreadAsync(string accessToken, string messageId)
    {
        return await UpdateEmailPropertyAsync(accessToken, messageId, new { isRead = false });
    }

    /// <summary>
    /// Delete email (move to deleted items)
    /// </summary>
    public async Task<bool> DeleteEmailAsync(string accessToken, string messageId)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.DeleteAsync($"{_options.BaseUrl}/me/messages/{messageId}");

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting email: {MessageId}", messageId);
            return false;
        }
    }

    /// <summary>
    /// Move email to folder
    /// </summary>
    public async Task<bool> MoveEmailAsync(string accessToken, string messageId, string destinationFolderId)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var body = new { destinationId = destinationFolderId };
            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{_options.BaseUrl}/me/messages/{messageId}/move", content);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving email: {MessageId}", messageId);
            return false;
        }
    }

    /// <summary>
    /// Get folder statistics
    /// </summary>
    public async Task<OutlookFolderStats?> GetFolderStatsAsync(string accessToken, string folderId)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var url = $"{_options.BaseUrl}/me/mailFolders/{folderId}?$select=displayName,totalItemCount,unreadItemCount";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var folder = JsonSerializer.Deserialize<GraphMailFolder>(content);

            return folder != null ? new OutlookFolderStats
            {
                FolderId = folderId,
                DisplayName = folder.DisplayName,
                TotalCount = folder.TotalItemCount ?? 0,
                UnreadCount = folder.UnreadItemCount ?? 0
            } : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting folder stats: {FolderId}", folderId);
            return null;
        }
    }

    /// <summary>
    /// Process cid: references in HTML body by replacing them with base64 data URIs
    /// Used for emails that were previously synced without inline image processing
    /// </summary>
    public async Task<string> ProcessCidReferencesAsync(string accessToken, string externalMessageId, string htmlBody)
    {
        if (string.IsNullOrEmpty(htmlBody) || !htmlBody.Contains("cid:"))
        {
            return htmlBody;
        }

        return await ReplaceCidWithBase64Async(accessToken, externalMessageId, htmlBody);
    }

    #endregion

    #region Sync Operations

    /// <summary>
    /// Sync emails from Outlook to local database
    /// </summary>
    public async Task<int> SyncEmailsToLocalAsync(string accessToken, long ownerId, string folderId = "inbox", int maxCount = 100)
    {
        try
        {
            var emails = await GetEmailsAsync(accessToken, folderId, maxCount, 0);
            var syncedCount = 0;

            foreach (var email in emails)
            {
                // Check if already exists by ExternalMessageId
                var existing = await _messageRepository.GetByExternalMessageIdAsync(email.Id, ownerId);
                if (existing != null) continue;

                // Check if this is a locally sent email (same subject, sender, and sent time within 5 minutes)
                // This handles the case where we sent an email locally but haven't set ExternalMessageId yet
                var localFolder = MapOutlookFolderToLocal(folderId);
                if (localFolder == "Sent" && email.SentDateTime.HasValue)
                {
                    var localMessage = await _messageRepository.FindLocalSentMessageAsync(
                        ownerId,
                        email.Subject ?? "",
                        email.SentDateTime.Value,
                        TimeSpan.FromMinutes(5));

                    if (localMessage != null)
                    {
                        // Update the local message with ExternalMessageId
                        localMessage.ExternalMessageId = email.Id;
                        await _messageRepository.UpdateAsync(localMessage);
                        _logger.LogInformation("Linked local sent message {LocalId} with Outlook message {OutlookId}",
                            localMessage.Id, email.Id);
                        continue;
                    }
                }

                // Create local message
                var message = new Message
                {
                    Subject = email.Subject ?? "",
                    Body = email.Body ?? "",
                    BodyPreview = email.BodyPreview ?? "",
                    MessageType = "Email",
                    Folder = localFolder,
                    Labels = "[\"External\"]",
                    SenderName = email.FromName ?? "",
                    SenderEmail = email.FromEmail ?? "",
                    Recipients = JsonSerializer.Serialize(email.ToRecipients ?? new List<RecipientDto>()),
                    IsRead = email.IsRead,
                    IsDraft = email.IsDraft,
                    HasAttachments = email.HasAttachments,
                    SentDate = email.SentDateTime,
                    ReceivedDate = email.ReceivedDateTime,
                    ExternalMessageId = email.Id,
                    OwnerId = ownerId,
                    IsValid = true
                };

                // Initialize snowflake ID before insert
                message.InitNewId();

                await _messageRepository.InsertAsync(message);
                syncedCount++;
            }

            _logger.LogInformation("Synced {Count} emails from Outlook for user {UserId}", syncedCount, ownerId);
            return syncedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing emails from Outlook");
            return 0;
        }
    }

    #endregion

    #region Private Methods

    private async Task<bool> UpdateEmailPropertyAsync(string accessToken, string messageId, object updateData)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var json = JsonSerializer.Serialize(updateData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(new HttpMethod("PATCH"),
                $"{_options.BaseUrl}/me/messages/{messageId}")
            {
                Content = content
            };

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating email property: {MessageId}", messageId);
            return false;
        }
    }

    private static OutlookEmailDto MapToOutlookEmailDto(GraphEmailMessage email)
    {
        return new OutlookEmailDto
        {
            Id = email.Id ?? "",
            Subject = email.Subject ?? "",
            Body = email.Body?.Content ?? "",
            BodyPreview = email.BodyPreview ?? "",
            FromName = email.From?.EmailAddress?.Name ?? "",
            FromEmail = email.From?.EmailAddress?.Address ?? "",
            ToRecipients = email.ToRecipients?.Select(r => new RecipientDto
            {
                Name = r.EmailAddress?.Name ?? "",
                Email = r.EmailAddress?.Address ?? ""
            }).ToList() ?? new List<RecipientDto>(),
            CcRecipients = email.CcRecipients?.Select(r => new RecipientDto
            {
                Name = r.EmailAddress?.Name ?? "",
                Email = r.EmailAddress?.Address ?? ""
            }).ToList(),
            BccRecipients = email.BccRecipients?.Select(r => new RecipientDto
            {
                Name = r.EmailAddress?.Name ?? "",
                Email = r.EmailAddress?.Address ?? ""
            }).ToList(),
            SentDateTime = email.SentDateTime,
            ReceivedDateTime = email.ReceivedDateTime,
            IsRead = email.IsRead ?? false,
            IsDraft = email.IsDraft ?? false,
            HasAttachments = email.HasAttachments ?? false,
            ParentFolderId = email.ParentFolderId
        };
    }

    private static string MapOutlookFolderToLocal(string outlookFolderId)
    {
        return outlookFolderId.ToLower() switch
        {
            "inbox" => "Inbox",
            "sentitems" => "Sent",
            "drafts" => "Drafts",
            "deleteditems" => "Trash",
            "archive" => "Archive",
            _ => "Inbox"
        };
    }

    /// <summary>
    /// Get inline attachments for a message
    /// </summary>
    private async Task<List<GraphAttachment>> GetInlineAttachmentsAsync(string accessToken, string messageId)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            // Get all attachments without $select to get full fileAttachment properties
            // contentId and contentBytes are only available on fileAttachment type, not base attachment
            var url = $"{_options.BaseUrl}/me/messages/{messageId}/attachments";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to get attachments for message {MessageId}: {Error}", messageId, errorContent);
                return new List<GraphAttachment>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<GraphAttachmentListResponse>(content);

            // Filter for inline attachments only (those with isInline=true and contentId)
            var inlineAttachments = result?.Value?
                .Where(a => a.IsInline == true && !string.IsNullOrEmpty(a.ContentId))
                .ToList() ?? new List<GraphAttachment>();

            _logger.LogDebug("Found {Total} attachments, {Inline} are inline for message {MessageId}",
                result?.Value?.Count ?? 0, inlineAttachments.Count, messageId);

            return inlineAttachments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inline attachments for message {MessageId}", messageId);
            return new List<GraphAttachment>();
        }
    }

    /// <summary>
    /// Replace cid: references in HTML body with base64 data URIs
    /// </summary>
    private async Task<string> ReplaceCidWithBase64Async(string accessToken, string messageId, string htmlBody)
    {
        try
        {
            var inlineAttachments = await GetInlineAttachmentsAsync(accessToken, messageId);

            if (inlineAttachments.Count == 0)
            {
                return htmlBody;
            }

            // Build a dictionary of contentId -> base64 data URI
            var cidMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var attachment in inlineAttachments)
            {
                if (!string.IsNullOrEmpty(attachment.ContentId) && !string.IsNullOrEmpty(attachment.ContentBytes))
                {
                    var contentType = attachment.ContentType ?? "image/png";
                    var dataUri = $"data:{contentType};base64,{attachment.ContentBytes}";

                    // ContentId may or may not have angle brackets, handle both cases
                    var contentId = attachment.ContentId.Trim('<', '>');
                    cidMap[contentId] = dataUri;
                }
            }

            if (cidMap.Count == 0)
            {
                return htmlBody;
            }

            // Replace all cid: references with base64 data URIs
            var result = Regex.Replace(
                htmlBody,
                @"(src|background)=[""']cid:([^""']+)[""']",
                match =>
                {
                    var attribute = match.Groups[1].Value;
                    var contentId = match.Groups[2].Value;

                    if (cidMap.TryGetValue(contentId, out var dataUri))
                    {
                        return $"{attribute}=\"{dataUri}\"";
                    }

                    // If no matching attachment found, keep original
                    return match.Value;
                },
                RegexOptions.IgnoreCase);

            _logger.LogInformation("Replaced {Count} cid references with base64 data URIs for message {MessageId}",
                cidMap.Count, messageId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replacing cid references for message {MessageId}", messageId);
            return htmlBody;
        }
    }

    #endregion
}
