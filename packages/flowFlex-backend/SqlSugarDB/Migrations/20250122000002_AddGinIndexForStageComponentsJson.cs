using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add GIN index for ff_stage.components_json to optimize JSONB queries
    /// </summary>
    public class Migration_20250122000002_AddGinIndexForStageComponentsJson
    {
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("[Migration] Adding GIN index for ff_stage.components_json...");

                // Create GIN index for JSONB operations on components_json
                var createIndexSql = @"
                    CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_ff_stage_components_json_gin 
                    ON ff_stage USING gin (components_json);";

                db.Ado.ExecuteCommand(createIndexSql);

                // Create additional index for specific JSONB path operations
                var createPathIndexSql = @"
                    CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_ff_stage_questionnaire_ids_gin 
                    ON ff_stage USING gin ((components_json -> 'QuestionnaireIds'));";

                db.Ado.ExecuteCommand(createPathIndexSql);

                Console.WriteLine("[Migration] Successfully added GIN indexes for ff_stage.components_json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Migration] Error adding GIN indexes: {ex.Message}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("[Migration] Dropping GIN indexes for ff_stage.components_json...");

                // Drop the indexes
                var dropIndex1Sql = "DROP INDEX CONCURRENTLY IF EXISTS idx_ff_stage_components_json_gin;";
                var dropIndex2Sql = "DROP INDEX CONCURRENTLY IF EXISTS idx_ff_stage_questionnaire_ids_gin;";

                db.Ado.ExecuteCommand(dropIndex1Sql);
                db.Ado.ExecuteCommand(dropIndex2Sql);

                Console.WriteLine("[Migration] Successfully dropped GIN indexes for ff_stage.components_json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Migration] Error dropping GIN indexes: {ex.Message}");
                throw;
            }
        }
    }
}