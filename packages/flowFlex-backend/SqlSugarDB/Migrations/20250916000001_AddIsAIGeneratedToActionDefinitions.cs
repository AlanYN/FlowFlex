using SqlSugar;
using System;
using System.Linq;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// 为Action Definitions表添加AI生成标识字段和触发类型字段
    /// 
    /// 此迁移为ff_action_definitions表添加：
    /// 1. is_ai_generated字段：标识Action是否由AI生成，便于区分和管理AI生成的Actions
    /// 2. trigger_type字段：定义Action的触发类型(Stage、Task、Question、Workflow)，默认为Task
    /// </summary>
    public class AddIsAIGeneratedToActionDefinitions_20250916000001
    {
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                // 获取表列信息
                var columns = db.DbMaintenance.GetColumnInfosByTableName("ff_action_definitions");

                // 检查is_ai_generated字段是否已存在
                var isAIGeneratedExists = columns.Any(c => c.DbColumnName.Equals("is_ai_generated", StringComparison.OrdinalIgnoreCase));

                if (!isAIGeneratedExists)
                {
                    // 添加is_ai_generated字段
                    db.Ado.ExecuteCommand(@"
                        ALTER TABLE ff_action_definitions 
                        ADD COLUMN is_ai_generated BOOLEAN NOT NULL DEFAULT FALSE;
                    ");
                    Console.WriteLine("✅ Added is_ai_generated column to ff_action_definitions table");
                }
                else
                {
                    Console.WriteLine("ℹ️ is_ai_generated column already exists in ff_action_definitions table, skipping");
                }

                // 检查trigger_type字段是否已存在
                var triggerTypeExists = columns.Any(c => c.DbColumnName.Equals("trigger_type", StringComparison.OrdinalIgnoreCase));

                if (!triggerTypeExists)
                {
                    // 添加trigger_type字段（可空，不设置默认值）
                    db.Ado.ExecuteCommand(@"
                        ALTER TABLE ff_action_definitions 
                        ADD COLUMN trigger_type VARCHAR(50) NULL;
                    ");
                    Console.WriteLine("✅ Added trigger_type column to ff_action_definitions table");
                }
                else
                {
                    Console.WriteLine("ℹ️ trigger_type column already exists in ff_action_definitions table, skipping");
                }

                // 创建索引以提升查询性能（可选）
                try
                {
                    // 为is_ai_generated字段创建索引
                    db.Ado.ExecuteCommand(@"
                        CREATE INDEX IF NOT EXISTS idx_ff_action_definitions_is_ai_generated 
                        ON ff_action_definitions(is_ai_generated);
                    ");
                    Console.WriteLine("✅ Created index idx_ff_action_definitions_is_ai_generated");

                    // 为trigger_type字段创建索引
                    db.Ado.ExecuteCommand(@"
                        CREATE INDEX IF NOT EXISTS idx_ff_action_definitions_trigger_type 
                        ON ff_action_definitions(trigger_type);
                    ");
                    Console.WriteLine("✅ Created index idx_ff_action_definitions_trigger_type");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Index creation failed (may already exist): {ex.Message}");
                }

                // 检查是否存在默认的system action，如果不存在则创建
                try
                {
                    var systemActionExists = db.Ado.GetInt(@"
                        SELECT COUNT(*) FROM ff_action_definitions 
                        WHERE action_code = 'SYS-COMP-STG' AND tenant_id = 'Cyntest-UT'
                    ");

                    if (systemActionExists == 0)
                    {
                        // 插入默认的system action
                        db.Ado.ExecuteCommand(@"
                            INSERT INTO ff_action_definitions (
                                id, action_code, action_name, description, action_type, action_config, 
                                is_enabled, create_date, modify_date, tenant_id, app_code, 
                                create_user_id, modify_user_id, is_valid, trigger_type
                            ) VALUES (
                                1753249000001,
                                'SYS-COMP-STG',
                                'Complete Stage',
                                'Complete current stage with validation',
                                'System',
                                '{""actionName"":""CompleteStage"",""useValidationApi"":true}',
                                true,
                                NOW(),
                                NOW(),
                                'DEFAULT',
                                'DEFAULT',
                                1753248912142,
                                1753248912142,
                                true,
                                'Task'
                            );
                        ");
                        Console.WriteLine("✅ Inserted default system action 'SYS-COMP-STG'");
                    }
                    else
                    {
                        Console.WriteLine("ℹ️ Default system action 'SYS-COMP-STG' already exists, skipping");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Failed to insert default system action: {ex.Message}");
                }

                // 更新已存在的AI生成的Actions（如果有的话）
                // 这里可以根据实际需求添加逻辑来标识已存在的AI生成Actions
                // 例如：根据description包含"Auto-generated"或"AI"关键词的Actions
                try
                {
                    var updatedCount = db.Ado.ExecuteCommand(@"
                        UPDATE ff_action_definitions 
                        SET is_ai_generated = true 
                        WHERE description LIKE '%Auto-generated%' 
                           OR description LIKE '%AI generated%'
                           OR action_name LIKE '%ai_%'
                           OR action_name LIKE '%auto_%';
                    ");
                    Console.WriteLine($"✅ Updated {updatedCount} existing AI-generated actions");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Failed to update existing AI-generated actions: {ex.Message}");
                }

                Console.WriteLine("🎉 Migration AddIsAIGeneratedToActionDefinitions_20250916000001 completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Migration AddIsAIGeneratedToActionDefinitions_20250916000001 failed: {ex.Message}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                // 删除此迁移中插入的默认system action
                try
                {
                    var deletedCount = db.Ado.ExecuteCommand(@"
                        DELETE FROM ff_action_definitions 
                        WHERE id = 1753249000001 
                          AND action_code = 'SYS-COMP-STG' 
                          AND tenant_id = 'Cyntest-UT';
                    ");
                    Console.WriteLine($"✅ Deleted {deletedCount} default system action(s) inserted by this migration");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Failed to delete default system action: {ex.Message}");
                }

                // 删除索引
                try
                {
                    // 删除is_ai_generated字段的索引
                    db.Ado.ExecuteCommand(@"
                        DROP INDEX IF EXISTS idx_ff_action_definitions_is_ai_generated;
                    ");
                    Console.WriteLine("✅ Dropped index idx_ff_action_definitions_is_ai_generated");

                    // 删除trigger_type字段的索引
                    db.Ado.ExecuteCommand(@"
                        DROP INDEX IF EXISTS idx_ff_action_definitions_trigger_type;
                    ");
                    Console.WriteLine("✅ Dropped index idx_ff_action_definitions_trigger_type");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Failed to drop indexes: {ex.Message}");
                }

                // 获取表列信息
                var columns = db.DbMaintenance.GetColumnInfosByTableName("ff_action_definitions");

                // 检查is_ai_generated字段是否存在
                var isAIGeneratedExists = columns.Any(c => c.DbColumnName.Equals("is_ai_generated", StringComparison.OrdinalIgnoreCase));

                if (isAIGeneratedExists)
                {
                    // 删除is_ai_generated字段
                    db.Ado.ExecuteCommand(@"
                        ALTER TABLE ff_action_definitions 
                        DROP COLUMN IF EXISTS is_ai_generated;
                    ");
                    Console.WriteLine("✅ Dropped is_ai_generated column from ff_action_definitions table");
                }
                else
                {
                    Console.WriteLine("ℹ️ is_ai_generated column does not exist in ff_action_definitions table, skipping");
                }

                // 检查trigger_type字段是否存在
                var triggerTypeExists = columns.Any(c => c.DbColumnName.Equals("trigger_type", StringComparison.OrdinalIgnoreCase));

                if (triggerTypeExists)
                {
                    // 删除trigger_type字段
                    db.Ado.ExecuteCommand(@"
                        ALTER TABLE ff_action_definitions 
                        DROP COLUMN IF EXISTS trigger_type;
                    ");
                    Console.WriteLine("✅ Dropped trigger_type column from ff_action_definitions table");
                }
                else
                {
                    Console.WriteLine("ℹ️ trigger_type column does not exist in ff_action_definitions table, skipping");
                }

                Console.WriteLine("🎉 Migration rollback AddIsAIGeneratedToActionDefinitions_20250916000001 completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Migration rollback AddIsAIGeneratedToActionDefinitions_20250916000001 failed: {ex.Message}");
                throw;
            }
        }
    }
}
