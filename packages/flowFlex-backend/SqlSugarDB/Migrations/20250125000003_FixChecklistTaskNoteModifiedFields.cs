using SqlSugar;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Fix ChecklistTaskNote modified_by_id and modified_by_name fields to allow NULL
    /// </summary>
    public class _20250125000003_FixChecklistTaskNoteModifiedFields
    {
        public static void Up(ISqlSugarClient db)
        {
            Console.WriteLine("[Migration] Fixing ChecklistTaskNote modified fields to allow NULL...");

            try
            {
                // Check if table exists first
                if (db.DbMaintenance.IsAnyTable("ff_checklist_task_note", false))
                {
                    // Get existing columns to check their constraints
                    var existingColumns = db.DbMaintenance.GetColumnInfosByTableName("ff_checklist_task_note", false);
                    var columnNames = existingColumns.Select(c => c.DbColumnName.ToLower()).ToHashSet();

                    // Alter modified_by_id column to allow NULL
                    if (columnNames.Contains("modified_by_id"))
                    {
                        try
                        {
                            db.Ado.ExecuteCommand(@"
                                ALTER TABLE ff_checklist_task_note 
                                ALTER COLUMN modified_by_id DROP NOT NULL;
                            ");
                            Console.WriteLine("[Migration] Set modified_by_id column to allow NULL");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Migration] Warning: Could not modify modified_by_id constraint: {ex.Message}");
                        }
                    }

                    // Alter modified_by_name column to allow NULL
                    if (columnNames.Contains("modified_by_name"))
                    {
                        try
                        {
                            db.Ado.ExecuteCommand(@"
                                ALTER TABLE ff_checklist_task_note 
                                ALTER COLUMN modified_by_name DROP NOT NULL;
                            ");
                            Console.WriteLine("[Migration] Set modified_by_name column to allow NULL");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Migration] Warning: Could not modify modified_by_name constraint: {ex.Message}");
                        }
                    }

                    // Update existing records to set NULL for empty modified fields
                    try
                    {
                        db.Ado.ExecuteCommand(@"
                            UPDATE ff_checklist_task_note 
                            SET modified_by_id = NULL 
                            WHERE modified_by_id = '';
                        ");

                        db.Ado.ExecuteCommand(@"
                            UPDATE ff_checklist_task_note 
                            SET modified_by_name = NULL 
                            WHERE modified_by_name = '';
                        ");
                        Console.WriteLine("[Migration] Updated existing records to use NULL for empty modified fields");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Migration] Warning: Could not update existing records: {ex.Message}");
                    }

                    Console.WriteLine("[Migration] Successfully fixed ChecklistTaskNote modified fields");
                }
                else
                {
                    Console.WriteLine("[Migration] ff_checklist_task_note table does not exist, skipping migration");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Migration] Error fixing ChecklistTaskNote modified fields: {ex.Message}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            Console.WriteLine("[Migration] Rolling back ChecklistTaskNote modified fields fix...");

            try
            {
                if (db.DbMaintenance.IsAnyTable("ff_checklist_task_note", false))
                {
                    // Set default values for NULL fields before adding NOT NULL constraints
                    try
                    {
                        db.Ado.ExecuteCommand(@"
                            UPDATE ff_checklist_task_note 
                            SET modified_by_id = '' 
                            WHERE modified_by_id IS NULL;
                        ");

                        db.Ado.ExecuteCommand(@"
                            UPDATE ff_checklist_task_note 
                            SET modified_by_name = '' 
                            WHERE modified_by_name IS NULL;
                        ");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Migration] Warning: Could not update NULL values: {ex.Message}");
                    }

                    // Add NOT NULL constraints back (if needed)
                    try
                    {
                        db.Ado.ExecuteCommand(@"
                            ALTER TABLE ff_checklist_task_note 
                            ALTER COLUMN modified_by_id SET NOT NULL;
                        ");

                        db.Ado.ExecuteCommand(@"
                            ALTER TABLE ff_checklist_task_note 
                            ALTER COLUMN modified_by_name SET NOT NULL;
                        ");
                        Console.WriteLine("[Migration] Restored NOT NULL constraints");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Migration] Warning: Could not restore NOT NULL constraints: {ex.Message}");
                    }
                }

                Console.WriteLine("[Migration] Successfully rolled back ChecklistTaskNote modified fields fix");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Migration] Error rolling back ChecklistTaskNote modified fields fix: {ex.Message}");
                throw;
            }
        }
    }
}