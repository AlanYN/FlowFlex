using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Fix next_retry_at column constraint in ff_events table
    /// Version: 2.0.0
    /// Created: 2025-01-22
    /// Description: Make next_retry_at column nullable to match Entity definition
    /// </summary>
    public class FixEventsNextRetryAtColumn_20250102000017
    {
        /// <summary>
        /// Execute migration - Fix next_retry_at column
        /// </summary>
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                // Check if ff_events table exists
                var tableExists = false;
                try
                {
                    var checkTableSql = @"
                        SELECT COUNT(*) 
                        FROM information_schema.tables 
                        WHERE table_name = 'ff_events'";
                    var result = db.Ado.GetScalar(checkTableSql);
                    tableExists = Convert.ToInt32(result) > 0;
                }
                catch
                {
                    // If check fails, assume table doesn't exist
                }

                if (tableExists)
                {
                    // Check if next_retry_at column has NOT NULL constraint
                    var hasNotNullConstraint = false;
                    try
                    {
                        var checkConstraintSql = @"
                            SELECT is_nullable 
                            FROM information_schema.columns 
                            WHERE table_name = 'ff_events' 
                            AND column_name = 'next_retry_at'";
                        var result = db.Ado.GetScalar(checkConstraintSql);
                        hasNotNullConstraint = result?.ToString() == "NO";
                    }
                    catch
                    {
                        // If check fails, assume column needs fixing
                        hasNotNullConstraint = true;
                    }

                    if (hasNotNullConstraint)
                    {
                        Console.WriteLine("🔧 修复 ff_events.next_retry_at 字段约束...");

                        // Drop the NOT NULL constraint on next_retry_at column
                        db.Ado.ExecuteCommand(@"
                            ALTER TABLE ff_events 
                            ALTER COLUMN next_retry_at DROP NOT NULL;
                        ");

                        Console.WriteLine("✅ 成功修复 ff_events.next_retry_at 字段约束");
                    }
                    else
                    {
                        Console.WriteLine("ℹ️ ff_events.next_retry_at 字段约束已正确");
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ ff_events 表不存在，跳过修复");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 修复 ff_events.next_retry_at 字段约束失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Rollback migration - Add NOT NULL constraint back
        /// </summary>
        public static void Down(ISqlSugarClient db)
        {
            try
            {
                // Note: We don't add NOT NULL constraint back as it would break existing functionality
                Console.WriteLine("⚠️ 回滚操作：保持 next_retry_at 字段为可空状态");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 回滚 ff_events.next_retry_at 字段约束失败: {ex.Message}");
                throw;
            }
        }
    }
}