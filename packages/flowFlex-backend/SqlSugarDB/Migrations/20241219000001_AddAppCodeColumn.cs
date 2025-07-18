using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add app_code column to all tables for application isolation
    /// </summary>
    public static class AddAppCodeColumnMigration
    {
        /// <summary>
        /// Execute migration
        /// </summary>
        /// <param name="db">SqlSugar client</param>
        public static void Execute(ISqlSugarClient db)
        {
            try
            {
                var tableNames = new string[]
                {
                    "ff_users", "ff_workflow", "ff_workflow_version", "ff_stage", "ff_stage_version",
                    "ff_onboarding", "ff_questionnaire", "ff_questionnaire_section", "ff_questionnaire_answers",
                    "ff_checklist", "ff_checklist_task", "ff_checklist_task_completion", "ff_internal_notes",
                    "ff_onboarding_file", "ff_operation_change_log", "ff_static_field_values", "ff_stage_completion_log"
                };

                foreach (var tableName in tableNames)
                {
                    AddAppCodeColumnToTable(db, tableName);
                }

                // Create indexes for better performance
                CreateIndexes(db, tableNames);

                // Add comments for app_code columns
                AddComments(db, tableNames);

                Console.WriteLine("App code columns added successfully to all tables");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding app_code columns: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Add app_code column to specific table
        /// </summary>
        private static void AddAppCodeColumnToTable(ISqlSugarClient db, string tableName)
        {
            try
            {
                if (db.DbMaintenance.IsAnyTable(tableName, false))
                {
                    // Check if column already exists
                    var columns = db.DbMaintenance.GetColumnInfosByTableName(tableName, false);
                    if (!columns.Any(c => c.DbColumnName.ToLower() == "app_code"))
                    {
                        db.Ado.ExecuteCommand($@"
                            ALTER TABLE {tableName} 
                            ADD COLUMN app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT';
                        ");
                        Console.WriteLine($"Added app_code column to {tableName}");
                    }
                    else
                    {
                        Console.WriteLine($"app_code column already exists in {tableName}");
                    }
                }
                else
                {
                    Console.WriteLine($"Table {tableName} does not exist, skipping...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding app_code column to {tableName}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Create indexes for app_code and tenant_id columns
        /// </summary>
        private static void CreateIndexes(ISqlSugarClient db, string[] tableNames)
        {
            foreach (var tableName in tableNames)
            {
                try
                {
                    if (db.DbMaintenance.IsAnyTable(tableName, false))
                    {
                        var indexName = $"idx_{tableName}_app_code_tenant_id";
                        db.Ado.ExecuteCommand($@"
                            CREATE INDEX IF NOT EXISTS {indexName} ON {tableName}(app_code, tenant_id);
                        ");
                        Console.WriteLine($"Created index {indexName}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating index for {tableName}: {ex.Message}");
                    // Don't throw here, continue with other tables
                }
            }
        }

        /// <summary>
        /// Add comments for app_code columns
        /// </summary>
        private static void AddComments(ISqlSugarClient db, string[] tableNames)
        {
            foreach (var tableName in tableNames)
            {
                try
                {
                    if (db.DbMaintenance.IsAnyTable(tableName, false))
                    {
                        db.Ado.ExecuteCommand($@"
                            COMMENT ON COLUMN {tableName}.app_code IS 'Application code for application isolation';
                        ");
                        Console.WriteLine($"Added comment to {tableName}.app_code");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error adding comment to {tableName}.app_code: {ex.Message}");
                    // Don't throw here, continue with other tables
                }
            }
        }
    }
} 