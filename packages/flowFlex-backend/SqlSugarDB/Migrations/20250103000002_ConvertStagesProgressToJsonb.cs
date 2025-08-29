using System;
using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Convert StagesProgress to JSONB Migration
    /// 
    /// This migration converts the stages_progress_json column from text/json to jsonb type
    /// for better performance, indexing capabilities, and query optimization.
    /// 
    /// Benefits of JSONB:
    /// - Faster queries due to binary storage format
    /// - Support for GIN indexes on JSON data
    /// - Built-in JSON operators and functions
    /// - Automatic validation of JSON structure
    /// </summary>
    public class ConvertStagesProgressToJsonb_20250103000002
    {
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("Starting ConvertStagesProgressToJsonb migration...");

                // Check if the column exists and its current type
                var columnExists = db.Ado.GetDataTable(@"
                    SELECT column_name, data_type 
                    FROM information_schema.columns 
                    WHERE table_name = 'ff_onboarding' 
                    AND column_name = 'stages_progress_json'
                ").Rows.Count > 0;

                if (columnExists)
                {
                    Console.WriteLine("Converting stages_progress_json column to JSONB type...");

                    // Convert the column type from text/json to jsonb
                    // PostgreSQL can automatically convert text to jsonb
                    db.Ado.ExecuteCommand(@"
                        ALTER TABLE ff_onboarding 
                        ALTER COLUMN stages_progress_json TYPE jsonb 
                        USING stages_progress_json::jsonb;
                    ");

                    Console.WriteLine("Column conversion completed successfully.");

                    // Create GIN index on the JSONB column for better query performance
                    Console.WriteLine("Creating GIN index on stages_progress_json...");

                    var indexExists = db.Ado.GetDataTable(@"
                        SELECT indexname 
                        FROM pg_indexes 
                        WHERE tablename = 'ff_onboarding' 
                        AND indexname = 'idx_ff_onboarding_stages_progress_json_gin'
                    ").Rows.Count > 0;

                    if (!indexExists)
                    {
                        db.Ado.ExecuteCommand(@"
                            CREATE INDEX idx_ff_onboarding_stages_progress_json_gin 
                            ON ff_onboarding USING GIN (stages_progress_json);
                        ");
                        Console.WriteLine("GIN index created successfully.");
                    }
                    else
                    {
                        Console.WriteLine("GIN index already exists, skipping creation.");
                    }

                    // Create additional functional indexes for common queries
                    Console.WriteLine("Creating functional indexes for common queries...");

                    // Index for querying by stage status
                    var statusIndexExists = db.Ado.GetDataTable(@"
                        SELECT indexname 
                        FROM pg_indexes 
                        WHERE tablename = 'ff_onboarding' 
                        AND indexname = 'idx_ff_onboarding_stages_status'
                    ").Rows.Count > 0;

                    if (!statusIndexExists)
                    {
                        db.Ado.ExecuteCommand(@"
                            CREATE INDEX idx_ff_onboarding_stages_status 
                            ON ff_onboarding USING GIN ((stages_progress_json -> 'status'));
                        ");
                        Console.WriteLine("Status functional index created.");
                    }

                    // Index for querying by completion status
                    var completionIndexExists = db.Ado.GetDataTable(@"
                        SELECT indexname 
                        FROM pg_indexes 
                        WHERE tablename = 'ff_onboarding' 
                        AND indexname = 'idx_ff_onboarding_stages_completion'
                    ").Rows.Count > 0;

                    if (!completionIndexExists)
                    {
                        db.Ado.ExecuteCommand(@"
                            CREATE INDEX idx_ff_onboarding_stages_completion 
                            ON ff_onboarding USING GIN ((stages_progress_json -> 'isCompleted'));
                        ");
                        Console.WriteLine("Completion functional index created.");
                    }
                }
                else
                {
                    Console.WriteLine("stages_progress_json column not found, skipping conversion.");
                }

                // Log completion
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                Console.WriteLine($"Migration ConvertStagesProgressToJsonb_20250103000002 completed at {timestamp}");
                Console.WriteLine("Benefits: Improved query performance, GIN indexing support, and JSON operators availability.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ConvertStagesProgressToJsonb migration: {ex.Message}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("Rolling back ConvertStagesProgressToJsonb migration...");

                // Drop the indexes first
                Console.WriteLine("Dropping JSONB indexes...");

                db.Ado.ExecuteCommand(@"
                    DROP INDEX IF EXISTS idx_ff_onboarding_stages_progress_json_gin;
                    DROP INDEX IF EXISTS idx_ff_onboarding_stages_status;
                    DROP INDEX IF EXISTS idx_ff_onboarding_stages_completion;
                ");

                // Convert back to text type
                Console.WriteLine("Converting stages_progress_json back to text type...");

                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_onboarding 
                    ALTER COLUMN stages_progress_json TYPE text 
                    USING stages_progress_json::text;
                ");

                Console.WriteLine("ConvertStagesProgressToJsonb migration rollback completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rolling back ConvertStagesProgressToJsonb migration: {ex.Message}");
                throw;
            }
        }
    }
}