using FlowFlex.Domain.Shared.Const;
using FlowFlex.Domain.Shared.Models;
using Item.Internal.Auth.Authorization;
using Item.Internal.Auth.Authorization.BnpToken.Services;
using Item.ThirdParty.IdentityHub;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace WebApi.Authorization
{
    public class WfeAuthorizationHandler : AbstractAuthorizationHandler
    {
        private readonly IdentityHubConfigOptions _options;
        private readonly UserContext _userContext;
        private readonly IdentityHubClient _client;

        public WfeAuthorizationHandler(IBnpTokenService bnpTokenService,
            IOptions<IdentityHubConfigOptions> options,
            UserContext userContext,
            IdentityHubClient client
            ) : base(bnpTokenService)
        {
            _options = options.Value;
            _userContext = userContext;
            _client = client;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestPermissions"></param>
        /// <returns></returns>
        protected override async Task<bool> CheckUserPermissionsAsync(IEnumerable<string> requestPermissions)
        {
            return await _client.UserRolePermissionCheck(_userContext.IamToken, requestPermissions.ToList());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected override async Task<bool> UserRequirementHandler(AuthorizationHandlerContext context)
        {
            await Task.CompletedTask;
            if (context.User.Identity is not null && context.User.Identity.IsAuthenticated)
            {
                var claims = context.User.Claims.ToList();
                var scheme = _userContext.Schema;
                if (scheme is not AuthSchemes.Identification)
                {
                    return true;
                }

                var tokenCategory = claims!.FirstOrDefault(x => x.Type == "token_category")?.Value;
                var userId = claims!
                    .FirstOrDefault(x => x.Type is "userId" or JwtRegisteredClaimNames.NameId)?.Value;
                if (userId == null)
                {
                    return false;
                }

                if (tokenCategory != null)
                {
                    var appId = claims.FirstOrDefault(x => x.Type == "app_id")?.Value;
                    if (appId != _options.ApplicationId)
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }
    }
}
