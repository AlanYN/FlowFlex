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
                RunMigration("20250101000002_AddAssignmentsJsonColumn", () => Migration_20250101000002_AddAssignmentsJsonColumn.Up(_db));
                RunMigration("20250101000003_RemoveWorkflowStageColumns", () => Migration_20250101000003_RemoveWorkflowStageColumns.Up(_db));
                RunMigration("20250101000004_AddQuestionnaireAssignmentsJsonColumn", () => Migration_20250101000004_AddQuestionnaireAssignmentsJsonColumn.Up(_db));
                RunMigration("20250101000005_RemoveQuestionnaireWorkflowStageColumns", () => Migration_20250101000005_RemoveQuestionnaireWorkflowStageColumns.Up(_db));
                RunMigration("20250101000006_CreateEventsTable", () => CreateEventsTable_20250101000006.Up(_db));
                RunMigration("20250101000007_AddStageComponentsField", () => Migration_20250101000007_AddStageComponentsField.Up(_db));
                // RunMigration("20250101000004_SeedDemoData", () => SeedDemoData_20250101000004.Up(_db));
                // Debug logging handled by structured logging
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
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
                    id BIGSERIAL PRIMARY KEY,
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
                // Debug logging handled by structured logging
                // Execute migration
                migrationAction();

                // Record migration history
                _db.Ado.ExecuteCommand($"INSERT INTO __migration_history (migration_id) VALUES ('{migrationId}')");
                // Debug logging handled by structured logging
            }
            else
            {
                // Debug logging handled by structured logging
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
