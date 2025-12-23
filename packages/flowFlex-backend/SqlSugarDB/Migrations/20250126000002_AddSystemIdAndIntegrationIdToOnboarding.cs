using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add SystemId and IntegrationId fields to ff_onboarding table
    /// Migration: 20250126000002_AddSystemIdAndIntegrationIdToOnboarding
    /// Date: 2025-01-26
    /// 
    /// This migration adds the following fields to Onboarding entity:
    /// 1. system_id - System ID from external integration (Entity Mapping System ID) (VARCHAR(100), nullable)
    /// 2. integration_id - Integration ID from external integration (BIGINT, nullable)
    /// 
    /// These fields enable integration tracking for onboarding records, including:
    /// - Identifying which external system integration created the onboarding
    /// - Linking onboardings to specific entity mappings via System ID
    /// - Filtering and querying onboardings by integration source
    /// - Supporting inbound attachment queries by System ID
    /// </summary>
    public static class Migration_20250126000002_AddSystemIdAndIntegrationIdToOnboarding
    {
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("Starting migration: 20250126000002_AddSystemIdAndIntegrationIdToOnboarding");

                // 1. Add system_id column (VARCHAR(100), nullable)
                Console.WriteLine("Adding system_id column...");
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ff_onboarding' 
                            AND column_name = 'system_id'
                        ) THEN
                            ALTER TABLE ff_onboarding 
                            ADD COLUMN system_id VARCHAR(100) NULL;
                            
                            COMMENT ON COLUMN ff_onboarding.system_id IS 'System ID from external integration (Entity Mapping System ID)';
                        END IF;
                    END $$;
                ");

                // 2. Add integration_id column (BIGINT, nullable)
                Console.WriteLine("Adding integration_id column...");
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ff_onboarding' 
                            AND column_name = 'integration_id'
                        ) THEN
                            ALTER TABLE ff_onboarding 
                            ADD COLUMN integration_id BIGINT NULL;
                            
                            COMMENT ON COLUMN ff_onboarding.integration_id IS 'Integration ID from external integration';
                        END IF;
                    END $$;
                ");

                // 3. Create index for system_id field for better query performance
                Console.WriteLine("Creating index on system_id column...");
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM pg_indexes 
                            WHERE tablename = 'ff_onboarding' 
                            AND indexname = 'idx_ff_onboarding_system_id'
                        ) THEN
                            CREATE INDEX idx_ff_onboarding_system_id ON ff_onboarding(system_id);
                        END IF;
                    END $$;
                ");

                // 4. Create index for integration_id field for better query performance
                Console.WriteLine("Creating index on integration_id column...");
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM pg_indexes 
                            WHERE tablename = 'ff_onboarding' 
                            AND indexname = 'idx_ff_onboarding_integration_id'
                        ) THEN
                            CREATE INDEX idx_ff_onboarding_integration_id ON ff_onboarding(integration_id);
                        END IF;
                    END $$;
                ");

                Console.WriteLine("Migration 20250126000002_AddSystemIdAndIntegrationIdToOnboarding completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in migration 20250126000002_AddSystemIdAndIntegrationIdToOnboarding: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("Rolling back migration: 20250126000002_AddSystemIdAndIntegrationIdToOnboarding");

                // Drop indexes
                Console.WriteLine("Dropping index idx_ff_onboarding_integration_id...");
                db.Ado.ExecuteCommand(@"
                    DROP INDEX IF EXISTS idx_ff_onboarding_integration_id;
                ");

                Console.WriteLine("Dropping index idx_ff_onboarding_system_id...");
                db.Ado.ExecuteCommand(@"
                    DROP INDEX IF EXISTS idx_ff_onboarding_system_id;
                ");

                // Drop integration_id column
                Console.WriteLine("Dropping integration_id column...");
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_onboarding DROP COLUMN IF EXISTS integration_id;
                ");

                // Drop system_id column
                Console.WriteLine("Dropping system_id column...");
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_onboarding DROP COLUMN IF EXISTS system_id;
                ");

                Console.WriteLine("Migration 20250126000002_AddSystemIdAndIntegrationIdToOnboarding rolled back successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rolling back migration 20250126000002_AddSystemIdAndIntegrationIdToOnboarding: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}

