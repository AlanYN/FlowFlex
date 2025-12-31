namespace FlowFlex.Application.Contracts.Dtos.OW.ChecklistTask;

/// <summary>
/// Toggle task completion request
/// </summary>
public class ToggleTaskCompletionRequest
{
    /// <summary>
    /// Whether the task is completed
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Completion notes
    /// </summary>
    public string CompletionNotes { get; set; } = string.Empty;

    /// <summary>
    /// Related files JSON (JSON string of file information array)
    /// </summary>
    public string FilesJson { get; set; } = "[]";
}

/// <summary>
/// Toggle note pin request
/// </summary>
public class ToggleNotePinRequest
{
    /// <summary>
    /// Whether the note is pinned
    /// </summary>
    public bool IsPinned { get; set; }
}

/// <summary>
/// Batch notes summary request
/// </summary>
public class BatchNotesSummaryRequest
{
    /// <summary>
    /// Task IDs to get notes summary
    /// </summary>
    public List<long> TaskIds { get; set; } = new();
}
