using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add FieldId column to ff_static_field_values table
    /// </summary>
    public class Migration_20260115000001_AddFieldIdToStaticFieldValue
    {
        public static void Up(ISqlSugarClient db)
        {
            // Check if column exists
            var columns = db.DbMaintenance.GetColumnInfosByTableName("ff_static_field_values", false);
            var hasFieldId = columns.Any(c => c.DbColumnName.ToLower() == "field_id");

            if (!hasFieldId)
            {
                // Add field_id column
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_static_field_values 
                    ADD COLUMN field_id BIGINT NULL
                ");
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_static_field_values 
                DROP COLUMN IF EXISTS field_id
            ");
        }
    }
}
