using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add assignee_json column to ff_checklist_task table
    /// </summary>
    public class Migration_20250122000021_AddAssigneeJsonToChecklistTask
    {
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("[AddAssigneeJsonToChecklistTask] Starting migration to add assignee_json column...");

                // Check if assignee_json column already exists
                var columnExists = db.Ado.GetDataTable(@"
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_name = 'ff_checklist_task' AND column_name = 'assignee_json'
                ").Rows.Count > 0;

                if (!columnExists)
                {
                    Console.WriteLine("[AddAssigneeJsonToChecklistTask] Adding assignee_json column to ff_checklist_task table...");
                    
                    // Add assignee_json column
                    db.Ado.ExecuteCommand(@"
                        ALTER TABLE ff_checklist_task 
                        ADD COLUMN assignee_json JSONB NULL;
                    ");

                    Console.WriteLine("[AddAssigneeJsonToChecklistTask] assignee_json column added successfully");
                }
                else
                {
                    Console.WriteLine("[AddAssigneeJsonToChecklistTask] assignee_json column already exists, skipping...");
                }

                // Create GIN index on assignee_json column for better query performance
                try
                {
                    db.Ado.ExecuteCommand(@"
                        CREATE INDEX IF NOT EXISTS idx_checklist_task_assignee_json 
                        ON ff_checklist_task USING gin(assignee_json);
                    ");
                    Console.WriteLine("[AddAssigneeJsonToChecklistTask] GIN index on assignee_json column created successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AddAssigneeJsonToChecklistTask] Warning: Could not create GIN index on assignee_json column: {ex.Message}");
                }

                // Migrate existing assignee data to the new JSON column
                try
                {
                    Console.WriteLine("[AddAssigneeJsonToChecklistTask] Migrating existing assignee data...");
                    
                    db.Ado.ExecuteCommand(@"
                        UPDATE ff_checklist_task 
                        SET assignee_json = json_build_object(
                            'userId', assignee_id,
                            'name', assignee_name,
                            'team', assigned_team,
                            'type', CASE WHEN assignee_id IS NOT NULL THEN 'User' ELSE 'Team' END,
                            'source', 'Migration'
                        )
                        WHERE (assignee_id IS NOT NULL OR assignee_name IS NOT NULL OR assigned_team IS NOT NULL)
                        AND assignee_json IS NULL;
                    ");
                    
                    Console.WriteLine("[AddAssigneeJsonToChecklistTask] Existing assignee data migrated successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AddAssigneeJsonToChecklistTask] Warning: Could not migrate existing assignee data: {ex.Message}");
                }

                Console.WriteLine("[AddAssigneeJsonToChecklistTask] Migration completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AddAssigneeJsonToChecklistTask] Migration failed: {ex.Message}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("[AddAssigneeJsonToChecklistTask] Rolling back assignee_json column migration...");

                // Drop index first
                try
                {
                    db.Ado.ExecuteCommand(@"DROP INDEX IF EXISTS idx_checklist_task_assignee_json;");
                    Console.WriteLine("[AddAssigneeJsonToChecklistTask] GIN index dropped successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AddAssigneeJsonToChecklistTask] Warning: Could not drop GIN index: {ex.Message}");
                }

                // Check if assignee_json column exists
                var columnExists = db.Ado.GetDataTable(@"
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_name = 'ff_checklist_task' AND column_name = 'assignee_json'
                ").Rows.Count > 0;

                if (columnExists)
                {
                    // Drop assignee_json column
                    db.Ado.ExecuteCommand(@"
                        ALTER TABLE ff_checklist_task 
                        DROP COLUMN assignee_json;
                    ");
                    Console.WriteLine("[AddAssigneeJsonToChecklistTask] assignee_json column dropped successfully");
                }

                Console.WriteLine("[AddAssigneeJsonToChecklistTask] Rollback completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AddAssigneeJsonToChecklistTask] Rollback failed: {ex.Message}");
                throw;
            }
        }
    }
}