using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add system_id field to EntityMapping table
    /// Migration: 20251127000001_AddSystemIdToEntityMapping
    /// Date: 2025-11-27
    /// 
    /// This migration adds a system_id field to store external system identifier 
    /// for entity mappings.
    /// </summary>
    public static class Migration_20251127000001_AddSystemIdToEntityMapping
    {
        public static void Up(ISqlSugarClient db)
        {
            Console.WriteLine("Starting migration: Add system_id to EntityMapping table");

            // Check if column already exists
            var columnExists = db.Ado.GetDataTable(@"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = 'ff_entity_mapping' 
                AND column_name = 'system_id'
            ").Rows.Count > 0;

            if (!columnExists)
            {
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_entity_mapping 
                    ADD COLUMN system_id VARCHAR(200);
                ");

                Console.WriteLine("Added system_id column to ff_entity_mapping table");
            }
            else
            {
                Console.WriteLine("Column 'system_id' already exists in ff_entity_mapping table, skipping");
            }

            Console.WriteLine("Migration completed: Add system_id to EntityMapping table");
        }

        public static void Down(ISqlSugarClient db)
        {
            Console.WriteLine("Starting rollback: Remove system_id from EntityMapping table");

            var columnExists = db.Ado.GetDataTable(@"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_name = 'ff_entity_mapping' 
                AND column_name = 'system_id'
            ").Rows.Count > 0;

            if (columnExists)
            {
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_entity_mapping 
                    DROP COLUMN system_id;
                ");

                Console.WriteLine("Removed system_id column from ff_entity_mapping table");
            }
            else
            {
                Console.WriteLine("Column 'system_id' does not exist in ff_entity_mapping table, skipping");
            }

            Console.WriteLine("Rollback completed: Remove system_id from EntityMapping table");
        }
    }
}


