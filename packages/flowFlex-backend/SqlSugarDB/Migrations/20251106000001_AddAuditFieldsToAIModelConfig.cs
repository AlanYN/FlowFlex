using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add audit fields to AIModelConfig table
    /// Adds: created_by, created_by_name, created_time, updated_by, updated_by_name, updated_time
    /// 
    /// 此迁移为 AI 模型配置表添加审计字段，用于追踪创建和修改记录
    /// 
    /// 好处：
    /// - 完整的审计追踪：记录谁创建、谁修改了配置
    /// - 时间追踪：记录创建和修改的时间戳
    /// - 用户友好：同时记录用户ID和用户名，便于显示
    /// - 合规性：满足审计和合规要求
    /// </summary>
    public class Migration_20251106000001_AddAuditFieldsToAIModelConfig
    {
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("Starting AddAuditFieldsToAIModelConfig migration...");

                // Add created_by column (创建人ID)
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_aimodel_config 
                    ADD COLUMN IF NOT EXISTS created_by BIGINT;
                ");

                // Add created_by_name column (创建人用户名)
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_aimodel_config 
                    ADD COLUMN IF NOT EXISTS created_by_name VARCHAR(100);
                ");

                // Add created_time column (创建时间)
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_aimodel_config 
                    ADD COLUMN IF NOT EXISTS created_time TIMESTAMP WITH TIME ZONE;
                ");

                // Add updated_by column (修改人ID)
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_aimodel_config 
                    ADD COLUMN IF NOT EXISTS updated_by BIGINT;
                ");

                // Add updated_by_name column (修改人用户名)
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_aimodel_config 
                    ADD COLUMN IF NOT EXISTS updated_by_name VARCHAR(100);
                ");

                // Add updated_time column (修改时间)
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_aimodel_config 
                    ADD COLUMN IF NOT EXISTS updated_time TIMESTAMP WITH TIME ZONE;
                ");

                Console.WriteLine("Audit fields added successfully.");

                // Add comments to columns
                db.Ado.ExecuteCommand(@"
                    COMMENT ON COLUMN ff_aimodel_config.created_by IS 'Creator user ID';
                ");

                db.Ado.ExecuteCommand(@"
                    COMMENT ON COLUMN ff_aimodel_config.created_by_name IS 'Creator username';
                ");

                db.Ado.ExecuteCommand(@"
                    COMMENT ON COLUMN ff_aimodel_config.created_time IS 'Creation timestamp (UTC)';
                ");

                db.Ado.ExecuteCommand(@"
                    COMMENT ON COLUMN ff_aimodel_config.updated_by IS 'Last modifier user ID';
                ");

                db.Ado.ExecuteCommand(@"
                    COMMENT ON COLUMN ff_aimodel_config.updated_by_name IS 'Last modifier username';
                ");

                db.Ado.ExecuteCommand(@"
                    COMMENT ON COLUMN ff_aimodel_config.updated_time IS 'Last modification timestamp (UTC)';
                ");

                Console.WriteLine("Column comments added successfully.");

                // Create indexes for better query performance
                db.Ado.ExecuteCommand(@"
                    CREATE INDEX IF NOT EXISTS idx_aimodel_config_created_by 
                    ON ff_aimodel_config(created_by);
                ");

                db.Ado.ExecuteCommand(@"
                    CREATE INDEX IF NOT EXISTS idx_aimodel_config_updated_by 
                    ON ff_aimodel_config(updated_by);
                ");

                db.Ado.ExecuteCommand(@"
                    CREATE INDEX IF NOT EXISTS idx_aimodel_config_created_time 
                    ON ff_aimodel_config(created_time);
                ");

                db.Ado.ExecuteCommand(@"
                    CREATE INDEX IF NOT EXISTS idx_aimodel_config_updated_time 
                    ON ff_aimodel_config(updated_time);
                ");

                Console.WriteLine("Indexes created successfully.");

                // Update existing records with default values
                db.Ado.ExecuteCommand(@"
                    UPDATE ff_aimodel_config 
                    SET 
                        created_by = user_id,
                        created_by_name = 'system',
                        created_time = NOW(),
                        updated_by = user_id,
                        updated_by_name = 'system',
                        updated_time = NOW()
                    WHERE created_by IS NULL;
                ");

                Console.WriteLine("Existing records updated with default audit values.");

                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                Console.WriteLine($"Migration AddAuditFieldsToAIModelConfig completed at {timestamp}");
                Console.WriteLine("Benefits: Added comprehensive audit trail for AI model configurations.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddAuditFieldsToAIModelConfig migration: {ex.Message}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("Rolling back AddAuditFieldsToAIModelConfig migration...");

                // Drop indexes
                db.Ado.ExecuteCommand(@"
                    DROP INDEX IF EXISTS idx_aimodel_config_updated_time;
                ");

                db.Ado.ExecuteCommand(@"
                    DROP INDEX IF EXISTS idx_aimodel_config_created_time;
                ");

                db.Ado.ExecuteCommand(@"
                    DROP INDEX IF EXISTS idx_aimodel_config_updated_by;
                ");

                db.Ado.ExecuteCommand(@"
                    DROP INDEX IF EXISTS idx_aimodel_config_created_by;
                ");

                Console.WriteLine("Indexes dropped successfully.");

                // Drop columns
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_aimodel_config 
                    DROP COLUMN IF EXISTS updated_time;
                ");

                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_aimodel_config 
                    DROP COLUMN IF EXISTS updated_by_name;
                ");

                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_aimodel_config 
                    DROP COLUMN IF EXISTS updated_by;
                ");

                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_aimodel_config 
                    DROP COLUMN IF EXISTS created_time;
                ");

                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_aimodel_config 
                    DROP COLUMN IF EXISTS created_by_name;
                ");

                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_aimodel_config 
                    DROP COLUMN IF EXISTS created_by;
                ");

                Console.WriteLine("Audit fields dropped successfully.");
                Console.WriteLine("AddAuditFieldsToAIModelConfig migration rollback completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rolling back AddAuditFieldsToAIModelConfig migration: {ex.Message}");
                throw;
            }
        }
    }
}

