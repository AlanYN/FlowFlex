using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Fix double-encoded assignee_json data
    /// </summary>
    public class Migration_20250122000022_FixAssigneeJsonEncoding
    {
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("[FixAssigneeJsonEncoding] Starting migration to fix double-encoded assignee_json data...");

                // Fix double-encoded JSON strings that start with triple quotes
                var fixedCount = db.Ado.ExecuteCommand(@"
                    UPDATE ff_checklist_task 
                    SET assignee_json = 
                        CASE 
                            -- Handle triple-quoted strings
                            WHEN assignee_json::text LIKE '""\""%' AND assignee_json::text LIKE '%\""\""%""'
                            THEN replace(replace(substring(assignee_json::text from 4 for length(assignee_json::text) - 6), '\\""', '""'), '\\u', '\u')::jsonb
                            -- Handle double-quoted strings  
                            WHEN assignee_json::text LIKE '""%' AND assignee_json::text LIKE '%""'
                            THEN replace(replace(substring(assignee_json::text from 2 for length(assignee_json::text) - 2), '\\""', '""'), '\\u', '\u')::jsonb
                            ELSE assignee_json
                        END
                    WHERE assignee_json IS NOT NULL 
                    AND (
                        assignee_json::text LIKE '""\""%' 
                        OR assignee_json::text LIKE '""%'
                    );
                ");

                Console.WriteLine($"[FixAssigneeJsonEncoding] Fixed {fixedCount} records with double-encoded JSON");

                // Additional cleanup for specific Unicode escaping issues
                try
                {
                    db.Ado.ExecuteCommand(@"
                        UPDATE ff_checklist_task 
                        SET assignee_json = 
                            replace(
                                replace(assignee_json::text, '\\u5F20\\u4E09', '张三'),
                                '\\u5F00\\u53D1\\u56E2\\u961F', '开发团队'
                            )::jsonb
                        WHERE assignee_json::text LIKE '%\\u5F20\\u4E09%' 
                        OR assignee_json::text LIKE '%\\u5F00\\u53D1\\u56E2\\u961F%';
                    ");
                    
                    Console.WriteLine("[FixAssigneeJsonEncoding] Fixed Unicode escaping in assignee names");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FixAssigneeJsonEncoding] Warning: Could not fix Unicode escaping: {ex.Message}");
                }

                Console.WriteLine("[FixAssigneeJsonEncoding] Migration completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FixAssigneeJsonEncoding] Migration failed: {ex.Message}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("[FixAssigneeJsonEncoding] Rolling back JSON encoding fix...");
                // Note: This rollback is not perfect since we're fixing malformed data
                // The original double-encoded data would be difficult to restore
                Console.WriteLine("[FixAssigneeJsonEncoding] Rollback completed (data remains in fixed state)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FixAssigneeJsonEncoding] Rollback failed: {ex.Message}");
                throw;
            }
        }
    }
}