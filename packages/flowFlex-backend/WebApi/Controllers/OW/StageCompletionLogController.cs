using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using System.Text.Json;
using FlowFlex.Application.Contracts.Dtos.OW.StageCompletion;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Shared.Attr;
using Item.Internal.StandardApi.Response;


namespace FlowFlex.WebApi.Controllers.OW
{
    /// <summary>
    /// Stage Completion Log API
    /// </summary>
    [ApiController]
    [Route("ow/logs/stage-completion/v{version:apiVersion}")]
    [Display(Name = "stage-completion-logs")]
    [Authorize] // 添加授权特性，要求所有stage completion log API都需要认证
    public class StageCompletionLogController : Controllers.ControllerBase
    {
        private readonly IStageCompletionLogService _stageCompletionLogService;

        public StageCompletionLogController(IStageCompletionLogService stageCompletionLogService)
        {
            _stageCompletionLogService = stageCompletionLogService;
        }

        /// <summary>
        /// Record stage completion log
        /// </summary>
        /// <param name="input">Log data</param>
        /// <returns>Recording result</returns>
        [HttpPost("")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> CreateLogAsync([FromBody] StageCompletionLogInputDto input)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                // Model state errors logged by structured logging
                return BadRequest(new { Code = 400, Message = errors, Status = "error" });
            }
            bool result = await _stageCompletionLogService.CreateLogAsync(input);
            return Success(result);
        }

        /// <summary>
        /// Get logs by onboarding id
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>Log list</returns>
        [HttpGet("onboarding/{onboardingId}")]
        [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetLogsByOnboardingAsync(long onboardingId)
        {
            var result = await _stageCompletionLogService.GetByOnboardingIdAsync(onboardingId);
            return Success(result);
        }

        /// <summary>
        /// Get logs by stage
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Onboarding ID (optional, if provided returns logs only for this onboarding)</param>
        /// <returns>Log list</returns>
        [HttpGet("stage/{stageId}")]
        [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetLogsByStageAsync(long stageId, [FromQuery] long? onboardingId = null)
        {
            object result;
            if (onboardingId.HasValue)
            {
                // If onboardingId is provided, return logs only for this onboarding and stage
                result = await _stageCompletionLogService.GetByOnboardingAndStageAsync(onboardingId.Value, stageId);
            }
            else
            {
                // If onboardingId is not provided, return all logs for this stage (for backward compatibility)
                result = await _stageCompletionLogService.GetByStageIdAsync(stageId);
            }
            return Success(result);
        }

        /// <summary>
        /// Get logs by onboarding and stage
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Log list</returns>
        [HttpGet("onboarding/{onboardingId}/stage/{stageId}")]
        [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetLogsByOnboardingAndStageAsync(long onboardingId, long stageId)
        {
            var result = await _stageCompletionLogService.GetByOnboardingAndStageAsync(onboardingId, stageId);
            return Success(result);
        }

        /// <summary>
        /// Get logs by log type
        /// </summary>
        /// <param name="logType">Log type</param>
        /// <param name="days">Recent days</param>
        /// <returns>Log list</returns>
        [HttpGet("by-type/{logType}")]
        [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetLogsByTypeAsync(string logType, [FromQuery] int days = 7)
        {
            var result = await _stageCompletionLogService.GetByLogTypeAsync(logType, days);
            return Success(result);
        }

        /// <summary>
        /// Get error logs
        /// </summary>
        /// <param name="days">Recent days</param>
        /// <returns>Error log list</returns>
        [HttpGet("errors")]
        [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetErrorLogsAsync([FromQuery] int days = 7)
        {
            var result = await _stageCompletionLogService.GetErrorLogsAsync(days);
            return Success(result);
        }

        /// <summary>
        /// Get log statistics
        /// </summary>
        /// <param name="onboardingId">Onboarding ID (optional)</param>
        /// <param name="days">Statistics days</param>
        /// <returns>Statistics information</returns>
        [HttpGet("statistics")]
        [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetLogStatisticsAsync([FromQuery] long? onboardingId = null, [FromQuery] int days = 7)
        {
            var result = await _stageCompletionLogService.GetLogStatisticsAsync(onboardingId, days);
            return Success(result);
        }

        /// <summary>
        /// Batch create logs
        /// </summary>
        /// <param name="inputs">Log data list</param>
        /// <returns>Creation result</returns>
        [HttpPost("batch")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> CreateLogsBatchAsync([FromBody] List<StageCompletionLogInputDto> inputs)
        {
            bool result = await _stageCompletionLogService.BatchCreateAsync(inputs);
            return Success(result);
        }

        /// <summary>
        /// Clean expired logs
        /// </summary>
        /// <param name="days">Retention days</param>
        /// <returns>Cleanup result</returns>
        [HttpPost("cleanup")]
        [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> CleanupExpiredLogsAsync([FromQuery] int days = 30)
        {
            var result = await _stageCompletionLogService.CleanupExpiredLogsAsync(days);
            return Success(result);
        }

        /// <summary>
        /// Export logs
        /// </summary>
        /// <param name="onboardingId">Onboarding ID (optional)</param>
        /// <param name="days">Export days</param>
        /// <param name="format">Export format (json, csv)</param>
        /// <returns>Export file</returns>
        [HttpGet("export")]
        [ProducesResponseType(typeof(FileResult), 200)]
        public async Task<IActionResult> ExportLogsAsync([FromQuery] long? onboardingId = null, [FromQuery] int days = 7, [FromQuery] string format = "json")
        {
            var logs = await _stageCompletionLogService.GetByOnboardingIdAsync(onboardingId ?? 0);
            string fileName = $"stage_completion_logs_{onboardingId ?? 0}_{DateTime.UtcNow:yyyyMMdd}.{format}";
            if (format.ToLower() == "csv")
            {
                var csv = "Id,OnboardingId,StageId,Action,LogType,Success,CreateDate\n";
                foreach (var log in logs)
                {
                    csv += $"{log.Id},{log.OnboardingId},{log.StageId},{log.Action},{log.LogType},{log.Success},{log.CreateDate}\n";
                }
                return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
            }
            else
            {
                var json = JsonSerializer.Serialize(logs);
                return File(Encoding.UTF8.GetBytes(json), "application/json", fileName);
            }
        }

        /// <summary>
        /// Get logs with pagination
        /// </summary>
        [HttpGet("list")]
        [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> ListAsync([FromQuery] StageCompletionLogQueryRequest request)
        {
            var result = await _stageCompletionLogService.ListAsync(request);
            return Success(result);
        }

        /// <summary>
        /// Delete a single log
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeleteAsync(long id)
        {
            var result = await _stageCompletionLogService.DeleteAsync(id);
            return Success(result);
        }

        /// <summary>
        /// Get client IP address
        /// </summary>
        /// <returns>IP address</returns>
        private string GetClientIpAddress()
        {
            string? ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Check if request is from proxy
            if (HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            }
            else if (HttpContext.Request.Headers.ContainsKey("X-Real-IP"))
            {
                ipAddress = HttpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
            }

            return ipAddress ?? "Unknown";
        }
    }
}

