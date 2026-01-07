namespace FlowFlex.Domain.Shared.Enums;

/// <summary>
/// 消息类型枚举
/// </summary>
public enum MessageType
{
    /// <summary>
    /// 内部消息
    /// </summary>
    Internal = 0,

    /// <summary>
    /// 客户邮件（通过 Outlook 集成）
    /// </summary>
    Email = 1,

    /// <summary>
    /// Portal 消息
    /// </summary>
    Portal = 2
}

/// <summary>
/// Message folder enum
/// </summary>
public enum MessageFolder
{
    /// <summary>
    /// Inbox
    /// </summary>
    Inbox = 0,

    /// <summary>
    /// Sent
    /// </summary>
    Sent = 1,

    /// <summary>
    /// Archive (virtual folder based on IsArchived flag)
    /// </summary>
    Archive = 2,

    /// <summary>
    /// Trash
    /// </summary>
    Trash = 3,

    /// <summary>
    /// Drafts
    /// </summary>
    Drafts = 4
}

/// <summary>
/// 消息标签枚举
/// </summary>
public enum MessageLabel
{
    /// <summary>
    /// 内部标签
    /// </summary>
    Internal = 0,

    /// <summary>
    /// 外部标签
    /// </summary>
    External = 1,

    /// <summary>
    /// 重要标签
    /// </summary>
    Important = 2,

    /// <summary>
    /// Portal 标签
    /// </summary>
    Portal = 3
}
