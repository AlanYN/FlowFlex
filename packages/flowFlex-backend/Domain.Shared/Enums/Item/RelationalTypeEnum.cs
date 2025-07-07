using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Item;

public enum RelationalTypeEnum
{
    /// <summary>
    /// Contact
    /// </summary>
    [Description("Contact")]
    Contact = 2,

    /// <summary>
    /// Companies
    /// </summary>
    [Description("Companies")]
    Companies = 3,

    /// <summary>
    /// Deal
    /// </summary>
    [Description("Deal")]
    Deal = 4,

    /// <summary>
    /// Task
    /// </summary>
    [Description("Task")]
    Task = ModuleType.Task,

    /// <summary>
    /// Note
    /// </summary>
    [Description("Note")]
    Note = ModuleType.Note,

    /// <summary>
    /// Log
    /// </summary>
    [Description("OperationLog")]
    OperationLog = ModuleType.OperationLog,
}
