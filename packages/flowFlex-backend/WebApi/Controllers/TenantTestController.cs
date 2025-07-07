using Microsoft.AspNetCore.Mvc;
using FlowFlex.Application.Contracts.Dtos.OW.Workflow;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.WebApi.Controllers
{
    /// <summary>
    /// 租户隔离测试控制�?
    /// </summary>
    [ApiController]
    [Route("api/tenant-test/v{version:apiVersion}")]
    [ApiVersion("1.0")]
    public class TenantTestController : BaseController
    {
        private readonly IWorkflowService _workflowService;
        private readonly IStageService _stageService;
        private readonly UserContext _userContext;

        public TenantTestController(
            IWorkflowService workflowService,
            IStageService stageService,
            UserContext userContext,
            IUserContextService userContextService) : base(userContextService)
        {
            _workflowService = workflowService;
            _stageService = stageService;
            _userContext = userContext;
        }

        /// <summary>
        /// 获取当前租户信息
        /// </summary>
        [HttpGet("tenant-info")]
        public async Task<IActionResult> GetTenantInfo()
        {
            var tenantInfo = new
            {
                TenantId = _userContext?.TenantId ?? "NULL",
                UserId = _userContext?.UserId ?? "NULL",
                UserName = _userContext?.UserName ?? "NULL",
                RequestHeaders = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                Timestamp = DateTimeOffset.Now
            };

            return Success(tenantInfo);
        }

        /// <summary>
        /// 获取当前租户的工作流列表（测试租户隔离）
        /// </summary>
        [HttpGet("workflows")]
        public async Task<IActionResult> GetWorkflows()
        {
            try
            {
                var workflows = await _workflowService.GetAllAsync();
                
                var result = new
                {
                    TenantId = _userContext?.TenantId ?? "NULL",
                    WorkflowCount = workflows.Count,
                    Workflows = workflows.Select(w => new
                    {
                        w.Id,
                        w.Name,
                        w.TenantId,
                        w.CreateDate,
                        w.IsActive
                    }).ToList()
                };

                return Success(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取当前租户的阶段列表（测试租户隔离�?
        /// </summary>
        [HttpGet("stages")]
        public async Task<IActionResult> GetStages()
        {
            try
            {
                var stages = await _stageService.GetAllAsync();
                
                var result = new
                {
                    TenantId = _userContext?.TenantId ?? "NULL",
                    StageCount = stages.Count,
                    Stages = stages.Select(s => new
                    {
                        s.Id,
                        s.Name,
                        s.TenantId,
                        s.WorkflowId,
                        s.CreateDate,
                        s.IsActive
                    }).ToList()
                };

                return Success(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建测试工作流（验证租户ID是否正确设置�?
        /// </summary>
        [HttpPost("test-workflow")]
        public async Task<IActionResult> CreateTestWorkflow([FromBody] CreateTestWorkflowRequest request)
        {
            try
            {
                var input = new WorkflowInputDto
                {
                    Name = request.Name ?? $"Test Workflow - {_userContext?.TenantId} - {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}",
                    Description = request.Description ?? $"Test workflow for tenant {_userContext?.TenantId}",
                    StartDate = DateTimeOffset.Now,
                    IsActive = true,
                    IsDefault = false
                };

                var workflowId = await _workflowService.CreateAsync(input);
                
                var result = new
                {
                    WorkflowId = workflowId,
                    TenantId = _userContext?.TenantId,
                    Message = "Test workflow created successfully"
                };

                return Success(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating test workflow: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理测试数据（删除当前租户的测试工作流）
        /// </summary>
        [HttpDelete("cleanup")]
        public async Task<IActionResult> CleanupTestData()
        {
            try
            {
                var workflows = await _workflowService.GetAllAsync();
                var testWorkflows = workflows.Where(w => w.Name.Contains("Test Workflow")).ToList();
                
                int deletedCount = 0;
                foreach (var workflow in testWorkflows)
                {
                    var success = await _workflowService.DeleteAsync(workflow.Id, confirm: true);
                    if (success)
                    {
                        deletedCount++;
                    }
                }
                
                var result = new
                {
                    TenantId = _userContext?.TenantId,
                    DeletedCount = deletedCount,
                    Message = $"Cleaned up {deletedCount} test workflows"
                };

                return Success(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error cleaning up test data: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 创建测试工作流请�?
    /// </summary>
    public class CreateTestWorkflowRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
} 
