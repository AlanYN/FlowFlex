using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Adds old_values_json column to ff_document_operation_log.
    /// Stores the cell values captured by the backend immediately before this mutation
    /// is applied, enabling Before → After comparison in the history panel.
    /// Structure mirrors params_json but only contains the affected cells' previous values.
    /// </summary>
    public static class Migration_20260930000001_AddOldValuesJsonToDocumentOperationLog
    {
        public static void Up(ISqlSugarClient db)
        {
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_document_operation_log
                ADD COLUMN IF NOT EXISTS old_values_json jsonb NULL;
            ");
        }

        public static void Down(ISqlSugarClient db)
        {
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_document_operation_log
                DROP COLUMN IF EXISTS old_values_json;
            ");
        }
    }
}
