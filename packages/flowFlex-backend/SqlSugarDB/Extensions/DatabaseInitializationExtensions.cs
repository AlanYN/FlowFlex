using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FlowFlex.SqlSugarDB.Migrations;
using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Extensions
{
    /// <summary>
    /// Database initialization extension methods
    /// </summary>
    public static class DatabaseInitializationExtensions
    {
        /// <summary>
        /// Initialize database
        /// </summary>
        /// <param name="serviceProvider">Service provider</param>
        public static void InitializeDatabase(this IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetService<ILogger<MigrationManager>>();
            
            try
            {
                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

                logger?.LogInformation("Starting database initialization...");
                // Use non-verbose logging for cleaner startup output
                var migrationManager = new MigrationManager(db, verboseLogging: false);
                migrationManager.RunMigrations();
                logger?.LogInformation("Database initialization completed successfully");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Database initialization failed");
                throw;
            }
        }

        /// <summary>
        /// Ensure database is created
        /// </summary>
        /// <param name="db">SqlSugar client</param>
        public static void EnsureDatabaseCreated(this ISqlSugarClient db)
        {
            try
            {
                // Test database connection
                db.Ado.CheckConnection();

                // Run migrations
                var migrationManager = new MigrationManager(db, verboseLogging: false);
                migrationManager.RunMigrations();
                // Debug logging handled by structured logging
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                throw;
            }
        }

        /// <summary>
        /// Reset database
        /// </summary>
        /// <param name="db">SqlSugar client</param>
        public static void ResetDatabase(this ISqlSugarClient db)
        {
            var migrationManager = new MigrationManager(db, verboseLogging: true); // Use verbose for reset operations
            migrationManager.RollbackMigrations();
            migrationManager.RunMigrations();
        }
    }
}
