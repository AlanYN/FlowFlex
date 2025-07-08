using Microsoft.Extensions.DependencyInjection;
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
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var migrationManager = new MigrationManager(db);
            migrationManager.RunMigrations();
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
                var migrationManager = new MigrationManager(db);
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
            var migrationManager = new MigrationManager(db);
            migrationManager.RollbackMigrations();
            migrationManager.RunMigrations();
        }
    }
}
