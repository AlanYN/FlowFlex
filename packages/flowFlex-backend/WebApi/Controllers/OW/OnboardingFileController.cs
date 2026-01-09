using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Net;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.Dtos.OW.OnboardingFile;

using Item.Internal.StandardApi.Response;
using System.ComponentModel;
using FlowFlex.Application.Filter;
using FlowFlex.Domain.Shared.Const;
using WebApi.Authorization;
using AppContext = FlowFlex.Domain.Shared.Models.AppContext;

namespace FlowFlex.WebApi.Controllers.OW
{
    /// <summary>
    /// Onboarding file management API
    /// </summary>
    [ApiController]
    [PortalAccess] // Allow Portal token access - Portal users can upload and view files
    [Route("ow/onboardings/{onboardingId}/files/v{version:apiVersion}")]
    [Display(Name = "onboarding-files")]
    [Authorize] // 添加授权特性，要求所有onboarding file API都需要认证
    public class OnboardingFileController : Controllers.ControllerBase
    {
        private readonly IOnboardingFileService _onboardingFileService;
        private readonly IOperatorContextService _operatorContextService;

        public OnboardingFileController(
            IOnboardingFileService onboardingFileService,
            IOperatorContextService operatorContextService)
        {
            _onboardingFileService = onboardingFileService;
            _operatorContextService = operatorContextService;
        }

        /// <summary>
        /// Upload file
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID (optional)</param>
        /// <param name="formFile">File to upload</param>
        /// <param name="category">File category</param>
        /// <param name="description">File description</param>
        /// <returns>Upload result</returns>
        /// Requires CASE:CREATE permission
        [HttpPost]
        [WFEAuthorize(PermissionConsts.Case.Create)]
        [ProducesResponseType<SuccessResponse<OnboardingFileOutputDto>>((int)HttpStatusCode.OK)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadFileAsync(
            [FromRoute] long onboardingId,
            [FromForm] long? stageId,
            IFormFile formFile,
            [FromForm] string category = "Document",
            [FromForm] string description = "")
        {
            var input = new OnboardingFileInputDto
            {
                OnboardingId = onboardingId,
                StageId = stageId,
                FormFile = formFile,
                Category = category,
                Description = description
            };

            var result = await _onboardingFileService.UploadFileAsync(input);
            return Success(result);
        }

        /// <summary>
        /// Batch upload files
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID (optional)</param>
        /// <param name="formFiles">List of files to upload</param>
        /// <param name="category">File category</param>
        /// <param name="description">File description</param>
        /// <returns>Batch upload result</returns>
        [HttpPost("batch")]
        [ProducesResponseType<SuccessResponse<List<OnboardingFileOutputDto>>>((int)HttpStatusCode.OK)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadMultipleFilesAsync(
            [FromRoute] long onboardingId,
            [FromForm] long? stageId,
            List<IFormFile> formFiles,
            [FromForm] string category = "Document",
            [FromForm] string description = "")
        {
            var results = new List<OnboardingFileOutputDto>();

            foreach (var formFile in formFiles)
            {
                var input = new OnboardingFileInputDto
                {
                    OnboardingId = onboardingId,
                    StageId = stageId,
                    FormFile = formFile,
                    Category = category,
                    Description = description
                };

                var result = await _onboardingFileService.UploadFileAsync(input);
                results.Add(result);
            }

            return Success(results);
        }

        /// <summary>
        /// Import files from external URLs (e.g., OSS links) - Synchronous
        /// Downloads files from provided URLs and saves them as onboarding files
        /// Returns import result with progress details for each file
        /// Note: This is a synchronous operation, use import-async for background processing
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="input">Import input containing file URLs</param>
        /// <returns>Import result with progress details</returns>
        [HttpPost("import")]
        [WFEAuthorize(PermissionConsts.Case.Create)]
        [ProducesResponseType<SuccessResponse<ImportFilesResultDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> ImportFilesFromUrlAsync(
            [FromRoute] long onboardingId,
            [FromBody] ImportFilesFromUrlInputDto input)
        {
            if (input == null)
            {
                return BadRequest("Import input is required");
            }

            if (input.Files == null || input.Files.Count == 0)
            {
                return BadRequest("At least one file URL is required");
            }

            // Set onboarding ID and operator info from route/context
            input.OnboardingId = onboardingId;
            input.OperatorId = _operatorContextService.GetOperatorId().ToString();
            input.OperatorName = _operatorContextService.GetOperatorDisplayName();

            var result = await _onboardingFileService.ImportFilesFromUrlAsync(input);
            return Success(result);
        }

        /// <summary>
        /// Start async import task from external URLs
        /// Returns immediately with task ID, import runs in background
        /// Use import-tasks/{taskId} to track progress
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="input">Import input containing file URLs</param>
        /// <returns>Task ID for tracking progress</returns>
        [HttpPost("import-async")]
        [WFEAuthorize(PermissionConsts.Case.Create)]
        [ProducesResponseType<SuccessResponse<StartImportTaskResponseDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> StartImportTaskAsync(
            [FromRoute] long onboardingId,
            [FromBody] ImportFilesFromUrlInputDto input)
        {
            if (input == null)
            {
                return BadRequest("Import input is required");
            }

            if (input.Files == null || input.Files.Count == 0)
            {
                return BadRequest("At least one file URL is required");
            }

            // Set onboarding ID and operator info from route/context
            input.OnboardingId = onboardingId;
            input.OperatorId = _operatorContextService.GetOperatorId().ToString();
            input.OperatorName = _operatorContextService.GetOperatorDisplayName();
            
            // Get TenantId from AppContext for background task
            var appContext = HttpContext.Items["AppContext"] as AppContext;
            input.TenantId = appContext?.TenantId ?? "default";

            var createdBy = _operatorContextService.GetOperatorDisplayName();
            var result = await _onboardingFileService.StartImportTaskAsync(input, createdBy);
            return Success(result);
        }

        /// <summary>
        /// Get import tasks by onboarding ID and stage ID
        /// Returns all import tasks (including in-progress and completed) for the specified stage
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>List of import tasks with progress details</returns>
        [HttpGet("import-tasks")]
        [WFEAuthorize(PermissionConsts.Case.Read)]
        [ProducesResponseType<SuccessResponse<List<FileImportTaskDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetImportTasksByStageAsync(
            [FromRoute] long onboardingId,
            [FromQuery] long stageId)
        {
            var result = await _onboardingFileService.GetImportTasksByStageAsync(onboardingId, stageId);
            return Success(result);
        }

        /// <summary>
        /// Get import task progress by task ID
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="taskId">Task ID</param>
        /// <returns>Task progress details</returns>
        [HttpGet("import-tasks/{taskId}")]
        [WFEAuthorize(PermissionConsts.Case.Read)]
        [ProducesResponseType<SuccessResponse<FileImportTaskDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetImportTaskProgressAsync(
            [FromRoute] long onboardingId,
            [FromRoute] string taskId)
        {
            var result = await _onboardingFileService.GetImportTaskProgressAsync(taskId);
            if (result == null)
            {
                return NotFound($"Import task '{taskId}' not found");
            }
            return Success(result);
        }

        /// <summary>
        /// Cancel a specific file import item
        /// Can only cancel items with status 'Pending' or 'Downloading'
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="taskId">Task ID</param>
        /// <param name="itemId">Item ID to cancel</param>
        /// <returns>Cancel result</returns>
        [HttpPost("import-tasks/{taskId}/items/{itemId}/cancel")]
        [WFEAuthorize(PermissionConsts.Case.Create)]
        [ProducesResponseType<SuccessResponse<CancelImportFileResponseDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> CancelImportItemAsync(
            [FromRoute] long onboardingId,
            [FromRoute] string taskId,
            [FromRoute] string itemId)
        {
            var result = await _onboardingFileService.CancelImportItemAsync(taskId, itemId);
            return Success(result);
        }

        /// <summary>
        /// Cancel all pending items in an import task
        /// Cancels all items with status 'Pending' or 'Downloading'
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="taskId">Task ID</param>
        /// <returns>Cancel result</returns>
        [HttpPost("import-tasks/{taskId}/cancel")]
        [WFEAuthorize(PermissionConsts.Case.Create)]
        [ProducesResponseType<SuccessResponse<CancelImportFileResponseDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> CancelAllPendingItemsAsync(
            [FromRoute] long onboardingId,
            [FromRoute] string taskId)
        {
            var result = await _onboardingFileService.CancelAllPendingItemsAsync(taskId);
            return Success(result);
        }

        /// <summary>
        /// Get all files for Onboarding
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID (optional, if specified only returns files for that Stage)</param>
        /// <param name="category">File category filter (optional)</param>
        /// <returns>File list</returns>
        /// Requires CASE:READ permission
        [HttpGet]
        [WFEAuthorize(PermissionConsts.Case.Read)]
        [ProducesResponseType<SuccessResponse<List<OnboardingFileOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetFilesAsync(
            [FromRoute] long onboardingId,
            [FromQuery] long? stageId = null,
            [FromQuery] string category = null)
        {
            var result = await _onboardingFileService.GetFilesAsync(onboardingId, stageId, category);
            return Success(result);
        }

        /// <summary>
        /// Download file
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <returns>File stream</returns>
        [HttpGet("{fileId}/download")]
        [ProducesResponseType(typeof(FileResult), 200)]
        public async Task<IActionResult> DownloadFileAsync([FromRoute] long fileId)
        {
            var (stream, fileName, contentType) = await _onboardingFileService.DownloadFileAsync(fileId);
            return File(stream, contentType, fileName);
        }

        /// <summary>
        /// Get file access URL
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <returns>File access URL</returns>
        [HttpGet("{fileId}/view")]
        [ProducesResponseType<SuccessResponse<string>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetFileUrlAsync([FromRoute] long fileId)
        {
            var result = await _onboardingFileService.GetFileUrlAsync(fileId);
            return Success(result);
        }

        /// <summary>
        /// Direct file preview - Compatible with legacy system interface
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <returns>File stream</returns>
        /// <remarks>
        /// Compatible with legacy system /crm/shared/v1/files/{id} interface
        /// Returns file stream directly for browser preview, consistent with legacy system behavior
        /// </remarks>
        [HttpGet("{fileId}/preview")]
        [ProducesResponseType(typeof(FileStreamResult), 200)]
        public async Task<IActionResult> PreviewFileAsync([FromRoute] long fileId)
        {
            var (stream, fileName, contentType) = await _onboardingFileService.DownloadFileAsync(fileId);
            return File(stream, contentType ?? "application/octet-stream", fileName);
        }

        /// <summary>
        /// Get file path URL - Compatible with legacy system interface
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <returns>File access path</returns>
        /// <remarks>
        /// Compatible with legacy system /crm/shared/v1/files/{id}/path interface
        /// Returns the access URL path for the file
        /// </remarks>
        [HttpGet("{fileId}/path")]
        [ProducesResponseType<SuccessResponse<string>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetFilePathAsync([FromRoute] long fileId)
        {
            var result = await _onboardingFileService.GetFileUrlAsync(fileId);
            return Success(result);
        }

        /// <summary>
        /// Delete file
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <returns>Delete result</returns>
        [HttpDelete("{fileId}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeleteFileAsync([FromRoute] long fileId)
        {
            var result = await _onboardingFileService.DeleteFileAsync(fileId);
            return Success(result);
        }

        /// <summary>
        /// Update file information
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="input">Update information</param>
        /// <returns>Update result</returns>
        [HttpPut("{fileId}")]
        [ProducesResponseType<SuccessResponse<OnboardingFileOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdateFileAsync(
            [FromRoute] long fileId,
            [FromBody] UpdateOnboardingFileInputDto input)
        {
            var result = await _onboardingFileService.UpdateFileAsync(fileId, input);
            return Success(result);
        }

        /// <summary>
        /// Set file as required
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="isRequired">Whether required</param>
        /// <returns>Update result</returns>
        [HttpPut("{fileId}/required")]
        [ProducesResponseType<SuccessResponse<OnboardingFileOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> SetFileRequiredAsync(
            [FromRoute] long fileId,
            [FromBody] bool isRequired = true)
        {
            var input = new UpdateOnboardingFileInputDto { IsRequired = isRequired };
            var result = await _onboardingFileService.UpdateFileAsync(fileId, input);
            return Success(result);
        }

        /// <summary>
        /// Validate file type
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="fileName">File name</param>
        /// <param name="fileSize">File size</param>
        /// <returns>Validation result</returns>
        [HttpPost("validate")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> ValidateFileAsync(
            [FromRoute] long onboardingId,
            [FromBody] string fileName,
            [FromQuery] long fileSize = 0)
        {
            // Basic file validation logic
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".gif", ".txt", ".xlsx", ".xls" };
            var maxFileSize = 100 * 1024 * 1024; // 100MB

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var isValidExtension = allowedExtensions.Contains(extension);
            var isValidSize = fileSize <= maxFileSize;

            var isValid = isValidExtension && isValidSize;
            return Success(isValid);
        }

        /// <summary>
        /// Batch delete files
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="fileIds">List of file IDs</param>
        /// <returns>Delete result</returns>
        [HttpDelete("batch")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> BatchDeleteFilesAsync(
            [FromRoute] long onboardingId,
            [FromBody] List<long> fileIds)
        {
            var results = new List<bool>();
            foreach (var fileId in fileIds)
            {
                var result = await _onboardingFileService.DeleteFileAsync(fileId);
                results.Add(result);
            }

            var allSuccess = results.All(r => r);
            return Success(allSuccess);
        }

        /// <summary>
        /// Get file statistics
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>File statistics</returns>
        [HttpGet("statistics")]
        [ProducesResponseType<SuccessResponse<OnboardingFileStatisticsDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetFileStatisticsAsync([FromRoute] long onboardingId)
        {
            var result = await _onboardingFileService.GetFileStatisticsAsync(onboardingId);
            return Success(result);
        }

        /// <summary>
        /// Get file list by Stage
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>File list</returns>
        [HttpGet("stage/{stageId}")]
        [ProducesResponseType<SuccessResponse<List<OnboardingFileOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetFilesByStageAsync(
            [FromRoute] long onboardingId,
            [FromRoute] long stageId)
        {
            var result = await _onboardingFileService.GetFilesByStageAsync(stageId);
            return Success(result);
        }

        /// <summary>
        /// Get file list by category
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="category">File category</param>
        /// <returns>File list</returns>
        [HttpGet("category/{category}")]
        [ProducesResponseType<SuccessResponse<List<OnboardingFileOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetFilesByCategoryAsync(
            [FromRoute] long onboardingId,
            [FromRoute] string category)
        {
            var result = await _onboardingFileService.GetFilesByCategoryAsync(category, onboardingId);
            return Success(result);
        }

        /// <summary>
        /// Get required file list
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>Required file list</returns>
        [HttpGet("required")]
        [ProducesResponseType<SuccessResponse<List<OnboardingFileOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetRequiredFilesAsync([FromRoute] long onboardingId)
        {
            var result = await _onboardingFileService.GetRequiredFilesAsync(onboardingId);
            return Success(result);
        }

        /// <summary>
        /// Get file details
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <returns>File details</returns>
        [HttpGet("{fileId}")]
        [ProducesResponseType<SuccessResponse<OnboardingFileOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetFileDetailsAsync([FromRoute] long fileId)
        {
            var result = await _onboardingFileService.GetFileDetailsAsync(fileId);
            return Success(result);
        }
    }
}
