using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.Options;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Infrastructure.Services;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// JWT service implementation
    /// </summary>
    public class JwtService : ISingletonService, IJwtService
    {
        private readonly JwtOptions _jwtOptions;

        public JwtService(IOptions<JwtOptions> jwtOptions)
        {
            _jwtOptions = jwtOptions.Value;
            
            // Add validation to ensure configuration is loaded properly
            if (string.IsNullOrEmpty(_jwtOptions.SecretKey))
            {
                throw new InvalidOperationException("JWT SecretKey is not configured properly. Please check the Security:JwtSecretKey setting in appsettings.json");
            }
            
            if (string.IsNullOrEmpty(_jwtOptions.Issuer))
            {
                throw new InvalidOperationException("JWT Issuer is not configured properly. Please check the Security:JwtIssuer setting in appsettings.json");
            }
            
            if (string.IsNullOrEmpty(_jwtOptions.Audience))
            {
                throw new InvalidOperationException("JWT Audience is not configured properly. Please check the Security:JwtAudience setting in appsettings.json");
            }
        }

        /// <summary>
        /// Generate JWT token
        /// </summary>
        /// <param name="user">User entity</param>
        /// <returns>JWT token</returns>
        public string GenerateJwtToken(User user)
        {
            return GenerateToken(user.Id, user.Email, user.Username, user.TenantId);
        }

        /// <summary>
        /// Generate JWT token
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="email">User email</param>
        /// <param name="username">Username</param>
        /// <returns>JWT token</returns>
        public string GenerateToken(long userId, string email, string username)
        {
            return GenerateToken(userId, email, username, "DEFAULT");
        }

        /// <summary>
        /// Generate JWT token
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="email">User email</param>
        /// <param name="username">Username</param>
        /// <param name="tenantId">Tenant ID</param>
        /// <returns>JWT token</returns>
        public string GenerateToken(long userId, string email, string username, string tenantId)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("username", username),
                new Claim("tenantId", tenantId ?? "DEFAULT"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var token = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Validate JWT token
        /// </summary>
        /// <param name="token">JWT token</param>
        /// <returns>Validation result</returns>
        public bool ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtOptions.SecretKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _jwtOptions.Issuer,
                    ValidAudience = _jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };

                tokenHandler.ValidateToken(token, validationParameters, out _);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get user ID from token
        /// </summary>
        /// <param name="token">JWT token</param>
        /// <returns>User ID</returns>
        public long? GetUserIdFromToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);

                var userIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub || x.Type == ClaimTypes.NameIdentifier);
                if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var userId))
                {
                    return userId;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get user email from token
        /// </summary>
        /// <param name="token">JWT token</param>
        /// <returns>User email</returns>
        public string GetEmailFromToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);

                var emailClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Email || x.Type == ClaimTypes.Email);
                return emailClaim?.Value;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get token expiry time in seconds
        /// </summary>
        /// <returns>Expiry time in seconds</returns>
        public int GetTokenExpiryInSeconds()
        {
            return _jwtOptions.ExpiryMinutes * 60;
        }
    }
}