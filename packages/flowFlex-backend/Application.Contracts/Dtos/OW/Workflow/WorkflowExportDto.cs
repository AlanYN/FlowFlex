using System;
using Item.Excel.Lib;
using Item.Common.Lib.JsonConverts;
using Newtonsoft.Json;

namespace FlowFlex.Application.Contracts.Dtos.OW.Workflow
{
    /// <summary>
    /// Workflow Excel export DTO
    /// </summary>
    public class WorkflowExportDto
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [ExcelColumn(Name = "ID")]
        [JsonConverter(typeof(ValueToStringConverter))]
        public long Id { get; set; }

        /// <summary>
        /// 流程名称
        /// </summary>
        [ExcelColumn(Name = "Workflow Name")]
        public string Name { get; set; }

        /// <summary>
        /// 流程描述
        /// </summary>
        [ExcelColumn(Name = "Description")]
        public string Description { get; set; }

        /// <summary>
        /// 是否为默认流程
        /// </summary>
        [ExcelColumn(Name = "Is Default")]
        public string IsDefault { get; set; }

        /// <summary>
        /// 流程状态
        /// </summary>
        [ExcelColumn(Name = "Status")]
        public string Status { get; set; }

        /// <summary>
        /// 是否激活
        /// </summary>
        [ExcelColumn(Name = "Is Active")]
        public string IsActive { get; set; }

        /// <summary>
        /// 开始日期
        /// </summary>
        [ExcelColumn(Name = "Start Date")]
        public string StartDate { get; set; }

        /// <summary>
        /// 结束日期
        /// </summary>
        [ExcelColumn(Name = "End Date")]
        public string EndDate { get; set; }

        /// <summary>
        /// 版本号
        /// </summary>
        [ExcelColumn(Name = "Version")]
        public int Version { get; set; }

        /// <summary>
        /// 阶段数量
        /// </summary>
        [ExcelColumn(Name = "Stage Count")]
        public int StageCount { get; set; }

        /// <summary>
        /// 阶段名称列表
        /// </summary>
        [ExcelColumn(Name = "Stage Names")]
        public string StageNames { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [ExcelColumn(Name = "Created Date")]
        public string CreateDate { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        [ExcelColumn(Name = "Created By")]
        public string CreateBy { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        [ExcelColumn(Name = "Modified Date")]
        public string ModifyDate { get; set; }

        /// <summary>
        /// 修改人
        /// </summary>
        [ExcelColumn(Name = "Modified By")]
        public string ModifyBy { get; set; }
    }
}