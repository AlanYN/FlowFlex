namespace FlowFlex.Application.Contracts.Dtos.OW.User
{
    /// <summary>
    /// Token details DTO for token management
    /// </summary>
    public class TokenDetailsDto
    {
        /// <summary>
        /// JWT Token string
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// JWT ID (jti claim)
        /// </summary>
        public string Jti { get; set; } = string.Empty;

        /// <summary>
        /// User ID
        /// </summary>
        public long UserId { get; set; }

        /// <summary>
        /// User email
        /// </summary>
        public string UserEmail { get; set; } = string.Empty;

        /// <summary>
        /// Token issued time
        /// </summary>
        public DateTimeOffset IssuedAt { get; set; }

        /// <summary>
        /// Token expiration time
        /// </summary>
        public DateTimeOffset ExpiresAt { get; set; }

        /// <summary>
        /// Token type
        /// </summary>
        public string TokenType { get; set; } = "login";

        /// <summary>
        /// IP address where token was issued
        /// </summary>
        public string IssuedIp { get; set; } = string.Empty;

        /// <summary>
        /// User agent when token was issued
        /// </summary>
        public string UserAgent { get; set; } = string.Empty;
    }
} 