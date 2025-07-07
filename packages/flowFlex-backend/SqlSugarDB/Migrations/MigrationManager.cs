using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Database migration manager
    /// </summary>
    public class MigrationManager
    {
        private readonly ISqlSugarClient _db;

        public MigrationManager(ISqlSugarClient db)
        {
            _db = db;
        }

        /// <summary>
        /// Execute all migrations
        /// </summary>
        public void RunMigrations()
        {
            try
            {
                // Create migration history table
                CreateMigrationHistoryTable();

                // Execute migrations
                RunMigration("20250101000000_InitialCreate", () => InitialCreate_20250101000000.Up(_db));
                RunMigration("20250101000001_CreateRemainingTables", () => CreateRemainingTables_20250101000001.Up(_db));
                // RunMigration("20250101000002_SeedDemoData", () => SeedDemoData_20250101000002.Up(_db));

                Console.WriteLine("All database migrations executed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Migration execution failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Create migration history table
        /// </summary>
        private void CreateMigrationHistoryTable()
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS __migration_history (
                    id BIGINT PRIMARY KEY,
                    migration_id VARCHAR(100) NOT NULL UNIQUE,
                    applied_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
                );
            ";
            
            _db.Ado.ExecuteCommand(sql);
        }

        /// <summary>
        /// Execute single migration
        /// </summary>
        private void RunMigration(string migrationId, Action migrationAction)
        {
            // Check if migration has already been executed
            var exists = _db.Ado.GetInt($"SELECT COUNT(*) FROM __migration_history WHERE migration_id = '{migrationId}'") > 0;
            
            if (!exists)
            {
                Console.WriteLine($"Executing migration: {migrationId}");
                
                // Execute migration
                migrationAction();
                
                // Record migration history
                _db.Ado.ExecuteCommand($"INSERT INTO __migration_history (migration_id) VALUES ('{migrationId}')");
                
                Console.WriteLine($"Migration {migrationId} executed successfully");
            }
            else
            {
                Console.WriteLine($"Migration {migrationId} already exists, skipping");
            }
        }

        /// <summary>
        /// Rollback migrations
        /// </summary>
        public void RollbackMigrations()
        {
            try
            {
                // SeedDemoData_20250101000002.Down(_db);
                CreateRemainingTables_20250101000001.Down(_db);
                InitialCreate_20250101000000.Down(_db);
                
                // Clear migration history
                _db.Ado.ExecuteCommand("DELETE FROM __migration_history");
                
                Console.WriteLine("All migrations have been rolled back!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Rollback failed: {ex.Message}");
                throw;
            }
        }
    }
} 
