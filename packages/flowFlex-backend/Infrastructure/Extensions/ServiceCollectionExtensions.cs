using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using FlowFlex.Infrastructure.Configuration;
using FlowFlex.Infrastructure.Services.Logging;
using FlowFlex.Application.Contracts.Options;
using FlowFlex.Infrastructure.Exceptions;
using FlowFlex.Infrastructure.Data;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Infrastructure.Services.Security;

namespace FlowFlex.Infrastructure.Extensions
{
    /// <summary>
    /// Infrastructure service registration extensions
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Register infrastructure services
        /// </summary>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Register configuration options
            services.AddOptions<DatabaseOptions>()
                .Bind(configuration.GetSection(DatabaseOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddOptions<SecurityOptions>()
                .Bind(configuration.GetSection(SecurityOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddOptions<AIOptions>()
                .Bind(configuration.GetSection(AIOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddOptions<MCPOptions>()
                .Bind(configuration.GetSection(MCPOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // Register logging services
            services.AddScoped<IApplicationLogger, ApplicationLogger>();

            // Register encryption services
            services.AddScoped<IEncryptionService, EncryptionService>();

            // Register repository services
            services.AddScoped(typeof(IOptimizedRepository<>), typeof(OptimizedRepository<>));

            // Register HttpClient for AI services
            services.AddHttpClient();

            // Register AI services (will be auto-registered via IScopedService interface)
            // services.AddScoped<IAIService, AIService>();
            // services.AddScoped<IMCPService, MCPService>();

            // Register database migration service
            services.AddDatabaseMigration();

            return services;
        }

        /// <summary>
        /// Add global exception handling middleware
        /// </summary>
        public static IServiceCollection AddGlobalExceptionHandling(this IServiceCollection services)
        {
            // Middleware does not need to be registered as a service, it can be used directly in the pipeline
            return services;
        }
    }
}