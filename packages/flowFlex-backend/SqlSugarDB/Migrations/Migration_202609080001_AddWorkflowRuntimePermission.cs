using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations;

/// <summary>
/// Migration to add Workflow Runtime Permission fields to ff_workflow.
/// Runtime Permission is independent from Template Permission.
/// RuntimeUseSameAsTemplate=true means Effective Runtime copies Template fields.
/// NOTE: No runtime_use_same_team_for_operate — Workflow Runtime Operate is always independent.
/// </summary>
public static class Migration_202609080001_AddWorkflowRuntimePermission
{
    public static void Up(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"
            ALTER TABLE ff_workflow
                ADD COLUMN IF NOT EXISTS runtime_use_same_as_template BOOLEAN NOT NULL DEFAULT TRUE,
                ADD COLUMN IF NOT EXISTS runtime_view_permission_mode SMALLINT NOT NULL DEFAULT 0,
                ADD COLUMN IF NOT EXISTS runtime_view_teams JSONB NULL,
                ADD COLUMN IF NOT EXISTS runtime_operate_teams JSONB NULL;
        ");
        Console.WriteLine("[Migration] Added runtime permission columns to ff_workflow");
    }

    public static void Down(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"
            ALTER TABLE ff_workflow
                DROP COLUMN IF EXISTS runtime_use_same_as_template,
                DROP COLUMN IF EXISTS runtime_view_permission_mode,
                DROP COLUMN IF EXISTS runtime_view_teams,
                DROP COLUMN IF EXISTS runtime_operate_teams;
        ");
        Console.WriteLine("[Migration] Removed runtime permission columns from ff_workflow");
    }
}
