using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FlowFlex.Infrastructure.Services;

namespace FlowFlex.Infrastructure.Extensions
{
    /// <summary>
    /// Database migration extensions
    /// </summary>
    public static class DatabaseMigrationExtensions
    {
        /// <summary>
        /// Execute database migrations asynchronously
        /// </summary>
        /// <param name="services">Service provider</param>
        /// <returns>Task</returns>
        public static async Task ExecuteDatabaseMigrationsAsync(this IServiceProvider services)
        {
            try
            {
                // Create a scope to resolve scoped services
                using var scope = services.CreateScope();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseMigrationService>>();
                var migrationService = scope.ServiceProvider.GetRequiredService<DatabaseMigrationService>();

                await migrationService.ExecuteMigrationsAsync();

                logger.LogInformation("Database migrations completed successfully");
            }
            catch (Exception ex)
            {
                // Create a scope to get logger
                using var scope = services.CreateScope();
                var logger = scope.ServiceProvider.GetService<ILogger<DatabaseMigrationService>>();
                logger?.LogError(ex, "Failed to execute database migrations");
                throw;
            }
        }

        /// <summary>
        /// Register database migration service
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddDatabaseMigration(this IServiceCollection services)
        {
            services.AddScoped<DatabaseMigrationService>();
            return services;
        }
    }
}