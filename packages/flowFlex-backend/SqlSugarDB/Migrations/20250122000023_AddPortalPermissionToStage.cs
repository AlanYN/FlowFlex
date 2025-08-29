using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add portal permission field to stage tables
    /// </summary>
    public class Migration_20250122000023_AddPortalPermissionToStage
    {
        public static void Up(ISqlSugarClient db)
        {
            // Add portal_permission column to ff_stage table
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_stage 
                ADD COLUMN IF NOT EXISTS portal_permission INTEGER NOT NULL DEFAULT 1;
            ");

            // Add comment to column
            db.Ado.ExecuteCommand(@"
                COMMENT ON COLUMN ff_stage.portal_permission IS 'Portal permission level: 1=Viewable, 2=Completable';
            ");

            // Add portal_permission column to ff_onboarding_stage table if it exists
            db.Ado.ExecuteCommand(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'ff_onboarding_stage') THEN
                        ALTER TABLE ff_onboarding_stage 
                        ADD COLUMN IF NOT EXISTS portal_permission INTEGER NOT NULL DEFAULT 1;
                        
                        COMMENT ON COLUMN ff_onboarding_stage.portal_permission IS 'Portal permission level: 1=Viewable, 2=Completable';
                    END IF;
                END
                $$;
            ");

            // Create index for better query performance
            db.Ado.ExecuteCommand(@"
                CREATE INDEX IF NOT EXISTS idx_stage_portal_permission 
                ON ff_stage(portal_permission);
            ");

            // Create index for onboarding_stage if it exists
            db.Ado.ExecuteCommand(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'ff_onboarding_stage') THEN
                        CREATE INDEX IF NOT EXISTS idx_onboarding_stage_portal_permission 
                        ON ff_onboarding_stage(portal_permission);
                    END IF;
                END
                $$;
            ");

            // Add check constraint to ensure valid values (1=Viewable, 2=Completable)
            db.Ado.ExecuteCommand(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.table_constraints 
                        WHERE constraint_name = 'chk_stage_portal_permission' 
                        AND table_name = 'ff_stage'
                    ) THEN
                        ALTER TABLE ff_stage 
                        ADD CONSTRAINT chk_stage_portal_permission 
                        CHECK (portal_permission IN (1, 2));
                    END IF;
                END
                $$;
            ");

            // Add check constraint for onboarding_stage if it exists
            db.Ado.ExecuteCommand(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'ff_onboarding_stage') THEN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.table_constraints 
                            WHERE constraint_name = 'chk_onboarding_stage_portal_permission' 
                            AND table_name = 'ff_onboarding_stage'
                        ) THEN
                            ALTER TABLE ff_onboarding_stage 
                            ADD CONSTRAINT chk_onboarding_stage_portal_permission 
                            CHECK (portal_permission IN (1, 2));
                        END IF;
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
                DROP CONSTRAINT IF EXISTS chk_stage_portal_permission;
            ");

            db.Ado.ExecuteCommand(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'ff_onboarding_stage') THEN
                        ALTER TABLE ff_onboarding_stage 
                        DROP CONSTRAINT IF EXISTS chk_onboarding_stage_portal_permission;
                    END IF;
                END
                $$;
            ");

            // Drop indexes
            db.Ado.ExecuteCommand(@"
                DROP INDEX IF EXISTS idx_stage_portal_permission;
            ");

            db.Ado.ExecuteCommand(@"
                DROP INDEX IF EXISTS idx_onboarding_stage_portal_permission;
            ");

            // Drop columns from ff_stage table
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_stage 
                DROP COLUMN IF EXISTS portal_permission;
            ");

            // Drop column from ff_onboarding_stage table if it exists
            db.Ado.ExecuteCommand(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'ff_onboarding_stage') THEN
                        ALTER TABLE ff_onboarding_stage 
                        DROP COLUMN IF EXISTS portal_permission;
                    END IF;
                END
                $$;
            ");
        }
    }
}