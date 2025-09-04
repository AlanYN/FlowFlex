using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.IServices.OW.ChangeLog;
using FlowFlex.Application.Services.OW;
using Microsoft.Extensions.DependencyInjection;

namespace FlowFlex.WebApi.Controllers.OW.ChangeLog
{
    /// <summary>
    /// Service collection extensions for operation log services
    /// </summary>
    public static class OperationLogServiceExtensions
    {
        /// <summary>
        /// Add operation log services to dependency injection container
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddOperationLogServices(this IServiceCollection services)
        {
            // Core services
            services.AddScoped<ILogCacheService, FlowFlex.Application.Services.OW.ChangeLog.LogCacheService>();
            services.AddScoped<ILogAggregationService, FlowFlex.Application.Services.OW.ChangeLog.LogAggregationService>();

            // Specialized business services
            services.AddScoped<IWorkflowLogService, FlowFlex.Application.Services.OW.ChangeLog.WorkflowLogService>();
            services.AddScoped<IStageLogService, FlowFlex.Application.Services.OW.ChangeLog.StageLogService>();
            services.AddScoped<IChecklistLogService, FlowFlex.Application.Services.OW.ChangeLog.ChecklistLogService>();
            services.AddScoped<IQuestionnaireLogService, FlowFlex.Application.Services.OW.ChangeLog.QuestionnaireLogService>();
            services.AddScoped<IActionLogService, FlowFlex.Application.Services.OW.ChangeLog.ActionLogService>();

            // Legacy service adapter for backward compatibility
            services.AddScoped<IOperationChangeLogService, FlowFlex.Application.Services.OW.ChangeLog.OperationChangeLogServiceLegacyAdapter>();

            return services;
        }

        /// <summary>
        /// Add operation log services with custom cache configuration
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="cacheConfiguration">Cache configuration action</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddOperationLogServices(
            this IServiceCollection services,
            Action<OperationLogCacheOptions> cacheConfiguration)
        {
            // Configure cache options
            services.Configure(cacheConfiguration);

            // Add the services
            return services.AddOperationLogServices();
        }
    }

    /// <summary>
    /// Operation log cache configuration options
    /// </summary>
    public class OperationLogCacheOptions
    {
        /// <summary>
        /// Default cache expiration for logs (in minutes)
        /// </summary>
        public int DefaultLogsExpirationMinutes { get; set; } = 15;

        /// <summary>
        /// Default cache expiration for statistics (in minutes)
        /// </summary>
        public int DefaultStatsExpirationMinutes { get; set; } = 30;

        /// <summary>
        /// Long-term cache expiration (in hours)
        /// </summary>
        public int LongTermExpirationHours { get; set; } = 2;

        /// <summary>
        /// Enable cache warm-up on application start
        /// </summary>
        public bool EnableCacheWarmUp { get; set; } = true;

        /// <summary>
        /// Maximum number of items to cache
        /// </summary>
        public int MaxCacheItems { get; set; } = 10000;

        /// <summary>
        /// Enable cache statistics tracking
        /// </summary>
        public bool EnableCacheStatistics { get; set; } = true;

        /// <summary>
        /// Cache key prefix for operation logs
        /// </summary>
        public string CacheKeyPrefix { get; set; } = "operation_logs:";

        /// <summary>
        /// Enable automatic cache invalidation
        /// </summary>
        public bool EnableAutomaticInvalidation { get; set; } = true;
    }
}