using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Remove static_fields_json and required_fields_json columns from ff_stage table
    /// Date: 2024-12-19
    /// </summary>
    public class _20241219_RemoveStaticFieldsColumn
    {
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                // Check if column exists before dropping it
                var columnExists = db.DbMaintenance.IsAnyColumn("ff_stage", "static_fields_json");

                if (columnExists)
                {
                    // Remove static_fields_json column from ff_stage table
                    db.DbMaintenance.DropColumn("ff_stage", "static_fields_json");
                    Console.WriteLine("Successfully removed static_fields_json column from ff_stage table");
                }
                else
                {
                    Console.WriteLine("static_fields_json column does not exist in ff_stage table, skipping removal");
                }

                // Check and remove required_fields_json column
                var requiredFieldsColumnExists = db.DbMaintenance.IsAnyColumn("ff_stage", "required_fields_json");

                if (requiredFieldsColumnExists)
                {
                    // Remove required_fields_json column from ff_stage table
                    db.DbMaintenance.DropColumn("ff_stage", "required_fields_json");
                    Console.WriteLine("Successfully removed required_fields_json column from ff_stage table");
                }
                else
                {
                    Console.WriteLine("required_fields_json column does not exist in ff_stage table, skipping removal");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing static_fields_json column: {ex.Message}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                // Re-add static_fields_json column if needed for rollback
                var columnExists = db.DbMaintenance.IsAnyColumn("ff_stage", "static_fields_json");

                if (!columnExists)
                {
                    db.DbMaintenance.AddColumn("ff_stage", new DbColumnInfo
                    {
                        DbColumnName = "static_fields_json",
                        DataType = "TEXT",
                        IsNullable = true,
                        ColumnDescription = "Associated Static Fields Configuration (JSON)"
                    });
                    Console.WriteLine("Successfully re-added static_fields_json column to ff_stage table");
                }
                else
                {
                    Console.WriteLine("static_fields_json column already exists in ff_stage table, skipping addition");
                }

                // Re-add required_fields_json column if needed for rollback
                var requiredFieldsColumnExists = db.DbMaintenance.IsAnyColumn("ff_stage", "required_fields_json");

                if (!requiredFieldsColumnExists)
                {
                    db.DbMaintenance.AddColumn("ff_stage", new DbColumnInfo
                    {
                        DbColumnName = "required_fields_json",
                        DataType = "TEXT",
                        IsNullable = true,
                        ColumnDescription = "Required Fields Configuration (JSON)"
                    });
                    Console.WriteLine("Successfully re-added required_fields_json column to ff_stage table");
                }
                else
                {
                    Console.WriteLine("required_fields_json column already exists in ff_stage table, skipping addition");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error re-adding static_fields_json column: {ex.Message}");
                throw;
            }
        }
    }
}