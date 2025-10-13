using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Migration to convert ff_stage.default_assignee from VARCHAR to JSONB
    /// This provides better query performance and data integrity for assignee arrays
    /// </summary>
    public class ConvertStageDefaultAssigneeToJsonb_20250109000001
    {
        /// <summary>
        /// Migration timestamp for ordering
        /// </summary>
        public static readonly DateTime MigrationDate = new DateTime(2025, 1, 9, 14, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Execute the migration to JSONB
        /// </summary>
        /// <param name="db">Database connection</param>
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Starting migration to JSONB format for default_assignee");

                // Step 1: Create a temporary column for JSONB data
                var createTempColumnSql = @"
                    ALTER TABLE ff_stage 
                    ADD COLUMN default_assignee_jsonb JSONB;
                ";
                db.Ado.ExecuteCommand(createTempColumnSql);
                Console.WriteLine("Created temporary JSONB column");

                // Step 2: Migrate existing data to JSONB format
                MigrateDataToJsonb(db);

                // Step 3: Drop the old VARCHAR column
                var dropOldColumnSql = @"
                    ALTER TABLE ff_stage 
                    DROP COLUMN default_assignee;
                ";
                db.Ado.ExecuteCommand(dropOldColumnSql);
                Console.WriteLine("Dropped old VARCHAR column");

                // Step 4: Rename the temporary column to the original name
                var renameColumnSql = @"
                    ALTER TABLE ff_stage 
                    RENAME COLUMN default_assignee_jsonb TO default_assignee;
                ";
                db.Ado.ExecuteCommand(renameColumnSql);
                Console.WriteLine("Renamed JSONB column to default_assignee");

                // Step 5: Create index for better query performance
                CreateJsonbIndexes(db);

                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Successfully converted default_assignee to JSONB format");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Migration error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Rollback the migration (convert back to VARCHAR)
        /// </summary>
        /// <param name="db">Database connection</param>
        public static void Down(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Rolling back JSONB migration for default_assignee");

                // Step 1: Create temporary VARCHAR column
                var createTempColumnSql = @"
                    ALTER TABLE ff_stage 
                    ADD COLUMN default_assignee_varchar VARCHAR(2000);
                ";
                db.Ado.ExecuteCommand(createTempColumnSql);

                // Step 2: Convert JSONB data back to comma-separated format
                RollbackJsonbData(db);

                // Step 3: Drop JSONB column and indexes
                DropJsonbIndexes(db);
                var dropJsonbColumnSql = @"
                    ALTER TABLE ff_stage 
                    DROP COLUMN default_assignee;
                ";
                db.Ado.ExecuteCommand(dropJsonbColumnSql);

                // Step 4: Rename VARCHAR column back
                var renameColumnSql = @"
                    ALTER TABLE ff_stage 
                    RENAME COLUMN default_assignee_varchar TO default_assignee;
                ";
                db.Ado.ExecuteCommand(renameColumnSql);

                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Successfully rolled back to VARCHAR format");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Rollback error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Migrate existing VARCHAR data to JSONB format
        /// </summary>
        private static void MigrateDataToJsonb(ISqlSugarClient db)
        {
            // Get all stages with non-null default_assignee values
            var stages = db.Ado.GetDataTable(@"
                SELECT id, default_assignee 
                FROM ff_stage 
                WHERE default_assignee IS NOT NULL AND default_assignee != ''
            ");

            int migratedCount = 0;
            int errorCount = 0;
            int nullifiedCount = 0;

            foreach (System.Data.DataRow row in stages.Rows)
            {
                try
                {
                    var id = Convert.ToInt64(row["id"]);
                    var assigneeString = row["default_assignee"]?.ToString();

                    if (!string.IsNullOrWhiteSpace(assigneeString))
                    {
                        List<string> assigneeList = null;

                        // Parse existing data (could be JSON or comma-separated)
                        if (assigneeString.TrimStart().StartsWith("["))
                        {
                            try
                            {
                                // Already JSON format
                                assigneeList = JsonSerializer.Deserialize<List<string>>(assigneeString);
                            }
                            catch (JsonException)
                            {
                                Console.WriteLine($"Warning: Invalid JSON format for stage {id}, setting to null");
                            }
                        }
                        else
                        {
                            // Comma-separated format, convert to list
                            assigneeList = assigneeString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .Where(s => !string.IsNullOrWhiteSpace(s) && s.Length >= 10) // Filter out incomplete IDs
                                .ToList();
                        }

                        if (assigneeList?.Count > 0)
                        {
                            // Convert to proper JSONB format
                            var jsonbValue = JsonSerializer.Serialize(assigneeList);

                            // Update the temporary JSONB column
                            db.Ado.ExecuteCommand(
                                "UPDATE ff_stage SET default_assignee_jsonb = @jsonb::jsonb WHERE id = @id",
                                new { jsonb = jsonbValue, id = id }
                            );

                            migratedCount++;
                        }
                        else
                        {
                            // Set to NULL for empty or invalid data
                            db.Ado.ExecuteCommand(
                                "UPDATE ff_stage SET default_assignee_jsonb = NULL WHERE id = @id",
                                new { id = id }
                            );
                            nullifiedCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    Console.WriteLine($"Error migrating stage {row["id"]}: {ex.Message}");

                    // Set problematic records to NULL to prevent index issues
                    try
                    {
                        var id = Convert.ToInt64(row["id"]);
                        db.Ado.ExecuteCommand(
                            "UPDATE ff_stage SET default_assignee_jsonb = NULL WHERE id = @id",
                            new { id = id }
                        );
                        nullifiedCount++;
                    }
                    catch
                    {
                        // If even setting to NULL fails, log it
                        Console.WriteLine($"Failed to nullify problematic record for stage {row["id"]}");
                    }
                }
            }

            Console.WriteLine($"Data migration completed: {migratedCount} successful, {errorCount} errors, {nullifiedCount} nullified");
        }

        /// <summary>
        /// Convert JSONB data back to comma-separated format for rollback
        /// </summary>
        private static void RollbackJsonbData(ISqlSugarClient db)
        {
            var stages = db.Ado.GetDataTable(@"
                SELECT id, default_assignee 
                FROM ff_stage 
                WHERE default_assignee IS NOT NULL
            ");

            foreach (System.Data.DataRow row in stages.Rows)
            {
                try
                {
                    var id = Convert.ToInt64(row["id"]);
                    var jsonbValue = row["default_assignee"]?.ToString();

                    if (!string.IsNullOrWhiteSpace(jsonbValue))
                    {
                        var assigneeList = JsonSerializer.Deserialize<List<string>>(jsonbValue);
                        var commaSeparated = string.Join(",", assigneeList);

                        // Truncate if necessary to fit VARCHAR(2000)
                        if (commaSeparated.Length > 2000)
                        {
                            commaSeparated = commaSeparated.Substring(0, 2000);
                        }

                        db.Ado.ExecuteCommand(
                            "UPDATE ff_stage SET default_assignee_varchar = @value WHERE id = @id",
                            new { value = commaSeparated, id = id }
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error rolling back stage {row["id"]}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Create indexes for JSONB column to improve query performance
        /// </summary>
        private static void CreateJsonbIndexes(ISqlSugarClient db)
        {
            try
            {
                // Create GIN index for JSONB operations
                var createIndexSql = @"
                    CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_ff_stage_default_assignee_gin 
                    ON ff_stage USING GIN (default_assignee);
                ";
                db.Ado.ExecuteCommand(createIndexSql);
                Console.WriteLine("Created GIN index for default_assignee JSONB column");

                // Create index for array length queries with proper NULL and type checking
                var createLengthIndexSql = @"
                    CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_ff_stage_default_assignee_length 
                    ON ff_stage (jsonb_array_length(default_assignee)) 
                    WHERE default_assignee IS NOT NULL 
                    AND jsonb_typeof(default_assignee) = 'array';
                ";
                db.Ado.ExecuteCommand(createLengthIndexSql);
                Console.WriteLine("Created array length index for default_assignee with type checking");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not create indexes: {ex.Message}");
                // Don't fail the migration if index creation fails
            }
        }

        /// <summary>
        /// Drop JSONB indexes during rollback
        /// </summary>
        private static void DropJsonbIndexes(ISqlSugarClient db)
        {
            try
            {
                db.Ado.ExecuteCommand("DROP INDEX IF EXISTS idx_ff_stage_default_assignee_gin;");
                db.Ado.ExecuteCommand("DROP INDEX IF EXISTS idx_ff_stage_default_assignee_length;");
                Console.WriteLine("Dropped JSONB indexes");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not drop indexes: {ex.Message}");
            }
        }
    }
}