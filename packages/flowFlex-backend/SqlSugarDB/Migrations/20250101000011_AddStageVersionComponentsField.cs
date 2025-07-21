using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add components_json field to ff_stage_version table
    /// </summary>
    public static class Migration_20250101000011_AddStageVersionComponentsField
    {
        /// <summary>
        /// Execute migration - Add components_json field
        /// </summary>
        public static void Up(ISqlSugarClient db)
        {
            // Add components_json column to ff_stage_version table
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_stage_version 
                ADD COLUMN IF NOT EXISTS components_json TEXT;
            ");

            // Add comment for the new column
            db.Ado.ExecuteCommand(@"
                COMMENT ON COLUMN ff_stage_version.components_json IS 'Stage Components Configuration (JSON format)';
            ");
        }

        /// <summary>
        /// Rollback migration - Remove components_json field
        /// </summary>
        public static void Down(ISqlSugarClient db)
        {
            // Remove components_json column from ff_stage_version table
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_stage_version 
                DROP COLUMN IF EXISTS components_json;
            ");
        }
    }
} 