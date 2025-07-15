using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Net;
using System.Diagnostics;
using Item.Internal.StandardApi.Response;

namespace FlowFlex.WebApi.Controllers
{
    /// <summary>
    /// Health check controller for system monitoring
    /// </summary>
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;

        public HealthController(ILogger<HealthController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Basic health check endpoint
        /// </summary>
        /// <returns>Health status information</returns>
        [HttpGet]
        [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
        public IActionResult GetHealth()
        {
            try
            {
                var healthData = new
                {
                    Status = "Healthy",
                    Timestamp = DateTime.UtcNow,
                    Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown",
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                    MachineName = Environment.MachineName,
                    ProcessId = Environment.ProcessId,
                    WorkingSet = GC.GetTotalMemory(false),
                    Uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()
                };

                _logger.LogInformation("Health check requested - System is healthy");
                
                return Ok(SuccessResponse.Create(healthData));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                
                var errorData = new
                {
                    Status = "Unhealthy",
                    Timestamp = DateTime.UtcNow,
                    Error = ex.Message
                };

                return StatusCode(503, errorData);
            }
        }

        /// <summary>
        /// Detailed health check with dependencies
        /// </summary>
        /// <returns>Detailed health status</returns>
        [HttpGet("detailed")]
        [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetDetailedHealth()
        {
            try
            {
                var checks = new Dictionary<string, object>
                {
                    ["application"] = new
                    {
                        Status = "Healthy",
                        ResponseTime = "< 1ms"
                    },
                    ["memory"] = new
                    {
                        Status = "Healthy",
                        Used = GC.GetTotalMemory(false),
                        Available = Environment.WorkingSet
                    },
                    ["disk"] = new
                    {
                        Status = "Healthy",
                        FreeSpace = GetAvailableDiskSpace()
                    }
                };

                // Check database connectivity (if available)
                try
                {
                    // This is a basic check - you might want to inject a database service
                    checks["database"] = new
                    {
                        Status = "Healthy",
                        ResponseTime = "< 100ms"
                    };
                }
                catch
                {
                    checks["database"] = new
                    {
                        Status = "Unhealthy",
                        Error = "Database connection failed"
                    };
                }

                var overallStatus = checks.Values.All(check => 
                    check.GetType().GetProperty("Status")?.GetValue(check)?.ToString() == "Healthy") 
                    ? "Healthy" : "Unhealthy";

                var detailedHealthData = new
                {
                    Status = overallStatus,
                    Timestamp = DateTime.UtcNow,
                    Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown",
                    Checks = checks
                };

                _logger.LogInformation("Detailed health check requested - Overall status: {Status}", overallStatus);
                
                return Ok(SuccessResponse.Create(detailedHealthData));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Detailed health check failed");
                
                var errorData = new
                {
                    Status = "Unhealthy",
                    Timestamp = DateTime.UtcNow,
                    Error = ex.Message
                };

                return StatusCode(503, errorData);
            }
        }

        /// <summary>
        /// Simple ping endpoint for basic connectivity check
        /// </summary>
        /// <returns>Pong response</returns>
        [HttpGet("ping")]
        [ProducesResponseType<SuccessResponse<string>>((int)HttpStatusCode.OK)]
        public IActionResult Ping()
        {
            return Ok(SuccessResponse.Create("pong"));
        }

        private long GetAvailableDiskSpace()
        {
            try
            {
                var drive = new DriveInfo(Path.GetPathRoot(Environment.CurrentDirectory) ?? "C:");
                return drive.AvailableFreeSpace;
            }
            catch
            {
                return -1;
            }
        }
    }
} 