using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Item.Common.Lib.EnumUtil;
using Item.Internal.StandardApi.Response;
using System.Net;
using FlowFlex.Application.Contracts.Dtos.OW.OperationChangeLog;
using FlowFlex.Application.Contracts.IServices.OW;

using FlowFlex.Domain.Shared.Enums.OW;
using System.Linq.Dynamic.Core;


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
    }
}
