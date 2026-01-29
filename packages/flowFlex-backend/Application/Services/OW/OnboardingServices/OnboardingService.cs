using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Application.Contracts.Dtos.OW.User;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.IServices.OW.Onboarding;
using FlowFlex.Domain.Shared.Models;
using System.Text.Json;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Onboarding service implementation - Facade that delegates to specialized sub-services
    /// </summary>
    public class OnboardingService : IOnboardingService
    {
        #region Fields

        private readonly IOnboardingStageProgressService _stageProgressService;
        private readonly IOnboardingQueryService _queryService;
        private readonly IOnboardingStatusService _statusService;
        private readonly IOnboardingStageManagementService _stageManagementService;
        private readonly IOnboardingCrudService _crudService;
        private readonly IOnboardingHelperService _helperService;
        private readonly IOnboardingUserManagementService _userManagementService;

        internal static readonly JsonSerializerOptions JsonOptions = Helpers.OW.OnboardingSharedUtilities.JsonOptions;

        #endregion

        #region Constructor

        public OnboardingService(
            IOnboardingStageProgressService stageProgressService,
            IOnboardingQueryService queryService,
            IOnboardingStatusService statusService,
            IOnboardingStageManagementService stageManagementService,
            IOnboardingCrudService crudService,
            IOnboardingHelperService helperService,
            IOnboardingUserManagementService userManagementService)
        {
            _stageProgressService = stageProgressService ?? throw new ArgumentNullException(nameof(stageProgressService));
            _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
            _statusService = statusService ?? throw new ArgumentNullException(nameof(statusService));
            _stageManagementService = stageManagementService ?? throw new ArgumentNullException(nameof(stageManagementService));
            _crudService = crudService ?? throw new ArgumentNullException(nameof(crudService));
            _helperService = helperService ?? throw new ArgumentNullException(nameof(helperService));
            _userManagementService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));
        }

        #endregion

        #region CRUD Operations

        public Task<long> CreateAsync(OnboardingInputDto input)
            => _crudService.CreateAsync(input);

        public Task<bool> UpdateAsync(long id, OnboardingInputDto input)
            => _crudService.UpdateAsync(id, input);

        public Task<bool> DeleteAsync(long id, bool confirm = false)
            => _crudService.DeleteAsync(id, confirm);

        public Task<OnboardingOutputDto?> GetByIdAsync(long id)
            => _crudService.GetByIdAsync(id);

        #endregion

        #region Query Operations

        public Task<PageModelDto<OnboardingOutputDto>> QueryAsync(OnboardingQueryRequest request)
            => _queryService.QueryAsync(request);

        public Task<PagedResult<OnboardingOutputDto>> GetActiveBySystemIdAsync(
            string systemId,
            string? entityId = null,
            string sortField = "createDate",
            string sortOrder = "desc",
            int pageIndex = 1,
            int pageSize = 20)
            => _queryService.GetActiveBySystemIdAsync(systemId, entityId, sortField, sortOrder, pageIndex, pageSize);

        public Task<Stream> ExportToExcelAsync(OnboardingQueryRequest query)
            => _queryService.ExportToExcelAsync(query);

        public Task<OnboardingProgressDto> GetProgressAsync(long id)
            => _queryService.GetProgressAsync(id);

        #endregion

        #region Status Operations

        public Task<bool> StartOnboardingAsync(long id, StartOnboardingInputDto input)
            => _statusService.StartOnboardingAsync(id, input);

        public Task<bool> PauseAsync(long id)
            => _statusService.PauseAsync(id);

        public Task<bool> ResumeAsync(long id)
            => _statusService.ResumeAsync(id);

        public Task<bool> ResumeWithConfirmationAsync(long id, ResumeOnboardingInputDto input)
            => _statusService.ResumeWithConfirmationAsync(id, input);

        public Task<bool> CancelAsync(long id, string reason)
            => _statusService.CancelAsync(id, reason);

        public Task<bool> AbortAsync(long id, AbortOnboardingInputDto input)
            => _statusService.AbortAsync(id, input);

        public Task<bool> ReactivateAsync(long id, ReactivateOnboardingInputDto input)
            => _statusService.ReactivateAsync(id, input);

        public Task<bool> RejectAsync(long id, RejectOnboardingInputDto input)
            => _statusService.RejectAsync(id, input);

        public Task<bool> ForceCompleteAsync(long id, ForceCompleteOnboardingInputDto input)
            => _statusService.ForceCompleteAsync(id, input);

        public Task<bool> AssignAsync(long id, AssignOnboardingInputDto input)
            => _statusService.AssignAsync(id, input);

        public Task<bool> UpdateCompletionRateAsync(long id)
            => _statusService.UpdateCompletionRateAsync(id);

        #endregion

        #region Stage Management Operations

        public Task<bool> MoveToNextStageAsync(long id)
            => _stageManagementService.MoveToNextStageAsync(id);

        public Task<bool> MoveToStageAsync(long id, MoveToStageInputDto input)
            => _stageManagementService.MoveToStageAsync(id, input);

        public Task<bool> CompleteCurrentStageAsync(long id, CompleteCurrentStageInputDto input)
            => _stageManagementService.CompleteCurrentStageAsync(id, input);

        public Task<bool> CompleteCurrentStageInternalAsync(long id, CompleteCurrentStageInputDto input)
            => _stageManagementService.CompleteCurrentStageInternalAsync(id, input);

        #endregion

        #region Stage Progress Operations

        public Task<bool> UpdateOnboardingStageAISummaryAsync(long onboardingId, long stageId, string aiSummary, DateTime generatedAt, double? confidence, string modelUsed)
            => _stageProgressService.UpdateOnboardingStageAISummaryAsync(onboardingId, stageId, aiSummary, generatedAt, confidence, modelUsed);

        public Task<bool> UpdateStageCustomFieldsAsync(long onboardingId, UpdateStageCustomFieldsInputDto input)
            => _stageProgressService.UpdateStageCustomFieldsAsync(onboardingId, input);

        public Task<bool> SaveStageAsync(long onboardingId, long stageId)
            => _stageProgressService.SaveStageAsync(onboardingId, stageId);

        #endregion

        #region User Management Operations

        public Task<List<UserTreeNodeDto>> GetAuthorizedUsersAsync(long id)
            => _userManagementService.GetAuthorizedUsersAsync(id);

        #endregion
    }
}
