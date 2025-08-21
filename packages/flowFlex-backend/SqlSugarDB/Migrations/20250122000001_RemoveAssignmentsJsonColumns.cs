using SqlSugar;
using FlowFlex.SqlSugarDB.Context;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// 删除 assignments_json 字段从 ff_checklist 和 ff_questionnaire 表
    /// Assignments 现在完全通过 Stage Components 管理
    /// </summary>
    public class Migration_20250122000001_RemoveAssignmentsJsonColumns
    {
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("🚀 开始删除 assignments_json 字段...");

                // 删除 ff_checklist 表的 assignments_json 字段
                var checklistColumns = db.DbMaintenance.GetColumnInfosByTableName("ff_checklist");
                var checklistAssignmentsJsonExists = checklistColumns.Any(c => c.DbColumnName.Equals("assignments_json", StringComparison.OrdinalIgnoreCase));

                if (checklistAssignmentsJsonExists)
                {
                    db.DbMaintenance.DropColumn("ff_checklist", "assignments_json");
                    Console.WriteLine("✅ 已从 ff_checklist 表删除 assignments_json 字段");
                }
                else
                {
                    Console.WriteLine("ℹ️ ff_checklist.assignments_json 字段不存在，跳过删除");
                }

                // 删除 ff_questionnaire 表的 assignments_json 字段
                var questionnaireColumns = db.DbMaintenance.GetColumnInfosByTableName("ff_questionnaire");
                var questionnaireAssignmentsJsonExists = questionnaireColumns.Any(c => c.DbColumnName.Equals("assignments_json", StringComparison.OrdinalIgnoreCase));

                if (questionnaireAssignmentsJsonExists)
                {
                    db.DbMaintenance.DropColumn("ff_questionnaire", "assignments_json");
                    Console.WriteLine("✅ 已从 ff_questionnaire 表删除 assignments_json 字段");
                }
                else
                {
                    Console.WriteLine("ℹ️ ff_questionnaire.assignments_json 字段不存在，跳过删除");
                }

                Console.WriteLine("🎉 删除 assignments_json 字段完成！Assignments 现在完全通过 Stage Components 管理。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 删除 assignments_json 字段失败: {ex.Message}");
                Console.WriteLine($"详细错误: {ex.StackTrace}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("🔄 开始回滚：重新添加 assignments_json 字段...");

                // 重新添加 ff_checklist 表的 assignments_json 字段
                var checklistColumns = db.DbMaintenance.GetColumnInfosByTableName("ff_checklist");
                var checklistAssignmentsJsonExists = checklistColumns.Any(c => c.DbColumnName.Equals("assignments_json", StringComparison.OrdinalIgnoreCase));

                if (!checklistAssignmentsJsonExists)
                {
                    db.DbMaintenance.AddColumn("ff_checklist", new DbColumnInfo
                    {
                        DbColumnName = "assignments_json",
                        DataType = "TEXT",
                        IsNullable = true,
                        ColumnDescription = "存储多个 workflow-stage 组合的 JSON 数据（已弃用，现通过 Stage Components 管理）"
                    });
                    Console.WriteLine("✅ 已重新添加 assignments_json 字段到 ff_checklist 表");
                }
                else
                {
                    Console.WriteLine("ℹ️ ff_checklist.assignments_json 字段已存在，跳过添加");
                }

                // 重新添加 ff_questionnaire 表的 assignments_json 字段
                var questionnaireColumns = db.DbMaintenance.GetColumnInfosByTableName("ff_questionnaire");
                var questionnaireAssignmentsJsonExists = questionnaireColumns.Any(c => c.DbColumnName.Equals("assignments_json", StringComparison.OrdinalIgnoreCase));

                if (!questionnaireAssignmentsJsonExists)
                {
                    db.DbMaintenance.AddColumn("ff_questionnaire", new DbColumnInfo
                    {
                        DbColumnName = "assignments_json",
                        DataType = "TEXT",
                        IsNullable = true,
                        ColumnDescription = "存储多个 workflow-stage 组合的 JSON 数据（已弃用，现通过 Stage Components 管理）"
                    });
                    Console.WriteLine("✅ 已重新添加 assignments_json 字段到 ff_questionnaire 表");
                }
                else
                {
                    Console.WriteLine("ℹ️ ff_questionnaire.assignments_json 字段已存在，跳过添加");
                }

                Console.WriteLine("🔄 回滚完成：assignments_json 字段已恢复");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 回滚失败: {ex.Message}");
                Console.WriteLine($"详细错误: {ex.StackTrace}");
                throw;
            }
        }
    }
}