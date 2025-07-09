using SqlSugar;
using FlowFlex.SqlSugarDB.Context;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// 删除 workflow_id 和 stage_id 字段从 ff_checklist 表
    /// </summary>
    public class Migration_20250101000003_RemoveWorkflowStageColumns
    {
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                // 检查字段是否存在
                var columns = db.DbMaintenance.GetColumnInfosByTableName("ff_checklist");
                var workflowIdExists = columns.Any(c => c.DbColumnName.Equals("workflow_id", StringComparison.OrdinalIgnoreCase));
                var stageIdExists = columns.Any(c => c.DbColumnName.Equals("stage_id", StringComparison.OrdinalIgnoreCase));

                if (workflowIdExists)
                {
                    // 删除 workflow_id 字段
                    db.DbMaintenance.DropColumn("ff_checklist", "workflow_id");
                    Console.WriteLine("✅ 已从 ff_checklist 表删除 workflow_id 字段");
                }
                else
                {
                    Console.WriteLine("ℹ️ workflow_id 字段不存在，跳过删除");
                }

                if (stageIdExists)
                {
                    // 删除 stage_id 字段
                    db.DbMaintenance.DropColumn("ff_checklist", "stage_id");
                    Console.WriteLine("✅ 已从 ff_checklist 表删除 stage_id 字段");
                }
                else
                {
                    Console.WriteLine("ℹ️ stage_id 字段不存在，跳过删除");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 删除 workflow_id/stage_id 字段失败: {ex.Message}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                // 检查字段是否存在
                var columns = db.DbMaintenance.GetColumnInfosByTableName("ff_checklist");
                var workflowIdExists = columns.Any(c => c.DbColumnName.Equals("workflow_id", StringComparison.OrdinalIgnoreCase));
                var stageIdExists = columns.Any(c => c.DbColumnName.Equals("stage_id", StringComparison.OrdinalIgnoreCase));

                if (!workflowIdExists)
                {
                    // 重新添加 workflow_id 字段
                    db.DbMaintenance.AddColumn("ff_checklist", new DbColumnInfo
                    {
                        DbColumnName = "workflow_id",
                        DataType = "BIGINT",
                        IsNullable = true,
                        ColumnDescription = "关联工作流ID（可选）"
                    });
                    Console.WriteLine("✅ 已重新添加 workflow_id 字段到 ff_checklist 表");
                }

                if (!stageIdExists)
                {
                    // 重新添加 stage_id 字段
                    db.DbMaintenance.AddColumn("ff_checklist", new DbColumnInfo
                    {
                        DbColumnName = "stage_id",
                        DataType = "BIGINT",
                        IsNullable = true,
                        ColumnDescription = "关联阶段ID（可选）"
                    });
                    Console.WriteLine("✅ 已重新添加 stage_id 字段到 ff_checklist 表");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 重新添加 workflow_id/stage_id 字段失败: {ex.Message}");
                throw;
            }
        }
    }
} 