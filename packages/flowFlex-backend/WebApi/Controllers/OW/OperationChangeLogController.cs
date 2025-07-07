using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Item.Common.Lib.EnumUtil;
using Item.Internal.StandardApi.Response;
using System.Net;
using FlowFlex.Application.Contracts.Dtos.OW.OperationChangeLog;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.Models;
using FlowFlex.Domain.Shared.Enums.OW;


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
        /// <param name="stageId">Stage ID (optional, used to filter logs for specific stage)</param>
        /// <param name="pageIndex">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Paginated operation log list</returns>
        [HttpGet("onboarding/{onboardingId}")]
        [ProducesResponseType<SuccessResponse<PagedResult<OperationChangeLogOutputDto>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetLogsByOnboardingAsync(
            long onboardingId,
            [FromQuery] long? stageId = null,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _operationChangeLogService.GetOperationLogsAsync(
                onboardingId,
                stageId,
                null,
                pageIndex,
                pageSize
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
            [FromQuery] int pageSize = 20)
        {
            var result = await _operationChangeLogService.GetOperationLogsAsync(
                null,
                stageId,
                null,
                pageIndex,
                pageSize
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
    }
}
