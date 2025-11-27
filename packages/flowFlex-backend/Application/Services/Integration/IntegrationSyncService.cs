using System.Diagnostics;
using AutoMapper;
using Domain.Shared.Enums;
using FlowFlex.Application.Contracts.Dtos.Integration;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.IServices.Integration;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums;
using FlowFlex.Domain.Shared.Exceptions;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FlowFlex.Application.Services.Integration
{
    /// <summary>
    /// Integration sync service implementation
    /// </summary>
    public class IntegrationSyncService : IIntegrationSyncService, IScopedService
    {
        private readonly IIntegrationSyncLogRepository _syncLogRepository;
        private readonly IIntegrationRepository _integrationRepository;
        private readonly IEntityMappingRepository _entityMappingRepository;
        private readonly IInboundFieldMappingRepository _fieldMappingRepository;
        private readonly IMapper _mapper;
        private readonly UserContext _userContext;
        private readonly ILogger<IntegrationSyncService> _logger;

        public IntegrationSyncService(
            IIntegrationSyncLogRepository syncLogRepository,
            IIntegrationRepository integrationRepository,
            IEntityMappingRepository entityMappingRepository,
            IInboundFieldMappingRepository fieldMappingRepository,
            IMapper mapper,
            UserContext userContext,
            ILogger<IntegrationSyncService> logger)
        {
            _syncLogRepository = syncLogRepository;
            _integrationRepository = integrationRepository;
            _entityMappingRepository = entityMappingRepository;
            _fieldMappingRepository = fieldMappingRepository;
            _mapper = mapper;
            _userContext = userContext;
            _logger = logger;
        }

        public async Task<bool> SyncInboundAsync(long integrationId, string entityType, string externalEntityId)
        {
            var stopwatch = Stopwatch.StartNew();
            var syncLog = new IntegrationSyncLog
            {
                IntegrationId = integrationId,
                SyncDirection = SyncDirection.ViewOnly,
                EntityType = entityType,
                ExternalId = externalEntityId,
                SyncStatus = SyncStatus.InProgress
            };
            syncLog.InitCreateInfo(_userContext);

            try
            {
                // Get integration
                var integration = await _integrationRepository.GetByIdAsync(integrationId);
                if (integration == null)
                {
                    throw new CRMException(ErrorCodeEnum.NotFound, "Integration not found");
                }

                // Get entity mapping
                var entityMapping = await _entityMappingRepository.GetByExternalEntityTypeAsync(integrationId, entityType);
                if (entityMapping == null)
                {
                    throw new CRMException(ErrorCodeEnum.NotFound, $"Entity mapping for '{entityType}' not found");
                }

                // Get field mappings by integration ID
                var fieldMappings = await _fieldMappingRepository.GetByIntegrationIdAsync(integrationId);
                var inboundFields = fieldMappings.Where(f => 
                    f.SyncDirection == SyncDirection.ViewOnly || 
                    f.SyncDirection == SyncDirection.Editable).ToList();

                // TODO: Implement actual data sync logic
                // 1. Fetch data from external system using integration credentials
                // 2. Transform data according to field mappings
                // 3. Create or update WFE entity
                // 4. Handle attachments if configured

                _logger.LogInformation($"Syncing inbound data for integration {integrationId}, entity type {entityType}, external ID {externalEntityId}");

                stopwatch.Stop();
                syncLog.SyncStatus = SyncStatus.Success;
                syncLog.DurationMs = stopwatch.ElapsedMilliseconds;
                syncLog.ResponsePayload = JsonConvert.SerializeObject(new { message = "Sync completed successfully" });

                await _syncLogRepository.InsertAsync(syncLog);

                return true;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                syncLog.SyncStatus = SyncStatus.Failed;
                syncLog.DurationMs = stopwatch.ElapsedMilliseconds;
                syncLog.ErrorMessage = ex.Message;
                syncLog.ResponsePayload = JsonConvert.SerializeObject(new { error = ex.ToString() });

                await _syncLogRepository.InsertAsync(syncLog);

                _logger.LogError(ex, $"Inbound sync failed for integration {integrationId}");
                throw;
            }
        }

        public async Task<bool> SyncOutboundAsync(long integrationId, string entityType, long wfeEntityId)
        {
            var stopwatch = Stopwatch.StartNew();
            var syncLog = new IntegrationSyncLog
            {
                IntegrationId = integrationId,
                SyncDirection = SyncDirection.OutboundOnly,
                EntityType = entityType,
                InternalId = wfeEntityId.ToString(),
                SyncStatus = SyncStatus.InProgress
            };
            syncLog.InitCreateInfo(_userContext);

            try
            {
                // Get integration
                var integration = await _integrationRepository.GetByIdAsync(integrationId);
                if (integration == null)
                {
                    throw new CRMException(ErrorCodeEnum.NotFound, "Integration not found");
                }

                // Get entity mapping
                var entityMapping = await _entityMappingRepository.GetByWfeMasterDataTypeAsync(integrationId, entityType);
                if (entityMapping == null)
                {
                    throw new CRMException(ErrorCodeEnum.NotFound, $"Entity mapping for '{entityType}' not found");
                }

                // Get field mappings by integration ID
                var fieldMappings = await _fieldMappingRepository.GetByIntegrationIdAsync(integrationId);
                var outboundFields = fieldMappings.Where(f => 
                    f.SyncDirection == SyncDirection.OutboundOnly || 
                    f.SyncDirection == SyncDirection.Editable).ToList();

                // TODO: Implement actual data sync logic
                // 1. Fetch WFE entity data
                // 2. Transform data according to field mappings
                // 3. Send data to external system using integration credentials
                // 4. Handle attachments if configured

                _logger.LogInformation($"Syncing outbound data for integration {integrationId}, entity type {entityType}, WFE ID {wfeEntityId}");

                stopwatch.Stop();
                syncLog.SyncStatus = SyncStatus.Success;
                syncLog.DurationMs = stopwatch.ElapsedMilliseconds;
                syncLog.ResponsePayload = JsonConvert.SerializeObject(new { message = "Sync completed successfully" });

                await _syncLogRepository.InsertAsync(syncLog);

                return true;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                syncLog.SyncStatus = SyncStatus.Failed;
                syncLog.DurationMs = stopwatch.ElapsedMilliseconds;
                syncLog.ErrorMessage = ex.Message;
                syncLog.ResponsePayload = JsonConvert.SerializeObject(new { error = ex.ToString() });

                await _syncLogRepository.InsertAsync(syncLog);

                _logger.LogError(ex, $"Outbound sync failed for integration {integrationId}");
                throw;
            }
        }

        public async Task<(List<IntegrationSyncLogOutputDto> items, int total)> GetSyncLogsAsync(
            long integrationId,
            int pageIndex,
            int pageSize,
            string? syncDirection = null,
            string? status = null)
        {
            var (items, total) = await _syncLogRepository.QueryPagedAsync(
                pageIndex,
                pageSize,
                integrationId,
                syncDirection,
                status);

            var dtos = _mapper.Map<List<IntegrationSyncLogOutputDto>>(items);

            return (dtos, total);
        }

        public async Task<bool> RetrySyncAsync(long syncLogId)
        {
            var syncLog = await _syncLogRepository.GetByIdAsync(syncLogId);
            if (syncLog == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Sync log not found");
            }

            if (syncLog.SyncStatus != SyncStatus.Failed)
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Only failed syncs can be retried");
            }

            // Retry the sync based on direction
            if (syncLog.SyncDirection == SyncDirection.ViewOnly)
            {
                return await SyncInboundAsync(syncLog.IntegrationId, syncLog.EntityType, syncLog.ExternalId ?? string.Empty);
            }
            else if (syncLog.SyncDirection == SyncDirection.OutboundOnly)
            {
                return await SyncOutboundAsync(syncLog.IntegrationId, syncLog.EntityType, long.Parse(syncLog.InternalId ?? "0"));
            }

            throw new CRMException(ErrorCodeEnum.BusinessError, "Invalid sync direction");
        }

        /// <summary>
        /// Log sync operation
        /// </summary>
        public async Task<long> LogSyncAsync(IntegrationSyncLogInputDto input)
        {
            var entity = _mapper.Map<Domain.Entities.Integration.IntegrationSyncLog>(input);
            entity.InitCreateInfo(_userContext);

            var id = await _syncLogRepository.InsertReturnSnowflakeIdAsync(entity);
            _logger.LogInformation($"Logged sync operation for integration {input.IntegrationId}, status: {input.SyncStatus}");

            return id;
        }

        /// <summary>
        /// Get sync statistics for an integration
        /// </summary>
        public async Task<Dictionary<string, int>> GetSyncStatisticsAsync(long integrationId, DateTime? startDate = null, DateTime? endDate = null)
        {
            return await _syncLogRepository.GetSyncStatisticsAsync(integrationId, startDate, endDate);
        }

        /// <summary>
        /// Get failed sync logs
        /// </summary>
        public async Task<List<IntegrationSyncLogOutputDto>> GetFailedSyncLogsAsync(long integrationId, int limit = 50)
        {
            var entities = await _syncLogRepository.GetFailedSyncLogsAsync(integrationId, limit);
            return _mapper.Map<List<IntegrationSyncLogOutputDto>>(entities);
        }
    }
}

