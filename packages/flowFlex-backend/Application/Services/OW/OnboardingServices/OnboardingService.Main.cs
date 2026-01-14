using AutoMapper;
using FlowFlex.Application.Contracts.Dtos;
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Application.Contracts.Dtos.OW.Permission;
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Attr;
using FlowFlex.Domain.Shared.Const;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Events;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Utils;
using FlowFlex.Infrastructure.Extensions;
using FlowFlex.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using OfficeOpenXml;
using SqlSugar;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
// using Item.Redis; // Temporarily disable Redis
using System.Text.Json;
using PermissionOperationType = FlowFlex.Domain.Shared.Enums.Permission.OperationTypeEnum;
using FlowFlex.Application.Contracts.Dtos.OW.User;
using FlowFlex.Application.Contracts.Dtos.OW.StageCondition;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Onboarding service implementation - Main class with constructor, fields and constants
    /// </summary>
    public partial class OnboardingService : IOnboardingService
    {
        #region Fields

        internal readonly IOnboardingRepository _onboardingRepository;
        internal readonly IWorkflowRepository _workflowRepository;
        internal readonly IStageRepository _stageRepository;
        internal readonly IUserInvitationRepository _userInvitationRepository;

        internal readonly IMapper _mapper;
        internal readonly UserContext _userContext;
        internal readonly IMediator _mediator;
        internal readonly IStageService _stageService;
        internal readonly IChecklistTaskCompletionService _checklistTaskCompletionService;
        internal readonly IQuestionnaireAnswerService _questionnaireAnswerService;
        internal readonly IStaticFieldValueService _staticFieldValueService;
        internal readonly IChecklistService _checklistService;
        internal readonly IQuestionnaireService _questionnaireService;
        internal readonly IOperatorContextService _operatorContextService;
        internal readonly IServiceScopeFactory _serviceScopeFactory;
        internal readonly IBackgroundTaskQueue _backgroundTaskQueue;
        internal readonly IActionManagementService _actionManagementService;
        internal readonly IOperationChangeLogService _operationChangeLogService;
        internal readonly Application.Contracts.IServices.OW.ChangeLog.IOnboardingLogService _onboardingLogService;
        internal readonly IPermissionService _permissionService;
        internal readonly Permission.CasePermissionService _casePermissionService;
        internal readonly IHttpContextAccessor _httpContextAccessor;
        internal readonly IUserService _userService;
        internal readonly ICaseCodeGeneratorService _caseCodeGeneratorService;
        internal readonly IEmailService _emailService;
        internal readonly IRulesEngineService _rulesEngineService;
        internal readonly IConditionActionExecutor _conditionActionExecutor;
        internal readonly ILogger<OnboardingService> _logger;

        // Cache key constants - temporarily disable Redis cache
        internal const string WORKFLOW_CACHE_PREFIX = "ow:workflow";
        internal const string STAGE_CACHE_PREFIX = "ow:stage";
        internal const int CACHE_EXPIRY_MINUTES = 30; // Cache for 30 minutes

        // Initialization tracking
        internal static readonly HashSet<long> _initializingEntities = new HashSet<long>();
        internal static readonly object _initializationLock = new object();

        // Shared JSON serializer options for consistent serialization across the service
        internal static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        #endregion

        #region Constructor

        public OnboardingService(
            IOnboardingRepository onboardingRepository,
            IWorkflowRepository workflowRepository,
            IStageRepository stageRepository,
            IUserInvitationRepository userInvitationRepository,

            IMapper mapper,
            UserContext userContext,
            IMediator mediator,
            IStageService stageService,
            IChecklistTaskCompletionService checklistTaskCompletionService,
            IQuestionnaireAnswerService questionnaireAnswerService,
            IStaticFieldValueService staticFieldValueService,
            IChecklistService checklistService,
            IQuestionnaireService questionnaireService,
            IOperatorContextService operatorContextService,
            IServiceScopeFactory serviceScopeFactory,
            IBackgroundTaskQueue backgroundTaskQueue,
            IActionManagementService actionManagementService,
            IOperationChangeLogService operationChangeLogService,
            Application.Contracts.IServices.OW.ChangeLog.IOnboardingLogService onboardingLogService,
            IPermissionService permissionService,
            Permission.CasePermissionService casePermissionService,
            IHttpContextAccessor httpContextAccessor,
            IUserService userService,
            ICaseCodeGeneratorService caseCodeGeneratorService,
            IEmailService emailService,
            IRulesEngineService rulesEngineService,
            IConditionActionExecutor conditionActionExecutor,
            ILogger<OnboardingService> logger)
        {
            _onboardingRepository = onboardingRepository ?? throw new ArgumentNullException(nameof(onboardingRepository));
            _workflowRepository = workflowRepository ?? throw new ArgumentNullException(nameof(workflowRepository));
            _stageRepository = stageRepository ?? throw new ArgumentNullException(nameof(stageRepository));
            _userInvitationRepository = userInvitationRepository ?? throw new ArgumentNullException(nameof(userInvitationRepository));

            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _stageService = stageService ?? throw new ArgumentNullException(nameof(stageService));
            _checklistTaskCompletionService = checklistTaskCompletionService ?? throw new ArgumentNullException(nameof(checklistTaskCompletionService));
            _questionnaireAnswerService = questionnaireAnswerService ?? throw new ArgumentNullException(nameof(questionnaireAnswerService));
            _staticFieldValueService = staticFieldValueService ?? throw new ArgumentNullException(nameof(staticFieldValueService));
            _httpContextAccessor = httpContextAccessor;
            _checklistService = checklistService ?? throw new ArgumentNullException(nameof(checklistService));
            _questionnaireService = questionnaireService ?? throw new ArgumentNullException(nameof(questionnaireService));
            _operatorContextService = operatorContextService ?? throw new ArgumentNullException(nameof(operatorContextService));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _backgroundTaskQueue = backgroundTaskQueue ?? throw new ArgumentNullException(nameof(backgroundTaskQueue));
            _actionManagementService = actionManagementService ?? throw new ArgumentNullException(nameof(actionManagementService));
            _operationChangeLogService = operationChangeLogService ?? throw new ArgumentNullException(nameof(operationChangeLogService));
            _onboardingLogService = onboardingLogService ?? throw new ArgumentNullException(nameof(onboardingLogService));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _casePermissionService = casePermissionService ?? throw new ArgumentNullException(nameof(casePermissionService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _caseCodeGeneratorService = caseCodeGeneratorService ?? throw new ArgumentNullException(nameof(caseCodeGeneratorService));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _rulesEngineService = rulesEngineService ?? throw new ArgumentNullException(nameof(rulesEngineService));
            _conditionActionExecutor = conditionActionExecutor ?? throw new ArgumentNullException(nameof(conditionActionExecutor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion
    }
}

