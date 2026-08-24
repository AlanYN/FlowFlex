using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations;

/// <summary>
/// Migration to add total_variance_days column to ff_onboarding table.
/// This column stores the overall variance in days (= Case actualEndDate - plannedEndDate).
/// Updated when the Case completes. Null for in-progress cases.
/// </summary>
public static class Migration_20260819000002_AddTotalVarianceDaysToOnboarding
{
    public static void Up(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"
            ALTER TABLE ff_onboarding ADD COLUMN IF NOT EXISTS total_variance_days INTEGER NULL;
        ");

        Console.WriteLine("[Migration] Added total_variance_days column to ff_onboarding table");
    }

    public static void Down(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"
            ALTER TABLE ff_onboarding DROP COLUMN IF EXISTS total_variance_days;
        ");

        Console.WriteLine("[Migration] Removed total_variance_days column from ff_onboarding table");
    }
}
