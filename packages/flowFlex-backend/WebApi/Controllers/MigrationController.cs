using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Net;
using FlowFlex.SqlSugarDB.Migrations;
using LocalSuccessResponse = FlowFlex.WebApi.Model.Response.SuccessResponse;
using Item.Internal.StandardApi.Response;
using SqlSugar;

namespace FlowFlex.WebApi.Controllers
{
    /// <summary>
    /// Database migration management API
    /// </summary>
    [ApiController]
    [Route("admin/migrations")]
    [Display(Name = "migration")]
    [AllowAnonymous] // 临时允许匿名访问以便测试
    public class MigrationController : ControllerBase
    {
        private readonly ISqlSugarClient _db;
        private readonly ILogger<MigrationController> _logger;

        public MigrationController(ISqlSugarClient db, ILogger<MigrationController> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Run encrypted access token migration
        /// </summary>
        /// <returns>Migration result</returns>
        [HttpPost("run-encrypted-token-migration")]
        [ProducesResponseType(typeof(LocalSuccessResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> RunEncryptedTokenMigrationAsync()
        {
            try
            {
                _logger.LogInformation("Starting encrypted access token migration...");
                
                await Task.Run(() => RunSpecificMigration.RunEncryptedAccessTokenMigration(_db));
                
                _logger.LogInformation("Encrypted access token migration completed successfully");
                
                return Ok(LocalSuccessResponse.Create("Encrypted access token migration completed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run encrypted access token migration");
                
                return BadRequest(ErrorResponse.Create("MIGRATION_FAILED", $"Migration failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Run token expiry nullable migration
        /// </summary>
        /// <returns>Migration result</returns>
        [HttpPost("run-token-expiry-nullable")]
        [ProducesResponseType(typeof(LocalSuccessResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> RunTokenExpiryNullableMigrationAsync()
        {
            try
            {
                _logger.LogInformation("Starting token expiry nullable migration...");
                
                await Task.Run(() => MakeTokenExpiryNullable_20250101000013.Up(_db));
                
                _logger.LogInformation("Token expiry nullable migration completed successfully");
                
                return Ok(LocalSuccessResponse.Create("Token expiry nullable migration completed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run token expiry nullable migration");
                
                return BadRequest(ErrorResponse.Create("MIGRATION_FAILED", $"Migration failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Run add app code to events migration
        /// </summary>
        /// <returns>Migration result</returns>
        [HttpPost("run-add-app-code-to-events")]
        [ProducesResponseType(typeof(LocalSuccessResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> RunAddAppCodeToEventsMigrationAsync()
        {
            try
            {
                _logger.LogInformation("Starting add app_code to events migration...");
                
                await Task.Run(() => AddAppCodeToEvents_20250101000014.Up(_db));
                
                _logger.LogInformation("Add app_code to events migration completed successfully");
                
                return Ok(LocalSuccessResponse.Create("Add app_code to events migration completed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run add app_code to events migration");
                
                return BadRequest(ErrorResponse.Create("MIGRATION_FAILED", $"Migration failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Check migration status
        /// </summary>
        /// <returns>Migration status information</returns>
        [HttpGet("status")]
        [ProducesResponseType(typeof(LocalSuccessResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetMigrationStatusAsync()
        {
            try
            {
                // Check if encrypted_access_token column exists
                var columnExists = await Task.Run(() => CheckColumnExists("ff_user_invitations", "encrypted_access_token"));
                
                // Check if token_expiry is nullable
                var tokenExpiryNullable = await Task.Run(() => CheckColumnNullable("ff_user_invitations", "token_expiry"));
                
                // Check if app_code column exists in ff_events
                var eventsAppCodeExists = await Task.Run(() => CheckColumnExists("ff_events", "app_code"));
                
                var status = new
                {
                    EncryptedAccessTokenMigration = new
                    {
                        Name = "20250101000012_AddEncryptedAccessTokenField",
                        Status = columnExists ? "Completed" : "Pending",
                        ColumnExists = columnExists
                    },
                    TokenExpiryNullableMigration = new
                    {
                        Name = "20250101000013_MakeTokenExpiryNullable",
                        Status = tokenExpiryNullable ? "Completed" : "Pending",
                        IsNullable = tokenExpiryNullable
                    },
                    AddAppCodeToEventsMigration = new
                    {
                        Name = "20250101000014_AddAppCodeToEvents",
                        Status = eventsAppCodeExists ? "Completed" : "Pending",
                        ColumnExists = eventsAppCodeExists
                    }
                };

                return Ok(LocalSuccessResponse.Create(status));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get migration status");
                
                return BadRequest(ErrorResponse.Create("STATUS_CHECK_FAILED", $"Failed to get migration status: {ex.Message}"));
            }
        }

        /// <summary>
        /// Check if a column exists in a table
        /// </summary>
        private bool CheckColumnExists(string tableName, string columnName)
        {
            try
            {
                var sql = @"
                    SELECT COUNT(*) 
                    FROM information_schema.columns 
                    WHERE table_name = @tableName 
                    AND column_name = @columnName";
                
                var count = _db.Ado.GetInt(sql, new { tableName, columnName });
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not check if column {ColumnName} exists in table {TableName}", columnName, tableName);
                return false;
            }
        }

        /// <summary>
        /// Check if a column is nullable
        /// </summary>
        private bool CheckColumnNullable(string tableName, string columnName)
        {
            try
            {
                var sql = @"
                    SELECT is_nullable 
                    FROM information_schema.columns 
                    WHERE table_name = @tableName 
                    AND column_name = @columnName";
                
                var result = _db.Ado.GetString(sql, new { tableName, columnName });
                return result == "YES";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not check if column {ColumnName} is nullable in table {TableName}", columnName, tableName);
                return false;
            }
        }
    }
} 