using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.Permission;
using FlowFlex.Domain.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace FlowFlex.WebApi.Filters
{
    /// <summary>
    /// Permission verification filter attribute
    /// Function: Verify user permission before executing controller action
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
        /// <param name="operationType">Operation type (View/Operate/Delete)</param>
        /// <param name="entityIdParameterName">Entity ID parameter name in route (default: "id")</param>
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
                // Step 1: Get entity ID from route parameters
                if (!context.ActionArguments.TryGetValue(_entityIdParameterName, out var entityIdObj))
                {
                    logger.LogWarning("Entity ID parameter '{ParameterName}' not found in action arguments", _entityIdParameterName);
                    context.Result = new BadRequestObjectResult(new
                    {
                        success = false,
                        errorCode = "INVALID_PARAMETER",
                        message = $"Entity ID parameter '{_entityIdParameterName}' is required"
                    });
                    return;
                }

                if (!long.TryParse(entityIdObj?.ToString(), out var entityId) || entityId <= 0)
                {
                    logger.LogWarning("Invalid entity ID: {EntityId}", entityIdObj);
                    context.Result = new BadRequestObjectResult(new
                    {
                        success = false,
                        errorCode = "INVALID_ENTITY_ID",
                        message = "Invalid entity ID"
                    });
                    return;
                }

                // Step 2: Get current user ID
                var userIdString = userContext?.UserId;
                if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out var userId) || userId <= 0)
                {
                    logger.LogWarning("User not authenticated or invalid user ID");
                    context.Result = new UnauthorizedObjectResult(new
                    {
                        success = false,
                        errorCode = "UNAUTHORIZED",
                        message = "User not authenticated"
                    });
                    return;
                }

                // Step 3: Check permission based on entity type
                var permissionResult = _entityType switch
                {
                    PermissionEntityTypeEnum.Workflow => await permissionService.CheckWorkflowAccessAsync(userId, entityId, _operationType),
                    PermissionEntityTypeEnum.Stage => await permissionService.CheckStageAccessAsync(userId, entityId, _operationType),
                    PermissionEntityTypeEnum.Case => await permissionService.CheckCaseAccessAsync(userId, entityId, _operationType),
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
                        errorCode = permissionResult.ErrorCode,
                        message = permissionResult.ErrorMessage
                    })
                    {
                        StatusCode = 403 // Forbidden
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
