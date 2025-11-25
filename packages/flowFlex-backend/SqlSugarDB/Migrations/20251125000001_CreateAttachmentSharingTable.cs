using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Create Attachment Sharing table
    /// Migration: 20251125000001_CreateAttachmentSharingTable
    /// Date: 2025-11-25
    /// 
    /// This migration creates the ff_attachment_sharing table to store
    /// configuration for receiving attachments from external system modules.
    /// 
    /// Fields:
    /// - integration_id: Reference to the parent integration
    /// - external_module_name: User-defined name for the external module
    /// - system_id: Auto-generated unique identifier for API calls
    /// - workflow_ids: JSON array of workflow IDs where this attachment sharing is available
    /// - is_active: Whether the configuration is active
    /// - description: Optional description
    /// - allowed_file_types: JSON array of allowed file extensions
    /// - max_file_size_mb: Maximum file size limit in MB
    /// </summary>
    public static class Migration_20251125000001_CreateAttachmentSharingTable
    {
        public static void Up(ISqlSugarClient db)
        {
            Console.WriteLine("Starting migration: Create Attachment Sharing table");

            // Check if table already exists
            var tableExists = db.Ado.GetDataTable(@"
                SELECT table_name 
                FROM information_schema.tables 
                WHERE table_name = 'ff_attachment_sharing'
            ").Rows.Count > 0;

            if (!tableExists)
            {
                // Create ff_attachment_sharing table
                db.Ado.ExecuteCommand(@"
                    CREATE TABLE ff_attachment_sharing (
                        id BIGINT NOT NULL PRIMARY KEY,
                        tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                        app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT',
                        integration_id BIGINT NOT NULL,
                        external_module_name VARCHAR(200) NOT NULL,
                        system_id VARCHAR(100) NOT NULL,
                        workflow_ids TEXT NOT NULL DEFAULT '[]',
                        is_active BOOLEAN DEFAULT TRUE,
                        description VARCHAR(500),
                        allowed_file_types TEXT,
                        max_file_size_mb INTEGER,
                        is_valid BOOLEAN DEFAULT TRUE,
                        create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                        modify_date TIMESTAMPTZ,
                        create_by VARCHAR(50) DEFAULT 'SYSTEM',
                        modify_by VARCHAR(50),
                        create_user_id BIGINT DEFAULT 0,
                        modify_user_id BIGINT DEFAULT 0
                    );
                ");

                Console.WriteLine("✓ Created ff_attachment_sharing table");

                // Create indexes
                db.Ado.ExecuteCommand(@"
                    CREATE INDEX idx_attachment_sharing_integration_id 
                    ON ff_attachment_sharing(integration_id);
                ");
                Console.WriteLine("✓ Created index on integration_id");

                db.Ado.ExecuteCommand(@"
                    CREATE UNIQUE INDEX idx_attachment_sharing_system_id 
                    ON ff_attachment_sharing(system_id);
                ");
                Console.WriteLine("✓ Created unique index on system_id");

                db.Ado.ExecuteCommand(@"
                    CREATE INDEX idx_attachment_sharing_tenant_id 
                    ON ff_attachment_sharing(tenant_id);
                ");
                Console.WriteLine("✓ Created index on tenant_id");

                db.Ado.ExecuteCommand(@"
                    CREATE INDEX idx_attachment_sharing_is_active 
                    ON ff_attachment_sharing(is_active);
                ");
                Console.WriteLine("✓ Created index on is_active");

                db.Ado.ExecuteCommand(@"
                    CREATE INDEX idx_attachment_sharing_is_valid 
                    ON ff_attachment_sharing(is_valid);
                ");
                Console.WriteLine("✓ Created index on is_valid");
            }
            else
            {
                Console.WriteLine("✓ Table 'ff_attachment_sharing' already exists, skipping");
            }

            Console.WriteLine("Migration completed: Create Attachment Sharing table");
        }

        public static void Down(ISqlSugarClient db)
        {
            Console.WriteLine("Starting rollback: Drop Attachment Sharing table");

            // Check if table exists
            var tableExists = db.Ado.GetDataTable(@"
                SELECT table_name 
                FROM information_schema.tables 
                WHERE table_name = 'ff_attachment_sharing'
            ").Rows.Count > 0;

            if (tableExists)
            {
                // Drop indexes first
                db.Ado.ExecuteCommand(@"DROP INDEX IF EXISTS idx_attachment_sharing_integration_id;");
                db.Ado.ExecuteCommand(@"DROP INDEX IF EXISTS idx_attachment_sharing_system_id;");
                db.Ado.ExecuteCommand(@"DROP INDEX IF EXISTS idx_attachment_sharing_tenant_id;");
                db.Ado.ExecuteCommand(@"DROP INDEX IF EXISTS idx_attachment_sharing_is_active;");
                db.Ado.ExecuteCommand(@"DROP INDEX IF EXISTS idx_attachment_sharing_is_valid;");

                Console.WriteLine("✓ Dropped indexes");

                // Drop table
                db.Ado.ExecuteCommand(@"DROP TABLE ff_attachment_sharing;");
                Console.WriteLine("✓ Dropped ff_attachment_sharing table");
            }
            else
            {
                Console.WriteLine("✓ Table 'ff_attachment_sharing' does not exist, skipping");
            }

            Console.WriteLine("Rollback completed: Drop Attachment Sharing table");
        }
    }
}

