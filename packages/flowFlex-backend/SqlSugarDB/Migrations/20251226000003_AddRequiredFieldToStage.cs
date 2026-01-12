using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add Required field to stage table
    /// Indicates whether this stage is required to complete the workflow
    /// </summary>
    public class Migration_20251226000003_AddRequiredFieldToStage
    {
        public static void Up(ISqlSugarClient db)
        {
            // Add required column
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_stage 
                ADD COLUMN IF NOT EXISTS required BOOLEAN NOT NULL DEFAULT FALSE;
            ");

            // Add comment to required column
            db.Ado.ExecuteCommand(@"
                COMMENT ON COLUMN ff_stage.required IS 'Indicates whether this stage is required to complete the workflow';
            ");
        }

        public static void Down(ISqlSugarClient db)
        {
            // Drop column from ff_stage table
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_stage 
                DROP COLUMN IF EXISTS required;
            ");
        }
    }
}
