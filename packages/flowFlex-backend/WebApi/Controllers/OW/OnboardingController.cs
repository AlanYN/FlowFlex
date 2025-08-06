using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.Dtos;
using FlowFlex.Domain.Shared.Attr;
using Item.Internal.StandardApi.Response;
using System.Net;

using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.WebApi.Controllers.OW
{
    /// <summary>
    /// Onboarding management API
    /// </summary>
    [ApiController]
    [Route("ow/onboardings/v{version:apiVersion}")]
    [Display(Name = "onboarding")]
    [Authorize] // 添加授权特性，要求所有onboarding API都需要认证
    public class OnboardingController : Controllers.ControllerBase
    {
        private readonly IOnboardingService _onboardingService;

        public OnboardingController(IOnboardingService onboardingService)
        {
            _onboardingService = onboardingService;
        }

        /// <summary>
        /// Create new onboarding
        /// </summary>
        [HttpPost]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> CreateAsync([FromBody] OnboardingInputDto input)
        {
            // Input validation and debug information logged by structured logging

            if (input != null)
            {
                // Input parameters logged by structured logging
            }

            // Model state validation logged by structured logging

            if (!ModelState.IsValid)
            {
                // Model state errors logged by structured logging
            }

            // Check if input is null and return appropriate error
            if (input == null)
            {
                // Input parameter validation logged by structured logging
                return BadRequest("Request body is required and must contain valid JSON");
            }

            // Service call logged by structured logging
            long result = await _onboardingService.CreateAsync(input);
            return Success(result);
        }

        /// <summary>
        /// Update onboarding
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdateAsync(long id, [FromBody] OnboardingInputDto input)
        {
            bool result = await _onboardingService.UpdateAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Delete onboarding (with confirmation)
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeleteAsync(long id, [FromQuery] bool confirm = false)
        {
            bool result = await _onboardingService.DeleteAsync(id, confirm);
            return Success(result);
        }

        /// <summary>
        /// Get onboarding by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType<SuccessResponse<OnboardingOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByIdAsync(long id)
        {
            OnboardingOutputDto result = await _onboardingService.GetByIdAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Get onboarding list
        /// </summary>
        [HttpGet]
        [ProducesResponseType<SuccessResponse<List<OnboardingOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetListAsync()
        {
            List<OnboardingOutputDto> result = await _onboardingService.GetListAsync();
            return Success(result);
        }



        /// <summary>
        /// Query onboarding list with pagination (POST method)
        /// Supports comma-separated values for leadId, leadName, and updatedBy fields
        /// All text search queries are case-insensitive
        /// Example: {"leadId": "11,22,33", "leadName": "company1,company2", "updatedBy": "user1,user2"}
        /// </summary>
        [HttpPost("query")]
        [ProducesResponseType<SuccessResponse<PageModelDto<OnboardingOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> QueryAsync([FromBody] OnboardingQueryRequest query)
        {
            try
            {
                PageModelDto<OnboardingOutputDto> result = await _onboardingService.QueryAsync(query);
                return Success(result);
            }
            catch (FlowFlex.Domain.Shared.CRMException ex)
            {
                // Log specific CRM exception details
                Console.WriteLine($"[CRM Exception] {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[Inner Exception] {ex.InnerException.Message}");
                }
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Unexpected Error] {ex.Message}");
                Console.WriteLine($"[Stack Trace] {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Test database connection health
        /// </summary>
        [HttpGet("health")]
        [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> HealthCheckAsync()
        {
            try
            {
                // Simple query to test database connection
                var testResult = await _onboardingService.GetListAsync();
                return Success(new { Status = "Healthy", Message = "Database connection is working", RecordCount = testResult?.Count ?? 0 });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Status = "Unhealthy", Message = ex.Message, Type = ex.GetType().Name });
            }
        }

        /// <summary>
        /// Test EstimatedCompletionDate serialization
        /// </summary>
        [HttpGet("test-estimated-completion-date")]
        [ProducesResponseType<SuccessResponse<OnboardingOutputDto>>((int)HttpStatusCode.OK)]
        public IActionResult TestEstimatedCompletionDate()
        {
            // Create a test OnboardingOutputDto with null EstimatedCompletionDate
            var testDto = new OnboardingOutputDto
            {
                Id = 123,
                WorkflowId = 1,
                WorkflowName = "Test Workflow",
                Status = "InProgress",
                CompletionRate = 50,
                StartDate = DateTimeOffset.UtcNow.AddDays(-5),
                EstimatedCompletionDate = null, // This should now appear in JSON as null
                ActualCompletionDate = null,
                LeadId = "TEST001",
                LeadName = "Test Company",
                CurrentStageOrder = 2,
                Priority = "Medium",
                IsActive = true,
                CreateDate = DateTimeOffset.UtcNow.AddDays(-10),
                CreateBy = "System",
                ModifyDate = DateTimeOffset.UtcNow,
                ModifyBy = "System"
            };

            return Success(testDto);
        }

        /// <summary>
        /// Search onboarding list with pagination (GET method for UI table)
        /// Supports comma-separated values for leadId, leadName, and updatedBy parameters
        /// All text search queries are case-insensitive
        /// Example: ?leadId=11,22,33&leadName=company1,company2&updatedBy=user1,user2
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType<SuccessResponse<PageModelDto<OnboardingOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> SearchAsync(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string sortField = "CreateDate",
            [FromQuery] string sortDirection = "desc",
            [FromQuery] string leadId = null,
            [FromQuery] string leadName = null,
            [FromQuery] long? lifeCycleStageId = null,
            [FromQuery] string lifeCycleStageName = null,
            [FromQuery] long? currentStageId = null,
            [FromQuery] string updatedBy = null,
            [FromQuery] long? updatedByUserId = null,
            [FromQuery] string createdBy = null,
            [FromQuery] long? createdByUserId = null,
            [FromQuery] string priority = null,
            [FromQuery] string status = null,
            [FromQuery] long? workflowId = null,
            [FromQuery] bool? isActive = null)
        {
            var query = new OnboardingQueryRequest
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                SortField = sortField,
                SortDirection = sortDirection,
                LeadId = leadId,
                LeadName = leadName,
                LifeCycleStageId = lifeCycleStageId,
                LifeCycleStageName = lifeCycleStageName,
                CurrentStageId = currentStageId,
                UpdatedBy = updatedBy,
                UpdatedByUserId = updatedByUserId,
                CreatedBy = createdBy,
                CreatedByUserId = createdByUserId,
                Priority = priority,
                Status = status,
                WorkflowId = workflowId,
                IsActive = isActive
            };

            PageModelDto<OnboardingOutputDto> result = await _onboardingService.QueryAsync(query);
            return Success(result);
        }

        /// <summary>
        /// Get onboarding statistics
        /// </summary>
        [HttpGet("statistics")]
        [ProducesResponseType<SuccessResponse<OnboardingStatisticsDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetStatisticsAsync()
        {
            OnboardingStatisticsDto result = await _onboardingService.GetStatisticsAsync();
            return Success(result);
        }

        /// <summary>
        /// Get overdue onboarding list
        /// </summary>
        [HttpGet("overdue")]
        [ProducesResponseType<SuccessResponse<List<OnboardingOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetOverdueListAsync()
        {
            List<OnboardingOutputDto> result = await _onboardingService.GetOverdueListAsync();
            return Success(result);
        }

        /// <summary>
        /// Move onboarding to next stage
        /// </summary>
        [HttpPost("{id}/next-stage")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> MoveToNextStageAsync(long id)
        {
            bool result = await _onboardingService.MoveToNextStageAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Move onboarding to previous stage
        /// </summary>
        [HttpPost("{id}/previous-stage")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> MoveToPreviousStageAsync(long id)
        {
            bool result = await _onboardingService.MoveToPreviousStageAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Move onboarding to specific stage
        /// </summary>
        [HttpPost("{id}/move-to-stage")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> MoveToStageAsync(long id, [FromBody] MoveToStageInputDto input)
        {
            bool result = await _onboardingService.MoveToStageAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Complete current stage
        /// </summary>
        [HttpPost("{id}/complete-stage")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> CompleteCurrentStageAsync(long id)
        {
            bool result = await _onboardingService.CompleteCurrentStageAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Complete current stage with validation
        /// </summary>
        [HttpPost("{id}/complete-stage-with-validation")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> CompleteCurrentStageWithValidationAsync(long id, [FromBody] CompleteCurrentStageInputDto input)
        {
            // Method call and parameters logged by structured logging

            try
            {
                bool result = await _onboardingService.CompleteCurrentStageAsync(id, input);
                // Service call success logged by structured logging
                return Success(result);
            }
            catch (Exception ex)
            {
                // Service call exception logged by structured logging
                throw;
            }
        }

        /// <summary>
        /// Complete current stage with details
        /// </summary>
        [HttpPost("{id}/complete-stage-with-details")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> CompleteStageAsync(long id, [FromBody] CompleteStageInputDto input)
        {
            bool result = await _onboardingService.CompleteStageAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Complete onboarding
        /// </summary>
        [HttpPost("{id}/complete")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> CompleteAsync(long id)
        {
            bool result = await _onboardingService.CompleteAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Complete onboarding with details
        /// </summary>
        [HttpPost("{id}/complete-with-details")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> CompleteWithDetailsAsync(long id, [FromBody] CompleteOnboardingInputDto input)
        {
            bool result = await _onboardingService.CompleteAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Pause onboarding
        /// </summary>
        [HttpPost("{id}/pause")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> PauseAsync(long id)
        {
            bool result = await _onboardingService.PauseAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Resume onboarding
        /// </summary>
        [HttpPost("{id}/resume")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> ResumeAsync(long id)
        {
            bool result = await _onboardingService.ResumeAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Reject onboarding application
        /// </summary>
        [HttpPost("{id}/reject")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> RejectAsync(long id, [FromBody] RejectOnboardingInputDto input)
        {
            bool result = await _onboardingService.RejectAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Cancel onboarding
        /// </summary>
        [HttpPost("{id}/cancel")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> CancelAsync(long id, [FromQuery] string reason = "")
        {
            bool result = await _onboardingService.CancelAsync(id, reason);
            return Success(result);
        }

        /// <summary>
        /// Assign onboarding to user
        /// </summary>
        [HttpPost("{id}/assign")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> AssignAsync(long id, [FromBody] AssignOnboardingInputDto input)
        {
            bool result = await _onboardingService.AssignAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Update completion rate
        /// </summary>
        [HttpPost("{id}/update-completion-rate")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdateCompletionRateAsync(long id)
        {
            bool result = await _onboardingService.UpdateCompletionRateAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Set onboarding priority
        /// </summary>
        [HttpPost("{id}/set-priority")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> SetPriorityAsync(long id, [FromQuery] string priority)
        {
            bool result = await _onboardingService.SetPriorityAsync(id, priority);
            return Success(result);
        }

        /// <summary>
        /// Batch update onboarding status
        /// </summary>
        [HttpPost("batch-update-status")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> BatchUpdateStatusAsync([FromBody] BatchUpdateStatusInputDto input)
        {
            bool result = await _onboardingService.BatchUpdateStatusAsync(input.Ids, input.Status);
            return Success(result);
        }

        /// <summary>
        /// Get onboarding timeline
        /// </summary>
        [HttpGet("{id}/timeline")]
        [ProducesResponseType<SuccessResponse<List<OnboardingTimelineDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetTimelineAsync(long id)
        {
            List<OnboardingTimelineDto> result = await _onboardingService.GetTimelineAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Add note to onboarding
        /// </summary>
        [HttpPost("{id}/notes")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> AddNoteAsync(long id, [FromBody] AddNoteInputDto input)
        {
            bool result = await _onboardingService.AddNoteAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Update onboarding status
        /// </summary>
        [HttpPut("{id}/status")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdateStatusAsync(long id, [FromBody] UpdateStatusInputDto input)
        {
            bool result = await _onboardingService.UpdateStatusAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Update onboarding priority
        /// </summary>
        [HttpPut("{id}/priority")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdatePriorityAsync(long id, [FromBody] UpdatePriorityInputDto input)
        {
            bool result = await _onboardingService.UpdatePriorityAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Restart onboarding
        /// </summary>
        [HttpPost("{id}/restart")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> RestartAsync(long id, [FromBody] RestartOnboardingInputDto input)
        {
            bool result = await _onboardingService.RestartAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Get onboarding progress
        /// </summary>
        [HttpGet("{id}/progress")]
        [ProducesResponseType<SuccessResponse<OnboardingProgressDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetProgressAsync(long id)
        {
            OnboardingProgressDto result = await _onboardingService.GetProgressAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Export onboarding list to Excel
        /// Supports comma-separated values for leadId, leadName, and updatedBy fields
        /// All text search queries are case-insensitive
        /// </summary>
        [HttpPost("export-excel")]
        [ProducesResponseType(typeof(FileResult), 200)]
        public async Task<IActionResult> ExportToExcelAsync([FromBody] OnboardingQueryRequest query)
        {
            var stream = await _onboardingService.ExportToExcelAsync(query);
            var fileName = $"onboarding_export_{DateTimeOffset.Now:MMddyyyy_HHmmss}.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        /// <summary>
        /// Export onboarding list to Excel (GET method with query parameters)
        /// Supports comma-separated values for leadId, leadName, and updatedBy parameters
        /// All text search queries are case-insensitive
        /// </summary>
        [HttpGet("export-excel")]
        [ProducesResponseType(typeof(FileResult), 200)]
        public async Task<IActionResult> ExportToExcelAsync(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10000, // Large page size for export
            [FromQuery] string sortField = "CreateDate",
            [FromQuery] string sortDirection = "desc",
            [FromQuery] string leadId = null,
            [FromQuery] string leadName = null,
            [FromQuery] long? lifeCycleStageId = null,
            [FromQuery] string lifeCycleStageName = null,
            [FromQuery] long? currentStageId = null,
            [FromQuery] string updatedBy = null,
            [FromQuery] long? updatedByUserId = null,
            [FromQuery] string priority = null,
            [FromQuery] string status = null,
            [FromQuery] long? workflowId = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string onboardingIds = null)
        {
            var query = new OnboardingQueryRequest
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                SortField = sortField,
                SortDirection = sortDirection,
                LeadId = leadId,
                LeadName = leadName,
                LifeCycleStageId = lifeCycleStageId,
                LifeCycleStageName = lifeCycleStageName,
                CurrentStageId = currentStageId,
                UpdatedBy = updatedBy,
                UpdatedByUserId = updatedByUserId,
                Priority = priority,
                Status = status,
                WorkflowId = workflowId,
                IsActive = isActive,
                OnboardingIds = !string.IsNullOrEmpty(onboardingIds) 
                    ? onboardingIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(id => long.TryParse(id.Trim(), out var parsedId) ? parsedId : 0)
                        .Where(id => id > 0)
                        .ToList()
                    : null
            };

            var stream = await _onboardingService.ExportToExcelAsync(query);
            var fileName = $"onboarding_export_{DateTimeOffset.Now:MMddyyyy_HHmmss}.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        /// <summary>
        /// Sync stages progress from workflow stages configuration
        /// Updates VisibleInPortal and AttachmentManagementNeeded fields from stage definitions
        /// </summary>
        [HttpPost("{id}/sync-stages-progress")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> SyncStagesProgressAsync(long id)
        {
            bool result = await _onboardingService.SyncStagesProgressAsync(id);
            return Success(result);
        }
    }

    /// <summary>
    /// Batch update status DTO
    /// </summary>
    public class BatchUpdateStatusDto
    {
        /// <summary>Onboarding IDs</summary>
        public List<long> Ids { get; set; }

        /// <summary>Target status</summary>
        public string Status { get; set; }
    }
}

