using SqlSugar;
using FlowFlex.SqlSugarDB.Context;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// 添加 is_ai_generated 字段到 ff_workflow 表
    /// </summary>
    public class Migration_20250101000003_AddIsAIGeneratedColumn
    {
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                // 检查字段是否已存在
                var columns = db.DbMaintenance.GetColumnInfosByTableName("ff_workflow");
                var isAIGeneratedExists = columns.Any(c => c.DbColumnName.Equals("is_ai_generated", StringComparison.OrdinalIgnoreCase));

                if (!isAIGeneratedExists)
                {
                    // 添加 is_ai_generated 字段
                    db.DbMaintenance.AddColumn("ff_workflow", new DbColumnInfo
                    {
                        DbColumnName = "is_ai_generated",
                        DataType = "BOOLEAN",
                        IsNullable = false,
                        DefaultValue = "false",
                        ColumnDescription = "标识该工作流是否由AI生成"
                    });

                    Console.WriteLine("✅ 已添加 is_ai_generated 字段到 ff_workflow 表");
                }
                else
                {
                    Console.WriteLine("ℹ️ is_ai_generated 字段已存在，跳过添加");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 添加 is_ai_generated 字段失败: {ex.Message}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                // 检查字段是否存在
                var columns = db.DbMaintenance.GetColumnInfosByTableName("ff_workflow");
                var isAIGeneratedExists = columns.Any(c => c.DbColumnName.Equals("is_ai_generated", StringComparison.OrdinalIgnoreCase));

                if (isAIGeneratedExists)
                {
                    // 删除 is_ai_generated 字段
                    db.DbMaintenance.DropColumn("ff_workflow", "is_ai_generated");
                    Console.WriteLine("✅ 已从 ff_workflow 表删除 is_ai_generated 字段");
                }
                else
                {
                    Console.WriteLine("ℹ️ is_ai_generated 字段不存在，跳过删除");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 删除 is_ai_generated 字段失败: {ex.Message}");
                throw;
            }
        }
    }
}