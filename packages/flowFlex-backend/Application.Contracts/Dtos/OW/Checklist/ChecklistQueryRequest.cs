using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Checklist;

/// <summary>
/// Checklist query request DTO
/// </summary>
public class ChecklistQueryRequest
{
    /// <summary>
    /// 页码（从1开始）
    /// </summary>
    public int PageIndex { get; set; } = 1;

    /// <summary>
    /// 每页大小
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// 名称过滤
    /// </summary>
    [StringLength(100)]
    public string Name { get; set; }

    /// <summary>
    /// 团队过滤
    /// </summary>
    [StringLength(100)]
    public string Team { get; set; }

    /// <summary>
    /// 类型过滤
    /// </summary>
    [StringLength(20)]
    public string Type { get; set; }

    /// <summary>
    /// 状态过滤
    /// </summary>
    [StringLength(20)]
    public string Status { get; set; }

    /// <summary>
    /// 是否模板过滤
    /// </summary>
    public bool? IsTemplate { get; set; }

    /// <summary>
    /// 是否激活过滤
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// 工作流ID过滤
    /// </summary>
    public long? WorkflowId { get; set; }

    /// <summary>
    /// 阶段ID过滤
    /// </summary>
    public long? StageId { get; set; }

    /// <summary>
    /// 排序字段
    /// </summary>
    [StringLength(50)]
    public string SortField { get; set; } = "CreateDate";

    /// <summary>
    /// 排序方向（asc/desc）
    /// </summary>
    [StringLength(10)]
    public string SortDirection { get; set; } = "desc";
}