using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Shared.Const;
using FlowFlex.Domain.Shared.Models;
using Item.ThirdParty.IdentityHub;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Newtonsoft.Json;
using System.Security.Claims;

namespace WebApi.Authentication
{
    public static class TokenValidatedHandler
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <exception cref="NullReferenceException"></exception>
        public static async Task OnTokenValidated(TokenValidatedContext context)
        {
            await Task.CompletedTask;
            var principal = context.Principal ?? throw new NullReferenceException(nameof(context.Principal));
            var claims = principal.Claims.ToList();
            var userContext = context.HttpContext.RequestServices.GetService<UserContext>() ??
                              throw new NullReferenceException(nameof(UserContext));

            var userService = context.HttpContext.RequestServices.GetService<IUserService>()!;
            try
            {
                var authorization = context.HttpContext.Request.Headers.Authorization;
                var user = await userService.GetUserByEmail(claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)!.Value);
                userContext.TenantId = claims.FirstOrDefault(x => x.Type == "tenantId")?.Value ?? "";
                userContext.UserId = user.Id.ToString();
                userContext.UserName = user.Username;
                userContext.Email = user.Email;
            }
            catch
            {
                context.Fail("auth failed");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <exception cref="NullReferenceException"></exception>
        public static async Task OnIdmTokenValidated(TokenValidatedContext context)
        {
            await Task.CompletedTask;
            var principal = context.Principal ?? throw new NullReferenceException(nameof(context.Principal));
            var claims = principal.Claims.ToList();
            var userContext = context.HttpContext.RequestServices.GetService<UserContext>() ??
                              throw new NullReferenceException(nameof(UserContext));
            
            
            try
            {
                var tokenCategory = claims!.FirstOrDefault(x => x.Type == "token_category")?.Value;
                var identityHubServerClient = context.HttpContext.RequestServices.GetService<IdentityHubClient>();
                
                if (tokenCategory != null && identityHubServerClient != null)
                {
                    var authorization = context.HttpContext.Request.Headers.Authorization;
                    userContext.IamToken = authorization;
                    
                    try
                    {
                        var userExtensionResult = await identityHubServerClient.UserExtensionAsync(authorization);
                        if (userExtensionResult?.Data != null)
                        {
                            if (!userExtensionResult.Data.IsVersionMatched)
                            {
                                context.Fail("Version mismatch");
                                return;
                            }
                            
                            userContext.TenantId = userExtensionResult.Data.TenantId;
                            userContext.ClientShortName = userExtensionResult.Data.ClientShortName;
                            userContext.RoleIds = userExtensionResult.Data.RoleIds;

                            var tokenThirdParty = userExtensionResult.Data.BnpToken;
                            if (!string.IsNullOrWhiteSpace(tokenThirdParty))
                            {
                                userContext.ThirdPartyToken = tokenThirdParty;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Don't fail authentication for UserExtension errors - just log and continue
                        // This allows the token validation to succeed even if the third-party service is unavailable
                    }
                }

                // Extract user information from JWT claims
                userContext.UserId = claims!.FirstOrDefault(x => x.Type == "userId")?.Value ?? 
                                   claims.FirstOrDefault(x => x.Type == "sub")?.Value ?? "unknown";
                userContext.ValidationVersion = claims.FirstOrDefault(x => x.Type == "version")?.Value ?? "1";
                userContext.UserName = claims.FirstOrDefault(x => x.Type == "userName")?.Value ?? 
                                     claims.FirstOrDefault(x => x.Type == "name")?.Value ?? "unknown";
                userContext.FirstName = claims.FirstOrDefault(x => x.Type == "firstName")?.Value;
                userContext.LastName = claims.FirstOrDefault(x => x.Type == "lastName")?.Value;
                userContext.Email = claims.FirstOrDefault(x => x.Type == "email")?.Value;
                
                // Set tenant ID from claims or headers
                userContext.TenantId = claims.FirstOrDefault(x => x.Type == "tenantId")?.Value ?? 
                                     context.HttpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault() ?? "1401";
                
                var tenants = claims.FirstOrDefault(x => x.Type == "tenants")?.Value;
                if (!string.IsNullOrWhiteSpace(tenants))
                {
                    try
                    {
                        userContext.Tenants = JsonConvert.DeserializeObject<Dictionary<string, string>>(tenants);
                    }
                    catch
                    {
                        userContext.Tenants = new Dictionary<string, string>();
                    }
                }

                userContext.CompanyId = "";
                userContext.Schema = AuthSchemes.Identification;
                userContext.AppCode = context.HttpContext.Request.Headers["X-App-Code"].FirstOrDefault() ?? "DEFAULT";
            }
            catch
            {
                // Instead of failing completely, we'll try to set minimal user context from JWT claims
                try
                {
                    userContext.UserId = claims!.FirstOrDefault(x => x.Type == "userId")?.Value ?? 
                                       claims.FirstOrDefault(x => x.Type == "sub")?.Value ?? "unknown";
                    userContext.TenantId = claims.FirstOrDefault(x => x.Type == "tenantId")?.Value ?? 
                                         context.HttpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault() ?? "1401";
                    userContext.UserName = claims.FirstOrDefault(x => x.Type == "userName")?.Value ?? "unknown";
                    userContext.Email = claims.FirstOrDefault(x => x.Type == "email")?.Value ?? "";
                    userContext.AppCode = context.HttpContext.Request.Headers["X-App-Code"].FirstOrDefault() ?? "DEFAULT";
                    userContext.Schema = AuthSchemes.Identification;
                }
                catch
                {
                    context.Fail("Authentication failed");
                }
            }
        }
    }
}
