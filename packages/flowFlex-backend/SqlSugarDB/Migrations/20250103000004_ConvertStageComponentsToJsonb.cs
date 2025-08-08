using System;
using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Convert ff_stage.components_json column to JSONB for better performance and indexing
    /// </summary>
    public class ConvertStageComponentsToJsonb_20250103000004
    {
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("[ConvertStageComponentsToJsonb] Starting migration...");

                // Check column existence
                var columnExists = db.Ado.GetDataTable(@"
                    SELECT 1 
                    FROM information_schema.columns 
                    WHERE table_name = 'ff_stage' 
                      AND column_name = 'components_json'
                ").Rows.Count > 0;

                if (!columnExists)
                {
                    Console.WriteLine("[ConvertStageComponentsToJsonb] Column components_json not found, skipping.");
                    return;
                }

                // Convert to jsonb with safe USING clause
                Console.WriteLine("[ConvertStageComponentsToJsonb] Converting ff_stage.components_json to jsonb...");
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_stage 
                    ALTER COLUMN components_json TYPE jsonb 
                    USING CASE 
                        WHEN components_json IS NULL OR components_json = '' THEN NULL 
                        ELSE components_json::jsonb 
                    END;
                ");

                // Create GIN index for jsonb column if not exists
                Console.WriteLine("[ConvertStageComponentsToJsonb] Creating GIN index on ff_stage.components_json...");
                var indexExists = db.Ado.GetDataTable(@"
                    SELECT indexname 
                    FROM pg_indexes 
                    WHERE tablename = 'ff_stage' 
                      AND indexname = 'idx_ff_stage_components_json_gin'
                ").Rows.Count > 0;

                if (!indexExists)
                {
                    db.Ado.ExecuteCommand(@"
                        CREATE INDEX idx_ff_stage_components_json_gin 
                        ON ff_stage USING GIN (components_json);
                    ");
                    Console.WriteLine("[ConvertStageComponentsToJsonb] GIN index created.");
                }
                else
                {
                    Console.WriteLine("[ConvertStageComponentsToJsonb] GIN index already exists, skipping.");
                }

                Console.WriteLine("[ConvertStageComponentsToJsonb] Migration completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConvertStageComponentsToJsonb] Error: {ex.Message}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("[ConvertStageComponentsToJsonb] Rolling back migration...");

                // Drop index if exists
                db.Ado.ExecuteCommand(@"
                    DROP INDEX IF EXISTS idx_ff_stage_components_json_gin;
                ");

                // Convert back to text
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_stage 
                    ALTER COLUMN components_json TYPE text 
                    USING components_json::text;
                ");

                Console.WriteLine("[ConvertStageComponentsToJsonb] Rollback completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConvertStageComponentsToJsonb] Rollback error: {ex.Message}");
                throw;
            }
        }
    }
}

