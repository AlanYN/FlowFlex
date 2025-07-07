using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Application.Helpers
{
    /// <summary>
    /// Workflow Excel export helper
    /// </summary>
    public static class WorkflowExcelExportHelper
    {
        /// <summary>
        /// 导出工作流为Excel格式
        /// </summary>
        /// <param name="workflow">工作流实�?/param>
        /// <returns>Excel内容�?/returns>
        public static Stream ExportToExcel(Workflow workflow)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add($"{workflow.Name}_Workflow");

            var currentRow = 1;

            // 设置列宽
            worksheet.Column(1).Width = 25;
            worksheet.Column(2).Width = 60;
            worksheet.Column(3).Width = 80;
            worksheet.Column(4).Width = 20;
            worksheet.Column(5).Width = 20;
            worksheet.Column(6).Width = 20;

            // �?行：WORKFLOW EXPORT
            worksheet.Cells[currentRow, 1].Value = "WORKFLOW EXPORT";
            worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
            worksheet.Cells[currentRow, 1].Style.Font.Size = 14;
            currentRow++;

            // �?行：空行
            currentRow++;

            // �?行：Workflow Information
            worksheet.Cells[currentRow, 1].Value = "Workflow Information";
            worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
            worksheet.Cells[currentRow, 1].Style.Font.Size = 12;
            currentRow++;

            // 工作流基本信�?- 按照指定格式
            var workflowInfoRows = new[]
            {
                new { Label = "Name", Value = workflow.Name },
                new { Label = "Description", Value = workflow.Description ?? "Complete workflow process" },
                new { Label = "Start Date", Value = workflow.StartDate.ToString("yyyy/M/d") },
                new { Label = "End Date", Value = workflow.EndDate?.ToString("yyyy/M/d") ?? "Not set" },
                new { Label = "Status", Value = workflow.Status ?? "Active" },
                new { Label = "Default Workflow", Value = workflow.IsDefault ? "Yes" : "No" },
                new { Label = "Created By", Value = workflow.CreateBy ?? "Admin" },
                new { Label = "Created Date", Value = workflow.CreateDate.ToString("yyyy/M/d") },
                new { Label = "Total Stages", Value = (workflow.Stages?.Count ?? 0).ToString() },
                new { Label = "Total Estimated Duration", Value = CalculateTotalEstimatedDuration(workflow.Stages) }
            };

            foreach (var info in workflowInfoRows)
            {
                worksheet.Cells[currentRow, 1].Value = info.Label;
                worksheet.Cells[currentRow, 2].Value = info.Value;
                worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
                currentRow++;
            }

            // 空行
            currentRow++;

            // 空行
            currentRow++;

            // WORKFLOW STAGES
            worksheet.Cells[currentRow, 1].Value = "WORKFLOW STAGES";
            worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
            worksheet.Cells[currentRow, 1].Style.Font.Size = 12;
            currentRow++;

            // 空行
            currentRow++;

            // 阶段表头
            worksheet.Cells[currentRow, 1].Value = "Sequence";
            worksheet.Cells[currentRow, 2].Value = "Stage Name";
            worksheet.Cells[currentRow, 3].Value = "Description";
            worksheet.Cells[currentRow, 4].Value = "Assigned Group";
            worksheet.Cells[currentRow, 5].Value = "Assignee";
            worksheet.Cells[currentRow, 6].Value = "Estimated Duration";

            // 设置表头样式
            for (int col = 1; col <= 6; col++)
            {
                worksheet.Cells[currentRow, col].Style.Font.Bold = true;
            }
            currentRow++;

            // 阶段数据
            if (workflow.Stages != null && workflow.Stages.Any())
            {
                var sortedStages = workflow.Stages.OrderBy(s => s.Order).ToList();

                foreach (var stage in sortedStages)
                {
                    worksheet.Cells[currentRow, 1].Value = stage.Order;
                    worksheet.Cells[currentRow, 2].Value = stage.Name;
                    worksheet.Cells[currentRow, 3].Value = stage.Description ?? "";
                    worksheet.Cells[currentRow, 4].Value = stage.DefaultAssignedGroup ?? "";
                    worksheet.Cells[currentRow, 5].Value = stage.DefaultAssignee ?? "";
                    worksheet.Cells[currentRow, 6].Value = FormatEstimatedDuration(stage.EstimatedDuration);

                    currentRow++;
                }
            }

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            return stream;
        }

        /// <summary>
        /// 批量导出多个工作流为Excel格式
        /// </summary>
        /// <param name="workflows">工作流列�?/param>
        /// <returns>Excel内容�?/returns>
        public static Stream ExportMultipleToExcel(List<Workflow> workflows)
        {
            using var package = new ExcelPackage();

            // 用于跟踪已使用的工作表名称，确保唯一�?
            HashSet<string> usedSheetNames = new HashSet<string>();

            // 为每个工作流创建独立的工作表
            foreach (var workflow in workflows.Take(10)) // 限制最�?0个工作表
            {
                var baseSheetName = SanitizeSheetName(workflow.Name);
                var uniqueSheetName = EnsureUniqueSheetName(baseSheetName, usedSheetNames);

                var worksheet = package.Workbook.Worksheets.Add(uniqueSheetName);
                CreateDetailedWorkflowSheet(worksheet, workflow);
            }

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            return stream;
        }

        /// <summary>
        /// 创建详细工作流工作表
        /// </summary>
        private static void CreateDetailedWorkflowSheet(ExcelWorksheet worksheet, Workflow workflow)
        {
            var currentRow = 1;

            // 设置列宽
            worksheet.Column(1).Width = 25;
            worksheet.Column(2).Width = 60;
            worksheet.Column(3).Width = 80;
            worksheet.Column(4).Width = 20;
            worksheet.Column(5).Width = 20;
            worksheet.Column(6).Width = 20;

            // WORKFLOW EXPORT
            worksheet.Cells[currentRow, 1].Value = "WORKFLOW EXPORT";
            worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
            worksheet.Cells[currentRow, 1].Style.Font.Size = 14;
            currentRow++;

            // 空行
            currentRow++;

            // Workflow Information
            worksheet.Cells[currentRow, 1].Value = "Workflow Information";
            worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
            worksheet.Cells[currentRow, 1].Style.Font.Size = 12;
            currentRow++;

            // 工作流基本信�?
            var workflowInfoRows = new[]
            {
                new { Label = "Name", Value = workflow.Name },
                new { Label = "Description", Value = workflow.Description ?? "Complete workflow process" },
                new { Label = "Start Date", Value = workflow.StartDate.ToString("yyyy/M/d") },
                new { Label = "End Date", Value = workflow.EndDate?.ToString("yyyy/M/d") ?? "Not set" },
                new { Label = "Status", Value = workflow.Status ?? "Active" },
                new { Label = "Default Workflow", Value = workflow.IsDefault ? "Yes" : "No" },
                new { Label = "Created By", Value = workflow.CreateBy ?? "Admin" },
                new { Label = "Created Date", Value = workflow.CreateDate.ToString("yyyy/M/d") },
                new { Label = "Total Stages", Value = (workflow.Stages?.Count ?? 0).ToString() },
                new { Label = "Total Estimated Duration", Value = CalculateTotalEstimatedDuration(workflow.Stages) }
            };

            foreach (var info in workflowInfoRows)
            {
                worksheet.Cells[currentRow, 1].Value = info.Label;
                worksheet.Cells[currentRow, 2].Value = info.Value;
                worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
                currentRow++;
            }

            // 空行
            currentRow++;

            // 空行
            currentRow++;

            // WORKFLOW STAGES
            worksheet.Cells[currentRow, 1].Value = "WORKFLOW STAGES";
            worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
            worksheet.Cells[currentRow, 1].Style.Font.Size = 12;
            currentRow++;

            // 空行
            currentRow++;

            // 阶段表头
            worksheet.Cells[currentRow, 1].Value = "Sequence";
            worksheet.Cells[currentRow, 2].Value = "Stage Name";
            worksheet.Cells[currentRow, 3].Value = "Description";
            worksheet.Cells[currentRow, 4].Value = "Assigned Group";
            worksheet.Cells[currentRow, 5].Value = "Assignee";
            worksheet.Cells[currentRow, 6].Value = "Estimated Duration";

            // 设置表头样式
            for (int col = 1; col <= 6; col++)
            {
                worksheet.Cells[currentRow, col].Style.Font.Bold = true;
            }
            currentRow++;

            // 阶段数据
            if (workflow.Stages != null && workflow.Stages.Any())
            {
                var sortedStages = workflow.Stages.OrderBy(s => s.Order).ToList();

                foreach (var stage in sortedStages)
                {
                    worksheet.Cells[currentRow, 1].Value = stage.Order;
                    worksheet.Cells[currentRow, 2].Value = stage.Name;
                    worksheet.Cells[currentRow, 3].Value = stage.Description ?? "";
                    worksheet.Cells[currentRow, 4].Value = stage.DefaultAssignedGroup ?? "";
                    worksheet.Cells[currentRow, 5].Value = stage.DefaultAssignee ?? "";
                    worksheet.Cells[currentRow, 6].Value = FormatEstimatedDuration(stage.EstimatedDuration);

                    currentRow++;
                }
            }
        }

        /// <summary>
        /// 格式化预估时�?
        /// </summary>
        private static string FormatEstimatedDuration(decimal? estimatedDuration)
        {
            if (!estimatedDuration.HasValue || estimatedDuration <= 0)
                return "Not set";

            if (estimatedDuration == 1)
                return "1 day";
            else
                return $"{estimatedDuration} days";
        }

        /// <summary>
        /// 计算总预估时�?
        /// </summary>
        private static string CalculateTotalEstimatedDuration(List<Stage> stages)
        {
            if (stages == null || !stages.Any())
                return "0 days";

            var totalDays = stages.Sum(s => s.EstimatedDuration ?? 0);

            if (totalDays == 1)
                return "1 day";
            else
                return $"{totalDays} days";
        }

        /// <summary>
        /// 清理工作表名�?
        /// </summary>
        private static string SanitizeSheetName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Workflow";

            // Excel工作表名称限�?
            var invalidChars = new[] { '\\', '/', '*', '?', ':', '[', ']' };
            var sanitized = name;

            foreach (var invalidChar in invalidChars)
            {
                sanitized = sanitized.Replace(invalidChar, '_');
            }

            // 限制长度�?1个字�?
            if (sanitized.Length > 31)
            {
                sanitized = sanitized.Substring(0, 31);
            }

            return sanitized;
        }

        /// <summary>
        /// 确保工作表名称唯一
        /// </summary>
        private static string EnsureUniqueSheetName(string baseSheetName, HashSet<string> usedSheetNames)
        {
            var uniqueSheetName = baseSheetName;
            var suffix = 1;

            // 添加调试日志
            Console.WriteLine($"[DEBUG] Processing base sheet name: {baseSheetName}");

            while (usedSheetNames.Contains(uniqueSheetName))
            {
                Console.WriteLine($"[DEBUG] Sheet name '{uniqueSheetName}' already exists, trying suffix {suffix}");

                // 确保添加后缀后不超过31个字符的限制
                var suffixStr = $"_{suffix}";
                var maxBaseLength = 31 - suffixStr.Length;

                var truncatedBase = baseSheetName.Length > maxBaseLength
                    ? baseSheetName.Substring(0, maxBaseLength)
                    : baseSheetName;

                uniqueSheetName = $"{truncatedBase}{suffixStr}";
                suffix++;

                // 防止无限循环
                if (suffix > 100)
                {
                    Console.WriteLine($"[ERROR] Unable to generate unique sheet name after 100 attempts for base: {baseSheetName}");
                    uniqueSheetName = $"Sheet_{Guid.NewGuid().ToString("N")[..8]}";
                    break;
                }
            }

            usedSheetNames.Add(uniqueSheetName);
            Console.WriteLine($"[DEBUG] Final unique sheet name: {uniqueSheetName}");

            return uniqueSheetName;
        }
    }
}
