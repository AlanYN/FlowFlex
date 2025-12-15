using System.Text.Json.Serialization;
using FlowFlex.Application.Contracts.IServices.OW;

namespace FlowFlex.Application.Services.OW;

/// <summary>
/// Outlook API configuration options
/// </summary>
public class OutlookOptions
{
    public const string SectionName = "OutlookApis";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://graph.microsoft.com/v1.0";
    public string Instance { get; set; } = "https://login.microsoftonline.com";
    public string TokenEndpoint { get; set; } = "/oauth2/v2.0/token";
}

/// <summary>
/// OAuth token response from Microsoft
/// </summary>
public class OAuthTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;

    public OutlookTokenResult ToOutlookTokenResult()
    {
        return new OutlookTokenResult
        {
            AccessToken = AccessToken,
            RefreshToken = RefreshToken,
            ExpiresIn = ExpiresIn,
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(ExpiresIn)
        };
    }
}

/// <summary>
/// Graph API email list response
/// </summary>
public class GraphEmailListResponse
{
    [JsonPropertyName("value")]
    public List<GraphEmailMessage>? Value { get; set; }

    [JsonPropertyName("@odata.nextLink")]
    public string? NextLink { get; set; }
}

/// <summary>
/// Graph API email message
/// </summary>
public class GraphEmailMessage
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    [JsonPropertyName("body")]
    public GraphEmailBody? Body { get; set; }

    [JsonPropertyName("bodyPreview")]
    public string? BodyPreview { get; set; }

    [JsonPropertyName("from")]
    public GraphEmailRecipient? From { get; set; }

    [JsonPropertyName("toRecipients")]
    public List<GraphEmailRecipient>? ToRecipients { get; set; }

    [JsonPropertyName("ccRecipients")]
    public List<GraphEmailRecipient>? CcRecipients { get; set; }

    [JsonPropertyName("bccRecipients")]
    public List<GraphEmailRecipient>? BccRecipients { get; set; }

    [JsonPropertyName("sentDateTime")]
    public DateTimeOffset? SentDateTime { get; set; }

    [JsonPropertyName("receivedDateTime")]
    public DateTimeOffset? ReceivedDateTime { get; set; }

    [JsonPropertyName("isRead")]
    public bool? IsRead { get; set; }

    [JsonPropertyName("isDraft")]
    public bool? IsDraft { get; set; }

    [JsonPropertyName("hasAttachments")]
    public bool? HasAttachments { get; set; }

    [JsonPropertyName("parentFolderId")]
    public string? ParentFolderId { get; set; }
}

/// <summary>
/// Graph API email body
/// </summary>
public class GraphEmailBody
{
    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

/// <summary>
/// Graph API email recipient
/// </summary>
public class GraphEmailRecipient
{
    [JsonPropertyName("emailAddress")]
    public GraphEmailAddress? EmailAddress { get; set; }
}

/// <summary>
/// Graph API email address
/// </summary>
public class GraphEmailAddress
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }
}

/// <summary>
/// Graph API mail folder
/// </summary>
public class GraphMailFolder
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("totalItemCount")]
    public int? TotalItemCount { get; set; }

    [JsonPropertyName("unreadItemCount")]
    public int? UnreadItemCount { get; set; }
}
