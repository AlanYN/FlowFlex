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
                ADD COLUMN visible_in_portal BOOLEAN DEFAULT TRUE;
            ");

            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_stage 
                ADD COLUMN attachment_management_needed BOOLEAN DEFAULT FALSE;
            ");

            // Add columns to ff_stage_version table
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_stage_version 
                ADD COLUMN visible_in_portal BOOLEAN DEFAULT TRUE;
            ");

            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_stage_version 
                ADD COLUMN attachment_management_needed BOOLEAN DEFAULT FALSE;
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

            // Drop columns from ff_stage_version table
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_stage_version 
                DROP COLUMN IF EXISTS visible_in_portal;
            ");

            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_stage_version 
                DROP COLUMN IF EXISTS attachment_management_needed;
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