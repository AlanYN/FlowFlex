using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add components_json field to ff_stage table
    /// </summary>
    public class Migration_20250101000007_AddStageComponentsField
    {
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("[AddStageComponentsField] Starting migration...");
                
                // First check if the table exists
                var tableExists = db.DbMaintenance.IsAnyTable("ff_stage");
                if (!tableExists)
                {
                    Console.WriteLine("⚠️ Table ff_stage does not exist, skipping components_json column addition");
                    return;
                }

                // Check if column exists
                var columns = db.DbMaintenance.GetColumnInfosByTableName("ff_stage");
                var componentsColumnExists = columns.Exists(c => c.DbColumnName.Equals("components_json", StringComparison.OrdinalIgnoreCase));

                if (!componentsColumnExists)
                {
                    Console.WriteLine("[AddStageComponentsField] Adding components_json column to ff_stage table...");
                    
                    // Add components_json column using direct SQL to avoid potential SqlSugar issues
                    var sql = @"
                        ALTER TABLE ff_stage 
                        ADD COLUMN components_json TEXT;
                        
                        COMMENT ON COLUMN ff_stage.components_json IS 'Stage components configuration (JSON)';
                    ";
                    
                    db.Ado.ExecuteCommand(sql);

                    Console.WriteLine("✅ Successfully added components_json column to ff_stage table");
                }
                else
                {
                    Console.WriteLine("ℹ️ components_json column already exists in ff_stage table");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error adding components_json column: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("[AddStageComponentsField] Starting rollback...");
                
                // First check if the table exists
                var tableExists = db.DbMaintenance.IsAnyTable("ff_stage");
                if (!tableExists)
                {
                    Console.WriteLine("⚠️ Table ff_stage does not exist, skipping components_json column removal");
                    return;
                }

                // Check if column exists
                var columns = db.DbMaintenance.GetColumnInfosByTableName("ff_stage");
                var componentsColumnExists = columns.Exists(c => c.DbColumnName.Equals("components_json", StringComparison.OrdinalIgnoreCase));

                if (componentsColumnExists)
                {
                    Console.WriteLine("[AddStageComponentsField] Dropping components_json column from ff_stage table...");
                    
                    // Drop components_json column using direct SQL
                    var sql = "ALTER TABLE ff_stage DROP COLUMN components_json;";
                    db.Ado.ExecuteCommand(sql);
                    
                    Console.WriteLine("✅ Successfully dropped components_json column from ff_stage table");
                }
                else
                {
                    Console.WriteLine("ℹ️ components_json column does not exist in ff_stage table");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error dropping components_json column: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
} 