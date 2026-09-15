using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations;

/// <summary>
/// Migration to add Stage Template inheritance flag and Runtime Permission fields to ff_stage.
/// Stage has both Template-layer inheritance (from Workflow Template) and
/// Runtime-layer inheritance (from Workflow Runtime), independently controlled.
/// </summary>
public static class Migration_202609080002_AddStageRuntimePermission
{
    public static void Up(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"
            ALTER TABLE ff_stage
                ADD COLUMN IF NOT EXISTS template_use_same_as_workflow BOOLEAN NOT NULL DEFAULT TRUE,
                ADD COLUMN IF NOT EXISTS runtime_use_same_as_workflow BOOLEAN NOT NULL DEFAULT TRUE,
                ADD COLUMN IF NOT EXISTS runtime_view_permission_mode SMALLINT NOT NULL DEFAULT 0,
                ADD COLUMN IF NOT EXISTS runtime_view_teams JSONB NULL,
                ADD COLUMN IF NOT EXISTS runtime_operate_teams JSONB NULL,
                ADD COLUMN IF NOT EXISTS runtime_use_same_team_for_operate BOOLEAN NOT NULL DEFAULT TRUE;
        ");
        Console.WriteLine("[Migration] Added runtime/template permission columns to ff_stage");
    }

    public static void Down(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"
            ALTER TABLE ff_stage
                DROP COLUMN IF EXISTS template_use_same_as_workflow,
                DROP COLUMN IF EXISTS runtime_use_same_as_workflow,
                DROP COLUMN IF EXISTS runtime_view_permission_mode,
                DROP COLUMN IF EXISTS runtime_view_teams,
                DROP COLUMN IF EXISTS runtime_operate_teams,
                DROP COLUMN IF EXISTS runtime_use_same_team_for_operate;
        ");
        Console.WriteLine("[Migration] Removed runtime/template permission columns from ff_stage");
    }
}
