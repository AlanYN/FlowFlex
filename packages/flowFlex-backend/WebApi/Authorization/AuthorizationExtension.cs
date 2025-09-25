using Item.Internal.Auth.Authorization;
using Item.Internal.Auth.Authorization.BnpToken;
using Item.Internal.Auth.Authorization.BnpToken.Services;
using Item.Internal.Auth.Authorization.BnpToken.Services.Implements;
using Microsoft.AspNetCore.Authorization;

namespace WebApi.Authorization;

/// <summary>
/// 
/// </summary>
public static class AuthorizationExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="authenticationSchemes"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddAuthorization<T>(this IServiceCollection services,
        params string[] authenticationSchemes) where T : AbstractAuthorizationHandler
    {
        services.AddScoped<IAuthorizationHandler, T>();
        services.AddSingleton<IBnpTokenService, BnpTokenService>();
        services.AddAuthorization(options =>
        {
            var defaultPolicyBuilder =
                new AuthorizationPolicyBuilder(authenticationSchemes).RequireAuthenticatedUser()
                    .AddRequirements(new UserRequirement());

            options.DefaultPolicy = defaultPolicyBuilder.Build();

            options.AddPolicy(AuthorizePolicy.Default,
                policy => { policy.Requirements.Add(new PermissionRequirement()); });

            options.AddPolicy(BnpTokenPolicy.POLICY_PREFIX,
                policy => { policy.Requirements.Add(new CustomRequirement()); });
        });

        return services;
    }
}
