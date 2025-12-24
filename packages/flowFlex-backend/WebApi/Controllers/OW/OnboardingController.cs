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
using FlowFlex.Application.Filter;
using FlowFlex.Domain.Shared.Enums.Permission;
using FlowFlex.WebApi.Filters;
using FlowFlex.Domain.Shared.Const;
using WebApi.Authorization;
using FlowFlex.Application.Contracts.Dtos.OW.User;
using AppContext = FlowFlex.Domain.Shared.Models.AppContext;

namespace FlowFlex.WebApi.Controllers.OW
{
    /// <summary>
    /// Onboarding management API
    /// </summary>
    [ApiController]
    [Route("ow/onboardings/v{version:apiVersion}")]
    [Display(Name = "onboarding")]
    [Authorize] // 添加授权特性，要求所有onboarding API都需要认证
    [PortalAccess] // Allow Portal token access - Portal users can view and update onboarding information
    public class OnboardingController : Controllers.ControllerBase
    {
        private readonly IOnboardingService _onboardingService;

        public OnboardingController(IOnboardingService onboardingService)
        {
            _onboardingService = onboardingService;
        }

        /// <summary>
        /// Create new onboarding
        /// Requires CASE:CREATE permission
        /// </summary>
        [HttpPost]
        [WFEAuthorize(PermissionConsts.Case.Create)]
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
        /// Supports updating all fields including permission configuration
        /// Permission fields: PermissionSubjectType, ViewPermissionMode, ViewTeams/ViewUsers, OperateTeams/OperateUsers
        /// Teams and Users are stored separately - data switches based on PermissionSubjectType
        /// Requires CASE:UPDATE permission
        /// </summary>
        [HttpPut("{id}")]
        [WFEAuthorize(PermissionConsts.Case.Update)]
        [RequirePermission(PermissionEntityTypeEnum.Case, OperationTypeEnum.Operate)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdateAsync(long id, [FromBody] OnboardingInputDto input)
        {
            bool result = await _onboardingService.UpdateAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Delete onboarding (with confirmation)
        /// Requires CASE:DELETE permission
        /// </summary>
        [HttpDelete("{id}")]
        [WFEAuthorize(PermissionConsts.Case.Delete)]
        [RequirePermission(PermissionEntityTypeEnum.Case, OperationTypeEnum.Delete)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeleteAsync(long id, [FromQuery] bool confirm = false)
        {
            bool result = await _onboardingService.DeleteAsync(id, confirm);
            return Success(result);
        }

        /// <summary>
        /// Get onboarding by ID
        /// Requires CASE:READ permission
        /// Tenant isolation is enforced - only data belonging to the current tenant can be accessed
        /// </summary>
        [HttpGet("{id}")]
        [WFEAuthorize(PermissionConsts.Case.Read)]
        [RequirePermission(PermissionEntityTypeEnum.Case, OperationTypeEnum.View)]
        [ProducesResponseType<SuccessResponse<OnboardingOutputDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByIdAsync(long id)
        {
            // Validate tenant isolation - ensure tenant ID is provided
            var appContext = HttpContext.Items["AppContext"] as AppContext;
            if (appContext == null || string.IsNullOrEmpty(appContext.TenantId) || appContext.TenantId == "DEFAULT")
            {
                return BadRequest("Tenant ID is required. Please provide X-Tenant-Id header.");
            }

            // Get onboarding data (tenant isolation is automatically enforced at repository level)
            // Repository automatically filters by tenant_id, so only data belonging to current tenant can be accessed
            OnboardingOutputDto? result = await _onboardingService.GetByIdAsync(id);
            // Return null if not found (instead of error)
            return Success(result);
        }

        /// <summary>
        /// Query onboarding list with pagination (POST method)
        /// Supports comma-separated values for leadId, caseName, and updatedBy fields
        /// All text search queries are case-insensitive and support fuzzy matching
        /// leadId supports fuzzy search (partial matching)
        /// Example: {"leadId": "c", "caseName": "company1,company2", "updatedBy": "user1,user2"}
        /// Requires CASE:READ permission
        /// </summary>
        [HttpPost("query")]
        [WFEAuthorize(PermissionConsts.Case.Read)]
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
        /// Complete current stage with validation
        /// Requires CASE:UPDATE permission
        /// </summary>
        [HttpPost("{id}/complete-stage-with-validation")]
        [WFEAuthorize(PermissionConsts.Case.Update)]
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
        /// Pause onboarding
        /// Requires CASE:UPDATE permission
        /// </summary>
        [HttpPost("{id}/pause")]
        [WFEAuthorize(PermissionConsts.Case.Update)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> PauseAsync(long id)
        {
            bool result = await _onboardingService.PauseAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Resume onboarding
        /// Requires CASE:UPDATE permission
        /// </summary>
        [HttpPost("{id}/resume")]
        [WFEAuthorize(PermissionConsts.Case.Update)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> ResumeAsync(long id)
        {
            bool result = await _onboardingService.ResumeAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Reject onboarding application
        /// Requires CASE:UPDATE permission
        /// </summary>
        [HttpPost("{id}/reject")]
        [WFEAuthorize(PermissionConsts.Case.Update)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> RejectAsync(long id, [FromBody] RejectOnboardingInputDto input)
        {
            bool result = await _onboardingService.RejectAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Cancel onboarding
        /// Requires CASE:UPDATE permission
        /// </summary>
        [HttpPost("{id}/cancel")]
        [WFEAuthorize(PermissionConsts.Case.Update)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> CancelAsync(long id, [FromQuery] string reason = "")
        {
            bool result = await _onboardingService.CancelAsync(id, reason);
            return Success(result);
        }

        /// <summary>
        /// Start onboarding (activate an inactive onboarding)
        /// Requires CASE:UPDATE permission
        /// </summary>
        [HttpPost("{id}/start")]
        [WFEAuthorize(PermissionConsts.Case.Update)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> StartOnboardingAsync(long id, [FromBody] StartOnboardingInputDto input)
        {
            bool result = await _onboardingService.StartOnboardingAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Abort onboarding (terminate the process)
        /// Requires CASE:UPDATE permission
        /// </summary>
        [HttpPost("{id}/abort")]
        [WFEAuthorize(PermissionConsts.Case.Update)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> AbortAsync(long id, [FromBody] AbortOnboardingInputDto input)
        {
            bool result = await _onboardingService.AbortAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Reactivate onboarding (restart an aborted onboarding)
        /// Requires CASE:UPDATE permission
        /// </summary>
        [HttpPost("{id}/reactivate")]
        [WFEAuthorize(PermissionConsts.Case.Update)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> ReactivateAsync(long id, [FromBody] ReactivateOnboardingInputDto input)
        {
            bool result = await _onboardingService.ReactivateAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Resume onboarding with confirmation
        /// Requires CASE:UPDATE permission
        /// </summary>
        [HttpPost("{id}/resume-with-confirmation")]
        [WFEAuthorize(PermissionConsts.Case.Update)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> ResumeWithConfirmationAsync(long id, [FromBody] ResumeOnboardingInputDto input)
        {
            bool result = await _onboardingService.ResumeWithConfirmationAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Force complete onboarding (bypass normal validation and set to Force Completed status)
        /// Requires CASE:UPDATE permission
        /// </summary>
        [HttpPost("{id}/force-complete")]
        [WFEAuthorize(PermissionConsts.Case.Update)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> ForceCompleteAsync(long id, [FromBody] ForceCompleteOnboardingInputDto input)
        {
            bool result = await _onboardingService.ForceCompleteAsync(id, input);
            return Success(result);
        }

        /// <summary>
        /// Get onboarding progress
        /// Requires CASE:READ permission
        /// </summary>
        [HttpGet("{id}/progress")]
        [WFEAuthorize(PermissionConsts.Case.Read)]
        [ProducesResponseType<SuccessResponse<OnboardingProgressDto>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetProgressAsync(long id)
        {
            OnboardingProgressDto result = await _onboardingService.GetProgressAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Export onboarding list to Excel
        /// Supports comma-separated values for leadId, caseName, and updatedBy fields
        /// All text search queries are case-insensitive
        /// Requires CASE:READ permission
        /// </summary>
        [HttpPost("export-excel")]
        [WFEAuthorize(PermissionConsts.Case.Read)]
        [ProducesResponseType(typeof(FileResult), 200)]
        public async Task<IActionResult> ExportToExcelAsync([FromBody] OnboardingQueryRequest query)
        {
            var stream = await _onboardingService.ExportToExcelAsync(query);
            var fileName = $"Cases_{DateTimeOffset.Now:MMddyyyy_HHmmss}.xlsx"; // local time for filename
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        /// <summary>
        /// Export onboarding list to Excel (GET method with query parameters)
        /// Supports comma-separated values for leadId, caseName, and updatedBy parameters
        /// All text search queries are case-insensitive
        /// Requires CASE:READ permission
        /// </summary>
        [HttpGet("export-excel")]
        [WFEAuthorize(PermissionConsts.Case.Read)]
        [ProducesResponseType(typeof(FileResult), 200)]
        public async Task<IActionResult> ExportToExcelAsync(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10000, // Large page size for export
            [FromQuery] string sortField = "CreateDate",
            [FromQuery] string sortDirection = "desc",
            [FromQuery] string leadId = null,
            [FromQuery] string caseName = null,
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
                CaseName = caseName,
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
            var fileName = $"Cases_{DateTimeOffset.Now:MMddyyyy_HHmmss}.xlsx"; // local time for filename
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }


        /// <summary>
        /// Update custom fields for a specific stage in onboarding's stagesProgress
        /// Updates CustomEstimatedDays and CustomEndTime fields
        /// Requires CASE:UPDATE permission
        /// </summary>
        [HttpPost("{id}/stage/update-custom-fields")]
        [WFEAuthorize(PermissionConsts.Case.Update)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdateStageCustomFieldsAsync(long id, [FromBody] UpdateStageCustomFieldsInputDto input)
        {
            bool result = await _onboardingService.UpdateStageCustomFieldsAsync(id, input);
            return Success(result);
        }


        /// <summary>
        /// Save a specific stage in onboarding's stagesProgress
        /// Updates the stage's IsSaved, SaveTime, and SavedById fields
        /// Requires CASE:UPDATE permission
        /// </summary>
        [HttpPost("{id}/save")]
        [WFEAuthorize(PermissionConsts.Case.Update)]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> SaveStageAsync(long id, [FromBody] SaveOnboardingStageDto input)
        {
            // Validate that the onboarding ID in URL matches the DTO
            if (input.OnboardingId != id)
            {
                return BadRequest("Onboarding ID in URL does not match the ID in request body");
            }

            bool result = await _onboardingService.SaveStageAsync(input.OnboardingId, input.StageId);
            return Success(result);
        }

        /// <summary>
        /// Get authorized users for onboarding based on permission configuration
        /// If case has no permission restrictions (Public mode), returns all users
        /// If case has permission restrictions, returns only authorized users based on ViewPermissionMode and ViewPermissionSubjectType
        /// Requires CASE:READ permission
        /// </summary>
        [HttpGet("{id}/authorized-users")]
        [WFEAuthorize(PermissionConsts.Case.Read)]
        [ProducesResponseType<SuccessResponse<List<UserTreeNodeDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAuthorizedUsersAsync(long id)
        {
            var result = await _onboardingService.GetAuthorizedUsersAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Get all active onboardings by System ID
        /// Returns all onboarding records where SystemId matches and IsActive is true
        /// Requires CASE:READ permission
        /// </summary>
        /// <param name="systemId">External system identifier (required)</param>
        /// <param name="entityId">External entity ID for filtering (optional)</param>
        /// <param name="sortField">Sort field: createDate, modifyDate, caseName, caseCode, status (default: createDate)</param>
        /// <param name="sortOrder">Sort order: asc, desc (default: desc)</param>
        /// <param name="limit">Maximum number of records to return (default: 100, max: 1000)</param>
        [HttpGet("by-system")]
        [WFEAuthorize(PermissionConsts.Case.Read)]
        [ProducesResponseType<SuccessResponse<List<OnboardingOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetActiveBySystemIdAsync(
            [FromQuery] string systemId,
            [FromQuery] string? entityId = null,
            [FromQuery] string sortField = "createDate",
            [FromQuery] string sortOrder = "desc",
            [FromQuery] int limit = 100)
        {
            if (string.IsNullOrWhiteSpace(systemId))
            {
                return BadRequest("systemId parameter is required");
            }

            // Validate and cap limit
            if (limit <= 0) limit = 100;
            if (limit > 1000) limit = 1000;

            var result = await _onboardingService.GetActiveBySystemIdAsync(systemId, entityId, sortField, sortOrder, limit);
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

