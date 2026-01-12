using FlowFlex.Application.Contracts.Dtos.OW.User;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Portal Token Service Interface
    /// Handles generation and validation of Portal-specific JWT tokens with limited scope
    /// </summary>
    public interface IPortalTokenService
    {
        /// <summary>
        /// Generate Portal-specific JWT token with limited scope
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="email">User email</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="tenantId">Tenant ID</param>
        /// <returns>Portal token details</returns>
        TokenDetailsDto GeneratePortalToken(long userId, string email, long onboardingId, string tenantId = "default");

        /// <summary>
        /// Validate Portal token and extract claims
        /// </summary>
        /// <param name="token">Portal JWT token</param>
        /// <returns>Portal token validation result</returns>
        PortalTokenValidationResult ValidatePortalToken(string token);

        /// <summary>
        /// Check if a token is a Portal token by examining its claims
        /// </summary>
        /// <param name="token">JWT token</param>
        /// <returns>True if token is a portal token</returns>
        bool IsPortalToken(string token);
    }
}

