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
                Console.WriteLine("[MigrationManager] Starting migration execution...");

                // Create migration history table
                CreateMigrationHistoryTable();

                // Execute migrations in order
                var migrations = new[]
                {
                    ("20250101000000_InitialCreate", (Action)(() => InitialCreate_20250101000000.Up(_db))),
                    ("20250101000001_CreateRemainingTables", (Action)(() => CreateRemainingTables_20250101000001.Up(_db))),
                    ("20250101000002_AddAssignmentsJsonColumn", (Action)(() => Migration_20250101000002_AddAssignmentsJsonColumn.Up(_db))),
                    ("20250101000003_RemoveWorkflowStageColumns", (Action)(() => Migration_20250101000003_RemoveWorkflowStageColumns.Up(_db))),
                    ("20250101000004_AddQuestionnaireAssignmentsJsonColumn", (Action)(() => Migration_20250101000004_AddQuestionnaireAssignmentsJsonColumn.Up(_db))),
                    ("20250101000005_RemoveQuestionnaireWorkflowStageColumns", (Action)(() => Migration_20250101000005_RemoveQuestionnaireWorkflowStageColumns.Up(_db))),
                    ("20250101000006_CreateEventsTable", (Action)(() => CreateEventsTable_20250101000006.Up(_db))),
                    ("20250101000007_AddStageComponentsField", (Action)(() => Migration_20250101000007_AddStageComponentsField.Up(_db))),
                    ("20250101000008_CreateUserInvitationsTable", (Action)(() => CreateUserInvitationsTable_20250101000008.Up(_db))),
                    ("20250101000009_AddStageIdToChecklistTaskCompletion", (Action)(() => AddStageIdToChecklistTaskCompletion_20250101000009.Up(_db))),
                    ("20250101000011_AddStageVersionComponentsField", (Action)(() => Migration_20250101000011_AddStageVersionComponentsField.Up(_db))),
                    ("20241219000001_AddAppCodeColumn", (Action)(() => AddAppCodeColumnMigration.Execute(_db))),
                    ("20241219000002_AddAppCodeColumnSafe", (Action)(() => AddAppCodeColumnSafeMigration.Execute(_db))),
                    ("20250101000010_CreateAccessTokenTable", (Action)(() => CreateAccessTokenTable_20250101000010.Up(_db))),
                    ("20250101000012_AddEncryptedAccessTokenField", (Action)(() => AddEncryptedAccessTokenField_20250101000012.Up(_db))),
                    ("20250101000013_MakeTokenExpiryNullable", (Action)(() => MakeTokenExpiryNullable_20250101000013.Up(_db)))
                };

                var failedMigrations = new List<string>();
                var successfulMigrations = new List<string>();

                foreach (var (migrationId, migrationAction) in migrations)
                {
                    try
                    {
                        RunMigration(migrationId, migrationAction);
                        successfulMigrations.Add(migrationId);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[MigrationManager] Migration {migrationId} failed: {ex.Message}");
                        failedMigrations.Add(migrationId);

                        // Continue with next migration instead of stopping
                        continue;
                    }
                }

                Console.WriteLine($"[MigrationManager] Migration execution completed. Successful: {successfulMigrations.Count}, Failed: {failedMigrations.Count}");

                if (failedMigrations.Any())
                {
                    Console.WriteLine($"[MigrationManager] Failed migrations: {string.Join(", ", failedMigrations)}");
                }

                if (successfulMigrations.Any())
                {
                    Console.WriteLine($"[MigrationManager] Successful migrations: {string.Join(", ", successfulMigrations)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MigrationManager] Critical error during migration execution: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Create migration history table
        /// </summary>
        private void CreateMigrationHistoryTable()
        {
            try
            {
                var sql = @"
                    CREATE TABLE IF NOT EXISTS __migration_history (
                        id BIGSERIAL PRIMARY KEY,
                        migration_id VARCHAR(100) NOT NULL UNIQUE,
                        applied_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
                    );
                    
                    CREATE INDEX IF NOT EXISTS idx_migration_history_migration_id ON __migration_history(migration_id);
                ";

                _db.Ado.ExecuteCommand(sql);
                Console.WriteLine("[MigrationManager] Migration history table ensured");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MigrationManager] Error creating migration history table: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Execute single migration
        /// </summary>
        private void RunMigration(string migrationId, Action migrationAction)
        {
            try
            {
                // Check if migration has already been executed
                var exists = _db.Ado.GetInt($"SELECT COUNT(*) FROM __migration_history WHERE migration_id = '{migrationId}'") > 0;

                if (!exists)
                {
                    Console.WriteLine($"[MigrationManager] Executing migration: {migrationId}");

                    // Execute migration
                    migrationAction();

                    // Record migration history
                    _db.Ado.ExecuteCommand($"INSERT INTO __migration_history (migration_id) VALUES ('{migrationId}')");

                    Console.WriteLine($"[MigrationManager] Migration {migrationId} completed successfully");
                }
                else
                {
                    Console.WriteLine($"[MigrationManager] Migration {migrationId} already executed, skipping");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MigrationManager] Error executing migration {migrationId}: {ex.Message}");
                throw;
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
                // Debug logging handled by structured logging
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                throw;
            }
        }
    }
}
