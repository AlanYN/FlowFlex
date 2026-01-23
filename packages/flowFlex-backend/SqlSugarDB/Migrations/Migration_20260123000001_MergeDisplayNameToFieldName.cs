using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Migration to merge display_name into field_name and remove display_name column
    /// </summary>
    public static class Migration_20260123000001_MergeDisplayNameToFieldName
    {
        public static void Up(ISqlSugarClient db)
        {
            Console.WriteLine("[Migration] Starting Migration_20260123000001_MergeDisplayNameToFieldName...");

            try
            {
                // Check if display_name column exists
                var columnExists = db.Ado.GetInt(@"
                    SELECT COUNT(*) FROM information_schema.columns 
                    WHERE table_name = 'ff_define_field' AND column_name = 'display_name'
                ") > 0;

                if (!columnExists)
                {
                    Console.WriteLine("[Migration] Column display_name does not exist, skipping migration");
                    return;
                }

                // Step 1: Update field_name with display_name value where display_name is not empty
                var updateSql = @"
                    UPDATE ff_define_field 
                    SET field_name = display_name 
                    WHERE display_name IS NOT NULL 
                      AND display_name <> '' 
                      AND display_name <> field_name
                ";
                var updatedRows = db.Ado.ExecuteCommand(updateSql);
                Console.WriteLine($"[Migration] Updated {updatedRows} rows: copied display_name to field_name");

                // Step 2: Drop the display_name column
                var dropColumnSql = @"
                    ALTER TABLE ff_define_field DROP COLUMN IF EXISTS display_name
                ";
                db.Ado.ExecuteCommand(dropColumnSql);
                Console.WriteLine("[Migration] Dropped display_name column from ff_define_field");

                Console.WriteLine("[Migration] Migration_20260123000001_MergeDisplayNameToFieldName completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Migration] Error in Migration_20260123000001_MergeDisplayNameToFieldName: {ex.Message}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            Console.WriteLine("[Migration] Rolling back Migration_20260123000001_MergeDisplayNameToFieldName...");

            try
            {
                // Re-add display_name column
                var addColumnSql = @"
                    ALTER TABLE ff_define_field 
                    ADD COLUMN IF NOT EXISTS display_name VARCHAR(200) DEFAULT ''
                ";
                db.Ado.ExecuteCommand(addColumnSql);

                // Copy field_name back to display_name
                var updateSql = @"
                    UPDATE ff_define_field SET display_name = field_name
                ";
                db.Ado.ExecuteCommand(updateSql);

                Console.WriteLine("[Migration] Rollback completed: re-added display_name column");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Migration] Error rolling back: {ex.Message}");
                throw;
            }
        }
    }
}
