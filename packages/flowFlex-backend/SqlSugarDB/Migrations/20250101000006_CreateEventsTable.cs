using SqlSugar;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// 创建事件表 ff_events
    /// Version: 2.0.0
    /// Created: 2025-01-01
    /// Description: Create events table for event sourcing and audit logging
    /// </summary>
    public class CreateEventsTable_20250101000006
    {
        /// <summary>
        /// 执行迁移
        /// </summary>
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                // 创建 ff_events 表
                db.CodeFirst.InitTables<Event>();

                // 创建索引
                CreateIndexes(db);

                Console.WriteLine("✅ 成功创建 ff_events 表及相关索引");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 创建 ff_events 表失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 回滚迁移
        /// </summary>
        public static void Down(ISqlSugarClient db)
        {
            try
            {
                // 删除索引
                DropIndexes(db);

                // 删除表
                db.DbMaintenance.DropTable<Event>();

                Console.WriteLine("✅ 成功删除 ff_events 表");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 删除 ff_events 表失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 创建索引
        /// </summary>
        private static void CreateIndexes(ISqlSugarClient db)
        {
            var tableName = "ff_events";

            // 1. 事件ID索引 (唯一)
            db.Ado.ExecuteCommand($@"
                CREATE UNIQUE INDEX IF NOT EXISTS idx_ff_events_event_id 
                ON {tableName} (event_id) 
                WHERE is_valid = true;
            ");

            // 2. 事件类型索引
            db.Ado.ExecuteCommand($@"
                CREATE INDEX IF NOT EXISTS idx_ff_events_event_type 
                ON {tableName} (event_type, event_timestamp DESC) 
                WHERE is_valid = true;
            ");

            // 3. 聚合根索引
            db.Ado.ExecuteCommand($@"
                CREATE INDEX IF NOT EXISTS idx_ff_events_aggregate 
                ON {tableName} (aggregate_id, aggregate_type, event_timestamp DESC) 
                WHERE is_valid = true;
            ");

            // 4. 租户ID索引
            db.Ado.ExecuteCommand($@"
                CREATE INDEX IF NOT EXISTS idx_ff_events_tenant 
                ON {tableName} (tenant_id, event_timestamp DESC) 
                WHERE is_valid = true;
            ");

            // 5. 事件状态索引
            db.Ado.ExecuteCommand($@"
                CREATE INDEX IF NOT EXISTS idx_ff_events_status 
                ON {tableName} (event_status, event_timestamp DESC) 
                WHERE is_valid = true;
            ");

            // 6. 事件来源索引
            db.Ado.ExecuteCommand($@"
                CREATE INDEX IF NOT EXISTS idx_ff_events_source 
                ON {tableName} (event_source, event_timestamp DESC) 
                WHERE is_valid = true;
            ");

            // 7. 重试相关索引
            db.Ado.ExecuteCommand($@"
                CREATE INDEX IF NOT EXISTS idx_ff_events_retry 
                ON {tableName} (requires_retry, next_retry_at, process_count) 
                WHERE is_valid = true AND requires_retry = true;
            ");

            // 8. 关联实体索引
            db.Ado.ExecuteCommand($@"
                CREATE INDEX IF NOT EXISTS idx_ff_events_related_entity 
                ON {tableName} (related_entity_id, related_entity_type, event_timestamp DESC) 
                WHERE is_valid = true AND related_entity_id IS NOT NULL;
            ");

            // 9. 事件时间戳索引
            db.Ado.ExecuteCommand($@"
                CREATE INDEX IF NOT EXISTS idx_ff_events_timestamp 
                ON {tableName} (event_timestamp DESC) 
                WHERE is_valid = true;
            ");

            // 10. 事件标签 GIN 索引 (用于 JSON 搜索)
            db.Ado.ExecuteCommand($@"
                CREATE INDEX IF NOT EXISTS idx_ff_events_tags_gin 
                ON {tableName} USING gin (event_tags) 
                WHERE is_valid = true;
            ");

            // 11. 事件数据 GIN 索引 (用于 JSON 搜索)
            db.Ado.ExecuteCommand($@"
                CREATE INDEX IF NOT EXISTS idx_ff_events_data_gin 
                ON {tableName} USING gin (event_data) 
                WHERE is_valid = true;
            ");

            // 12. 复合索引：租户 + 事件类型 + 时间戳
            db.Ado.ExecuteCommand($@"
                CREATE INDEX IF NOT EXISTS idx_ff_events_tenant_type_time 
                ON {tableName} (tenant_id, event_type, event_timestamp DESC) 
                WHERE is_valid = true;
            ");

            // 13. 复合索引：聚合根 + 事件类型 + 时间戳
            db.Ado.ExecuteCommand($@"
                CREATE INDEX IF NOT EXISTS idx_ff_events_aggregate_type_time 
                ON {tableName} (aggregate_id, event_type, event_timestamp DESC) 
                WHERE is_valid = true;
            ");

            Console.WriteLine("✅ 成功创建 ff_events 表索引");
        }

        /// <summary>
        /// 删除索引
        /// </summary>
        private static void DropIndexes(ISqlSugarClient db)
        {
            var indexes = new[]
            {
                "idx_ff_events_event_id",
                "idx_ff_events_event_type",
                "idx_ff_events_aggregate",
                "idx_ff_events_tenant",
                "idx_ff_events_status",
                "idx_ff_events_source",
                "idx_ff_events_retry",
                "idx_ff_events_related_entity",
                "idx_ff_events_timestamp",
                "idx_ff_events_tags_gin",
                "idx_ff_events_data_gin",
                "idx_ff_events_tenant_type_time",
                "idx_ff_events_aggregate_type_time"
            };

            foreach (var index in indexes)
            {
                try
                {
                    db.Ado.ExecuteCommand($"DROP INDEX IF EXISTS {index};");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ 删除索引 {index} 失败: {ex.Message}");
                }
            }

            Console.WriteLine("✅ 成功删除 ff_events 表索引");
        }
    }
} 