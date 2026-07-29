using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Application.Contracts.Dtos.OW.Checklist;
using FlowFlex.Application.Contracts.Dtos.OW.ChecklistTask;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts;
using Item.Internal.StandardApi.Response;
using System.Net;
using FlowFlex.Domain.Shared.Attr;
using FlowFlex.Application.Filter;
using FlowFlex.Domain.Shared.Const;
using WebApi.Authorization;

namespace FlowFlex.WebApi.Controllers.OW;

/// <summary>
/// Checklist Task Completion Controller - Manages the completion status of checklist tasks within onboarding cases.
/// Tracks which tasks are completed per onboarding, supports file attachments for evidence, and provides completion statistics.
/// </summary>
[ApiController]
[PortalAccess] // Allow Portal token access - Portal users can complete checklist tasks
[Route("ow/checklist-task-completions/v{version:apiVersion}")]
[Display(Name = "checklist-task-completion")]
[Authorize] // Require authentication for all checklist task completion APIs
public class ChecklistTaskCompletionController : Controllers.ControllerBase
{
    private readonly IChecklistTaskCompletionService _completionService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IChecklistTaskNoteService _noteService;

    public ChecklistTaskCompletionController(IChecklistTaskCompletionService completionService, IFileStorageService fileStorageService, IChecklistTaskNoteService noteService)
    {
        _completionService = completionService;
        _fileStorageService = fileStorageService;
        _noteService = noteService;
    }

    /// <summary>
    /// Get all task completions or filter by onboardingId and stageId
    /// Requires any READ permission (WORKFLOW, CASE, CHECKLIST, QUESTION, or TOOL)
    /// This is a shared query API accessible by any module with read permission
    /// </summary>
    [HttpGet]
    [WFEAuthorize(
        PermissionConsts.Workflow.Read,
        PermissionConsts.Case.Read,
        PermissionConsts.Checklist.Read,
        PermissionConsts.Question.Read,
        PermissionConsts.Tool.Read)]
    [ProducesResponseType<SuccessResponse<List<ChecklistTaskCompletionOutputDto>>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetTaskCompletions([FromQuery] long? onboardingId = null, [FromQuery] long? stageId = null)
    {
        // If both onboardingId and stageId are provided, filter by both
        if (onboardingId.HasValue && stageId.HasValue)
        {
            var result = await _completionService.GetByOnboardingAndStageAsync(onboardingId.Value, stageId.Value);
            return Success(result);
        }

        // Otherwise, return all task completions
        var allResults = await _completionService.GetAllTaskCompletionsAsync();
        return Success(allResults);
    }

    /// <summary>
    /// Save or update a single task completion record (upsert by onboardingId + taskId)
    /// </summary>
    /// <param name="input">Task completion data including onboardingId, taskId, isCompleted, completionNotes</param>
    /// <returns>Whether save was successful</returns>
    [HttpPost]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> SaveTaskCompletion([FromBody] ChecklistTaskCompletionInputDto input)
    {
        var result = await _completionService.SaveTaskCompletionAsync(input);
        return Success(result);
    }

    /// <summary>
    /// Batch save multiple task completion records at once
    /// </summary>
    /// <param name="inputs">List of task completion records to save</param>
    /// <returns>Whether all saves were successful</returns>
    [HttpPost("batch")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> BatchSaveTaskCompletions([FromBody] List<ChecklistTaskCompletionInputDto> inputs)
    {
        var result = await _completionService.BatchSaveTaskCompletionsAsync(inputs);
        return Success(result);
    }

    /// <summary>
    /// Get task completion records filtered by lead ID and checklist ID
    /// </summary>
    /// <param name="leadId">Lead identifier (external system reference)</param>
    /// <param name="checklistId">Checklist ID</param>
    /// <returns>List of task completion records</returns>
    [HttpGet("lead/{leadId}/checklist/{checklistId}")]
    [ProducesResponseType<SuccessResponse<List<ChecklistTaskCompletionOutputDto>>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetByLeadAndChecklist(string leadId, long checklistId)
    {
        var result = await _completionService.GetByLeadAndChecklistAsync(leadId, checklistId);
        return Success(result);
    }

    /// <summary>
    /// Get task completion records by onboarding ID and checklist ID
    /// </summary>
    /// <param name="onboardingId">Onboarding case ID</param>
    /// <param name="checklistId">Checklist ID</param>
    /// <returns>List of task completion records for the specified checklist within the onboarding</returns>
    [HttpGet("onboarding/{onboardingId}/checklist/{checklistId}")]
    [ProducesResponseType<SuccessResponse<List<ChecklistTaskCompletionOutputDto>>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetByOnboardingAndChecklist(long onboardingId, long checklistId)
    {
        var result = await _completionService.GetByOnboardingAndChecklistAsync(onboardingId, checklistId);
        return Success(result);
    }

    /// <summary>
    /// Get completion statistics for a checklist within an onboarding (total tasks, completed tasks, completion rate)
    /// </summary>
    /// <param name="onboardingId">Onboarding case ID</param>
    /// <param name="checklistId">Checklist ID</param>
    /// <returns>Object with totalTasks, completedTasks, and completionRate (0-100%)</returns>
    [HttpGet("onboarding/{onboardingId}/checklist/{checklistId}/stats")]
    [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetCompletionStats(long onboardingId, long checklistId)
    {
        var (totalTasks, completedTasks, completionRate) = await _completionService.GetCompletionStatsAsync(onboardingId, checklistId);

        var result = new
        {
            TotalTasks = totalTasks,
            CompletedTasks = completedTasks,
            CompletionRate = completionRate
        };

        return Success(result);
    }

    /// <summary>
    /// Toggle a task's completion status (complete/uncomplete) with optional notes and file attachments
    /// </summary>
    /// <param name="onboardingId">Onboarding case ID</param>
    /// <param name="taskId">Checklist task ID</param>
    /// <param name="request">Toggle request with isCompleted flag, optional completionNotes and filesJson</param>
    /// <returns>Updated task completion record</returns>
    [HttpPost("onboarding/{onboardingId}/task/{taskId}/toggle")]
    [ProducesResponseType<SuccessResponse<ChecklistTaskCompletionOutputDto>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> ToggleTaskCompletion(long onboardingId, long taskId, [FromBody] ToggleTaskCompletionRequest request)
    {
        var result = await _completionService.ToggleTaskCompletionAsync(onboardingId, taskId, request.IsCompleted, request.CompletionNotes, request.FilesJson);
        return Success(result);
    }

    /// <summary>
    /// Upload task completion file
    /// </summary>
    /// <param name="formFile">File to upload</param>
    /// <param name="category">File category (optional, default: "ChecklistTaskCompletion")</param>
    /// <param name="taskId">Task ID (optional)</param>
    /// <param name="onboardingId">Onboarding ID (optional)</param>
    /// <returns>Complete file upload information</returns>
    [HttpPost("upload-file")]
    [ProducesResponseType<SuccessResponse<ChecklistTaskFileUploadResponseDto>>((int)HttpStatusCode.OK)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadTaskFileAsync(
        IFormFile formFile,
        [FromForm] string category = "ChecklistTaskCompletion",
        [FromForm] long? taskId = null,
        [FromForm] long? onboardingId = null)
    {
        // Validate file first
        var validationResult = await _fileStorageService.ValidateFileAsync(formFile);
        if (!validationResult.IsValid)
        {
            return BadRequest($"File validation failed: {validationResult.ErrorMessage}");
        }

        // Save file using file storage service
        var storageResult = await _fileStorageService.SaveFileAsync(formFile, category);

        if (!storageResult.Success)
        {
            return BadRequest($"File upload failed: {storageResult.ErrorMessage}");
        }

        // Get current gateway/host information
        var request = HttpContext.Request;
        var gateway = $"{request.Scheme}://{request.Host}";
        var fullAccessUrl = storageResult.AccessUrl?.StartsWith("http") == true
            ? storageResult.AccessUrl
            : $"{gateway}{storageResult.AccessUrl}";

        // Create comprehensive response
        var response = new ChecklistTaskFileUploadResponseDto
        {
            Success = storageResult.Success,
            AccessUrl = storageResult.AccessUrl,
            OriginalFileName = storageResult.OriginalFileName,
            FileName = storageResult.FileName,
            FilePath = storageResult.FilePath,
            FileSize = storageResult.FileSize,
            ContentType = storageResult.ContentType,
            Category = category,
            FileHash = storageResult.FileHash,
            UploadTime = DateTime.UtcNow,
            ErrorMessage = storageResult.ErrorMessage,
            Gateway = gateway,
            FullAccessUrl = fullAccessUrl,
            TaskId = taskId,
            OnboardingId = onboardingId
        };

        return Success(response);
    }

    /// <summary>
    /// Batch upload task completion files
    /// </summary>
    /// <param name="formFiles">List of files to upload</param>
    /// <param name="category">File category (optional, default: "ChecklistTaskCompletion")</param>
    /// <param name="taskId">Task ID (optional)</param>
    /// <param name="onboardingId">Onboarding ID (optional)</param>
    /// <returns>List of complete file upload information</returns>
    [HttpPost("batch-upload-files")]
    [ProducesResponseType<SuccessResponse<List<ChecklistTaskFileUploadResponseDto>>>((int)HttpStatusCode.OK)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadMultipleTaskFilesAsync(
        List<IFormFile> formFiles,
        [FromForm] string category = "ChecklistTaskCompletion",
        [FromForm] long? taskId = null,
        [FromForm] long? onboardingId = null)
    {
        var uploadResults = new List<ChecklistTaskFileUploadResponseDto>();
        var errors = new List<string>();

        // Get current gateway/host information
        var request = HttpContext.Request;
        var gateway = $"{request.Scheme}://{request.Host}";

        foreach (var formFile in formFiles)
        {
            var response = new ChecklistTaskFileUploadResponseDto
            {
                OriginalFileName = formFile?.FileName ?? "unknown",
                Category = category,
                UploadTime = DateTime.UtcNow,
                Gateway = gateway,
                TaskId = taskId,
                OnboardingId = onboardingId
            };

            if (formFile == null || formFile.Length == 0)
            {
                response.Success = false;
                response.ErrorMessage = "File is empty";
                uploadResults.Add(response);
                errors.Add($"File {formFile?.FileName ?? "unknown"} is empty");
                continue;
            }

            // Validate file
            var validationResult = await _fileStorageService.ValidateFileAsync(formFile);
            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.ErrorMessage = $"Validation failed: {validationResult.ErrorMessage}";
                uploadResults.Add(response);
                errors.Add($"File {formFile.FileName} validation failed: {validationResult.ErrorMessage}");
                continue;
            }

            // Save file
            var storageResult = await _fileStorageService.SaveFileAsync(formFile, category);
            if (!storageResult.Success)
            {
                response.Success = false;
                response.ErrorMessage = $"Upload failed: {storageResult.ErrorMessage}";
                uploadResults.Add(response);
                errors.Add($"File {formFile.FileName} upload failed: {storageResult.ErrorMessage}");
                continue;
            }

            // Success case
            var fullAccessUrl = storageResult.AccessUrl?.StartsWith("http") == true
                ? storageResult.AccessUrl
                : $"{gateway}{storageResult.AccessUrl}";

            response.Success = storageResult.Success;
            response.AccessUrl = storageResult.AccessUrl;
            response.FileName = storageResult.FileName;
            response.FilePath = storageResult.FilePath;
            response.FileSize = storageResult.FileSize;
            response.ContentType = storageResult.ContentType;
            response.FileHash = storageResult.FileHash;
            response.FullAccessUrl = fullAccessUrl;
            uploadResults.Add(response);
        }

        // Return all results, both successful and failed
        return Success(uploadResults);
    }

    /// <summary>
    /// Get task notes summary for a specific task within an onboarding (used in completion context)
    /// </summary>
    /// <param name="onboardingId">Onboarding case ID</param>
    /// <param name="taskId">Checklist task ID</param>
    /// <returns>Notes summary including total count, pinned count, and latest note preview</returns>
    [HttpGet("onboarding/{onboardingId}/task/{taskId}/notes-summary")]
    [ProducesResponseType<SuccessResponse<ChecklistTaskNotesSummaryDto>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetTaskNotesSummary(long onboardingId, long taskId)
    {
        var summary = await _noteService.GetNotesSummaryAsync(taskId, onboardingId);
        return Success(summary);
    }
}


