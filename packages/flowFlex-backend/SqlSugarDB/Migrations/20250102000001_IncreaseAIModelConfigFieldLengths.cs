using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// 增加AI模型配置表字段长度限制
    /// 
    /// 此迁移修改AI模型配置表的字段长度限制，解决长API密钥和URL无法存储的问题
    /// 
    /// 修改内容：
    /// - api_key字段从varchar(500)扩展到varchar(2000)，支持长JWT token
    /// - base_url字段从varchar(500)扩展到varchar(1000)，支持更长的API地址
    /// </summary>
    public class IncreaseAIModelConfigFieldLengths_20250102000001
    {
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("Starting IncreaseAIModelConfigFieldLengths migration...");

                // 检查表是否存在
                if (db.DbMaintenance.IsAnyTable("ff_aimodel_config", false))
                {
                    Console.WriteLine("Updating AIModelConfig table field lengths...");

                    // 修改api_key字段长度为2000
                    db.Ado.ExecuteCommand(@"
                        ALTER TABLE ff_aimodel_config 
                        ALTER COLUMN api_key TYPE varchar(2000);
                    ");
                    Console.WriteLine("Updated api_key field length to 2000 characters.");

                    // 修改base_url字段长度为1000
                    db.Ado.ExecuteCommand(@"
                        ALTER TABLE ff_aimodel_config 
                        ALTER COLUMN base_url TYPE varchar(1000);
                    ");
                    Console.WriteLine("Updated base_url field length to 1000 characters.");

                    Console.WriteLine("AIModelConfig field lengths updated successfully.");
                }
                else
                {
                    Console.WriteLine("AIModelConfig table does not exist, skipping field length updates.");
                }

                // 记录完成
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                Console.WriteLine($"Migration IncreaseAIModelConfigFieldLengths_20250102000001 completed at {timestamp}");
                Console.WriteLine("Benefits: Increased field lengths to support longer API keys (JWT tokens) and URLs.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in IncreaseAIModelConfigFieldLengths migration: {ex.Message}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("Rolling back IncreaseAIModelConfigFieldLengths migration...");

                // 检查表是否存在
                if (db.DbMaintenance.IsAnyTable("ff_aimodel_config", false))
                {
                    Console.WriteLine("Reverting AIModelConfig field lengths...");

                    // 恢复api_key字段长度为500（注意：如果有超长数据会失败）
                    try
                    {
                        db.Ado.ExecuteCommand(@"
                            ALTER TABLE ff_aimodel_config 
                            ALTER COLUMN api_key TYPE varchar(500);
                        ");
                        Console.WriteLine("Reverted api_key field length to 500 characters.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Could not revert api_key field length (may have data longer than 500 chars): {ex.Message}");
                    }

                    // 恢复base_url字段长度为500
                    try
                    {
                        db.Ado.ExecuteCommand(@"
                            ALTER TABLE ff_aimodel_config 
                            ALTER COLUMN base_url TYPE varchar(500);
                        ");
                        Console.WriteLine("Reverted base_url field length to 500 characters.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Could not revert base_url field length (may have data longer than 500 chars): {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("AIModelConfig table does not exist, skipping field length reversion.");
                }

                Console.WriteLine("IncreaseAIModelConfigFieldLengths migration rollback completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rolling back IncreaseAIModelConfigFieldLengths migration: {ex.Message}");
                throw;
            }
        }
    }
}