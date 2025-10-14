using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add View and Operate Permission fields to stage table
    /// Adds: view_permission_mode, view_teams, operate_teams
    /// </summary>
    public class Migration_20251014000001_AddStagePermissionFields
    {
        public static void Up(ISqlSugarClient db)
        {
            // Add view_permission_mode column (0: Public, 1: VisibleToTeams, 2: InvisibleToTeams, 3: Private)
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_stage 
                ADD COLUMN IF NOT EXISTS view_permission_mode INTEGER NOT NULL DEFAULT 0;
            ");

            // Add comment to view_permission_mode column
            db.Ado.ExecuteCommand(@"
                COMMENT ON COLUMN ff_stage.view_permission_mode IS 'View permission mode: 0=Public, 1=VisibleToTeams, 2=InvisibleToTeams, 3=Private';
            ");

            // Add view_teams column (JSONB array of team names)
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_stage 
                ADD COLUMN IF NOT EXISTS view_teams JSONB DEFAULT NULL;
            ");

            // Add comment to view_teams column
            db.Ado.ExecuteCommand(@"
                COMMENT ON COLUMN ff_stage.view_teams IS 'JSONB array of team names for view permission control';
            ");

            // Add operate_teams column (JSONB array of team names)
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_stage 
                ADD COLUMN IF NOT EXISTS operate_teams JSONB DEFAULT NULL;
            ");

            // Add comment to operate_teams column
            db.Ado.ExecuteCommand(@"
                COMMENT ON COLUMN ff_stage.operate_teams IS 'JSONB array of team names that can perform operations (Create/Update/Delete)';
            ");

            // Create indexes for better query performance
            db.Ado.ExecuteCommand(@"
                CREATE INDEX IF NOT EXISTS idx_stage_view_permission_mode 
                ON ff_stage(view_permission_mode);
            ");

            // Create GIN indexes for JSONB columns to enable efficient array queries
            db.Ado.ExecuteCommand(@"
                CREATE INDEX IF NOT EXISTS idx_stage_view_teams 
                ON ff_stage USING GIN(view_teams);
            ");

            db.Ado.ExecuteCommand(@"
                CREATE INDEX IF NOT EXISTS idx_stage_operate_teams 
                ON ff_stage USING GIN(operate_teams);
            ");

            // Add check constraint to ensure valid view_permission_mode values (0=Public, 1=VisibleToTeams, 2=InvisibleToTeams, 3=Private)
            db.Ado.ExecuteCommand(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.table_constraints 
                        WHERE constraint_name = 'chk_stage_view_permission_mode' 
                        AND table_name = 'ff_stage'
                    ) THEN
                        ALTER TABLE ff_stage 
                        ADD CONSTRAINT chk_stage_view_permission_mode 
                        CHECK (view_permission_mode IN (0, 1, 2, 3));
                    END IF;
                END
                $$;
            ");
        }

        public static void Down(ISqlSugarClient db)
        {
            // Drop constraints
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_stage 
                DROP CONSTRAINT IF EXISTS chk_stage_view_permission_mode;
            ");

            // Drop indexes
            db.Ado.ExecuteCommand(@"
                DROP INDEX IF EXISTS idx_stage_view_permission_mode;
            ");

            db.Ado.ExecuteCommand(@"
                DROP INDEX IF EXISTS idx_stage_view_teams;
            ");

            db.Ado.ExecuteCommand(@"
                DROP INDEX IF EXISTS idx_stage_operate_teams;
            ");

            // Drop columns from ff_stage table
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_stage 
                DROP COLUMN IF EXISTS view_permission_mode;
            ");

            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_stage 
                DROP COLUMN IF EXISTS view_teams;
            ");

            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_stage 
                DROP COLUMN IF EXISTS operate_teams;
            ");
        }
    }
}

