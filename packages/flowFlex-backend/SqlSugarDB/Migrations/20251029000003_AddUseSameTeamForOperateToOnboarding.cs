using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add UseSameTeamForOperate field to onboarding (case) table
    /// Adds: use_same_team_for_operate
    /// </summary>
    public class Migration_20251029000003_AddUseSameTeamForOperateToOnboarding
    {
        public static void Up(ISqlSugarClient db)
        {
            // Add use_same_team_for_operate column
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_onboarding 
                ADD COLUMN IF NOT EXISTS use_same_team_for_operate BOOLEAN NOT NULL DEFAULT FALSE;
            ");

            // Add comment to use_same_team_for_operate column
            db.Ado.ExecuteCommand(@"
                COMMENT ON COLUMN ff_onboarding.use_same_team_for_operate IS 'Indicates whether operate teams/users should use the same teams/users as view permission. When true, OperateTeams/OperateUsers will be automatically synchronized with ViewTeams/ViewUsers based on the permission subject type';
            ");

            // Create index for better query performance
            db.Ado.ExecuteCommand(@"
                CREATE INDEX IF NOT EXISTS idx_onboarding_use_same_team_for_operate 
                ON ff_onboarding(use_same_team_for_operate);
            ");
        }

        public static void Down(ISqlSugarClient db)
        {
            // Drop index
            db.Ado.ExecuteCommand(@"
                DROP INDEX IF EXISTS idx_onboarding_use_same_team_for_operate;
            ");

            // Drop column from ff_onboarding table
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_onboarding 
                DROP COLUMN IF EXISTS use_same_team_for_operate;
            ");
        }
    }
}

