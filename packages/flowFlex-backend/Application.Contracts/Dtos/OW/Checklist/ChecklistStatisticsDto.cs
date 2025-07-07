namespace FlowFlex.Application.Contracts.Dtos.OW.Checklist;

/// <summary>
/// Checklist statistics DTO
/// </summary>
public class ChecklistStatisticsDto
{
    /// <summary>
    /// 团队名称
    /// </summary>
    public string Team { get; set; }

    /// <summary>
    /// 总清单数
    /// </summary>
    public int TotalChecklists { get; set; }

    /// <summary>
    /// 激活清单数
    /// </summary>
    public int ActiveChecklists { get; set; }

    /// <summary>
    /// 模板数量
    /// </summary>
    public int TemplateCount { get; set; }

    /// <summary>
    /// 实例数量
    /// </summary>
    public int InstanceCount { get; set; }

    /// <summary>
    /// 总任务数
    /// </summary>
    public int TotalTasks { get; set; }

    /// <summary>
    /// 已完成任务数
    /// </summary>
    public int CompletedTasks { get; set; }

    /// <summary>
    /// 平均完成率
    /// </summary>
    public decimal AverageCompletionRate { get; set; }

    /// <summary>
    /// 逾期任务数
    /// </summary>
    public int OverdueTasks { get; set; }

    /// <summary>
    /// 总预计时间（小时）
    /// </summary>
    public int TotalEstimatedHours { get; set; }

    /// <summary>
    /// 总实际时间（小时）
    /// </summary>
    public int TotalActualHours { get; set; }
}