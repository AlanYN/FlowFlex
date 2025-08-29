using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.Stage;
using FlowFlex.Application.Contracts.IServices.OW;
using Item.Internal.StandardApi.Response;
using System.Linq.Dynamic.Core;
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using FlowFlex.Infrastructure.Services;

namespace FlowFlex.WebApi.Controllers.OW
{
    /// <summary>
    /// Stage management API - Includes stage basic management and content management functions
    /// </summary>
    [ApiController]
    [Route("ow/stages/v{version:apiVersion}")]
    [Display(Name = "stage")]
    [Authorize] // 添加授权特性，要求所有stage API都需要认证
    public class StageController : Controllers.ControllerBase
    {
        private readonly IStageService _stageService;
        private readonly IOnboardingService _onboardingService;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;

        public StageController(IStageService stageService, IOnboardingService onboardingService, IBackgroundTaskQueue backgroundTaskQueue)
        {
            _stageService = stageService;
            _onboardingService = onboardingService;
            _backgroundTaskQueue = backgroundTaskQueue;
        }

        #region Stage Basic Management Functions

        /// <summary>
        /// Create stage
        /// </summary>
        [HttpPost]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Create([FromBody] StageInputDto input)
        {
            var id = await _stageService.CreateAsync(input);
            return Success(id);
        }

        /// <summary>
        /// Update stage
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Update(long id, [FromBody] StageInputDto input)
        {
            var result = await _stageService.UpdateAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Delete stage (with confirmation)
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Delete(long id, [FromQuery] bool confirm = false)
        {
            var result = await _stageService.DeleteAsync(id, confirm);
            return Success(result);
        }

        /// <summary>
        /// Get stage by id
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType<SuccessResponse<StageOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetById(long id)
        {
            var data = await _stageService.GetByIdAsync(id);
            return Success(data);
        }

        /// <summary>
        /// Query stage (paged)
        /// </summary>
        [HttpPost("query")]
        [ProducesResponseType<SuccessResponse<PagedResult<StageOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Query([FromBody] StageQueryRequest query)
        {
            var data = await _stageService.QueryAsync(query);
            return Success(data);
        }

        /// <summary>
        /// Get all stages (no pagination)
        /// </summary>
        [HttpGet("all")]
        [ProducesResponseType<SuccessResponse<List<StageOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAll()
        {
            var data = await _stageService.GetAllAsync();
            return Success(data);
        }

        /// <summary>
        /// Sort stages
        /// </summary>
        [HttpPost("sort")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> SortStages([FromBody] SortStagesInputDto input)
        {
            var result = await _stageService.SortStagesAsync(input);
            return Success(result);
        }

        /// <summary>
        /// Combine stages
        /// </summary>
        [HttpPost("combine")]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> CombineStages([FromBody] CombineStagesInputDto input)
        {
            var newStageId = await _stageService.CombineStagesAsync(input);
            return Success(newStageId);
        }

        /// <summary>
        /// Set stage color
        /// </summary>
        [HttpPost("{id}/color")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> SetColor(long id, [FromBody] SetStageColorInputDto input)
        {
            var result = await _stageService.SetColorAsync(id, input.Color);
            return Success(result);
        }



        /// <summary>
        /// Duplicate stage
        /// </summary>
        [HttpPost("{id}/duplicate")]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Duplicate(long id, [FromBody] DuplicateStageInputDto input)
        {
            var newId = await _stageService.DuplicateAsync(id, input);
            return Success(newId);
        }

        /// <summary>
        /// Update stage components
        /// </summary>
        [HttpPost("{id}/components")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdateComponents(long id, [FromBody] UpdateStageComponentsInputDto input)
        {
            var result = await _stageService.UpdateComponentsAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Get stage components
        /// </summary>
        [HttpGet("{id}/components")]
        [ProducesResponseType<SuccessResponse<List<FlowFlex.Domain.Shared.Models.StageComponent>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetComponents(long id)
        {
            var components = await _stageService.GetComponentsAsync(id);
            return Success(components);
        }

        /// <summary>
        /// Force sync stage assignments (debug/maintenance endpoint)
        /// </summary>
        [HttpPost("{id}/sync-assignments")]
        [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> ForceSyncAssignments(long id)
        {
            try
            {
                // Get stage and extract current component IDs
                var stage = await _stageService.GetByIdAsync(id);
                if (stage == null)
                {
                    return BadRequest("Stage not found");
                }

                var checklistIds = stage.Components?
                    .Where(c => c.Key == "checklist")
                    .SelectMany(c => c.ChecklistIds ?? new List<long>())
                    .Distinct()
                    .ToList() ?? new List<long>();

                var questionnaireIds = stage.Components?
                    .Where(c => c.Key == "questionnaires")
                    .SelectMany(c => c.QuestionnaireIds ?? new List<long>())
                    .Distinct()
                    .ToList() ?? new List<long>();

                Console.WriteLine($"[DEBUG] Manual sync for stage {id}:");
                Console.WriteLine($"[DEBUG] Checklist IDs: [{string.Join(",", checklistIds)}]");
                Console.WriteLine($"[DEBUG] Questionnaire IDs: [{string.Join(",", questionnaireIds)}]");

                // Force sync with empty old IDs to ensure all current assignments are created
                var result = await _stageService.SyncAssignmentsFromStageComponentsAsync(
                    id,
                    stage.WorkflowId,
                    new List<long>(), // empty old checklist IDs
                    checklistIds,
                    new List<long>(), // empty old questionnaire IDs
                    questionnaireIds);

                return Success(new
                {
                    success = result,
                    stageId = id,
                    workflowId = stage.WorkflowId,
                    checklistIds = checklistIds,
                    questionnaireIds = questionnaireIds,
                    message = result ? "Sync completed successfully" : "Sync failed"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Manual sync error: {ex.Message}");
                return BadRequest($"Sync failed: {ex.Message}");
            }
        }

        #endregion

        #region Stage Content Management Functions

        /// <summary>
        /// Get complete stage content
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>Complete stage content</returns>
        /// <remarks>
        /// Get all content of the stage:
        /// - Static fields (required/optional)
        /// - Checklist task list
        /// - Questionnaire content
        /// - File upload management
        /// - Internal notes
        /// - Operation logs
        /// </remarks>
        [HttpGet("{stageId}/onboarding/{onboardingId}/content")]
        [ProducesResponseType<SuccessResponse<StageContentDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetStageContentAsync(long stageId, long onboardingId)
        {
            var result = await _stageService.GetStageContentAsync(stageId, onboardingId);
            return Success(result);
        }



        /// <summary>
        /// Update checklist task status
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="taskId">Task ID</param>
        /// <param name="request">Task update request</param>
        /// <returns>Update result</returns>
        /// <remarks>
        /// Update checklist task:
        /// - Mark task as completed/not completed
        /// - Add completion notes
        /// - Record completion time
        /// - Update overall completion rate
        /// </remarks>
        [HttpPut("{stageId}/onboarding/{onboardingId}/checklist/tasks/{taskId}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdateChecklistTaskAsync(long stageId, long onboardingId, long taskId, [FromBody] UpdateChecklistTaskRequest request)
        {
            var result = await _stageService.UpdateChecklistTaskAsync(stageId, onboardingId, taskId, request.IsCompleted, request.CompletionNotes);
            return Success(result);
        }

        /// <summary>
        /// Submit questionnaire answers
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="questionId">Question ID</param>
        /// <param name="request">Answer content</param>
        /// <returns>Submission result</returns>
        /// <remarks>
        /// Submit questionnaire answers:
        /// - Record question answers
        /// - Validate required questions
        /// - Update questionnaire completion rate
        /// - Support multiple question types
        /// </remarks>
        [HttpPost("{stageId}/onboarding/{onboardingId}/questionnaire/questions/{questionId}/answer")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> SubmitQuestionnaireAnswerAsync(long stageId, long onboardingId, long questionId, [FromBody] SubmitAnswerRequest request)
        {
            var result = await _stageService.SubmitQuestionnaireAnswerAsync(stageId, onboardingId, questionId, request.Answer);
            return Success(result);
        }

        /// <summary>
        /// Upload stage file
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="request">File upload request</param>
        /// <returns>Uploaded file information</returns>
        /// <remarks>
        /// Upload stage file:
        /// - File type validation
        /// - File size limit
        /// - File categorization
        /// - File storage management
        /// </remarks>
        [HttpPost("{stageId}/onboarding/{onboardingId}/files")]
        [ProducesResponseType<SuccessResponse<StageFileDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> UploadStageFileAsync(long stageId, long onboardingId, [FromBody] UploadFileRequest request)
        {
            var result = await _stageService.UploadStageFileAsync(stageId, onboardingId, request.FileName, request.FileContent, request.FileCategory);
            return Success(result);
        }

        /// <summary>
        /// Delete stage file
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="fileId">File ID</param>
        /// <returns>Delete result</returns>
        /// <remarks>
        /// Delete stage file:
        /// - Remove file from storage
        /// - Update file list
        /// - Record deletion time
        /// </remarks>
        [HttpDelete("{stageId}/onboarding/{onboardingId}/files/{fileId}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeleteStageFileAsync(long stageId, long onboardingId, long fileId)
        {
            var result = await _stageService.DeleteStageFileAsync(stageId, onboardingId, fileId);
            return Success(result);
        }

        /// <summary>
        /// Validate stage completion
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>Completion validation result</returns>
        /// <remarks>
        /// Validate stage completion:
        /// - Check required fields completion
        /// - Check checklist completion
        /// - Check questionnaire completion
        /// - Check file upload completion
        /// - Check internal notes completion
        /// - Check operation logs completion
        /// </remarks>
        [HttpGet("{stageId}/onboarding/{onboardingId}/validation")]
        [ProducesResponseType<SuccessResponse<StageCompletionValidationDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> ValidateStageCompletionAsync(long stageId, long onboardingId)
        {
            var result = await _stageService.ValidateStageCompletionAsync(stageId, onboardingId);
            return Success(result);
        }

        /// <summary>
        /// Complete stage
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="request">Completion request</param>
        /// <returns>Completion result</returns>
        /// <remarks>
        /// Complete stage:
        /// - Record completion time
        /// - Update completion status
        /// - Update completion notes
        /// - Update completion rate
        /// </remarks>
        [HttpPost("{stageId}/onboarding/{onboardingId}/complete")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> CompleteStageAsync(long stageId, long onboardingId, [FromBody] CompleteStageRequest request)
        {
            var result = await _stageService.CompleteStageAsync(stageId, onboardingId, request.CompletionNotes);
            return Success(result);
        }

        /// <summary>
        /// Add stage note
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="request">Note content</param>
        /// <returns>Add result</returns>
        /// <remarks>
        /// Add stage note:
        /// - Record note content
        /// - Record note type
        /// - Record note timestamp
        /// - Record note author
        /// </remarks>
        [HttpPost("{stageId}/onboarding/{onboardingId}/notes")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> AddStageNoteAsync(long stageId, long onboardingId, [FromBody] AddStageNoteRequest request)
        {
            var result = await _stageService.AddStageNoteAsync(stageId, onboardingId, request.NoteContent, request.IsPrivate);
            return Success(result);
        }

        /// <summary>
        /// Get stage notes
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Stage notes</returns>
        /// <remarks>
        /// Get stage notes:
        /// - Filter by stage
        /// - Filter by onboarding
        /// - Paginate results
        /// </remarks>
        [HttpGet("{stageId}/onboarding/{onboardingId}/notes")]
        [ProducesResponseType<SuccessResponse<StageNotesDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetStageNotesAsync(long stageId, long onboardingId, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _stageService.GetStageNotesAsync(stageId, onboardingId, pageIndex, pageSize);
            return Success(result);
        }

        /// <summary>
        /// Update stage note
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="noteId">Note ID</param>
        /// <param name="request">Note content</param>
        /// <returns>Update result</returns>
        /// <remarks>
        /// Update stage note:
        /// - Record note content
        /// - Record note timestamp
        /// - Record note author
        /// </remarks>
        [HttpPut("{stageId}/onboarding/{onboardingId}/notes/{noteId}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdateStageNoteAsync(long stageId, long onboardingId, long noteId, [FromBody] UpdateStageNoteRequest request)
        {
            var result = await _stageService.UpdateStageNoteAsync(stageId, onboardingId, noteId, request.NoteContent);
            return Success(result);
        }

        /// <summary>
        /// Delete stage note
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="noteId">Note ID</param>
        /// <returns>Delete result</returns>
        /// <remarks>
        /// Delete stage note:
        /// - Remove note from storage
        /// - Record deletion time
        /// </remarks>
        [HttpDelete("{stageId}/onboarding/{onboardingId}/notes/{noteId}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeleteStageNoteAsync(long stageId, long onboardingId, long noteId)
        {
            var result = await _stageService.DeleteStageNoteAsync(stageId, onboardingId, noteId);
            return Success(result);
        }

        /// <summary>
        /// Generate AI Summary for stage with streaming response
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Onboarding ID (optional)</param>
        /// <param name="language">Preferred language for summary (optional)</param>
        /// <returns>Streaming AI summary response</returns>
        [HttpPost("{stageId}/ai-summary/stream")]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task StreamAISummary(
            [FromRoute] long stageId,
            [FromQuery] long? onboardingId = null,
            [FromQuery] string? language = null)
        {
            // 设置流式响应头 - 纯文本流
            Response.ContentType = "text/plain; charset=utf-8";
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");
            Response.Headers.Append("Access-Control-Allow-Origin", "*");

            try
            {
                // 准备AI Summary生成输入
                var summaryOptions = new StageSummaryOptions
                {
                    Language = language ?? "auto",
                    SummaryLength = "medium",
                    IncludeTaskAnalysis = true,
                    IncludeQuestionnaireInsights = true
                };

                // 调用AI服务生成摘要
                var summaryResult = await _stageService.GenerateAISummaryAsync(stageId, onboardingId, summaryOptions);

                if (summaryResult.Success && !string.IsNullOrEmpty(summaryResult.Summary))
                {
                    // 直接以纯文本形式流式发送AI内容
                    var fullContent = summaryResult.Summary;
                    var chunkSize = Math.Max(5, fullContent.Length / 20); // 将内容分成20个左右的块，每块至少5个字符

                    for (int i = 0; i < fullContent.Length; i += chunkSize)
                    {
                        var chunk = fullContent.Substring(i, Math.Min(chunkSize, fullContent.Length - i));

                        // 直接发送文本内容，不使用JSON格式
                        await Response.WriteAsync(chunk);
                        await Response.Body.FlushAsync();

                        // 添加小延迟来模拟真实的AI流式响应
                        await Task.Delay(100);
                    }

                    // 异步更新数据库 - 不阻塞响应
                    var generatedAt = DateTime.UtcNow;
                    _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                    {
                        try
                        {
                            await UpdateAISummaryInDatabaseAsync(stageId, onboardingId, fullContent, generatedAt, summaryResult.ConfidenceScore, summaryResult.ModelUsed);
                        }
                        catch (Exception updateEx)
                        {
                            // 记录错误但不影响用户体验
                            Console.WriteLine($"Failed to update AI summary in database: {updateEx.Message}");
                        }
                    });
                }
                else
                {
                    // 发送错误信息，以纯文本形式
                    var errorMessage = summaryResult?.Message ?? "Failed to generate AI summary";
                    await Response.WriteAsync($"Error: {errorMessage}");
                }

                await Response.Body.FlushAsync();
            }
            catch (Exception ex)
            {
                // 发送错误信息，以纯文本形式
                await Response.WriteAsync($"Error: Stream error: {ex.Message}");
                await Response.Body.FlushAsync();
            }
        }

        /// <summary>
        /// 异步更新AI Summary到数据库
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Onboarding ID (optional)</param>
        /// <param name="aiSummary">AI Summary content</param>
        /// <param name="generatedAt">Generated timestamp</param>
        /// <param name="confidence">Confidence score</param>
        /// <param name="modelUsed">AI model used</param>
        /// <returns></returns>
        private async Task UpdateAISummaryInDatabaseAsync(long stageId, long? onboardingId, string aiSummary, DateTime generatedAt, double? confidence, string modelUsed)
        {
            try
            {
                // 1. 更新Stage表的AI Summary字段（backfill，如果为空的话）
                await _stageService.UpdateStageAISummaryIfEmptyAsync(stageId, aiSummary, generatedAt, confidence, modelUsed);

                // 2. 如果有onboardingId，更新Onboarding的stagesProgress
                if (onboardingId.HasValue)
                {
                    await _onboardingService.UpdateOnboardingStageAISummaryAsync(onboardingId.Value, stageId, aiSummary, generatedAt, confidence, modelUsed);
                }

                Console.WriteLine($"✅ Successfully updated AI summary in database for stage {stageId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to update AI summary in database for stage {stageId}: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Data Consistency Management

        /// <summary>
        /// Validate stage data consistency between components and mappings
        /// </summary>
        [HttpPost("{id}/validate-consistency")]
        [ProducesResponseType<SuccessResponse<StageConsistencyResult>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> ValidateConsistency(long id, [FromQuery] bool autoRepair = true)
        {
            var result = await _stageService.ValidateAndRepairConsistencyAsync(id, autoRepair);
            return Success(result);
        }

        /// <summary>
        /// Batch validate stage data consistency for multiple stages
        /// </summary>
        [HttpPost("batch/validate-consistency")]
        [ProducesResponseType<SuccessResponse<List<StageConsistencyResult>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> BatchValidateConsistency([FromBody] BatchValidateConsistencyRequest request)
        {
            if (request.StageIds == null || !request.StageIds.Any())
            {
                return BadRequest("StageIds cannot be empty");
            }

            var results = await _stageService.BatchValidateAndRepairConsistencyAsync(request.StageIds, request.AutoRepair);
            return Success(results);
        }

        /// <summary>
        /// Manually sync checklist name change to all related stages
        /// </summary>
        [HttpPost("sync/checklist-name/{checklistId}")]
        [ProducesResponseType<SuccessResponse<int>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> SyncChecklistNameChange(long checklistId, [FromBody] SyncNameChangeRequest request)
        {
            var nameSync = HttpContext.RequestServices.GetService<IStageComponentNameSyncService>();
            if (nameSync == null)
            {
                return BadRequest("Component name sync service not available");
            }

            var updatedStages = await nameSync.SyncChecklistNameChangeAsync(checklistId, request.NewName);
            return Success(updatedStages);
        }

        /// <summary>
        /// Manually sync questionnaire name change to all related stages
        /// </summary>
        [HttpPost("sync/questionnaire-name/{questionnaireId}")]
        [ProducesResponseType<SuccessResponse<int>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> SyncQuestionnaireNameChange(long questionnaireId, [FromBody] SyncNameChangeRequest request)
        {
            var nameSync = HttpContext.RequestServices.GetService<IStageComponentNameSyncService>();
            if (nameSync == null)
            {
                return BadRequest("Component name sync service not available");
            }

            var updatedStages = await nameSync.SyncQuestionnaireNameChangeAsync(questionnaireId, request.NewName);
            return Success(updatedStages);
        }

        /// <summary>
        /// Refresh component names for a specific stage
        /// </summary>
        [HttpPost("{id}/refresh-component-names")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> RefreshStageComponentNames(long id)
        {
            var nameSync = HttpContext.RequestServices.GetService<IStageComponentNameSyncService>();
            if (nameSync == null)
            {
                return BadRequest("Component name sync service not available");
            }

            var success = await nameSync.RefreshStageComponentNamesAsync(id);
            return Success(success);
        }

        /// <summary>
        /// Validate and fix all stage component names across the system
        /// </summary>
        [HttpPost("validate-and-fix-all-component-names")]
        [ProducesResponseType<SuccessResponse<int>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> ValidateAndFixAllComponentNames()
        {
            var nameSync = HttpContext.RequestServices.GetService<IStageComponentNameSyncService>();
            if (nameSync == null)
            {
                return BadRequest("Component name sync service not available");
            }

            var fixedStages = await nameSync.ValidateAndFixAllStageComponentNamesAsync();
            return Success(fixedStages);
        }



        #endregion
    }

    #region Request Models

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

    #endregion

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



}