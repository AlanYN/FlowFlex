using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Remove AI Summary fields from Stage table
    /// Migration: 20250126000001_RemoveAISummaryFieldsFromStage
    /// </summary>
    public class RemoveAISummaryFieldsFromStage_20250126000001
    {
        /// <summary>
        /// Apply migration - Remove AI Summary fields from ff_stage table
        /// </summary>
        /// <param name="db">Database context</param>
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("Starting RemoveAISummaryFieldsFromStage migration...");

                // Check if columns exist before dropping them
                var columns = new[]
                {
                    "ai_summary",
                    "ai_summary_generated_at",
                    "ai_summary_confidence",
                    "ai_summary_model",
                    "ai_summary_data"
                };

                foreach (var columnName in columns)
                {
                    try
                    {
                        // Check if column exists first
                        var columnExists = CheckColumnExists(db, "ff_stage", columnName);
                        
                        if (columnExists)
                        {
                            Console.WriteLine($"Dropping column {columnName} from ff_stage table...");
                            
                            var sql = $"ALTER TABLE ff_stage DROP COLUMN IF EXISTS {columnName}";
                            db.Ado.ExecuteCommand(sql);
                            
                            Console.WriteLine($"Successfully dropped column {columnName}");
                        }
                        else
                        {
                            Console.WriteLine($"Column {columnName} does not exist, skipping...");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error dropping column {columnName}: {ex.Message}");
                        // Continue with other columns even if one fails
                    }
                }

                Console.WriteLine("Successfully completed RemoveAISummaryFieldsFromStage migration");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to execute RemoveAISummaryFieldsFromStage migration: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Rollback migration - Add AI Summary fields back to ff_stage table
        /// </summary>
        /// <param name="db">Database context</param>
        public static void Down(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("Starting rollback for RemoveAISummaryFieldsFromStage migration...");

                // Re-add the columns with their original definitions
                var columnDefinitions = new[]
                {
                    ("ai_summary", "VARCHAR(2000)"),
                    ("ai_summary_generated_at", "TIMESTAMP"),
                    ("ai_summary_confidence", "DECIMAL(5,4)"),
                    ("ai_summary_model", "VARCHAR(100)"),
                    ("ai_summary_data", "JSONB")
                };

                foreach (var (columnName, columnType) in columnDefinitions)
                {
                    try
                    {
                        // Check if column already exists
                        var columnExists = CheckColumnExists(db, "ff_stage", columnName);
                        
                        if (!columnExists)
                        {
                            Console.WriteLine($"Adding column {columnName} to ff_stage table...");
                            
                            var sql = $"ALTER TABLE ff_stage ADD COLUMN {columnName} {columnType}";
                            db.Ado.ExecuteCommand(sql);
                            
                            Console.WriteLine($"Successfully added column {columnName}");
                        }
                        else
                        {
                            Console.WriteLine($"Column {columnName} already exists, skipping...");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error adding column {columnName}: {ex.Message}");
                        // Continue with other columns even if one fails
                    }
                }

                Console.WriteLine("Successfully completed rollback for RemoveAISummaryFieldsFromStage migration");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to rollback RemoveAISummaryFieldsFromStage migration: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Check if a column exists in a table
        /// </summary>
        private static bool CheckColumnExists(ISqlSugarClient db, string tableName, string columnName)
        {
            try
            {
                var sql = @"
                    SELECT COUNT(*) 
                    FROM information_schema.columns 
                    WHERE table_name = @tableName 
                    AND column_name = @columnName";
                
                var result = db.Ado.SqlQuery<int>(sql, new { tableName, columnName }).FirstOrDefault();
                return result > 0;
            }
            catch
            {
                // If we can't check, assume it exists to be safe
                return true;
            }
        }
    }
}