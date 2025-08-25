using SqlSugar;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add ActionId and ActionName fields to ChecklistTask table
    /// </summary>
    public class _20250122000024_AddActionFieldsToChecklistTask
    {
        public static void Up(ISqlSugarClient db)
        {
            Console.WriteLine("[Migration] Adding ActionId and ActionName fields to ff_checklist_task table...");

            try
            {
                // Check if columns exist first
                var existingColumns = db.DbMaintenance.GetColumnInfosByTableName("ff_checklist_task", false);
                var columnNames = existingColumns.Select(c => c.DbColumnName.ToLower()).ToHashSet();

                // Add action_id column if not exists
                if (!columnNames.Contains("action_id"))
                {
                    db.Ado.ExecuteCommand(@"
                        ALTER TABLE ff_checklist_task 
                        ADD COLUMN action_id BIGINT NULL;
                    ");
                    Console.WriteLine("[Migration] Added action_id column to ff_checklist_task");
                }
                else
                {
                    Console.WriteLine("[Migration] action_id column already exists in ff_checklist_task");
                }

                // Add action_name column if not exists
                if (!columnNames.Contains("action_name"))
                {
                    db.Ado.ExecuteCommand(@"
                        ALTER TABLE ff_checklist_task 
                        ADD COLUMN action_name VARCHAR(200) NULL;
                    ");
                    Console.WriteLine("[Migration] Added action_name column to ff_checklist_task");
                }
                else
                {
                    Console.WriteLine("[Migration] action_name column already exists in ff_checklist_task");
                }

                // Add comment for action_id column
                try
                {
                    db.Ado.ExecuteCommand(@"
                        COMMENT ON COLUMN ff_checklist_task.action_id IS 'Associated Action Primary Key ID';
                    ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Migration] Warning: Could not add comment for action_id: {ex.Message}");
                }

                // Add comment for action_name column
                try
                {
                    db.Ado.ExecuteCommand(@"
                        COMMENT ON COLUMN ff_checklist_task.action_name IS 'Action Name';
                    ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Migration] Warning: Could not add comment for action_name: {ex.Message}");
                }

                // Create index on action_id for better query performance
                try
                {
                    db.Ado.ExecuteCommand(@"
                        CREATE INDEX IF NOT EXISTS idx_ff_checklist_task_action_id 
                        ON ff_checklist_task (action_id) 
                        WHERE action_id IS NOT NULL;
                    ");
                    Console.WriteLine("[Migration] Created index on action_id column");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Migration] Warning: Could not create index on action_id: {ex.Message}");
                }

                Console.WriteLine("[Migration] Successfully added ActionId and ActionName fields to ChecklistTask table");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Migration] Error adding ActionId and ActionName fields: {ex.Message}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            Console.WriteLine("[Migration] Rolling back ActionId and ActionName fields from ff_checklist_task table...");

            try
            {
                // Check if columns exist before trying to drop them
                var existingColumns = db.DbMaintenance.GetColumnInfosByTableName("ff_checklist_task", false);
                var columnNames = existingColumns.Select(c => c.DbColumnName.ToLower()).ToHashSet();

                // Drop index first
                try
                {
                    db.Ado.ExecuteCommand(@"
                        DROP INDEX IF EXISTS idx_ff_checklist_task_action_id;
                    ");
                    Console.WriteLine("[Migration] Dropped index on action_id column");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Migration] Warning: Could not drop index on action_id: {ex.Message}");
                }

                // Drop action_name column if exists
                if (columnNames.Contains("action_name"))
                {
                    db.Ado.ExecuteCommand(@"
                        ALTER TABLE ff_checklist_task 
                        DROP COLUMN action_name;
                    ");
                    Console.WriteLine("[Migration] Dropped action_name column from ff_checklist_task");
                }

                // Drop action_id column if exists
                if (columnNames.Contains("action_id"))
                {
                    db.Ado.ExecuteCommand(@"
                        ALTER TABLE ff_checklist_task 
                        DROP COLUMN action_id;
                    ");
                    Console.WriteLine("[Migration] Dropped action_id column from ff_checklist_task");
                }

                Console.WriteLine("[Migration] Successfully rolled back ActionId and ActionName fields from ChecklistTask table");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Migration] Error rolling back ActionId and ActionName fields: {ex.Message}");
                throw;
            }
        }
    }
}