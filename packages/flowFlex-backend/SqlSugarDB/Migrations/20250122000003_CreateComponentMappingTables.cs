using SqlSugar;
using FlowFlex.SqlSugarDB.Context;
using System.Text.Json;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Complete component mapping tables creation with Snowflake IDs and data sync
    /// Consolidates all mapping table related migrations into one comprehensive migration
    /// </summary>
    public class Migration_20250122000003_CreateComponentMappingTables
    {
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("🚀 [ComponentMappingTablesComplete] Starting complete component mapping tables setup...");

                // Step 1: Create questionnaire-stage mapping table with Snowflake ID
                var createQuestionnaireMapping = @"
                    CREATE TABLE IF NOT EXISTS ff_questionnaire_stage_mapping (
                        id BIGINT PRIMARY KEY,
                        questionnaire_id BIGINT NOT NULL,
                        stage_id BIGINT NOT NULL,
                        workflow_id BIGINT NOT NULL,
                        tenant_id VARCHAR(50) NOT NULL DEFAULT 'DEFAULT',
                        app_code VARCHAR(50) NOT NULL DEFAULT 'DEFAULT',
                        is_valid BOOLEAN NOT NULL DEFAULT TRUE,
                        created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                        updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                        create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                        modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                        create_by VARCHAR(100) DEFAULT '',
                        modify_by VARCHAR(100) DEFAULT '',
                        create_user_id BIGINT DEFAULT 1,
                        modify_user_id BIGINT DEFAULT 1
                    );";

                db.Ado.ExecuteCommand(createQuestionnaireMapping);
                Console.WriteLine("✅ [ComponentMappingTablesComplete] Created ff_questionnaire_stage_mapping table with Snowflake ID");

                // Step 2: Create checklist-stage mapping table with Snowflake ID
                var createChecklistMapping = @"
                    CREATE TABLE IF NOT EXISTS ff_checklist_stage_mapping (
                        id BIGINT PRIMARY KEY,
                        checklist_id BIGINT NOT NULL,
                        stage_id BIGINT NOT NULL,
                        workflow_id BIGINT NOT NULL,
                        tenant_id VARCHAR(50) NOT NULL DEFAULT 'DEFAULT',
                        app_code VARCHAR(50) NOT NULL DEFAULT 'DEFAULT',
                        is_valid BOOLEAN NOT NULL DEFAULT TRUE,
                        created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                        updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                        create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                        modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                        create_by VARCHAR(100) DEFAULT '',
                        modify_by VARCHAR(100) DEFAULT '',
                        create_user_id BIGINT DEFAULT 1,
                        modify_user_id BIGINT DEFAULT 1
                    );";

                db.Ado.ExecuteCommand(createChecklistMapping);
                Console.WriteLine("✅ [ComponentMappingTablesComplete] Created ff_checklist_stage_mapping table with Snowflake ID");

                // Step 3: Create indexes
                var createIndexes = @"
                    -- Questionnaire mapping indexes
                    CREATE INDEX IF NOT EXISTS idx_questionnaire_mapping 
                    ON ff_questionnaire_stage_mapping (questionnaire_id, tenant_id, app_code);
                    
                    CREATE INDEX IF NOT EXISTS idx_questionnaire_stage_mapping 
                    ON ff_questionnaire_stage_mapping (stage_id, tenant_id, app_code);
                    
                    CREATE INDEX IF NOT EXISTS idx_questionnaire_workflow_mapping 
                    ON ff_questionnaire_stage_mapping (workflow_id, tenant_id, app_code);

                    -- Checklist mapping indexes
                    CREATE INDEX IF NOT EXISTS idx_checklist_mapping 
                    ON ff_checklist_stage_mapping (checklist_id, tenant_id, app_code);
                    
                    CREATE INDEX IF NOT EXISTS idx_checklist_stage_mapping 
                    ON ff_checklist_stage_mapping (stage_id, tenant_id, app_code);
                    
                    CREATE INDEX IF NOT EXISTS idx_checklist_workflow_mapping 
                    ON ff_checklist_stage_mapping (workflow_id, tenant_id, app_code);";

                db.Ado.ExecuteCommand(createIndexes);
                Console.WriteLine("✅ [ComponentMappingTablesComplete] Created mapping table indexes");

                // Step 4: Sync existing stage component data
                SyncExistingStageComponentData(db);

                Console.WriteLine("🎉 [ComponentMappingTablesComplete] Complete component mapping tables setup finished successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [ComponentMappingTablesComplete] Error: {ex.Message}");
                throw;
            }
        }

        private static void SyncExistingStageComponentData(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("[ComponentMappingTablesComplete] Starting to sync existing stage component data...");

                // Get all stages with components_json data
                var stages = db.Ado.GetDataTable(@"
                    SELECT id, workflow_id, components_json, tenant_id, app_code 
                    FROM ff_stage 
                    WHERE components_json IS NOT NULL 
                    AND components_json != '' 
                    AND components_json != 'null'
                    AND is_valid = true
                ");

                int totalStages = stages.Rows.Count;
                int processedStages = 0;
                int syncedMappings = 0;

                Console.WriteLine($"[ComponentMappingTablesComplete] Found {totalStages} stages with component data");

                foreach (System.Data.DataRow row in stages.Rows)
                {
                    try
                    {
                        var stageId = Convert.ToInt64(row["id"]);
                        var workflowId = Convert.ToInt64(row["workflow_id"]);
                        var componentsJson = row["components_json"]?.ToString();
                        var tenantId = row["tenant_id"]?.ToString() ?? "DEFAULT";
                        var appCode = row["app_code"]?.ToString() ?? "DEFAULT";

                        if (string.IsNullOrEmpty(componentsJson))
                        {
                            processedStages++;
                            continue;
                        }

                        // Parse components JSON
                        try
                        {
                            var normalized = TryUnwrapComponentsJson(componentsJson);
                            var components = JsonSerializer.Deserialize<List<StageComponent>>(normalized);

                            if (components != null)
                            {
                                foreach (var component in components)
                                {
                                    // Sync questionnaire mappings
                                    if (component.Key == "questionnaires" && component.QuestionnaireIds?.Any() == true)
                                    {
                                        foreach (var questionnaireId in component.QuestionnaireIds)
                                        {
                                            var exists = db.Ado.GetDataTable($@"
                                                SELECT 1 FROM ff_questionnaire_stage_mapping 
                                                WHERE questionnaire_id = {questionnaireId} 
                                                AND stage_id = {stageId}
                                            ").Rows.Count > 0;

                                            if (!exists)
                                            {
                                                // Generate Snowflake ID
                                                var snowflakeId = GenerateSnowflakeId();

                                                db.Ado.ExecuteCommand($@"
                                                    INSERT INTO ff_questionnaire_stage_mapping 
                                                    (id, questionnaire_id, stage_id, workflow_id, tenant_id, app_code, is_valid, created_at, updated_at, create_date, modify_date, create_by, modify_by, create_user_id, modify_user_id)
                                                    VALUES ({snowflakeId}, {questionnaireId}, {stageId}, {workflowId}, '{tenantId}', '{appCode}', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, '', '', 1, 1)
                                                ");
                                                syncedMappings++;
                                            }
                                        }
                                    }

                                    // Sync checklist mappings
                                    if (component.Key == "checklist" && component.ChecklistIds?.Any() == true)
                                    {
                                        foreach (var checklistId in component.ChecklistIds)
                                        {
                                            var exists = db.Ado.GetDataTable($@"
                                                SELECT 1 FROM ff_checklist_stage_mapping 
                                                WHERE checklist_id = {checklistId} 
                                                AND stage_id = {stageId}
                                            ").Rows.Count > 0;

                                            if (!exists)
                                            {
                                                // Generate Snowflake ID
                                                var snowflakeId = GenerateSnowflakeId();

                                                db.Ado.ExecuteCommand($@"
                                                    INSERT INTO ff_checklist_stage_mapping 
                                                    (id, checklist_id, stage_id, workflow_id, tenant_id, app_code, is_valid, created_at, updated_at, create_date, modify_date, create_by, modify_by, create_user_id, modify_user_id)
                                                    VALUES ({snowflakeId}, {checklistId}, {stageId}, {workflowId}, '{tenantId}', '{appCode}', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, '', '', 1, 1)
                                                ");
                                                syncedMappings++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            Console.WriteLine($"[ComponentMappingTablesComplete] JSON parsing error for stage {stageId}: {ex.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ComponentMappingTablesComplete] Error processing stage {row["id"]}: {ex.Message}");
                    }

                    processedStages++;

                    if (processedStages % 10 == 0)
                    {
                        Console.WriteLine($"[ComponentMappingTablesComplete] Progress: {processedStages}/{totalStages} stages processed, {syncedMappings} mappings synced");
                    }
                }

                Console.WriteLine($"✅ [ComponentMappingTablesComplete] Sync completed: {processedStages} stages processed, {syncedMappings} mappings synced");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [ComponentMappingTablesComplete] Error syncing data: {ex.Message}");
                throw;
            }
        }

        private static string TryUnwrapComponentsJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return "[]";

            try
            {
                // Try to parse as string first (handle double-wrapped JSON)
                if (json.StartsWith("\"") && json.EndsWith("\""))
                {
                    var unwrapped = JsonSerializer.Deserialize<string>(json);
                    return unwrapped ?? "[]";
                }

                // Validate JSON by attempting to parse
                JsonSerializer.Deserialize<JsonElement>(json);
                return json;
            }
            catch
            {
                Console.WriteLine($"[ComponentMappingTablesComplete] Invalid JSON format: {json.Substring(0, Math.Min(100, json.Length))}...");
                return "[]";
            }
        }

        private static long GenerateSnowflakeId()
        {
            // Simple snowflake-like ID generation for migration
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var random = new Random().Next(0, 4095); // 12-bit random number
            return (timestamp << 12) | random;
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("[ComponentMappingTablesComplete] Rolling back component mapping tables...");

                db.Ado.ExecuteCommand("DROP TABLE IF EXISTS ff_questionnaire_stage_mapping CASCADE;");
                db.Ado.ExecuteCommand("DROP TABLE IF EXISTS ff_checklist_stage_mapping CASCADE;");

                Console.WriteLine("✅ [ComponentMappingTablesComplete] Rollback completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [ComponentMappingTablesComplete] Rollback error: {ex.Message}");
                throw;
            }
        }
    }

    // Helper class for JSON deserialization
    public class StageComponent
    {
        public string Key { get; set; } = "";
        public List<long>? QuestionnaireIds { get; set; }
        public List<long>? ChecklistIds { get; set; }
    }
}