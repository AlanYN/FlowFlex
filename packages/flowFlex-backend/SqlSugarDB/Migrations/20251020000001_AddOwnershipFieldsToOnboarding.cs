using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add ownership fields to ff_onboarding table
    /// Migration: 20251020000001_AddOwnershipFieldsToOnboarding
    /// Date: 2025-10-20
    /// 
    /// This migration adds the following ownership fields to Onboarding entity:
    /// 1. ownership - User ID who owns this onboarding (BIGINT, nullable)
    /// 2. ownership_name - User name who owns this onboarding (VARCHAR(100), nullable)
    /// 3. ownership_email - User email who owns this onboarding (VARCHAR(200), nullable)
    /// 
    /// These fields enable ownership tracking for onboarding records, including:
    /// - Identifying the owner of each onboarding
    /// - Filtering and querying by ownership
    /// - Supporting ownership-based permission control
    /// - Displaying ownership information in UI and exports
    /// </summary>
    public static class Migration_20251020000001_AddOwnershipFieldsToOnboarding
    {
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("Starting migration: 20251020000001_AddOwnershipFieldsToOnboarding");

                // 1. Add ownership column (BIGINT, nullable)
                Console.WriteLine("Adding ownership column...");
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ff_onboarding' 
                            AND column_name = 'ownership'
                        ) THEN
                            ALTER TABLE ff_onboarding 
                            ADD COLUMN ownership BIGINT NULL;
                            
                            COMMENT ON COLUMN ff_onboarding.ownership IS 'User ID who owns this onboarding';
                        END IF;
                    END $$;
                ");

                // 2. Add ownership_name column (VARCHAR(100), nullable)
                Console.WriteLine("Adding ownership_name column...");
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ff_onboarding' 
                            AND column_name = 'ownership_name'
                        ) THEN
                            ALTER TABLE ff_onboarding 
                            ADD COLUMN ownership_name VARCHAR(100) NULL;
                            
                            COMMENT ON COLUMN ff_onboarding.ownership_name IS 'User name who owns this onboarding';
                        END IF;
                    END $$;
                ");

                // 3. Add ownership_email column (VARCHAR(200), nullable)
                Console.WriteLine("Adding ownership_email column...");
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ff_onboarding' 
                            AND column_name = 'ownership_email'
                        ) THEN
                            ALTER TABLE ff_onboarding 
                            ADD COLUMN ownership_email VARCHAR(200) NULL;
                            
                            COMMENT ON COLUMN ff_onboarding.ownership_email IS 'User email who owns this onboarding';
                        END IF;
                    END $$;
                ");

                // 4. Create index for ownership field for better query performance
                Console.WriteLine("Creating index on ownership column...");
                db.Ado.ExecuteCommand(@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM pg_indexes 
                            WHERE tablename = 'ff_onboarding' 
                            AND indexname = 'idx_ff_onboarding_ownership'
                        ) THEN
                            CREATE INDEX idx_ff_onboarding_ownership ON ff_onboarding(ownership);
                        END IF;
                    END $$;
                ");

                Console.WriteLine("Migration 20251020000001_AddOwnershipFieldsToOnboarding completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in migration 20251020000001_AddOwnershipFieldsToOnboarding: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("Rolling back migration: 20251020000001_AddOwnershipFieldsToOnboarding");

                // Drop index
                Console.WriteLine("Dropping index idx_ff_onboarding_ownership...");
                db.Ado.ExecuteCommand(@"
                    DROP INDEX IF EXISTS idx_ff_onboarding_ownership;
                ");

                // Drop ownership_email column
                Console.WriteLine("Dropping ownership_email column...");
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_onboarding DROP COLUMN IF EXISTS ownership_email;
                ");

                // Drop ownership_name column
                Console.WriteLine("Dropping ownership_name column...");
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_onboarding DROP COLUMN IF EXISTS ownership_name;
                ");

                // Drop ownership column
                Console.WriteLine("Dropping ownership column...");
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_onboarding DROP COLUMN IF EXISTS ownership;
                ");

                Console.WriteLine("Migration 20251020000001_AddOwnershipFieldsToOnboarding rolled back successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rolling back migration 20251020000001_AddOwnershipFieldsToOnboarding: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}

