using FlowFlex.Application.Contracts.Dtos.OW.Permission;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.IServices.OW.Onboarding;
using FlowFlex.Application.Services.OW.Permission;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Const;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;
using PermissionOperationType = FlowFlex.Domain.Shared.Enums.Permission.OperationTypeEnum;

namespace FlowFlex.Application.Services.OW.OnboardingServices
{
    /// <summary>
    /// Service for onboarding permission operations
    /// Centralizes all permission-related logic for onboarding module
    /// </summary>
    public class OnboardingPermissionService : IOnboardingPermissionService
    {
        private readonly IPermissionService _permissionService;
        private readonly CasePermissionService _casePermissionService;
        private readonly UserContext _userContext;
        private readonly ILogger<OnboardingPermissionService> _logger;

        public OnboardingPermissionService(
            IPermissionService permissionService,
            CasePermissionService casePermissionService,
            UserContext userContext,
            ILogger<OnboardingPermissionService> logger)
        {
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _casePermissionService = casePermissionService ?? throw new ArgumentNullException(nameof(casePermissionService));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Permission Check Methods

        /// <inheritdoc />
        public async Task<bool> CheckCaseOperatePermissionAsync(long caseId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                _logger.LogDebug("CheckCaseOperatePermissionAsync - No valid user ID, returning false");
                return false;
            }

            // Fast path: Admin users have full access
            if (HasAdminPrivileges())
            {
                _logger.LogDebug("CheckCaseOperatePermissionAsync - User {UserId} has admin privileges, granting access", userId);
                return true;
            }

            // Fast path: Client Credentials token has full access
            if (IsClientCredentialsToken())
            {
                _logger.LogDebug("CheckCaseOperatePermissionAsync - Client Credentials token detected, granting access");
                return true;
            }

            var permissionResult = await _permissionService.CheckCaseAccessAsync(
                userId.Value,
                caseId,
                PermissionOperationType.Operate);

            return permissionResult.Success && permissionResult.CanOperate;
        }

        /// <inheritdoc />
        public async Task EnsureCaseOperatePermissionAsync(long caseId)
        {
            if (!await CheckCaseOperatePermissionAsync(caseId))
            {
                throw new CRMException(ErrorCodeEnum.OperationNotAllowed,
                    $"User does not have permission to operate on case {caseId}");
            }
        }

        /// <inheritdoc />
        public async Task<bool> CheckCaseViewPermissionAsync(long caseId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                _logger.LogDebug("CheckCaseViewPermissionAsync - No valid user ID, returning false");
                return false;
            }

            // Fast path: Admin users have full access
            if (HasAdminPrivileges())
            {
                _logger.LogDebug("CheckCaseViewPermissionAsync - User {UserId} has admin privileges, granting access", userId);
                return true;
            }

            // Fast path: Client Credentials token has full access
            if (IsClientCredentialsToken())
            {
                _logger.LogDebug("CheckCaseViewPermissionAsync - Client Credentials token detected, granting access");
                return true;
            }

            var permissionResult = await _permissionService.CheckCaseAccessAsync(
                userId.Value,
                caseId,
                PermissionOperationType.View);

            return permissionResult.Success && permissionResult.CanView;
        }

        /// <inheritdoc />
        public async Task EnsureCaseViewPermissionAsync(long caseId)
        {
            if (!await CheckCaseViewPermissionAsync(caseId))
            {
                throw new CRMException(ErrorCodeEnum.OperationNotAllowed,
                    $"User does not have permission to view case {caseId}");
            }
        }

        #endregion

        #region Permission Info Methods

        /// <inheritdoc />
        public async Task<PermissionInfoDto> GetCasePermissionInfoAsync(long caseId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return new PermissionInfoDto
                {
                    CanView = false,
                    CanOperate = false,
                    ErrorMessage = "User not authenticated"
                };
            }

            // Fast path: Admin users have full access
            if (HasAdminPrivileges())
            {
                return new PermissionInfoDto
                {
                    CanView = true,
                    CanOperate = true
                };
            }

            // Fast path: Client Credentials token has full access
            if (IsClientCredentialsToken())
            {
                return new PermissionInfoDto
                {
                    CanView = true,
                    CanOperate = true
                };
            }

            return await _permissionService.GetCasePermissionInfoAsync(userId.Value, caseId);
        }

        /// <inheritdoc />
        public async Task<PermissionInfoDto> GetStagePermissionInfoAsync(long stageId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return new PermissionInfoDto
                {
                    CanView = false,
                    CanOperate = false,
                    ErrorMessage = "User not authenticated"
                };
            }

            // Fast path: Admin users have full access
            if (HasAdminPrivileges())
            {
                return new PermissionInfoDto
                {
                    CanView = true,
                    CanOperate = true
                };
            }

            return await _permissionService.GetStagePermissionInfoAsync(userId.Value, stageId);
        }

        /// <inheritdoc />
        public async Task<Dictionary<long, PermissionInfoDto>> BatchCheckCasePermissionsAsync(
            List<Domain.Entities.OW.Onboarding> entities)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue || entities == null || !entities.Any())
            {
                return new Dictionary<long, PermissionInfoDto>();
            }

            // Fast path: Admin users have full access
            if (HasAdminPrivileges() || IsClientCredentialsToken())
            {
                return entities.ToDictionary(
                    e => e.Id,
                    e => new PermissionInfoDto { CanView = true, CanOperate = true });
            }

            // Pre-check module permissions once
            var canViewCases = await CanViewCasesAsync();
            var canOperateCases = await CanOperateCasesAsync();

            // Delegate to CasePermissionService for batch permission checking
            return await _casePermissionService.CheckBatchCasePermissionsAsync(
                entities,
                userId.Value,
                canViewCases,
                canOperateCases);
        }

        #endregion

        #region User Context Methods

        /// <inheritdoc />
        public long? GetCurrentUserId()
        {
            var userId = _userContext?.UserId;
            if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out var userIdLong))
            {
                return null;
            }
            return userIdLong;
        }

        /// <inheritdoc />
        public bool IsAuthenticated()
        {
            return GetCurrentUserId().HasValue;
        }

        /// <inheritdoc />
        public bool IsSystemAdmin()
        {
            return _userContext?.IsSystemAdmin == true;
        }

        /// <inheritdoc />
        public bool IsTenantAdmin()
        {
            return _userContext != null && _userContext.HasAdminPrivileges(_userContext.TenantId);
        }

        /// <inheritdoc />
        public bool HasAdminPrivileges()
        {
            return IsSystemAdmin() || IsTenantAdmin();
        }

        /// <inheritdoc />
        public List<string> GetUserTeamIds()
        {
            return _permissionService.GetUserTeamIds() ?? new List<string>();
        }

        /// <inheritdoc />
        public bool IsClientCredentialsToken()
        {
            return _userContext?.Schema == AuthSchemes.ItemIamClientIdentification;
        }

        #endregion

        #region Module Permission Methods

        /// <inheritdoc />
        public async Task<bool> CanViewCasesAsync()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return false;
            }

            // Fast path: Admin users have full access
            if (HasAdminPrivileges() || IsClientCredentialsToken())
            {
                return true;
            }

            return await _permissionService.CheckGroupPermissionAsync(userId.Value, PermissionConsts.Case.Read);
        }

        /// <inheritdoc />
        public async Task<bool> CanOperateCasesAsync()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return false;
            }

            // Fast path: Admin users have full access
            if (HasAdminPrivileges() || IsClientCredentialsToken())
            {
                return true;
            }

            return await _permissionService.CheckGroupPermissionAsync(userId.Value, PermissionConsts.Case.Update);
        }

        #endregion
    }
}
