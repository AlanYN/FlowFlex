using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Stage;

/// <summary>
/// Update Checklist task request
/// </summary>
public class UpdateChecklistTaskRequest
{
    /// <summary>
    /// Whether completed
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Completion notes
    /// </summary>
    public string CompletionNotes { get; set; }
}

/// <summary>
/// Submit questionnaire answer request
/// </summary>
public class SubmitAnswerRequest
{
    /// <summary>
    /// Answer content
    /// </summary>
    public object Answer { get; set; }
}

/// <summary>
/// File upload request
/// </summary>
public class UploadFileRequest
{
    /// <summary>
    /// File name
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// File content
    /// </summary>
    public byte[] FileContent { get; set; }

    /// <summary>
    /// File category
    /// </summary>
    public string FileCategory { get; set; }
}

/// <summary>
/// Complete Stage request
/// </summary>
public class CompleteStageRequest
{
    /// <summary>
    /// Completion notes
    /// </summary>
    public string CompletionNotes { get; set; }
}

/// <summary>
/// Add Stage note request
/// </summary>
public class AddStageNoteRequest
{
    /// <summary>
    /// Note content
    /// </summary>
    public string NoteContent { get; set; }

    /// <summary>
    /// Whether private note
    /// </summary>
    public bool IsPrivate { get; set; }
}

/// <summary>
/// Update Stage note request
/// </summary>
public class UpdateStageNoteRequest
{
    /// <summary>
    /// Note content
    /// </summary>
    public string NoteContent { get; set; }
}

/// <summary>
/// Batch validate consistency request
/// </summary>
public class BatchValidateConsistencyRequest
{
    /// <summary>
    /// Stage IDs to validate
    /// </summary>
    [Required]
    public List<long> StageIds { get; set; }

    /// <summary>
    /// Whether to automatically repair inconsistencies
    /// </summary>
    public bool AutoRepair { get; set; } = true;
}

/// <summary>
/// Sync name change request
/// </summary>
public class SyncNameChangeRequest
{
    /// <summary>
    /// New name to sync
    /// </summary>
    [Required]
    public string NewName { get; set; }
}
