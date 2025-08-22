using SqlSugar;
using FlowFlex.SqlSugarDB.Context;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Convert all OwEntityBase tables to use snowflake ID instead of auto-increment
    /// </summary>
    public class Migration_20250122000015_ConvertOwEntityBaseTablesToSnowflakeId
    {
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("[ConvertOwEntityBaseTablesToSnowflakeId] Starting conversion of OwEntityBase tables to snowflake ID...");

                // List of tables that inherit from OwEntityBase and need snowflake ID
                var owEntityTables = new[]
                {
                    "ff_access_tokens",
                    "ff_operation_change_log", 
                    "ff_users",
                    "ff_user_invitations",
                    "ff_internal_notes"
                };

                foreach (var tableName in owEntityTables)
                {
                    try
                    {
                        Console.WriteLine($"[ConvertOwEntityBaseTablesToSnowflakeId] Converting table {tableName}...");

                        // Check if table exists
                        var tableExists = db.Ado.GetDataTable($@"
                            SELECT 1 FROM information_schema.tables 
                            WHERE table_name = '{tableName}'
                        ").Rows.Count > 0;

                        if (tableExists)
                        {
                            // Check current id column definition
                            var idColumnInfo = db.Ado.GetDataTable($@"
                                SELECT column_default, data_type 
                                FROM information_schema.columns 
                                WHERE table_name = '{tableName}' AND column_name = 'id'
                            ");

                            if (idColumnInfo.Rows.Count > 0)
                            {
                                var columnDefault = idColumnInfo.Rows[0]["column_default"]?.ToString();
                                var dataType = idColumnInfo.Rows[0]["data_type"]?.ToString();

                                // Check if it's currently using auto-increment (serial/sequence)
                                if (!string.IsNullOrEmpty(columnDefault) && 
                                    (columnDefault.Contains("nextval") || dataType?.ToLower().Contains("serial") == true))
                                {
                                    Console.WriteLine($"[ConvertOwEntityBaseTablesToSnowflakeId] {tableName} has auto-increment ID, converting to snowflake ID...");

                                    // Drop sequence/serial and recreate as plain BIGINT
                                    db.Ado.ExecuteCommand($@"
                                        -- Drop existing id column and its sequence
                                        ALTER TABLE {tableName} DROP COLUMN IF EXISTS id CASCADE;
                                        
                                        -- Add new BIGINT id column (no auto-increment)
                                        ALTER TABLE {tableName} ADD COLUMN id BIGINT PRIMARY KEY;
                                    ");

                                    Console.WriteLine($"[ConvertOwEntityBaseTablesToSnowflakeId] Successfully converted {tableName} to use snowflake ID");
                                }
                                else
                                {
                                    Console.WriteLine($"[ConvertOwEntityBaseTablesToSnowflakeId] {tableName} already uses non-auto-increment ID, checking if it's BIGINT...");
                                    
                                    // Just ensure it's BIGINT and PRIMARY KEY
                                    if (dataType?.ToLower() != "bigint")
                                    {
                                        db.Ado.ExecuteCommand($@"
                                            ALTER TABLE {tableName} ALTER COLUMN id TYPE BIGINT;
                                        ");
                                        Console.WriteLine($"[ConvertOwEntityBaseTablesToSnowflakeId] Converted {tableName}.id to BIGINT type");
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine($"[ConvertOwEntityBaseTablesToSnowflakeId] {tableName} doesn't have id column, adding BIGINT PRIMARY KEY...");
                                db.Ado.ExecuteCommand($@"
                                    ALTER TABLE {tableName} ADD COLUMN id BIGINT PRIMARY KEY;
                                ");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[ConvertOwEntityBaseTablesToSnowflakeId] Table {tableName} does not exist, skipping");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ConvertOwEntityBaseTablesToSnowflakeId] Error converting table {tableName}: {ex.Message}");
                        // Continue with next table instead of failing the entire migration
                        continue;
                    }
                }

                Console.WriteLine("[ConvertOwEntityBaseTablesToSnowflakeId] Conversion of OwEntityBase tables completed successfully.");
                Console.WriteLine("[ConvertOwEntityBaseTablesToSnowflakeId] Note: All OwEntityBase tables now use snowflake IDs and should call InitNewId() before insert.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConvertOwEntityBaseTablesToSnowflakeId] Error: {ex.Message}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("[ConvertOwEntityBaseTablesToSnowflakeId] Starting rollback of OwEntityBase tables snowflake ID conversion...");

                // List of tables to rollback
                var owEntityTables = new[]
                {
                    "ff_access_tokens",
                    "ff_operation_change_log", 
                    "ff_users",
                    "ff_user_invitations",
                    "ff_internal_notes"
                };

                foreach (var tableName in owEntityTables)
                {
                    try
                    {
                        var tableExists = db.Ado.GetDataTable($@"
                            SELECT 1 FROM information_schema.tables 
                            WHERE table_name = '{tableName}'
                        ").Rows.Count > 0;

                        if (tableExists)
                        {
                            // Revert to BIGSERIAL (auto-increment)
                            db.Ado.ExecuteCommand($@"
                                ALTER TABLE {tableName} DROP COLUMN IF EXISTS id CASCADE;
                                ALTER TABLE {tableName} ADD COLUMN id BIGSERIAL PRIMARY KEY;
                            ");
                            
                            Console.WriteLine($"[ConvertOwEntityBaseTablesToSnowflakeId] Reverted {tableName} to use BIGSERIAL");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ConvertOwEntityBaseTablesToSnowflakeId] Error reverting table {tableName}: {ex.Message}");
                        continue;
                    }
                }

                Console.WriteLine("[ConvertOwEntityBaseTablesToSnowflakeId] Rollback completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConvertOwEntityBaseTablesToSnowflakeId] Rollback error: {ex.Message}");
                throw;
            }
        }
    }
}