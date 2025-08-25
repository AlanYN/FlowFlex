using SqlSugar;
using System.Linq;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Database migration manager
    /// </summary>
    public class MigrationManager
    {
        private readonly ISqlSugarClient _db;
        private readonly bool _verboseLogging;

        public MigrationManager(ISqlSugarClient db, bool verboseLogging = false)
        {
            _db = db;
            _verboseLogging = verboseLogging;
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
                    ("20250101000002_AddAssignmentsJsonColumn", (Action)(() => Migration_20250101000002_AddAssignmentsJsonColumn.Up(_db))),
                    ("20250101000003_RemoveWorkflowStageColumns", (Action)(() => Migration_20250101000003_RemoveWorkflowStageColumns.Up(_db))),
                    ("20250101000004_AddQuestionnaireAssignmentsJsonColumn", (Action)(() => Migration_20250101000004_AddQuestionnaireAssignmentsJsonColumn.Up(_db))),
                    ("20250101000005_RemoveQuestionnaireWorkflowStageColumns", (Action)(() => Migration_20250101000005_RemoveQuestionnaireWorkflowStageColumns.Up(_db))),
                    ("20250101000006_CreateEventsTable", (Action)(() => CreateEventsTable_20250101000006.Up(_db))),
                    ("20250101000007_AddStageComponentsField", (Action)(() => Migration_20250101000007_AddStageComponentsField.Up(_db))),
                    ("20250101000008_CreateUserInvitationsTable", (Action)(() => CreateUserInvitationsTable_20250101000008.Up(_db))),
                    ("20250101000009_AddShortUrlIdToUserInvitations", (Action)(() => AddShortUrlIdToUserInvitations_20250101000009.Up(_db))),
                    ("20250101000010_AddStageIdToChecklistTaskCompletion", (Action)(() => AddStageIdToChecklistTaskCompletion_20250101000009.Up(_db))),
                    ("20241219000001_AddAppCodeColumn", (Action)(() => AddAppCodeColumnMigration.Execute(_db))),
                    ("20241219000002_AddAppCodeColumnSafe", (Action)(() => AddAppCodeColumnSafeMigration.Execute(_db))),
                    ("20250101000012_CreateAccessTokenTable", (Action)(() => CreateAccessTokenTable_20250101000010.Up(_db))),
                    ("20250101000013_AddEncryptedAccessTokenField", (Action)(() => AddEncryptedAccessTokenField_20250101000012.Up(_db))),
                    ("20250101000014_MakeTokenExpiryNullable", (Action)(() => MakeTokenExpiryNullable_20250101000013.Up(_db))),
                    ("20250101000015_AddAppCodeToEvents", (Action)(() => AddAppCodeToEvents_20250101000014.Up(_db))),
                    ("20250101000016_AddPortalVisibilityAndAttachmentFields", (Action)(() => AddPortalVisibilityAndAttachmentFields_20250101000015.Up(_db))),
                    ("20250102000017_UpdateStagesProgressWithPortalFields", (Action)(() => UpdateStagesProgressWithPortalFields_20250102000016.Up(_db))),
                    ("20250102000018_FixEventsNextRetryAtColumn", (Action)(() => FixEventsNextRetryAtColumn_20250102000017.Up(_db))),
                    ("20250103000001_SimplifyStagesProgressStructure", (Action)(() => SimplifyStagesProgressStructure_20250103000001.Up(_db))),
                    ("20250103000002_ConvertStagesProgressToJsonb", (Action)(() => ConvertStagesProgressToJsonb_20250103000002.Up(_db))),
                    ("20250103000004_ConvertStageComponentsToJsonb", (Action)(() => ConvertStageComponentsToJsonb_20250103000004.Up(_db))),
                    ("20250103000005_ConvertTextJsonColumnsToJsonb", (Action)(() => ConvertTextJsonColumnsToJsonb_20250103000005.Up(_db))),
                    ("20250103000003_AddCurrentSectionIndexToQuestionnaireAnswers", (Action)(() => AddCurrentSectionIndexToQuestionnaireAnswers_20250103000003.Up(_db))),
                    ("20250801000001_AddUserAIModelConfig", (Action)(() => AddUserAIModelConfig_20250801000001.Up(_db))),
                    ("20250801000002_DropQuestionnaireSectionTable", (Action)(() => DropQuestionnaireSectionTable_20250801000002.Up(_db))),
                    ("20250102000001_IncreaseAIModelConfigFieldLengths", (Action)(() => IncreaseAIModelConfigFieldLengths_20250102000001.Up(_db))),
                    ("20250101000003_AddIsAIGeneratedColumn", (Action)(() => Migration_20250101000003_AddIsAIGeneratedColumn.Up(_db))),
                    ("20250120000001_AddAISummaryFieldsToStage", (Action)(() => AddAISummaryFieldsToStage_20250120000001.Up(_db))),
                    ("20250122000001_RemoveAssignmentsJsonColumns", (Action)(() => Migration_20250122000001_RemoveAssignmentsJsonColumns.Up(_db))),
                    ("20250122000002_AddGinIndexForStageComponentsJson", (Action)(() => Migration_20250122000002_AddGinIndexForStageComponentsJson.Up(_db))),
                    ("20250122000003_CreateComponentMappingTables", (Action)(() => Migration_20250122000003_CreateComponentMappingTables.Up(_db))),
                    ("20250122000015_ConvertOwEntityBaseTablesToSnowflakeId", (Action)(() => Migration_20250122000015_ConvertOwEntityBaseTablesToSnowflakeId.Up(_db))),
                    ("20250122000020_AddTeamColumnToUsers", (Action)(() => Migration_20250122000020_AddTeamColumnToUsers.Up(_db))),
                    ("20250122000021_AddAssigneeJsonToChecklistTask", (Action)(() => Migration_20250122000021_AddAssigneeJsonToChecklistTask.Up(_db))),
                    ("20250122000022_FixAssigneeJsonEncoding", (Action)(() => Migration_20250122000022_FixAssigneeJsonEncoding.Up(_db))),
                    ("20250122000023_AddPortalPermissionToStage", (Action)(() => Migration_20250122000023_AddPortalPermissionToStage.Up(_db))),
                    ("20250122000024_AddActionFieldsToChecklistTask", (Action)(() => _20250122000024_AddActionFieldsToChecklistTask.Up(_db))),
                    ("20250125000001_CreateAIPromptHistoryTable", (Action)(() => CreateAIPromptHistoryTable_20250125000001.Up(_db)))
                };

                // Pre-check all migrations to reduce individual SQL queries
                var migrationStatuses = PreCheckMigrationStatuses(migrations.Select(m => m.Item1).ToArray());

                var failedMigrations = new List<string>();
                var successfulMigrations = new List<string>();
                var skippedMigrations = new List<string>();
                var executedMigrations = new List<string>();

                foreach (var (migrationId, migrationAction) in migrations)
                {
                    try
                    {
                        var wasExecuted = RunMigration(migrationId, migrationAction, migrationStatuses);
                        if (wasExecuted)
                        {
                            executedMigrations.Add(migrationId);
                        }
                        else
                        {
                            skippedMigrations.Add(migrationId);
                        }
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

                // Summary log
                Console.WriteLine($"[MigrationManager] Migration summary - Total: {migrations.Length}, " +
                                $"Executed: {executedMigrations.Count}, " +
                                $"Skipped: {skippedMigrations.Count}, " +
                                $"Failed: {failedMigrations.Count}");

                if (executedMigrations.Any())
                {
                    Console.WriteLine($"[MigrationManager] Newly executed migrations: {string.Join(", ", executedMigrations)}");
                }

                if (failedMigrations.Any())
                {
                    Console.WriteLine($"[MigrationManager] Failed migrations: {string.Join(", ", failedMigrations)}");
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
        /// Pre-check migration statuses to reduce individual SQL queries
        /// </summary>
        private Dictionary<string, bool> PreCheckMigrationStatuses(string[] migrationIds)
        {
            var statuses = new Dictionary<string, bool>();
            
            try
            {
                // Query all migration statuses in one go using IN clause (more compatible)
                var migrationIdList = string.Join(",", migrationIds.Select(id => $"'{id}'"));
                var sql = $"SELECT migration_id FROM __migration_history WHERE migration_id IN ({migrationIdList})";
                var executedMigrations = _db.Ado.SqlQuery<string>(sql);
                
                foreach (var migrationId in migrationIds)
                {
                    statuses[migrationId] = executedMigrations.Contains(migrationId);
                }
            }
            catch
            {
                // Fallback to individual checks if bulk query fails
                foreach (var migrationId in migrationIds)
                {
                    statuses[migrationId] = false; // Assume not executed
                }
            }
            
            return statuses;
        }

        /// <summary>
        /// Execute single migration
        /// </summary>
        /// <returns>True if migration was executed, false if skipped</returns>
        private bool RunMigration(string migrationId, Action migrationAction, Dictionary<string, bool> migrationStatuses)
        {
            try
            {
                // Use pre-checked status to avoid individual SQL queries
                var exists = migrationStatuses.ContainsKey(migrationId) && migrationStatuses[migrationId];

                if (!exists)
                {
                    if (_verboseLogging)
                {
                    Console.WriteLine($"[MigrationManager] Executing migration: {migrationId}");
                    }

                    // Execute migration
                    migrationAction();

                    // Record migration history
                    _db.Ado.ExecuteCommand($"INSERT INTO __migration_history (migration_id) VALUES ('{migrationId}')");

                    if (_verboseLogging)
                    {
                    Console.WriteLine($"[MigrationManager] Migration {migrationId} completed successfully");
                    }
                    return true;
                }
                else
                {
                    // Don't log each skipped migration to reduce noise
                    return false;
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
                Console.WriteLine("[MigrationManager] Starting migration rollback...");
                
                // 按照逆序回滚迁移（最新的先回滚）
                CreateAIPromptHistoryTable_20250125000001.Down(_db);
                Console.WriteLine("[MigrationManager] Rolled back CreateAIPromptHistoryTable_20250125000001");
                
                IncreaseAIModelConfigFieldLengths_20250102000001.Down(_db);
                Console.WriteLine("[MigrationManager] Rolled back IncreaseAIModelConfigFieldLengths_20250102000001");
                
                AddUserAIModelConfig_20250801000001.Down(_db);
                Console.WriteLine("[MigrationManager] Rolled back AddUserAIModelConfig_20250801000001");
                
                // 回滚其他迁移
                // SeedDemoData_20250101000002.Down(_db);
                InitialCreate_20250101000000.Down(_db);
                Console.WriteLine("[MigrationManager] Rolled back InitialCreate_20250101000000");

                // Clear migration history
                _db.Ado.ExecuteCommand("DELETE FROM __migration_history");
                Console.WriteLine("[MigrationManager] Cleared migration history");
                
                Console.WriteLine("[MigrationManager] Migration rollback completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MigrationManager] Error during migration rollback: {ex.Message}");
                throw;
            }
        }
    }
}
