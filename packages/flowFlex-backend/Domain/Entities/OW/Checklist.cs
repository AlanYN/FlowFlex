using System.ComponentModel.DataAnnotations;
using SqlSugar;
using FlowFlex.Domain.Entities.Base;
using System.Text.Json;
using Newtonsoft.Json.Linq;

namespace FlowFlex.Domain.Entities.OW;

/// <summary>
/// Simple Assignment DTO for domain use (DEPRECATED)
/// Assignments are now managed through Stage Components only
/// </summary>
[Obsolete("This DTO is deprecated. Assignments are now managed through Stage Components only.")]
public class AssignmentDto
{
    public long WorkflowId { get; set; }
    public long StageId { get; set; } // 0 表示空的 StageId
}

/// <summary>
/// Checklist Entity - Task List
/// </summary>
[SugarTable("ff_checklist")]
public class Checklist : EntityBaseCreateInfo
{
    /// <summary>
    /// Checklist Name
    /// </summary>

    [StringLength(100)]
    public string Name { get; set; }

    /// <summary>
    /// Checklist Description
    /// </summary>
    [StringLength(500)]
    public string Description { get; set; }

    /// <summary>
    /// Assigned Team (Role)
    /// </summary>
    [StringLength(100)]
    public string Team { get; set; }

    /// <summary>
    /// Checklist Type (Template/Instance)
    /// </summary>
    [StringLength(50)]
    public string Type { get; set; } = "Instance";

    /// <summary>
    /// Checklist Status
    /// </summary>
    [StringLength(50)]
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Is Template Flag
    /// </summary>
    public bool IsTemplate { get; set; } = false;

    /// <summary>
    /// Template ID (if created from template)
    /// </summary>
    public long? TemplateId { get; set; }

    /// <summary>
    /// Estimated Hours to Complete
    /// </summary>
    public int EstimatedHours { get; set; } = 0;

    /// <summary>
    /// Is Active Flag
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Assignments properties removed - now managed through Stage Components only
    // Legacy support: Keep AssignmentDto for backward compatibility in services that still reference it
}
