using SqlSugar;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add FilesJson field to ChecklistTaskCompletion table
    /// </summary>
    public class _20250125000004_AddFilesJsonToChecklistTaskCompletion
    {
        public static void Up(ISqlSugarClient db)
        {
            Console.WriteLine("[Migration] Adding files_json field to ff_checklist_task_completion table...");

            try
            {
                // Check if column exists first
                var existingColumns = db.DbMaintenance.GetColumnInfosByTableName("ff_checklist_task_completion", false);
                var columnNames = existingColumns.Select(c => c.DbColumnName.ToLower()).ToHashSet();

                // Add files_json column if not exists
                if (!columnNames.Contains("files_json"))
                {
                    db.Ado.ExecuteCommand(@"
                        ALTER TABLE ff_checklist_task_completion 
                        ADD COLUMN files_json TEXT DEFAULT '[]';
                    ");
                    Console.WriteLine("[Migration] Added files_json column to ff_checklist_task_completion");
                }
                else
                {
                    Console.WriteLine("[Migration] files_json column already exists in ff_checklist_task_completion");
                }

                // Add comment for files_json column
                try
                {
                    db.Ado.ExecuteCommand(@"
                        COMMENT ON COLUMN ff_checklist_task_completion.files_json IS 'Related files JSON (file information array as JSON string)';
                    ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Migration] Warning: Could not add comment for files_json: {ex.Message}");
                }

                // Create index on files_json for better query performance (if using PostgreSQL)
                try
                {
                    db.Ado.ExecuteCommand(@"
                        CREATE INDEX IF NOT EXISTS idx_ff_checklist_task_completion_files_json 
                        ON ff_checklist_task_completion USING GIN (files_json::jsonb) 
                        WHERE files_json IS NOT NULL AND files_json != '[]';
                    ");
                    Console.WriteLine("[Migration] Created GIN index on files_json column");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Migration] Warning: Could not create GIN index on files_json: {ex.Message}");
                    // Try to create a simple index instead
                    try
                    {
                        db.Ado.ExecuteCommand(@"
                            CREATE INDEX IF NOT EXISTS idx_ff_checklist_task_completion_files_json_simple
                            ON ff_checklist_task_completion (files_json) 
                            WHERE files_json IS NOT NULL AND files_json != '[]';
                        ");
                        Console.WriteLine("[Migration] Created simple index on files_json column");
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine($"[Migration] Warning: Could not create any index on files_json: {ex2.Message}");
                    }
                }

                Console.WriteLine("[Migration] Successfully added FilesJson field to ChecklistTaskCompletion table");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Migration] Error adding FilesJson field: {ex.Message}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            Console.WriteLine("[Migration] Rolling back FilesJson field from ff_checklist_task_completion table...");

            try
            {
                // Check if column exists before trying to drop it
                var existingColumns = db.DbMaintenance.GetColumnInfosByTableName("ff_checklist_task_completion", false);
                var columnNames = existingColumns.Select(c => c.DbColumnName.ToLower()).ToHashSet();

                // Drop indexes first
                try
                {
                    db.Ado.ExecuteCommand("DROP INDEX IF EXISTS idx_ff_checklist_task_completion_files_json;");
                    db.Ado.ExecuteCommand("DROP INDEX IF EXISTS idx_ff_checklist_task_completion_files_json_simple;");
                    Console.WriteLine("[Migration] Dropped indexes on files_json column");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Migration] Warning: Could not drop indexes: {ex.Message}");
                }

                // Drop column if exists
                if (columnNames.Contains("files_json"))
                {
                    db.Ado.ExecuteCommand(@"
                        ALTER TABLE ff_checklist_task_completion 
                        DROP COLUMN files_json;
                    ");
                    Console.WriteLine("[Migration] Dropped files_json column from ff_checklist_task_completion");
                }

                Console.WriteLine("[Migration] Successfully rolled back FilesJson field from ChecklistTaskCompletion table");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Migration] Error rolling back FilesJson field: {ex.Message}");
                throw;
            }
        }
    }
}