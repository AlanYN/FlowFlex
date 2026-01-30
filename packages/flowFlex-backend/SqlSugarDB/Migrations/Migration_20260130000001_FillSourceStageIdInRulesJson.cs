using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations;

/// <summary>
/// Migration to fill sourceStageId in rules_json for existing stage conditions.
/// For existing data, the sourceStageId will be set to the stage_id of the stage condition.
/// </summary>
public static class Migration_20260130000001_FillSourceStageIdInRulesJson
{
    public static void Up(ISqlSugarClient db)
    {
        // Update rules_json to add sourceStageId to each rule if not present
        // The sourceStageId will be set to the stage_id of the stage condition
        var sql = @"
            UPDATE ff_stage_condition
            SET rules_json = (
                SELECT jsonb_set(
                    rules_json,
                    '{rules}',
                    (
                        SELECT jsonb_agg(
                            CASE 
                                WHEN rule->>'sourceStageId' IS NULL OR rule->>'sourceStageId' = ''
                                THEN rule || jsonb_build_object('sourceStageId', stage_id::text)
                                ELSE rule
                            END
                        )
                        FROM jsonb_array_elements(rules_json->'rules') AS rule
                    )
                )
            )
            WHERE is_valid = true
              AND rules_json IS NOT NULL
              AND rules_json->'rules' IS NOT NULL
              AND jsonb_array_length(rules_json->'rules') > 0
              AND EXISTS (
                  SELECT 1 
                  FROM jsonb_array_elements(rules_json->'rules') AS rule
                  WHERE rule->>'sourceStageId' IS NULL OR rule->>'sourceStageId' = ''
              );
        ";

        var affectedRows = db.Ado.ExecuteCommand(sql);
        Console.WriteLine($"[Migration] Updated {affectedRows} stage conditions with sourceStageId in rules_json");
    }

    public static void Down(ISqlSugarClient db)
    {
        // Remove sourceStageId from rules_json (optional rollback)
        // Note: This will remove sourceStageId from all rules, which may not be desired
        // if some rules had sourceStageId set manually
        Console.WriteLine("[Migration] Rollback: sourceStageId removal is not implemented to preserve manually set values");
    }
}
