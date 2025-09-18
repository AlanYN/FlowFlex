using FlowFlex.Domain.Entities.OW;
using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// 创建AI Prompt历史记录表
    /// 
    /// 此迁移创建AI Prompt历史记录表，用于存储AI请求的详细信息，包括：
    /// - 输入的prompt内容和AI响应内容
    /// - 使用的AI模型信息（提供商、模型名称、模型ID）
    /// - 请求成功/失败状态和错误信息
    /// - 响应时间、Token使用量等性能指标
    /// - 关联的实体信息（Stage、Workflow、Onboarding等）
    /// - 用户信息和请求上下文（IP、User Agent等）
    /// - 额外的元数据（JSON格式）
    /// 
    /// 继承层次：AIPromptHistory -> EntityBaseCreateInfo -> EntityBase -> AbstractEntityBase -> IdEntityBase
    /// 
    /// 包含字段：
    /// - 基础字段：Id (雪花ID), TenantId, AppCode, IsValid
    /// - 审计字段：CreateDate, ModifyDate, CreateBy, ModifyBy, CreateUserId, ModifyUserId
    /// - AI专用字段：PromptType, EntityType, EntityId, OnboardingId, ModelProvider, ModelName, ModelId
    /// - 内容字段：PromptContent, ResponseContent, IsSuccess, ErrorMessage, ResponseTimeMs
    /// - 扩展字段：TokenUsage, Metadata, UserId, UserName, IpAddress, UserAgent
    /// 
    /// 好处：
    /// - 提供完整的AI使用情况审计跟踪
    /// - 支持AI性能分析和Prompt优化
    /// - 便于调试AI相关问题和异常情况
    /// - 支持用户行为分析和使用统计
    /// - 为AI成本控制和资源优化提供数据支持
    /// - 支持多租户和多应用隔离
    /// </summary>
    public class CreateAIPromptHistoryTable_20250125000001
    {
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("Starting CreateAIPromptHistoryTable migration...");

                // 创建AI Prompt历史记录表
                if (!db.DbMaintenance.IsAnyTable("ff_ai_prompt_history", false))
                {
                    Console.WriteLine("Creating AIPromptHistory table...");
                    db.CodeFirst.InitTables<AIPromptHistory>();
                    Console.WriteLine("AIPromptHistory table created successfully.");

                    // 验证表结构
                    var columns = db.DbMaintenance.GetColumnInfosByTableName("ff_ai_prompt_history", false);
                    Console.WriteLine($"Table created with {columns.Count} columns:");
                    foreach (var column in columns.Take(5)) // 只显示前5个列作为验证
                    {
                        Console.WriteLine($"  - {column.DbColumnName} ({column.DataType})");
                    }
                    Console.WriteLine("  - ... and more columns");

                    // 确保entity_id和onboarding_id列允许NULL值（修复约束问题）
                    Console.WriteLine("Ensuring nullable columns are properly configured...");
                    db.Ado.ExecuteCommand(@"
                        ALTER TABLE ff_ai_prompt_history 
                        ALTER COLUMN entity_id DROP NOT NULL;
                    ");
                    db.Ado.ExecuteCommand(@"
                        ALTER TABLE ff_ai_prompt_history 
                        ALTER COLUMN onboarding_id DROP NOT NULL;
                    ");

                    // 创建索引以提高查询性能
                    Console.WriteLine("Creating indexes on AIPromptHistory...");

                    // 1. 按实体类型和ID查询的索引（用于查找特定实体的AI历史）
                    db.Ado.ExecuteCommand(@"
                        CREATE INDEX IF NOT EXISTS idx_ai_prompt_history_entity 
                        ON ff_ai_prompt_history (entity_type, entity_id, create_date DESC) 
                        WHERE entity_type IS NOT NULL AND entity_id IS NOT NULL;
                    ");

                    // 2. 按Onboarding ID查询的索引（用于查找特定入职流程的AI历史）
                    db.Ado.ExecuteCommand(@"
                        CREATE INDEX IF NOT EXISTS idx_ai_prompt_history_onboarding 
                        ON ff_ai_prompt_history (onboarding_id, create_date DESC) 
                        WHERE onboarding_id IS NOT NULL;
                    ");

                    // 3. 按用户查询的索引（用于查找特定用户的AI使用历史）
                    db.Ado.ExecuteCommand(@"
                        CREATE INDEX IF NOT EXISTS idx_ai_prompt_history_user 
                        ON ff_ai_prompt_history (user_id, create_date DESC) 
                        WHERE user_id IS NOT NULL;
                    ");

                    // 4. 按Prompt类型查询的索引（用于分析不同AI功能的使用情况）
                    db.Ado.ExecuteCommand(@"
                        CREATE INDEX IF NOT EXISTS idx_ai_prompt_history_type 
                        ON ff_ai_prompt_history (prompt_type, create_date DESC);
                    ");

                    // 5. 按模型提供商查询的索引（用于分析不同AI模型的使用情况）
                    db.Ado.ExecuteCommand(@"
                        CREATE INDEX IF NOT EXISTS idx_ai_prompt_history_provider 
                        ON ff_ai_prompt_history (model_provider, create_date DESC) 
                        WHERE model_provider IS NOT NULL;
                    ");

                    // 6. 按成功状态查询的索引（用于查找失败的AI请求）
                    db.Ado.ExecuteCommand(@"
                        CREATE INDEX IF NOT EXISTS idx_ai_prompt_history_success 
                        ON ff_ai_prompt_history (is_success, create_date DESC);
                    ");

                    // 7. 按租户和应用过滤的复合索引（多租户支持）
                    db.Ado.ExecuteCommand(@"
                        CREATE INDEX IF NOT EXISTS idx_ai_prompt_history_tenant_app 
                        ON ff_ai_prompt_history (tenant_id, app_code, is_valid, create_date DESC);
                    ");

                    // 8. 按响应时间查询的索引（用于性能分析）
                    db.Ado.ExecuteCommand(@"
                        CREATE INDEX IF NOT EXISTS idx_ai_prompt_history_performance 
                        ON ff_ai_prompt_history (response_time_ms DESC, create_date DESC) 
                        WHERE response_time_ms IS NOT NULL;
                    ");

                    // 9. 按模型和类型的复合索引（用于分析特定模型在特定功能上的表现）
                    db.Ado.ExecuteCommand(@"
                        CREATE INDEX IF NOT EXISTS idx_ai_prompt_history_model_type 
                        ON ff_ai_prompt_history (model_provider, prompt_type, create_date DESC) 
                        WHERE model_provider IS NOT NULL;
                    ");

                    // 10. 按日期范围查询的索引（用于时间范围分析）
                    db.Ado.ExecuteCommand(@"
                        CREATE INDEX IF NOT EXISTS idx_ai_prompt_history_date_range 
                        ON ff_ai_prompt_history (create_date DESC, prompt_type, is_success);
                    ");

                    // 11. 用于清理旧记录的索引
                    db.Ado.ExecuteCommand(@"
                        CREATE INDEX IF NOT EXISTS idx_ai_prompt_history_cleanup 
                        ON ff_ai_prompt_history (is_valid, create_date) 
                        WHERE is_valid = true;
                    ");

                    Console.WriteLine("All 11 indexes created successfully.");

                    // 添加表注释（如果数据库支持）
                    try
                    {
                        db.Ado.ExecuteCommand(@"
                            COMMENT ON TABLE ff_ai_prompt_history IS 'AI Prompt历史记录表 - 存储所有AI请求的详细信息，用于审计、分析和优化';
                        ");
                        Console.WriteLine("Table comment added successfully.");
                    }
                    catch (Exception commentEx)
                    {
                        Console.WriteLine($"Warning: Could not add table comment: {commentEx.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("AIPromptHistory table already exists, skipping creation.");
                }

                Console.WriteLine("CreateAIPromptHistoryTable migration completed successfully.");
                Console.WriteLine("");
                Console.WriteLine("=== AI Prompt History 功能说明 ===");
                Console.WriteLine("1. 自动记录所有AI调用的prompt和响应");
                Console.WriteLine("2. 支持按实体、用户、模型等多维度查询");
                Console.WriteLine("3. 提供性能分析和成本统计数据");
                Console.WriteLine("4. 支持多租户和应用隔离");
                Console.WriteLine("5. 包含11个优化索引提升查询性能");
                Console.WriteLine("6. 支持自动清理旧记录功能");
                Console.WriteLine("=====================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateAIPromptHistoryTable migration: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("Rolling back CreateAIPromptHistoryTable migration...");

                // 删除所有索引
                Console.WriteLine("Dropping all AIPromptHistory indexes...");
                db.Ado.ExecuteCommand("DROP INDEX IF EXISTS idx_ai_prompt_history_entity;");
                db.Ado.ExecuteCommand("DROP INDEX IF EXISTS idx_ai_prompt_history_onboarding;");
                db.Ado.ExecuteCommand("DROP INDEX IF EXISTS idx_ai_prompt_history_user;");
                db.Ado.ExecuteCommand("DROP INDEX IF EXISTS idx_ai_prompt_history_type;");
                db.Ado.ExecuteCommand("DROP INDEX IF EXISTS idx_ai_prompt_history_provider;");
                db.Ado.ExecuteCommand("DROP INDEX IF EXISTS idx_ai_prompt_history_success;");
                db.Ado.ExecuteCommand("DROP INDEX IF EXISTS idx_ai_prompt_history_tenant_app;");
                db.Ado.ExecuteCommand("DROP INDEX IF EXISTS idx_ai_prompt_history_performance;");
                db.Ado.ExecuteCommand("DROP INDEX IF EXISTS idx_ai_prompt_history_model_type;");
                db.Ado.ExecuteCommand("DROP INDEX IF EXISTS idx_ai_prompt_history_date_range;");
                db.Ado.ExecuteCommand("DROP INDEX IF EXISTS idx_ai_prompt_history_cleanup;");
                Console.WriteLine("All indexes dropped successfully.");

                // 删除表
                if (db.DbMaintenance.IsAnyTable("ff_ai_prompt_history", false))
                {
                    Console.WriteLine("Dropping AIPromptHistory table...");
                    db.DbMaintenance.DropTable<AIPromptHistory>();
                    Console.WriteLine("AIPromptHistory table dropped successfully.");
                }

                Console.WriteLine("CreateAIPromptHistoryTable rollback completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rolling back CreateAIPromptHistoryTable migration: {ex.Message}");
                throw;
            }
        }
    }
}