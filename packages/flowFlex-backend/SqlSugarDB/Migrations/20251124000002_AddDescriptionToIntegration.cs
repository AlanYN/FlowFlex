using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add Description field to Integration table
    /// Migration: 20251124000002_AddDescriptionToIntegration
    /// Date: 2025-11-24
    /// 
    /// This migration adds a description field to the ff_integration table
    /// to allow users to provide additional information about the integration.
    /// 
    /// Note: IntegrationOutputDto also includes ConfiguredEntityTypeNames field
    /// which is computed from EntityMapping.WfeEntityType and does not require
    /// a database column.
    /// </summary>
    public static class Migration_20251124000002_AddDescriptionToIntegration
    {
        public static void Up(ISqlSugarClient db)
        {
            Console.WriteLine("Starting migration: Add Description to Integration table");

            // Check if column already exists
            var columnExists = db.Ado.GetDataTable(@"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = 'ff_integration' 
                AND column_name = 'description'
            ").Rows.Count > 0;

            if (!columnExists)
            {
                // Add description column to ff_integration table
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_integration 
                    ADD COLUMN description VARCHAR(500);
                ");

                Console.WriteLine("✓ Added description column to ff_integration table");
            }
            else
            {
                Console.WriteLine("✓ Column 'description' already exists in ff_integration table, skipping");
            }

            Console.WriteLine("Migration completed: Add Description to Integration table");
        }

        public static void Down(ISqlSugarClient db)
        {
            Console.WriteLine("Starting rollback: Remove Description from Integration table");

            // Check if column exists before dropping
            var columnExists = db.Ado.GetDataTable(@"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = 'ff_integration' 
                AND column_name = 'description'
            ").Rows.Count > 0;

            if (columnExists)
            {
                // Remove description column from ff_integration table
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_integration 
                    DROP COLUMN description;
                ");

                Console.WriteLine("✓ Removed description column from ff_integration table");
            }
            else
            {
                Console.WriteLine("✓ Column 'description' does not exist in ff_integration table, skipping");
            }

            Console.WriteLine("Rollback completed: Remove Description from Integration table");
        }
    }
}

