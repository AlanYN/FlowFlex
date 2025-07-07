using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
            Console.WriteLine("=== OnboardingController.CreateAsync - Debug Info ===");
            Console.WriteLine($"Input is null: {input == null}");
            
            if (input != null)
            {
                Console.WriteLine($"LeadId: '{input.LeadId}'");
                Console.WriteLine($"LeadName: '{input.LeadName}'");
                Console.WriteLine($"WorkflowId: {input.WorkflowId}");
                Console.WriteLine($"ContactEmail: '{input.ContactEmail}'");
            }
            
            Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");
            
            if (!ModelState.IsValid)
            {
                Console.WriteLine("=== ModelState Errors ===");
                foreach (var modelError in ModelState)
                {
                    Console.WriteLine($"Key: {modelError.Key}");
                    foreach (var error in modelError.Value.Errors)
                    {
                        Console.WriteLine($"  Error: {error.ErrorMessage}");
                    }
                }
            }
            
            // Check if input is null and return appropriate error
            if (input == null)
            {
                Console.WriteLine("Input parameter is null, returning BadRequest");
                return BadRequest("Request body is required and must contain valid JSON");
            }
            
            Console.WriteLine("=== Calling OnboardingService.CreateAsync ===");
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
        /// </summary>
        [HttpPost("query")]
        [ProducesResponseType<SuccessResponse<PageModelDto<OnboardingOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> QueryAsync([FromBody] OnboardingQueryRequest query)
        {
            PageModelDto<OnboardingOutputDto> result = await _onboardingService.QueryAsync(query);
            return Success(result);
        }

        /// <summary>
        /// Search onboarding list with pagination (GET method for UI table)
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
            Console.WriteLine($"[DEBUG] Controller: CompleteCurrentStageWithValidationAsync called");
            Console.WriteLine($"[DEBUG] Controller: Onboarding ID = {id}");
            Console.WriteLine($"[DEBUG] Controller: Input CurrentStageId = {input?.CurrentStageId}");
            Console.WriteLine($"[DEBUG] Controller: Input CompletionNotes = {input?.CompletionNotes}");

            try
            {
                bool result = await _onboardingService.CompleteCurrentStageAsync(id, input);
                Console.WriteLine($"[DEBUG] Controller: Service call successful, result = {result}");
                return Success(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Controller: Service call failed with exception: {ex.Message}");
                Console.WriteLine($"[DEBUG] Controller: Exception type: {ex.GetType().Name}");
                Console.WriteLine($"[DEBUG] Controller: Stack trace: {ex.StackTrace}");
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
        /// </summary>
        [HttpPost("export-excel")]
        [ProducesResponseType(typeof(FileResult), 200)]
        public async Task<IActionResult> ExportToExcelAsync([FromBody] OnboardingQueryRequest query)
        {
            var stream = await _onboardingService.ExportToExcelAsync(query);
            var fileName = $"onboarding_export_{DateTimeOffset.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        /// <summary>
        /// Export onboarding list to Excel (GET method with query parameters)
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
                Priority = priority,
                Status = status,
                WorkflowId = workflowId,
                IsActive = isActive
            };

            var stream = await _onboardingService.ExportToExcelAsync(query);
            var fileName = $"onboarding_export_{DateTimeOffset.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
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

