using SqlSugar;
using FlowFlex.SqlSugarDB.Context;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// 从 ff_questionnaire 表移除 workflow_id 和 stage_id 字段
    /// </summary>
    public class Migration_20250101000005_RemoveQuestionnaireWorkflowStageColumns
    {
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                // 检查字段是否存在
                var columns = db.DbMaintenance.GetColumnInfosByTableName("ff_questionnaire");
                var workflowIdExists = columns.Any(c => c.DbColumnName.Equals("workflow_id", StringComparison.OrdinalIgnoreCase));
                var stageIdExists = columns.Any(c => c.DbColumnName.Equals("stage_id", StringComparison.OrdinalIgnoreCase));

                if (workflowIdExists)
                {
                    db.DbMaintenance.DropColumn("ff_questionnaire", "workflow_id");
                    Console.WriteLine("✅ 已从 ff_questionnaire 表删除 workflow_id 字段");
                }
                else
                {
                    Console.WriteLine("ℹ️ workflow_id 字段不存在，跳过删除");
                }

                if (stageIdExists)
                {
                    db.DbMaintenance.DropColumn("ff_questionnaire", "stage_id");
                    Console.WriteLine("✅ 已从 ff_questionnaire 表删除 stage_id 字段");
                }
                else
                {
                    Console.WriteLine("ℹ️ stage_id 字段不存在，跳过删除");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 删除字段失败: {ex.Message}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                // 检查字段是否存在
                var columns = db.DbMaintenance.GetColumnInfosByTableName("ff_questionnaire");
                var workflowIdExists = columns.Any(c => c.DbColumnName.Equals("workflow_id", StringComparison.OrdinalIgnoreCase));
                var stageIdExists = columns.Any(c => c.DbColumnName.Equals("stage_id", StringComparison.OrdinalIgnoreCase));

                if (!workflowIdExists)
                {
                    db.DbMaintenance.AddColumn("ff_questionnaire", new DbColumnInfo
                    {
                        DbColumnName = "workflow_id",
                        DataType = "BIGINT",
                        IsNullable = true,
                        ColumnDescription = "关联的工作流 ID（向后兼容）"
                    });
                    Console.WriteLine("✅ 已添加 workflow_id 字段到 ff_questionnaire 表");
                }

                if (!stageIdExists)
                {
                    db.DbMaintenance.AddColumn("ff_questionnaire", new DbColumnInfo
                    {
                        DbColumnName = "stage_id",
                        DataType = "BIGINT",
                        IsNullable = true,
                        ColumnDescription = "关联的阶段 ID（向后兼容）"
                    });
                    Console.WriteLine("✅ 已添加 stage_id 字段到 ff_questionnaire 表");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 添加字段失败: {ex.Message}");
                throw;
            }
        }
    }
}