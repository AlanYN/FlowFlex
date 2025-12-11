using FlowFlex.Application.Contracts.Dtos.Integration;
using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Application.Contracts.IServices.Integration;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Repository.Action;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.Action;
using FlowFlex.Domain.Shared.Exceptions;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
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
        private readonly UserContext _userContext;
        private readonly ILogger<ExternalIntegrationService> _logger;

        // AES encryption key (should match IntegrationService)
        private const string ENCRYPTION_KEY = "FlowFlex2024IntegrationKey123456"; // 32 bytes for AES-256

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
            _userContext = userContext;
            _logger = logger;
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
        /// </summary>
        public async Task<CreateCaseFromExternalResponse> CreateCaseAsync(CreateCaseFromExternalRequest request)
        {
            _logger.LogInformation("Creating case from external system: SystemId={SystemId}, WorkflowId={WorkflowId}",
                request.SystemId, request.WorkflowId);

            if (string.IsNullOrWhiteSpace(request.SystemId))
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "System ID is required");
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

            // Create onboarding input
            var onboardingInput = new OnboardingInputDto
            {
                WorkflowId = request.WorkflowId,
                LeadId = request.LeadId,
                LeadName = request.CustomerName,
                ContactPerson = request.ContactName ?? request.CustomerName,
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

            // Create the case
            var caseId = await _onboardingService.CreateAsync(onboardingInput);

            // Get the created case to return details
            var createdCase = await _onboardingRepository.GetByIdAsync(caseId);

            // Update SystemId and IntegrationId
            if (createdCase != null)
            {
                createdCase.SystemId = request.SystemId;
                createdCase.IntegrationId = entityMapping.IntegrationId;
                createdCase.InitModifyInfo(_userContext);
                await _onboardingRepository.UpdateAsync(createdCase);
                _logger.LogInformation("Updated SystemId={SystemId} and IntegrationId={IntegrationId} for case {CaseId}",
                    request.SystemId, entityMapping.IntegrationId, caseId);
            }

            _logger.LogInformation("Successfully created case {CaseId} from external system", caseId);

            return new CreateCaseFromExternalResponse
            {
                CaseId = caseId,
                CaseCode = createdCase?.CaseCode ?? "",
                WorkflowId = request.WorkflowId,
                WorkflowName = workflow.Name,
                CurrentStageId = firstStage.Id,
                CurrentStageName = firstStage.Name ?? "",
                Status = "Started",
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
                    o.LeadName == request.CustomerName && o.IsValid);

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
        public async Task<GetAttachmentsFromExternalResponse> GetInboundAttachmentsBySystemIdAsync(string systemId)
        {
            _logger.LogInformation("Getting inbound attachments by System ID: SystemId={SystemId}", systemId);

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
                // Get all onboardings by System ID
                var onboardings = await _onboardingRepository.GetListAsync(o =>
                    o.SystemId == systemId && o.IsValid);

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
        public async Task<GetAttachmentsFromExternalResponse> FetchInboundAttachmentsFromExternalAsync(string systemId)
        {
            _logger.LogInformation("Fetching inbound attachments from external system by System ID: SystemId={SystemId}", systemId);

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

                // Step 4: Execute Actions and collect attachments
                var allAttachments = new List<ExternalAttachmentDto>();
                var actionExecutions = new List<ActionExecutionInfo>();
                var errors = new List<string>();

                foreach (var config in inboundAttachmentConfigs)
                {
                    var actionExecutionInfo = new ActionExecutionInfo
                    {
                        ActionId = config.ActionId.ToString(),
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

                        // Prepare context data with SystemId
                        var contextData = new Dictionary<string, object>
                        {
                            { "systemId", systemId },
                            { "SystemId", systemId },
                            { "integrationId", integrationId.ToString() },
                            { "IntegrationId", integrationId.ToString() },
                            { "moduleName", config.ModuleName },
                            { "ModuleName", config.ModuleName }
                        };

                        // Execute Action
                        var actionResult = await _actionExecutionService.ExecuteActionAsync(config.ActionId, contextData);

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
                                    
                                    // Extract error message if business failed
                                    if (!businessSuccess)
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
                                }
                            }
                            else
                            {
                                actionExecutionInfo.IsSuccess = httpSuccess;
                            }
                        }
                        catch
                        {
                            // If parsing fails, assume success based on having a result
                            actionExecutionInfo.IsSuccess = true;
                        }

                        // Step 5: Parse action result to extract attachments
                        var attachments = ParseAttachmentsFromActionResult(actionResult, integration.Name, config.ModuleName);
                        allAttachments.AddRange(attachments);
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

                _logger.LogInformation("Fetched {Count} attachments from external system for SystemId={SystemId}",
                    allAttachments.Count, systemId);

                return new GetAttachmentsFromExternalResponse
                {
                    Success = true,
                    Data = new AttachmentsData
                    {
                        Attachments = allAttachments,
                        Total = allAttachments.Count,
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

                // Try to find attachments in various nested structures
                JArray? attachmentsArray = null;

                // Path 1: data.data.attachments (nested external API response)
                attachmentsArray = responseData.SelectToken("data.data.attachments") as JArray;

                // Path 2: data.attachments (standard response)
                if (attachmentsArray == null)
                {
                    attachmentsArray = responseData.SelectToken("data.attachments") as JArray;
                }

                // Path 3: attachments (direct)
                if (attachmentsArray == null)
                {
                    attachmentsArray = responseData.SelectToken("attachments") as JArray;
                }

                // Path 4: Check if response itself is an array
                if (attachmentsArray == null && responseData is JArray directArray)
                {
                    attachmentsArray = directArray;
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
    }
}
