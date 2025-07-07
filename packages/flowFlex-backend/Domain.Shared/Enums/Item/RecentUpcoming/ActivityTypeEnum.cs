namespace FlowFlex.Domain.Shared.Enums.Item.RecentUpcoming;

public enum ActivityTypeEnum
{
    Activity = 1,

    /// <summary>
    /// ��¼Note
    /// </summary>
    Note = ModuleType.Note,

    /// <summary>
    /// ����
    /// </summary>
    Task = ModuleType.Task,

    /// <summary>
    /// ��־
    /// </summary>
    OperationLog = ModuleType.OperationLog,
}
