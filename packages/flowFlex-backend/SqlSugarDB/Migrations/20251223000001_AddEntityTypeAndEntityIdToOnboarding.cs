using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add EntityType and EntityId fields to ff_onboarding table
    /// Migration: 20251223000001_AddEntityTypeAndEntityIdToOnboarding
    /// Date: 2025-12-23
    /// 
    /// This migration adds the following fields to Onboarding entity:
    /// 1. entity_type - External Entity Type (e.g., "lead", "customer", "account") (VARCHAR(100), nullable)
    /// 2. entity_id - External Entity ID from external integration (VARCHAR(100), nullable)
    /// 
    /// These fields enable tracking of external system entity information:
    /// - Identifying the type of entity from external system (lead, customer, account, etc.)
    /// - Storing the original entity ID from external system
    /// - Supporting queries and filtering by entity type and ID
    /// </summary>
    public static class Migration_20251223000001_AddEntityTypeAndEntityIdToOnboarding
    {
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("Starting migration: 20251223000001_AddEntityTypeAndEntityIdToOnboarding");

                // 1. Add entity_type column (VARCHAR(100), nullable)
                Console.WriteLine("Adding entity_type column...");
                db.Ado.ExecuteCommand(@"
                    DO $
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ff_onboarding' 
                            AND column_name = 'entity_type'
                        ) THEN
                            ALTER TABLE ff_onboarding 
                            ADD COLUMN entity_type VARCHAR(100) NULL;
                            
                            COMMENT ON COLUMN ff_onboarding.entity_type IS 'External Entity Type (e.g., lead, customer, account) from external integration';
                        END IF;
                    END $;
                ");

                // 2. Add entity_id column (VARCHAR(100), nullable)
                Console.WriteLine("Adding entity_id column...");
                db.Ado.ExecuteCommand(@"
                    DO $
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ff_onboarding' 
                            AND column_name = 'entity_id'
                        ) THEN
                            ALTER TABLE ff_onboarding 
                            ADD COLUMN entity_id VARCHAR(100) NULL;
                            
                            COMMENT ON COLUMN ff_onboarding.entity_id IS 'External Entity ID from external integration (e.g., Lead ID, Customer ID)';
                        END IF;
                    END $;
                ");

                // 3. Create index for entity_type field for better query performance
                Console.WriteLine("Creating index on entity_type column...");
                db.Ado.ExecuteCommand(@"
                    DO $
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM pg_indexes 
                            WHERE tablename = 'ff_onboarding' 
                            AND indexname = 'idx_ff_onboarding_entity_type'
                        ) THEN
                            CREATE INDEX idx_ff_onboarding_entity_type ON ff_onboarding(entity_type);
                        END IF;
                    END $;
                ");

                // 4. Create index for entity_id field for better query performance
                Console.WriteLine("Creating index on entity_id column...");
                db.Ado.ExecuteCommand(@"
                    DO $
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM pg_indexes 
                            WHERE tablename = 'ff_onboarding' 
                            AND indexname = 'idx_ff_onboarding_entity_id'
                        ) THEN
                            CREATE INDEX idx_ff_onboarding_entity_id ON ff_onboarding(entity_id);
                        END IF;
                    END $;
                ");

                // 5. Create composite index for entity_type and entity_id for better query performance
                Console.WriteLine("Creating composite index on entity_type and entity_id columns...");
                db.Ado.ExecuteCommand(@"
                    DO $
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM pg_indexes 
                            WHERE tablename = 'ff_onboarding' 
                            AND indexname = 'idx_ff_onboarding_entity_type_id'
                        ) THEN
                            CREATE INDEX idx_ff_onboarding_entity_type_id ON ff_onboarding(entity_type, entity_id);
                        END IF;
                    END $;
                ");

                Console.WriteLine("Migration 20251223000001_AddEntityTypeAndEntityIdToOnboarding completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in migration 20251223000001_AddEntityTypeAndEntityIdToOnboarding: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("Rolling back migration: 20251223000001_AddEntityTypeAndEntityIdToOnboarding");

                // Drop indexes
                Console.WriteLine("Dropping index idx_ff_onboarding_entity_type_id...");
                db.Ado.ExecuteCommand(@"
                    DROP INDEX IF EXISTS idx_ff_onboarding_entity_type_id;
                ");

                Console.WriteLine("Dropping index idx_ff_onboarding_entity_id...");
                db.Ado.ExecuteCommand(@"
                    DROP INDEX IF EXISTS idx_ff_onboarding_entity_id;
                ");

                Console.WriteLine("Dropping index idx_ff_onboarding_entity_type...");
                db.Ado.ExecuteCommand(@"
                    DROP INDEX IF EXISTS idx_ff_onboarding_entity_type;
                ");

                // Drop entity_id column
                Console.WriteLine("Dropping entity_id column...");
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_onboarding DROP COLUMN IF EXISTS entity_id;
                ");

                // Drop entity_type column
                Console.WriteLine("Dropping entity_type column...");
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_onboarding DROP COLUMN IF EXISTS entity_type;
                ");

                Console.WriteLine("Migration 20251223000001_AddEntityTypeAndEntityIdToOnboarding rolled back successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rolling back migration 20251223000001_AddEntityTypeAndEntityIdToOnboarding: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}
