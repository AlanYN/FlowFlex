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
                    userContext.AppCode = context.HttpContext.Request.Headers["X-App-Code"].FirstOrDefault() ?? "DEFAULT";
                    
                    // Load user teams from IDM
                    await LoadUserTeamsAsync(context, userContext);
                }
            }
            catch
            {
                context.Fail("Version mismatch");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <exception cref="NullReferenceException"></exception>
        public static async Task OnIamItemTokenValidated(TokenValidatedContext context)
        {
            await Task.CompletedTask;
            var principal = context.Principal ?? throw new NullReferenceException(nameof(context.Principal));
            var claims = principal.Claims.ToList();
            var userContext = context.HttpContext.RequestServices.GetService<UserContext>() ??
                              throw new NullReferenceException(nameof(UserContext));

            var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();

            var tokenType = claims!.FirstOrDefault(x => x.Type == "grant_type")?.Value;
            try
            {
                var identityHubClient = context.HttpContext.RequestServices.GetService<IdentityHubClient>();
                switch (tokenType)
                {
                    case "password":
                    case "authorization_code":
                    case "refresh_token":
                        userContext.Schema = AuthSchemes.ItemIamIdentification;
                        var authorization = context.HttpContext.Request.Headers.Authorization;
                        userContext.IamToken = authorization;
                        var userInfo = await identityHubClient.UserInfoAsync(authorization);
                        var validatedUserExtensionResult = await identityHubClient.UserExtensionAsync(authorization);
                        if (!validatedUserExtensionResult!.Data!.IsVersionMatched)
                        {
                            context.Fail("Version mismatch");
                            return;
                        }

                        userContext.RoleIds = validatedUserExtensionResult.Data.RoleIds;
                        userContext.ValidationVersion = claims!.FirstOrDefault(x => x.Type == "jti")?.Value;
                        userContext.UserId = userInfo.UserId;
                        userContext.UserName = userInfo.UserName;
                        userContext.LastName = userInfo.LastName;
                        userContext.FirstName = userInfo.FirstName;
                        userContext.Email = userInfo.Email;
                        var iamClientId = claims!.FirstOrDefault(x => x.Type == "client_id")?.Value;
                        foreach (var item in userInfo.Tenants)
                        {
                            userContext.Tenants.Add(item.Key.ToString(), item.Value);
                        }
                        if (context.Request.Headers.ContainsKey("X-Tenant-Id"))
                        {
                            var headerTenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
                            userContext.TenantId = headerTenantId;
                            userContext.CompanyId = headerTenantId;
                        }
                        else
                        {
                            userContext.TenantId = userInfo.TenantId.ToString();
                            userContext.CompanyId = userInfo.TenantId.ToString();
                        }
                        userContext.AppCode = context.HttpContext.Request.Headers["X-App-Code"].FirstOrDefault() ?? "DEFAULT";
                        
                        // Load user teams from IDM for password/authorization_code/refresh_token
                        await LoadUserTeamsAsync(context, userContext);
                        break;
                    case "client_credentials":
                        userContext.Schema = AuthSchemes.ItemIamClientIdentification;
                        if (context.Request.Headers.ContainsKey("X-Tenant-Id"))
                        {
                            var headerTenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
                            userContext.TenantId = headerTenantId;
                            userContext.CompanyId = headerTenantId;
                        }
                        userContext.AppCode = context.HttpContext.Request.Headers["X-App-Code"].FirstOrDefault() ?? "DEFAULT";
                        break;
                }
            }
            catch
            {
                context.Fail("Version mismatch");
            }
        }

        /// <summary>
        /// Load user teams from IDM and populate UserContext.UserTeams
        /// </summary>
        private static async Task LoadUserTeamsAsync(TokenValidatedContext context, UserContext userContext)
        {
            try
            {
                // Get IdmUserDataClient to fetch team information
                var idmUserDataClient = context.HttpContext.RequestServices.GetService<FlowFlex.Application.Services.OW.IdmUserDataClient>();
                if (idmUserDataClient == null)
                {
                    // Log warning but don't fail authentication
                    Console.WriteLine($"[TokenValidatedHandler] IdmUserDataClient not available, UserTeams will not be populated for user {userContext.UserId}");
                    return;
                }

                // Get all team users for the tenant
                var teamUsersResponse = await idmUserDataClient.GetAllTeamUsersAsync(userContext.TenantId, 10000, 1);
                if (teamUsersResponse == null || !teamUsersResponse.Any())
                {
                    Console.WriteLine($"[TokenValidatedHandler] No team users found for tenant {userContext.TenantId}");
                    return;
                }

                // Filter team users by current user name
                // Note: IdmTeamUserDto uses UserName (string) not UserId
                var userTeamRelations = teamUsersResponse
                    .Where(tu => tu.UserName == userContext.UserName)
                    .ToList();

                if (!userTeamRelations.Any())
                {
                    Console.WriteLine($"[TokenValidatedHandler] User {userContext.UserId} is not assigned to any teams");
                    return;
                }

                // Get all teams to build the hierarchy
                var teamsResponse = await idmUserDataClient.GetAllTeamsAsync(userContext.TenantId, 10000, 1);
                if (teamsResponse?.Data == null || !teamsResponse.Data.Any())
                {
                    Console.WriteLine($"[TokenValidatedHandler] No teams found for tenant {userContext.TenantId}");
                    return;
                }

                // Build team hierarchy for the user
                var userTeamIds = userTeamRelations.Select(tu => tu.TeamId).ToHashSet();
                var allTeams = teamsResponse.Data;

                // Find user's primary team (first team or team with no parent)
                var primaryTeam = userTeamRelations.FirstOrDefault();
                if (primaryTeam != null && long.TryParse(primaryTeam.TeamId, out var primaryTeamId))
                {
                    userContext.UserTeams = new UserTeamModel
                    {
                        TeamId = primaryTeamId,
                        SubTeam = new List<UserTeamModel>()
                    };

                    // Add other teams as sub-teams (simplified hierarchy)
                    foreach (var teamRelation in userTeamRelations.Skip(1))
                    {
                        if (long.TryParse(teamRelation.TeamId, out var teamId))
                        {
                            userContext.UserTeams.SubTeam.Add(new UserTeamModel
                            {
                                TeamId = teamId,
                                SubTeam = new List<UserTeamModel>()
                            });
                        }
                    }

                    Console.WriteLine($"[TokenValidatedHandler] Loaded {userTeamRelations.Count} teams for user {userContext.UserId}");
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail authentication
                Console.WriteLine($"[TokenValidatedHandler] Error loading user teams: {ex.Message}");
                Console.WriteLine($"[TokenValidatedHandler] Stack trace: {ex.StackTrace}");
            }
        }
    }
}
