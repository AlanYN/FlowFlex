using FlowFlex.Application.Contracts.Dtos.OW.Permission;
using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.IServices.OW.Onboarding;
using FlowFlex.Application.Helpers;
using FlowFlex.Application.Helpers.OW;
using FlowFlex.Application.Services.OW.Permission;
using FlowFlex.Application.Services.Shared;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Const;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using PermissionOperationType = FlowFlex.Domain.Shared.Enums.Permission.OperationTypeEnum;

namespace FlowFlex.Application.Services.OW.OnboardingServices
{
    /// <summary>
    /// Service for onboarding permission operations
    /// Centralizes all permission-related logic for onboarding module
    /// </summary>
    public class OnboardingPermissionService : IOnboardingPermissionService, IScopedService
    {
        private readonly IPermissionService _permissionService;
        private readonly CasePermissionService _casePermissionService;
        private readonly UserContext _userContext;
        private readonly ILogger<OnboardingPermissionService> _logger;
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IWorkflowRepository _workflowRepository;

        public OnboardingPermissionService(
            IPermissionService permissionService,
            CasePermissionService casePermissionService,
            UserContext userContext,
            ILogger<OnboardingPermissionService> logger,
            IOnboardingRepository onboardingRepository,
            IWorkflowRepository workflowRepository)
        {
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _casePermissionService = casePermissionService ?? throw new ArgumentNullException(nameof(casePermissionService));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _onboardingRepository = onboardingRepository ?? throw new ArgumentNullException(nameof(onboardingRepository));
            _workflowRepository = workflowRepository ?? throw new ArgumentNullException(nameof(workflowRepository));
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

        #region Snapshot Methods

        /// <inheritdoc />
        public async Task<bool> ReapplyWorkflowPermissionAsync(long onboardingId)
        {
            var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
            if (onboarding == null)
                throw new CRMException(ErrorCodeEnum.DataNotFound, $"Case {onboardingId} not found.");

            var workflow = await _workflowRepository.GetByIdAsync(onboarding.WorkflowId);
            if (workflow == null)
                throw new CRMException(ErrorCodeEnum.DataNotFound,
                    $"Workflow {onboarding.WorkflowId} not found for Case {onboardingId}.");

            var wfRuntime = PermissionCalculator.ComputeWorkflowEffectiveRuntime(workflow);

            var maxViewTeamsJson = JsonSerializer.Serialize(wfRuntime.ViewTeams);
            var maxOperateTeamsJson = JsonSerializer.Serialize(wfRuntime.OperateTeams);

            _logger.LogInformation(
                "ReapplyWorkflowPermissionAsync - Updating snapshot for Case {OnboardingId}: ViewMode={ViewMode}, ViewTeams={ViewTeams}",
                onboardingId, wfRuntime.ViewMode, maxViewTeamsJson);

            // Only update the max_* snapshot fields — do NOT touch StagesProgressJson or other fields
            var db = _onboardingRepository.GetSqlSugarClient();
            var sql = @"UPDATE ff_onboarding 
                        SET max_view_permission_mode = @MaxViewPermissionMode,
                            max_view_teams = @MaxViewTeams::jsonb,
                            max_operate_teams = @MaxOperateTeams::jsonb,
                            modify_date = @ModifyDate
                        WHERE id = @Id";

            var result = await db.Ado.ExecuteCommandAsync(sql, new
            {
                MaxViewPermissionMode = (int)wfRuntime.ViewMode,
                MaxViewTeams = maxViewTeamsJson,
                MaxOperateTeams = maxOperateTeamsJson,
                ModifyDate = DateTimeOffset.UtcNow,
                Id = onboardingId
            });

            return result > 0;
        }

        #endregion

        #region Case Stage Permission Methods

        /// <inheritdoc />
        public async Task<bool> UpdateStagePermissionAsync(long onboardingId, long stageId, CaseStagePermissionInputDto input)
        {
            // Load the onboarding entity
            var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
            if (onboarding == null)
                throw new CRMException(ErrorCodeEnum.DataNotFound, $"Case {onboardingId} not found.");

            // Parse current stage progress JSON
            var stagesProgress = StagesProgressHelper.ParseStagesProgress(
                onboarding.StagesProgressJson,
                _logger,
                $"OnboardingId={onboardingId}");

            // Find the target stage progress entry
            var stageProgress = stagesProgress.FirstOrDefault(sp => sp.StageId == stageId);
            if (stageProgress == null)
                throw new CRMException(ErrorCodeEnum.DataNotFound,
                    $"Stage {stageId} not found in Case {onboardingId}.");

            // --- Validation ---

            if (!input.InheritFromWorkflowStage)
            {
                // Requirement 13.3: VisibleTo with no Teams/Users → validation error
                if (input.ViewPermissionMode == ViewPermissionModeEnum.VisibleToTeams
                    && (input.ViewTeams == null || input.ViewTeams.Count == 0)
                    && (input.ViewUsers == null || input.ViewUsers.Count == 0))
                {
                    throw new CRMException(ErrorCodeEnum.ParamInvalid,
                        "ViewTeams or ViewUsers must not be empty when ViewPermissionMode is VisibleToTeams.");
                }

                // Requirement 13.4: ViewTeams ⊄ MaxStageViewTeams → boundary exceeded
                if (input.ViewTeams != null && input.ViewTeams.Count > 0)
                {
                    if (!PermissionCalculator.IsSubsetOf(input.ViewTeams, stageProgress.MaxStageViewTeams))
                    {
                        throw new CRMException(ErrorCodeEnum.PermissionBoundaryExceeded,
                            "Selected view teams exceed the Stage snapshot boundary. Stage view teams must be a subset of MaxStageViewTeams.");
                    }
                }
            }

            // --- Apply the permission configuration ---

            if (input.InheritFromWorkflowStage)
            {
                // Requirement 13.2: Inherit mode — clear all independent configuration fields
                stageProgress.StagePermissionInheritFromWorkflowStage = true;
                stageProgress.StageViewPermissionMode = null;
                stageProgress.StageViewPermissionSubjectType = PermissionSubjectTypeEnum.Team;
                stageProgress.StageViewTeams = null;
                stageProgress.StageViewUsers = null;
                stageProgress.StageUseSameTeamForOperate = true;
                stageProgress.StageOperatePermissionSubjectType = PermissionSubjectTypeEnum.Team;
                stageProgress.StageOperateTeams = null;
                stageProgress.StageOperateUsers = null;
                stageProgress.StageRollBackInherit = true;
                stageProgress.StageRollBackUseSameAsOperate = true;
                stageProgress.StageRollBackPermissionSubjectType = PermissionSubjectTypeEnum.Team;
                stageProgress.StageRollBackTeams = null;
                stageProgress.StageRollBackUsers = null;
            }
            else
            {
                // Independent configuration
                stageProgress.StagePermissionInheritFromWorkflowStage = false;

                // View
                stageProgress.StageViewPermissionMode = input.ViewPermissionMode;
                stageProgress.StageViewPermissionSubjectType = input.ViewPermissionSubjectType;
                stageProgress.StageViewTeams = input.ViewTeams;
                stageProgress.StageViewUsers = input.ViewUsers;

                // Operate
                stageProgress.StageUseSameTeamForOperate = input.UseSameTeamForOperate;
                stageProgress.StageOperatePermissionSubjectType = input.OperatePermissionSubjectType;
                stageProgress.StageOperateTeams = input.OperateTeams;
                stageProgress.StageOperateUsers = input.OperateUsers;

                // Roll Back
                stageProgress.StageRollBackInherit = input.RollBackInherit;
                stageProgress.StageRollBackUseSameAsOperate = input.RollBackUseSameAsOperate;
                stageProgress.StageRollBackPermissionSubjectType = input.RollBackPermissionSubjectType;
                stageProgress.StageRollBackTeams = input.RollBackTeams;
                stageProgress.StageRollBackUsers = input.RollBackUsers;
            }

            // Serialize updated progress back to JSON
            var updatedJson = JsonSerializer.Serialize(stagesProgress, OnboardingSharedUtilities.JsonOptions);

            _logger.LogInformation(
                "UpdateStagePermissionAsync - Updating stage permission for Case {OnboardingId}, Stage {StageId}: Inherit={Inherit}",
                onboardingId, stageId, input.InheritFromWorkflowStage);

            // Persist only the stages_progress_json column
            var db = _onboardingRepository.GetSqlSugarClient();
            var sql = "UPDATE ff_onboarding SET stages_progress_json = @StagesProgressJson::jsonb, modify_date = @ModifyDate WHERE id = @Id";
            var rowsAffected = await db.Ado.ExecuteCommandAsync(sql, new
            {
                StagesProgressJson = updatedJson,
                ModifyDate = DateTimeOffset.UtcNow,
                Id = onboardingId
            });

            return rowsAffected > 0;
        }

        #endregion
    }
}
