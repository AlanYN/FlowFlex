using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Migration to add co_assignees column to ff_stage table
    /// Date: 2025-01-05
    /// </summary>
    public class AddCoAssigneesToStage_20250105000001
    {
        /// <summary>
        /// Execute migration - Add co_assignees JSONB column
        /// </summary>
        public static void Up(ISqlSugarClient db)
        {
            var tableName = "ff_stage";
            var columnName = "co_assignees";

            // Check if column already exists
            var columnExists = db.DbMaintenance.GetColumnInfosByTableName(tableName)
                .Any(c => c.DbColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase));

            if (!columnExists)
            {
                // Add co_assignees column as JSONB type
                db.Ado.ExecuteCommand($@"
                    ALTER TABLE {tableName} 
                    ADD COLUMN {columnName} jsonb DEFAULT NULL;
                ");

                // Add comment for the column
                db.Ado.ExecuteCommand($@"
                    COMMENT ON COLUMN {tableName}.{columnName} IS 'Co-assignees - JSONB array of additional user IDs assigned to this stage';
                ");

                Console.WriteLine($"[Migration] Added column '{columnName}' to table '{tableName}'");
            }
            else
            {
                Console.WriteLine($"[Migration] Column '{columnName}' already exists in table '{tableName}', skipping...");
            }
        }

        /// <summary>
        /// Rollback migration - Remove co_assignees column
        /// </summary>
        public static void Down(ISqlSugarClient db)
        {
            var tableName = "ff_stage";
            var columnName = "co_assignees";

            var columnExists = db.DbMaintenance.GetColumnInfosByTableName(tableName)
                .Any(c => c.DbColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase));

            if (columnExists)
            {
                db.Ado.ExecuteCommand($@"
                    ALTER TABLE {tableName} 
                    DROP COLUMN {columnName};
                ");

                Console.WriteLine($"[Migration] Removed column '{columnName}' from table '{tableName}'");
            }
        }
    }
}
