using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// 为Action Trigger Mapping表添加工作流和阶段字段
    /// 
    /// 此迁移确保ff_action_trigger_mappings表包含work_flow_id和stage_id字段，
    /// 并且这些字段是可空的，以支持不同类型的触发器映射。
    /// </summary>
    public class AddWorkflowStageFieldsToActionMapping_20250904000001
    {
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                // Check if work_flow_id column exists, add if not
                var workflowIdExists = db.DbMaintenance.IsAnyColumn("ff_action_trigger_mappings", "work_flow_id");
                if (!workflowIdExists)
                {
                    db.Ado.ExecuteCommand(@"
                        ALTER TABLE ff_action_trigger_mappings 
                        ADD COLUMN work_flow_id BIGINT NULL;
                    ");
                    Console.WriteLine("Added work_flow_id column to ff_action_trigger_mappings table");
                }
                else
                {
                    // Ensure the column is nullable
                    db.Ado.ExecuteCommand(@"
                        ALTER TABLE ff_action_trigger_mappings 
                        ALTER COLUMN work_flow_id DROP NOT NULL;
                    ");
                    Console.WriteLine("Made work_flow_id column nullable in ff_action_trigger_mappings table");
                }

                // Check if stage_id column exists, add if not
                var stageIdExists = db.DbMaintenance.IsAnyColumn("ff_action_trigger_mappings", "stage_id");
                if (!stageIdExists)
                {
                    db.Ado.ExecuteCommand(@"
                        ALTER TABLE ff_action_trigger_mappings 
                        ADD COLUMN stage_id BIGINT NULL;
                    ");
                    Console.WriteLine("Added stage_id column to ff_action_trigger_mappings table");
                }
                else
                {
                    // Ensure the column is nullable
                    db.Ado.ExecuteCommand(@"
                        ALTER TABLE ff_action_trigger_mappings 
                        ALTER COLUMN stage_id DROP NOT NULL;
                    ");
                    Console.WriteLine("Made stage_id column nullable in ff_action_trigger_mappings table");
                }

                // Create index for workflow and stage based queries
                try
                {
                    db.Ado.ExecuteCommand(@"
                        CREATE INDEX IF NOT EXISTS idx_action_trigger_mappings_workflow_stage 
                        ON ff_action_trigger_mappings(work_flow_id, stage_id) 
                        WHERE work_flow_id IS NOT NULL OR stage_id IS NOT NULL;
                    ");
                    Console.WriteLine("Created index idx_action_trigger_mappings_workflow_stage");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Index creation failed (may already exist): {ex.Message}");
                }

                Console.WriteLine("Migration AddWorkflowStageFieldsToActionMapping_20250904000001 completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Migration AddWorkflowStageFieldsToActionMapping_20250904000001 failed: {ex.Message}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                // Drop the index
                db.Ado.ExecuteCommand(@"
                    DROP INDEX IF EXISTS idx_action_trigger_mappings_workflow_stage;
                ");

                // Note: We don't drop the columns in Down() as they might contain important data
                // If you really need to remove them, uncomment the lines below:
                /*
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_action_trigger_mappings 
                    DROP COLUMN IF EXISTS work_flow_id;
                ");
                
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_action_trigger_mappings 
                    DROP COLUMN IF EXISTS stage_id;
                ");
                */

                Console.WriteLine("Migration AddWorkflowStageFieldsToActionMapping_20250904000001 rollback completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Migration rollback AddWorkflowStageFieldsToActionMapping_20250904000001 failed: {ex.Message}");
                throw;
            }
        }
    }
}