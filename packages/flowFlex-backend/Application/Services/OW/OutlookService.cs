using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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
        var authUrl = $"{_options.Instance}/{_options.TenantId}/oauth2/v2.0/authorize" +
            $"?client_id={_options.ClientId}" +
            $"&response_type=code" +
            $"&redirect_uri={Uri.EscapeDataString(_options.RedirectUri)}" +
            $"&scope={Uri.EscapeDataString(scopes)}" +
            $"&state={state}" +
            $"&response_mode=query";

        return authUrl;
    }

    /// <summary>
    /// Exchange authorization code for access token
    /// </summary>
    public async Task<OutlookTokenResult?> GetTokenFromAuthorizationCodeAsync(string authorizationCode)
    {
        try
        {
            var tokenRequestParams = new Dictionary<string, string>
            {
                { "client_id", _options.ClientId },
                { "client_secret", _options.ClientSecret },
                { "code", authorizationCode },
                { "redirect_uri", _options.RedirectUri },
                { "grant_type", "authorization_code" },
                { "scope", "User.Read Mail.Read Mail.ReadWrite Mail.Send offline_access" }
            };

            var content = new FormUrlEncodedContent(tokenRequestParams);
            var tokenEndpoint = $"{_options.Instance}/{_options.TenantId}{_options.TokenEndpoint}";
            
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
            var tokenEndpoint = $"{_options.Instance}/{_options.TenantId}{_options.TokenEndpoint}";
            
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

            return email != null ? MapToOutlookEmailDto(email) : null;
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

            var message = new
            {
                message = new
                {
                    subject = input.Subject,
                    body = new
                    {
                        contentType = input.IsHtml ? "HTML" : "Text",
                        content = input.Body
                    },
                    toRecipients = input.ToRecipients.Select(r => new
                    {
                        emailAddress = new { address = r.Email, name = r.Name }
                    }).ToList(),
                    ccRecipients = input.CcRecipients?.Select(r => new
                    {
                        emailAddress = new { address = r.Email, name = r.Name }
                    }).ToList(),
                    bccRecipients = input.BccRecipients?.Select(r => new
                    {
                        emailAddress = new { address = r.Email, name = r.Name }
                    }).ToList()
                },
                saveToSentItems = true
            };

            var json = JsonSerializer.Serialize(message);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_options.BaseUrl}/me/sendMail", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send email: {Error}", errorContent);
                return false;
            }

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
                // Check if already exists
                var existing = await _messageRepository.GetByExternalMessageIdAsync(email.Id, ownerId);
                if (existing != null) continue;

                // Create local message
                var message = new Message
                {
                    Subject = email.Subject ?? "",
                    Body = email.Body ?? "",
                    BodyPreview = email.BodyPreview ?? "",
                    MessageType = "Email",
                    Folder = MapOutlookFolderToLocal(folderId),
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

    #endregion
}
