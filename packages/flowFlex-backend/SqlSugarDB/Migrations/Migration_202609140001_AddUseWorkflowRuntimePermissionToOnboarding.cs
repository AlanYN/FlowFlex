using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations;

/// <summary>
/// Adds use_workflow_runtime_permission column to ff_onboarding.
/// true (default) = Case inherits Workflow Runtime Permission snapshot.
/// false = Case uses its own view/operate fields within snapshot boundary.
/// </summary>
public static class Migration_202609140001_AddUseWorkflowRuntimePermissionToOnboarding
{
    public static void Up(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"
            ALTER TABLE ff_onboarding
                ADD COLUMN IF NOT EXISTS use_workflow_runtime_permission BOOLEAN NOT NULL DEFAULT TRUE;
        ");
        Console.WriteLine("[Migration] Added use_workflow_runtime_permission column to ff_onboarding");
    }

    public static void Down(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"
            ALTER TABLE ff_onboarding
                DROP COLUMN IF EXISTS use_workflow_runtime_permission;
        ");
        Console.WriteLine("[Migration] Removed use_workflow_runtime_permission column from ff_onboarding");
    }
}
