using FlowFlex.Application.Contracts.Dtos.Integration;
using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Application.Contracts.IServices.Integration;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Exceptions;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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

                // Convert to ExternalAttachmentDto
                var attachments = files.Select(f => new ExternalAttachmentDto
                {
                    Id = f.Id.ToString(),
                    FileName = f.OriginalFileName ?? f.StoredFileName ?? "unknown",
                    FileSize = f.FileSize.ToString(),
                    FileType = f.ContentType ?? "application/octet-stream",
                    FileExt = f.FileExtension ?? string.Empty,
                    CreateDate = f.UploadedDate.ToString("yyyy-MM-dd HH:mm:ss +00:00"),
                    DownloadLink = !string.IsNullOrEmpty(f.AccessUrl) 
                        ? f.AccessUrl 
                        : $"/ow/onboarding-files/v1.0/{f.Id}/download"
                }).ToList();

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

                // Get all attachments from these onboardings
                var allAttachments = new List<ExternalAttachmentDto>();
                foreach (var onboarding in allOnboardings)
                {
                    var files = await _onboardingFileRepository.GetFilesByOnboardingAsync(onboarding.Id);
                    var attachments = files.Select(f => new ExternalAttachmentDto
                    {
                        Id = f.Id.ToString(),
                        FileName = f.OriginalFileName ?? f.StoredFileName ?? "unknown",
                        FileSize = f.FileSize.ToString(),
                        FileType = f.ContentType ?? "application/octet-stream",
                        FileExt = f.FileExtension ?? string.Empty,
                        CreateDate = f.UploadedDate.ToString("yyyy-MM-dd HH:mm:ss +00:00"),
                        DownloadLink = !string.IsNullOrEmpty(f.AccessUrl)
                            ? f.AccessUrl
                            : $"/ow/onboarding-files/v1.0/{f.Id}/download"
                    });
                    allAttachments.AddRange(attachments);
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
                    var attachments = files.Select(f => new ExternalAttachmentDto
                    {
                        Id = f.Id.ToString(),
                        FileName = f.OriginalFileName ?? f.StoredFileName ?? "unknown",
                        FileSize = f.FileSize.ToString(),
                        FileType = f.ContentType ?? "application/octet-stream",
                        FileExt = f.FileExtension ?? string.Empty,
                        CreateDate = f.UploadedDate.ToString("yyyy-MM-dd HH:mm:ss +00:00"),
                        DownloadLink = !string.IsNullOrEmpty(f.AccessUrl)
                            ? f.AccessUrl
                            : $"/ow/onboarding-files/v1.0/{f.Id}/download"
                    });
                    allAttachments.AddRange(attachments);
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
    }
}
