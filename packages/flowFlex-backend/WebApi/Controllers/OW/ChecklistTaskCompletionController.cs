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

namespace FlowFlex.WebApi.Controllers.OW;

/// <summary>
/// Checklist Task Completion Controller
/// </summary>
[ApiController]
[Route("ow/checklist-task-completions/v{version:apiVersion}")]
[Display(Name = "checklist-task-completion")]
[Authorize] // 添加授权特性，要求所有checklist task completion API都需要认证
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
    /// </summary>
    [HttpGet]
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
    /// Save task completion
    /// </summary>
    [HttpPost]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> SaveTaskCompletion([FromBody] ChecklistTaskCompletionInputDto input)
    {
        var result = await _completionService.SaveTaskCompletionAsync(input);
        return Success(result);
    }

    /// <summary>
    /// Batch save task completions
    /// </summary>
    [HttpPost("batch")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> BatchSaveTaskCompletions([FromBody] List<ChecklistTaskCompletionInputDto> inputs)
    {
        var result = await _completionService.BatchSaveTaskCompletionsAsync(inputs);
        return Success(result);
    }

    /// <summary>
    /// Get task completions by lead and checklist
    /// </summary>
    [HttpGet("lead/{leadId}/checklist/{checklistId}")]
    [ProducesResponseType<SuccessResponse<List<ChecklistTaskCompletionOutputDto>>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetByLeadAndChecklist(string leadId, long checklistId)
    {
        var result = await _completionService.GetByLeadAndChecklistAsync(leadId, checklistId);
        return Success(result);
    }

    /// <summary>
    /// Get task completions by onboarding and checklist
    /// </summary>
    [HttpGet("onboarding/{onboardingId}/checklist/{checklistId}")]
    [ProducesResponseType<SuccessResponse<List<ChecklistTaskCompletionOutputDto>>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetByOnboardingAndChecklist(long onboardingId, long checklistId)
    {
        var result = await _completionService.GetByOnboardingAndChecklistAsync(onboardingId, checklistId);
        return Success(result);
    }

    /// <summary>
    /// Get completion statistics
    /// </summary>
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
    /// Toggle task completion
    /// </summary>
    [HttpPost("onboarding/{onboardingId}/task/{taskId}/toggle")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
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
        if (formFile == null || formFile.Length == 0)
        {
            return BadRequest("File is required");
        }

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
        if (formFiles == null || formFiles.Count == 0)
        {
            return BadRequest("At least one file is required");
        }

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
    /// Get task notes summary for completion context
    /// </summary>
    [HttpGet("onboarding/{onboardingId}/task/{taskId}/notes-summary")]
    [ProducesResponseType<SuccessResponse<ChecklistTaskNotesSummaryDto>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetTaskNotesSummary(long onboardingId, long taskId)
    {
        var summary = await _noteService.GetNotesSummaryAsync(taskId, onboardingId);
        return Success(summary);
    }

    /// <summary>
    /// Process checklist components and publish action trigger events for completed tasks
    /// </summary>
    [HttpPost("process-checklist-actions")]
    [ProducesResponseType<SuccessResponse<ChecklistActionProcessingResultDto>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> ProcessChecklistActions([FromBody] ProcessChecklistActionsRequestDto request)
    {
        var result = await _completionService.ProcessChecklistComponentActionsAsync(request);
        return Success(result);
    }
}

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


