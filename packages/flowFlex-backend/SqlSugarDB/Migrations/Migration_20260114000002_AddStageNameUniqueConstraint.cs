using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add unique constraint on ff_stage table to prevent duplicate stage names within the same workflow.
    /// Only applies to valid records (is_valid = true).
    /// </summary>
    public class Migration_20260114000002_AddStageNameUniqueConstraint
    {
        private static ISqlSugarClient _db;

        public static void Up(ISqlSugarClient db)
        {
            _db = db;

            // Create a partial unique index for stage name within workflow and tenant
            _db.Ado.ExecuteCommand(@"
                CREATE UNIQUE INDEX IF NOT EXISTS uq_stage_name_workflow 
                ON ff_stage (workflow_id, name, tenant_id) 
                WHERE is_valid = true;
            ");
        }

        public static void Down(ISqlSugarClient db)
        {
            _db = db;

            // Drop the unique index
            _db.Ado.ExecuteCommand(@"
                DROP INDEX IF EXISTS uq_stage_name_workflow;
            ");
        }
    }
}
