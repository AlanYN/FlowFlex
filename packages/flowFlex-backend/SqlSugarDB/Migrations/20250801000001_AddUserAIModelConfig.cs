using FlowFlex.Domain.Entities.OW;
using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// 添加用户AI模型配置表
    /// 
    /// 此迁移创建用户AI模型配置表，用于存储用户自定义的AI模型配置信息
    /// 
    /// 好处：
    /// - 支持用户自定义AI模型配置
    /// - 允许用户选择不同的AI提供商（ZhipuAI, OpenAI, Claude, DeepSeek等）
    /// - 提供连接测试功能，确保配置的可用性
    /// - 允许设置默认AI模型配置
    /// </summary>
    public class AddUserAIModelConfig_20250801000001
    {
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("Starting AddUserAIModelConfig migration...");

                // 创建用户AI模型配置表
                if (!db.DbMaintenance.IsAnyTable("ff_aimodel_config", false))
                {
                    Console.WriteLine("Creating AIModelConfig table...");
                    db.CodeFirst.InitTables<AIModelConfig>();
                    Console.WriteLine("AIModelConfig table created successfully.");

                    // 表创建成功，默认配置数据已移除，请通过UI界面手动添加配置
                    Console.WriteLine("Default AI model configurations added successfully.");

                    // 创建索引以提高查询性能
                    Console.WriteLine("Creating indexes on AIModelConfig...");
                    
                    // 使用新的表名 ff_aimodel_config
                    db.Ado.ExecuteCommand(@"
                        CREATE INDEX IF NOT EXISTS idx_user_ai_model_config_user_id 
                        ON ff_aimodel_config (user_id);
                    ");
                    
                    db.Ado.ExecuteCommand(@"
                        CREATE INDEX IF NOT EXISTS idx_user_ai_model_config_provider 
                        ON ff_aimodel_config (provider);
                    ");
                    
                    db.Ado.ExecuteCommand(@"
                        CREATE INDEX IF NOT EXISTS idx_user_ai_model_config_is_default 
                        ON ff_aimodel_config (is_default);
                    ");
                    
                    Console.WriteLine("Indexes created successfully.");
                }
                else
                {
                    Console.WriteLine("AIModelConfig table already exists, skipping creation.");
                }

                // 记录完成
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                Console.WriteLine($"Migration AddUserAIModelConfig_20250801000001 completed at {timestamp}");
                Console.WriteLine("Benefits: Added support for user-defined AI model configurations with multiple providers.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddUserAIModelConfig migration: {ex.Message}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("Rolling back AddUserAIModelConfig migration...");
                
                // 先删除索引
                Console.WriteLine("Dropping indexes on AIModelConfig...");
                
                db.Ado.ExecuteCommand(@"
                    DROP INDEX IF EXISTS idx_user_ai_model_config_user_id;
                ");
                
                db.Ado.ExecuteCommand(@"
                    DROP INDEX IF EXISTS idx_user_ai_model_config_provider;
                ");
                
                db.Ado.ExecuteCommand(@"
                    DROP INDEX IF EXISTS idx_user_ai_model_config_is_default;
                ");
                
                // 检查表是否存在
                if (db.DbMaintenance.IsAnyTable("ff_aimodel_config", false))
                {
                    // 删除表
                    Console.WriteLine("Dropping AIModelConfig table...");
                    
                    db.DbMaintenance.DropTable("ff_aimodel_config");
                    
                    Console.WriteLine("AIModelConfig table dropped successfully.");
                }
                else
                {
                    Console.WriteLine("AIModelConfig table does not exist, skipping removal.");
                }
                
                Console.WriteLine("AddUserAIModelConfig migration rollback completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rolling back AddUserAIModelConfig migration: {ex.Message}");
                throw;
            }
        }
    }
} 