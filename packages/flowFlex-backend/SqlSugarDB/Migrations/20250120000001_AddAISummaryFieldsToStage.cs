using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add AI Summary fields to Stage table
    /// 
    /// This migration adds AI summary related fields to the Stage table:
    /// - ai_summary: Stores the AI generated summary text
    /// - ai_summary_generated_at: Timestamp when the summary was generated
    /// - ai_summary_confidence: Confidence score of the AI summary (0-1)
    /// - ai_summary_model: The AI model used for summary generation
    /// - ai_summary_data: Detailed AI summary data in JSONB format
    /// 
    /// Benefits:
    /// - Enables automatic AI-powered stage summaries
    /// - Provides audit trail for AI summary generation
    /// - Supports multiple AI models and confidence tracking
    /// - Stores detailed breakdown data for analysis
    /// </summary>
    public class AddAISummaryFieldsToStage_20250120000001
    {
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("Starting AddAISummaryFieldsToStage migration...");

                // Check if ff_stage table exists
                var stageTableExists = db.DbMaintenance.IsAnyTable("ff_stage");
                if (!stageTableExists)
                {
                    Console.WriteLine("Stage table (ff_stage) does not exist, skipping AI summary fields migration");
                    return;
                }

                // Add AI Summary field
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_stage 
                    ADD COLUMN IF NOT EXISTS ai_summary VARCHAR(2000);
                ");
                Console.WriteLine("Added ai_summary column to ff_stage table");

                // Add AI Summary Generation Date field
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_stage 
                    ADD COLUMN IF NOT EXISTS ai_summary_generated_at TIMESTAMP;
                ");
                Console.WriteLine("Added ai_summary_generated_at column to ff_stage table");

                // Add AI Summary Confidence Score field
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_stage 
                    ADD COLUMN IF NOT EXISTS ai_summary_confidence DECIMAL(5,4);
                ");
                Console.WriteLine("Added ai_summary_confidence column to ff_stage table");

                // Add AI Summary Model field
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_stage 
                    ADD COLUMN IF NOT EXISTS ai_summary_model VARCHAR(100);
                ");
                Console.WriteLine("Added ai_summary_model column to ff_stage table");

                // Add AI Summary Detailed Data field (JSONB)
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_stage 
                    ADD COLUMN IF NOT EXISTS ai_summary_data JSONB;
                ");
                Console.WriteLine("Added ai_summary_data column to ff_stage table");

                // Add comments to columns
                db.Ado.ExecuteCommand(@"
                    COMMENT ON COLUMN ff_stage.ai_summary IS 'AI Generated Summary';
                    COMMENT ON COLUMN ff_stage.ai_summary_generated_at IS 'AI Summary Generation Date';
                    COMMENT ON COLUMN ff_stage.ai_summary_confidence IS 'AI Summary Confidence Score (0-1)';
                    COMMENT ON COLUMN ff_stage.ai_summary_model IS 'AI Model Used for Summary Generation';
                    COMMENT ON COLUMN ff_stage.ai_summary_data IS 'AI Summary Detailed Data (JSONB)';
                ");
                Console.WriteLine("Added column comments for AI summary fields");

                // Create index on ai_summary_generated_at for performance
                try
                {
                    db.Ado.ExecuteCommand(@"
                        CREATE INDEX IF NOT EXISTS idx_ff_stage_ai_summary_generated_at 
                        ON ff_stage(ai_summary_generated_at) 
                        WHERE ai_summary_generated_at IS NOT NULL;
                    ");
                    Console.WriteLine("Created index on ai_summary_generated_at column");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to create index on ai_summary_generated_at: {ex.Message}, continuing migration");
                }

                // Create index on ai_summary_model for analytics
                try
                {
                    db.Ado.ExecuteCommand(@"
                        CREATE INDEX IF NOT EXISTS idx_ff_stage_ai_summary_model 
                        ON ff_stage(ai_summary_model) 
                        WHERE ai_summary_model IS NOT NULL;
                    ");
                    Console.WriteLine("Created index on ai_summary_model column");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to create index on ai_summary_model: {ex.Message}, continuing migration");
                }

                Console.WriteLine("Successfully completed AddAISummaryFieldsToStage migration");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to execute AddAISummaryFieldsToStage migration: {ex.Message}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("Starting rollback for AddAISummaryFieldsToStage migration...");

                // Check if ff_stage table exists
                var stageTableExists = db.DbMaintenance.IsAnyTable("ff_stage");
                if (!stageTableExists)
                {
                    Console.WriteLine("Stage table (ff_stage) does not exist, skipping rollback");
                    return;
                }

                // Drop indexes first
                try
                {
                    db.Ado.ExecuteCommand("DROP INDEX IF EXISTS idx_ff_stage_ai_summary_generated_at;");
                    db.Ado.ExecuteCommand("DROP INDEX IF EXISTS idx_ff_stage_ai_summary_model;");
                    Console.WriteLine("Dropped AI summary indexes");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to drop some indexes during rollback: {ex.Message}");
                }

                // Remove AI Summary fields
                var columnsToRemove = new[] { "ai_summary", "ai_summary_generated_at", "ai_summary_confidence", "ai_summary_model", "ai_summary_data" };

                foreach (var column in columnsToRemove)
                {
                    try
                    {
                        db.Ado.ExecuteCommand($"ALTER TABLE ff_stage DROP COLUMN IF EXISTS {column};");
                        Console.WriteLine($"Removed {column} column from ff_stage table");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to remove {column} column: {ex.Message}");
                    }
                }

                Console.WriteLine("Successfully completed rollback for AddAISummaryFieldsToStage migration");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to rollback AddAISummaryFieldsToStage migration: {ex.Message}");
                throw;
            }
        }
    }
}