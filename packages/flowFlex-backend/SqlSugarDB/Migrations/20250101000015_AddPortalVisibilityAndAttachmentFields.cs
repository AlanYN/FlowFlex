using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add portal visibility and attachment management fields to stage tables
    /// </summary>
    public class AddPortalVisibilityAndAttachmentFields_20250101000015
    {
        public static void Up(ISqlSugarClient db)
        {
            // Add columns to ff_stage table
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_stage 
                ADD COLUMN IF NOT EXISTS visible_in_portal BOOLEAN NOT NULL DEFAULT TRUE;
            ");

            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_stage 
                ADD COLUMN IF NOT EXISTS attachment_management_needed BOOLEAN NOT NULL DEFAULT FALSE;
            ");

            // Add comments to columns
            db.Ado.ExecuteCommand(@"
                COMMENT ON COLUMN ff_stage.visible_in_portal IS 'Controls whether this stage is visible in the portal';
                COMMENT ON COLUMN ff_stage.attachment_management_needed IS 'Indicates whether file upload is required for this stage';
            ");

            // Add columns to ff_onboarding_stage table
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_onboarding_stage 
                ADD COLUMN IF NOT EXISTS visible_in_portal BOOLEAN NOT NULL DEFAULT TRUE;
            ");

            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_onboarding_stage 
                ADD COLUMN IF NOT EXISTS attachment_management_needed BOOLEAN NOT NULL DEFAULT FALSE;
            ");

            // Add comments to columns
            db.Ado.ExecuteCommand(@"
                COMMENT ON COLUMN ff_onboarding_stage.visible_in_portal IS 'Controls whether this stage is visible in the portal';
                COMMENT ON COLUMN ff_onboarding_stage.attachment_management_needed IS 'Indicates whether file upload is required for this stage';
            ");

            // Create indexes for better query performance
            db.Ado.ExecuteCommand(@"
                CREATE INDEX IF NOT EXISTS idx_stage_visible_in_portal 
                ON ff_stage(visible_in_portal);
            ");

            db.Ado.ExecuteCommand(@"
                CREATE INDEX IF NOT EXISTS idx_stage_attachment_management_needed 
                ON ff_stage(attachment_management_needed);
            ");
        }

        public static void Down(ISqlSugarClient db)
        {
            // Drop indexes
            db.Ado.ExecuteCommand(@"
                DROP INDEX IF EXISTS idx_stage_visible_in_portal;
            ");

            db.Ado.ExecuteCommand(@"
                DROP INDEX IF EXISTS idx_stage_attachment_management_needed;
            ");

            // Drop columns from ff_stage table
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_stage 
                DROP COLUMN IF EXISTS visible_in_portal;
            ");

            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_stage 
                DROP COLUMN IF EXISTS attachment_management_needed;
            ");
        }
    }
}