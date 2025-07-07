using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.Models;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Application.Contracts.Dtos.OW.StageCompletion;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Stage completion log service implementation
    /// </summary>
    public class StageCompletionLogService : IStageCompletionLogService, IScopedService
    {
        private readonly IStageCompletionLogRepository _repository;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserContext _userContext;

        public StageCompletionLogService(
            IStageCompletionLogRepository repository,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            UserContext userContext)
        {
            _repository = repository;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _userContext = userContext;
        }

        /// <summary>
        /// Create completion log
        /// </summary>
        public async Task<bool> CreateLogAsync(StageCompletionLogInputDto input)
        {
            try
            {
                var logs = new List<StageCompletionLog>();
                var tenantId = GetTenantId();

                Console.WriteLine($"🔍 Creating stage completion log with TenantId: {tenantId}");
                Console.WriteLine($"🔍 Input entries count: {input.Entries?.Count ?? 0}");

                foreach (var entry in input.Entries)
                {
                    var log = new StageCompletionLog
                    {
                        OnboardingId = entry.Data.OnboardingId ?? 0,
                        StageId = entry.Data.StageId ?? 0,
                        Action = entry.Action,
                        LogType = entry.LogType,
                        LogData = JsonConvert.SerializeObject(entry.Data),
                        Success = entry.Success,
                        ErrorMessage = entry.Data.ErrorMessage,
                        NetworkStatus = entry.Data.NetworkStatus,
                        ResponseTime = entry.Data.ResponseTime,
                        UserAgent = entry.UserAgent,
                        IpAddress = entry.IpAddress ?? GetClientIpAddress(),
                        RequestUrl = entry.Data.Endpoint,
                        SessionId = "" // DTO doesn't have SessionId field, temporarily using empty string
                    };

                    // Initialize create information with proper ID and timestamps
                    log.InitCreateInfo(_userContext);

                    Console.WriteLine($"🔍 Created log entry: OnboardingId={log.OnboardingId}, StageId={log.StageId}, TenantId={log.TenantId}");
                    logs.Add(log);
                }

                Console.WriteLine($"🔍 Calling repository CreateBatchAsync with {logs.Count} logs");
                var result = await _repository.CreateBatchAsync(logs);
                Console.WriteLine($"🔍 Repository CreateBatchAsync result: {result}");

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to create stage completion log: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                Console.WriteLine($"❌ Inner exception: {ex.InnerException?.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get log list by Onboarding ID
        /// </summary>
        public async Task<List<StageCompletionLogOutputDto>> GetByOnboardingIdAsync(long onboardingId)
        {
            var entities = await _repository.GetByOnboardingIdAsync(onboardingId);
            return _mapper.Map<List<StageCompletionLogOutputDto>>(entities);
        }

        /// <summary>
        /// Get log list by Stage ID
        /// </summary>
        public async Task<List<StageCompletionLogOutputDto>> GetByStageIdAsync(long stageId)
        {
            var entities = await _repository.GetByStageIdAsync(stageId);
            return _mapper.Map<List<StageCompletionLogOutputDto>>(entities);
        }

        /// <summary>
        /// Get log list by Onboarding ID and Stage ID
        /// </summary>
        public async Task<List<StageCompletionLogOutputDto>> GetByOnboardingAndStageAsync(long onboardingId, long stageId)
        {
            var entities = await _repository.GetByOnboardingAndStageAsync(onboardingId, stageId);
            return _mapper.Map<List<StageCompletionLogOutputDto>>(entities);
        }

        /// <summary>
        /// Get log list by log type
        /// </summary>
        public async Task<List<StageCompletionLogOutputDto>> GetByLogTypeAsync(string logType, int days = 7)
        {
            var entities = await _repository.GetByLogTypeAsync(logType, days);
            return _mapper.Map<List<StageCompletionLogOutputDto>>(entities);
        }

        /// <summary>
        /// Get error log list
        /// </summary>
        public async Task<List<StageCompletionLogOutputDto>> GetErrorLogsAsync(int days = 7)
        {
            var entities = await _repository.GetErrorLogsAsync(days);
            return _mapper.Map<List<StageCompletionLogOutputDto>>(entities);
        }

        /// <summary>
        /// Get log statistics
        /// </summary>
        public async Task<Dictionary<string, object>> GetLogStatisticsAsync(long? onboardingId = null, int days = 7)
        {
            return await _repository.GetLogStatisticsAsync(onboardingId, days);
        }

        /// <summary>
        /// Batch create logs
        /// </summary>
        public async Task<bool> BatchCreateAsync(List<StageCompletionLogInputDto> inputs)
        {
            try
            {
                var allLogs = new List<StageCompletionLog>();
                var tenantId = GetTenantId();

                Console.WriteLine($"🔍 Batch creating stage completion logs with TenantId: {tenantId}");
                Console.WriteLine($"🔍 Input count: {inputs?.Count ?? 0}");

                foreach (var input in inputs)
                {
                    foreach (var entry in input.Entries)
                    {
                        var log = new StageCompletionLog
                        {
                            OnboardingId = entry.Data.OnboardingId ?? 0,
                            StageId = entry.Data.StageId ?? 0,
                            Action = entry.Action,
                            LogType = entry.LogType,
                            LogData = JsonConvert.SerializeObject(entry.Data),
                            Success = entry.Success,
                            ErrorMessage = entry.Data.ErrorMessage,
                            NetworkStatus = entry.Data.NetworkStatus,
                            ResponseTime = entry.Data.ResponseTime,
                            UserAgent = entry.UserAgent,
                            IpAddress = entry.IpAddress ?? GetClientIpAddress(),
                            RequestUrl = entry.Data.Endpoint,
                            SessionId = "" // DTO doesn't have SessionId field, temporarily using empty string
                        };

                        // Initialize create information with proper ID and timestamps
                        log.InitCreateInfo(_userContext);

                        allLogs.Add(log);
                    }
                }

                Console.WriteLine($"🔍 Calling repository CreateBatchAsync with {allLogs.Count} logs");
                var result = await _repository.CreateBatchAsync(allLogs);
                Console.WriteLine($"🔍 Repository CreateBatchAsync result: {result}");

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to batch create stage completion logs: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                Console.WriteLine($"❌ Inner exception: {ex.InnerException?.Message}");
                return false;
            }
        }

        /// <summary>
        /// Clean up expired logs
        /// </summary>
        public async Task<int> CleanupExpiredLogsAsync(int days = 30)
        {
            return await _repository.CleanupExpiredLogsAsync(days);
        }

        /// <summary>
        /// Log list (paged/conditional query)
        /// </summary>
        public async Task<PageModelDto<StageCompletionLogOutputDto>> ListAsync(StageCompletionLogQueryRequest request)
        {
            var (data, total) = await _repository.ListAsync(
                request.OnboardingId,
                request.StageId,
                request.LogType,
                request.Success,
                request.StartDate,
                request.EndDate,
                request.PageIndex,
                request.PageSize);
            var dtos = _mapper.Map<List<StageCompletionLogOutputDto>>(data);
            return new PageModelDto<StageCompletionLogOutputDto>(request.PageIndex, request.PageSize, dtos, total);
        }

        /// <summary>
        /// Delete single log
        /// </summary>
        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _repository.GetFirstAsync(x => x.Id == id && x.IsValid);
            if (entity == null) return false;
            entity.IsValid = false;
            entity.InitUpdateInfo(_userContext);
            return await _repository.UpdateAsync(entity);
        }

        private string GetTenantId()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return "";

            // Try to get TenantId from request headers
            var tenantId = context.Request.Headers["TenantId"].FirstOrDefault();
            if (string.IsNullOrEmpty(tenantId))
            {
                tenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            }

            // If still empty, use default value
            if (string.IsNullOrEmpty(tenantId))
            {
                tenantId = "default";
            }

            return tenantId;
        }

        private string GetClientIpAddress()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return "";

            var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            }
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "";
            }

            return ipAddress;
        }
    }
}