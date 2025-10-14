using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add Portal Availability and Permission fields to workflow table
    /// Adds: visible_in_portal, portal_permission, view_permission_mode, view_teams, operate_teams
    /// </summary>
    public class Migration_20251013000001_AddWorkflowPermissionFields
    {
        public static void Up(ISqlSugarClient db)
        {
            // Add visible_in_portal column
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_workflow 
                ADD COLUMN IF NOT EXISTS visible_in_portal BOOLEAN NOT NULL DEFAULT TRUE;
            ");

            // Add comment to visible_in_portal column
            db.Ado.ExecuteCommand(@"
                COMMENT ON COLUMN ff_workflow.visible_in_portal IS 'Controls whether this workflow is visible in the customer portal';
            ");

            // Add portal_permission column (0: NotAvailable, 1: Viewable, 2: Completable)
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_workflow 
                ADD COLUMN IF NOT EXISTS portal_permission INTEGER DEFAULT 1;
            ");

            // Add comment to portal_permission column
            db.Ado.ExecuteCommand(@"
                COMMENT ON COLUMN ff_workflow.portal_permission IS 'Portal permission level: 0=NotAvailable, 1=Viewable, 2=Completable';
            ");

            // Add view_permission_mode column (0: Public, 1: VisibleToTeams, 2: InvisibleToTeams)
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_workflow 
                ADD COLUMN IF NOT EXISTS view_permission_mode INTEGER NOT NULL DEFAULT 0;
            ");

            // Add comment to view_permission_mode column
            db.Ado.ExecuteCommand(@"
                COMMENT ON COLUMN ff_workflow.view_permission_mode IS 'View permission mode: 0=Public, 1=VisibleToTeams, 2=InvisibleToTeams';
            ");

            // Add view_teams column (JSONB array of team names)
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_workflow 
                ADD COLUMN IF NOT EXISTS view_teams JSONB DEFAULT NULL;
            ");

            // Add comment to view_teams column
            db.Ado.ExecuteCommand(@"
                COMMENT ON COLUMN ff_workflow.view_teams IS 'JSONB array of team names for view permission control';
            ");

            // Add operate_teams column (JSONB array of team names)
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_workflow 
                ADD COLUMN IF NOT EXISTS operate_teams JSONB DEFAULT NULL;
            ");

            // Add comment to operate_teams column
            db.Ado.ExecuteCommand(@"
                COMMENT ON COLUMN ff_workflow.operate_teams IS 'JSONB array of team names that can perform operations (Create/Update/Delete)';
            ");

            // Create indexes for better query performance
            db.Ado.ExecuteCommand(@"
                CREATE INDEX IF NOT EXISTS idx_workflow_visible_in_portal 
                ON ff_workflow(visible_in_portal);
            ");

            db.Ado.ExecuteCommand(@"
                CREATE INDEX IF NOT EXISTS idx_workflow_portal_permission 
                ON ff_workflow(portal_permission);
            ");

            db.Ado.ExecuteCommand(@"
                CREATE INDEX IF NOT EXISTS idx_workflow_view_permission_mode 
                ON ff_workflow(view_permission_mode);
            ");

            // Create GIN indexes for JSONB columns to enable efficient array queries
            db.Ado.ExecuteCommand(@"
                CREATE INDEX IF NOT EXISTS idx_workflow_view_teams 
                ON ff_workflow USING GIN(view_teams);
            ");

            db.Ado.ExecuteCommand(@"
                CREATE INDEX IF NOT EXISTS idx_workflow_operate_teams 
                ON ff_workflow USING GIN(operate_teams);
            ");

            // Add check constraint to ensure valid portal_permission values (0=NotAvailable, 1=Viewable, 2=Completable)
            db.Ado.ExecuteCommand(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.table_constraints 
                        WHERE constraint_name = 'chk_workflow_portal_permission' 
                        AND table_name = 'ff_workflow'
                    ) THEN
                        ALTER TABLE ff_workflow 
                        ADD CONSTRAINT chk_workflow_portal_permission 
                        CHECK (portal_permission IS NULL OR portal_permission IN (0, 1, 2));
                    END IF;
                END
                $$;
            ");

            // Add check constraint to ensure valid view_permission_mode values (0=Public, 1=VisibleToTeams, 2=InvisibleToTeams)
            db.Ado.ExecuteCommand(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.table_constraints 
                        WHERE constraint_name = 'chk_workflow_view_permission_mode' 
                        AND table_name = 'ff_workflow'
                    ) THEN
                        ALTER TABLE ff_workflow 
                        ADD CONSTRAINT chk_workflow_view_permission_mode 
                        CHECK (view_permission_mode IN (0, 1, 2));
                    END IF;
                END
                $$;
            ");
        }

        public static void Down(ISqlSugarClient db)
        {
            // Drop constraints
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_workflow 
                DROP CONSTRAINT IF EXISTS chk_workflow_portal_permission;
            ");

            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_workflow 
                DROP CONSTRAINT IF EXISTS chk_workflow_view_permission_mode;
            ");

            // Drop indexes
            db.Ado.ExecuteCommand(@"
                DROP INDEX IF EXISTS idx_workflow_visible_in_portal;
            ");

            db.Ado.ExecuteCommand(@"
                DROP INDEX IF EXISTS idx_workflow_portal_permission;
            ");

            db.Ado.ExecuteCommand(@"
                DROP INDEX IF EXISTS idx_workflow_view_permission_mode;
            ");

            db.Ado.ExecuteCommand(@"
                DROP INDEX IF EXISTS idx_workflow_view_teams;
            ");

            db.Ado.ExecuteCommand(@"
                DROP INDEX IF EXISTS idx_workflow_operate_teams;
            ");

            // Drop columns from ff_workflow table
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_workflow 
                DROP COLUMN IF EXISTS visible_in_portal;
            ");

            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_workflow 
                DROP COLUMN IF EXISTS portal_permission;
            ");

            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_workflow 
                DROP COLUMN IF EXISTS view_permission_mode;
            ");

            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_workflow 
                DROP COLUMN IF EXISTS view_teams;
            ");

            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_workflow 
                DROP COLUMN IF EXISTS operate_teams;
            ");
        }
    }
}

