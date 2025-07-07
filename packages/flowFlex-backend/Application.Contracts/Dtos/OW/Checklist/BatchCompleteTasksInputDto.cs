using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Checklist;

/// <summary>
/// Batch complete tasks input DTO
/// </summary>
public class BatchCompleteTasksInputDto
{
    /// <summary>
    /// List of task IDs to complete
    /// </summary>
    
    public List<long> TaskIds { get; set; } = new List<long>();

    /// <summary>
    /// Completion notes
    /// </summary>
    public string CompletionNotes { get; set; } = string.Empty;

    /// <summary>
    /// Actual hours spent
    /// </summary>
    public int ActualHours { get; set; }
}