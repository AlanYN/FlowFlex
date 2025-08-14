using System.ComponentModel.DataAnnotations;
using SqlSugar;
using FlowFlex.Domain.Entities.Base;
using System.Text.Json;
using Newtonsoft.Json.Linq;

namespace FlowFlex.Domain.Entities.OW;

/// <summary>
/// Simple Assignment DTO for domain use
/// </summary>
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

    /// <summary>
    /// Assignments stored as JSONB (raw JToken to handle both array and object)
    /// </summary>
    [SugarColumn(ColumnName = "assignments_json", ColumnDataType = "jsonb", IsJson = true, IsNullable = true)]
    public JToken AssignmentsRaw { get; set; }

    /// <summary>
    /// Assignments property (not directly mapped to DB)
    /// - Supports both array and single object legacy formats
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public List<AssignmentDto> Assignments
    {
        get
        {
            try
            {
                if (AssignmentsRaw == null || AssignmentsRaw.Type == JTokenType.Null)
                {
                    return new List<AssignmentDto>();
                }
                if (AssignmentsRaw.Type == JTokenType.Array)
                {
                    return AssignmentsRaw.ToObject<List<AssignmentDto>>() ?? new List<AssignmentDto>();
                }
                if (AssignmentsRaw.Type == JTokenType.Object)
                {
                    var single = AssignmentsRaw.ToObject<AssignmentDto>();
                    return single != null ? new List<AssignmentDto> { single } : new List<AssignmentDto>();
                }
                if (AssignmentsRaw.Type == JTokenType.String)
                {
                    // Handle double-encoded string
                    var text = AssignmentsRaw.ToObject<string>();
                    if (string.IsNullOrWhiteSpace(text)) return new List<AssignmentDto>();
                    // Try parse as array first, then object
                    try { return JArray.Parse(text).ToObject<List<AssignmentDto>>() ?? new List<AssignmentDto>(); } catch { }
                    try { var obj = JObject.Parse(text).ToObject<AssignmentDto>(); return obj != null ? new List<AssignmentDto> { obj } : new List<AssignmentDto>(); } catch { }
                }
            }
            catch { }
            return new List<AssignmentDto>();
        }
        set
        {
            if (value == null || value.Count == 0)
            {
                AssignmentsRaw = null;
                return;
            }
            AssignmentsRaw = JArray.FromObject(value);
        }
    }
}
