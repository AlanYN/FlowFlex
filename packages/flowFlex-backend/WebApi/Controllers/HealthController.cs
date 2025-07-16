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
            return Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// Database connectivity check
        /// </summary>
        [HttpGet("database")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public IActionResult DatabaseCheck()
        {
            try
            {
                // Test database connection using SqlSugar
                _sqlSugarClient.Ado.CheckConnection();
                
                return Ok(new 
                { 
                    Status = "Healthy", 
                    Database = "Connected",
                    TestResult = "Connection OK",
                    Timestamp = DateTime.UtcNow,
                    Provider = _sqlSugarClient.CurrentConnectionConfig.DbType.ToString(),
                    ConnectionString = HidePassword(_sqlSugarClient.CurrentConnectionConfig.ConnectionString)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new 
                { 
                    Status = "Unhealthy", 
                    Database = "Disconnected",
                    Error = ex.Message,
                    Type = ex.GetType().Name,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Detailed system health check
        /// </summary>
        [HttpGet("detailed")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> DetailedCheck()
        {
            var healthStatus = new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0",
                Environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                Database = await CheckDatabaseHealthAsync(),
                Memory = new
                {
                    WorkingSet = GC.GetTotalMemory(false),
                    Gen0Collections = GC.CollectionCount(0),
                    Gen1Collections = GC.CollectionCount(1),
                    Gen2Collections = GC.CollectionCount(2)
                },
                Server = new
                {
                    MachineName = System.Environment.MachineName,
                    ProcessorCount = System.Environment.ProcessorCount,
                    OSVersion = System.Environment.OSVersion.ToString(),
                    DotNETVersion = System.Environment.Version.ToString()
                }
            };

            return Ok(healthStatus);
        }

        private Task<object> CheckDatabaseHealthAsync()
        {
            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                // Test connection using SqlSugar's synchronous method
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
                    Error = ex.Message,
                    Type = ex.GetType().Name
                };
                
                return Task.FromResult<object>(result);
            }
        }

        private static string HidePassword(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return "";

            return System.Text.RegularExpressions.Regex.Replace(
                connectionString, 
                @"(password|pwd)\s*=\s*[^;]*", 
                "$1=***", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
    }
} 