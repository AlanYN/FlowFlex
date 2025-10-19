namespace FlowFlex.Application.Contracts.Dtos.OW.User
{
    /// <summary>
    /// Portal token validation result
    /// </summary>
    public class PortalTokenValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public long? UserId { get; set; }
        public string? Email { get; set; }
        public long? OnboardingId { get; set; }
        public string? TenantId { get; set; }
        public string? Jti { get; set; }
        public string? Scope { get; set; }
    }
}

