using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add Integration association fields to Action Definitions table
    /// Migration: 20251127000002_AddIntegrationFieldsToActionDefinitions
    /// Date: 2025-11-27
    /// 
    /// This migration adds fields to ff_action_definitions table:
    /// 1. integration_id - associates this action with a specific integration
    /// 2. data_direction_inbound - whether this action receives data from external system
    /// 3. data_direction_outbound - whether this action sends data to external system
    /// 
    /// These fields enable Action to be linked with Integration for Inbound/Outbound Settings configuration.
    /// </summary>
    public static class Migration_20251127000002_AddIntegrationFieldsToActionDefinitions
    {
        public static void Up(ISqlSugarClient db)
        {
            Console.WriteLine("Starting migration: Add Integration fields to Action Definitions table");

            // 1. Add integration_id column
            var integrationIdColumnExists = db.Ado.GetDataTable(@"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = 'ff_action_definitions' 
                AND column_name = 'integration_id'
            ").Rows.Count > 0;

            if (!integrationIdColumnExists)
            {
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_action_definitions 
                    ADD COLUMN integration_id BIGINT;
                ");

                // Create index for integration_id
                db.Ado.ExecuteCommand(@"
                    CREATE INDEX IF NOT EXISTS idx_action_definitions_integration_id 
                    ON ff_action_definitions(integration_id) 
                    WHERE integration_id IS NOT NULL;
                ");

                Console.WriteLine("✓ Added integration_id column to ff_action_definitions table");
            }
            else
            {
                Console.WriteLine("✓ Column 'integration_id' already exists in ff_action_definitions table, skipping");
            }

            // 2. Add data_direction_inbound column
            var inboundColumnExists = db.Ado.GetDataTable(@"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = 'ff_action_definitions' 
                AND column_name = 'data_direction_inbound'
            ").Rows.Count > 0;

            if (!inboundColumnExists)
            {
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_action_definitions 
                    ADD COLUMN data_direction_inbound BOOLEAN DEFAULT FALSE;
                ");

                Console.WriteLine("✓ Added data_direction_inbound column to ff_action_definitions table");
            }
            else
            {
                Console.WriteLine("✓ Column 'data_direction_inbound' already exists in ff_action_definitions table, skipping");
            }

            // 3. Add data_direction_outbound column
            var outboundColumnExists = db.Ado.GetDataTable(@"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = 'ff_action_definitions' 
                AND column_name = 'data_direction_outbound'
            ").Rows.Count > 0;

            if (!outboundColumnExists)
            {
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_action_definitions 
                    ADD COLUMN data_direction_outbound BOOLEAN DEFAULT FALSE;
                ");

                Console.WriteLine("✓ Added data_direction_outbound column to ff_action_definitions table");
            }
            else
            {
                Console.WriteLine("✓ Column 'data_direction_outbound' already exists in ff_action_definitions table, skipping");
            }

            // 4. Create composite index for data direction queries
            db.Ado.ExecuteCommand(@"
                CREATE INDEX IF NOT EXISTS idx_action_definitions_data_direction 
                ON ff_action_definitions(data_direction_inbound, data_direction_outbound) 
                WHERE data_direction_inbound = true OR data_direction_outbound = true;
            ");

            Console.WriteLine("Migration completed: Add Integration fields to Action Definitions table");
        }

        public static void Down(ISqlSugarClient db)
        {
            Console.WriteLine("Starting rollback: Remove Integration fields from Action Definitions table");

            // 1. Remove integration_id column
            var integrationIdColumnExists = db.Ado.GetDataTable(@"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = 'ff_action_definitions' 
                AND column_name = 'integration_id'
            ").Rows.Count > 0;

            if (integrationIdColumnExists)
            {
                // Drop index first
                db.Ado.ExecuteCommand(@"
                    DROP INDEX IF EXISTS idx_action_definitions_integration_id;
                ");

                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_action_definitions 
                    DROP COLUMN integration_id;
                ");

                Console.WriteLine("✓ Removed integration_id column from ff_action_definitions table");
            }
            else
            {
                Console.WriteLine("✓ Column 'integration_id' does not exist in ff_action_definitions table, skipping");
            }

            // 2. Remove data_direction_inbound column
            var inboundColumnExists = db.Ado.GetDataTable(@"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = 'ff_action_definitions' 
                AND column_name = 'data_direction_inbound'
            ").Rows.Count > 0;

            if (inboundColumnExists)
            {
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_action_definitions 
                    DROP COLUMN data_direction_inbound;
                ");

                Console.WriteLine("✓ Removed data_direction_inbound column from ff_action_definitions table");
            }
            else
            {
                Console.WriteLine("✓ Column 'data_direction_inbound' does not exist in ff_action_definitions table, skipping");
            }

            // 3. Remove data_direction_outbound column
            var outboundColumnExists = db.Ado.GetDataTable(@"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = 'ff_action_definitions' 
                AND column_name = 'data_direction_outbound'
            ").Rows.Count > 0;

            if (outboundColumnExists)
            {
                // Drop composite index first
                db.Ado.ExecuteCommand(@"
                    DROP INDEX IF EXISTS idx_action_definitions_data_direction;
                ");

                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_action_definitions 
                    DROP COLUMN data_direction_outbound;
                ");

                Console.WriteLine("✓ Removed data_direction_outbound column from ff_action_definitions table");
            }
            else
            {
                Console.WriteLine("✓ Column 'data_direction_outbound' does not exist in ff_action_definitions table, skipping");
            }

            Console.WriteLine("Rollback completed: Remove Integration fields from Action Definitions table");
        }
    }
}


