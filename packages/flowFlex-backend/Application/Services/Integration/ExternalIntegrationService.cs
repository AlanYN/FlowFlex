using FlowFlex.Application.Contracts.Dtos.Integration;
using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Application.Contracts.Dtos.OW.StaticField;
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Application.Contracts.IServices.Integration;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.Options;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Repository.Action;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.Action;
using FlowFlex.Domain.Shared.Exceptions;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Domain.Shared.Enums;

namespace FlowFlex.Application.Services.Integration
{
    /// <summary>
    /// External integration service implementation for CRM/ERP system integration
    /// </summary>
    public class ExternalIntegrationService : IExternalIntegrationService, IScopedService
    {
        private readonly IIntegrationRepository _integrationRepository;
        private readonly IEntityMappingRepository _entityMappingRepository;
        private readonly IWorkflowRepository _workflowRepository;
        private readonly IStageRepository _stageRepository;
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IOnboardingService _onboardingService;
        private readonly IOnboardingFileRepository _onboardingFileRepository;
        private readonly IOnboardingFileService _onboardingFileService;
        private readonly IActionDefinitionRepository _actionDefinitionRepository;
        private readonly IActionExecutionService _actionExecutionService;
        private readonly IInboundFieldMappingRepository _fieldMappingRepository;
        private readonly IStaticFieldValueService _staticFieldValueService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IdentityHubOptions _idmOptions;
        private readonly UserContext _userContext;
        private readonly ILogger<ExternalIntegrationService> _logger;

        // AES encryption key (should match IntegrationService)
        private const string ENCRYPTION_KEY = "FlowFlex2024IntegrationKey123456"; // 32 bytes for AES-256

        // Field name to property mapping for Onboarding entity
        private static readonly Dictionary<string, string> FieldToPropertyMapping = new(StringComparer.OrdinalIgnoreCase)
        {
            // Customer/Lead fields
            // Note: CUSTOMERNAME is intentionally NOT mapped to CaseName to avoid overwriting case name
            { "CASENAME", "CaseName" },
            { "LEADNAME", "CaseName" },
            { "CONTACTPERSON", "ContactPerson" },
            { "CONTACTNAME", "ContactPerson" },
            { "CONTACTEMAIL", "ContactEmail" },
            { "CONTACTPHONE", "LeadPhone" },
            { "LEADPHONE", "LeadPhone" },
            { "PHONE", "LeadPhone" },
            { "LEADEMAIL", "LeadEmail" },
            { "EMAIL", "LeadEmail" },
            { "LEADID", "LeadId" },
            // Assignee fields
            { "ASSIGNEE", "CurrentAssigneeName" },
            { "CURRENTASSIGNEENAME", "CurrentAssigneeName" },
            { "ASSIGNEEID", "CurrentAssigneeId" },
            { "CURRENTASSIGNEEID", "CurrentAssigneeId" },
            // Team fields
            { "TEAM", "CurrentTeam" },
            { "CURRENTTEAM", "CurrentTeam" },
            // Ownership fields
            { "OWNERSHIP", "Ownership" },
            { "OWNER", "Ownership" },
            { "OWNERSHIPNAME", "OwnershipName" },
            { "OWNERNAME", "OwnershipName" },
            { "OWNERSHIPEMAIL", "OwnershipEmail" },
            { "OWNEREMAIL", "OwnershipEmail" },
            // Status and priority
            { "PRIORITY", "Priority" },
            { "STATUS", "Status" },
            // Lifecycle
            { "LIFECYCLESTAGENAME", "LifeCycleStageName" },
            { "LIFECYCLESTAGEID", "LifeCycleStageId" },
            // Notes
            { "NOTES", "Notes" },
            { "DESCRIPTION", "Notes" }
        };

        public ExternalIntegrationService(
            IIntegrationRepository integrationRepository,
            IEntityMappingRepository entityMappingRepository,
            IWorkflowRepository workflowRepository,
            IStageRepository stageRepository,
            IOnboardingRepository onboardingRepository,
            IOnboardingService onboardingService,
            IOnboardingFileRepository onboardingFileRepository,
            IOnboardingFileService onboardingFileService,
            IActionDefinitionRepository actionDefinitionRepository,
            IActionExecutionService actionExecutionService,
            IInboundFieldMappingRepository fieldMappingRepository,
            IStaticFieldValueService staticFieldValueService,
            IHttpClientFactory httpClientFactory,
            IOptions<IdentityHubOptions> idmOptions,
            UserContext userContext,
            ILogger<ExternalIntegrationService> logger)
        {
            _integrationRepository = integrationRepository;
            _entityMappingRepository = entityMappingRepository;
            _workflowRepository = workflowRepository;
            _stageRepository = stageRepository;
            _onboardingRepository = onboardingRepository;
            _onboardingService = onboardingService;
            _onboardingFileRepository = onboardingFileRepository;
            _onboardingFileService = onboardingFileService;
            _actionDefinitionRepository = actionDefinitionRepository;
            _actionExecutionService = actionExecutionService;
            _fieldMappingRepository = fieldMappingRepository;
            _staticFieldValueService = staticFieldValueService;
            _httpClientFactory = httpClientFactory;
            _idmOptions = idmOptions.Value;
            _userContext = userContext;
            _logger = logger;
        }

        /// <summary>
        /// Get entity type mappings by Integration System Name
        /// </summary>
        public async Task<EntityTypeMappingResponse> GetEntityTypeMappingsBySystemNameAsync(string systemName)
        {
            _logger.LogInformation("Getting entity type mappings for System Name: {SystemName}", systemName);

            if (string.IsNullOrWhiteSpace(systemName))
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "System Name is required");
            }

            // Get integration by system name
            var integration = await _integrationRepository.GetBySystemNameAsync(systemName);

            if (integration == null)
            {
                _logger.LogWarning("No integration found for System Name: {SystemName}", systemName);
                throw new CRMException(ErrorCodeEnum.NotFound, $"Integration not found for System Name '{systemName}'");
            }

            // Get entity mappings for this integration
            var entityMappings = await _entityMappingRepository.GetByIntegrationIdAsync(integration.Id);

            // Build response
            var response = new EntityTypeMappingResponse
            {
                IntegrationId = integration.Id,
                IntegrationName = integration.Name,
                SystemName = integration.SystemName,
                EntityTypeMappings = entityMappings
                    .Where(em => em.IsActive && em.IsValid)
                    .Select(em => new EntityTypeMappingItemDto
                    {
                        Id = em.Id,
                        SystemId = em.SystemId ?? string.Empty,
                        ExternalEntityName = em.ExternalEntityName,
                        ExternalEntityType = em.ExternalEntityType,
                        WfeEntityType = em.WfeEntityType,
                        WorkflowIds = JsonConvert.DeserializeObject<List<long>>(em.WorkflowIds ?? "[]") ?? new List<long>(),
                        IsActive = em.IsActive
                    })
                    .ToList()
            };

            _logger.LogInformation("Found {Count} entity type mappings for System Name: {SystemName}",
                response.EntityTypeMappings.Count, systemName);

            return response;
        }

        /// <summary>
        /// Get workflows available for a specific entity mapping by System ID
        /// </summary>
        public async Task<List<WorkflowInfoDto>> GetWorkflowsBySystemIdAsync(string systemId)
        {
            _logger.LogInformation("Getting workflows for System ID: {SystemId}", systemId);

            if (string.IsNullOrWhiteSpace(systemId))
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "System ID is required");
            }

            // Get entity mapping by System ID
            var entityMapping = await _entityMappingRepository.GetBySystemIdAsync(systemId);

            if (entityMapping == null)
            {
                _logger.LogWarning("No entity mapping found for System ID: {SystemId}", systemId);
                throw new CRMException(ErrorCodeEnum.NotFound, $"Entity mapping not found for System ID '{systemId}'");
            }

            // Parse workflow IDs from entity mapping
            var workflowIds = JsonConvert.DeserializeObject<List<long>>(entityMapping.WorkflowIds ?? "[]") ?? new List<long>();

            if (!workflowIds.Any())
            {
                _logger.LogInformation("No workflows configured for System ID: {SystemId}", systemId);
                return new List<WorkflowInfoDto>();
            }

            // Get workflow details
            var workflows = new List<WorkflowInfoDto>();
            foreach (var workflowId in workflowIds)
            {
                var workflow = await _workflowRepository.GetByIdAsync(workflowId);
                if (workflow != null && workflow.IsValid)
                {
                    workflows.Add(new WorkflowInfoDto
                    {
                        Id = workflow.Id,
                        Name = workflow.Name,
                        Description = workflow.Description,
                        IsDefault = workflow.IsDefault
                    });
                }
            }

            _logger.LogInformation("Found {Count} workflows for System ID: {SystemId}",
                workflows.Count, systemId);

            return workflows;
        }

        /// <summary>
        /// Create a case/onboarding from external system
        /// Always creates a new case, subsequent field updates will be applied to this new case
        /// </summary>
        public async Task<CreateCaseFromExternalResponse> CreateCaseAsync(CreateCaseFromExternalRequest request)
        {
            _logger.LogInformation("Creating case from external system: SystemId={SystemId}, WorkflowId={WorkflowId}, EntityType={EntityType}, EntityId={EntityId}",
                request.SystemId, request.WorkflowId, request.EntityType, request.EntityId);

            if (string.IsNullOrWhiteSpace(request.SystemId))
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "System ID is required");
            }

            if (string.IsNullOrWhiteSpace(request.EntityType))
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Entity Type is required");
            }

            if (string.IsNullOrWhiteSpace(request.EntityId))
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Entity ID is required");
            }

            if (string.IsNullOrWhiteSpace(request.CaseName))
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Case Name is required");
            }

            if (string.IsNullOrWhiteSpace(request.ContactName))
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Contact Name is required");
            }

            if (string.IsNullOrWhiteSpace(request.ContactEmail))
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Contact Email is required");
            }

            // Get entity mapping by System ID
            var entityMapping = await _entityMappingRepository.GetBySystemIdAsync(request.SystemId);
            if (entityMapping == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound,
                    $"Entity mapping not found for System ID '{request.SystemId}'");
            }

            // Verify workflow is allowed for this entity mapping
            var workflowIds = JsonConvert.DeserializeObject<List<long>>(entityMapping.WorkflowIds ?? "[]") ?? new List<long>();
            if (!workflowIds.Contains(request.WorkflowId))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    $"Workflow {request.WorkflowId} is not configured for this entity mapping");
            }

            // Verify workflow exists
            var workflow = await _workflowRepository.GetWithStagesAsync(request.WorkflowId);
            if (workflow == null || !workflow.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Workflow not found");
            }

            // Get first stage
            var firstStage = workflow.Stages?.OrderBy(s => s.Order).FirstOrDefault();
            if (firstStage == null)
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Workflow has no stages configured");
            }

            long caseId;
            Domain.Entities.OW.Onboarding? targetCase;

            // Always create a new case - do not check for existing cases with the same EntityId
            var onboardingInput = new OnboardingInputDto
            {
                WorkflowId = request.WorkflowId,
                LeadId = request.EntityId,
                CaseName = request.CaseName,
                ContactPerson = request.ContactName,
                ContactEmail = request.ContactEmail,
                LeadPhone = request.ContactPhone,
                Status = "Started",
                StartDate = DateTimeOffset.UtcNow,
                Priority = "Medium",
                IsActive = true,
                CustomFieldsJson = request.CustomFields != null
                    ? JsonConvert.SerializeObject(request.CustomFields)
                    : null
            };

            caseId = await _onboardingService.CreateAsync(onboardingInput);
            targetCase = await _onboardingRepository.GetByIdAsync(caseId);

            // Update SystemId, IntegrationId, EntityType and EntityId for new case
            if (targetCase != null)
            {
                targetCase.SystemId = request.SystemId;
                targetCase.IntegrationId = entityMapping.IntegrationId;
                targetCase.EntityType = request.EntityType;
                targetCase.EntityId = request.EntityId;
                
                // Ensure CurrentStageId is set to first stage if not already set
                if (!targetCase.CurrentStageId.HasValue)
                {
                    targetCase.CurrentStageId = firstStage.Id;
                    targetCase.CurrentStageOrder = firstStage.Order;
                    _logger.LogInformation("Set CurrentStageId={StageId} for case {CaseId}", firstStage.Id, caseId);
                }
                
                targetCase.InitModifyInfo(_userContext);
                await _onboardingRepository.UpdateAsync(targetCase);
                
                _logger.LogInformation("Updated SystemId={SystemId}, IntegrationId={IntegrationId}, EntityType={EntityType}, EntityId={EntityId}, CurrentStageId={CurrentStageId} for case {CaseId}",
                    request.SystemId, entityMapping.IntegrationId, request.EntityType, request.EntityId, targetCase.CurrentStageId, caseId);
            }

            _logger.LogInformation("Successfully created new case {CaseId} from external system", caseId);

            // Execute CaseInfo Actions - find actions with field mappings configured
            if (targetCase != null)
            {
                await ExecuteCaseInfoActionsAndPopulateFieldsAsync(
                    entityMapping, 
                    targetCase, 
                    request.EntityId);
            }

            return new CreateCaseFromExternalResponse
            {
                CaseId = caseId,
                CaseCode = targetCase?.CaseCode ?? "",
                WorkflowId = request.WorkflowId,
                WorkflowName = workflow.Name,
                CurrentStageId = targetCase?.CurrentStageId ?? firstStage.Id,
                CurrentStageName = firstStage.Name ?? "",
                Status = targetCase?.Status ?? "Started",
                CreatedAt = DateTimeOffset.UtcNow
            };
        }

        /// <summary>
        /// Get case information by parameters (demo endpoint)
        /// </summary>
        public async Task<CaseInfoResponse> GetCaseInfoAsync(CaseInfoRequest request)
        {
            _logger.LogInformation("Getting case info: LeadId={LeadId}, CustomerName={CustomerName}",
                request.LeadId, request.CustomerName);

            var response = new CaseInfoResponse
            {
                LeadId = request.LeadId,
                CustomerName = request.CustomerName,
                ContactName = request.ContactName,
                ContactEmail = request.ContactEmail,
                ContactPhone = request.ContactPhone
            };

            // Try to find case by LeadId or CustomerName
            if (!string.IsNullOrEmpty(request.LeadId))
            {
                var cases = await _onboardingRepository.GetListAsync(o =>
                    o.LeadId == request.LeadId && o.IsValid);

                var onboarding = cases.FirstOrDefault();
                if (onboarding != null)
                {
                    response.CaseId = onboarding.Id;
                    response.CaseCode = onboarding.CaseCode;
                    response.CaseStatus = onboarding.Status;

                    // Get current stage name
                    if (onboarding.CurrentStageId.HasValue)
                    {
                        var stage = await _stageRepository.GetByIdAsync(onboarding.CurrentStageId.Value);
                        response.CurrentStageName = stage?.Name;
                    }

                    // Get workflow name if we have workflow ID
                    if (onboarding.WorkflowId > 0)
                    {
                        var workflow = await _workflowRepository.GetByIdAsync(onboarding.WorkflowId);
                        response.WorkflowName = workflow?.Name;
                    }
                }
            }
            else if (!string.IsNullOrEmpty(request.CustomerName))
            {
                var cases = await _onboardingRepository.GetListAsync(o =>
                    o.CaseName == request.CustomerName && o.IsValid);

                var onboarding = cases.FirstOrDefault();
                if (onboarding != null)
                {
                    response.CaseId = onboarding.Id;
                    response.CaseCode = onboarding.CaseCode;
                    response.CaseStatus = onboarding.Status;

                    // Get current stage name
                    if (onboarding.CurrentStageId.HasValue)
                    {
                        var stage = await _stageRepository.GetByIdAsync(onboarding.CurrentStageId.Value);
                        response.CurrentStageName = stage?.Name;
                    }

                    if (onboarding.WorkflowId > 0)
                    {
                        var workflow = await _workflowRepository.GetByIdAsync(onboarding.WorkflowId);
                        response.WorkflowName = workflow?.Name;
                    }
                }
            }

            return response;
        }

        /// <summary>
        /// Get attachments by case ID (onboarding ID)
        /// </summary>
        public async Task<GetAttachmentsFromExternalResponse> GetAttachmentsByCaseIdAsync(string caseId)
        {
            _logger.LogInformation("Getting attachments by case ID: CaseId={CaseId}", caseId);

            if (string.IsNullOrWhiteSpace(caseId))
            {
                return new GetAttachmentsFromExternalResponse
                {
                    Success = false,
                    Message = "CaseId is required"
                };
            }

            try
            {
                // Parse caseId to long (onboarding ID)
                if (!long.TryParse(caseId, out var onboardingId))
                {
                    _logger.LogWarning("Invalid CaseId format: CaseId={CaseId}", caseId);
                    return new GetAttachmentsFromExternalResponse
                    {
                        Success = false,
                        Message = "Invalid CaseId format"
                    };
                }

                // Get files from database
                var files = await _onboardingFileRepository.GetFilesByOnboardingAsync(onboardingId);

                // Convert to ExternalAttachmentDto with real-time OSS URL generation
                var attachments = new List<ExternalAttachmentDto>();
                foreach (var f in files)
                {
                    // Generate real-time OSS URL instead of using stored URL (which may expire and return 403)
                    var fallbackUrl = $"/ow/onboarding-files/v1.0/{f.Id}/download";
                    string downloadLink;
                    try
                    {
                        downloadLink = await _onboardingFileService.GetFileUrlAsync(f.Id);

                        // If the returned URL is null or empty, use fallback
                        if (string.IsNullOrEmpty(downloadLink))
                        {
                            _logger.LogWarning("GetFileUrlAsync returned empty for file {FileId}, falling back to download endpoint", f.Id);
                            downloadLink = fallbackUrl;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to generate real-time URL for file {FileId}, falling back to download endpoint", f.Id);
                        downloadLink = fallbackUrl;
                    }

                    attachments.Add(new ExternalAttachmentDto
                    {
                        Id = f.Id.ToString(),
                        FileName = f.OriginalFileName ?? f.StoredFileName ?? "unknown",
                        FileSize = f.FileSize.ToString(),
                        FileType = f.ContentType ?? "application/octet-stream",
                        FileExt = f.FileExtension ?? string.Empty,
                        CreateDate = f.UploadedDate.ToString("yyyy-MM-dd HH:mm:ss +00:00"),
                        DownloadLink = downloadLink
                    });
                }

                _logger.LogInformation("Successfully retrieved {Count} attachments for CaseId={CaseId}",
                    attachments.Count, caseId);

                return new GetAttachmentsFromExternalResponse
                {
                    Success = true,
                    Data = new AttachmentsData
                    {
                        Attachments = attachments,
                        Total = attachments.Count
                    },
                    Message = "Success"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attachments by case ID: CaseId={CaseId}", caseId);
                return new GetAttachmentsFromExternalResponse
                {
                    Success = false,
                    Message = $"Failed to get attachments: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Get outbound attachments by System ID
        /// </summary>
        public async Task<GetAttachmentsFromExternalResponse> GetOutboundAttachmentsBySystemIdAsync(string systemId)
        {
            _logger.LogInformation("Getting outbound attachments by System ID: SystemId={SystemId}", systemId);

            if (string.IsNullOrWhiteSpace(systemId))
            {
                return new GetAttachmentsFromExternalResponse
                {
                    Success = false,
                    Message = "SystemId is required"
                };
            }

            try
            {
                // Get entity mapping by System ID
                var entityMapping = await _entityMappingRepository.GetBySystemIdAsync(systemId);
                if (entityMapping == null)
                {
                    _logger.LogWarning("No entity mapping found for System ID: {SystemId}", systemId);
                    return new GetAttachmentsFromExternalResponse
                    {
                        Success = false,
                        Message = $"Entity mapping not found for System ID '{systemId}'"
                    };
                }

                // Parse workflow IDs from entity mapping
                var workflowIds = JsonConvert.DeserializeObject<List<long>>(entityMapping.WorkflowIds ?? "[]") ?? new List<long>();
                if (!workflowIds.Any())
                {
                    _logger.LogInformation("No workflows configured for System ID: {SystemId}", systemId);
                    return new GetAttachmentsFromExternalResponse
                    {
                        Success = true,
                        Data = new AttachmentsData
                        {
                            Attachments = new List<ExternalAttachmentDto>(),
                            Total = 0
                        },
                        Message = "Success"
                    };
                }

                // Get all onboardings for these workflows
                var allOnboardings = new List<Domain.Entities.OW.Onboarding>();
                foreach (var workflowId in workflowIds)
                {
                    var onboardings = await _onboardingRepository.GetListByWorkflowIdAsync(workflowId);
                    allOnboardings.AddRange(onboardings);
                }

                // Get all attachments from these onboardings with real-time OSS URL generation
                var allAttachments = new List<ExternalAttachmentDto>();
                foreach (var onboarding in allOnboardings)
                {
                    var files = await _onboardingFileRepository.GetFilesByOnboardingAsync(onboarding.Id);
                    foreach (var f in files)
                    {
                        // Generate real-time OSS URL instead of using stored URL (which may expire and return 403)
                        var fallbackUrl = $"/ow/onboarding-files/v1.0/{f.Id}/download";
                        string downloadLink;
                        try
                        {
                            downloadLink = await _onboardingFileService.GetFileUrlAsync(f.Id);

                            // If the returned URL is null or empty, use fallback
                            if (string.IsNullOrEmpty(downloadLink))
                            {
                                _logger.LogWarning("GetFileUrlAsync returned empty for file {FileId}, falling back to download endpoint", f.Id);
                                downloadLink = fallbackUrl;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to generate real-time URL for file {FileId}, falling back to download endpoint", f.Id);
                            downloadLink = fallbackUrl;
                        }

                        allAttachments.Add(new ExternalAttachmentDto
                        {
                            Id = f.Id.ToString(),
                            FileName = f.OriginalFileName ?? f.StoredFileName ?? "unknown",
                            FileSize = f.FileSize.ToString(),
                            FileType = f.ContentType ?? "application/octet-stream",
                            FileExt = f.FileExtension ?? string.Empty,
                            CreateDate = f.UploadedDate.ToString("yyyy-MM-dd HH:mm:ss +00:00"),
                            DownloadLink = downloadLink
                        });
                    }
                }

                _logger.LogInformation("Successfully retrieved {Count} attachments for SystemId={SystemId}",
                    allAttachments.Count, systemId);

                return new GetAttachmentsFromExternalResponse
                {
                    Success = true,
                    Data = new AttachmentsData
                    {
                        Attachments = allAttachments,
                        Total = allAttachments.Count
                    },
                    Message = "Success"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting outbound attachments by System ID: SystemId={SystemId}", systemId);
                return new GetAttachmentsFromExternalResponse
                {
                    Success = false,
                    Message = $"Failed to get attachments: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Get inbound attachments by System ID
        /// Retrieves attachment list from all onboardings associated with the System ID
        /// </summary>
        /// <param name="systemId">System ID (unique identifier for entity mapping)</param>
        /// <param name="entityId">External system entity ID (optional, for filtering attachments by specific entity)</param>
        public async Task<GetAttachmentsFromExternalResponse> GetInboundAttachmentsBySystemIdAsync(string systemId, string? entityId = null)
        {
            _logger.LogInformation("Getting inbound attachments by System ID: SystemId={SystemId}, EntityId={EntityId}", systemId, entityId);

            if (string.IsNullOrWhiteSpace(systemId))
            {
                return new GetAttachmentsFromExternalResponse
                {
                    Success = false,
                    Message = "SystemId is required"
                };
            }

            try
            {
                // Get all onboardings by System ID, optionally filtered by EntityId
                // Use ClearFilter to skip tenant filtering for external integration queries
                var query = _onboardingRepository.ClearFilter()
                    .Where(o => o.SystemId == systemId && o.IsValid);
                
                if (!string.IsNullOrWhiteSpace(entityId))
                {
                    query = query.Where(o => o.EntityId == entityId);
                }
                
                var onboardings = await query.ToListAsync();

                if (!onboardings.Any())
                {
                    _logger.LogInformation("No onboardings found for System ID: {SystemId}", systemId);
                    return new GetAttachmentsFromExternalResponse
                    {
                        Success = true,
                        Data = new AttachmentsData
                        {
                            Attachments = new List<ExternalAttachmentDto>(),
                            Total = 0
                        },
                        Message = "Success"
                    };
                }

                // Get all attachments from these onboardings
                var allAttachments = new List<ExternalAttachmentDto>();
                foreach (var onboarding in onboardings)
                {
                    // Use ClearFilter for file repository as well
                    var files = await _onboardingFileRepository.ClearFilter()
                        .Where(f => f.OnboardingId == onboarding.Id && f.IsValid == true)
                        .OrderByDescending(f => f.UploadedDate)
                        .ToListAsync();
                        
                    foreach (var f in files)
                    {
                        // Generate real-time OSS URL instead of using stored URL (which may expire and return 403)
                        var fallbackUrl = $"/ow/onboarding-files/v1.0/{f.Id}/download";
                        string downloadLink;
                        try
                        {
                            downloadLink = await _onboardingFileService.GetFileUrlAsync(f.Id);

                            // If the returned URL is null or empty, use fallback
                            if (string.IsNullOrEmpty(downloadLink))
                            {
                                _logger.LogWarning("GetFileUrlAsync returned empty for file {FileId}, falling back to download endpoint", f.Id);
                                downloadLink = fallbackUrl;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to generate real-time URL for file {FileId}, falling back to download endpoint", f.Id);
                            downloadLink = fallbackUrl;
                        }

                        allAttachments.Add(new ExternalAttachmentDto
                        {
                            Id = f.Id.ToString(),
                            FileName = f.OriginalFileName ?? f.StoredFileName ?? "unknown",
                            FileSize = f.FileSize.ToString(),
                            FileType = f.ContentType ?? "application/octet-stream",
                            FileExt = f.FileExtension ?? string.Empty,
                            CreateDate = f.UploadedDate.ToString("yyyy-MM-dd HH:mm:ss +00:00"),
                            DownloadLink = downloadLink,
                            EntityType = onboarding.EntityType,
                            EntityId = onboarding.EntityId
                        });
                    }
                }

                _logger.LogInformation("Successfully retrieved {Count} attachments for SystemId={SystemId}",
                    allAttachments.Count, systemId);

                return new GetAttachmentsFromExternalResponse
                {
                    Success = true,
                    Data = new AttachmentsData
                    {
                        Attachments = allAttachments,
                        Total = allAttachments.Count
                    },
                    Message = "Success"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inbound attachments by System ID: SystemId={SystemId}", systemId);
                return new GetAttachmentsFromExternalResponse
                {
                    Success = false,
                    Message = $"Failed to get attachments: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Fetch inbound attachments from external system by System ID
        /// 1. Get IntegrationId from EntityMapping by SystemId
        /// 2. Get InboundAttachment configuration from Integration
        /// 3. Execute HTTP Action to fetch attachments from external system
        /// 4. Parse and return the attachment list
        /// </summary>
        public async Task<GetAttachmentsFromExternalResponse> FetchInboundAttachmentsFromExternalAsync(string systemId, string? entityId = null)
        {
            _logger.LogInformation("Fetching inbound attachments from external system: SystemId={SystemId}, EntityId={EntityId}", 
                systemId, entityId ?? "(not provided)");

            if (string.IsNullOrWhiteSpace(systemId))
            {
                return new GetAttachmentsFromExternalResponse
                {
                    Success = false,
                    Message = "SystemId is required",
                    Msg = "SystemId is required",
                    Code = "400"
                };
            }

            try
            {
                // Step 1: Get entity mapping by System ID to find IntegrationId
                var entityMapping = await _entityMappingRepository.GetBySystemIdAsync(systemId);
                if (entityMapping == null)
                {
                    _logger.LogWarning("No entity mapping found for System ID: {SystemId}", systemId);
                    return new GetAttachmentsFromExternalResponse
                    {
                        Success = false,
                        Message = $"Entity mapping not found for System ID '{systemId}'",
                        Msg = "Entity mapping not found",
                        Code = "404"
                    };
                }

                var integrationId = entityMapping.IntegrationId;
                _logger.LogInformation("Found IntegrationId={IntegrationId} for SystemId={SystemId}", integrationId, systemId);

                // Step 2: Get Integration to retrieve InboundAttachments configuration
                var integration = await _integrationRepository.GetByIdAsync(integrationId);
                if (integration == null)
                {
                    _logger.LogWarning("Integration not found: IntegrationId={IntegrationId}", integrationId);
                    return new GetAttachmentsFromExternalResponse
                    {
                        Success = false,
                        Message = $"Integration not found for ID '{integrationId}'",
                        Msg = "Integration not found",
                        Code = "404"
                    };
                }

                // Step 3: Parse InboundAttachments configuration
                if (string.IsNullOrEmpty(integration.InboundAttachments))
                {
                    _logger.LogInformation("No InboundAttachments configured for Integration: IntegrationId={IntegrationId}", integrationId);
                    return new GetAttachmentsFromExternalResponse
                    {
                        Success = true,
                        Data = new AttachmentsData
                        {
                            Attachments = new List<ExternalAttachmentDto>(),
                            Total = 0
                        },
                        Message = "No inbound attachments configured",
                        Msg = "",
                        Code = "200"
                    };
                }

                List<InboundAttachmentItemDto> inboundAttachmentConfigs;
                try
                {
                    inboundAttachmentConfigs = JsonConvert.DeserializeObject<List<InboundAttachmentItemDto>>(integration.InboundAttachments)
                        ?? new List<InboundAttachmentItemDto>();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse InboundAttachments configuration: IntegrationId={IntegrationId}", integrationId);
                    return new GetAttachmentsFromExternalResponse
                    {
                        Success = false,
                        Message = "Invalid InboundAttachments configuration format",
                        Msg = "Invalid configuration format",
                        Code = "500"
                    };
                }

                if (!inboundAttachmentConfigs.Any())
                {
                    _logger.LogInformation("InboundAttachments configuration is empty: IntegrationId={IntegrationId}", integrationId);
                    return new GetAttachmentsFromExternalResponse
                    {
                        Success = true,
                        Data = new AttachmentsData
                        {
                            Attachments = new List<ExternalAttachmentDto>(),
                            Total = 0
                        },
                        Message = "No inbound attachment configurations",
                        Msg = "",
                        Code = "200"
                    };
                }

                // Step 4: Get authentication token if needed (do this once for all actions)
                string? accessToken = null;
                string? tenantCode = null;
                
                // Pre-fetch authentication info for actions that need it
                accessToken = await GetIntegrationAccessTokenAsync(integration);
                if (!string.IsNullOrEmpty(accessToken))
                {
                    tenantCode = await GetTenantCodeAsync(integration.TenantId, accessToken);
                    _logger.LogInformation("Obtained authentication info for Integration {IntegrationId}: HasToken={HasToken}, TenantCode={TenantCode}",
                        integrationId, !string.IsNullOrEmpty(accessToken), tenantCode ?? "N/A");
                }

                // Step 5: Execute Actions and collect attachments
                var actionExecutions = new List<ActionExecutionInfo>();
                var errors = new List<string>();

                foreach (var config in inboundAttachmentConfigs)
                {
                    var actionExecutionInfo = new ActionExecutionInfo
                    {
                        ActionId = config.ActionId.ToString(),
                        IntegrationName = integration.Name ?? string.Empty,
                        ModuleName = config.ModuleName ?? string.Empty,
                        IsSuccess = false,
                        Attachments = new List<ExternalAttachmentDto>()
                    };

                    try
                    {
                        _logger.LogInformation("Executing Action for inbound attachment: ActionId={ActionId}, ModuleName={ModuleName}",
                            config.ActionId, config.ModuleName);

                        // Get Action Definition
                        var actionDefinition = await _actionDefinitionRepository.GetByIdAsync(config.ActionId);
                        if (actionDefinition == null)
                        {
                            _logger.LogWarning("Action definition not found: ActionId={ActionId}", config.ActionId);
                            actionExecutionInfo.ActionName = $"Action {config.ActionId}";
                            actionExecutionInfo.ErrorMessage = "Action definition not found";
                            errors.Add($"Action {config.ActionId} not found");
                            actionExecutions.Add(actionExecutionInfo);
                            continue;
                        }

                        actionExecutionInfo.ActionName = actionDefinition.ActionName ?? string.Empty;

                        // Prepare context data with SystemId and EntityId
                        var contextData = new Dictionary<string, object>
                        {
                            { "systemId", systemId },
                            { "SystemId", systemId },
                            { "integrationId", integrationId.ToString() },
                            { "IntegrationId", integrationId.ToString() },
                            { "moduleName", config.ModuleName },
                            { "ModuleName", config.ModuleName }
                        };
                        
                        // Add entityId if provided
                        if (!string.IsNullOrEmpty(entityId))
                        {
                            contextData["entityId"] = entityId;
                            contextData["EntityId"] = entityId;
                        }

                        // Check if action needs authentication injection
                        var needsAuthInjection = !HasAuthorizationConfigured(actionDefinition.ActionConfig) && 
                                                  !string.IsNullOrEmpty(accessToken);
                        
                        JToken? actionResult;
                        
                        if (needsAuthInjection)
                        {
                            _logger.LogInformation("Injecting authentication headers for Action {ActionId}", config.ActionId);
                            
                            // Inject authentication headers into action config
                            var modifiedConfig = InjectAuthenticationHeaders(actionDefinition, accessToken!, tenantCode);
                            
                            // Execute action directly with modified config
                            var actionType = Enum.Parse<ActionTypeEnum>(actionDefinition.ActionType);
                            var result = await _actionExecutionService.ExecuteActionDirectlyAsync(
                                actionType, 
                                JsonConvert.SerializeObject(modifiedConfig), 
                                contextData);
                            actionResult = result != null ? JToken.FromObject(result) : null;
                        }
                        else
                        {
                            // Execute Action normally
                            actionResult = await _actionExecutionService.ExecuteActionAsync(config.ActionId, contextData);
                        }

                        if (actionResult == null)
                        {
                            _logger.LogWarning("Action execution returned null: ActionId={ActionId}", config.ActionId);
                            actionExecutionInfo.ErrorMessage = "Action returned no result";
                            errors.Add($"Action {config.ActionId} returned no result");
                            actionExecutions.Add(actionExecutionInfo);
                            continue;
                        }

                        // Extract status code and error message from action result if available
                        try
                        {
                            // actionResult is already a JToken, convert to JObject if needed
                            var resultJson = actionResult as JObject ?? JObject.FromObject(actionResult);
                            if (resultJson["statusCode"] != null)
                            {
                                actionExecutionInfo.StatusCode = resultJson["statusCode"].Value<int>();
                            }

                            // Check HTTP-level success first
                            var httpSuccess = resultJson["success"]?.Value<bool>() ??
                                (actionExecutionInfo.StatusCode >= 200 && actionExecutionInfo.StatusCode < 300);

                            // Parse response content to check business-level success and extract error message
                            var responseContent = resultJson["response"]?.ToString();
                            if (!string.IsNullOrEmpty(responseContent))
                            {
                                try
                                {
                                    var responseJson = JObject.Parse(responseContent);
                                    var businessSuccess = responseJson["success"]?.Value<bool>() ?? true;

                                    // Overall success requires both HTTP and business success
                                    actionExecutionInfo.IsSuccess = httpSuccess && businessSuccess;

                                    // Extract error message if HTTP request failed
                                    if (!httpSuccess)
                                    {
                                        actionExecutionInfo.ErrorMessage = GetHttpErrorMessage(actionExecutionInfo.StatusCode, responseContent);
                                    }
                                    // Extract error message if business logic failed
                                    else if (!businessSuccess)
                                    {
                                        actionExecutionInfo.ErrorMessage = responseJson["message"]?.ToString()
                                            ?? responseJson["msg"]?.ToString()
                                            ?? "Business logic failed";
                                    }
                                }
                                catch
                                {
                                    // If response parsing fails, use HTTP success
                                    actionExecutionInfo.IsSuccess = httpSuccess;
                                    if (!httpSuccess)
                                    {
                                        actionExecutionInfo.ErrorMessage = GetHttpErrorMessage(actionExecutionInfo.StatusCode, responseContent);
                                    }
                                }
                            }
                            else
                            {
                                actionExecutionInfo.IsSuccess = httpSuccess;
                                if (!httpSuccess)
                                {
                                    actionExecutionInfo.ErrorMessage = GetHttpErrorMessage(actionExecutionInfo.StatusCode, null);
                                }
                            }
                        }
                        catch
                        {
                            // If parsing fails, assume success based on having a result
                            actionExecutionInfo.IsSuccess = true;
                        }

                        // Step 5: Parse action result to extract attachments
                        var attachments = ParseAttachmentsFromActionResult(actionResult, integration.Name, config.ModuleName);
                        actionExecutionInfo.Attachments = attachments;

                        _logger.LogInformation("Extracted {Count} attachments from Action {ActionId}",
                            attachments.Count, config.ActionId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing Action: ActionId={ActionId}", config.ActionId);
                        actionExecutionInfo.ErrorMessage = ex.Message;
                        errors.Add($"Action {config.ActionId} execution failed: {ex.Message}");
                    }

                    actionExecutions.Add(actionExecutionInfo);
                }

                var message = errors.Any()
                    ? $"Success with {errors.Count} errors: {string.Join("; ", errors)}"
                    : "Success";

                var totalAttachments = actionExecutions.Sum(a => a.Attachments?.Count ?? 0);
                _logger.LogInformation("Fetched {Count} attachments from external system for SystemId={SystemId}",
                    totalAttachments, systemId);

                return new GetAttachmentsFromExternalResponse
                {
                    Success = true,
                    Data = new AttachmentsData
                    {
                        ActionExecutions = actionExecutions
                    },
                    Message = message,
                    Msg = "",
                    Code = "200"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching inbound attachments from external system: SystemId={SystemId}", systemId);
                return new GetAttachmentsFromExternalResponse
                {
                    Success = false,
                    Message = $"Failed to fetch attachments: {ex.Message}",
                    Msg = ex.Message,
                    Code = "500"
                };
            }
        }

        /// <summary>
        /// Parse attachments from action execution result
        /// Expected format:
        /// {
        ///   "data": {
        ///     "success": true,
        ///     "data": {
        ///       "attachments": [...],
        ///       "total": N
        ///     },
        ///     "message": "Success"
        ///   },
        ///   "success": true,
        ///   "msg": "",
        ///   "code": "200"
        /// }
        /// </summary>
        private List<ExternalAttachmentDto> ParseAttachmentsFromActionResult(JToken actionResult, string integrationName, string moduleName)
        {
            var attachments = new List<ExternalAttachmentDto>();

            try
            {
                // Try to extract the response content from different possible structures
                JToken? responseData = null;

                // Structure 1: Direct response wrapper from HttpApiActionExecutor
                // { success: true, statusCode: 200, response: "...", headers: {...} }
                if (actionResult["response"] != null)
                {
                    var responseStr = actionResult["response"]?.ToString();
                    if (!string.IsNullOrEmpty(responseStr))
                    {
                        try
                        {
                            responseData = JToken.Parse(responseStr);
                        }
                        catch
                        {
                            // Response is not JSON, skip
                            _logger.LogWarning("Action response is not valid JSON: {Response}", responseStr?.Substring(0, Math.Min(200, responseStr?.Length ?? 0)));
                            return attachments;
                        }
                    }
                }
                else
                {
                    // Use actionResult directly
                    responseData = actionResult;
                }

                if (responseData == null)
                {
                    return attachments;
                }

                // Log the parsed response structure for debugging
                _logger.LogDebug("Parsed response data structure: {ResponseData}", responseData.ToString(Newtonsoft.Json.Formatting.None).Substring(0, Math.Min(500, responseData.ToString(Newtonsoft.Json.Formatting.None).Length)));

                // Try to find attachments in various nested structures
                JArray? attachmentsArray = null;

                // Path 1: data.data.attachments (nested external API response)
                attachmentsArray = responseData.SelectToken("data.data.attachments") as JArray;
                _logger.LogDebug("Path 1 (data.data.attachments): {Found}", attachmentsArray != null ? $"Found {attachmentsArray.Count} items" : "Not found");

                // Path 2: data.attachments (standard response)
                if (attachmentsArray == null)
                {
                    attachmentsArray = responseData.SelectToken("data.attachments") as JArray;
                    _logger.LogDebug("Path 2 (data.attachments): {Found}", attachmentsArray != null ? $"Found {attachmentsArray.Count} items" : "Not found");
                }

                // Path 3: attachments (direct)
                if (attachmentsArray == null)
                {
                    attachmentsArray = responseData.SelectToken("attachments") as JArray;
                    _logger.LogDebug("Path 3 (attachments): {Found}", attachmentsArray != null ? $"Found {attachmentsArray.Count} items" : "Not found");
                }

                // Path 4: Check if response itself is an array
                if (attachmentsArray == null && responseData is JArray directArray)
                {
                    attachmentsArray = directArray;
                    _logger.LogDebug("Path 4 (direct array): Found {Count} items", attachmentsArray.Count);
                }

                if (attachmentsArray == null || !attachmentsArray.Any())
                {
                    _logger.LogDebug("No attachments found in action result for module: {ModuleName}", moduleName);
                    return attachments;
                }

                // Parse each attachment
                foreach (var item in attachmentsArray)
                {
                    try
                    {
                        var attachment = new ExternalAttachmentDto
                        {
                            Id = item["id"]?.ToString() ?? string.Empty,
                            FileName = item["fileName"]?.ToString() ?? item["filename"]?.ToString() ?? item["name"]?.ToString() ?? "unknown",
                            FileSize = item["fileSize"]?.ToString() ?? item["size"]?.ToString() ?? "0",
                            FileType = item["fileType"]?.ToString() ?? item["contentType"]?.ToString() ?? item["mimeType"]?.ToString() ?? "application/octet-stream",
                            FileExt = item["fileExt"]?.ToString() ?? item["extension"]?.ToString() ?? string.Empty,
                            CreateDate = item["createDate"]?.ToString() ?? item["createdAt"]?.ToString() ?? item["createTime"]?.ToString() ?? DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss +00:00"),
                            DownloadLink = ConvertToHttps(item["downloadLink"]?.ToString() ?? item["downloadUrl"]?.ToString() ?? item["url"]?.ToString() ?? string.Empty),
                            IntegrationName = integrationName,
                            ModuleName = moduleName
                        };

                        // Only add if we have at least an ID or file name
                        if (!string.IsNullOrEmpty(attachment.Id) || !string.IsNullOrEmpty(attachment.FileName))
                        {
                            attachments.Add(attachment);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse attachment item: {Item}", item.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing attachments from action result for module: {ModuleName}", moduleName);
            }

            return attachments;
        }

        /// <summary>
        /// Converts HTTP URLs to HTTPS for security
        /// </summary>
        /// <param name="url">The original URL</param>
        /// <returns>URL with HTTPS protocol</returns>
        private static string ConvertToHttps(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return url;
            }

            // Convert http:// to https:// for secure connections
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                return "https://" + url.Substring(7);
            }

            return url;
        }

        /// <summary>
        /// Gets a human-readable error message based on HTTP status code
        /// </summary>
        /// <param name="statusCode">HTTP status code</param>
        /// <param name="responseContent">Optional response content for additional context</param>
        /// <returns>Descriptive error message</returns>
        private static string GetHttpErrorMessage(int? statusCode, string? responseContent)
        {
            var baseMessage = statusCode switch
            {
                400 => "Bad Request - The request was invalid or malformed",
                401 => "Unauthorized - Authentication failed or credentials are missing",
                403 => "Forbidden - Access denied to the requested resource",
                404 => "Not Found - The requested resource does not exist",
                405 => "Method Not Allowed - The HTTP method is not supported",
                408 => "Request Timeout - The request took too long to complete",
                429 => "Too Many Requests - Rate limit exceeded",
                500 => "Internal Server Error - The external server encountered an error",
                502 => "Bad Gateway - The external server received an invalid response",
                503 => "Service Unavailable - The external service is temporarily unavailable",
                504 => "Gateway Timeout - The external server did not respond in time",
                _ => statusCode.HasValue 
                    ? $"HTTP Error {statusCode} - Request failed" 
                    : "Unknown error - Request failed without status code"
            };

            // Try to extract error message from response content if available
            if (!string.IsNullOrEmpty(responseContent))
            {
                try
                {
                    var responseJson = JObject.Parse(responseContent);
                    var detailMessage = responseJson["message"]?.ToString() 
                        ?? responseJson["error"]?.ToString()
                        ?? responseJson["msg"]?.ToString();
                    if (!string.IsNullOrEmpty(detailMessage))
                    {
                        return $"{baseMessage}: {detailMessage}";
                    }
                }
                catch
                {
                    // If parsing fails, just use the base message
                }
            }

            return baseMessage;
        }

        #region Integration Authentication Helper Methods

        /// <summary>
        /// Get OAuth2 access token from Integration credentials
        /// </summary>
        /// <param name="integration">Integration entity</param>
        /// <returns>Access token or null if failed</returns>
        private async Task<string?> GetIntegrationAccessTokenAsync(Domain.Entities.Integration.Integration integration)
        {
            try
            {
                if (string.IsNullOrEmpty(integration.EncryptedCredentials))
                {
                    _logger.LogWarning("Integration {IntegrationId} has no credentials configured", integration.Id);
                    return null;
                }

                var credentials = DecryptCredentials(integration.EncryptedCredentials);
                if (credentials == null || !credentials.Any())
                {
                    _logger.LogWarning("Failed to decrypt credentials for Integration {IntegrationId}", integration.Id);
                    return null;
                }

                // Only OAuth2 authentication is supported for auto-auth
                if (integration.AuthMethod != AuthenticationMethod.OAuth2)
                {
                    _logger.LogDebug("Integration {IntegrationId} uses {AuthMethod}, skipping auto-auth", 
                        integration.Id, integration.AuthMethod);
                    return null;
                }

                if (!credentials.TryGetValue("clientId", out var clientId) ||
                    !credentials.TryGetValue("clientSecret", out var clientSecret))
                {
                    _logger.LogWarning("Integration {IntegrationId} missing clientId or clientSecret", integration.Id);
                    return null;
                }

                if (string.IsNullOrEmpty(integration.EndpointUrl))
                {
                    _logger.LogWarning("Integration {IntegrationId} has no endpoint URL configured", integration.Id);
                    return null;
                }

                _logger.LogInformation("Requesting OAuth2 token for Integration {IntegrationId} from {EndpointUrl}", 
                    integration.Id, integration.EndpointUrl);

                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                var request = new HttpRequestMessage(HttpMethod.Post, integration.EndpointUrl);
                
                // Use Basic Auth header for client credentials (RFC 6749)
                var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);

                var formData = new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" }
                };
                request.Content = new FormUrlEncodedContent(formData);

                var response = await httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OAuth2 token request failed for Integration {IntegrationId}: {StatusCode} - {Response}", 
                        integration.Id, response.StatusCode, responseContent);
                    return null;
                }

                var tokenResponse = JObject.Parse(responseContent);
                var accessToken = tokenResponse["access_token"]?.ToString();

                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogWarning("OAuth2 response missing access_token for Integration {IntegrationId}", integration.Id);
                    return null;
                }

                _logger.LogInformation("Successfully obtained OAuth2 token for Integration {IntegrationId}", integration.Id);
                return accessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OAuth2 token for Integration {IntegrationId}", integration.Id);
                return null;
            }
        }

        /// <summary>
        /// Get tenant code from IDM service by tenant ID
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="accessToken">Access token for IDM API</param>
        /// <returns>Tenant code or null if failed</returns>
        private async Task<string?> GetTenantCodeAsync(string tenantId, string accessToken)
        {
            try
            {
                if (string.IsNullOrEmpty(tenantId))
                {
                    return null;
                }

                // Get IDM service URL from configuration
                var idmBaseUrl = _idmOptions.BaseUrl;
                if (string.IsNullOrEmpty(idmBaseUrl))
                {
                    _logger.LogError("IDM BaseUrl is not configured in IdmApis settings");
                    return null;
                }

                var idmUrl = $"{idmBaseUrl}/api/v1/tenant/{tenantId}";

                _logger.LogInformation("Fetching tenant code from IDM: TenantId={TenantId}, Url={Url}", tenantId, idmUrl);

                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                var request = new HttpRequestMessage(HttpMethod.Get, idmUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get tenant info from IDM: {StatusCode} - {Response}", 
                        response.StatusCode, responseContent);
                    return null;
                }

                var tenantResponse = JObject.Parse(responseContent);
                var tenantCode = tenantResponse["data"]?["tenantCode"]?.ToString();

                if (string.IsNullOrEmpty(tenantCode))
                {
                    _logger.LogWarning("IDM response missing tenantCode for TenantId={TenantId}", tenantId);
                    return null;
                }

                _logger.LogInformation("Successfully obtained tenant code: TenantId={TenantId}, TenantCode={TenantCode}", 
                    tenantId, tenantCode);
                return tenantCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tenant code for TenantId={TenantId}", tenantId);
                return null;
            }
        }        

        /// <summary>
        /// Check if action config has Authorization header configured
        /// </summary>
        /// <param name="actionConfig">Action configuration JToken</param>
        /// <returns>True if Authorization is configured</returns>
        private bool HasAuthorizationConfigured(JToken? actionConfig)
        {
            if (actionConfig == null)
                return false;

            try
            {
                // Check headers dictionary
                var headers = actionConfig["headers"] as JObject;
                if (headers != null)
                {
                    foreach (var prop in headers.Properties())
                    {
                        if (prop.Name.Equals("Authorization", StringComparison.OrdinalIgnoreCase) &&
                            !string.IsNullOrEmpty(prop.Value?.ToString()))
                        {
                            return true;
                        }
                    }
                }

                // Check headersList array
                var headersList = actionConfig["headersList"] as JArray;
                if (headersList != null)
                {
                    foreach (var item in headersList)
                    {
                        var key = item["key"]?.ToString();
                        var value = item["value"]?.ToString();
                        if (!string.IsNullOrEmpty(key) && 
                            key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) &&
                            !string.IsNullOrEmpty(value))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Inject authentication headers into action config
        /// </summary>
        /// <param name="actionDefinition">Action definition entity</param>
        /// <param name="accessToken">OAuth2 access token</param>
        /// <param name="tenantCode">Tenant code for x-tenant-id header</param>
        /// <returns>Modified action config as JToken</returns>
        private JToken InjectAuthenticationHeaders(Domain.Entities.Action.ActionDefinition actionDefinition, string accessToken, string? tenantCode)
        {
            try
            {
                var config = actionDefinition.ActionConfig?.DeepClone() ?? new JObject();
                
                // Ensure headers object exists
                if (config["headers"] == null)
                {
                    config["headers"] = new JObject();
                }

                var headers = config["headers"] as JObject;
                if (headers != null)
                {
                    // Add Authorization header
                    headers["Authorization"] = $"Bearer {accessToken}";
                    
                    // Add x-tenant-id header if tenant code is available
                    if (!string.IsNullOrEmpty(tenantCode))
                    {
                        headers["x-tenant-id"] = tenantCode;
                    }
                }

                // Also update headersList if it exists
                var headersList = config["headersList"] as JArray;
                if (headersList != null)
                {
                    // Remove existing Authorization and x-tenant-id entries
                    var toRemove = headersList
                        .Where(h => 
                        {
                            var key = h["key"]?.ToString();
                            return key?.Equals("Authorization", StringComparison.OrdinalIgnoreCase) == true ||
                                   key?.Equals("x-tenant-id", StringComparison.OrdinalIgnoreCase) == true;
                        })
                        .ToList();
                    
                    foreach (var item in toRemove)
                    {
                        headersList.Remove(item);
                    }

                    // Add new Authorization header
                    headersList.Add(new JObject
                    {
                        ["key"] = "Authorization",
                        ["value"] = $"Bearer {accessToken}"
                    });

                    // Add x-tenant-id header if tenant code is available
                    if (!string.IsNullOrEmpty(tenantCode))
                    {
                        headersList.Add(new JObject
                        {
                            ["key"] = "x-tenant-id",
                            ["value"] = tenantCode
                        });
                    }
                }

                _logger.LogDebug("Injected authentication headers into action config for Action {ActionId}", actionDefinition.Id);
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error injecting authentication headers for Action {ActionId}", actionDefinition.Id);
                return actionDefinition.ActionConfig ?? new JObject();
            }
        }

        /// <summary>
        /// Decrypt credentials from encrypted string
        /// </summary>
        private Dictionary<string, string>? DecryptCredentials(string encryptedCredentials)
        {
            try
            {
                var json = DecryptString(encryptedCredentials, ENCRYPTION_KEY);
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt credentials");
                return null;
            }
        }

        /// <summary>
        /// Decrypt string using AES
        /// </summary>
        private string DecryptString(string cipherText, string key)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = new byte[16]; // Use zero IV for simplicity (should use random IV in production)

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            
            return sr.ReadToEnd();
        }

        /// <summary>
        /// Execute CaseInfo Actions and populate fields via Field Mapping (Core logic)
        /// </summary>
        /// <param name="integration">Integration entity</param>
        /// <param name="entityMapping">Entity mapping</param>
        /// <param name="caseEntity">The case/onboarding entity</param>
        /// <param name="entityId">External entity ID to pass to the action</param>
        /// <returns>Tuple of (actionsExecuted, fieldsMapped)</returns>
        private async Task<(int ActionsExecuted, int FieldsMapped)> ExecuteCaseInfoActionsInternalAsync(
            Domain.Entities.Integration.Integration integration,
            Domain.Entities.Integration.EntityMapping entityMapping,
            Domain.Entities.OW.Onboarding caseEntity,
            string entityId)
        {
            int actionsExecuted = 0;
            int totalFieldsMapped = 0;

            // Get all actions that have field mappings (these are CaseInfo Actions)
            var actionsWithMappings = await GetActionsWithFieldMappingsAsync(entityMapping.IntegrationId);
            if (actionsWithMappings == null || !actionsWithMappings.Any())
            {
                _logger.LogInformation("No CaseInfo Actions found for Integration {IntegrationId}", entityMapping.IntegrationId);
                return (0, 0);
            }

            _logger.LogInformation("Found {Count} CaseInfo Actions for Integration {IntegrationId}",
                actionsWithMappings.Count, entityMapping.IntegrationId);

            // Get authentication info once for all actions
            string? accessToken = null;
            string? tenantCode = null;

            foreach (var (actionDefinition, fieldMappings) in actionsWithMappings)
            {
                try
                {
                    _logger.LogInformation("Executing CaseInfo Action {ActionId} ({ActionName}) for Case {CaseId}",
                        actionDefinition.Id, actionDefinition.ActionName, caseEntity.Id);

                    // Get authentication if not already obtained
                    if (string.IsNullOrEmpty(accessToken))
                    {
                        accessToken = await GetIntegrationAccessTokenAsync(integration);
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            tenantCode = await GetTenantCodeAsync(integration.TenantId, accessToken);
                            _logger.LogInformation("Obtained authentication for CaseInfo Actions: HasToken={HasToken}, TenantCode={TenantCode}",
                                !string.IsNullOrEmpty(accessToken), tenantCode ?? "N/A");
                        }
                    }

                    // Prepare context data with entityId
                    var contextData = new Dictionary<string, object>
                    {
                        { "entityId", entityId },
                        { "EntityId", entityId },
                        { "caseId", caseEntity.Id.ToString() },
                        { "CaseId", caseEntity.Id.ToString() },
                        { "systemId", entityMapping.SystemId ?? "" },
                        { "SystemId", entityMapping.SystemId ?? "" },
                        { "integrationId", entityMapping.IntegrationId.ToString() },
                        { "IntegrationId", entityMapping.IntegrationId.ToString() }
                    };

                    // Execute action with fresh authentication
                    JToken? actionResult;
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        _logger.LogInformation("Injecting fresh authentication headers for CaseInfo Action {ActionId}", actionDefinition.Id);
                        var modifiedConfig = InjectAuthenticationHeaders(actionDefinition, accessToken, tenantCode);
                        var actionType = Enum.Parse<ActionTypeEnum>(actionDefinition.ActionType);
                        var result = await _actionExecutionService.ExecuteActionDirectlyAsync(
                            actionType,
                            JsonConvert.SerializeObject(modifiedConfig),
                            contextData);
                        actionResult = result != null ? JToken.FromObject(result) : null;
                    }
                    else
                    {
                        _logger.LogWarning("No access token available for CaseInfo Action {ActionId}, executing with original config", actionDefinition.Id);
                        actionResult = await _actionExecutionService.ExecuteActionAsync(actionDefinition.Id, contextData);
                    }

                    if (actionResult == null)
                    {
                        _logger.LogWarning("CaseInfo Action {ActionId} returned null result", actionDefinition.Id);
                        continue;
                    }

                    // Check if action was successful
                    var resultJson = actionResult as JObject ?? JObject.FromObject(actionResult);
                    var statusCode = resultJson["statusCode"]?.Value<int>() ?? 0;
                    var success = resultJson["success"]?.Value<bool>() ?? (statusCode >= 200 && statusCode < 300);

                    if (!success)
                    {
                        _logger.LogWarning("CaseInfo Action {ActionId} failed with status {StatusCode}", actionDefinition.Id, statusCode);
                        continue;
                    }

                    // Parse response data
                    _logger.LogInformation("CaseInfo Action {ActionId} raw result: {Result}", 
                        actionDefinition.Id, resultJson.ToString(Newtonsoft.Json.Formatting.None));
                    
                    var responseData = ExtractResponseData(resultJson);
                    if (responseData == null)
                    {
                        _logger.LogWarning("CaseInfo Action {ActionId} returned no data to map. ResultJson keys: {Keys}", 
                            actionDefinition.Id, string.Join(", ", resultJson.Properties().Select(p => p.Name)));
                        continue;
                    }

                    _logger.LogInformation("CaseInfo Action {ActionId} extracted response data: {Data}", 
                        actionDefinition.Id, responseData.ToString(Newtonsoft.Json.Formatting.None));

                    // Check if external API response indicates success
                    var externalSuccess = responseData["success"]?.Value<bool>();
                    if (externalSuccess.HasValue && !externalSuccess.Value)
                    {
                        var errorMsg = responseData["msg"]?.ToString() ?? responseData["message"]?.ToString() ?? "Unknown error";
                        var errorCode = responseData["code"]?.ToString() ?? "";
                        _logger.LogWarning("CaseInfo Action {ActionId} - External API returned error: {ErrorMsg} (Code: {ErrorCode})", 
                            actionDefinition.Id, errorMsg, errorCode);
                        continue;
                    }

                    // Apply field mappings to populate case data
                    await ApplyFieldMappingsToCase(caseEntity, responseData, fieldMappings);

                    actionsExecuted++;
                    totalFieldsMapped += fieldMappings.Count;

                    _logger.LogInformation("Successfully executed CaseInfo Action {ActionId} and populated {FieldCount} fields for Case {CaseId}",
                        actionDefinition.Id, fieldMappings.Count, caseEntity.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing CaseInfo Action {ActionId} for Case {CaseId}",
                        actionDefinition.Id, caseEntity.Id);
                }
            }

            return (actionsExecuted, totalFieldsMapped);
        }

        /// <summary>
        /// Execute CaseInfo Actions after case creation and populate fields via Field Mapping
        /// Actions with InboundFieldMapping configured are considered CaseInfo Actions
        /// </summary>
        /// <param name="entityMapping">Entity mapping</param>
        /// <param name="createdCase">The created case/onboarding</param>
        /// <param name="entityId">External entity ID to pass to the action</param>
        private async Task ExecuteCaseInfoActionsAndPopulateFieldsAsync(
            Domain.Entities.Integration.EntityMapping entityMapping,
            Domain.Entities.OW.Onboarding createdCase,
            string entityId)
        {
            try
            {
                _logger.LogInformation("Looking for CaseInfo Actions with field mappings for Integration {IntegrationId}",
                    entityMapping.IntegrationId);

                // Get integration for authentication
                var integration = await _integrationRepository.GetByIdAsync(entityMapping.IntegrationId);
                if (integration == null)
                {
                    _logger.LogWarning("Integration {IntegrationId} not found, skipping CaseInfo Actions", entityMapping.IntegrationId);
                    return;
                }

                // Execute CaseInfo Actions using core logic
                var (actionsExecuted, fieldsMapped) = await ExecuteCaseInfoActionsInternalAsync(
                    integration, entityMapping, createdCase, entityId);

                if (actionsExecuted > 0)
                {
                    _logger.LogInformation("Completed CaseInfo Actions for Case {CaseId}: Executed {ActionsExecuted} actions, mapped {FieldsMapped} fields",
                        createdCase.Id, actionsExecuted, fieldsMapped);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the case creation
                _logger.LogError(ex, "Error executing CaseInfo Actions for Case {CaseId}", createdCase.Id);
            }
        }

        /// <summary>
        /// Get all actions that have field mappings configured for an integration
        /// Only returns actions that belong to the specified integration
        /// </summary>
        private async Task<List<(Domain.Entities.Action.ActionDefinition Action, List<Domain.Entities.Integration.InboundFieldMapping> Mappings)>> GetActionsWithFieldMappingsAsync(long integrationId)
        {
            var result = new List<(Domain.Entities.Action.ActionDefinition, List<Domain.Entities.Integration.InboundFieldMapping>)>();

            try
            {
                // Method 1: Get actions from InboundAttachments configuration
                var integration = await _integrationRepository.GetByIdAsync(integrationId);
                if (integration == null)
                {
                    _logger.LogWarning("Integration {IntegrationId} not found", integrationId);
                    return result;
                }

                var configuredActionIds = new HashSet<long>();
                
                // Parse InboundAttachments to get configured action IDs
                if (!string.IsNullOrEmpty(integration.InboundAttachments))
                {
                    try
                    {
                        var inboundConfigs = JsonConvert.DeserializeObject<List<InboundAttachmentItemDto>>(integration.InboundAttachments);
                        if (inboundConfigs != null)
                        {
                            foreach (var config in inboundConfigs)
                            {
                                configuredActionIds.Add(config.ActionId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse InboundAttachments for Integration {IntegrationId}", integrationId);
                    }
                }

                // Method 2: Get actions from ActionTriggerMapping where TriggerSourceId = IntegrationId
                var triggerMappings = await _actionDefinitionRepository.GetMappingsWithActionDetailsByTriggerSourceIdAsync(integrationId);
                if (triggerMappings != null && triggerMappings.Any())
                {
                    foreach (var mapping in triggerMappings)
                    {
                        if (mapping.ActionDefinitionId > 0)
                        {
                            configuredActionIds.Add(mapping.ActionDefinitionId);
                        }
                    }
                }

                // Get field mappings for all configured actions
                foreach (var actionId in configuredActionIds)
                {
                    var fieldMappings = await _fieldMappingRepository.GetByActionIdAsync(actionId);
                    if (fieldMappings != null && fieldMappings.Any())
                    {
                        var actionDefinition = await _actionDefinitionRepository.GetByIdAsync(actionId);
                        if (actionDefinition != null && actionDefinition.IsValid)
                        {
                            result.Add((actionDefinition, fieldMappings));
                            _logger.LogDebug("Found CaseInfo Action {ActionId} ({ActionName}) with {MappingCount} field mappings",
                                actionId, actionDefinition.ActionName, fieldMappings.Count);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting actions with field mappings for Integration {IntegrationId}", integrationId);
            }

            return result;
        }

        /// <summary>
        /// Extract response data from action result
        /// </summary>
        private JObject? ExtractResponseData(JObject resultJson)
        {
            try
            {
                var responseContent = resultJson["response"]?.ToString();
                if (string.IsNullOrEmpty(responseContent))
                {
                    return null;
                }

                var responseJson = JObject.Parse(responseContent);

                // Try different data paths
                // Path 1: data.data (nested response)
                var data = responseJson.SelectToken("data.data") as JObject;
                if (data != null) return data;

                // Path 2: data (standard response)
                data = responseJson.SelectToken("data") as JObject;
                if (data != null) return data;

                // Path 3: Direct response
                return responseJson;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract response data from action result");
                return null;
            }
        }

        /// <summary>
        /// Apply field mappings to populate case data from external system response
        /// Uses reflection to dynamically set properties and saves to StaticFieldValue
        /// </summary>
        private async Task ApplyFieldMappingsToCase(
            Domain.Entities.OW.Onboarding caseEntity,
            JObject responseData,
            List<Domain.Entities.Integration.InboundFieldMapping> fieldMappings)
        {
            _logger.LogInformation("Starting Field Mapping for Case {CaseId}. Response data: {ResponseData}, Mapping count: {MappingCount}",
                caseEntity.Id, 
                responseData.ToString(Newtonsoft.Json.Formatting.None),
                fieldMappings.Count);

            var caseUpdated = false;
            var staticFieldValues = new List<StaticFieldValueInputDto>();
            var customFields = !string.IsNullOrEmpty(caseEntity.CustomFieldsJson)
                ? JsonConvert.DeserializeObject<Dictionary<string, object>>(caseEntity.CustomFieldsJson) ?? new Dictionary<string, object>()
                : new Dictionary<string, object>();

            var onboardingType = typeof(Domain.Entities.OW.Onboarding);

            foreach (var mapping in fieldMappings)
            {
                try
                {
                    // Skip outbound-only mappings
                    if (mapping.SyncDirection == SyncDirection.OutboundOnly)
                    {
                        continue;
                    }

                    // Get value from response data using externalFieldName
                    var externalValue = GetValueFromResponseData(responseData, mapping.ExternalFieldName);
                    
                    // Use default value if external value is empty
                    if (string.IsNullOrEmpty(externalValue) && !string.IsNullOrEmpty(mapping.DefaultValue))
                    {
                        externalValue = mapping.DefaultValue;
                    }

                    if (string.IsNullOrEmpty(externalValue))
                    {
                        continue;
                    }

                    var wfeFieldId = mapping.WfeFieldId?.ToUpper() ?? "";
                    var propertySet = false;

                    // Try to map to Onboarding entity property using the mapping dictionary
                    if (FieldToPropertyMapping.TryGetValue(wfeFieldId, out var propertyName))
                    {
                        var property = onboardingType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                        if (property != null && property.CanWrite)
                        {
                            try
                            {
                                var convertedValue = ConvertToPropertyType(externalValue, property.PropertyType);
                                property.SetValue(caseEntity, convertedValue);
                                caseUpdated = true;
                                propertySet = true;
                                _logger.LogDebug("Set property {PropertyName} = {Value} via reflection", propertyName, externalValue);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to set property {PropertyName} with value {Value}", propertyName, externalValue);
                            }
                        }
                    }

                    // If not a standard field, store in custom fields
                    if (!propertySet)
                    {
                        customFields[mapping.WfeFieldId] = ConvertFieldValue(externalValue, mapping.FieldType);
                        caseUpdated = true;
                    }

                    // Prepare StaticFieldValue for saving to stage
                    if (caseEntity.CurrentStageId.HasValue)
                    {
                        staticFieldValues.Add(new StaticFieldValueInputDto
                        {
                            OnboardingId = caseEntity.Id,
                            StageId = caseEntity.CurrentStageId.Value,
                            FieldName = mapping.WfeFieldId,
                            DisplayName = mapping.WfeFieldId,
                            FieldLabel = mapping.WfeFieldId,
                            FieldValueJson = JsonConvert.SerializeObject(externalValue),
                            FieldType = mapping.FieldType.ToString().ToLower(),
                            Source = "external_integration",
                            Status = "Submitted"
                        });
                    }

                    _logger.LogDebug("Mapped field {ExternalField} -> {WfeField} = {Value}",
                        mapping.ExternalFieldName, mapping.WfeFieldId, externalValue);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to map field {ExternalField} -> {WfeField}",
                        mapping.ExternalFieldName, mapping.WfeFieldId);
                }
            }

            // Update case entity if any fields were mapped
            if (caseUpdated)
            {
                caseEntity.CustomFieldsJson = JsonConvert.SerializeObject(customFields);
                caseEntity.InitModifyInfo(_userContext);
                await _onboardingRepository.UpdateAsync(caseEntity);
                _logger.LogInformation("Updated case {CaseId} with {FieldCount} mapped field values", 
                    caseEntity.Id, fieldMappings.Count);
            }

            // Save static field values to stage
            if (staticFieldValues.Any() && caseEntity.CurrentStageId.HasValue)
            {
                try
                {
                    var batchInput = new BatchStaticFieldValueInputDto
                    {
                        OnboardingId = caseEntity.Id,
                        StageId = caseEntity.CurrentStageId.Value,
                        FieldValues = staticFieldValues,
                        OverwriteExisting = true,
                        Source = "external_integration"
                    };

                    await _staticFieldValueService.BatchSaveAsync(batchInput);
                    _logger.LogInformation("Saved {Count} static field values for Case {CaseId} Stage {StageId}",
                        staticFieldValues.Count, caseEntity.Id, caseEntity.CurrentStageId.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to save static field values for Case {CaseId}", caseEntity.Id);
                }
            }
        }

        /// <summary>
        /// Convert string value to target property type
        /// </summary>
        private object? ConvertToPropertyType(string value, Type targetType)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            try
            {
                if (underlyingType == typeof(string))
                    return value;

                if (underlyingType == typeof(long))
                    return long.TryParse(value, out var l) ? l : null;

                if (underlyingType == typeof(int))
                    return int.TryParse(value, out var i) ? i : null;

                if (underlyingType == typeof(decimal))
                    return decimal.TryParse(value, out var d) ? d : null;

                if (underlyingType == typeof(double))
                    return double.TryParse(value, out var db) ? db : null;

                if (underlyingType == typeof(bool))
                    return bool.TryParse(value, out var b) ? b : null;

                if (underlyingType == typeof(DateTimeOffset))
                    return DateTimeOffset.TryParse(value, out var dto) ? dto : null;

                if (underlyingType == typeof(DateTime))
                    return DateTime.TryParse(value, out var dt) ? dt : null;

                // For enums
                if (underlyingType.IsEnum)
                    return Enum.TryParse(underlyingType, value, true, out var e) ? e : null;

                return Convert.ChangeType(value, underlyingType);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get value from response data, supporting nested paths
        /// </summary>
        private string? GetValueFromResponseData(JObject responseData, string fieldName)
        {
            if (responseData == null || string.IsNullOrEmpty(fieldName))
                return null;

            try
            {
                // Try direct field access first
                var token = responseData[fieldName];
                if (token != null && token.Type != JTokenType.Null)
                {
                    return token.ToString();
                }

                // Try case-insensitive search
                foreach (var prop in responseData.Properties())
                {
                    if (prop.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
                    {
                        return prop.Value?.ToString();
                    }
                }

                // Try nested path (e.g., "data.fieldName")
                token = responseData.SelectToken(fieldName);
                if (token != null && token.Type != JTokenType.Null)
                {
                    return token.ToString();
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Convert field value based on field type
        /// </summary>
        private object ConvertFieldValue(string value, FieldType fieldType)
        {
            try
            {
                return fieldType switch
                {
                    FieldType.Number => decimal.TryParse(value, out var num) ? num : value,
                    FieldType.Boolean => bool.TryParse(value, out var b) ? b : value,
                    FieldType.Date => DateTimeOffset.TryParse(value, out var dt) ? dt : value,
                    _ => value
                };
            }
            catch
            {
                return value;
            }
        }

        #endregion

        #region Retry Field Mapping

        /// <summary>
        /// Retry field mapping execution for a specific case
        /// Re-executes CaseInfo actions and applies field mappings to update case data
        /// </summary>
        public async Task<RetryFieldMappingResponse> RetryFieldMappingAsync(long caseId)
        {
            _logger.LogInformation("Retrying field mapping for Case {CaseId}", caseId);

            try
            {
                // Get case entity
                var caseEntity = await _onboardingRepository.GetByIdAsync(caseId);
                if (caseEntity == null)
                {
                    return new RetryFieldMappingResponse
                    {
                        Success = false,
                        CaseId = caseId,
                        Message = $"Case {caseId} not found",
                        ErrorDetails = "Case does not exist"
                    };
                }

                // Get entity mapping by SystemId
                if (string.IsNullOrEmpty(caseEntity.SystemId))
                {
                    return new RetryFieldMappingResponse
                    {
                        Success = false,
                        CaseId = caseId,
                        Message = "Case does not have SystemId configured",
                        ErrorDetails = "SystemId is required to retry field mapping"
                    };
                }

                var entityMapping = await _entityMappingRepository.GetBySystemIdAsync(caseEntity.SystemId);
                if (entityMapping == null)
                {
                    return new RetryFieldMappingResponse
                    {
                        Success = false,
                        CaseId = caseId,
                        Message = $"Entity mapping not found for SystemId: {caseEntity.SystemId}",
                        ErrorDetails = "Entity mapping does not exist"
                    };
                }

                // Get integration
                var integration = await _integrationRepository.GetByIdAsync(entityMapping.IntegrationId);
                if (integration == null)
                {
                    return new RetryFieldMappingResponse
                    {
                        Success = false,
                        CaseId = caseId,
                        Message = $"Integration {entityMapping.IntegrationId} not found",
                        ErrorDetails = "Integration does not exist"
                    };
                }

                // Execute CaseInfo Actions using core logic
                var (actionsExecuted, fieldsMapped) = await ExecuteCaseInfoActionsInternalAsync(
                    integration, entityMapping, caseEntity, caseEntity.EntityId ?? "");

                if (actionsExecuted == 0)
                {
                    return new RetryFieldMappingResponse
                    {
                        Success = false,
                        CaseId = caseId,
                        Message = "No CaseInfo Actions were executed",
                        ErrorDetails = "No actions configured for field mapping or all actions failed"
                    };
                }

                return new RetryFieldMappingResponse
                {
                    Success = true,
                    CaseId = caseId,
                    ActionsExecuted = actionsExecuted,
                    FieldsMapped = fieldsMapped,
                    Message = $"Successfully retried field mapping. Executed {actionsExecuted} actions and mapped {fieldsMapped} fields."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying field mapping for Case {CaseId}", caseId);
                return new RetryFieldMappingResponse
                {
                    Success = false,
                    CaseId = caseId,
                    Message = "Failed to retry field mapping",
                    ErrorDetails = ex.Message
                };
            }
        }

        #endregion
    }
}
