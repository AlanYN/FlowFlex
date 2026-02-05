using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.Questionnaire;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts;
using FlowFlex.Application.Contracts.Dtos;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Helpers;
using FlowFlex.Domain.Shared.Models;
using Item.Internal.StandardApi.Response;
using System.Net;
using FlowFlex.Application.Filter;
using FlowFlex.Domain.Shared.Const;
using WebApi.Authorization;

namespace FlowFlex.WebApi.Controllers.OW
{
    /// <summary>
    /// Questionnaire management API
    /// </summary>
    [ApiController]
    [PortalAccess] // Allow Portal token access - Portal users can view and submit questionnaires
    [Route("ow/questionnaires/v{version:apiVersion}")]
    [Display(Name = "questionnaire")]
    [Authorize] // 添加授权特性，要求所有questionnaire API都需要认证
    public class QuestionnaireController : Controllers.ControllerBase
    {
        private readonly IQuestionnaireService _questionnaireService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IAttachmentService _attachmentService;
        private readonly IComponentMappingService _mappingService;
        private readonly UserContext _userContext;

        public QuestionnaireController(
            IQuestionnaireService questionnaireService, 
            IFileStorageService fileStorageService, 
            IAttachmentService attachmentService,
            IComponentMappingService mappingService,
            UserContext userContext)
        {
            _questionnaireService = questionnaireService;
            _fileStorageService = fileStorageService;
            _attachmentService = attachmentService;
            _mappingService = mappingService;
            _userContext = userContext;
        }

        /// <summary>
        /// Create questionnaire
        /// Requires QUESTION:CREATE permission
        /// </summary>
        [HttpPost]
        [WFEAuthorize(PermissionConsts.Question.Create)]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Create([FromBody] QuestionnaireInputDto input)
        {
            // Input validation and model state errors logged by structured logging

            if (!ModelState.IsValid)
            {
                // Model state errors logged by structured logging
            }

            if (input == null)
            {
                // Input parameter validation logged by structured logging
                return BadRequest("Input parameter is null");
            }

            // Input parameters logged by structured logging

            var id = await _questionnaireService.CreateAsync(input);
            return Success(id);
        }

        /// <summary>
        /// Update questionnaire
        /// Requires QUESTION:UPDATE permission
        /// </summary>
        [HttpPut("{id}")]
        [WFEAuthorize(PermissionConsts.Question.Update)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Update(long id, [FromBody] QuestionnaireInputDto input)
        {
            // Input validation
            if (input == null)
            {
                return BadRequest("Request body is required and must contain valid JSON");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _questionnaireService.UpdateAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Delete questionnaire (with confirmation)
        /// Requires QUESTION:DELETE permission
        /// </summary>
        [HttpDelete("{id}")]
        [WFEAuthorize(PermissionConsts.Question.Delete)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Delete(long id, [FromQuery] bool confirm = false)
        {
            var result = await _questionnaireService.DeleteAsync(id, confirm);
            return Success(result);
        }

        /// <summary>
        /// Get questionnaire by id
        /// Requires QUESTION:READ permission
        /// </summary>
        [HttpGet("{id}")]
        [WFEAuthorize(PermissionConsts.Question.Read)]
        [ProducesResponseType<SuccessResponse<QuestionnaireOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetById(long id)
        {
            var data = await _questionnaireService.GetByIdAsync(id);
            return Success(data);
        }

        /// <summary>
        /// Get questionnaire list by category
        /// Requires QUESTION:READ permission
        /// </summary>
        [HttpGet]
        [WFEAuthorize(PermissionConsts.Question.Read)]
        [ProducesResponseType<SuccessResponse<List<QuestionnaireOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetList([FromQuery] string category = null)
        {
            var data = await _questionnaireService.GetListAsync(category);
            return Success(data);
        }

        /// <summary>
        /// Get questionnaires by stage ID
        /// Requires QUESTION:READ permission
        /// </summary>
        [HttpGet("by-stage/{stageId}")]
        [WFEAuthorize(PermissionConsts.Question.Read)]
        [ProducesResponseType<SuccessResponse<List<QuestionnaireOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByStageId(long stageId)
        {
            var data = await _questionnaireService.GetByStageIdAsync(stageId);
            return Success(data);
        }

        /// <summary>
        /// Get questionnaires by multiple IDs (batch query)
        /// Requires any READ permission (WORKFLOW, CASE, CHECKLIST, QUESTION, or TOOL)
        /// This is a shared query API accessible by any module with read permission
        /// </summary>
        [HttpPost("batch/by-ids")]
        [WFEAuthorize(
            PermissionConsts.Workflow.Read,
            PermissionConsts.Case.Read,
            PermissionConsts.Checklist.Read,
            PermissionConsts.Question.Read,
            PermissionConsts.Tool.Read)]
        [ProducesResponseType<SuccessResponse<List<QuestionnaireOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByIds([FromBody] List<long> ids)
        {
            var data = await _questionnaireService.GetByIdsAsync(ids);
            return Success(data);
        }

        /// <summary>
        /// Query questionnaire (paged)
        /// Supports comma-separated values for name field
        /// All text search queries are case-insensitive
        /// Example: {"name": "questionnaire1,questionnaire2"}
        /// Requires any READ permission (WORKFLOW, CASE, CHECKLIST, QUESTION, or TOOL)
        /// This is a shared query API accessible by any module with read permission
        /// </summary>
        [HttpPost("query")]
        [WFEAuthorize(
            PermissionConsts.Workflow.Read,
            PermissionConsts.Case.Read,
            PermissionConsts.Checklist.Read,
            PermissionConsts.Question.Read,
            PermissionConsts.Tool.Read)]
        [ProducesResponseType<SuccessResponse<List<QuestionnaireOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Query([FromBody] QuestionnaireQueryRequest query)
        {
            var data = await _questionnaireService.QueryAsync(query);
            return Success(data);
        }

        /// <summary>
        /// Duplicate questionnaire
        /// Requires QUESTION:CREATE permission
        /// </summary>
        [HttpPost("{id}/duplicate")]
        [WFEAuthorize(PermissionConsts.Question.Create)]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Duplicate(long id, [FromBody] DuplicateQuestionnaireInputDto input)
        {
            var newId = await _questionnaireService.DuplicateAsync(id, input);
            return Success(newId);
        }

        /// <summary>
        /// Preview questionnaire
        /// Requires QUESTION:READ permission
        /// </summary>
        [HttpGet("{id}/preview")]
        [WFEAuthorize(PermissionConsts.Question.Read)]
        [ProducesResponseType<SuccessResponse<QuestionnaireOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Preview(long id)
        {
            var data = await _questionnaireService.PreviewAsync(id);
            return Success(data);
        }

        /// <summary>
        /// Publish questionnaire
        /// Requires QUESTION:UPDATE permission
        /// </summary>
        [HttpPost("{id}/publish")]
        [WFEAuthorize(PermissionConsts.Question.Update)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Publish(long id)
        {
            var result = await _questionnaireService.PublishAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Archive questionnaire
        /// Requires QUESTION:UPDATE permission
        /// </summary>
        [HttpPost("{id}/archive")]
        [WFEAuthorize(PermissionConsts.Question.Update)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Archive(long id)
        {
            var result = await _questionnaireService.ArchiveAsync(id);
            return Success(result);
        }

        // GetTemplates API removed - template functionality discontinued

        // CreateFromTemplate API removed - template functionality discontinued

        /// <summary>
        /// Validate questionnaire structure
        /// Requires QUESTION:UPDATE permission
        /// </summary>
        [HttpPost("{id}/validate")]
        [WFEAuthorize(PermissionConsts.Question.Update)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> ValidateStructure(long id)
        {
            var result = await _questionnaireService.ValidateStructureAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Update questionnaire statistics
        /// Requires QUESTION:UPDATE permission
        /// </summary>
        [HttpPost("{id}/update-statistics")]
        [WFEAuthorize(PermissionConsts.Question.Update)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdateStatistics(long id)
        {
            var result = await _questionnaireService.UpdateStatisticsAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Batch get questionnaires by stage IDs
        /// Requires any READ permission (WORKFLOW, CASE, CHECKLIST, QUESTION, or TOOL)
        /// This is a shared query API accessible by any module with read permission
        /// </summary>
        [HttpPost("batch/by-stages")]
        [WFEAuthorize(
            PermissionConsts.Workflow.Read,
            PermissionConsts.Case.Read,
            PermissionConsts.Checklist.Read,
            PermissionConsts.Question.Read,
            PermissionConsts.Tool.Read)]
        [ProducesResponseType<SuccessResponse<BatchStageQuestionnaireResponse>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByStageIdsBatch([FromBody] BatchStageQuestionnaireRequest request)
        {
            var result = await _questionnaireService.GetByStageIdsBatchAsync(request);
            return Success(result);
        }

        /// <summary>
        /// Upload question file
        /// </summary>
        /// <param name="formFile">File to upload</param>
        /// <param name="category">File category (optional, default: "QuestionnaireQuestion")</param>
        /// <returns>Complete file upload information</returns>
        [HttpPost("questions/upload-file")]
        [WFEAuthorize(PermissionConsts.Question.Update)]
        [ProducesResponseType<SuccessResponse<QuestionnaireFileUploadResponseDto>>((int)HttpStatusCode.OK)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadQuestionFileAsync(
            IFormFile formFile,
            [FromForm] string category = "QuestionnaireQuestion")
        {
            // Validate file first
            var validationResult = await _fileStorageService.ValidateFileAsync(formFile);
            if (!validationResult.IsValid)
            {
                return BadRequest($"File validation failed: {validationResult.ErrorMessage}");
            }

            // Use AttachmentService to save file and get ID
            var attachmentDto = new AttachmentDto
            {
                FileData = formFile,
                CreateBy = _userContext.UserName
            };

            var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
            var attachment = await _attachmentService.CreateAttachmentAsync(
                attachmentDto, tenantId, CancellationToken.None);

            // Get current gateway/host information
            var request = HttpContext.Request;
            var gateway = $"{request.Scheme}://{request.Host}";
            var fullAccessUrl = attachment.AccessUrl?.StartsWith("http") == true
                ? attachment.AccessUrl
                : $"{gateway}{attachment.AccessUrl}";

            // Create comprehensive response with ID
            var response = new QuestionnaireFileUploadResponseDto
            {
                Id = attachment.Id,
                Success = true,
                AccessUrl = attachment.AccessUrl,
                OriginalFileName = attachment.RealName,
                FileName = attachment.FileName,
                FilePath = attachment.FilePath,
                FileSize = attachment.FileSize,
                ContentType = attachment.FileType,
                Category = category,
                FileHash = attachment.FileHash,
                UploadTime = DateTime.UtcNow,
                ErrorMessage = null,
                Gateway = gateway,
                FullAccessUrl = fullAccessUrl
            };

            return Success(response);
        }

        /// <summary>
        /// Batch upload question files
        /// </summary>
        /// <param name="formFiles">List of files to upload</param>
        /// <param name="category">File category (optional, default: "QuestionnaireQuestion")</param>
        /// <returns>List of complete file upload information</returns>
        [HttpPost("questions/batch-upload-files")]
        [WFEAuthorize(PermissionConsts.Question.Update)]
        [ProducesResponseType<SuccessResponse<List<QuestionnaireFileUploadResponseDto>>>((int)HttpStatusCode.OK)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadMultipleQuestionFilesAsync(
            List<IFormFile> formFiles,
            [FromForm] string category = "QuestionnaireQuestion")
        {
            var uploadResults = new List<QuestionnaireFileUploadResponseDto>();
            var errors = new List<string>();

            // Get current gateway/host information
            var request = HttpContext.Request;
            var gateway = $"{request.Scheme}://{request.Host}";
            var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);

            foreach (var formFile in formFiles)
            {
                var response = new QuestionnaireFileUploadResponseDto
                {
                    OriginalFileName = formFile?.FileName ?? "unknown",
                    Category = category,
                    UploadTime = DateTime.UtcNow,
                    Gateway = gateway
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

                try
                {
                    // Use AttachmentService to save file and get ID
                    var attachmentDto = new AttachmentDto
                    {
                        FileData = formFile,
                        CreateBy = _userContext.UserName
                    };

                    var attachment = await _attachmentService.CreateAttachmentAsync(
                        attachmentDto, tenantId, CancellationToken.None);

                    // Success case
                    var fullAccessUrl = attachment.AccessUrl?.StartsWith("http") == true
                        ? attachment.AccessUrl
                        : $"{gateway}{attachment.AccessUrl}";

                    response.Id = attachment.Id;
                    response.Success = true;
                    response.AccessUrl = attachment.AccessUrl;
                    response.FileName = attachment.FileName;
                    response.FilePath = attachment.FilePath;
                    response.FileSize = attachment.FileSize;
                    response.ContentType = attachment.FileType;
                    response.FileHash = attachment.FileHash;
                    response.FullAccessUrl = fullAccessUrl;
                    uploadResults.Add(response);
                }
                catch (Exception ex)
                {
                    response.Success = false;
                    response.ErrorMessage = $"Upload failed: {ex.Message}";
                    uploadResults.Add(response);
                    errors.Add($"File {formFile.FileName} upload failed: {ex.Message}");
                }
            }

            // Return all results, both successful and failed
            return Success(uploadResults);
        }

        /// <summary>
        /// Debug endpoint: Find which stages contain a specific questionnaire
        /// Requires QUESTION:READ permission
        /// </summary>
        [HttpGet("{id}/debug/stages")]
        [WFEAuthorize(PermissionConsts.Question.Read)]
        [ProducesResponseType<SuccessResponse<List<object>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> DebugFindStagesContainingQuestionnaire(long id)
        {
            var stages = await _questionnaireService.FindStagesContainingQuestionnaireAsync(id);

            var result = stages.Select(s => new
            {
                StageId = s.Id,
                StageName = s.Name,
                WorkflowId = s.WorkflowId,
                ComponentsJson = s.ComponentsJson
            }).ToList();

            return Success(result);
        }

        /// <summary>
        /// Sync component mappings (初始化映射表数据)
        /// Requires QUESTION:UPDATE permission
        /// </summary>
        [HttpPost("sync-mappings")]
        [WFEAuthorize(PermissionConsts.Question.Update)]
        [ApiExplorerSettings(IgnoreApi = true)] // 隐藏在 Swagger 中，仅用于管理
        public async Task<IActionResult> SyncMappings()
        {
            try
            {
                await _mappingService.SyncAllStageMappingsAsync();
                return Success("Component mappings synchronized successfully");
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to sync mappings: {ex.Message}");
            }
        }
    }
}

