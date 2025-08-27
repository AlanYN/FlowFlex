using SqlSugar;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Create ChecklistTaskNote table
    /// </summary>
    public class _20250125000002_CreateChecklistTaskNoteTable
    {
        public static void Up(ISqlSugarClient db)
        {
            Console.WriteLine("[Migration] Creating ff_checklist_task_note table...");

            try
            {
                // Check if table exists first
                if (!db.DbMaintenance.IsAnyTable("ff_checklist_task_note", false))
                {
                    // Create table using SqlSugar CodeFirst
                    db.CodeFirst.InitTables<ChecklistTaskNote>();
                    Console.WriteLine("[Migration] Created ff_checklist_task_note table");
                }
                else
                {
                    Console.WriteLine("[Migration] ff_checklist_task_note table already exists");
                }

                // Create indexes for better query performance
                var indexes = new[]
                {
                    "CREATE INDEX IF NOT EXISTS idx_ff_checklist_task_note_task_onboarding ON ff_checklist_task_note (task_id, onboarding_id);",
                    "CREATE INDEX IF NOT EXISTS idx_ff_checklist_task_note_is_pinned ON ff_checklist_task_note (is_pinned) WHERE is_pinned = true;",
                    "CREATE INDEX IF NOT EXISTS idx_ff_checklist_task_note_is_deleted ON ff_checklist_task_note (is_deleted);",
                    "CREATE INDEX IF NOT EXISTS idx_ff_checklist_task_note_priority ON ff_checklist_task_note (priority);",
                    "CREATE INDEX IF NOT EXISTS idx_ff_checklist_task_note_note_type ON ff_checklist_task_note (note_type);",
                    "CREATE INDEX IF NOT EXISTS idx_ff_checklist_task_note_created_by ON ff_checklist_task_note (created_by_id);",
                    "CREATE INDEX IF NOT EXISTS idx_ff_checklist_task_note_create_date ON ff_checklist_task_note (create_date DESC);"
                };

                foreach (var indexSql in indexes)
                {
                    try
                    {
                        db.Ado.ExecuteCommand(indexSql);
                        Console.WriteLine($"[Migration] Created index: {indexSql.Split(' ')[5]}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Migration] Warning: Could not create index - {ex.Message}");
                    }
                }

                // Add table and column comments
                try
                {
                    var comments = new[]
                    {
                        "COMMENT ON TABLE ff_checklist_task_note IS 'Checklist Task Notes and Comments';",
                        "COMMENT ON COLUMN ff_checklist_task_note.task_id IS 'Associated Task ID';",
                        "COMMENT ON COLUMN ff_checklist_task_note.onboarding_id IS 'Associated Onboarding ID';",
                        "COMMENT ON COLUMN ff_checklist_task_note.content IS 'Note content (max 2000 chars)';",
                        "COMMENT ON COLUMN ff_checklist_task_note.note_type IS 'Note type (General, Security, Progress, Issue, etc.)';",
                        "COMMENT ON COLUMN ff_checklist_task_note.priority IS 'Priority level (High, Medium, Low)';",
                        "COMMENT ON COLUMN ff_checklist_task_note.created_by_id IS 'User ID who created the note';",
                        "COMMENT ON COLUMN ff_checklist_task_note.created_by_name IS 'User name who created the note';",
                        "COMMENT ON COLUMN ff_checklist_task_note.modified_by_id IS 'User ID who last modified the note';",
                        "COMMENT ON COLUMN ff_checklist_task_note.modified_by_name IS 'User name who last modified the note';",
                        "COMMENT ON COLUMN ff_checklist_task_note.is_deleted IS 'Soft delete flag';",
                        "COMMENT ON COLUMN ff_checklist_task_note.is_pinned IS 'Whether the note is pinned';"
                    };

                    foreach (var commentSql in comments)
                    {
                        db.Ado.ExecuteCommand(commentSql);
                    }
                    Console.WriteLine("[Migration] Added table and column comments");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Migration] Warning: Could not add comments - {ex.Message}");
                }

                Console.WriteLine("[Migration] Successfully created ChecklistTaskNote table and indexes");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Migration] Error creating ChecklistTaskNote table: {ex.Message}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            Console.WriteLine("[Migration] Rolling back ChecklistTaskNote table...");

            try
            {
                // Drop indexes first (if they exist)
                var indexNames = new[]
                {
                    "idx_ff_checklist_task_note_task_onboarding",
                    "idx_ff_checklist_task_note_is_pinned",
                    "idx_ff_checklist_task_note_is_deleted",
                    "idx_ff_checklist_task_note_priority",
                    "idx_ff_checklist_task_note_note_type",
                    "idx_ff_checklist_task_note_created_by",
                    "idx_ff_checklist_task_note_create_date"
                };

                foreach (var indexName in indexNames)
                {
                    try
                    {
                        db.Ado.ExecuteCommand($"DROP INDEX IF EXISTS {indexName};");
                    }
                    catch
                    {
                        // Ignore index drop errors
                    }
                }

                // Drop table if exists
                if (db.DbMaintenance.IsAnyTable("ff_checklist_task_note", false))
                {
                    db.Ado.ExecuteCommand("DROP TABLE ff_checklist_task_note;");
                    Console.WriteLine("[Migration] Dropped ff_checklist_task_note table");
                }

                Console.WriteLine("[Migration] Successfully rolled back ChecklistTaskNote table");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Migration] Error rolling back ChecklistTaskNote table: {ex.Message}");
                throw;
            }
        }
    }
}