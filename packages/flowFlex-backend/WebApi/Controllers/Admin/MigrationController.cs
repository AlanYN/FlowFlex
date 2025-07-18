using Microsoft.AspNetCore.Mvc;
using FlowFlex.Infrastructure.Services;
using FlowFlex.Application.Contracts.IServices.OW;

namespace FlowFlex.WebApi.Controllers.Admin
{
    /// <summary>
    /// Database migration management controller
    /// </summary>
    [ApiController]
    [Route("api/admin/[controller]")]
    public class MigrationController : ControllerBase
    {
        private readonly DatabaseMigrationService _migrationService;
        private readonly ILogger<MigrationController> _logger;

        public MigrationController(
            DatabaseMigrationService migrationService,
            ILogger<MigrationController> logger)
        {
            _migrationService = migrationService;
            _logger = logger;
        }

        /// <summary>
        /// Get migration history
        /// </summary>
        /// <returns>Migration history</returns>
        [HttpGet("history")]
        public async Task<IActionResult> GetMigrationHistory()
        {
            try
            {
                var history = await _migrationService.GetMigrationHistoryAsync();
                return Ok(new
                {
                    Code = 200,
                    Message = "Migration history retrieved successfully",
                    Data = history
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving migration history");
                return StatusCode(500, new
                {
                    Code = 500,
                    Message = "Failed to retrieve migration history",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Execute pending migrations manually
        /// </summary>
        /// <returns>Migration result</returns>
        [HttpPost("execute")]
        public async Task<IActionResult> ExecuteMigrations()
        {
            try
            {
                await _migrationService.ExecuteMigrationsAsync();
                return Ok(new
                {
                    Code = 200,
                    Message = "Migrations executed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing migrations");
                return StatusCode(500, new
                {
                    Code = 500,
                    Message = "Failed to execute migrations",
                    Error = ex.Message
                });
            }
        }
    }
}