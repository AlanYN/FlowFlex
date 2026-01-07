using System;
using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Convert legacy TEXT/VARCHAR *_json columns to JSONB
    /// </summary>
    public class ConvertTextJsonColumnsToJsonb_20250103000005
    {
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("[ConvertTextJsonColumnsToJsonb] Starting conversion...");

                // Helper local function
                void ConvertColumn(string table, string column, string indexName = null)
                {
                    var exists = db.Ado.GetDataTable($@"SELECT 1 FROM information_schema.columns WHERE table_name = '{table}' AND column_name = '{column}'").Rows.Count > 0;
                    if (!exists)
                    {
                        Console.WriteLine($"[ConvertTextJsonColumnsToJsonb] Column {table}.{column} not found, skip.");
                        return;
                    }

                    // Check if column is already jsonb type
                    var isJsonb = db.Ado.GetDataTable($@"
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = '{table}' AND column_name = '{column}' AND data_type = 'jsonb'
                    ").Rows.Count > 0;

                    if (isJsonb)
                    {
                        Console.WriteLine($"[ConvertTextJsonColumnsToJsonb] Column {table}.{column} is already jsonb, skip conversion.");
                    }
                    else
                    {
                        Console.WriteLine($"[ConvertTextJsonColumnsToJsonb] Converting {table}.{column} to jsonb...");

                        // First, clean up invalid JSON data by setting them to NULL
                        try
                        {
                            db.Ado.ExecuteCommand($@"
                                UPDATE {table}
                                SET {column} = NULL
                                WHERE {column} IS NOT NULL 
                                  AND {column} != ''
                                  AND {column} !~ '^\s*[\[{{].*[\]}}]\s*$';
                            ");
                            Console.WriteLine($"[ConvertTextJsonColumnsToJsonb] Cleaned invalid JSON data in {table}.{column}.");
                        }
                        catch (Exception cleanEx)
                        {
                            Console.WriteLine($"[ConvertTextJsonColumnsToJsonb] Warning: Could not clean data: {cleanEx.Message}");
                        }

                        // Now convert to jsonb with safer handling
                        db.Ado.ExecuteCommand($@"
                            ALTER TABLE {table}
                            ALTER COLUMN {column} TYPE jsonb
                            USING CASE 
                                WHEN {column} IS NULL OR {column} = '' OR TRIM({column}) = '' THEN NULL 
                                WHEN {column} ~ '^\s*[\[{{].*[\]}}]\s*$' THEN {column}::jsonb
                                ELSE NULL 
                            END;
                        ");
                    }

                    if (!string.IsNullOrWhiteSpace(indexName))
                    {
                        var idxExists = db.Ado.GetDataTable($@"SELECT 1 FROM pg_indexes WHERE tablename = '{table}' AND indexname = '{indexName}'").Rows.Count > 0;
                        if (!idxExists)
                        {
                            db.Ado.ExecuteCommand($@"CREATE INDEX {indexName} ON {table} USING GIN ({column});");
                            Console.WriteLine($"[ConvertTextJsonColumnsToJsonb] Created GIN index {indexName}.");
                        }
                    }
                }

                // ff_checklist_task.attachments_json
                ConvertColumn("ff_checklist_task", "attachments_json", "idx_ff_checklist_task_attachments_json_gin");

                // ff_onboarding.custom_fields_json
                ConvertColumn("ff_onboarding", "custom_fields_json", "idx_ff_onboarding_custom_fields_json_gin");

                // ff_questionnaire.structure_json
                ConvertColumn("ff_questionnaire", "structure_json", "idx_ff_questionnaire_structure_json_gin");

                // ff_questionnaire.tags_json
                ConvertColumn("ff_questionnaire", "tags_json", "idx_ff_questionnaire_tags_json_gin");

                // ff_questionnaire.assignments_json
                ConvertColumn("ff_questionnaire", "assignments_json", "idx_ff_questionnaire_assignments_json_gin");

                // ff_checklist.assignments_json
                ConvertColumn("ff_checklist", "assignments_json", "idx_ff_checklist_assignments_json_gin");

                // ff_questionnaire_answers.answer_json
                ConvertColumn("ff_questionnaire_answers", "answer_json", "idx_ff_questionnaire_answers_answer_json_gin");

                // ff_workflow.config_json (VARCHAR -> jsonb)
                ConvertColumn("ff_workflow", "config_json", "idx_ff_workflow_config_json_gin");

                Console.WriteLine("[ConvertTextJsonColumnsToJsonb] Conversion completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConvertTextJsonColumnsToJsonb] Error: {ex.Message}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("[ConvertTextJsonColumnsToJsonb] Rolling back...");

                void DropIndex(string table, string indexName)
                {
                    db.Ado.ExecuteCommand($"DROP INDEX IF EXISTS {indexName};");
                }

                void ConvertBack(string table, string column, string toType)
                {
                    db.Ado.ExecuteCommand($@"
                        ALTER TABLE {table}
                        ALTER COLUMN {column} TYPE {toType}
                        USING {column}::{toType};
                    ");
                }

                // Drop indexes
                DropIndex("ff_checklist_task", "idx_ff_checklist_task_attachments_json_gin");
                DropIndex("ff_onboarding", "idx_ff_onboarding_custom_fields_json_gin");
                DropIndex("ff_questionnaire", "idx_ff_questionnaire_structure_json_gin");
                DropIndex("ff_questionnaire", "idx_ff_questionnaire_tags_json_gin");
                DropIndex("ff_questionnaire", "idx_ff_questionnaire_assignments_json_gin");
                DropIndex("ff_checklist", "idx_ff_checklist_assignments_json_gin");
                DropIndex("ff_questionnaire_answers", "idx_ff_questionnaire_answers_answer_json_gin");
                DropIndex("ff_workflow", "idx_ff_workflow_config_json_gin");

                // Convert back types
                ConvertBack("ff_checklist_task", "attachments_json", "text");
                ConvertBack("ff_onboarding", "custom_fields_json", "text");
                ConvertBack("ff_questionnaire", "structure_json", "text");
                ConvertBack("ff_questionnaire", "tags_json", "text");
                ConvertBack("ff_questionnaire", "assignments_json", "text");
                ConvertBack("ff_checklist", "assignments_json", "text");
                ConvertBack("ff_questionnaire_answers", "answer_json", "text");
                ConvertBack("ff_workflow", "config_json", "varchar");

                Console.WriteLine("[ConvertTextJsonColumnsToJsonb] Rollback completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConvertTextJsonColumnsToJsonb] Rollback error: {ex.Message}");
                throw;
            }
        }
    }
}

