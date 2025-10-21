namespace FlowFlex.Application.Contracts.Options
{
    /// <summary>
    /// Identity Hub configuration options
    /// </summary>
    public class IdentityHubOptions
    {
        /// <summary>
        /// IDM service base address
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// Client ID
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Client secret
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// Token endpoint
        /// </summary>
        public string TokenEndpoint { get; set; } = "/connect/token";

        /// <summary>
        /// User extension information endpoint
        /// </summary>
        public string UserExtensionEndpoint { get; set; } = "/api/v1/users/current/extension";

        /// <summary>
        /// Role permission check endpoint
        /// </summary>
        public string UserRolePermissionCheck { get; set; } = "/api/v1/role/check";

        /// <summary>
        /// User select endpoint
        /// </summary>
        public string UserSelect { get; set; } = "/api/v1/users/user-select";

        /// <summary>
        /// Query user endpoint
        /// </summary>
        public string QueryUser { get; set; } = "/api/v1/users";

        /// <summary>
        /// Query teams endpoint
        /// </summary>
        public string QueryTeams { get; set; } = "/api/v1/public/teams";

        /// <summary>
        /// Query team users endpoint
        /// </summary>
        public string QueryTeamUsers { get; set; } = "/api/v1/public/teamusers";

        /// <summary>
        /// Search user by username or email endpoint
        /// </summary>
        public string SearchUserByUserNameOrEmail { get; set; } = "/api/v1/users/search";

        /// <summary>
        /// Company information endpoint
        /// </summary>
        public string GetCompanyEndpoint { get; set; } = "/api/v1/masterData/company";

        /// <summary>
        /// Current user information endpoint
        /// </summary>
        public string GetCurrentUserInfoEndpoint { get; set; } = "/api/v1/users/current/info";

        /// <summary>
        /// App ID for IDM public API requests (required for /api/v1/public/* endpoints)
        /// </summary>
        public string AppId { get; set; } = "5";
    }
}
