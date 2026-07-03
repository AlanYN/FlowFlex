using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations;

/// <summary>
/// Migration to support multiple conditions per stage:
/// 1. Add condition_fallback_stage_id to ff_stage (Stage-level fallback)
/// 2. Add order column to ff_stage_condition
/// 3. Drop unique constraint on ff_stage_condition (stage_id, tenant_id)
/// 4. Migrate existing fallback data from condition to stage
/// </summary>
public static class Migration_20260701000001_AddMultiConditionSupport
{
    public static void Up(ISqlSugarClient db)
    {
        // 1. Add condition_fallback_stage_id to ff_stage
        db.Ado.ExecuteCommand(@"
            ALTER TABLE ff_stage 
            ADD COLUMN IF NOT EXISTS condition_fallback_stage_id BIGINT NULL;
        ");

        // 2. Add order column to ff_stage_condition
        db.Ado.ExecuteCommand(@"
            ALTER TABLE ff_stage_condition 
            ADD COLUMN IF NOT EXISTS ""order"" INTEGER NOT NULL DEFAULT 0;
        ");

        // 3. Drop the partial unique index (allows multiple conditions per stage)
        db.Ado.ExecuteCommand(@"
            DROP INDEX IF EXISTS uq_stage_condition;
        ");

        // 4. Migrate existing fallback_stage_id from condition to stage
        db.Ado.ExecuteCommand(@"
            UPDATE ff_stage s
            SET condition_fallback_stage_id = sc.fallback_stage_id
            FROM ff_stage_condition sc
            WHERE sc.stage_id = s.id
              AND sc.is_valid = true
              AND sc.fallback_stage_id IS NOT NULL;
        ");

        // 5. Add index for querying conditions by stage (performance)
        db.Ado.ExecuteCommand(@"
            CREATE INDEX IF NOT EXISTS idx_stage_condition_stage_order 
            ON ff_stage_condition(stage_id, ""order"") 
            WHERE is_valid = true;
        ");

        Console.WriteLine("[Migration] Added multi-condition support: stage fallback field, condition order, removed unique constraint, migrated fallback data");
    }

    public static void Down(ISqlSugarClient db)
    {
        // Remove the new index
        db.Ado.ExecuteCommand(@"
            DROP INDEX IF EXISTS idx_stage_condition_stage_order;
        ");

        // Restore the unique constraint
        db.Ado.ExecuteCommand(@"
            CREATE UNIQUE INDEX uq_stage_condition 
            ON ff_stage_condition (stage_id, tenant_id) 
            WHERE is_valid = true;
        ");

        // Remove order column
        db.Ado.ExecuteCommand(@"
            ALTER TABLE ff_stage_condition DROP COLUMN IF EXISTS ""order"";
        ");

        // Remove condition_fallback_stage_id from stage
        db.Ado.ExecuteCommand(@"
            ALTER TABLE ff_stage DROP COLUMN IF EXISTS condition_fallback_stage_id;
        ");

        Console.WriteLine("[Migration] Reverted multi-condition support");
    }
}
