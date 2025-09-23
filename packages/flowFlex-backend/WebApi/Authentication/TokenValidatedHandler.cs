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
                if (tokenCategory != null)
                {
                    var authorization = context.HttpContext.Request.Headers.Authorization;
                    userContext.IamToken = authorization;
                    var userExtensionResult = await identityHubServerClient.UserExtensionAsync(authorization);
                    if (!userExtensionResult!.Data.IsVersionMatched)
                    {
                        context.Fail("Version mismatch");
                        return;
                    }
                    userContext.TenantId = userExtensionResult.Data.TenantId;
                    userContext.ClientShortName = userExtensionResult.Data.ClientShortName;
                    userContext.RoleIds = userExtensionResult.Data.RoleIds;

                    userContext.UserId = claims!.First(x => x.Type == "userId").Value;
                    userContext.ValidationVersion = claims.First(x => x.Type == "version").Value;
                    userContext.UserName = claims.First(x => x.Type == "userName").Value;
                    userContext.FirstName = claims.FirstOrDefault(x => x.Type == "firstName")?.Value;
                    userContext.LastName = claims.FirstOrDefault(x => x.Type == "lastName")?.Value;
                    userContext.Email = claims.FirstOrDefault(x => x.Type == "email")?.Value;
                    var tenants = claims.FirstOrDefault(x => x.Type == "tenants")?.Value;
                    if (!string.IsNullOrWhiteSpace(tenants))
                    {
                        userContext.Tenants = JsonConvert.DeserializeObject<Dictionary<string, string>>(tenants);
                    }

                    var tokenThirdParty = userExtensionResult.Data.BnpToken;
                    if (!string.IsNullOrWhiteSpace(tokenThirdParty))
                    {
                        userContext.ThirdPartyToken = tokenThirdParty;
                    }

                    userContext.CompanyId = "";
                    userContext.Schema = AuthSchemes.Identification;
                }
            }
            catch
            {
                context.Fail("Version mismatch");
            }
        }
    }
}
