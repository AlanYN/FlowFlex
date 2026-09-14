using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlowFlex.Domain.Shared.Enums.OW;

namespace FlowFlex.SqlSugarDB.Migrations;

/// <summary>
/// Migration to add Permission Snapshot fields to ff_onboarding.
/// Snapshot captures Workflow Runtime Permission at Case creation time.
/// Three steps:
///   1. DDL: add max_view_permission_mode, max_view_teams, max_operate_teams columns
///   2. SQL backfill: fill max_* for is_valid=true onboardings where max_view_permission_mode IS NULL
///   3. C# JSONB backfill: fill MaxStage* fields inside stages_progress_json (batched, idempotent)
/// NOTE: Depends on Migration 1 (runtime_use_same_as_template column in ff_workflow).
/// Down() only removes the 3 new columns; does NOT revert backfill data.
/// </summary>
public static class Migration_202609080003_AddOnboardingPermissionSnapshot
{
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static void Up(ISqlSugarClient db)
    {
        // ── Step 1: DDL ──────────────────────────────────────────────────────────
        db.Ado.ExecuteCommand(@"
            ALTER TABLE ff_onboarding
                ADD COLUMN IF NOT EXISTS max_view_permission_mode SMALLINT NULL,
                ADD COLUMN IF NOT EXISTS max_view_teams JSONB NULL,
                ADD COLUMN IF NOT EXISTS max_operate_teams JSONB NULL;
        ");
        Console.WriteLine("[Migration] Step 1: Added max_* snapshot columns to ff_onboarding");

        // ── Step 2: SQL backfill (Workflow-level snapshot) ───────────────────────
        // Idempotent: only updates records where max_view_permission_mode IS NULL
        // Depends on runtime_use_same_as_template added by Migration_202609080001
        var sqlBackfillRows = db.Ado.ExecuteCommand(@"
            UPDATE ff_onboarding o
            SET
                max_view_permission_mode = CASE
                    WHEN w.runtime_use_same_as_template THEN w.view_permission_mode
                    ELSE w.runtime_view_permission_mode
                END,
                max_view_teams = CASE
                    WHEN w.runtime_use_same_as_template THEN w.view_teams
                    ELSE w.runtime_view_teams
                END,
                max_operate_teams = CASE
                    WHEN w.runtime_use_same_as_template THEN w.operate_teams
                    ELSE w.runtime_operate_teams
                END
            FROM ff_workflow w
            WHERE o.workflow_id = w.id
              AND o.is_valid = TRUE
              AND w.is_valid = TRUE
              AND o.max_view_permission_mode IS NULL;
        ");
        Console.WriteLine($"[Migration] Step 2: Backfilled max_* for {sqlBackfillRows} onboarding records");

        // ── Step 3: C# JSONB backfill (Stage-level MaxStage* snapshot) ──────────
        BackfillStageProgressSnapshots(db);
    }

    private static void BackfillStageProgressSnapshots(ISqlSugarClient db)
    {
        const int batchSize = 100;
        int offset = 0;
        int totalUpdated = 0;

        Console.WriteLine("[Migration] Step 3: Starting C# JSONB backfill for stages_progress_json...");

        while (true)
        {
            // Query batch of active onboardings with non-null stages_progress_json
            var rows = db.Ado.SqlQuery<OnboardingSnapshotRow>($@"
                SELECT o.id, o.workflow_id, o.stages_progress_json
                FROM ff_onboarding o
                WHERE o.is_valid = TRUE
                  AND o.stages_progress_json IS NOT NULL
                ORDER BY o.id
                LIMIT {batchSize} OFFSET {offset}
            ");

            if (rows == null || rows.Count == 0) break;

            foreach (var row in rows)
            {
                try
                {
                    // Deserialize StageProgress list
                    List<OnboardingStageProgressSnapshot> progressList;
                    try
                    {
                        progressList = JsonSerializer.Deserialize<List<OnboardingStageProgressSnapshot>>(
                            row.StagesProgressJson, _jsonOptions);
                    }
                    catch
                    {
                        Console.WriteLine($"[Migration] Warning: Could not deserialize stages_progress_json for onboarding {row.Id}, skipping");
                        continue;
                    }

                    if (progressList == null || progressList.Count == 0) continue;

                    // Idempotent check (per-stage): skip stages that already have a snapshot.
                    // Do NOT skip the entire onboarding just because one stage has been filled —
                    // later-added stages may still be missing their snapshot.
                    bool hasAnyUnfilled = progressList.Any(p => p.MaxStageViewPermissionMode == null);
                    if (!hasAnyUnfilled) continue;

                    // Load Workflow
                    var workflowRows = db.Ado.SqlQuery<WorkflowPermissionRow>(@"
                        SELECT id, runtime_use_same_as_template, view_permission_mode, view_teams,
                               operate_teams, runtime_view_permission_mode, runtime_view_teams, runtime_operate_teams
                        FROM ff_workflow
                        WHERE id = @workflowId AND is_valid = TRUE",
                        new SugarParameter("@workflowId", row.WorkflowId));

                    if (workflowRows == null || workflowRows.Count == 0)
                    {
                        Console.WriteLine($"[Migration] Warning: Workflow {row.WorkflowId} not found (is_valid=false) for onboarding {row.Id}, skipping");
                        continue;
                    }

                    var wf = workflowRows[0];

                    // Compute Workflow Effective Runtime
                    var wfEffectiveViewMode = wf.RuntimeUseSameAsTemplate
                        ? (ViewPermissionModeEnum)wf.ViewPermissionMode
                        : (ViewPermissionModeEnum)wf.RuntimeViewPermissionMode;
                    var wfEffectiveViewTeams = DeserializeStringList(wf.RuntimeUseSameAsTemplate ? wf.ViewTeams : wf.RuntimeViewTeams);
                    var wfEffectiveOperateTeams = DeserializeStringList(wf.RuntimeUseSameAsTemplate ? wf.OperateTeams : wf.RuntimeOperateTeams);

                    // Load Stages for this workflow
                    var stageRows = db.Ado.SqlQuery<StagePermissionRow>(@"
                        SELECT id, runtime_use_same_as_workflow, runtime_view_permission_mode,
                               runtime_view_teams, runtime_operate_teams, runtime_use_same_team_for_operate,
                               roll_back_teams
                        FROM ff_stage
                        WHERE workflow_id = @workflowId AND is_valid = TRUE",
                        new SugarParameter("@workflowId", row.WorkflowId));

                    var stageLookup = (stageRows ?? new List<StagePermissionRow>())
                        .ToDictionary(s => s.Id);

                    bool modified = false;
                    foreach (var progress in progressList)
                    {
                        if (!stageLookup.TryGetValue(progress.StageId, out var stage)) continue;

                        // Per-stage idempotent: skip stages whose snapshot was already computed
                        if (progress.MaxStageViewPermissionMode != null) continue;

                        // Compute Stage Effective Runtime
                        List<string> stageViewTeams;
                        List<string> stageOperateTeams;
                        List<string> stageRollBackTeams;
                        ViewPermissionModeEnum stageViewMode;
                        ViewPermissionModeEnum stageOperateMode;

                        if (stage.RuntimeUseSameAsWorkflow)
                        {
                            stageViewMode = wfEffectiveViewMode;
                            stageViewTeams = wfEffectiveViewTeams;
                            stageOperateMode = wfEffectiveViewMode; // same as view when inheriting
                            stageOperateTeams = wfEffectiveOperateTeams;
                        }
                        else
                        {
                            stageViewMode = (ViewPermissionModeEnum)stage.RuntimeViewPermissionMode;
                            stageViewTeams = DeserializeStringList(stage.RuntimeViewTeams);
                            if (stage.RuntimeUseSameTeamForOperate)
                            {
                                stageOperateMode = stageViewMode;
                                stageOperateTeams = stageViewTeams;
                            }
                            else
                            {
                                stageOperateMode = stageViewMode; // mode same, teams differ
                                stageOperateTeams = DeserializeStringList(stage.RuntimeOperateTeams);
                            }
                        }

                        stageRollBackTeams = DeserializeStringList(stage.RollBackTeams);

                        progress.MaxStageViewPermissionMode = stageViewMode;
                        progress.MaxStageViewTeams = stageViewTeams;
                        progress.MaxStageOperatePermissionMode = stageOperateMode;
                        progress.MaxStageOperateTeams = stageOperateTeams;
                        progress.MaxStageRollBackTeams = stageRollBackTeams;
                        modified = true;
                    }

                    if (modified)
                    {
                        var updatedJson = JsonSerializer.Serialize(progressList, _jsonOptions);
                        db.Ado.ExecuteCommand(@"
                            UPDATE ff_onboarding SET stages_progress_json = @json::jsonb WHERE id = @id",
                            new SugarParameter("@json", updatedJson),
                            new SugarParameter("@id", row.Id));
                        totalUpdated++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Migration] Warning: Error processing onboarding {row.Id}: {ex.Message}, skipping");
                }
            }

            offset += batchSize;
            if (rows.Count < batchSize) break;
        }

        Console.WriteLine($"[Migration] Step 3: Updated stages_progress_json for {totalUpdated} onboarding records");
    }

    private static List<string> DeserializeStringList(string json)
    {
        if (string.IsNullOrEmpty(json)) return new List<string>();
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>(); }
        catch { return new List<string>(); }
    }

    public static void Down(ISqlSugarClient db)
    {
        // Only remove the three new columns; backfill data in stages_progress_json is NOT reverted.
        db.Ado.ExecuteCommand(@"
            ALTER TABLE ff_onboarding
                DROP COLUMN IF EXISTS max_view_permission_mode,
                DROP COLUMN IF EXISTS max_view_teams,
                DROP COLUMN IF EXISTS max_operate_teams;
        ");
        Console.WriteLine("[Migration] Removed max_* snapshot columns from ff_onboarding (backfill data NOT reverted)");
    }

    // ── Internal projection types for raw SQL queries ────────────────────────

    private class OnboardingSnapshotRow
    {
        [SugarColumn(ColumnName = "id")]
        public long Id { get; set; }
        [SugarColumn(ColumnName = "workflow_id")]
        public long WorkflowId { get; set; }
        [SugarColumn(ColumnName = "stages_progress_json")]
        public string StagesProgressJson { get; set; }
    }

    private class WorkflowPermissionRow
    {
        [SugarColumn(ColumnName = "id")]
        public long Id { get; set; }
        [SugarColumn(ColumnName = "runtime_use_same_as_template")]
        public bool RuntimeUseSameAsTemplate { get; set; }
        [SugarColumn(ColumnName = "view_permission_mode")]
        public int ViewPermissionMode { get; set; }
        [SugarColumn(ColumnName = "view_teams")]
        public string ViewTeams { get; set; }
        [SugarColumn(ColumnName = "operate_teams")]
        public string OperateTeams { get; set; }
        [SugarColumn(ColumnName = "runtime_view_permission_mode")]
        public int RuntimeViewPermissionMode { get; set; }
        [SugarColumn(ColumnName = "runtime_view_teams")]
        public string RuntimeViewTeams { get; set; }
        [SugarColumn(ColumnName = "runtime_operate_teams")]
        public string RuntimeOperateTeams { get; set; }
    }

    private class StagePermissionRow
    {
        [SugarColumn(ColumnName = "id")]
        public long Id { get; set; }
        [SugarColumn(ColumnName = "runtime_use_same_as_workflow")]
        public bool RuntimeUseSameAsWorkflow { get; set; }
        [SugarColumn(ColumnName = "runtime_view_permission_mode")]
        public int RuntimeViewPermissionMode { get; set; }
        [SugarColumn(ColumnName = "runtime_view_teams")]
        public string RuntimeViewTeams { get; set; }
        [SugarColumn(ColumnName = "runtime_operate_teams")]
        public string RuntimeOperateTeams { get; set; }
        [SugarColumn(ColumnName = "runtime_use_same_team_for_operate")]
        public bool RuntimeUseSameTeamForOperate { get; set; }
        [SugarColumn(ColumnName = "roll_back_teams")]
        public string RollBackTeams { get; set; }
    }

    // Minimal projection of OnboardingStageProgress for backfill — only MaxStage* fields needed
    private class OnboardingStageProgressSnapshot
    {
        public long StageId { get; set; }

        // Idempotent check field
        public List<string> MaxStageViewTeams { get; set; }

        // Fields to write
        public ViewPermissionModeEnum? MaxStageViewPermissionMode { get; set; }
        public ViewPermissionModeEnum? MaxStageOperatePermissionMode { get; set; }
        public List<string> MaxStageOperateTeams { get; set; }
        public List<string> MaxStageRollBackTeams { get; set; }
    }
}
