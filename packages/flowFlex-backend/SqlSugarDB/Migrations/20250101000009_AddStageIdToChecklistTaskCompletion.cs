using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add stage_id column to ff_checklist_task_completion table
    /// </summary>
    public class AddStageIdToChecklistTaskCompletion_20250101000009
    {
        /// <summary>
        /// Execute migration to add stage_id column
        /// </summary>
        public static void Up(ISqlSugarClient db)
        {
            // Check if stage_id column exists in ff_checklist_task_completion table
            var checkColumnSql = @"
                SELECT COUNT(*) 
                FROM information_schema.columns 
                WHERE table_name = 'ff_checklist_task_completion' 
                AND column_name = 'stage_id';
            ";

            var columnExists = db.Ado.GetInt(checkColumnSql) > 0;

            if (!columnExists)
            {
                // Add stage_id column
                var addColumnSql = @"
                    ALTER TABLE ff_checklist_task_completion 
                    ADD COLUMN stage_id BIGINT NULL;
                ";

                db.Ado.ExecuteCommand(addColumnSql);

                // Add index for stage_id
                var addIndexSql = @"
                    CREATE INDEX IF NOT EXISTS idx_checklist_task_completion_stage_id 
                    ON ff_checklist_task_completion(stage_id);
                ";

                db.Ado.ExecuteCommand(addIndexSql);

                // Add composite index for onboarding_id and stage_id
                var addCompositeIndexSql = @"
                    CREATE INDEX IF NOT EXISTS idx_checklist_task_completion_onboarding_stage 
                    ON ff_checklist_task_completion(onboarding_id, stage_id);
                ";

                db.Ado.ExecuteCommand(addCompositeIndexSql);

                Console.WriteLine("Successfully added stage_id column and indexes to ff_checklist_task_completion table");
            }
            else
            {
                Console.WriteLine("stage_id column already exists in ff_checklist_task_completion table, skipping migration");
            }
        }

        /// <summary>
        /// Rollback migration to remove stage_id column
        /// </summary>
        public static void Down(ISqlSugarClient db)
        {
            // Remove the indexes first
            var dropCompositeIndexSql = @"
                DROP INDEX IF EXISTS idx_checklist_task_completion_onboarding_stage;
            ";

            var dropIndexSql = @"
                DROP INDEX IF EXISTS idx_checklist_task_completion_stage_id;
            ";

            var dropColumnSql = @"
                ALTER TABLE ff_checklist_task_completion 
                DROP COLUMN IF EXISTS stage_id;
            ";

            db.Ado.ExecuteCommand(dropCompositeIndexSql);
            db.Ado.ExecuteCommand(dropIndexSql);
            db.Ado.ExecuteCommand(dropColumnSql);

            Console.WriteLine("Successfully removed stage_id column and indexes from ff_checklist_task_completion table");
        }
    }
}