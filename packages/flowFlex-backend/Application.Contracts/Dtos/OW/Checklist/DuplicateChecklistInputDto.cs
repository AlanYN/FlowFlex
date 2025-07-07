using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Checklist;

/// <summary>
/// Duplicate checklist input DTO
/// </summary>
public class DuplicateChecklistInputDto
{
    /// <summary>
    /// 新清单名称
    /// </summary>
    
    [StringLength(100)]
    public string Name { get; set; }

    /// <summary>
    /// 新清单描述
    /// </summary>
    [StringLength(500)]
    public string Description { get; set; }

    /// <summary>
    /// 目标团队
    /// </summary>
    [StringLength(100)]
    public string TargetTeam { get; set; }

    /// <summary>
    /// 是否设置为模板
    /// </summary>
    public bool SetAsTemplate { get; set; } = false;

    /// <summary>
    /// 是否复制任务
    /// </summary>
    public bool CopyTasks { get; set; } = true;
}