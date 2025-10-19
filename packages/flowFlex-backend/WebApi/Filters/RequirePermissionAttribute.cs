using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Shared.Enums.Permission;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlowFlex.WebApi.Filters
{
    /// <summary>
    /// Permission verification attribute
    /// Function: Declarative permission control for API endpoints
    /// Usage: [RequirePermission(PermissionEntityTypeEnum.Workflow, OperationTypeEnum.View)]
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequirePermissionAttribute : Attribute, IAsyncActionFilter
    {
        private readonly PermissionEntityTypeEnum _entityType;
        private readonly OperationTypeEnum _operationType;
        private readonly string _entityIdParameterName;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="entityType">Entity type (Workflow/Stage/Case)</param>
        /// <param name="operationType">Operation type (Create/View/Operate/Delete/Assign)</param>
        /// <param name="entityIdParameterName">Entity ID parameter name in route/query (default: "id")</param>
        public RequirePermissionAttribute(
            PermissionEntityTypeEnum entityType,
            OperationTypeEnum operationType,
            string entityIdParameterName = "id")
        {
            _entityType = entityType;
            _operationType = operationType;
            _entityIdParameterName = entityIdParameterName;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RequirePermissionAttribute>>();
            var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            var userContext = context.HttpContext.RequestServices.GetRequiredService<UserContext>();

            try
            {
                // Step 1: Get user ID
                if (string.IsNullOrWhiteSpace(userContext?.UserId) || !long.TryParse(userContext.UserId, out var userId))
                {
                    logger.LogWarning("Invalid or missing user ID in UserContext");
                    context.Result = new ObjectResult(new
                    {
                        success = false,
                        errorCode = "UNAUTHORIZED",
                        message = "User not authenticated"
                    })
                    {
                        StatusCode = 401
                    };
                    return;
                }

                // Step 2: Get entity ID from route/query parameters
                if (!context.ActionArguments.TryGetValue(_entityIdParameterName, out var entityIdObj) ||
                    !long.TryParse(entityIdObj?.ToString(), out var entityId))
                {
                    logger.LogWarning(
                        "Entity ID parameter '{ParameterName}' not found or invalid",
                        _entityIdParameterName);
                    context.Result = new ObjectResult(new
                    {
                        success = false,
                        errorCode = "INVALID_REQUEST",
                        message = $"Entity ID parameter '{_entityIdParameterName}' is required"
                    })
                    {
                        StatusCode = 400
                    };
                    return;
                }

                // Step 3: Check permission based on entity type
                var permissionResult = _entityType switch
                {
                    PermissionEntityTypeEnum.Workflow => await permissionService.CheckWorkflowAccessAsync(
                        userId, entityId, _operationType),
                    PermissionEntityTypeEnum.Stage => await permissionService.CheckStageAccessAsync(
                        userId, entityId, _operationType),
                    PermissionEntityTypeEnum.Case => await permissionService.CheckCaseAccessAsync(
                        userId, entityId, _operationType),
                    _ => throw new ArgumentException($"Unsupported entity type: {_entityType}")
                };

                // Step 4: Handle permission result
                if (!permissionResult.Success)
                {
                    logger.LogWarning(
                        "Permission denied - UserId: {UserId}, EntityType: {EntityType}, EntityId: {EntityId}, Operation: {Operation}, Reason: {Reason}",
                        userId, _entityType, entityId, _operationType, permissionResult.ErrorMessage);

                    context.Result = new ObjectResult(new
                    {
                        success = false,
                        errorCode = permissionResult.ErrorCode ?? "ACCESS_DENIED",
                        message = permissionResult.ErrorMessage ?? "Access denied"
                    })
                    {
                        StatusCode = 403
                    };
                    return;
                }

                // Step 5: Permission granted, continue execution
                logger.LogInformation(
                    "Permission granted - UserId: {UserId}, EntityType: {EntityType}, EntityId: {EntityId}, Operation: {Operation}, Reason: {Reason}",
                    userId, _entityType, entityId, _operationType, permissionResult.GrantReason);

                // Store permission result in HttpContext for later use
                context.HttpContext.Items["PermissionResult"] = permissionResult;

                await next();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during permission check");
                context.Result = new ObjectResult(new
                {
                    success = false,
                    errorCode = "PERMISSION_CHECK_ERROR",
                    message = "Internal error during permission check"
                })
                {
                    StatusCode = 500
                };
            }
        }
    }
}

