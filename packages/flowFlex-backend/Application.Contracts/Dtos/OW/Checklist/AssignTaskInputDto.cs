using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Checklist;

/// <summary>
/// Assign task input DTO
/// </summary>
public class AssignTaskInputDto
{
    /// <summary>
    /// Assignee user ID
    /// </summary>

    public long AssigneeId { get; set; }

    /// <summary>
    /// Assignee user name
    /// </summary>

    public string AssigneeName { get; set; } = string.Empty;
}