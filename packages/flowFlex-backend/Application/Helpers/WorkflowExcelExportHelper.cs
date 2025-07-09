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
        /// Export workflow to Excel format
        /// </summary>
        /// <param name="workflow">Workflow instance</param>
        /// <returns>Excel content</returns>
        public static Stream ExportToExcel(Workflow workflow)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add($"{workflow.Name}_Workflow");

            var currentRow = 1;

            // Set column widths
            worksheet.Column(1).Width = 25;
            worksheet.Column(2).Width = 60;
            worksheet.Column(3).Width = 80;
            worksheet.Column(4).Width = 20;
            worksheet.Column(5).Width = 20;
            worksheet.Column(6).Width = 20;

            // First row: WORKFLOW EXPORT
            worksheet.Cells[currentRow, 1].Value = "WORKFLOW EXPORT";
            worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
            worksheet.Cells[currentRow, 1].Style.Font.Size = 14;
            currentRow++;

            // Second row: empty line
            currentRow++;

            // Third row: Workflow Information
            worksheet.Cells[currentRow, 1].Value = "Workflow Information";
            worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
            worksheet.Cells[currentRow, 1].Style.Font.Size = 12;
            currentRow++;

            // Basic workflow information - in specified format
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

            // Empty line
            currentRow++;

            // Empty line
            currentRow++;

            // WORKFLOW STAGES
            worksheet.Cells[currentRow, 1].Value = "WORKFLOW STAGES";
            worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
            worksheet.Cells[currentRow, 1].Style.Font.Size = 12;
            currentRow++;

            // Empty line
            currentRow++;

            // Stage headers
            worksheet.Cells[currentRow, 1].Value = "Sequence";
            worksheet.Cells[currentRow, 2].Value = "Stage Name";
            worksheet.Cells[currentRow, 3].Value = "Description";
            worksheet.Cells[currentRow, 4].Value = "Assigned Group";
            worksheet.Cells[currentRow, 5].Value = "Assignee";
            worksheet.Cells[currentRow, 6].Value = "Estimated Duration";

            // Set header styles
            for (int col = 1; col <= 6; col++)
            {
                worksheet.Cells[currentRow, col].Style.Font.Bold = true;
            }
            currentRow++;

            // Stage data
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
        /// Batch export multiple workflows to Excel format
        /// </summary>
        /// <param name="workflows">Workflow list</param>
        /// <returns>Excel content</returns>
        public static Stream ExportMultipleToExcel(List<Workflow> workflows)
        {
            using var package = new ExcelPackage();

            // Track used worksheet names to ensure uniqueness
            HashSet<string> usedSheetNames = new HashSet<string>();

            // Create independent worksheet for each workflow
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

            // Sheet name processing logged by structured logging

            while (usedSheetNames.Contains(uniqueSheetName))
            {
                // Sheet name collision handling logged by structured logging

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
                    // Sheet name generation failure logged by structured logging
                    uniqueSheetName = $"Sheet_{Guid.NewGuid().ToString("N")[..8]}";
                    break;
                }
            }

            usedSheetNames.Add(uniqueSheetName);
            // Final sheet name logged by structured logging

            return uniqueSheetName;
        }
    }
}
