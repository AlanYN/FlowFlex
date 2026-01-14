using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Update unique constraint on ff_stage_condition table to only apply to valid records.
    /// This allows soft-deleted records (IsValid = false) to not block new record creation.
    /// </summary>
    public class Migration_20260114000001_UpdateStageConditionUniqueConstraint
    {
        private static ISqlSugarClient _db;

        public static void Up(ISqlSugarClient db)
        {
            _db = db;

            // Drop the old unique constraint
            _db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_stage_condition 
                DROP CONSTRAINT IF EXISTS uq_stage_condition;
            ");

            // Create a partial unique index that only applies to valid records
            _db.Ado.ExecuteCommand(@"
                CREATE UNIQUE INDEX uq_stage_condition 
                ON ff_stage_condition (stage_id, tenant_id) 
                WHERE is_valid = true;
            ");
        }

        public static void Down(ISqlSugarClient db)
        {
            _db = db;

            // Drop the partial unique index
            _db.Ado.ExecuteCommand(@"
                DROP INDEX IF EXISTS uq_stage_condition;
            ");

            // Recreate the original unique constraint
            _db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_stage_condition 
                ADD CONSTRAINT uq_stage_condition UNIQUE (stage_id, tenant_id);
            ");
        }
    }
}
