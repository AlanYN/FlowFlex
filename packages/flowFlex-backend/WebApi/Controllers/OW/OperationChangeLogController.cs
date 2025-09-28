using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Item.Common.Lib.EnumUtil;
using Item.Internal.StandardApi.Response;
using System.Net;
using FlowFlex.Application.Contracts.Dtos.OW.OperationChangeLog;
using FlowFlex.Application.Contracts.IServices.OW;

using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Models;


namespace FlowFlex.WebApi.Controllers.OW
{
    /// <summary>
    /// Operation change log controller
    /// </summary>

    [ApiController]

    [Route("ow/change-logs/v{version:apiVersion}")]
    [Display(Name = "Operation Change Logs")]
    public class OperationChangeLogController : Controllers.ControllerBase
    {
        private readonly IOperationChangeLogService _operationChangeLogService;

        public OperationChangeLogController(IOperationChangeLogService operationChangeLogService)
        {
            _operationChangeLogService = operationChangeLogService;
        }

        /// <summary>
        /// Get operation change log list
        /// </summary>
        /// <param name="query">Query conditions</param>
        /// <returns>Paginated operation log list</returns>
        [HttpPost("list")]
        [ProducesResponseType<SuccessResponse<PagedResult<OperationChangeLogOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetOperationLogsAsync([FromBody] OperationChangeLogQueryDto query)
        {
            OperationTypeEnum? operationType = null;
            if (!string.IsNullOrEmpty(query.OperationType) && Enum.TryParse<OperationTypeEnum>(query.OperationType, out var opType))
            {
                operationType = opType;
            }

            var result = await _operationChangeLogService.GetOperationLogsAsync(
                query.OnboardingId,
                query.StageId,
                operationType,
                query.PageIndex,
                query.PageSize
            );

            return Success(result);
        }

        /// <summary>
        /// Get operation logs by Onboarding ID
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID (required for performance optimization)</param>
        /// <param name="pageIndex">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Paginated operation log list</returns>
        [HttpGet("onboarding/{onboardingId}")]
        [ProducesResponseType<SuccessResponse<PagedResult<OperationChangeLogOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetLogsByOnboardingAsync(
            long onboardingId,
            [FromQuery] long stageId, // 现在是必填参数
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool includeActionExecutions = true)
        {
            // Use optimized method for better performance (database-level pagination)
            var result = await _operationChangeLogService.GetOperationLogsByStageComponentsOptimizedAsync(
                stageId,
                onboardingId,
                null,
                pageIndex,
                pageSize,
                includeActionExecutions
            );

            return Success(result);
        }

        /// <summary>
        /// Get operation logs by Stage ID
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="pageIndex">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Paginated operation log list</returns>
        [HttpGet("stage/{stageId}")]
        [ProducesResponseType<SuccessResponse<PagedResult<OperationChangeLogOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetLogsByStageAsync(
            long stageId,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool includeActionExecutions = true)
        {
            // Use new method to get stage components logs (includes tasks and questions action executions)
            var result = await _operationChangeLogService.GetOperationLogsByStageComponentsAsync(
                stageId,
                null,
                null,
                pageIndex,
                pageSize,
                includeActionExecutions
            );

            return Success(result);
        }

        /// <summary>
        /// Get operation logs by business module and business ID
        /// </summary>
        /// <param name="businessModule">Business module</param>
        /// <param name="businessId">Business ID</param>
        /// <param name="pageIndex">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Paginated operation log list</returns>
        [HttpGet("business/{businessModule}/{businessId}")]
        [ProducesResponseType<SuccessResponse<PagedResult<OperationChangeLogOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetLogsByBusinessAsync(
            string businessModule,
            long businessId,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _operationChangeLogService.GetLogsByBusinessAsync(
                businessModule,
                businessId,
                pageIndex,
                pageSize
            );

            return Success(result);
        }

        /// <summary>
        /// Get operation logs by business ID with optional business type and related data
        /// </summary>
        /// <param name="businessId">Business ID</param>
        /// <param name="type">Business type (optional) - Workflow, Stage, Checklist, Questionnaire, ChecklistTask</param>
        /// <param name="pageIndex">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Paginated operation log list</returns>
        [HttpGet("business/{businessId}")]
        [ProducesResponseType<SuccessResponse<PagedResult<OperationChangeLogOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetLogsByBusinessIdAsync(
            long businessId,
            [FromQuery] BusinessTypeEnum? type = null,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _operationChangeLogService.GetLogsByBusinessIdWithTypeAsync(
                businessId,
                type,
                pageIndex,
                pageSize
            );

            return Success(result);
        }

        /// <summary>
        /// Get operation logs by multiple business IDs (batch query)
        /// </summary>
        /// <param name="businessIds">Business IDs separated by comma (e.g., "123,456,789")</param>
        /// <param name="pageIndex">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Paginated operation log list</returns>
        [HttpGet("business/batch/{businessIds}")]
        [ProducesResponseType<SuccessResponse<PagedResult<OperationChangeLogOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetLogsByBusinessIdsAsync(
            string businessIds,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 20)
        {
            // Parse business IDs from comma-separated string
            var businessIdList = new List<long>();
            try
            {
                if (!string.IsNullOrWhiteSpace(businessIds))
                {
                    businessIdList = businessIds
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(id => long.Parse(id.Trim()))
                        .Distinct()
                        .ToList();
                }
            }
            catch (FormatException)
            {
                return BadRequest("Invalid business ID format. Please provide comma-separated numeric IDs (e.g., '123,456,789').");
            }

            if (!businessIdList.Any())
            {
                return BadRequest("At least one valid business ID must be provided.");
            }

            // Limit the number of IDs to prevent performance issues
            if (businessIdList.Count > 100)
            {
                return BadRequest("Too many business IDs. Maximum 100 IDs are allowed per request.");
            }

            var result = await _operationChangeLogService.GetLogsByBusinessIdsAsync(
                businessIdList,
                pageIndex,
                pageSize
            );

            return Success(result);
        }

        /// <summary>
        /// Get operation statistics
        /// </summary>
        /// <param name="onboardingId">Onboarding ID (optional)</param>
        /// <param name="stageId">Stage ID (optional)</param>
        /// <returns>Operation statistics</returns>
        [HttpGet("statistics")]
        [ProducesResponseType<SuccessResponse<Dictionary<string, int>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetOperationStatisticsAsync(
            [FromQuery] long? onboardingId = null,
            [FromQuery] long? stageId = null)
        {
            var result = await _operationChangeLogService.GetOperationStatisticsAsync(onboardingId, stageId);
            return Success(result);
        }

        /// <summary>
        /// Get operation type enum list
        /// </summary>
        /// <returns>Operation type list</returns>
        [HttpGet("operation-types")]
        [ProducesResponseType<SuccessResponse<List<object>>>((int)HttpStatusCode.OK)]
        public IActionResult GetOperationTypes()
        {
            var operationTypes = Enum.GetValues<OperationTypeEnum>()
                .Select(x => new
                {
                    Value = x.ToString(),
                    Label = x.GetDescription(),
                    Code = (int)x
                })
                .ToList();

            return Success(operationTypes);
        }

        /// <summary>
        /// Get business module enum list
        /// </summary>
        /// <returns>Business module list</returns>
        [HttpGet("business-modules")]
        [ProducesResponseType<SuccessResponse<List<object>>>((int)HttpStatusCode.OK)]
        public IActionResult GetBusinessModules()
        {
            var businessModules = Enum.GetValues<BusinessModuleEnum>()
                .Select(x => new
                {
                    Value = x.ToString(),
                    Label = x.GetDescription(),
                    Code = (int)x
                })
                .ToList();

            return Success(businessModules);
        }

        /// <summary>
        /// Get operation status enum list
        /// </summary>
        /// <returns>Operation status list</returns>
        [HttpGet("operation-statuses")]
        [ProducesResponseType<SuccessResponse<List<object>>>((int)HttpStatusCode.OK)]
        public IActionResult GetOperationStatuses()
        {
            var operationStatuses = Enum.GetValues<OperationStatusEnum>()
                .Select(x => new
                {
                    Value = x.ToString(),
                    Label = x.GetDescription(),
                    Code = (int)x
                })
                .ToList();

            return Success(operationStatuses);
        }

        /// <summary>
        /// Get operation logs by Task ID
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <param name="onboardingId">Onboarding ID (optional)</param>
        /// <param name="pageIndex">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Paginated operation log list including task action executions</returns>
        [HttpGet("task/{taskId}")]
        [ProducesResponseType<SuccessResponse<PagedResult<OperationChangeLogOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetLogsByTaskAsync(
            long taskId,
            [FromQuery] long? onboardingId = null,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool includeActionExecutions = true)
        {
            var result = await _operationChangeLogService.GetOperationLogsByTaskAsync(
                taskId,
                onboardingId,
                null,
                pageIndex,
                pageSize,
                includeActionExecutions
            );

            return Success(result);
        }

        /// <summary>
        /// Get operation logs by Question ID
        /// </summary>
        /// <param name="questionId">Question ID</param>
        /// <param name="onboardingId">Onboarding ID (optional)</param>
        /// <param name="pageIndex">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Paginated operation log list including question action executions</returns>
        [HttpGet("question/{questionId}")]
        [ProducesResponseType<SuccessResponse<PagedResult<OperationChangeLogOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetLogsByQuestionAsync(
            long questionId,
            [FromQuery] long? onboardingId = null,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool includeActionExecutions = true)
        {
            var result = await _operationChangeLogService.GetOperationLogsByQuestionAsync(
                questionId,
                onboardingId,
                null,
                pageIndex,
                pageSize,
                includeActionExecutions
            );

            return Success(result);
        }

        /// <summary>
        /// Get operation logs by Action ID - specifically for Action change history
        /// </summary>
        /// <param name="actionId">Action ID</param>
        /// <param name="pageIndex">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Paginated operation log list for Action changes and related action executions</returns>
        [HttpGet("action/{actionId}")]
        [ProducesResponseType<SuccessResponse<PagedResult<OperationChangeLogOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetLogsByActionAsync(
            long actionId,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 20)
        {
            // Get logs for Action type (BusinessTypeEnum.Action = 6)
            var result = await _operationChangeLogService.GetLogsByBusinessIdWithTypeAsync(
                actionId,
                BusinessTypeEnum.Action,
                pageIndex,
                pageSize
            );

            return Success(result);
        }
    }
}
