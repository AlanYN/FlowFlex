using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace FlowFlex.Application.Contracts.Dtos.OW.User
{
    /// <summary>
    /// Basic response model for IDM APIs
    /// </summary>
    public class BasicResponse<T>
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public int Code { get; set; }
        public T Data { get; set; }
    }

    /// <summary>
    /// Pagination model for IDM APIs
    /// </summary>
    public class PageModel<T>
    {
        public int PageIndex { get; set; } = 1;
        public int PageCount { get; set; } = 0;
        public int DataCount { get; set; } = 0;
        public int PageSize { get; set; }
        public T Data { get; set; }
    }

    /// <summary>
    /// IDM User Output DTO
    /// </summary>
    public class IdmUserOutputDto
    {
        public string Id { get; set; }
        public string HeadId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool? IsActivated { get; set; }
        public string SSO { get; set; }
        public int? Source { get; set; }
        public List<string> UserGroups { get; set; } = new List<string>();
        public int Status { get; set; }
        public string CreatedBy { get; set; }
        public DateTimeOffset? CreatedTime { get; set; }
        public string UpdatedBy { get; set; }
        public DateTimeOffset? UpdatedTime { get; set; }
        public int? UserType { get; set; }
        public List<string> Companies { get; set; } = new List<string>();
        public List<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
    }

    /// <summary>
    /// User Permission model
    /// </summary>
    public class UserPermission
    {
        public string TenantId { get; set; }
        public int? UserType { get; set; }
        public List<string> RoleIds { get; set; } = new List<string>();
    }

    /// <summary>
    /// Token response model for IDM authentication
    /// </summary>
    public class IdentityHubClientTokenResponse
    {
        [JsonProperty("access_token")]
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("expires_in")]
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("token_type")]
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }

        [JsonPropertyName("error")]
        public string Error { get; set; }
    }

    /// <summary>
    /// Team information model from IDM
    /// </summary>
    public class IdmTeamDto
    {
        [JsonProperty("id")]
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonProperty("teamName")]
        [JsonPropertyName("teamName")]
        public string TeamName { get; set; }

        [JsonProperty("teamMembers")]
        [JsonPropertyName("teamMembers")]
        public int TeamMembers { get; set; }

        [JsonProperty("createDate")]
        [JsonPropertyName("createDate")]
        public string CreateDate { get; set; }
    }

    /// <summary>
    /// Team user relationship model from IDM
    /// </summary>
    public class IdmTeamUserDto
    {
        [JsonProperty("id")]
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonProperty("userName")]
        [JsonPropertyName("userName")]
        public string UserName { get; set; }

        [JsonProperty("email")]
        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonProperty("firstName")]
        [JsonPropertyName("firstName")]
        public string FirstName { get; set; }

        [JsonProperty("lastName")]
        [JsonPropertyName("lastName")]
        public string LastName { get; set; }

        [JsonProperty("userType")]
        [JsonPropertyName("userType")]
        public int? UserType { get; set; }

        [JsonProperty("teamId")]
        [JsonPropertyName("teamId")]
        public string TeamId { get; set; }

        [JsonProperty("createDate")]
        [JsonPropertyName("createDate")]
        public string CreateDate { get; set; }
    }

    /// <summary>
    /// Team tree node model from IDM teamTree API
    /// </summary>
    public class IdmTeamTreeNodeDto
    {
        [JsonProperty("value")]
        [JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonProperty("label")]
        [JsonPropertyName("label")]
        public string Label { get; set; }

        [JsonProperty("children")]
        [JsonPropertyName("children")]
        public List<IdmTeamTreeNodeDto> Children { get; set; } = new List<IdmTeamTreeNodeDto>();
    }

    /// <summary>
    /// User query type enumeration
    /// </summary>
    public enum UserQueryType
    {
        UserName = 1,
        Key = 2
    }
}
