using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Migration to rename ff_field_mapping to ff_inbound_field_mapping and simplify columns
    /// </summary>
    public class Migration_20251127000004_RenameFieldMappingTable
    {
        public static void Up(ISqlSugarClient db)
        {
            Console.WriteLine("[Migration] Processing ff_inbound_field_mapping table...");

            try
            {
                // Check if old table exists
                var oldTableExists = db.Ado.GetInt(@"
                    SELECT COUNT(*) FROM information_schema.tables 
                    WHERE table_name = 'ff_field_mapping'") > 0;

                // Check if new table already exists
                var newTableExists = db.Ado.GetInt(@"
                    SELECT COUNT(*) FROM information_schema.tables 
                    WHERE table_name = 'ff_inbound_field_mapping'") > 0;

                if (newTableExists)
                {
                    Console.WriteLine("[Migration] Table ff_inbound_field_mapping already exists, skipping");

                    // Still need to drop old columns if they exist
                    DropOldColumnsIfExist(db);
                }
                else if (oldTableExists)
                {
                    // Rename the table
                    db.Ado.ExecuteCommand(@"ALTER TABLE ff_field_mapping RENAME TO ff_inbound_field_mapping;");
                    Console.WriteLine("[Migration] Renamed ff_field_mapping to ff_inbound_field_mapping");

                    // Drop old columns
                    DropOldColumnsIfExist(db);

                    // Create new indexes
                    CreateIndexes(db);
                }
                else
                {
                    // Neither table exists, create new table
                    Console.WriteLine("[Migration] Creating new ff_inbound_field_mapping table...");

                    db.Ado.ExecuteCommand(@"
                        CREATE TABLE IF NOT EXISTS ff_inbound_field_mapping (
                            id BIGINT PRIMARY KEY,
                            integration_id BIGINT NOT NULL,
                            action_id BIGINT,
                            external_field_name VARCHAR(100) NOT NULL,
                            wfe_field_id VARCHAR(100) NOT NULL,
                            field_type INTEGER NOT NULL DEFAULT 0,
                            sync_direction INTEGER NOT NULL DEFAULT 0,
                            sort_order INTEGER NOT NULL DEFAULT 0,
                            is_required BOOLEAN NOT NULL DEFAULT FALSE,
                            default_value VARCHAR(500),
                            create_date TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                            modify_date TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                            create_by VARCHAR(100),
                            modify_by VARCHAR(100),
                            create_user_id BIGINT,
                            modify_user_id BIGINT,
                            is_valid BOOLEAN NOT NULL DEFAULT TRUE,
                            tenant_id BIGINT NOT NULL,
                            app_code VARCHAR(50)
                        );
                    ");

                    Console.WriteLine("[Migration] Created ff_inbound_field_mapping table");

                    // Create indexes
                    CreateIndexes(db);
                }

                Console.WriteLine("[Migration] ff_inbound_field_mapping table migration completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Migration] Error during migration: {ex.Message}");
                throw;
            }
        }

        private static void DropOldColumnsIfExist(ISqlSugarClient db)
        {
            // Drop entity_mapping_id column if exists
            var hasEntityMappingId = db.Ado.GetInt(@"
                SELECT COUNT(*) FROM information_schema.columns 
                WHERE table_name = 'ff_inbound_field_mapping' AND column_name = 'entity_mapping_id'") > 0;

            if (hasEntityMappingId)
            {
                db.Ado.ExecuteCommand(@"ALTER TABLE ff_inbound_field_mapping DROP COLUMN entity_mapping_id;");
                Console.WriteLine("[Migration] Dropped column entity_mapping_id");
            }

            // Drop transform_rules column if exists
            var hasTransformRules = db.Ado.GetInt(@"
                SELECT COUNT(*) FROM information_schema.columns 
                WHERE table_name = 'ff_inbound_field_mapping' AND column_name = 'transform_rules'") > 0;

            if (hasTransformRules)
            {
                db.Ado.ExecuteCommand(@"ALTER TABLE ff_inbound_field_mapping DROP COLUMN transform_rules;");
                Console.WriteLine("[Migration] Dropped column transform_rules");
            }

            // Drop old indexes if they exist
            try
            {
                db.Ado.ExecuteCommand(@"DROP INDEX IF EXISTS idx_field_mapping_integration_id;");
                db.Ado.ExecuteCommand(@"DROP INDEX IF EXISTS idx_field_mapping_entity_mapping_id;");
                db.Ado.ExecuteCommand(@"DROP INDEX IF EXISTS idx_field_mapping_action_id;");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Migration] Warning: Could not drop old indexes: {ex.Message}");
            }
        }

        private static void CreateIndexes(ISqlSugarClient db)
        {
            db.Ado.ExecuteCommand(@"
                CREATE INDEX IF NOT EXISTS idx_inbound_field_mapping_integration_id 
                ON ff_inbound_field_mapping(integration_id);");

            db.Ado.ExecuteCommand(@"
                CREATE INDEX IF NOT EXISTS idx_inbound_field_mapping_action_id 
                ON ff_inbound_field_mapping(action_id);");

            db.Ado.ExecuteCommand(@"
                CREATE INDEX IF NOT EXISTS idx_inbound_field_mapping_integration_action 
                ON ff_inbound_field_mapping(integration_id, action_id);");

            Console.WriteLine("[Migration] Created indexes for ff_inbound_field_mapping");
        }

        public static void Down(ISqlSugarClient db)
        {
            Console.WriteLine("[Migration] Rolling back ff_inbound_field_mapping table migration...");

            try
            {
                // Check if new table exists
                var newTableExists = db.Ado.GetInt(@"
                    SELECT COUNT(*) FROM information_schema.tables 
                    WHERE table_name = 'ff_inbound_field_mapping'") > 0;

                if (newTableExists)
                {
                    // Add back the removed columns
                    db.Ado.ExecuteCommand(@"
                        ALTER TABLE ff_inbound_field_mapping 
                        ADD COLUMN IF NOT EXISTS entity_mapping_id BIGINT;");

                    db.Ado.ExecuteCommand(@"
                        ALTER TABLE ff_inbound_field_mapping 
                        ADD COLUMN IF NOT EXISTS transform_rules TEXT DEFAULT '{}';");

                    // Rename back to original name
                    db.Ado.ExecuteCommand(@"ALTER TABLE ff_inbound_field_mapping RENAME TO ff_field_mapping;");

                    Console.WriteLine("[Migration] Rolled back to ff_field_mapping");
                }

                Console.WriteLine("[Migration] Rollback completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Migration] Error during rollback: {ex.Message}");
                throw;
            }
        }
    }
}
