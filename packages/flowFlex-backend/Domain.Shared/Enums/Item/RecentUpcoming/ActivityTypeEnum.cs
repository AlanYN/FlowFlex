namespace FlowFlex.Domain.Shared.Enums.Item.RecentUpcoming;

public enum ActivityTypeEnum
{
    Activity = 1,

    /// <summary>
    /// 记录Note
    /// </summary>
    Note = ModuleType.Note,

    /// <summary>
    /// 任务
    /// </summary>
    Task = ModuleType.Task,

    /// <summary>
    /// 日志
    /// </summary>
    OperationLog = ModuleType.OperationLog,
}
