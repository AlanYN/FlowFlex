using SqlSugar;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add ActionMappingId field to ChecklistTask table
    /// </summary>
    public class _20250125000001_AddActionMappingIdToChecklistTask
    {
        public static void Up(ISqlSugarClient db)
        {
            Console.WriteLine("[Migration] Adding ActionMappingId field to ff_checklist_task table...");

            try
            {
                // Check if column exists first
                var existingColumns = db.DbMaintenance.GetColumnInfosByTableName("ff_checklist_task", false);
                var columnNames = existingColumns.Select(c => c.DbColumnName.ToLower()).ToHashSet();

                // Add action_mapping_id column if not exists
                if (!columnNames.Contains("action_mapping_id"))
                {
                    db.Ado.ExecuteCommand(@"
                        ALTER TABLE ff_checklist_task 
                        ADD COLUMN action_mapping_id BIGINT NULL;
                    ");
                    Console.WriteLine("[Migration] Added action_mapping_id column to ff_checklist_task");
                }
                else
                {
                    Console.WriteLine("[Migration] action_mapping_id column already exists in ff_checklist_task");
                }

                // Add comment for action_mapping_id column
                try
                {
                    db.Ado.ExecuteCommand(@"
                        COMMENT ON COLUMN ff_checklist_task.action_mapping_id IS 'Associated ActionTriggerMapping Primary Key ID';
                    ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Migration] Warning: Could not add comment for action_mapping_id: {ex.Message}");
                }

                // Create index on action_mapping_id for better query performance
                try
                {
                    db.Ado.ExecuteCommand(@"
                        CREATE INDEX IF NOT EXISTS idx_ff_checklist_task_action_mapping_id 
                        ON ff_checklist_task (action_mapping_id) 
                        WHERE action_mapping_id IS NOT NULL;
                    ");
                    Console.WriteLine("[Migration] Created index on action_mapping_id column");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Migration] Warning: Could not create index on action_mapping_id: {ex.Message}");
                }

                Console.WriteLine("[Migration] Successfully added ActionMappingId field to ChecklistTask table");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Migration] Error adding ActionMappingId field: {ex.Message}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            Console.WriteLine("[Migration] Rolling back ActionMappingId field from ff_checklist_task table...");

            try
            {
                // Check if column exists before trying to drop it
                var existingColumns = db.DbMaintenance.GetColumnInfosByTableName("ff_checklist_task", false);
                var columnNames = existingColumns.Select(c => c.DbColumnName.ToLower()).ToHashSet();

                // Drop index first
                try
                {
                    db.Ado.ExecuteCommand(@"
                        DROP INDEX IF EXISTS idx_ff_checklist_task_action_mapping_id;
                    ");
                    Console.WriteLine("[Migration] Dropped index on action_mapping_id column");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Migration] Warning: Could not drop index on action_mapping_id: {ex.Message}");
                }

                // Drop action_mapping_id column if exists
                if (columnNames.Contains("action_mapping_id"))
                {
                    db.Ado.ExecuteCommand(@"
                        ALTER TABLE ff_checklist_task 
                        DROP COLUMN action_mapping_id;
                    ");
                    Console.WriteLine("[Migration] Dropped action_mapping_id column from ff_checklist_task");
                }

                Console.WriteLine("[Migration] Successfully rolled back ActionMappingId field from ChecklistTask table");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Migration] Error rolling back ActionMappingId field: {ex.Message}");
                throw;
            }
        }
    }
}