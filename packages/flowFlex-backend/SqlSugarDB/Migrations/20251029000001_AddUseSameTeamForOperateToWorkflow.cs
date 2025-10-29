using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add UseSameTeamForOperate field to workflow table
    /// Adds: use_same_team_for_operate
    /// </summary>
    public class Migration_20251029000001_AddUseSameTeamForOperateToWorkflow
    {
        public static void Up(ISqlSugarClient db)
        {
            // Add use_same_team_for_operate column
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_workflow 
                ADD COLUMN IF NOT EXISTS use_same_team_for_operate BOOLEAN NOT NULL DEFAULT FALSE;
            ");

            // Add comment to use_same_team_for_operate column
            db.Ado.ExecuteCommand(@"
                COMMENT ON COLUMN ff_workflow.use_same_team_for_operate IS 'Indicates whether operate teams should use the same teams as view permission. When true, OperateTeams will be automatically synchronized with ViewTeams';
            ");

            // Create index for better query performance
            db.Ado.ExecuteCommand(@"
                CREATE INDEX IF NOT EXISTS idx_workflow_use_same_team_for_operate 
                ON ff_workflow(use_same_team_for_operate);
            ");
        }

        public static void Down(ISqlSugarClient db)
        {
            // Drop index
            db.Ado.ExecuteCommand(@"
                DROP INDEX IF EXISTS idx_workflow_use_same_team_for_operate;
            ");

            // Drop column from ff_workflow table
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_workflow 
                DROP COLUMN IF EXISTS use_same_team_for_operate;
            ");
        }
    }
}

