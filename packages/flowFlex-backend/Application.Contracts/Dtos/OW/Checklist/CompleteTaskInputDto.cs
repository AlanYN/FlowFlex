using System;
using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Checklist;

/// <summary>
/// Complete task input DTO
/// </summary>
public class CompleteTaskInputDto
{
    /// <summary>
    /// 完成备注
    /// </summary>
    [StringLength(500)]
    public string CompletionNotes { get; set; }

    /// <summary>
    /// 实际完成时间（小时）
    /// </summary>
    public int ActualHours { get; set; } = 0;

    /// <summary>
    /// 完成日期
    /// </summary>
    public DateTimeOffset? CompletedDate { get; set; }
}