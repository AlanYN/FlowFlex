using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations;

/// <summary>
/// Migration to add sla_days and component_weights columns to ff_stage table.
/// sla_days: SLA requirement in days (null = no SLA).
/// component_weights: JSONB array of component weights for CompletionPercentage calculation.
/// </summary>
public static class Migration_20260819000001_AddGanttFieldsToStage
{
    public static void Up(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"
            ALTER TABLE ff_stage ADD COLUMN IF NOT EXISTS sla_days INTEGER NULL;
            ALTER TABLE ff_stage ADD COLUMN IF NOT EXISTS component_weights JSONB NULL;
        ");

        Console.WriteLine("[Migration] Added sla_days and component_weights columns to ff_stage table");
    }

    public static void Down(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"
            ALTER TABLE ff_stage DROP COLUMN IF EXISTS sla_days;
            ALTER TABLE ff_stage DROP COLUMN IF EXISTS component_weights;
        ");

        Console.WriteLine("[Migration] Removed sla_days and component_weights columns from ff_stage table");
    }
}
