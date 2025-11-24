using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add action_id field to Field Mapping and Outbound Field Config tables
    /// Migration: 20251124000003_AddActionIdToFieldMappings
    /// Date: 2025-11-24
    /// 
    /// This migration adds action_id fields to:
    /// 1. ff_field_mapping - to directly associate field mappings with actions
    /// 2. ff_outbound_field_config - to directly associate outbound field configs with actions
    /// 
    /// This allows Field Mapping and Fields to Share to be directly linked to Action ID,
    /// enabling the Inbound Settings and Outbound Settings pages to filter by action.
    /// </summary>
    public static class Migration_20251124000003_AddActionIdToFieldMappings
    {
        public static void Up(ISqlSugarClient db)
        {
            Console.WriteLine("Starting migration: Add action_id to Field Mapping and Outbound Field Config tables");

            // 1. Add action_id to ff_field_mapping table
            var fieldMappingColumnExists = db.Ado.GetDataTable(@"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = 'ff_field_mapping' 
                AND column_name = 'action_id'
            ").Rows.Count > 0;

            if (!fieldMappingColumnExists)
            {
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_field_mapping 
                    ADD COLUMN action_id BIGINT;
                ");

                // Create index for action_id
                db.Ado.ExecuteCommand(@"
                    CREATE INDEX IF NOT EXISTS idx_field_mapping_action_id 
                    ON ff_field_mapping(action_id);
                ");

                Console.WriteLine("✓ Added action_id column to ff_field_mapping table");
            }
            else
            {
                Console.WriteLine("✓ Column 'action_id' already exists in ff_field_mapping table, skipping");
            }

            // 2. Check if ff_outbound_field_config table exists, if not create it
            var outboundFieldConfigTableExists = db.Ado.GetDataTable(@"
                SELECT table_name 
                FROM information_schema.tables 
                WHERE table_name = 'ff_outbound_field_config'
            ").Rows.Count > 0;

            if (!outboundFieldConfigTableExists)
            {
                // Create ff_outbound_field_config table
                db.Ado.ExecuteCommand(@"
                    CREATE TABLE IF NOT EXISTS ff_outbound_field_config (
                        id BIGINT NOT NULL PRIMARY KEY,
                        tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                        app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT',
                        outbound_configuration_id BIGINT NOT NULL,
                        action_id BIGINT,
                        wfe_field_id VARCHAR(100) NOT NULL,
                        external_field_name VARCHAR(100) NOT NULL,
                        sort_order INTEGER DEFAULT 0,
                        is_required BOOLEAN DEFAULT FALSE,
                        is_valid BOOLEAN DEFAULT TRUE,
                        create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                        modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                        create_by VARCHAR(50) DEFAULT 'SYSTEM',
                        modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                        create_user_id BIGINT DEFAULT 0,
                        modify_user_id BIGINT DEFAULT 0,
                        CONSTRAINT fk_outbound_field_config_outbound_configuration FOREIGN KEY (outbound_configuration_id) 
                            REFERENCES ff_outbound_configuration(id) ON DELETE CASCADE
                    );

                    CREATE INDEX IF NOT EXISTS idx_outbound_field_config_outbound_configuration_id 
                        ON ff_outbound_field_config(outbound_configuration_id);
                    CREATE INDEX IF NOT EXISTS idx_outbound_field_config_action_id 
                        ON ff_outbound_field_config(action_id);
                    CREATE INDEX IF NOT EXISTS idx_outbound_field_config_wfe_field_id 
                        ON ff_outbound_field_config(wfe_field_id);
                ");

                Console.WriteLine("✓ Created ff_outbound_field_config table with action_id column");
            }
            else
            {
                // Table exists, check if action_id column exists
                var outboundFieldConfigColumnExists = db.Ado.GetDataTable(@"
                    SELECT column_name 
                    FROM information_schema.columns 
                    WHERE table_name = 'ff_outbound_field_config' 
                    AND column_name = 'action_id'
                ").Rows.Count > 0;

                if (!outboundFieldConfigColumnExists)
                {
                    db.Ado.ExecuteCommand(@"
                        ALTER TABLE ff_outbound_field_config 
                        ADD COLUMN action_id BIGINT;
                    ");

                    // Create index for action_id
                    db.Ado.ExecuteCommand(@"
                        CREATE INDEX IF NOT EXISTS idx_outbound_field_config_action_id 
                        ON ff_outbound_field_config(action_id);
                    ");

                    Console.WriteLine("✓ Added action_id column to ff_outbound_field_config table");
                }
                else
                {
                    Console.WriteLine("✓ Column 'action_id' already exists in ff_outbound_field_config table, skipping");
                }
            }

            Console.WriteLine("Migration completed: Add action_id to Field Mapping and Outbound Field Config tables");
        }

        public static void Down(ISqlSugarClient db)
        {
            Console.WriteLine("Starting rollback: Remove action_id from Field Mapping and Outbound Field Config tables");

            // 1. Remove action_id from ff_field_mapping table
            var fieldMappingColumnExists = db.Ado.GetDataTable(@"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = 'ff_field_mapping' 
                AND column_name = 'action_id'
            ").Rows.Count > 0;

            if (fieldMappingColumnExists)
            {
                // Drop index first
                db.Ado.ExecuteCommand(@"
                    DROP INDEX IF EXISTS idx_field_mapping_action_id;
                ");

                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_field_mapping 
                    DROP COLUMN action_id;
                ");

                Console.WriteLine("✓ Removed action_id column from ff_field_mapping table");
            }
            else
            {
                Console.WriteLine("✓ Column 'action_id' does not exist in ff_field_mapping table, skipping");
            }

            // 2. Remove action_id from ff_outbound_field_config table
            var outboundFieldConfigTableExists = db.Ado.GetDataTable(@"
                SELECT table_name 
                FROM information_schema.tables 
                WHERE table_name = 'ff_outbound_field_config'
            ").Rows.Count > 0;

            if (outboundFieldConfigTableExists)
            {
                var outboundFieldConfigColumnExists = db.Ado.GetDataTable(@"
                    SELECT column_name 
                    FROM information_schema.columns 
                    WHERE table_name = 'ff_outbound_field_config' 
                    AND column_name = 'action_id'
                ").Rows.Count > 0;

                if (outboundFieldConfigColumnExists)
                {
                    // Drop index first
                    db.Ado.ExecuteCommand(@"
                        DROP INDEX IF EXISTS idx_outbound_field_config_action_id;
                    ");

                    db.Ado.ExecuteCommand(@"
                        ALTER TABLE ff_outbound_field_config 
                        DROP COLUMN action_id;
                    ");

                    Console.WriteLine("✓ Removed action_id column from ff_outbound_field_config table");
                }
                else
                {
                    Console.WriteLine("✓ Column 'action_id' does not exist in ff_outbound_field_config table, skipping");
                }
            }
            else
            {
                Console.WriteLine("✓ Table 'ff_outbound_field_config' does not exist, skipping");
            }

            Console.WriteLine("Rollback completed: Remove action_id from Field Mapping and Outbound Field Config tables");
        }
    }
}

