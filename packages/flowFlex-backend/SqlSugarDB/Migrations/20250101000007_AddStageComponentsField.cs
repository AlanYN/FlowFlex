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
                // Check if column exists
                var columns = db.DbMaintenance.GetColumnInfosByTableName("ff_stage");
                var componentsColumnExists = columns.Exists(c => c.DbColumnName.Equals("components_json", StringComparison.OrdinalIgnoreCase));

                if (!componentsColumnExists)
                {
                    // Add components_json column
                    db.DbMaintenance.AddColumn("ff_stage", new DbColumnInfo
                    {
                        DbColumnName = "components_json",
                        DataType = "TEXT",
                        IsNullable = true,
                        ColumnDescription = "Stage components configuration (JSON)"
                    });

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
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                // Check if column exists
                var columns = db.DbMaintenance.GetColumnInfosByTableName("ff_stage");
                var componentsColumnExists = columns.Exists(c => c.DbColumnName.Equals("components_json", StringComparison.OrdinalIgnoreCase));

                if (componentsColumnExists)
                {
                    // Drop components_json column
                    db.DbMaintenance.DropColumn("ff_stage", "components_json");
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
                throw;
            }
        }
    }
} 