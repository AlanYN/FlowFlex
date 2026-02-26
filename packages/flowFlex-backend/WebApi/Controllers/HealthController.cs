using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Net;

namespace FlowFlex.WebApi.Controllers
{
    /// <summary>
    /// Health check controller
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ISqlSugarClient _sqlSugarClient;

        public HealthController(ISqlSugarClient sqlSugarClient)
        {
            _sqlSugarClient = sqlSugarClient;
        }

        /// <summary>
        /// Basic health check
        /// </summary>
        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public IActionResult Get()
        {
            return Ok(new { Status = "Healthy", Timestamp = DateTimeOffset.UtcNow });
        }

        /// <summary>
        /// Database connectivity check
        /// Requires authentication to prevent information disclosure
        /// </summary>
        [HttpGet("database")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public IActionResult DatabaseCheck()
        {
            try
            {
                _sqlSugarClient.Ado.CheckConnection();

                return Ok(new
                {
                    Status = "Healthy",
                    Database = "Connected",
                    TestResult = "Connection OK",
                    Timestamp = DateTimeOffset.UtcNow,
                    Provider = _sqlSugarClient.CurrentConnectionConfig.DbType.ToString()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    Status = "Unhealthy",
                    Database = "Disconnected",
                    Error = ex.Message,
                    Timestamp = DateTimeOffset.UtcNow
                });
            }
        }

        /// <summary>
        /// Detailed system health check
        /// Requires authentication - exposes system diagnostics information
        /// </summary>
        [HttpGet("detailed")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> DetailedCheck()
        {
            var healthStatus = new
            {
                Status = "Healthy",
                Timestamp = DateTimeOffset.UtcNow,
                Version = "1.0.0",
                Database = await CheckDatabaseHealthAsync(),
                Memory = new
                {
                    WorkingSet = GC.GetTotalMemory(false),
                    Gen0Collections = GC.CollectionCount(0),
                    Gen1Collections = GC.CollectionCount(1),
                    Gen2Collections = GC.CollectionCount(2)
                }
            };

            return Ok(healthStatus);
        }

        private Task<object> CheckDatabaseHealthAsync()
        {
            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                _sqlSugarClient.Ado.CheckConnection();
                stopwatch.Stop();

                var result = new
                {
                    Status = "Connected",
                    ResponseTime = $"{stopwatch.ElapsedMilliseconds}ms",
                    TestResult = "Connection OK",
                    Provider = _sqlSugarClient.CurrentConnectionConfig.DbType.ToString()
                };

                return Task.FromResult<object>(result);
            }
            catch (Exception ex)
            {
                var result = new
                {
                    Status = "Disconnected",
                    Error = ex.Message
                };

                return Task.FromResult<object>(result);
            }
        }
    }
}
