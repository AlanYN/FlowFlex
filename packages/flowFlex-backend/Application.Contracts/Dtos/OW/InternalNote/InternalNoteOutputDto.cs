using System;

namespace FlowFlex.Application.Contracts.Dtos.OW.InternalNote;

/// <summary>
/// Internal note output DTO
/// </summary>
public class InternalNoteOutputDto
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Onboarding ID
    /// </summary>
    public long OnboardingId { get; set; }

    /// <summary>
    /// Stage ID
    /// </summary>
    public long? StageId { get; set; }

    /// <summary>
    /// Stage名称
    /// </summary>
    public string StageName { get; set; }

    /// <summary>
    /// 备注标题 (可选)
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 备注内容
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// 备注类型
    /// </summary>
    public string NoteType { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public string Priority { get; set; }

    /// <summary>
    /// 是否已解决
    /// </summary>
    public bool IsResolved { get; set; }

    /// <summary>
    /// 解决时间
    /// </summary>
    public DateTimeOffset? ResolvedTime { get; set; }

    /// <summary>
    /// 解决人
    /// </summary>
    public string ResolvedBy { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>
    /// 创建人
    /// </summary>
    public string CreateBy { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>
    /// 修改人
    /// </summary>
    public string ModifyBy { get; set; }
}