using System;
using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.Dtos.OW.OperationChangeLog
{
    /// <summary>
    /// 操作变更日志输出DTO
    /// </summary>
    public class OperationChangeLogOutputDto
    {
        /// <summary>
        /// 日志ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 操作类型
        /// </summary>
        public string OperationType { get; set; }

        /// <summary>
        /// 操作类型显示名称
        /// </summary>
        public string OperationTypeDisplayName { get; set; }

        /// <summary>
        /// 业务模块
        /// </summary>
        public string BusinessModule { get; set; }

        /// <summary>
        /// 业务主键ID
        /// </summary>
        public long BusinessId { get; set; }

        /// <summary>
        /// 关联的Onboarding ID
        /// </summary>
        public long? OnboardingId { get; set; }

        /// <summary>
        /// 关联的Stage ID
        /// </summary>
        public long? StageId { get; set; }

        /// <summary>
        /// 操作标题
        /// </summary>
        public string OperationTitle { get; set; }

        /// <summary>
        /// 操作描述
        /// </summary>
        public string OperationDescription { get; set; }

        /// <summary>
        /// 变更前的数据
        /// </summary>
        public string BeforeData { get; set; }

        /// <summary>
        /// 变更后的数据
        /// </summary>
        public string AfterData { get; set; }

        /// <summary>
        /// 变更字段列表
        /// </summary>
        public List<string> ChangedFields { get; set; } = new List<string>();

        /// <summary>
        /// 操作人ID
        /// </summary>
        public long OperatorId { get; set; }

        /// <summary>
        /// 操作人姓名
        /// </summary>
        public string OperatorName { get; set; }

        /// <summary>
        /// 操作时间
        /// </summary>
        public DateTimeOffset OperationTime { get; set; }

        /// <summary>
        /// 操作时间显示文本（相对时间）
        /// </summary>
        public string OperationTimeDisplay { get; set; }

        /// <summary>
        /// IP地址
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// 用户代理
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// 操作来源
        /// </summary>
        public string OperationSource { get; set; }

        /// <summary>
        /// 扩展数据
        /// </summary>
        public string ExtendedData { get; set; }

        /// <summary>
        /// 操作状态
        /// </summary>
        public string OperationStatus { get; set; }

        /// <summary>
        /// 操作状态显示名称
        /// </summary>
        public string OperationStatusDisplayName { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 租户ID
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTimeOffset ModifyDate { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        public string CreateBy { get; set; }

        /// <summary>
        /// 修改人
        /// </summary>
        public string ModifyBy { get; set; }

        /// <summary>
        /// 创建用户ID
        /// </summary>
        public long CreateUserId { get; set; }

        /// <summary>
        /// 修改用户ID
        /// </summary>
        public long ModifyUserId { get; set; }
    }

    /// <summary>
    /// 操作变更日志查询请求DTO
    /// </summary>
    public class OperationChangeLogQueryDto
    {
        /// <summary>
        /// Onboarding ID
        /// </summary>
        public long? OnboardingId { get; set; }

        /// <summary>
        /// Stage ID
        /// </summary>
        public long? StageId { get; set; }

        /// <summary>
        /// 操作类型
        /// </summary>
        public string OperationType { get; set; }

        /// <summary>
        /// 业务模块
        /// </summary>
        public string BusinessModule { get; set; }

        /// <summary>
        /// 操作人ID
        /// </summary>
        public long? OperatorId { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTimeOffset? StartDate { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTimeOffset? EndDate { get; set; }

        /// <summary>
        /// 操作状态
        /// </summary>
        public string OperationStatus { get; set; }

        /// <summary>
        /// 页码
        /// </summary>
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// 页大小
        /// </summary>
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// 操作统计信息DTO
    /// </summary>
    public class OperationStatisticsDto
    {
        /// <summary>
        /// 总操作数
        /// </summary>
        public int TotalOperations { get; set; }

        /// <summary>
        /// 各操作类型统计
        /// </summary>
        public Dictionary<string, int> OperationTypeStats { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// 各状态统计
        /// </summary>
        public Dictionary<string, int> StatusStats { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// 今日操作数
        /// </summary>
        public int TodayOperations { get; set; }

        /// <summary>
        /// 本周操作数
        /// </summary>
        public int WeekOperations { get; set; }

        /// <summary>
        /// 本月操作数
        /// </summary>
        public int MonthOperations { get; set; }
    }
}