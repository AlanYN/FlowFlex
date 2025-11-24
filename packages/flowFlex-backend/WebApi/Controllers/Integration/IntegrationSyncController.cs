using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Application.Contracts.Dtos.Integration;
using FlowFlex.Application.Contracts.IServices.Integration;
using Item.Internal.StandardApi.Response;
using System.Net;

namespace FlowFlex.WebApi.Controllers.Integration
{
    /// <summary>
    /// Integration sync management API
    /// </summary>
    [ApiController]
    [Route("integration/sync/v{version:apiVersion}")]
    [Display(Name = "integration-sync")]
    [Authorize]
    public class IntegrationSyncController : Controllers.ControllerBase
    {
        private readonly IIntegrationSyncService _syncService;

        public IntegrationSyncController(IIntegrationSyncService syncService)
        {
            _syncService = syncService;
        }

        /// <summary>
        /// Sync data from external system to WFE (Inbound)
        /// </summary>
        [HttpPost("inbound")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> SyncInbound(
            [FromQuery] long integrationId,
            [FromQuery] string entityType,
            [FromQuery] string externalEntityId)
        {
            var result = await _syncService.SyncInboundAsync(integrationId, entityType, externalEntityId);
            return Success(result);
        }

        /// <summary>
        /// Sync data from WFE to external system (Outbound)
        /// </summary>
        [HttpPost("outbound")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> SyncOutbound(
            [FromQuery] long integrationId,
            [FromQuery] string entityType,
            [FromQuery] long wfeEntityId)
        {
            var result = await _syncService.SyncOutboundAsync(integrationId, entityType, wfeEntityId);
            return Success(result);
        }

        /// <summary>
        /// Get sync logs with pagination (path parameter)
        /// </summary>
        [HttpGet("{integrationId}/logs")]
        [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetSyncLogsByPath(
            long integrationId,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 15,
            [FromQuery] string? syncDirection = null,
            [FromQuery] string? status = null)
        {
            var (items, total) = await _syncService.GetSyncLogsAsync(
                integrationId,
                pageIndex,
                pageSize,
                syncDirection,
                status);

            return Success(new
            {
                items,
                total,
                pageIndex,
                pageSize
            });
        }

        /// <summary>
        /// Get sync logs with pagination (query parameter)
        /// </summary>
        [HttpGet("logs")]
        [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetSyncLogs(
            [FromQuery] long integrationId,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 15,
            [FromQuery] string? syncDirection = null,
            [FromQuery] string? status = null)
        {
            var (items, total) = await _syncService.GetSyncLogsAsync(
                integrationId,
                pageIndex,
                pageSize,
                syncDirection,
                status);

            return Success(new
            {
                items,
                total,
                pageIndex,
                pageSize
            });
        }

        /// <summary>
        /// Get sync statistics
        /// </summary>
        [HttpGet("{integrationId}/statistics")]
        [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetSyncStatistics(long integrationId)
        {
            var statistics = await _syncService.GetSyncStatisticsAsync(integrationId);
            return Success(statistics);
        }

        /// <summary>
        /// Retry failed sync
        /// </summary>
        [HttpPost("retry/{syncLogId}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> RetrySync(long syncLogId)
        {
            var result = await _syncService.RetrySyncAsync(syncLogId);
            return Success(result);
        }
    }
}

