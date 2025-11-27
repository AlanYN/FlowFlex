using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Migration to remove integration_id, data_direction_inbound, data_direction_outbound from ff_action_definitions
    /// Integration association is now handled through ff_action_trigger_mappings (TriggerType = 'Integration')
    /// </summary>
    public class Migration_20251127000005_RemoveIntegrationIdFromActionDefinitions
    {
        public static void Up(ISqlSugarClient db)
        {
            Console.WriteLine("[Migration] Removing integration fields from ff_action_definitions...");

            try
            {
                // Drop integration_id column if exists
                var hasIntegrationId = db.Ado.GetInt(@"
                    SELECT COUNT(*) FROM information_schema.columns 
                    WHERE table_name = 'ff_action_definitions' AND column_name = 'integration_id'") > 0;

                if (hasIntegrationId)
                {
                    // Drop index first if exists
                    db.Ado.ExecuteCommand(@"DROP INDEX IF EXISTS idx_action_definitions_integration_id;");
                    db.Ado.ExecuteCommand(@"ALTER TABLE ff_action_definitions DROP COLUMN integration_id;");
                    Console.WriteLine("[Migration] Dropped column integration_id");
                }

                // Drop data_direction_inbound column if exists
                var hasDataDirectionInbound = db.Ado.GetInt(@"
                    SELECT COUNT(*) FROM information_schema.columns 
                    WHERE table_name = 'ff_action_definitions' AND column_name = 'data_direction_inbound'") > 0;

                if (hasDataDirectionInbound)
                {
                    db.Ado.ExecuteCommand(@"ALTER TABLE ff_action_definitions DROP COLUMN data_direction_inbound;");
                    Console.WriteLine("[Migration] Dropped column data_direction_inbound");
                }

                // Drop data_direction_outbound column if exists
                var hasDataDirectionOutbound = db.Ado.GetInt(@"
                    SELECT COUNT(*) FROM information_schema.columns 
                    WHERE table_name = 'ff_action_definitions' AND column_name = 'data_direction_outbound'") > 0;

                if (hasDataDirectionOutbound)
                {
                    // Drop composite index first if exists
                    db.Ado.ExecuteCommand(@"DROP INDEX IF EXISTS idx_action_definitions_data_direction;");
                    db.Ado.ExecuteCommand(@"ALTER TABLE ff_action_definitions DROP COLUMN data_direction_outbound;");
                    Console.WriteLine("[Migration] Dropped column data_direction_outbound");
                }

                Console.WriteLine("[Migration] Successfully removed integration fields from ff_action_definitions");
                Console.WriteLine("[Migration] Note: Integration association is now handled through ff_action_trigger_mappings (TriggerType = 'Integration')");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Migration] Error during migration: {ex.Message}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            Console.WriteLine("[Migration] Rolling back - adding integration fields to ff_action_definitions...");

            try
            {
                // Add integration_id column
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_action_definitions 
                    ADD COLUMN IF NOT EXISTS integration_id BIGINT;");

                db.Ado.ExecuteCommand(@"
                    CREATE INDEX IF NOT EXISTS idx_action_definitions_integration_id 
                    ON ff_action_definitions(integration_id);");

                // Add data_direction_inbound column
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_action_definitions 
                    ADD COLUMN IF NOT EXISTS data_direction_inbound BOOLEAN DEFAULT FALSE;");

                // Add data_direction_outbound column
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_action_definitions 
                    ADD COLUMN IF NOT EXISTS data_direction_outbound BOOLEAN DEFAULT FALSE;");

                db.Ado.ExecuteCommand(@"
                    CREATE INDEX IF NOT EXISTS idx_action_definitions_data_direction 
                    ON ff_action_definitions(data_direction_inbound, data_direction_outbound);");

                Console.WriteLine("[Migration] Rollback completed - added integration fields back");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Migration] Error during rollback: {ex.Message}");
                throw;
            }
        }
    }
}

