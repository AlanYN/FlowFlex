using SqlSugar;
using FlowFlex.SqlSugarDB.Context;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// 添加 assignments_json 字段到 ff_checklist 表
    /// </summary>
    public class Migration_20250101000002_AddAssignmentsJsonColumn
    {
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                // 检查字段是否已存在
                var columns = db.DbMaintenance.GetColumnInfosByTableName("ff_checklist");
                var assignmentsJsonExists = columns.Any(c => c.DbColumnName.Equals("assignments_json", StringComparison.OrdinalIgnoreCase));

                if (!assignmentsJsonExists)
                {
                    // 添加 assignments_json 字段
                    db.DbMaintenance.AddColumn("ff_checklist", new DbColumnInfo
                    {
                        DbColumnName = "assignments_json",
                        DataType = "TEXT",
                        IsNullable = true,
                        ColumnDescription = "存储多个 workflow-stage 组合的 JSON 数据"
                    });

                    Console.WriteLine("✅ 已添加 assignments_json 字段到 ff_checklist 表");
                }
                else
                {
                    Console.WriteLine("ℹ️ assignments_json 字段已存在，跳过添加");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 添加 assignments_json 字段失败: {ex.Message}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                // 检查字段是否存在
                var columns = db.DbMaintenance.GetColumnInfosByTableName("ff_checklist");
                var assignmentsJsonExists = columns.Any(c => c.DbColumnName.Equals("assignments_json", StringComparison.OrdinalIgnoreCase));

                if (assignmentsJsonExists)
                {
                    // 删除 assignments_json 字段
                    db.DbMaintenance.DropColumn("ff_checklist", "assignments_json");
                    Console.WriteLine("✅ 已从 ff_checklist 表删除 assignments_json 字段");
                }
                else
                {
                    Console.WriteLine("ℹ️ assignments_json 字段不存在，跳过删除");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 删除 assignments_json 字段失败: {ex.Message}");
                throw;
            }
        }
    }
} 