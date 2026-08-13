using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations;

/// <summary>
/// Migration to add roll_back_teams column to ff_stage table.
/// This column stores a JSONB array of team IDs that are allowed to roll back completed stages.
/// NULL or empty array means no one can roll back (security default).
/// </summary>
public static class Migration_20260806000001_AddRollBackTeamsToStage
{
    public static void Up(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"
            ALTER TABLE ff_stage ADD COLUMN IF NOT EXISTS roll_back_teams jsonb;
        ");

        Console.WriteLine("[Migration] Added roll_back_teams column to ff_stage table");
    }

    public static void Down(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"
            ALTER TABLE ff_stage DROP COLUMN IF EXISTS roll_back_teams;
        ");

        Console.WriteLine("[Migration] Removed roll_back_teams column from ff_stage table");
    }
}
