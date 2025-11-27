using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Refactor Attachment Configuration
    /// Migration: 20251127000003_RefactorAttachmentConfiguration
    /// Date: 2025-11-27
    /// 
    /// This migration:
    /// 1. Drops ff_outbound_configuration table
    /// 2. Drops ff_outbound_field_config table
    /// 3. Drops ff_attachment_sharing table
    /// 4. Drops ff_inbound_configuration table
    /// 5. Removes outbound_attachment_workflow_ids column from ff_integration
    /// 6. Adds inbound_attachments column to ff_integration (JSON array with module_name, workflow_id, stage_id)
    /// 7. Adds outbound_attachments column to ff_integration (JSON array with workflow_id, stage_id)
    /// </summary>
    public static class Migration_20251127000003_RefactorAttachmentConfiguration
    {
        public static void Up(ISqlSugarClient db)
        {
            Console.WriteLine("Starting migration: Refactor Attachment Configuration");

            // 1. Drop ff_outbound_field_config table (has foreign key to ff_outbound_configuration)
            var outboundFieldConfigExists = db.Ado.GetDataTable(@"
                SELECT table_name 
                FROM information_schema.tables 
                WHERE table_name = 'ff_outbound_field_config'
            ").Rows.Count > 0;

            if (outboundFieldConfigExists)
            {
                db.Ado.ExecuteCommand(@"DROP TABLE ff_outbound_field_config;");
                Console.WriteLine("✓ Dropped ff_outbound_field_config table");
            }
            else
            {
                Console.WriteLine("✓ Table 'ff_outbound_field_config' does not exist, skipping");
            }

            // 2. Drop ff_outbound_configuration table
            var outboundConfigExists = db.Ado.GetDataTable(@"
                SELECT table_name 
                FROM information_schema.tables 
                WHERE table_name = 'ff_outbound_configuration'
            ").Rows.Count > 0;

            if (outboundConfigExists)
            {
                db.Ado.ExecuteCommand(@"DROP TABLE ff_outbound_configuration;");
                Console.WriteLine("✓ Dropped ff_outbound_configuration table");
            }
            else
            {
                Console.WriteLine("✓ Table 'ff_outbound_configuration' does not exist, skipping");
            }

            // 3. Drop ff_attachment_sharing table
            var attachmentSharingExists = db.Ado.GetDataTable(@"
                SELECT table_name 
                FROM information_schema.tables 
                WHERE table_name = 'ff_attachment_sharing'
            ").Rows.Count > 0;

            if (attachmentSharingExists)
            {
                db.Ado.ExecuteCommand(@"DROP TABLE ff_attachment_sharing;");
                Console.WriteLine("✓ Dropped ff_attachment_sharing table");
            }
            else
            {
                Console.WriteLine("✓ Table 'ff_attachment_sharing' does not exist, skipping");
            }

            // 4. Drop ff_inbound_configuration table
            var inboundConfigExists = db.Ado.GetDataTable(@"
                SELECT table_name 
                FROM information_schema.tables 
                WHERE table_name = 'ff_inbound_configuration'
            ").Rows.Count > 0;

            if (inboundConfigExists)
            {
                db.Ado.ExecuteCommand(@"DROP TABLE ff_inbound_configuration;");
                Console.WriteLine("✓ Dropped ff_inbound_configuration table");
            }
            else
            {
                Console.WriteLine("✓ Table 'ff_inbound_configuration' does not exist, skipping");
            }

            // 5. Remove outbound_attachment_workflow_ids column from ff_integration
            var oldColumnExists = db.Ado.GetDataTable(@"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = 'ff_integration' AND column_name = 'outbound_attachment_workflow_ids'
            ").Rows.Count > 0;

            if (oldColumnExists)
            {
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_integration DROP COLUMN outbound_attachment_workflow_ids;
                ");
                Console.WriteLine("✓ Removed outbound_attachment_workflow_ids column from ff_integration");
            }
            else
            {
                Console.WriteLine("✓ Column 'outbound_attachment_workflow_ids' does not exist, skipping");
            }

            // 5. Add inbound_attachments column to ff_integration
            var inboundColumnExists = db.Ado.GetDataTable(@"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = 'ff_integration' AND column_name = 'inbound_attachments'
            ").Rows.Count > 0;

            if (!inboundColumnExists)
            {
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_integration ADD COLUMN inbound_attachments TEXT;
                ");
                Console.WriteLine("✓ Added inbound_attachments column to ff_integration");
            }
            else
            {
                Console.WriteLine("✓ Column 'inbound_attachments' already exists, skipping");
            }

            // 6. Add outbound_attachments column to ff_integration
            var outboundColumnExists = db.Ado.GetDataTable(@"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = 'ff_integration' AND column_name = 'outbound_attachments'
            ").Rows.Count > 0;

            if (!outboundColumnExists)
            {
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_integration ADD COLUMN outbound_attachments TEXT;
                ");
                Console.WriteLine("✓ Added outbound_attachments column to ff_integration");
            }
            else
            {
                Console.WriteLine("✓ Column 'outbound_attachments' already exists, skipping");
            }

            Console.WriteLine("Migration completed: Refactor Attachment Configuration");
        }

        public static void Down(ISqlSugarClient db)
        {
            Console.WriteLine("Starting rollback: Refactor Attachment Configuration");

            // Note: This rollback will not restore data, only recreate table structures

            // Remove new columns from ff_integration
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_integration DROP COLUMN IF EXISTS inbound_attachments;
                ALTER TABLE ff_integration DROP COLUMN IF EXISTS outbound_attachments;
            ");
            Console.WriteLine("✓ Removed new attachment columns from ff_integration");

            // Restore outbound_attachment_workflow_ids column
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_integration ADD COLUMN IF NOT EXISTS outbound_attachment_workflow_ids TEXT;
            ");
            Console.WriteLine("✓ Restored outbound_attachment_workflow_ids column");

            // Recreate ff_outbound_configuration table
            db.Ado.ExecuteCommand(@"
                CREATE TABLE IF NOT EXISTS ff_outbound_configuration (
                    id BIGINT NOT NULL PRIMARY KEY,
                    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                    app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT',
                    integration_id BIGINT NOT NULL,
                    action_id BIGINT,
                    entity_types TEXT NOT NULL DEFAULT '[]',
                    field_mappings TEXT NOT NULL DEFAULT '[]',
                    attachment_settings TEXT NOT NULL DEFAULT '{}',
                    sync_mode INTEGER NOT NULL DEFAULT 0,
                    webhook_url VARCHAR(500),
                    is_valid BOOLEAN DEFAULT TRUE,
                    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(50) DEFAULT 'SYSTEM',
                    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                    create_user_id BIGINT DEFAULT 0,
                    modify_user_id BIGINT DEFAULT 0
                );
            ");
            Console.WriteLine("✓ Recreated ff_outbound_configuration table");

            // Recreate ff_attachment_sharing table
            db.Ado.ExecuteCommand(@"
                CREATE TABLE IF NOT EXISTS ff_attachment_sharing (
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
            Console.WriteLine("✓ Recreated ff_attachment_sharing table");

            // Recreate ff_inbound_configuration table
            db.Ado.ExecuteCommand(@"
                CREATE TABLE IF NOT EXISTS ff_inbound_configuration (
                    id BIGINT NOT NULL PRIMARY KEY,
                    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                    app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT',
                    integration_id BIGINT NOT NULL,
                    action_id BIGINT,
                    entity_types TEXT NOT NULL DEFAULT '[]',
                    field_mappings TEXT NOT NULL DEFAULT '[]',
                    attachment_settings TEXT NOT NULL DEFAULT '{}',
                    auto_sync BOOLEAN DEFAULT FALSE,
                    sync_interval INTEGER DEFAULT 0,
                    last_sync_date TIMESTAMPTZ,
                    is_valid BOOLEAN DEFAULT TRUE,
                    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(50) DEFAULT 'SYSTEM',
                    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                    create_user_id BIGINT DEFAULT 0,
                    modify_user_id BIGINT DEFAULT 0
                );
            ");
            Console.WriteLine("✓ Recreated ff_inbound_configuration table");

            Console.WriteLine("Rollback completed: Refactor Attachment Configuration");
        }
    }
}

