using AutoMapper;
using MediatR;
using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.Dtos;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Events;
using System.Linq;
using SqlSugar;
using FlowFlex.Domain.Shared.Attr;
using System.Text;
using System.IO;
using System.Globalization;
using Item.Excel.Lib;
// using Item.Redis; // Temporarily disable Redis
using System.Text.Json;
using System.Diagnostics;
using FlowFlex.Domain.Shared.Models;
using System.Linq.Expressions;
using FlowFlex.Application.Services.OW.Extensions;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Onboarding service implementation
    /// </summary>
    public class OnboardingService : IOnboardingService
    {
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IWorkflowRepository _workflowRepository;
        private readonly IStageRepository _stageRepository;
        private readonly IStageCompletionLogRepository _stageCompletionLogRepository;
        private readonly IMapper _mapper;
        private readonly UserContext _userContext;
        private readonly IMediator _mediator;
        // Cache key constants - temporarily disable Redis cache
        private const string WORKFLOW_CACHE_PREFIX = "ow:workflow";
        private const string STAGE_CACHE_PREFIX = "ow:stage";
        private const int CACHE_EXPIRY_MINUTES = 30; // Cache for 30 minutes

        public OnboardingService(
            IOnboardingRepository onboardingRepository,
            IWorkflowRepository workflowRepository,
            IStageRepository stageRepository,
            IStageCompletionLogRepository stageCompletionLogRepository,
            IMapper mapper,
            UserContext userContext,
            IMediator mediator)
        {
            _onboardingRepository = onboardingRepository ?? throw new ArgumentNullException(nameof(onboardingRepository));
            _workflowRepository = workflowRepository ?? throw new ArgumentNullException(nameof(workflowRepository));
            _stageRepository = stageRepository ?? throw new ArgumentNullException(nameof(stageRepository));
            _stageCompletionLogRepository = stageCompletionLogRepository ?? throw new ArgumentNullException(nameof(stageCompletionLogRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        /// <summary>
        /// Create a new onboarding instance
        /// </summary>
        public async Task<long> CreateAsync(OnboardingInputDto input)
        {
            try
            {
                Console.WriteLine("=== OnboardingService.CreateAsync - Step 1: Method entry ===");
                
                // Check all injected dependencies
                if (_onboardingRepository == null)
                {
                    Console.WriteLine("ERROR: _onboardingRepository is null!");
                    throw new CRMException(ErrorCodeEnum.SystemError, "Onboarding repository is not available");
                }
                
                if (_workflowRepository == null)
                {
                    Console.WriteLine("ERROR: _workflowRepository is null!");
                    throw new CRMException(ErrorCodeEnum.SystemError, "Workflow repository is not available");
                }
                
                if (_stageRepository == null)
                {
                    Console.WriteLine("ERROR: _stageRepository is null!");
                    throw new CRMException(ErrorCodeEnum.SystemError, "Stage repository is not available");
                }
                
                if (_mapper == null)
                {
                    Console.WriteLine("ERROR: _mapper is null!");
                    throw new CRMException(ErrorCodeEnum.SystemError, "Mapper is not available");
                }
                
                if (_userContext == null)
                {
                    Console.WriteLine("ERROR: _userContext is null!");
                    throw new CRMException(ErrorCodeEnum.SystemError, "User context is not available");
                }
                
                if (input == null)
                {
                    Console.WriteLine("ERROR: input parameter is null!");
                    throw new CRMException(ErrorCodeEnum.ParamInvalid, "Input parameter cannot be null");
                }
                
                Console.WriteLine($"=== OnboardingService.CreateAsync - Step 2: All dependencies checked, input WorkflowId: {input.WorkflowId} ===");

                // Ensure the table exists before inserting
                Console.WriteLine("=== OnboardingService.CreateAsync - Step 3: Calling EnsureTableExistsAsync ===");
                await _onboardingRepository.EnsureTableExistsAsync();
                Console.WriteLine("=== OnboardingService.CreateAsync - Step 4: EnsureTableExistsAsync completed ===");

                // Get tenant ID from UserContext (injected from HTTP headers via middleware)
                string tenantId = _userContext?.TenantId ?? "default";
                Console.WriteLine($"Using tenant ID from UserContext: {tenantId}");

                // Handle default workflow selection if WorkflowId is not provided
                Console.WriteLine($"=== OnboardingService.CreateAsync - Step 5: Checking WorkflowId - HasValue: {input.WorkflowId.HasValue}, Value: {input.WorkflowId?.ToString() ?? "null"} ===");
                
                if (!input.WorkflowId.HasValue || input.WorkflowId.Value <= 0)
                {
                    Console.WriteLine("WorkflowId not provided or invalid, attempting to get default workflow...");
                    
                    var defaultWorkflow = await _workflowRepository.GetDefaultWorkflowAsync();

                    if (defaultWorkflow != null && defaultWorkflow.IsValid && defaultWorkflow.IsActive)
                    {
                        input.WorkflowId = defaultWorkflow.Id;
                        Console.WriteLine($"Using default workflow - ID: {defaultWorkflow.Id}, Name: {defaultWorkflow.Name}");
                    }
                    else
                    {
                        Console.WriteLine("No default workflow found, trying to get first active workflow...");
                        var activeWorkflows = await _workflowRepository.GetActiveWorkflowsAsync();
                        var firstActiveWorkflow = activeWorkflows?.FirstOrDefault();

                        if (firstActiveWorkflow != null)
                        {
                            input.WorkflowId = firstActiveWorkflow.Id;
                            Console.WriteLine($"Using first active workflow - ID: {firstActiveWorkflow.Id}, Name: {firstActiveWorkflow.Name}");
                        }
                        else
                        {
                            throw new CRMException(ErrorCodeEnum.DataNotFound, "No default or active workflow found. Please specify a valid WorkflowId or configure a default workflow.");
                        }
                    }
                }

                Console.WriteLine($"=== Creating onboarding for WorkflowId: {input.WorkflowId} ===");

                // Check if Lead ID already exists for this tenant with enhanced checking
                Console.WriteLine($"Checking for existing onboarding - TenantId: '{tenantId}', LeadId: '{input.LeadId}'");

                // Use SqlSugar client directly for more precise checking
                var sqlSugarClient = _onboardingRepository.GetSqlSugarClient();
                var existingActiveOnboarding = await sqlSugarClient.Queryable<Onboarding>()
                    .Where(x => x.TenantId == tenantId &&
                               x.LeadId == input.LeadId &&
                               x.IsValid == true &&
                               x.IsActive == true)
                    .FirstAsync();

                if (existingActiveOnboarding != null)
                {
                    Console.WriteLine($"Found existing active onboarding - ID: {existingActiveOnboarding.Id}, Status: {existingActiveOnboarding.Status}");
                    throw new CRMException(ErrorCodeEnum.BusinessError,
                        $"An active onboarding already exists for Lead ID '{input.LeadId}' in tenant '{tenantId}'. " +
                        $"Existing onboarding ID: {existingActiveOnboarding.Id}, Status: {existingActiveOnboarding.Status}");
                }

                Console.WriteLine($"Lead ID check passed: {input.LeadId}");

                // Validate workflow exists with detailed logging
                Console.WriteLine($"Attempting to find workflow with ID: {input.WorkflowId.Value}");
                var workflow = await _workflowRepository.GetByIdAsync(input.WorkflowId.Value);
                Console.WriteLine($"Workflow query result: {(workflow != null ? "Found" : "Not Found")}");

                if (workflow != null)
                {
                    Console.WriteLine($"Workflow details - ID: {workflow.Id}, Name: {workflow.Name}, IsValid: {workflow.IsValid}, IsActive: {workflow.IsActive}");
                }

                if (workflow == null || !workflow.IsValid)
                {
                    // Try to get all workflows to see what's available
                    Console.WriteLine("=== Diagnostic: Getting all available workflows ===");
                    try
                    {
                        var allWorkflows = await _workflowRepository.GetListAsync(x => x.IsValid);
                        Console.WriteLine($"Available workflows count: {allWorkflows.Count}");
                        foreach (var w in allWorkflows.Take(5)) // Show first 5
                        {
                            Console.WriteLine($"  - ID: {w.Id}, Name: {w.Name}, IsActive: {w.IsActive}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to get all workflows: {ex.Message}");
                    }

                    throw new CRMException(ErrorCodeEnum.DataNotFound, $"Workflow not found for ID: {input.WorkflowId.Value}");
                }

                // Get first stage of the workflow with detailed logging
                Console.WriteLine($"Getting stages for workflow ID: {input.WorkflowId.Value}");
                var stages = await _stageRepository.GetByWorkflowIdAsync(input.WorkflowId.Value);
                Console.WriteLine($"Stages found: {stages.Count}");

                var firstStage = stages.OrderBy(x => x.Order).FirstOrDefault();
                if (firstStage == null)
                {
                    Console.WriteLine("=== No stages found - trying to create a default stage ===");
                    // Instead of failing, let's allow creation without stages and warn
                    Console.WriteLine("WARNING: Workflow has no stages configured. Creating onboarding without initial stage.");
                }
                else
                {
                    Console.WriteLine($"First stage - ID: {firstStage.Id}, Name: {firstStage.Name}, Order: {firstStage.Order}");
                }

                // Create new onboarding entity
                var entity = _mapper.Map<Onboarding>(input);

                Console.WriteLine("=== Before setting entity properties ===");
                Console.WriteLine($"Mapped entity - LeadId: {entity.LeadId}, WorkflowId: {entity.WorkflowId}");

                // Set initial values with explicit null checks
                entity.CurrentStageId = firstStage?.Id;
                entity.CurrentStageOrder = firstStage?.Order ?? 0;
                entity.Status = string.IsNullOrEmpty(entity.Status) ? "Started" : entity.Status;
                entity.StartDate = entity.StartDate ?? DateTimeOffset.UtcNow;
                entity.CurrentStageStartTime = DateTimeOffset.UtcNow;
                entity.CompletionRate = 0;
                entity.IsPrioritySet = false;
                entity.Priority = string.IsNullOrEmpty(entity.Priority) ? "Medium" : entity.Priority;
                entity.IsActive = true;

                // Initialize create information with proper ID and timestamps
                entity.InitCreateInfo(_userContext);

                Console.WriteLine("=== After entity initialization ===");
                Console.WriteLine($"Entity ID: {entity.Id}, TenantId: {entity.TenantId}");
                Console.WriteLine($"CreateDate: {entity.CreateDate}, CreateBy: {entity.CreateBy}");

                // Generate unique ID if not set
                if (entity.Id == 0)
                {
                    entity.InitNewId();
                }

                Console.WriteLine("=== After setting entity properties ===");
                Console.WriteLine($"Entity validation - Id: {entity.Id}, TenantId: '{entity.TenantId}', LeadId: '{entity.LeadId}'");
                Console.WriteLine($"Status: '{entity.Status}', Priority: '{entity.Priority}', IsValid: {entity.IsValid}");
                Console.WriteLine($"CreateBy: '{entity.CreateBy}', CreateDate: {entity.CreateDate}");
                Console.WriteLine($"WorkflowId: {entity.WorkflowId}, CurrentStageId: {entity.CurrentStageId}");

                // Validate entity before insertion
                if (entity.WorkflowId <= 0)
                {
                    throw new CRMException(ErrorCodeEnum.ParamInvalid, "WorkflowId must be greater than 0");
                }

                if (string.IsNullOrWhiteSpace(entity.LeadId))
                {
                    throw new CRMException(ErrorCodeEnum.ParamInvalid, "LeadId cannot be null or empty");
                }

                if (string.IsNullOrWhiteSpace(entity.TenantId))
                {
                    throw new CRMException(ErrorCodeEnum.ParamInvalid, "TenantId cannot be null or empty");
                }

                // Ensure Id is generated (SqlSugar expects this for some operations)
                if (entity.Id == 0)
                {
                    entity.Id = SnowFlakeSingle.Instance.NextId();
                    Console.WriteLine($"Generated new Id for entity: {entity.Id}");
                }

                Console.WriteLine("=== Final entity state before insertion ===");
                Console.WriteLine($"Id: {entity.Id}");
                Console.WriteLine($"TenantId: '{entity.TenantId}'");
                Console.WriteLine($"WorkflowId: {entity.WorkflowId}");
                Console.WriteLine($"CurrentStageId: {entity.CurrentStageId}");
                Console.WriteLine($"LeadId: '{entity.LeadId}'");
                Console.WriteLine($"LeadName: '{entity.LeadName}'");
                Console.WriteLine($"Status: '{entity.Status}'");
                Console.WriteLine($"IsValid: {entity.IsValid}");
                Console.WriteLine($"IsActive: {entity.IsActive}");
                Console.WriteLine($"CreateDate: {entity.CreateDate}");

                Console.WriteLine("Inserting onboarding entity...");

                // Use a completely simplified approach to avoid SqlSugar issues
                long insertedId;

                try
                {
                    // Generate snowflake ID for the entity
                    entity.InitNewId();
                    Console.WriteLine($"Generated snowflake ID: {entity.Id}");

                    // Create table if not exists first
                    Console.WriteLine("Ensuring table structure...");
                    if (!sqlSugarClient.DbMaintenance.IsAnyTable("ff_onboarding"))
                    {
                        Console.WriteLine("Creating ff_onboarding table...");
                        sqlSugarClient.CodeFirst.SetStringDefaultLength(200).InitTables<Onboarding>();
                    }

                    // Use simple insert and then get the last inserted ID
                    Console.WriteLine("Performing simple insert...");
                    var insertResult = await sqlSugarClient.Insertable(entity).ExecuteCommandAsync();

                    if (insertResult > 0)
                    {
                        Console.WriteLine($"Insert successful, {insertResult} rows affected. Getting last inserted ID...");

                        // Get the last inserted record by combining unique fields
                        var lastInserted = await sqlSugarClient.Queryable<Onboarding>()
                            .Where(x => x.LeadId == entity.LeadId &&
                                       x.WorkflowId == entity.WorkflowId &&
                                       x.TenantId == entity.TenantId)
                            .OrderByDescending(x => x.CreateDate)
                            .FirstAsync();

                        if (lastInserted != null)
                        {
                            insertedId = lastInserted.Id;
                            Console.WriteLine($"=== Insert successful. ID: {insertedId} ===");
                        }
                        else
                        {
                            Console.WriteLine("Insert successful but could not retrieve the inserted record. Assuming success with ID 0.");
                            // If insert was successful but we can't find the record, still return success
                            // This might happen due to timing or indexing issues
                            insertedId = 0; // Return 0 to indicate success but unknown ID
                            Console.WriteLine($"=== Insert assumed successful. ID: {insertedId} ===");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Insert failed - no rows affected");
                        throw new CRMException(ErrorCodeEnum.SystemError, "Insert failed - no rows were affected");
                    }
                }
                catch (Exception insertEx)
                {
                    Console.WriteLine($"=== Simple insert failed ===");
                    Console.WriteLine($"Exception Type: {insertEx.GetType().Name}");
                    Console.WriteLine($"Message: {insertEx.Message}");
                    Console.WriteLine($"StackTrace: {insertEx.StackTrace}");

                    // Check if this is a duplicate key error
                    if (insertEx.Message.Contains("23505") && insertEx.Message.Contains("idx_ff_onboarding_unique_lead"))
                    {
                        // This is specifically the unique constraint violation we're dealing with
                        Console.WriteLine("=== Detected unique constraint violation for lead ===");
                        throw new CRMException(ErrorCodeEnum.BusinessError,
                            $"A duplicate onboarding record was detected for Lead ID '{entity.LeadId}' in tenant '{entity.TenantId}'. " +
                            $"This may be due to concurrent requests or an existing active record. Please check existing onboardings and try again.");
                    }

                    // Final fallback: manual SQL with minimal fields
                    Console.WriteLine("=== Attempting minimal SQL insertion as final fallback ===");
                    try
                    {
                        var sql = @"
                            INSERT INTO ff_onboarding (
                                tenant_id, is_valid, create_date, modify_date, create_by, modify_by,
                                create_user_id, modify_user_id, workflow_id, current_stage_order,
                                lead_id, lead_name, lead_email, lead_phone, status, completion_rate,
                                priority, is_priority_set, notes, is_active
                            ) VALUES (
                                @TenantId, @IsValid, @CreateDate, @ModifyDate, @CreateBy, @ModifyBy,
                                @CreateUserId, @ModifyUserId, @WorkflowId, @CurrentStageOrder,
                                @LeadId, @LeadName, @LeadEmail, @LeadPhone, @Status, @CompletionRate,
                                @Priority, @IsPrioritySet, @Notes, @IsActive
                            ) RETURNING id";

                        var parameters = new
                        {
                            TenantId = entity.TenantId,
                            IsValid = true,
                            CreateDate = DateTimeOffset.UtcNow,
                            ModifyDate = DateTimeOffset.UtcNow,
                            CreateBy = GetCurrentUserName(),
                            ModifyBy = GetCurrentUserName(),
                            CreateUserId = 0L,
                            ModifyUserId = 0L,
                            WorkflowId = entity.WorkflowId,
                            CurrentStageOrder = entity.CurrentStageOrder,
                            LeadId = entity.LeadId,
                            LeadName = entity.LeadName,
                            LeadEmail = entity.LeadEmail,
                            LeadPhone = entity.LeadPhone,
                            Status = entity.Status,
                            CompletionRate = entity.CompletionRate,
                            Priority = entity.Priority,
                            IsPrioritySet = entity.IsPrioritySet,
                            Notes = entity.Notes ?? "",
                            IsActive = entity.IsActive
                        };

                        Console.WriteLine("Executing minimal SQL...");
                        insertedId = await sqlSugarClient.Ado.SqlQuerySingleAsync<long>(sql, parameters);
                        Console.WriteLine($"=== Minimal SQL insert successful. ID: {insertedId} ===");
                    }
                    catch (Exception sqlEx)
                    {
                        Console.WriteLine($"=== Minimal SQL insert also failed: {sqlEx.Message} ===");
                        Console.WriteLine($"SQL Stack trace: {sqlEx.StackTrace}");

                        // Check if this is also a duplicate key error
                        if (sqlEx.Message.Contains("23505") && sqlEx.Message.Contains("idx_ff_onboarding_unique_lead"))
                        {
                            throw new CRMException(ErrorCodeEnum.BusinessError,
                                $"A duplicate onboarding record exists for Lead ID '{entity.LeadId}' in tenant '{entity.TenantId}'. " +
                                $"Please check existing onboardings and ensure the Lead ID is unique within the tenant.");
                        }

                        throw new CRMException(ErrorCodeEnum.SystemError,
                            $"All insertion methods failed. Simple insert: {insertEx.Message}, Minimal SQL: {sqlEx.Message}");
                    }
                }

                // Initialize stage progress after successful creation
                if (insertedId > 0)
                {
                    try
                    {
                        // Re-fetch the inserted entity to ensure we have complete data
                        var insertedEntity = await _onboardingRepository.GetByIdAsync(insertedId);
                        if (insertedEntity != null)
                        {
                            // Initialize stage progress
                            await InitializeStagesProgressAsync(insertedEntity, stages);
                            
                            // Update entity to save stage progress
                            await _onboardingRepository.UpdateAsync(insertedEntity);
                            
                            Console.WriteLine("Stages progress initialized successfully");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Failed to initialize stages progress: {ex.Message}");
                        // Don't throw exception as the main creation operation has already succeeded
                    }
                    
                    // Clear query cache (async execution, doesn't affect main flow)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await ClearOnboardingQueryCacheAsync();
                            Console.WriteLine("Query cache cleared after onboarding creation");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Cache cleanup failed during creation: {ex.Message}");
                        }
                    });
                }

                return insertedId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== Error creating onboarding ===");
                Console.WriteLine($"Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Source: {ex.Source}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"InnerException: {ex.InnerException.Message}");
                }
                Console.WriteLine("===============================");
                throw;
            }
        }

        /// <summary>
        /// Update an existing onboarding
        /// </summary>
        public async Task<bool> UpdateAsync(long id, OnboardingInputDto input)
        {
            try
            {
                Console.WriteLine($"=== Updating onboarding ID: {id} ===");

                var entity = await _onboardingRepository.GetByIdAsync(id);
                if (entity == null)
                {
                    Console.WriteLine($"Onboarding not found for ID: {id}");
                    throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
                }

                Console.WriteLine($"Found onboarding - Current Workflow: {entity.WorkflowId}, New Workflow: {input.WorkflowId}");

                // Record original workflow and stage ID for cache cleanup
                var originalWorkflowId = entity.WorkflowId;
                var originalStageId = entity.CurrentStageId;

                // If workflow changed, validate new workflow and reset stages
                if (entity.WorkflowId != input.WorkflowId)
                {
                    Console.WriteLine("Workflow changed - validating new workflow...");
                    var workflow = await _workflowRepository.GetByIdAsync(input.WorkflowId);
                    if (workflow == null)
                    {
                        Console.WriteLine($"New workflow not found: {input.WorkflowId}");
                        throw new CRMException(ErrorCodeEnum.DataNotFound, "Workflow not found");
                    }

                    Console.WriteLine("Getting stages for new workflow...");
                    var stages = await _stageRepository.GetByWorkflowIdAsync(input.WorkflowId.Value);
                    var firstStage = stages.OrderBy(x => x.Order).FirstOrDefault();

                    Console.WriteLine($"First stage for new workflow: {(firstStage != null ? firstStage.Name : "None")}");

                    entity.CurrentStageId = firstStage?.Id;
                    entity.CurrentStageOrder = firstStage?.Order ?? 1;
                    entity.CompletionRate = 0;
                }

                // Map the input to entity (this will update all the mappable fields)
                Console.WriteLine("Mapping input to entity...");
                _mapper.Map(input, entity);

                // Update system fields
                entity.ModifyDate = DateTimeOffset.UtcNow;
                entity.ModifyBy = GetCurrentUserName();
                entity.ModifyUserId = 0; // TODO: Get from user context

                Console.WriteLine("Calling repository update...");
                var result = await _onboardingRepository.UpdateAsync(entity);

                // Log onboarding update and clear cache
                if (result)
                {
                    await LogOnboardingActionAsync(entity, "Update Onboarding", "onboarding_update", true, new
                    {
                        UpdatedFields = new
                        {
                            input.LeadName,
                            input.Priority,
                            input.CurrentAssigneeName,
                            input.CurrentTeam,
                            input.Notes,
                            input.CustomFieldsJson
                        },
                        UpdatedBy = GetCurrentUserName(),
                        UpdatedAt = DateTimeOffset.UtcNow
                    });

                    // Clear related cache data (async execution, doesn't affect main flow)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var cacheCleanupTasks = new List<Task>();

                            // Clear query cache
                            cacheCleanupTasks.Add(ClearOnboardingQueryCacheAsync());

                            // If workflow changed, clear cache for both original and new workflow
                            if (originalWorkflowId != entity.WorkflowId)
                            {
                                cacheCleanupTasks.Add(ClearRelatedCacheAsync(originalWorkflowId));
                                cacheCleanupTasks.Add(ClearRelatedCacheAsync(entity.WorkflowId));
                            }
                            else
                            {
                                cacheCleanupTasks.Add(ClearRelatedCacheAsync(entity.WorkflowId));
                            }

                            // If stage changed, clear related stage cache
                            if (originalStageId != entity.CurrentStageId)
                            {
                                if (originalStageId.HasValue)
                                {
                                    cacheCleanupTasks.Add(ClearRelatedCacheAsync(null, originalStageId.Value));
                                }
                                if (entity.CurrentStageId.HasValue)
                                {
                                    cacheCleanupTasks.Add(ClearRelatedCacheAsync(null, entity.CurrentStageId.Value));
                                }
                            }

                            await Task.WhenAll(cacheCleanupTasks);
                            Console.WriteLine("Cache cleanup completed for onboarding update");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Cache cleanup failed during update: {ex.Message}");
                        }
                    });
                }

                Console.WriteLine($"=== Update result: {result} ===");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== Error updating onboarding ===");
                Console.WriteLine($"Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Source: {ex.Source}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"InnerException: {ex.InnerException.Message}");
                }
                Console.WriteLine("===============================");
                throw;
            }
        }

        /// <summary>
        /// Delete an onboarding (with confirmation)
        /// </summary>
        public async Task<bool> DeleteAsync(long id, bool confirm = false)
        {
            if (!confirm)
            {
                throw new CRMException(ErrorCodeEnum.OperationNotAllowed, "Delete operation requires confirmation");
            }

            Console.WriteLine($"=== DeleteAsync Debug Info ===");
            Console.WriteLine($"Requested ID: {id}");
            Console.WriteLine($"Current User TenantId: {_userContext.TenantId}");
            Console.WriteLine($"Current User ID: {_userContext.UserId}");

            // First try to query without tenant filter to see if record actually exists
            Onboarding entityWithoutFilter = null;
            try
            {
                var sqlSugarClient = _onboardingRepository.GetSqlSugarClient();
                sqlSugarClient.QueryFilter.ClearAndBackup();
                entityWithoutFilter = await sqlSugarClient.Queryable<Onboarding>()
                    .Where(x => x.Id == id)
                    .FirstAsync();
                sqlSugarClient.QueryFilter.Restore();

                if (entityWithoutFilter != null)
                {
                    Console.WriteLine($"Found record without tenant filter:");
                    Console.WriteLine($"  - ID: {entityWithoutFilter.Id}");
                    Console.WriteLine($"  - TenantId: '{entityWithoutFilter.TenantId}'");
                    Console.WriteLine($"  - IsValid: {entityWithoutFilter.IsValid}");
                    Console.WriteLine($"  - LeadName: {entityWithoutFilter.LeadName}");
                }
                else
                {
                    Console.WriteLine($"Record with ID {id} does not exist in database");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking record without filter: {ex.Message}");
            }

            // Query using normal repository method (with tenant filter)
            var entity = await _onboardingRepository.GetByIdAsync(id);
            Console.WriteLine($"Repository GetByIdAsync result: {(entity != null ? $"Found entity ID {entity.Id}" : "No entity found with tenant filter")}");

            if (entity == null || !entity.IsValid)
            {
                // If record exists but tenant doesn't match, provide more detailed error information
                if (entityWithoutFilter != null)
                {
                    if (entityWithoutFilter.TenantId != _userContext.TenantId)
                    {
                        Console.WriteLine($"Tenant mismatch: Record TenantId='{entityWithoutFilter.TenantId}', User TenantId='{_userContext.TenantId}'");
                        throw new CRMException(ErrorCodeEnum.DataNotFound, $"Onboarding not found or access denied. Record belongs to different tenant.");
                    }
                    else if (!entityWithoutFilter.IsValid)
                    {
                        Console.WriteLine($"Record is soft deleted (IsValid=false)");
                        throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding has already been deleted");
                    }
                }

                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            // Use soft delete instead of hard delete
            entity.IsValid = false;
            entity.ModifyDate = DateTimeOffset.UtcNow;
            entity.ModifyBy = GetCurrentUserName();
            entity.ModifyUserId = GetCurrentUserId() ?? 0;

            var result = await _onboardingRepository.UpdateAsync(entity);

            // Clear related cache after successful deletion
            if (result)
            {
                await ClearOnboardingQueryCacheAsync();
                await ClearRelatedCacheAsync(entity.WorkflowId, entity.CurrentStageId);
            }

            return result;
        }

        /// <summary>
        /// Get onboarding by ID
        /// </summary>
        public async Task<OnboardingOutputDto> GetByIdAsync(long id)
        {
            try
            {
                var entity = await _onboardingRepository.GetByIdAsync(id);
                if (entity == null)
                {
                    throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
                }

                // Load stages progress from JSON
                LoadStagesProgressFromJson(entity);

                // If stage progress is empty, try to initialize it
                if (entity.StagesProgress == null || !entity.StagesProgress.Any())
                {
                    Console.WriteLine("Stages progress is empty, attempting to initialize...");
                    try
                    {
                        var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
                        if (stages != null && stages.Any())
                        {
                            await InitializeStagesProgressAsync(entity, stages);
                            await _onboardingRepository.UpdateAsync(entity);
                            Console.WriteLine($"Initialized {entity.StagesProgress?.Count ?? 0} stages progress entries");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to initialize stages progress: {ex.Message}");
                    }
                }

                var result = _mapper.Map<OnboardingOutputDto>(entity);

                // Get workflow name
                var workflow = await _workflowRepository.GetByIdAsync(entity.WorkflowId);
                result.WorkflowName = workflow?.Name;

                // Get current stage name
                if (entity.CurrentStageId.HasValue)
                {
                    var stage = await _stageRepository.GetByIdAsync(entity.CurrentStageId.Value);
                    result.CurrentStageName = stage?.Name;
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new CRMException(ErrorCodeEnum.SystemError, $"Error getting onboarding by ID {id}: {ex.Message}");
            }
        }

        /// <summary>
        /// Get onboarding list
        /// </summary>
        public async Task<List<OnboardingOutputDto>> GetListAsync()
        {
            var entities = await _onboardingRepository.GetListAsync(x => x.IsValid);

            // Load stages progress from JSON for each entity
            foreach (var entity in entities)
            {
                LoadStagesProgressFromJson(entity);
            }

            return _mapper.Map<List<OnboardingOutputDto>>(entities);
        }

        /// <summary>
        /// Query onboarding with pagination
        /// </summary>
        public async Task<PageModelDto<OnboardingOutputDto>> QueryAsync(OnboardingQueryRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            var tenantId = _userContext?.TenantId ?? "default";

            try
            {
                Console.WriteLine($"[QUERY] Starting query for tenant: {tenantId}");

                // Build query conditions list - using safe BaseRepository approach
                var whereExpressions = new List<Expression<Func<Onboarding, bool>>>();
                
                // Basic filter conditions
                whereExpressions.Add(x => x.IsValid == true);
                whereExpressions.Add(x => x.TenantId.ToLower() == tenantId.ToLower());

                // Apply filter conditions
                if (request.WorkflowId.HasValue && request.WorkflowId.Value > 0)
                {
                    whereExpressions.Add(x => x.WorkflowId == request.WorkflowId.Value);
                }

                if (request.CurrentStageId.HasValue && request.CurrentStageId.Value > 0)
                {
                    whereExpressions.Add(x => x.CurrentStageId == request.CurrentStageId.Value);
                }

                if (!string.IsNullOrEmpty(request.LeadId) && request.LeadId != "string")
                {
                    whereExpressions.Add(x => x.LeadId.Contains(request.LeadId));
                }

                // Support batch LeadIds query for performance optimization
                if (request.LeadIds?.Any() == true)
                {
                    whereExpressions.Add(x => request.LeadIds.Contains(x.LeadId));
                }

                if (!string.IsNullOrEmpty(request.LeadName) && request.LeadName != "string")
                {
                    whereExpressions.Add(x => x.LeadName.Contains(request.LeadName));
                }

                if (request.LifeCycleStageId.HasValue && request.LifeCycleStageId.Value > 0)
                {
                    whereExpressions.Add(x => x.LifeCycleStageId == request.LifeCycleStageId.Value);
                }

                if (!string.IsNullOrEmpty(request.LifeCycleStageName) && request.LifeCycleStageName != "string")
                {
                    whereExpressions.Add(x => x.LifeCycleStageName.Contains(request.LifeCycleStageName));
                }

                if (!string.IsNullOrEmpty(request.Priority) && request.Priority != "string")
                {
                    whereExpressions.Add(x => x.Priority == request.Priority);
                }

                if (!string.IsNullOrEmpty(request.Status) && request.Status != "string")
                {
                    whereExpressions.Add(x => x.Status == request.Status);
                }

                if (request.IsActive.HasValue)
                {
                    whereExpressions.Add(x => x.IsActive == request.IsActive.Value);
                }

                if (!string.IsNullOrEmpty(request.UpdatedBy) && request.UpdatedBy != "string")
                {
                    whereExpressions.Add(x => x.ModifyBy.Contains(request.UpdatedBy));
                }

                if (request.UpdatedByUserId.HasValue && request.UpdatedByUserId.Value > 0)
                {
                    whereExpressions.Add(x => x.ModifyUserId == request.UpdatedByUserId.Value);
                }

                if (!string.IsNullOrEmpty(request.CreatedBy) && request.CreatedBy != "string")
                {
                    whereExpressions.Add(x => x.CreateBy.Contains(request.CreatedBy));
                }

                if (request.CreatedByUserId.HasValue && request.CreatedByUserId.Value > 0)
                {
                    whereExpressions.Add(x => x.CreateUserId == request.CreatedByUserId.Value);
                }

                // Determine sort field and direction
                Expression<Func<Onboarding, object>> orderByExpression = GetOrderByExpression(request);
                bool isAsc = GetSortDirection(request);

                // Apply pagination
                var pageIndex = Math.Max(1, request.PageIndex > 0 ? request.PageIndex : 1);
                var pageSize = Math.Max(1, Math.Min(100, request.PageSize > 0 ? request.PageSize : 10));

                // Use BaseRepository's safe pagination method
                var (pagedEntities, totalCount) = await _onboardingRepository.GetPageListAsync(
                    whereExpressions,
                    pageIndex,
                    pageSize,
                    orderByExpression,
                    isAsc
                );

                // Batch get Workflow and Stage information to avoid N+1 queries
                var (workflows, stages) = await GetRelatedDataBatchOptimizedAsync(pagedEntities);

                // Create lookup dictionaries to improve search performance
                var workflowDict = workflows.ToDictionary(w => w.Id, w => w.Name);
                var stageDict = stages.ToDictionary(s => s.Id, s => s.Name);

                // Batch process JSON deserialization
                ProcessStagesProgressParallel(pagedEntities);

                // Map to output DTOs
                var results = _mapper.Map<List<OnboardingOutputDto>>(pagedEntities);

                // Use dictionaries to quickly populate workflow and stage names
                foreach (var result in results)
                {
                    result.WorkflowName = workflowDict.GetValueOrDefault(result.WorkflowId);
                    if (result.CurrentStageId.HasValue)
                    {
                        result.CurrentStageName = stageDict.GetValueOrDefault(result.CurrentStageId.Value);
                    }
                }

                var pageModel = new PageModelDto<OnboardingOutputDto>(pageIndex, pageSize, results, totalCount);

                // Record performance statistics
                stopwatch.Stop();
                Console.WriteLine($"[QUERY] Query completed in {stopwatch.ElapsedMilliseconds}ms, returned {results.Count} records");

                return pageModel;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"[ERROR] Query failed after {stopwatch.ElapsedMilliseconds}ms: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                throw new CRMException(ErrorCodeEnum.SystemError, 
                    $"Error querying onboardings: {ex.Message}");
            }
        }

        /// <summary>
        /// Get sort expression
        /// </summary>
        private Expression<Func<Onboarding, object>> GetOrderByExpression(OnboardingQueryRequest request)
        {
            var sortBy = request.SortField?.ToLower() ?? "createdate";
            
            return sortBy switch
            {
                "id" => x => x.Id,
                "leadid" => x => x.LeadId,
                "leadname" => x => x.LeadName,
                "workflowid" => x => x.WorkflowId,
                "currentstageid" => x => x.CurrentStageId,
                "lifecyclestageid" => x => x.LifeCycleStageId,
                "lifecyclestagename" => x => x.LifeCycleStageName,
                "priority" => x => x.Priority,
                "status" => x => x.Status,
                "isactive" => x => x.IsActive,
                "completionrate" => x => x.CompletionRate,
                "createdate" => x => x.CreateDate,
                "modifydate" => x => x.ModifyDate,
                "createby" => x => x.CreateBy,
                "modifyby" => x => x.ModifyBy,
                _ => x => x.CreateDate
            };
        }

        /// <summary>
        /// Get sort direction
        /// </summary>
        private bool GetSortDirection(OnboardingQueryRequest request)
        {
            var sortDirection = request.SortDirection?.ToLower() ?? "desc";
            return sortDirection == "asc";
        }

        /// <summary>
        /// Build query cache key
        /// </summary>
        private string BuildQueryCacheKey(OnboardingQueryRequest request, string tenantId)
        {
            var keyParts = new List<string>
            {
                "ow:onboarding:query",
                tenantId,
                request.PageIndex.ToString(),
                request.PageSize.ToString(),
                request.WorkflowId?.ToString() ?? "null",
                request.CurrentStageId?.ToString() ?? "null",
                request.LeadId ?? "null",
                request.Status ?? "null",
                request.Priority ?? "null",
                request.SortField ?? "createdate",
                request.SortDirection ?? "desc"
            };
            return string.Join(":", keyParts);
        }

        /// <summary>
        /// 尝试从缓存获取查询结果
        /// </summary>
        private async Task<PageModelDto<OnboardingOutputDto>> TryGetCachedQueryResultAsync(string cacheKey)
        {
            try
            {
                // Redis缓存暂时禁用
                string cachedJson = null;
                if (!string.IsNullOrEmpty(cachedJson))
                {
                    var cached = JsonSerializer.Deserialize<PageModelDto<OnboardingOutputDto>>(cachedJson);
                    if (cached != null)
                    {
                        Console.WriteLine("[CACHE] Cache hit for onboarding query");
                        return cached;
                    }
                }
                Console.WriteLine("[CACHE] Cache miss for onboarding query");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cache get error: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 缓存查询结果
        /// </summary>
        private async Task CacheQueryResultAsync(string cacheKey, PageModelDto<OnboardingOutputDto> result, TimeSpan expiry)
        {
            try
            {
                var json = JsonSerializer.Serialize(result);
                // Redis缓存暂时禁用
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cache set error: {ex.Message}");
            }
        }

        /// <summary>
        /// 优化的总数查询
        /// </summary>
        private async Task<int> GetOptimizedTotalCountAsync(ISugarQueryable<Onboarding> queryable)
        {
            // 使用优化的计数查询，避免复杂的JOIN
            return await queryable.CountAsync();
        }

        /// <summary>
        /// 应用优化的排序
        /// </summary>
        private ISugarQueryable<Onboarding> ApplyOptimizedSorting(ISugarQueryable<Onboarding> queryable, OnboardingQueryRequest request)
        {
            switch (request.SortField?.ToLower())
            {
                case "leadname":
                    return request.SortDirection?.ToLower() == "asc"
                            ? queryable.OrderBy(x => x.LeadName)
                            : queryable.OrderByDescending(x => x.LeadName);
                case "status":
                    return request.SortDirection?.ToLower() == "asc"
                            ? queryable.OrderBy(x => x.Status)
                            : queryable.OrderByDescending(x => x.Status);
                case "completionrate":
                    return request.SortDirection?.ToLower() == "asc"
                            ? queryable.OrderBy(x => x.CompletionRate)
                            : queryable.OrderByDescending(x => x.CompletionRate);
                case "startdate":
                    return request.SortDirection?.ToLower() == "asc"
                            ? queryable.OrderBy(x => x.StartDate)
                            : queryable.OrderByDescending(x => x.StartDate);
                case "string":
                case null:
                case "":
                default:
                    return request.SortDirection?.ToLower() == "asc"
                            ? queryable.OrderBy(x => x.CreateDate)
                            : queryable.OrderByDescending(x => x.CreateDate);
            }
        }

        /// <summary>
        /// 优化的关联数据批量获取
        /// </summary>
        private async Task<(List<Workflow> workflows, List<Stage> stages)> GetRelatedDataBatchOptimizedAsync(List<Onboarding> entities)
        {
            var workflowIds = entities.Select(x => x.WorkflowId).Distinct().ToList();
            var stageIds = entities.Where(x => x.CurrentStageId.HasValue)
                    .Select(x => x.CurrentStageId.Value).Distinct().ToList();

            // 并行获取 workflows 和 stages 信息
            var workflowsTask = GetWorkflowsBatchAsync(workflowIds);
            var stagesTask = GetStagesBatchAsync(stageIds);

            await Task.WhenAll(workflowsTask, stagesTask);

            return (await workflowsTask, await stagesTask);
        }

        /// <summary>
        /// 并行处理阶段进度
        /// </summary>
        private void ProcessStagesProgressParallel(List<Onboarding> entities)
        {
            try
            {
                if (entities.Count <= 10)
                {
                    // 小数据集直接处理
                    foreach (var entity in entities)
                    {
                        LoadStagesProgressFromJson(entity);
                    }
                }
                else
                {
                    // 大数据集并行处理，但使用更安全的方式
                    // 创建实体的副本列表以避免集合修改异常
                    var entitiesCopy = entities.ToList();
                    Parallel.ForEach(entitiesCopy, new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, 4) // 限制并行度
                    }, entity =>
                    {
                        try
                        {
                            LoadStagesProgressFromJson(entity);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing stages progress for entity {entity.Id}: {ex.Message}");
                            // 确保即使单个实体处理失败，也不影响其他实体
                            entity.StagesProgress = new List<OnboardingStageProgress>();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ProcessStagesProgressParallel: {ex.Message}");
                // 如果并行处理失败，回退到顺序处理
                foreach (var entity in entities)
                {
                    try
                    {
                        LoadStagesProgressFromJson(entity);
                    }
                    catch (Exception entityEx)
                    {
                        Console.WriteLine($"Error processing entity {entity.Id}: {entityEx.Message}");
                        entity.StagesProgress = new List<OnboardingStageProgress>();
                    }
                }
            }
        }

        /// <summary>
        /// 批量获取 Workflows 信息，避免 N+1 查询，支持缓存
        /// </summary>
        private async Task<List<Workflow>> GetWorkflowsBatchAsync(List<long> workflowIds)
        {
            if (!workflowIds.Any())
                return new List<Workflow>();

            try
            {
                var workflows = new List<Workflow>();
                var uncachedIds = new List<long>();

                // 安全获取租户ID
                var tenantId = !string.IsNullOrEmpty(_userContext?.TenantId)
                    ? _userContext.TenantId.ToLowerInvariant()
                    : "default";

                // 先从缓存中获取
                foreach (var id in workflowIds)
                {
                    try
                    {
                        var cacheKey = $"{WORKFLOW_CACHE_PREFIX}:{tenantId}:{id}";
                        // Redis缓存暂时禁用
                        string cachedWorkflow = null;

                        if (!string.IsNullOrEmpty(cachedWorkflow))
                        {
                            var workflow = JsonSerializer.Deserialize<Workflow>(cachedWorkflow);
                            if (workflow != null)
                            {
                                workflows.Add(workflow);
                            }
                        }
                        else
                        {
                            uncachedIds.Add(id);
                        }
                    }
                    catch (Exception cacheEx)
                    {
                        Console.WriteLine($"Redis cache error for workflow {id}: {cacheEx.Message}");
                        uncachedIds.Add(id);
                    }
                }

                // 从数据库获取未缓存的数据 - 使用单独的连接避免并发冲突
                if (uncachedIds.Any())
                {
                    List<Workflow> dbWorkflows;
                    try
                    {
                        // 添加短暂延迟，避免连接冲突
                        await Task.Delay(10);
                        dbWorkflows = await _workflowRepository.GetListAsync(w => uncachedIds.Contains(w.Id) && w.IsValid);
                        workflows.AddRange(dbWorkflows);
                    }
                    catch (Exception dbEx) when (dbEx.Message.Contains("command is already in progress"))
                    {
                        Console.WriteLine($"[GetWorkflowsBatchAsync] Connection conflict detected, retrying...");
                        await Task.Delay(50); // 更长的延迟
                        dbWorkflows = await _workflowRepository.GetListAsync(w => uncachedIds.Contains(w.Id) && w.IsValid);
                        workflows.AddRange(dbWorkflows);
                    }
                    // 将新获取的数据缓存
                    var cacheExpiry = TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES);
                    var cacheTasks = dbWorkflows.Select(async workflow =>
                    {
                        try
                        {
                            var cacheKey = $"{WORKFLOW_CACHE_PREFIX}:{tenantId}:{workflow.Id}";
                            var serializedWorkflow = JsonSerializer.Serialize(workflow);
                            // Redis缓存暂时禁用
                            await Task.CompletedTask;
                        }
                        catch (Exception cacheEx)
                        {
                            Console.WriteLine($"Failed to cache workflow {workflow.Id}: {cacheEx.Message}");
                        }
                    });

                    await Task.WhenAll(cacheTasks);
                }

                return workflows;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to batch get workflows: {ex.Message}");
                // 如果缓存失败，直接从数据库获取
                return await _workflowRepository.GetListAsync(w => workflowIds.Contains(w.Id) && w.IsValid);
            }
        }

        /// <summary>
        /// 批量获取 Stages 信息，避免 N+1 查询，支持缓存
        /// </summary>
        private async Task<List<Stage>> GetStagesBatchAsync(List<long> stageIds)
        {
            if (!stageIds.Any())
                return new List<Stage>();

            try
            {
                var stages = new List<Stage>();
                var uncachedIds = new List<long>();

                // 安全获取租户ID
                var tenantId = !string.IsNullOrEmpty(_userContext?.TenantId)
                    ? _userContext.TenantId.ToLowerInvariant()
                    : "default";

                // 先从缓存中获取
                foreach (var id in stageIds)
                {
                    try
                    {
                        var cacheKey = $"{STAGE_CACHE_PREFIX}:{tenantId}:{id}";
                        // Redis缓存暂时禁用
                        string cachedStage = null;

                        if (!string.IsNullOrEmpty(cachedStage))
                        {
                            var stage = JsonSerializer.Deserialize<Stage>(cachedStage);
                            if (stage != null)
                            {
                                stages.Add(stage);
                            }
                        }
                        else
                        {
                            uncachedIds.Add(id);
                        }
                    }
                    catch (Exception cacheEx)
                    {
                        Console.WriteLine($"Redis cache error for stage {id}: {cacheEx.Message}");
                        uncachedIds.Add(id);
                    }
                }

                // 从数据库获取未缓存的数据 - 使用单独的连接避免并发冲突
                if (uncachedIds.Any())
                {
                    List<Stage> dbStages;
                    try
                    {
                        // 添加短暂延迟，避免连接冲突
                        await Task.Delay(15);
                        dbStages = await _stageRepository.GetListAsync(s => uncachedIds.Contains(s.Id) && s.IsValid);
                        stages.AddRange(dbStages);
                    }
                    catch (Exception dbEx) when (dbEx.Message.Contains("command is already in progress"))
                    {
                        Console.WriteLine($"[GetStagesBatchAsync] Connection conflict detected, retrying...");
                        await Task.Delay(75); // 更长的延迟
                        dbStages = await _stageRepository.GetListAsync(s => uncachedIds.Contains(s.Id) && s.IsValid);
                        stages.AddRange(dbStages);
                    }
                    // 将新获取的数据缓存
                    var cacheExpiry = TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES);
                    var cacheTasks = dbStages.Select(async stage =>
                    {
                        try
                        {
                            var cacheKey = $"{STAGE_CACHE_PREFIX}:{tenantId}:{stage.Id}";
                            var serializedStage = JsonSerializer.Serialize(stage);
                            // Redis缓存暂时禁用
                            await Task.CompletedTask;
                        }
                        catch (Exception cacheEx)
                        {
                            Console.WriteLine($"Failed to cache stage {stage.Id}: {cacheEx.Message}");
                        }
                    });

                    await Task.WhenAll(cacheTasks);
                }

                return stages;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to batch get stages: {ex.Message}");
                // 如果缓存失败，直接从数据库获取
                return await _stageRepository.GetListAsync(s => stageIds.Contains(s.Id) && s.IsValid);
            }
        }

        /// <summary>
        /// Check if leads have onboarding (batch operation)
        /// </summary>
        public async Task<Dictionary<string, bool>> BatchCheckLeadOnboardingAsync(List<string> leadIds)
        {
            var result = new Dictionary<string, bool>();

            if (leadIds == null || !leadIds.Any())
            {
                return result;
            }

            try
            {
                // 批量查询所有Lead的Onboarding记录
                var onboardings = await _onboardingRepository.GetByLeadIdsAsync(leadIds);
                var existingLeadIds = onboardings.Select(o => o.LeadId).ToHashSet();

                // 为所有Lead设置状态
                foreach (var leadId in leadIds)
                {
                    result[leadId] = existingLeadIds.Contains(leadId);
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in batch checking lead onboarding: {ex.Message}");

                // 如果批量查询失败，为所有Lead设置默认值
                foreach (var leadId in leadIds)
                {
                    result[leadId] = false;
                }

                return result;
            }
        }

        /// <summary>
        /// Move to next stage
        /// </summary>
        public async Task<bool> MoveToNextStageAsync(long id)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
            var orderedStages = stages.OrderBy(x => x.Order).ToList();
            var currentStageIndex = orderedStages.FindIndex(x => x.Id == entity.CurrentStageId);

            if (currentStageIndex == -1 || currentStageIndex >= orderedStages.Count - 1)
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "No next stage available");
            }

            var nextStage = orderedStages[currentStageIndex + 1];
            await _onboardingRepository.UpdateStageAsync(id, nextStage.Id, nextStage.Order);

            // Update status to InProgress if it was Started
            if (entity.Status == "Started")
            {
                await _onboardingRepository.UpdateStatusAsync(id, "InProgress");
            }

            return true;
        }

        /// <summary>
        /// Move to previous stage
        /// </summary>
        public async Task<bool> MoveToPreviousStageAsync(long id)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
            var orderedStages = stages.OrderBy(x => x.Order).ToList();
            var currentStageIndex = orderedStages.FindIndex(x => x.Id == entity.CurrentStageId);

            if (currentStageIndex <= 0)
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "No previous stage available");
            }

            var previousStage = orderedStages[currentStageIndex - 1];
            return await _onboardingRepository.UpdateStageAsync(id, previousStage.Id, previousStage.Order);
        }

        /// <summary>
        /// Move to specific stage
        /// </summary>
        public async Task<bool> MoveToStageAsync(long id, MoveToStageInputDto input)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            var stage = await _stageRepository.GetByIdAsync(input.StageId);
            if (stage == null || !stage.IsValid || stage.WorkflowId != entity.WorkflowId)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Stage not found or not belongs to current workflow");
            }

            return await _onboardingRepository.UpdateStageAsync(id, stage.Id, stage.Order);
        }

        /// <summary>
        /// Complete current stage
        /// </summary>
        public async Task<bool> CompleteCurrentStageAsync(long id)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            Console.WriteLine($"=== CompleteCurrentStageAsync Debug ===");
            Console.WriteLine($"Onboarding ID: {id}");
            Console.WriteLine($"Current Stage Order: {entity.CurrentStageOrder}");
            Console.WriteLine($"Current Stage ID: {entity.CurrentStageId}");
            Console.WriteLine($"Current Status: {entity.Status}");
            Console.WriteLine($"Current Completion Rate: {entity.CompletionRate}");

            // Check if onboarding is already completed
            if (entity.Status == "Completed")
            {
                Console.WriteLine("Onboarding is already completed, returning success response");
                Console.WriteLine("Note: This is handled gracefully instead of throwing an exception");
                return true; // Return success since the desired outcome (completion) is already achieved
            }

            // 已移除Stage 1完成前必须设置优先级的验证
            // if (entity.CurrentStageOrder == 1 && !entity.IsPrioritySet)
            // {
            //     throw new CRMException(ErrorCodeEnum.BusinessError, "Priority must be set before completing Stage 1. Please set priority first.");
            // }

            // Get all stages for this workflow
            var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
            var orderedStages = stages.OrderBy(x => x.Order).ToList();
            var totalStages = orderedStages.Count;

            Console.WriteLine($"Total Stages: {totalStages}");
            Console.WriteLine($"Ordered Stages: {string.Join(", ", orderedStages.Select(s => $"{s.Order}:{s.Name}"))}");

            // Find current stage index
            var currentStageIndex = orderedStages.FindIndex(x => x.Id == entity.CurrentStageId);
            Console.WriteLine($"Current Stage Index: {currentStageIndex}");

            if (currentStageIndex == -1)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Current stage not found in workflow");
            }

            // Get current stage details
            var currentStage = orderedStages[currentStageIndex];
            Console.WriteLine($"Current Stage: {currentStage.Name} (Order: {currentStage.Order}, ID: {currentStage.Id})");

            // Check if current stage is already completed by comparing completion rate and stage progression
            var expectedCompletionRateForCurrentStage = totalStages > 0 ? Math.Round((decimal)currentStageIndex / totalStages * 100, 2) : 0;
            var expectedCompletionRateAfterCompletion = totalStages > 0 ? Math.Round((decimal)(currentStageIndex + 1) / totalStages * 100, 2) : 0;

            Console.WriteLine($"Expected completion rate for current stage: {expectedCompletionRateForCurrentStage}%");
            Console.WriteLine($"Expected completion rate after completion: {expectedCompletionRateAfterCompletion}%");
            Console.WriteLine($"Actual completion rate: {entity.CompletionRate}%");

            // More precise check: if completion rate is already at or above what it should be after completing this stage,
            // check if force completion is enabled
            if (entity.CompletionRate >= expectedCompletionRateAfterCompletion)
            {
                Console.WriteLine($"⚠️ WARNING: Stage appears to already be completed (completion rate {entity.CompletionRate}% >= expected {expectedCompletionRateAfterCompletion}%)");
                Console.WriteLine($"📝 Allowing re-completion of stage '{currentStage.Name}' for flexibility");
                // Always allow re-completion for flexibility, just log the warning
            }

            // Additional check: if the current stage order doesn't match the expected stage based on completion rate
            var expectedCurrentStageIndex = entity.CompletionRate > 0 ? (int)Math.Floor((decimal)entity.CompletionRate / 100 * totalStages) : 0;
            if (currentStageIndex < expectedCurrentStageIndex)
            {
                Console.WriteLine($"⚠️ WARNING: Stage progression inconsistency detected: current stage index {currentStageIndex}, but completion rate {entity.CompletionRate}% suggests stage index should be {expectedCurrentStageIndex}");
                Console.WriteLine($"📝 Allowing stage completion despite progression inconsistency for flexibility");
                // Don't throw exception, just log the warning and continue
            }

            // Check stage completion logs to see if this stage has already been completed
            Console.WriteLine($"Checking stage completion logs for onboarding {id} and stage {currentStage.Id}...");
            try
            {
                var stageCompletionLogs = await _stageCompletionLogRepository.GetByOnboardingAndStageAsync(id, currentStage.Id);
                var completionLogs = stageCompletionLogs.Where(log =>
                    log.LogType == "complete" &&
                    log.Success &&
                    log.Action.Contains("complete", StringComparison.OrdinalIgnoreCase)).ToList();

                Console.WriteLine($"Found {completionLogs.Count} successful completion logs for this stage");

                if (completionLogs.Any())
                {
                    var latestCompletionLog = completionLogs.OrderByDescending(log => log.CreateDate).First();
                    Console.WriteLine($"⚠️ WARNING: Stage '{currentStage.Name}' was already completed at {latestCompletionLog.CreateDate}");
                    Console.WriteLine($"📝 Allowing re-completion for flexibility - will update completion timestamp");
                    // Don't throw exception, allow re-completion but log the previous completion
                }

                Console.WriteLine("No previous completion logs found for this stage - proceeding with completion");
            }
            catch (CRMException)
            {
                // Re-throw CRM exceptions (our business logic exceptions)
                throw;
            }
            catch (Exception ex)
            {
                // Log but don't fail on stage completion log check errors
                Console.WriteLine($"Warning: Failed to check stage completion logs: {ex.Message}");
                Console.WriteLine("Proceeding with stage completion despite log check failure");
            }

            // Check if this is the last stage
            var isLastStage = currentStageIndex >= totalStages - 1;
            Console.WriteLine($"Is Last Stage: {isLastStage}");

            if (isLastStage)
            {
                // Complete the entire onboarding
                Console.WriteLine("Completing entire onboarding (last stage)");
                entity.Status = "Completed";
                entity.CompletionRate = 100;
                entity.ActualCompletionDate = DateTimeOffset.UtcNow;

                // Update stage tracking info
                await UpdateStageTrackingInfoAsync(entity);

                var result = await _onboardingRepository.UpdateAsync(entity);
                Console.WriteLine($"Final update result: {result}");

                // Publish stage completion event for final stage completion
                if (result)
                {
                    Console.WriteLine("Publishing OnboardingStageCompletedEvent for final stage completion");
                    await PublishStageCompletionEventForCurrentStageAsync(entity, currentStage, isLastStage);
                }

                return result;
            }
            else
            {
                // Move to next stage
                var nextStage = orderedStages[currentStageIndex + 1];
                Console.WriteLine($"Moving to next stage: {nextStage.Order}:{nextStage.Name} (ID: {nextStage.Id})");

                entity.CurrentStageId = nextStage.Id;
                entity.CurrentStageOrder = nextStage.Order;
                entity.CurrentStageStartTime = DateTimeOffset.UtcNow;

                // Update stages progress and calculate completion rate based on stage order progression
                await UpdateStagesProgressAsync(entity, currentStage.Id, GetCurrentUserName(), GetCurrentUserId(), "Stage completed via customer portal");
                LoadStagesProgressFromJson(entity);
                entity.CompletionRate = CalculateCompletionRateByStageOrder(entity.StagesProgress);

                // Log stage completion to Change Log
                await LogStageCompletionForCurrentStageAsync(entity, currentStage, GetCurrentUserName(), GetCurrentUserId(), "Stage completed via customer portal");

                var completedCount = entity.StagesProgress.Count(s => s.IsCompleted);
                Console.WriteLine($"Completed Stages: {completedCount}");
                Console.WriteLine($"New Completion Rate: {entity.CompletionRate}%");

                // Update status to InProgress if it was Started
                if (entity.Status == "Started")
                {
                    entity.Status = "InProgress";
                    Console.WriteLine("Status updated from Started to InProgress");
                }

                // Update stage tracking info
                await UpdateStageTrackingInfoAsync(entity);

                var result = await _onboardingRepository.UpdateAsync(entity);
                Console.WriteLine($"Stage progression update result: {result}");

                // Publish stage completion event
                if (result)
                {
                    Console.WriteLine("Publishing OnboardingStageCompletedEvent for stage progression");
                    await PublishStageCompletionEventForCurrentStageAsync(entity, currentStage, isLastStage);
                }

                Console.WriteLine($"=== CompleteCurrentStageAsync End ===");
                return result;
            }
        }

        /// <summary>
        /// Complete specified stage with validation (supports non-sequential completion)
        /// </summary>
        public async Task<bool> CompleteCurrentStageAsync(long id, CompleteCurrentStageInputDto input)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            Console.WriteLine($"=== CompleteCurrentStageAsync with Validation (Non-Sequential) ===");
            Console.WriteLine($"Onboarding ID: {id}");

            // Get target stage ID with backward compatibility
            long targetStageId;
            try
            {
                targetStageId = input.GetTargetStageId();
            }
            catch (ArgumentException ex)
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, $"Invalid stage ID parameters: {ex.Message}");
            }

            Console.WriteLine($"Stage ID to complete: {targetStageId}");
            Console.WriteLine($"Frontend Current Stage ID: {input.CurrentStageId}");
            Console.WriteLine($"Backend Current Stage ID: {entity.CurrentStageId}");
            Console.WriteLine($"Current Status: {entity.Status}");
            Console.WriteLine($"Current Completion Rate: {entity.CompletionRate}");

            // Check if onboarding is already completed
            if (entity.Status == "Completed")
            {
                Console.WriteLine("Onboarding is already completed, returning success response");
                Console.WriteLine("Note: This is handled gracefully instead of throwing an exception");
                return true; // Return success since the desired outcome (completion) is already achieved
            }

            // Optional: Check if frontend stage matches backend stage (only if CurrentStageId is provided)
            if (input.CurrentStageId.HasValue && entity.CurrentStageId != input.CurrentStageId)
            {
                Console.WriteLine($"Stage mismatch detected! Frontend: {input.CurrentStageId}, Backend: {entity.CurrentStageId}");
                Console.WriteLine("🔧 Auto-correcting stage mismatch instead of throwing error...");

                // Auto-correct: Update completion rate and sync stage information
                Console.WriteLine("🔄 Updating completion rate to ensure backend data is current...");
                try
                {
                    await UpdateCompletionRateAsync(id);
                    Console.WriteLine("✅ Completion rate updated successfully");

                    // Reload the entity to get the latest data after completion rate update
                    entity = await _onboardingRepository.GetByIdAsync(id);
                    Console.WriteLine($"📥 Reloaded entity - Current Stage ID: {entity.CurrentStageId}");

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Warning: Failed to update completion rate during auto-correction: {ex.Message}");
                }

                // Check if the mismatch still exists after correction
                if (entity.CurrentStageId != input.CurrentStageId)
                {
                    Console.WriteLine($"⚠️ Stage mismatch still exists after correction. Using backend stage {entity.CurrentStageId} to proceed.");
                    Console.WriteLine("📝 Note: Frontend will be updated with correct stage information in the response.");
                }
                else
                {
                    Console.WriteLine("✅ Stage mismatch resolved after completion rate update");
                }
            }

            Console.WriteLine("✅ Stage validation passed");

            // Get all stages for this workflow
            var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
            var orderedStages = stages.OrderBy(x => x.Order).ToList();
            var totalStages = orderedStages.Count;

            Console.WriteLine($"Total Stages: {totalStages}");
            Console.WriteLine($"Ordered Stages: {string.Join(", ", orderedStages.Select(s => $"{s.Order}:{s.Name}"))}");

            // Find the stage to complete
            var stageToComplete = orderedStages.FirstOrDefault(x => x.Id == targetStageId);
            if (stageToComplete == null)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Specified stage not found in workflow");
            }

            Console.WriteLine($"Stage to complete: {stageToComplete.Name} (Order: {stageToComplete.Order}, ID: {stageToComplete.Id})");

            // Validate if this stage can be completed
            var (canComplete, validationError) = await ValidateStageCanBeCompletedAsync(entity, stageToComplete.Id);
            if (!canComplete)
            {
                Console.WriteLine($"Stage completion validation failed: {validationError}");
                throw new CRMException(ErrorCodeEnum.BusinessError, validationError);
            }

            Console.WriteLine("✅ Stage completion validation passed");

            // Check stage completion logs to see if this stage has already been completed
            Console.WriteLine($"Checking stage completion logs for onboarding {id} and stage {stageToComplete.Id}...");
            try
            {
                var stageCompletionLogs = await _stageCompletionLogRepository.GetByOnboardingAndStageAsync(id, stageToComplete.Id);
                var completionLogs = stageCompletionLogs.Where(log =>
                    log.LogType == "complete" &&
                    log.Success &&
                    log.Action.Contains("complete", StringComparison.OrdinalIgnoreCase)).ToList();

                Console.WriteLine($"Found {completionLogs.Count} successful completion logs for this stage");

                if (completionLogs.Any())
                {
                    var latestCompletionLog = completionLogs.OrderByDescending(log => log.CreateDate).First();
                    Console.WriteLine($"⚠️ WARNING: Stage '{stageToComplete.Name}' was already completed at {latestCompletionLog.CreateDate}");
                    Console.WriteLine($"📝 Allowing re-completion for flexibility - will update completion timestamp");
                    // Don't throw exception, allow re-completion but log the previous completion
                }

                Console.WriteLine("No previous completion logs found for this stage - proceeding with completion");
            }
            catch (CRMException)
            {
                // Re-throw CRM exceptions (our business logic exceptions)
                throw;
            }
            catch (Exception ex)
            {
                // Log but don't fail on stage completion log check errors
                Console.WriteLine($"Warning: Failed to check stage completion logs: {ex.Message}");
                Console.WriteLine("Proceeding with stage completion despite log check failure");
            }

            // Update stages progress for the completed stage (non-sequential completion)
            Console.WriteLine("Updating stages progress for stage completion...");
            await UpdateStagesProgressAsync(entity, stageToComplete.Id, GetCurrentUserName(), GetCurrentUserId(), input.CompletionNotes);
            LoadStagesProgressFromJson(entity);

            // Calculate new completion rate based on completed stages
            entity.CompletionRate = CalculateCompletionRateByCompletedStages(entity.StagesProgress);
            var completedCount = entity.StagesProgress.Count(s => s.IsCompleted);
            Console.WriteLine($"Completed Stages: {completedCount}");
            Console.WriteLine($"New Completion Rate: {entity.CompletionRate}%");

            // Check if all stages are completed
            var allStagesCompleted = entity.StagesProgress.All(s => s.IsCompleted);
            if (allStagesCompleted)
            {
                // Complete the entire onboarding
                Console.WriteLine("All stages completed - completing entire onboarding");
                entity.Status = "Completed";
                entity.CompletionRate = 100;
                entity.ActualCompletionDate = DateTimeOffset.UtcNow;

                // Add completion notes if provided
                if (!string.IsNullOrEmpty(input.CompletionNotes))
                {
                    var noteText = $"[Onboarding Completed] Final stage '{stageToComplete.Name}' completed: {input.CompletionNotes}";
                    entity.Notes = string.IsNullOrEmpty(entity.Notes)
                        ? noteText
                        : $"{entity.Notes}\n{noteText}";
                    Console.WriteLine($"Added final completion notes: {input.CompletionNotes}");
                }
            }
            else
            {
                // Update status to InProgress if it was Started
                if (entity.Status == "Started")
                {
                    entity.Status = "InProgress";
                    Console.WriteLine("Status updated from Started to InProgress");
                }

                // Add completion notes if provided
                if (!string.IsNullOrEmpty(input.CompletionNotes))
                {
                    var noteText = $"[Stage Completed] {stageToComplete.Name}: {input.CompletionNotes}";
                    entity.Notes = string.IsNullOrEmpty(entity.Notes)
                        ? noteText
                        : $"{entity.Notes}\n{noteText}";
                    Console.WriteLine($"Added completion notes: {input.CompletionNotes}");
                }
            }

            // Update stage tracking info
            await UpdateStageTrackingInfoAsync(entity);

            // Update the entity
            var result = await _onboardingRepository.UpdateAsync(entity);
            Console.WriteLine($"Update result: {result}");

            // Publish stage completion event
            if (result)
            {
                Console.WriteLine("Publishing OnboardingStageCompletedEvent");
                await PublishStageCompletionEventForCurrentStageAsync(entity, stageToComplete, allStagesCompleted);
            }

            Console.WriteLine($"=== CompleteCurrentStageAsync with Validation (Non-Sequential) End ===");
            return result;
        }

        /// <summary>
        /// Complete current stage with details
        /// </summary>
        public async Task<bool> CompleteStageAsync(long id, CompleteStageInputDto input)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            if (entity.Status == "Completed")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Onboarding is already completed");
            }

            // Get current stage info
            var currentStage = await _stageRepository.GetByIdAsync(entity.CurrentStageId ?? 0);
            if (currentStage == null)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Current stage not found");
            }

            // Validate if this stage can be completed
            var (canComplete, validationError) = await ValidateStageCanBeCompletedAsync(entity, currentStage.Id);
            if (!canComplete)
            {
                Console.WriteLine($"Stage completion validation failed in CompleteStageAsync: {validationError}");
                throw new CRMException(ErrorCodeEnum.BusinessError, validationError);
            }

            Console.WriteLine("✅ Stage completion validation passed in CompleteStageAsync");

            // Log stage completion
            await LogStageCompletionAsync(entity, currentStage, input);

            // Check if this is the last stage
            var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
            var orderedStages = stages.OrderBy(x => x.Order).ToList();
            var currentStageIndex = orderedStages.FindIndex(x => x.Id == entity.CurrentStageId);

            if (currentStageIndex == orderedStages.Count - 1)
            {
                // This is the last stage, complete the entire onboarding
                entity.Status = "Completed";
                entity.CompletionRate = 100;
                entity.ActualCompletionDate = DateTimeOffset.UtcNow;
            }
            else if (input.AutoMoveToNext)
            {
                // Move to next stage
                var nextStage = orderedStages[currentStageIndex + 1];
                entity.CurrentStageId = nextStage.Id;
                entity.CurrentStageOrder = nextStage.Order;
                entity.CurrentStageStartTime = DateTimeOffset.UtcNow;

                // Update completion rate based on stage order progression
                await UpdateStagesProgressAsync(entity, currentStage.Id, input.CompletedBy ?? GetCurrentUserName(), input.CompletedById ?? GetCurrentUserId(), input.CompletionNotes);

                // Calculate completion rate based on completed stages count
                LoadStagesProgressFromJson(entity);
                entity.CompletionRate = CalculateCompletionRateByCompletedStages(entity.StagesProgress);

                // Update status to InProgress if it was Started
                if (entity.Status == "Started")
                {
                    entity.Status = "InProgress";
                }
            }

            // Add completion notes to onboarding notes
            if (!string.IsNullOrEmpty(input.CompletionNotes))
            {
                var noteText = $"[Stage Completed] {currentStage.Name}: {input.CompletionNotes}";
                entity.Notes = string.IsNullOrEmpty(entity.Notes)
                    ? noteText
                    : $"{entity.Notes}\n{noteText}";
            }

            // Add rating if provided
            if (input.Rating.HasValue)
            {
                var ratingText = $"[Stage Rating] {currentStage.Name}: {input.Rating}/5 stars";
                entity.Notes = string.IsNullOrEmpty(entity.Notes)
                    ? ratingText
                    : $"{entity.Notes}\n{ratingText}";
            }

            // Add feedback if provided
            if (!string.IsNullOrEmpty(input.Feedback))
            {
                var feedbackText = $"[Stage Feedback] {currentStage.Name}: {input.Feedback}";
                entity.Notes = string.IsNullOrEmpty(entity.Notes)
                    ? feedbackText
                    : $"{entity.Notes}\n{feedbackText}";
            }

            // Update stage tracking info
            await UpdateStageTrackingInfoAsync(entity);

            var result = await _onboardingRepository.UpdateAsync(entity);

            // Publish Kafka event for stage completion
            if (result)
            {
                await PublishStageCompletionEventAsync(entity, currentStage, input);
            }

            return result;
        }

        /// <summary>
        /// Complete onboarding
        /// </summary>
        public async Task<bool> CompleteAsync(long id)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            if (entity.Status == "Completed")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Onboarding is already completed");
            }

            return await _onboardingRepository.UpdateStatusAsync(id, "Completed");
        }

        /// <summary>
        /// Pause onboarding
        /// </summary>
        public async Task<bool> PauseAsync(long id)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            if (entity.Status == "Completed")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Cannot pause completed onboarding");
            }

            return await _onboardingRepository.UpdateStatusAsync(id, "Paused");
        }

        /// <summary>
        /// Resume onboarding
        /// </summary>
        public async Task<bool> ResumeAsync(long id)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            if (entity.Status != "Paused")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Only paused onboarding can be resumed");
            }

            return await _onboardingRepository.UpdateStatusAsync(id, "InProgress");
        }

        /// <summary>
        /// Cancel onboarding
        /// </summary>
        public async Task<bool> CancelAsync(long id, string reason)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            if (entity.Status == "Completed")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Cannot cancel completed onboarding");
            }

            // Update notes with cancellation reason
            if (!string.IsNullOrEmpty(reason))
            {
                entity.Notes = $"Cancelled: {reason}. {entity.Notes}".Trim();
                await _onboardingRepository.UpdateAsync(entity);
            }

            // Log cancellation to Change Log
            await LogOnboardingActionAsync(entity, "Cancel Onboarding", "onboarding_cancel", true, new
            {
                CancellationReason = reason,
                CancelledAt = DateTimeOffset.UtcNow,
                CancelledBy = GetCurrentUserName()
            });

            return await _onboardingRepository.UpdateStatusAsync(id, "Cancelled");
        }

        /// <summary>
        /// Reject onboarding application
        /// </summary>
        public async Task<bool> RejectAsync(long id, RejectOnboardingInputDto input)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            if (entity.Status == "Completed")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Cannot reject completed onboarding");
            }

            if (entity.Status == "Rejected" || entity.Status == "Terminated")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Onboarding is already rejected or terminated");
            }

            var currentTime = DateTimeOffset.UtcNow;

            // Update onboarding status
            entity.Status = input.TerminateWorkflow ? "Terminated" : "Rejected";
            entity.ModifyDate = currentTime;

            // Update notes with rejection reason
            var rejectionNote = $"[{(input.TerminateWorkflow ? "TERMINATED" : "REJECTED")}] {input.RejectionReason}";
            if (!string.IsNullOrEmpty(input.AdditionalNotes))
            {
                rejectionNote += $" - Additional Notes: {input.AdditionalNotes}";
            }
            rejectionNote += $" - {(input.TerminateWorkflow ? "Terminated" : "Rejected")} by: {input.RejectedBy} at {currentTime:yyyy-MM-dd HH:mm:ss}";

            entity.Notes = string.IsNullOrEmpty(entity.Notes)
                ? rejectionNote
                : $"{entity.Notes}\n{rejectionNote}";

            // Update stages progress to reflect rejection/termination
            LoadStagesProgressFromJson(entity);
            if (entity.StagesProgress != null && entity.StagesProgress.Any())
            {
                foreach (var stage in entity.StagesProgress)
                {
                    if (stage.Status == "InProgress" || stage.Status == "Pending")
                    {
                        stage.Status = input.TerminateWorkflow ? "Terminated" : "Rejected";
                        stage.IsTerminated = input.TerminateWorkflow;
                        stage.RejectionReason = input.RejectionReason;
                        stage.RejectionTime = currentTime;
                        stage.RejectedBy = input.RejectedBy;
                        stage.LastUpdatedTime = currentTime;
                        stage.LastUpdatedBy = input.RejectedBy;

                        if (input.TerminateWorkflow)
                        {
                            stage.TerminationTime = currentTime;
                            stage.TerminatedBy = input.RejectedBy;
                        }
                    }
                }

                // Serialize updated stages progress back to JSON
                entity.StagesProgressJson = System.Text.Json.JsonSerializer.Serialize(entity.StagesProgress);
            }

            // Update stage tracking info
            await UpdateStageTrackingInfoAsync(entity);

            // Save changes
            var result = await _onboardingRepository.UpdateAsync(entity);

            if (result)
            {
                // Log rejection to Change Log
                await LogOnboardingActionAsync(entity,
                    input.TerminateWorkflow ? "Terminate Onboarding" : "Reject Application",
                    input.TerminateWorkflow ? "onboarding_terminate" : "application_reject",
                    true,
                    new
                    {
                        RejectionReason = input.RejectionReason,
                        TerminateWorkflow = input.TerminateWorkflow,
                        AdditionalNotes = input.AdditionalNotes,
                        RejectedBy = input.RejectedBy,
                        RejectedById = input.RejectedById,
                        RejectedAt = currentTime,
                        SendNotification = input.SendNotification,
                        PreviousStatus = "InProgress", // Assuming it was in progress
                        NewStatus = entity.Status
                    });

                // TODO: Send notification if requested
                if (input.SendNotification)
                {
                    // Implement notification logic here
                    Console.WriteLine($"Notification should be sent for {(input.TerminateWorkflow ? "terminated" : "rejected")} onboarding {id}");
                }
            }

            return result;
        }

        /// <summary>
        /// Assign onboarding to user
        /// </summary>
        public async Task<bool> AssignAsync(long id, AssignOnboardingInputDto input)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            entity.CurrentAssigneeId = input.AssigneeId;
            entity.CurrentAssigneeName = input.AssigneeName;
            entity.CurrentTeam = input.Team;

            return await _onboardingRepository.UpdateAsync(entity);
        }

        /// <summary>
        /// Get onboarding statistics
        /// </summary>
        public async Task<OnboardingStatisticsDto> GetStatisticsAsync()
        {
            try
            {
                var stats = await _onboardingRepository.GetStatisticsAsync();
                var statusCount = await _onboardingRepository.GetCountByStatusAsync();

                return new OnboardingStatisticsDto
                {
                    TotalCount = (int)stats["TotalCount"],
                    InProgressCount = (int)stats["InProgressCount"],
                    CompletedCount = (int)stats["CompletedCount"],
                    PausedCount = (int)stats["PausedCount"],
                    CancelledCount = (int)stats["CancelledCount"],
                    OverdueCount = (int)stats["OverdueCount"],
                    AverageCompletionRate = (decimal)stats["AverageCompletionRate"],
                    StatusStatistics = statusCount,
                    PriorityStatistics = new Dictionary<string, int>(),
                    TeamStatistics = new Dictionary<string, int>(),
                    StageStatistics = new Dictionary<string, int>()
                };
            }
            catch (Exception ex)
            {
                throw new CRMException(ErrorCodeEnum.SystemError, $"Error getting statistics: {ex.Message}");
            }
        }

        /// <summary>
        /// Update completion rate based on stage progress
        /// </summary>
        public async Task<bool> UpdateCompletionRateAsync(long id)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
            var totalStages = stages.Count;

            // Calculate completion rate based on stage progress
            // CurrentStageOrder represents the current stage (1-based), so completed stages = CurrentStageOrder - 1
            var completedStages = Math.Max(0, entity.CurrentStageOrder - 1);
            var stageBasedCompletionRate = totalStages > 0 ? (decimal)completedStages / totalStages * 100 : 0;

            // Log calculation details for debugging
            Console.WriteLine($"=== Stage-based Completion Rate Calculation ===");
            Console.WriteLine($"Onboarding ID: {id}");
            Console.WriteLine($"Current Stage Order: {entity.CurrentStageOrder}");
            Console.WriteLine($"Total Stages: {totalStages}");
            Console.WriteLine($"Completed Stages: {completedStages}");
            Console.WriteLine($"Calculated Completion Rate: {stageBasedCompletionRate:F2}%");
            Console.WriteLine($"Current Database Rate: {entity.CompletionRate:F2}%");

            // Always update to the stage-based completion rate
            Console.WriteLine($"✓ Updating completion rate from {entity.CompletionRate:F2}% to {stageBasedCompletionRate:F2}%");
            return await _onboardingRepository.UpdateCompletionRateAsync(id, stageBasedCompletionRate);
        }

        /// <summary>
        /// Get overdue onboarding list
        /// </summary>
        public async Task<List<OnboardingOutputDto>> GetOverdueListAsync()
        {
            var entities = await _onboardingRepository.GetOverdueListAsync();
            return _mapper.Map<List<OnboardingOutputDto>>(entities);
        }

        /// <summary>
        /// Batch update status
        /// </summary>
        public async Task<bool> BatchUpdateStatusAsync(List<long> ids, string status)
        {
            if (!ids.Any())
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "No onboarding IDs provided");
            }

            var validStatuses = new[] { "Started", "InProgress", "Completed", "Paused", "Cancelled" };
            if (!validStatuses.Contains(status))
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Invalid status");
            }

            return await _onboardingRepository.BatchUpdateStatusAsync(ids, status);
        }

        /// <summary>
        /// Set priority for onboarding (required for Stage 1 completion)
        /// </summary>
        public async Task<bool> SetPriorityAsync(long id, string priority)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            var validPriorities = new[] { "Low", "Medium", "High", "Critical" };
            if (!validPriorities.Contains(priority))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Invalid priority. Must be one of: Low, Medium, High, Critical");
            }

            entity.Priority = priority;
            entity.IsPrioritySet = true;

            // Update stage tracking info
            await UpdateStageTrackingInfoAsync(entity);

            return await _onboardingRepository.UpdateAsync(entity);
        }

        /// <summary>
        /// Update stage tracking information
        /// </summary>
        private async Task UpdateStageTrackingInfoAsync(Onboarding entity)
        {
            // TODO: Get current user context
            // Use real user information from UserContext
            entity.StageUpdatedTime = DateTimeOffset.UtcNow;
            entity.StageUpdatedBy = GetCurrentUserName();
            entity.StageUpdatedById = GetCurrentUserId() ?? 0;
            entity.StageUpdatedByEmail = GetCurrentUserEmail();
        }

        /// <summary>
        /// Get onboarding timeline
        /// </summary>
        public async Task<List<OnboardingTimelineDto>> GetTimelineAsync(long id)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            // TODO: Implement timeline repository to get actual timeline data
            // For now, return a sample timeline based on onboarding data
            var timeline = new List<OnboardingTimelineDto>
            {
                new OnboardingTimelineDto
                {
                    Id = 1,
                    EventType = "Created",
                    Description = "Onboarding created",
                    EventTime = entity.CreateDate,
                    UserId = entity.CreateUserId,
                    UserName = entity.CreateBy,
                    Details = $"Onboarding created for {entity.LeadName}"
                }
            };

            if (entity.StageUpdatedTime.HasValue)
            {
                timeline.Add(new OnboardingTimelineDto
                {
                    Id = 2,
                    EventType = "StageUpdated",
                    Description = "Stage updated",
                    EventTime = entity.StageUpdatedTime.Value,
                    UserId = entity.StageUpdatedById,
                    UserName = entity.StageUpdatedBy,
                    StageId = entity.CurrentStageId,
                    Details = $"Stage updated by {entity.StageUpdatedBy}"
                });
            }

            return timeline.OrderByDescending(t => t.EventTime).ToList();
        }

        /// <summary>
        /// Add note to onboarding
        /// </summary>
        public async Task<bool> AddNoteAsync(long id, AddNoteInputDto input)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            // Add note to existing notes
            var noteText = $"[{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm}] {input.Content}";
            if (!string.IsNullOrEmpty(input.Type))
            {
                noteText = $"[{input.Type}] {noteText}";
            }

            entity.Notes = string.IsNullOrEmpty(entity.Notes)
                ? noteText
                : $"{entity.Notes}\n{noteText}";

            return await _onboardingRepository.UpdateAsync(entity);
        }

        /// <summary>
        /// Update onboarding status
        /// </summary>
        public async Task<bool> UpdateStatusAsync(long id, UpdateStatusInputDto input)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            var validStatuses = new[] { "Started", "InProgress", "Completed", "Paused", "Cancelled" };
            if (!validStatuses.Contains(input.Status))
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Invalid status");
            }

            entity.Status = input.Status;

            if (!string.IsNullOrEmpty(input.Remarks))
            {
                var remarkText = $"[Status Update] {input.Remarks}";
                entity.Notes = string.IsNullOrEmpty(entity.Notes)
                    ? remarkText
                    : $"{entity.Notes}\n{remarkText}";
            }

            // Update stage tracking info
            await UpdateStageTrackingInfoAsync(entity);

            return await _onboardingRepository.UpdateAsync(entity);
        }

        /// <summary>
        /// Update onboarding priority
        /// </summary>
        public async Task<bool> UpdatePriorityAsync(long id, UpdatePriorityInputDto input)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            var validPriorities = new[] { "Low", "Medium", "High", "Critical" };
            if (!validPriorities.Contains(input.Priority))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Invalid priority. Must be one of: Low, Medium, High, Critical");
            }

            entity.Priority = input.Priority;
            entity.IsPrioritySet = true;

            if (!string.IsNullOrEmpty(input.Remarks))
            {
                var remarkText = $"[Priority Update] Priority set to {input.Priority}. {input.Remarks}";
                entity.Notes = string.IsNullOrEmpty(entity.Notes)
                    ? remarkText
                    : $"{entity.Notes}\n{remarkText}";
            }

            // Update stage tracking info
            await UpdateStageTrackingInfoAsync(entity);

            return await _onboardingRepository.UpdateAsync(entity);
        }

        /// <summary>
        /// Complete onboarding with details
        /// </summary>
        public async Task<bool> CompleteAsync(long id, CompleteOnboardingInputDto input)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            if (entity.Status == "Completed")
            {
                Console.WriteLine("Onboarding is already completed, returning success response");
                Console.WriteLine("Note: This is handled gracefully instead of throwing an exception");
                return true; // Return success since the desired outcome (completion) is already achieved
            }

            entity.Status = "Completed";
            entity.CompletionRate = 100;
            entity.ActualCompletionDate = DateTimeOffset.UtcNow;

            // Add completion notes
            if (!string.IsNullOrEmpty(input.CompletionNotes))
            {
                var noteText = $"[Completion] {input.CompletionNotes}";
                entity.Notes = string.IsNullOrEmpty(entity.Notes)
                    ? noteText
                    : $"{entity.Notes}\n{noteText}";
            }

            // Add rating if provided
            if (input.Rating.HasValue)
            {
                var ratingText = $"[Rating] {input.Rating}/5 stars";
                entity.Notes = string.IsNullOrEmpty(entity.Notes)
                    ? ratingText
                    : $"{entity.Notes}\n{ratingText}";
            }

            // Add feedback if provided
            if (!string.IsNullOrEmpty(input.Feedback))
            {
                var feedbackText = $"[Feedback] {input.Feedback}";
                entity.Notes = string.IsNullOrEmpty(entity.Notes)
                    ? feedbackText
                    : $"{entity.Notes}\n{feedbackText}";
            }

            // Update stage tracking info
            await UpdateStageTrackingInfoAsync(entity);

            return await _onboardingRepository.UpdateAsync(entity);
        }

        /// <summary>
        /// Restart onboarding
        /// </summary>
        public async Task<bool> RestartAsync(long id, RestartOnboardingInputDto input)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            if (entity.Status == "InProgress")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Onboarding is already in progress");
            }

            entity.Status = "InProgress";
            entity.ActualCompletionDate = null;

            if (input.ResetProgress)
            {
                entity.CurrentStageId = null;
                entity.CurrentStageOrder = 0;
                entity.CompletionRate = 0;
                entity.CurrentStageStartTime = DateTimeOffset.UtcNow;
            }

            // Add restart notes
            var restartText = $"[Restart] Onboarding restarted";
            if (!string.IsNullOrEmpty(input.Reason))
            {
                restartText += $" - Reason: {input.Reason}";
            }
            if (!string.IsNullOrEmpty(input.Notes))
            {
                restartText += $" - Notes: {input.Notes}";
            }

            entity.Notes = string.IsNullOrEmpty(entity.Notes)
                ? restartText
                : $"{entity.Notes}\n{restartText}";

            // Update stage tracking info
            await UpdateStageTrackingInfoAsync(entity);

            return await _onboardingRepository.UpdateAsync(entity);
        }

        /// <summary>
        /// Get onboarding progress
        /// </summary>
        public async Task<OnboardingProgressDto> GetProgressAsync(long id)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            // Load stages progress from JSON
            LoadStagesProgressFromJson(entity);

            var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
            var totalStages = stages.Count;
            var completedStages = entity.CurrentStageOrder;

            // Calculate estimated completion time based on average stage duration
            var avgStageDuration = TimeSpan.FromDays(7); // Default 7 days per stage
            var remainingStages = totalStages - completedStages;
            var estimatedCompletion = entity.CreateDate.AddDays(totalStages * 7);

            // Check if overdue
            var isOverdue = entity.Status != "Completed" &&
                           entity.EstimatedCompletionDate.HasValue &&
                           DateTimeOffset.UtcNow > entity.EstimatedCompletionDate.Value;

            // Map stages progress to DTO
            var stagesProgressDto = _mapper.Map<List<OnboardingStageProgressDto>>(entity.StagesProgress);

            return new OnboardingProgressDto
            {
                OnboardingId = entity.Id,
                CurrentStageId = entity.CurrentStageId,
                CurrentStageName = stages.FirstOrDefault(s => s.Id == entity.CurrentStageId)?.Name,
                TotalStages = totalStages,
                CompletedStages = completedStages,
                CompletionPercentage = entity.CompletionRate,
                StartTime = entity.CreateDate,
                EstimatedCompletionTime = entity.EstimatedCompletionDate ?? estimatedCompletion,
                ActualCompletionTime = entity.ActualCompletionDate,
                IsOverdue = isOverdue,
                Status = entity.Status,
                Priority = entity.Priority,
                StagesProgress = stagesProgressDto
            };
        }

        /// <summary>
        /// Log stage completion to change log
        /// </summary>
        private async Task LogStageCompletionAsync(Onboarding onboarding, Stage stage, CompleteStageInputDto input)
        {
            try
            {
                // 获取真实用户信息
                var currentUserName = GetCurrentUserName();
                var currentUserId = GetCurrentUserId();
                var actualCompletedBy = !string.IsNullOrEmpty(input.CompletedBy) ? input.CompletedBy : currentUserName;
                var actualCompletedById = input.CompletedById ?? currentUserId;

                var logData = new
                {
                    OnboardingId = onboarding.Id,
                    LeadId = onboarding.LeadId,
                    StageId = stage.Id,
                    StageName = stage.Name,
                    CompletionNotes = input.CompletionNotes,
                    Rating = input.Rating,
                    Feedback = input.Feedback,
                    AttachmentsJson = input.AttachmentsJson,
                    AutoMoveToNext = input.AutoMoveToNext,
                    CompletedTime = DateTimeOffset.UtcNow,
                    CompletedBy = actualCompletedBy,
                    CompletedById = actualCompletedById,
                    CompletionMethod = "Manual",
                    PreviousStatus = "InProgress",
                    NewStatus = "Completed"
                };

                var stageCompletionLog = new StageCompletionLog
                {
                    TenantId = onboarding.TenantId,
                    OnboardingId = onboarding.Id,
                    StageId = stage.Id,
                    StageName = stage.Name,
                    LogType = "stage_complete",
                    Action = "Complete Stage",
                    LogData = System.Text.Json.JsonSerializer.Serialize(logData),
                    Success = true,
                    NetworkStatus = "online",
                    CreateBy = actualCompletedBy,
                    ModifyBy = actualCompletedBy,
                    CreateUserId = actualCompletedById ?? 0,
                    ModifyUserId = actualCompletedById ?? 0
                };

                // Save to StageCompletionLog repository
                await _stageCompletionLogRepository.InsertAsync(stageCompletionLog);
                Console.WriteLine($"✅ Stage completion logged to Change Log for Stage: {stage.Name} by {actualCompletedBy}");
            }
            catch (Exception ex)
            {
                // Log error but don't fail the stage completion
                Console.WriteLine($"❌ Failed to log stage completion: {ex.Message}");
            }
        }

        /// <summary>
        /// Log general onboarding action to change log
        /// </summary>
        private async Task LogOnboardingActionAsync(Onboarding onboarding, string action, string logType, bool success, object additionalData = null)
        {
            try
            {
                var logData = new
                {
                    OnboardingId = onboarding.Id,
                    LeadId = onboarding.LeadId,
                    LeadName = onboarding.LeadName,
                    WorkflowId = onboarding.WorkflowId,
                    CurrentStageId = onboarding.CurrentStageId,
                    Status = onboarding.Status,
                    Priority = onboarding.Priority,
                    ActionTime = DateTimeOffset.UtcNow,
                    ActionBy = GetCurrentUserName(),
                    AdditionalData = additionalData
                };

                var stageCompletionLog = new StageCompletionLog
                {
                    TenantId = onboarding.TenantId,
                    OnboardingId = onboarding.Id,
                    StageId = onboarding.CurrentStageId ?? 0,
                    StageName = "N/A",
                    LogType = logType,
                    Action = action,
                    LogData = System.Text.Json.JsonSerializer.Serialize(logData),
                    Success = success,
                    NetworkStatus = "online",
                    CreateBy = GetCurrentUserName(),
                    ModifyBy = GetCurrentUserName()
                };

                await _stageCompletionLogRepository.InsertAsync(stageCompletionLog);
                Console.WriteLine($"✅ Onboarding action logged to Change Log: {action}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to log onboarding action '{action}': {ex.Message}");
            }
        }

        /// <summary>
        /// Log task completion to change log
        /// </summary>
        private async Task LogTaskCompletionAsync(long onboardingId, long stageId, string stageName, long taskId, string taskName, bool isCompleted, string completionNotes = "", string completedBy = null)
        {
            try
            {
                var logData = new
                {
                    OnboardingId = onboardingId,
                    StageId = stageId,
                    StageName = stageName,
                    TaskId = taskId,
                    TaskName = taskName,
                    IsCompleted = isCompleted,
                    CompletionNotes = completionNotes,
                    CompletedTime = DateTimeOffset.UtcNow,
                    CompletedBy = completedBy ?? GetCurrentUserName(),
                    Action = isCompleted ? "Task Completed" : "Task Marked Incomplete"
                };

                var stageCompletionLog = new StageCompletionLog
                {
                    TenantId = GetTenantIdFromOnboarding(onboardingId),
                    OnboardingId = onboardingId,
                    StageId = stageId,
                    StageName = stageName,
                    LogType = "task_completion",
                    Action = isCompleted ? "Task Completed" : "Task Marked Incomplete",
                    LogData = System.Text.Json.JsonSerializer.Serialize(logData),
                    Success = true,
                    NetworkStatus = "online",
                    CreateBy = completedBy ?? GetCurrentUserName(),
                    ModifyBy = completedBy ?? GetCurrentUserName()
                };

                await _stageCompletionLogRepository.InsertAsync(stageCompletionLog);
                Console.WriteLine($"✅ Task completion logged to Change Log: {taskName} - {(isCompleted ? "Completed" : "Incomplete")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to log task completion: {ex.Message}");
            }
        }

        /// <summary>
        /// Log stage completion for current stage async
        /// </summary>
        private async Task LogStageCompletionForCurrentStageAsync(Onboarding onboarding, Stage stage, string completedBy, long? completedById, string completionNotes)
        {
            try
            {
                // 获取真实用户信息，如果参数没有提供的话
                var actualCompletedBy = !string.IsNullOrEmpty(completedBy) ? completedBy : GetCurrentUserName();
                var actualCompletedById = completedById ?? GetCurrentUserId();

                var logData = new
                {
                    OnboardingId = onboarding.Id,
                    LeadId = onboarding.LeadId,
                    LeadName = onboarding.LeadName,
                    StageId = stage.Id,
                    StageName = stage.Name,
                    StageOrder = stage.Order,
                    CompletionNotes = completionNotes,
                    CompletedTime = DateTimeOffset.UtcNow,
                    CompletedBy = actualCompletedBy,
                    CompletedById = actualCompletedById,
                    CompletionMethod = "Manual",
                    PreviousStatus = "InProgress",
                    NewStatus = "Completed",
                    CompletionRate = onboarding.CompletionRate,
                    WorkflowId = onboarding.WorkflowId,
                    Priority = onboarding.Priority
                };

                var stageCompletionLog = new StageCompletionLog
                {
                    TenantId = onboarding.TenantId,
                    OnboardingId = onboarding.Id,
                    StageId = stage.Id,
                    StageName = stage.Name,
                    LogType = "stage_complete",
                    Action = "Complete Stage",
                    LogData = System.Text.Json.JsonSerializer.Serialize(logData),
                    Success = true,
                    NetworkStatus = "online",
                    CreateBy = actualCompletedBy,
                    ModifyBy = actualCompletedBy,
                    CreateUserId = actualCompletedById ?? 0,
                    ModifyUserId = actualCompletedById ?? 0
                };

                await _stageCompletionLogRepository.InsertAsync(stageCompletionLog);
                Console.WriteLine($"✅ Stage completion logged to Change Log for Stage: {stage.Name} by {actualCompletedBy}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to log stage completion for current stage: {ex.Message}");
            }
        }

        /// <summary>
        /// Publish stage completion event
        /// </summary>
        private async Task PublishStageCompletionEventAsync(Onboarding onboarding, Stage stage, CompleteStageInputDto input)
        {
            try
            {
                // Get next stage info if auto-moving
                string nextStageName = null;
                if (input.AutoMoveToNext && onboarding.CurrentStageId.HasValue)
                {
                    var nextStage = await _stageRepository.GetByIdAsync(onboarding.CurrentStageId.Value);
                    nextStageName = nextStage?.Name;
                }

                // Publish the OnboardingStageCompletedEvent for enhanced event handling
                var onboardingStageCompletedEvent = new OnboardingStageCompletedEvent
                {
                    EventId = Guid.NewGuid().ToString(),
                    Timestamp = DateTimeOffset.UtcNow,
                    Version = "1.0",
                    TenantId = onboarding.TenantId,
                    OnboardingId = onboarding.Id,
                    LeadId = onboarding.LeadId,
                    WorkflowId = onboarding.WorkflowId,
                    WorkflowName = (await _workflowRepository.GetByIdAsync(onboarding.WorkflowId))?.Name ?? "Unknown",
                    CompletedStageId = stage.Id,
                    CompletedStageName = stage.Name,
                    StageCategory = stage.Name ?? "Unknown",
                    NextStageId = input.AutoMoveToNext ? onboarding.CurrentStageId : null,
                    NextStageName = nextStageName,
                    CompletionRate = onboarding.CompletionRate,
                    IsFinalStage = onboarding.Status == "Completed",
                    AssigneeName = GetCurrentUserFullName(),
                    ResponsibleTeam = onboarding.CurrentTeam ?? "Default",
                    Priority = onboarding.Priority ?? "Medium",
                    Source = "CustomerPortal",
                    BusinessContext = new Dictionary<string, object>
                    {
                        ["CompletionNotes"] = input.CompletionNotes ?? "",
                        ["Rating"] = input.Rating?.ToString() ?? "",
                        ["Feedback"] = input.Feedback ?? "",
                        ["AutoMoveToNext"] = input.AutoMoveToNext
                    },
                    RoutingTags = new List<string> { "onboarding", "stage-completion", "customer-portal" },
                    Description = $"Stage '{stage.Name}' completed for Onboarding {onboarding.Id}",
                    Tags = new List<string> { "onboarding", "stage-completion", "unknown" }
                };

                await _mediator.Publish(onboardingStageCompletedEvent);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the stage completion
                Console.WriteLine($"Failed to publish stage completion event: {ex.Message}");
            }
        }

        /// <summary>
        /// Publish stage completion event for current stage completion (without CompleteStageInputDto)
        /// </summary>
        private async Task PublishStageCompletionEventForCurrentStageAsync(Onboarding onboarding, Stage completedStage, bool isFinalStage)
        {
            try
            {
                // Get next stage info if not final stage
                string nextStageName = null;
                long? nextStageId = null;
                if (!isFinalStage && onboarding.CurrentStageId.HasValue)
                {
                    var nextStage = await _stageRepository.GetByIdAsync(onboarding.CurrentStageId.Value);
                    nextStageName = nextStage?.Name;
                    nextStageId = nextStage?.Id;
                }

                // Publish the OnboardingStageCompletedEvent for enhanced event handling
                var onboardingStageCompletedEvent = new OnboardingStageCompletedEvent
                {
                    EventId = Guid.NewGuid().ToString(),
                    Timestamp = DateTimeOffset.UtcNow,
                    Version = "1.0",
                    TenantId = onboarding.TenantId,
                    OnboardingId = onboarding.Id,
                    LeadId = onboarding.LeadId,
                    WorkflowId = onboarding.WorkflowId,
                    WorkflowName = (await _workflowRepository.GetByIdAsync(onboarding.WorkflowId))?.Name ?? "Unknown",
                    CompletedStageId = completedStage.Id,
                    CompletedStageName = completedStage.Name,
                    StageCategory = completedStage.Name ?? "Unknown",
                    NextStageId = nextStageId,
                    NextStageName = nextStageName,
                    CompletionRate = onboarding.CompletionRate,
                    IsFinalStage = isFinalStage,
                    AssigneeName = onboarding.CurrentAssigneeName ?? GetCurrentUserFullName(),
                    ResponsibleTeam = onboarding.CurrentTeam ?? "Default",
                    Priority = onboarding.Priority ?? "Medium",
                    Source = "CustomerPortal",
                    BusinessContext = new Dictionary<string, object>
                    {
                        ["CompletionMethod"] = "CompleteCurrentStage",
                        ["AutoMoveToNext"] = !isFinalStage,
                        ["CompletionNotes"] = "Stage completed via CompleteCurrentStageAsync"
                    },
                    RoutingTags = new List<string> { "onboarding", "stage-completion", "customer-portal", "auto-progression" },
                    Description = $"Stage '{completedStage.Name}' completed for Onboarding {onboarding.Id} via CompleteCurrentStageAsync",
                    Tags = new List<string> { "onboarding", "stage-completion", "auto-progression" }
                };

                Console.WriteLine($"Publishing OnboardingStageCompletedEvent: EventId={onboardingStageCompletedEvent.EventId}, OnboardingId={onboarding.Id}, CompletedStage={completedStage.Name}, IsFinalStage={isFinalStage}");
                await _mediator.Publish(onboardingStageCompletedEvent);
                Console.WriteLine("OnboardingStageCompletedEvent published successfully");
            }
            catch (Exception ex)
            {
                // Log error but don't fail the stage completion
                Console.WriteLine($"Failed to publish stage completion event for current stage: {ex.Message}");
                Console.WriteLine($"Exception details: {ex}");
            }
        }

        /// <summary>
        /// Export onboarding data to CSV
        /// </summary>
        public async Task<Stream> ExportToCsvAsync(OnboardingQueryRequest query)
        {
            // Get data using existing query method
            var result = await QueryAsync(query);
            var data = result.Data;

            // Transform to export format
            var exportData = data.Select(item => new OnboardingExportDto
            {
                Id = item.Id.ToString(),
                CompanyName = item.LeadName,
                LifeCycleStage = item.LifeCycleStageName,
                OnboardStage = item.CurrentStageName,
                UpdatedBy = item.ModifyBy,
                UpdateTime = item.ModifyDate.ToString("yyyy-MM-dd HH:mm:ss"),
                StartDate = item.StartDate?.ToString("yyyy-MM-dd") ?? "",
                Eta = item.EstimatedCompletionDate?.ToString("yyyy-MM-dd") ?? "",
                Priority = item.Priority,
                Progress = (int)item.CompletionRate
            }).ToList();

            // Generate CSV content
            var csvContent = new StringBuilder();
            csvContent.AppendLine("id,companyName,lifeCycleStage,onboardStage,updatedBy,updateTime,startDate,eta,priority,progress");

            foreach (var item in exportData)
            {
                csvContent.AppendLine($"{item.Id}," +
                    $"\"{item.CompanyName?.Replace("\"", "\"\"")}\"," +
                    $"\"{item.LifeCycleStage?.Replace("\"", "\"\"")}\"," +
                    $"\"{item.OnboardStage?.Replace("\"", "\"\"")}\"," +
                    $"\"{item.UpdatedBy?.Replace("\"", "\"\"")}\"," +
                    $"{item.UpdateTime:yyyy-MM-dd HH:mm:ss}," +
                    $"\"{item.StartDate}\"," +
                    $"\"{item.Eta}\"," +
                    $"\"{item.Priority?.Replace("\"", "\"\"")}\"," +
                    $"{item.Progress}");
            }

            // Convert to stream
            var bytes = Encoding.UTF8.GetBytes(csvContent.ToString());
            return new MemoryStream(bytes);
        }

        /// <summary>
        /// Export onboarding data to Excel
        /// </summary>
        public async Task<Stream> ExportToExcelAsync(OnboardingQueryRequest query)
        {
            // Get data using existing query method
            var result = await QueryAsync(query);
            var data = result.Data;

            // Transform to export format
            var exportData = data.Select(item => new OnboardingExportDto
            {
                Id = item.Id.ToString(),
                CompanyName = item.LeadName,
                LifeCycleStage = item.LifeCycleStageName,
                OnboardStage = item.CurrentStageName,
                UpdatedBy = item.ModifyBy,
                UpdateTime = item.ModifyDate.ToString("yyyy-MM-dd HH:mm:ss"),
                StartDate = item.StartDate?.ToString("yyyy-MM-dd") ?? "",
                Eta = item.EstimatedCompletionDate?.ToString("yyyy-MM-dd") ?? "",
                Priority = item.Priority,
                Progress = (int)item.CompletionRate
            }).ToList();

            // Use ExcelHelper to generate Excel file
            return ExcelHelper<OnboardingExportDto>.ExportExcel(exportData);
        }

        /// <summary>
        /// Initialize stages progress array for a new onboarding
        /// </summary>
        private async Task InitializeStagesProgressAsync(Onboarding entity, List<Stage> stages)
        {
            try
            {
                entity.StagesProgress = new List<OnboardingStageProgress>();

                if (stages == null || !stages.Any())
                {
                    Console.WriteLine("No stages found for workflow, skipping stages progress initialization");
                    entity.StagesProgressJson = "[]";
                    return;
                }

                var orderedStages = stages.OrderBy(s => s.Order).ToList();
                var currentTime = DateTimeOffset.UtcNow;

                // Use sequential stage order (1, 2, 3, 4, 5...) instead of the original stage.Order
                for (int i = 0; i < orderedStages.Count; i++)
                {
                    var stage = orderedStages[i];
                    var sequentialOrder = i + 1; // Sequential order starting from 1

                    var stageProgress = new OnboardingStageProgress
                    {
                        StageId = stage.Id,
                        StageName = stage.Name,
                        StageOrder = sequentialOrder, // Use sequential order instead of stage.Order
                        Status = sequentialOrder == 1 ? "InProgress" : "Pending", // First stage starts as InProgress
                        IsCompleted = false,
                        StartTime = sequentialOrder == 1 ? currentTime : null, // First stage starts immediately
                        CompletionTime = null,
                        CompletedById = null,
                        CompletedBy = null,
                        EstimatedDays = stage.EstimatedDuration,
                        Notes = null,
                        IsCurrent = sequentialOrder == 1, // First stage is current
                        StaticFieldsJson = stage.StaticFieldsJson, // 填充静态字段JSON
                        StaticFields = !string.IsNullOrEmpty(stage.StaticFieldsJson)
                            ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(stage.StaticFieldsJson) ?? new List<string>()
                            : new List<string>() // 填充静态字段列表
                    };

                    entity.StagesProgress.Add(stageProgress);

                    Console.WriteLine($"Initialized stage: {stage.Name} with sequential order {sequentialOrder} (original order: {stage.Order})");
                }

                // Serialize to JSON for database storage
                entity.StagesProgressJson = System.Text.Json.JsonSerializer.Serialize(entity.StagesProgress);

                Console.WriteLine($"Initialized {entity.StagesProgress.Count} stages progress entries");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing stages progress: {ex.Message}");
                entity.StagesProgress = new List<OnboardingStageProgress>();
                entity.StagesProgressJson = "[]";
            }
        }

        /// <summary>
        /// Update stages progress - supports non-sequential stage completion
        /// </summary>
        private async Task UpdateStagesProgressAsync(Onboarding entity, long completedStageId, string completedBy = null, long? completedById = null, string notes = null)
        {
            try
            {
                // Deserialize current progress
                if (!string.IsNullOrEmpty(entity.StagesProgressJson))
                {
                    entity.StagesProgress = System.Text.Json.JsonSerializer.Deserialize<List<OnboardingStageProgress>>(entity.StagesProgressJson) ?? new List<OnboardingStageProgress>();
                }

                var currentTime = DateTimeOffset.UtcNow;
                var completedStage = entity.StagesProgress.FirstOrDefault(s => s.StageId == completedStageId);

                if (completedStage != null)
                {
                    Console.WriteLine($"=== UpdateStagesProgressAsync ===");
                    Console.WriteLine($"Completing stage: {completedStage.StageName} (Order: {completedStage.StageOrder})");

                    // Check if stage can be re-completed
                    var wasAlreadyCompleted = completedStage.IsCompleted;

                    // Mark current stage as completed
                    completedStage.Status = "Completed";
                    completedStage.IsCompleted = true;
                    completedStage.CompletionTime = currentTime;
                    completedStage.CompletedBy = completedBy ?? GetCurrentUserName();
                    completedStage.CompletedById = completedById ?? GetCurrentUserId();
                    completedStage.IsCurrent = false;
                    completedStage.CompletionMethod = "Manual";
                    completedStage.LastUpdatedTime = currentTime;
                    completedStage.LastUpdatedBy = completedBy ?? GetCurrentUserName();

                    if (!string.IsNullOrEmpty(notes))
                    {
                        // Append new notes to existing notes if stage was re-completed
                        if (wasAlreadyCompleted && !string.IsNullOrEmpty(completedStage.Notes))
                        {
                            completedStage.Notes += $"\n[Re-completed {currentTime:yyyy-MM-dd HH:mm:ss}] {notes}";
                        }
                        else
                        {
                            completedStage.Notes = notes;
                        }
                    }

                    Console.WriteLine($"Marked stage {completedStage.StageName} as {(wasAlreadyCompleted ? "re-completed" : "completed")}");

                    // Find next stage to activate (first incomplete stage after current completed stage)
                    var nextStage = entity.StagesProgress
                        .Where(s => s.StageOrder > completedStage.StageOrder && !s.IsCompleted)
                        .OrderBy(s => s.StageOrder)
                        .FirstOrDefault();

                    // Clear all current stage flags first
                    foreach (var stage in entity.StagesProgress)
                    {
                        stage.IsCurrent = false;
                    }

                    if (nextStage != null)
                    {
                        // Activate the next incomplete stage
                        nextStage.Status = "InProgress";
                        nextStage.StartTime = currentTime;
                        nextStage.IsCurrent = true;
                        nextStage.LastUpdatedTime = currentTime;
                        nextStage.LastUpdatedBy = completedBy ?? GetCurrentUserName();

                        Console.WriteLine($"Started next stage: {nextStage.StageName} (Order: {nextStage.StageOrder})");
                    }
                    else
                    {
                        // Check if there are any incomplete stages with lower order (in case of non-sequential completion)
                        var earlierIncompleteStage = entity.StagesProgress
                            .Where(s => !s.IsCompleted)
                            .OrderBy(s => s.StageOrder)
                            .FirstOrDefault();

                        if (earlierIncompleteStage != null)
                        {
                            // Activate the earliest incomplete stage
                            earlierIncompleteStage.Status = "InProgress";
                            earlierIncompleteStage.StartTime = currentTime;
                            earlierIncompleteStage.IsCurrent = true;
                            earlierIncompleteStage.LastUpdatedTime = currentTime;
                            earlierIncompleteStage.LastUpdatedBy = completedBy ?? GetCurrentUserName();

                            Console.WriteLine($"Started earliest incomplete stage: {earlierIncompleteStage.StageName} (Order: {earlierIncompleteStage.StageOrder})");
                        }
                        else
                        {
                            Console.WriteLine("All stages completed - no more stages to activate");
                        }
                    }

                    // Update completion rate based on completed stages
                    entity.CompletionRate = CalculateCompletionRateByCompletedStages(entity.StagesProgress);

                    Console.WriteLine($"Updated completion rate: {entity.CompletionRate}% (based on completed stages count)");
                }

                // Serialize back to JSON
                entity.StagesProgressJson = System.Text.Json.JsonSerializer.Serialize(entity.StagesProgress);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating stages progress: {ex.Message}");
            }
        }

        /// <summary>
        /// Load stages progress from JSON - 优化版本，减少不必要的处理
        /// </summary>
        private void LoadStagesProgressFromJson(Onboarding entity)
        {
            try
            {
                if (!string.IsNullOrEmpty(entity.StagesProgressJson))
                {
                    entity.StagesProgress = JsonSerializer.Deserialize<List<OnboardingStageProgress>>(entity.StagesProgressJson) ?? new List<OnboardingStageProgress>();

                    // 只在必要时处理 StaticFields，避免每次都反序列化
                    foreach (var stageProgress in entity.StagesProgress)
                    {
                        if (stageProgress.StaticFields == null)
                        {
                            if (!string.IsNullOrEmpty(stageProgress.StaticFieldsJson))
                            {
                                try
                                {
                                    stageProgress.StaticFields = JsonSerializer.Deserialize<List<string>>(stageProgress.StaticFieldsJson) ?? new List<string>();
                                }
                                catch
                                {
                                    stageProgress.StaticFields = new List<string>();
                                }
                            }
                            else
                            {
                                stageProgress.StaticFields = new List<string>();
                            }
                        }
                    }

                    // 只在需要时修复阶段顺序，避免不必要的序列化
                    if (NeedsStageOrderFix(entity.StagesProgress))
                    {
                        FixStageOrderSequence(entity.StagesProgress);
                        entity.StagesProgressJson = JsonSerializer.Serialize(entity.StagesProgress);
                    }
                }
                else
                {
                    entity.StagesProgress = new List<OnboardingStageProgress>();
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"Error loading stages progress from JSON: {ex.Message}");
#endif
                entity.StagesProgress = new List<OnboardingStageProgress>();
            }
        }

        /// <summary>
        /// 检查是否需要修复阶段顺序
        /// </summary>
        private bool NeedsStageOrderFix(List<OnboardingStageProgress> stagesProgress)
        {
            if (stagesProgress == null || !stagesProgress.Any())
                return false;

            var orderedStages = stagesProgress.OrderBy(s => s.StageOrder).ToList();
            for (int i = 0; i < orderedStages.Count; i++)
            {
                if (orderedStages[i].StageOrder != i + 1)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Fix stage order to be sequential (1, 2, 3, 4, 5...) instead of potentially non-consecutive orders
        /// </summary>
        private void FixStageOrderSequence(List<OnboardingStageProgress> stagesProgress)
        {
            try
            {
                if (stagesProgress == null || !stagesProgress.Any())
                {
                    return;
                }

                // Sort by current stage order to maintain the original sequence
                var orderedStages = stagesProgress.OrderBy(s => s.StageOrder).ToList();

                // Check if stage orders are already sequential
                bool needsFixing = false;
                for (int i = 0; i < orderedStages.Count; i++)
                {
                    if (orderedStages[i].StageOrder != i + 1)
                    {
                        needsFixing = true;
                        break;
                    }
                }

                if (!needsFixing)
                {
                    Console.WriteLine("Stage orders are already sequential, no fixing needed");
                    return;
                }

                Console.WriteLine("Fixing stage order sequence...");

                // Reassign sequential stage orders
                for (int i = 0; i < orderedStages.Count; i++)
                {
                    var oldOrder = orderedStages[i].StageOrder;
                    var newOrder = i + 1;

                    orderedStages[i].StageOrder = newOrder;

                    Console.WriteLine($"Fixed stage '{orderedStages[i].StageName}': order {oldOrder} -> {newOrder}");
                }

                // Update the original list with fixed orders safely
                // Instead of modifying the list during enumeration, replace each item individually
                for (int i = 0; i < stagesProgress.Count; i++)
                {
                    var matchingStage = orderedStages.FirstOrDefault(s => s.StageId == stagesProgress[i].StageId);
                    if (matchingStage != null)
                    {
                        stagesProgress[i] = matchingStage;
                    }
                }

                Console.WriteLine($"Successfully fixed stage order sequence for {stagesProgress.Count} stages");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fixing stage order sequence: {ex.Message}");
            }
        }

        /// <summary>
        /// Validate if a stage can be completed based on business rules
        /// </summary>
        private async Task<(bool CanComplete, string ErrorMessage)> ValidateStageCanBeCompletedAsync(Onboarding entity, long stageId)
        {
            try
            {
                Console.WriteLine($"=== ValidateStageCanBeCompletedAsync ===");
                Console.WriteLine($"Onboarding ID: {entity.Id}");
                Console.WriteLine($"Stage ID to complete: {stageId}");
                Console.WriteLine($"Current Stage ID: {entity.CurrentStageId}");

                // Load stages progress
                LoadStagesProgressFromJson(entity);

                if (entity.StagesProgress == null || !entity.StagesProgress.Any())
                {
                    return (false, "No stages progress found");
                }

                var stageToComplete = entity.StagesProgress.FirstOrDefault(s => s.StageId == stageId);
                if (stageToComplete == null)
                {
                    return (false, "Stage not found in progress");
                }

                Console.WriteLine($"Stage to complete: {stageToComplete.StageName} (Order: {stageToComplete.StageOrder})");

                // Check if stage is already completed
                if (stageToComplete.IsCompleted)
                {
                    Console.WriteLine($"⚠️ WARNING: Stage '{stageToComplete.StageName}' is already completed");
                    Console.WriteLine($"📝 Allowing re-completion for flexibility");
                    // Don't return false, allow re-completion but log the warning
                }

                // Check onboarding status
                if (entity.Status == "Completed")
                {
                    return (false, "Onboarding is already completed");
                }

                if (entity.Status == "Cancelled" || entity.Status == "Rejected")
                {
                    return (false, $"Cannot complete stages when onboarding status is {entity.Status}");
                }

                // Allow completing any non-completed stage (removed sequential restriction)
                Console.WriteLine($"✅ Stage '{stageToComplete.StageName}' can be completed (non-sequential completion allowed)");
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error validating stage completion: {ex.Message}");
                return (false, $"Validation error: {ex.Message}");
            }
        }

        /// <summary>
        /// Validate stage completion order - ensures stages are completed in sequence
        /// </summary>
        private bool ValidateStageCompletionOrder(List<OnboardingStageProgress> stagesProgress, OnboardingStageProgress stageToComplete)
        {
            try
            {
                if (stagesProgress == null || !stagesProgress.Any() || stageToComplete == null)
                {
                    return false;
                }

                // If this is the first stage (order 1), it can always be completed
                if (stageToComplete.StageOrder == 1)
                {
                    Console.WriteLine($"Stage '{stageToComplete.StageName}' is the first stage, allowing completion");
                    return true;
                }

                // Check if all previous stages are completed
                var previousStages = stagesProgress
                    .Where(s => s.StageOrder < stageToComplete.StageOrder)
                    .OrderBy(s => s.StageOrder)
                    .ToList();

                var incompleteStages = previousStages.Where(s => !s.IsCompleted).ToList();

                if (incompleteStages.Any())
                {
                    Console.WriteLine($"Cannot complete stage '{stageToComplete.StageName}' - {incompleteStages.Count} previous stages are incomplete:");
                    foreach (var incompleteStage in incompleteStages)
                    {
                        Console.WriteLine($"  - Stage '{incompleteStage.StageName}' (Order: {incompleteStage.StageOrder}) is {incompleteStage.Status}");
                    }
                    return false;
                }

                Console.WriteLine($"All previous stages are completed, allowing completion of stage '{stageToComplete.StageName}'");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error validating stage completion order: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Calculate completion rate based on stage order progression
        /// This method ensures that progress is calculated correctly even when stage orders are not consecutive (e.g., 1, 3, 4, 5)
        /// </summary>
        private decimal CalculateCompletionRateByStageOrder(List<OnboardingStageProgress> stagesProgress)
        {
            try
            {
                if (stagesProgress == null || !stagesProgress.Any())
                {
                    return 0;
                }

                // Sort stages by order to ensure correct progression calculation
                var orderedStages = stagesProgress.OrderBy(s => s.StageOrder).ToList();

                // Get all unique stage orders
                var stageOrders = orderedStages.Select(s => s.StageOrder).Distinct().OrderBy(o => o).ToList();

                if (!stageOrders.Any())
                {
                    return 0;
                }

                // Calculate completion based on stage order progression
                var completedStageOrders = orderedStages
                    .Where(s => s.IsCompleted)
                    .Select(s => s.StageOrder)
                    .Distinct()
                    .OrderBy(o => o)
                    .ToList();

                if (!completedStageOrders.Any())
                {
                    return 0;
                }

                // Find the highest completed stage order
                var highestCompletedOrder = completedStageOrders.Max();

                // Calculate progress based on stage order position
                var totalStageOrderRange = stageOrders.Max() - stageOrders.Min() + 1;
                var completedStageOrderRange = highestCompletedOrder - stageOrders.Min() + 1;

                // Alternative calculation: based on completed stages count vs total stages count
                var completedStagesCount = completedStageOrders.Count;
                var totalStagesCount = stageOrders.Count;

                // Use the more accurate calculation method
                decimal completionRate;

                if (totalStagesCount > 0)
                {
                    // Method 1: Simple count-based calculation (more intuitive)
                    completionRate = Math.Round((decimal)completedStagesCount / totalStagesCount * 100, 2);

                    Console.WriteLine($"Stage Order Calculation Details:");
                    Console.WriteLine($"- Stage Orders: [{string.Join(", ", stageOrders)}]");
                    Console.WriteLine($"- Completed Stage Orders: [{string.Join(", ", completedStageOrders)}]");
                    Console.WriteLine($"- Completed Stages: {completedStagesCount}/{totalStagesCount}");
                    Console.WriteLine($"- Completion Rate: {completionRate}%");
                }
                else
                {
                    completionRate = 0;
                }

                return completionRate;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating completion rate by stage order: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Get TenantId from onboarding
        /// </summary>
        private string GetTenantIdFromOnboarding(long onboardingId)
        {
            try
            {
                var onboarding = _onboardingRepository.GetByIdAsync(onboardingId).Result;
                return onboarding?.TenantId ?? "default";
            }
            catch
            {
                return "default";
            }
        }

        /// <summary>
        /// Get current user name from UserContext
        /// </summary>
        private string GetCurrentUserName()
        {
            return !string.IsNullOrEmpty(_userContext?.UserName) ? _userContext.UserName : "System";
        }

        /// <summary>
        /// Get current user email from UserContext
        /// </summary>
        private string GetCurrentUserEmail()
        {
            return !string.IsNullOrEmpty(_userContext?.Email) ? _userContext.Email : "system@example.com";
        }

        /// <summary>
        /// Get current user ID from UserContext
        /// </summary>
        private long? GetCurrentUserId()
        {
            if (long.TryParse(_userContext?.UserId, out long userId))
            {
                return userId;
            }
            return null;
        }

        /// <summary>
        /// Get current user full name from UserContext
        /// </summary>
        private string GetCurrentUserFullName()
        {
            if (_userContext != null)
            {
                var fullName = $"{_userContext.FirstName} {_userContext.LastName}".Trim();
                if (!string.IsNullOrEmpty(fullName))
                {
                    return fullName;
                }
                return !string.IsNullOrEmpty(_userContext.UserName) ? _userContext.UserName : "System";
            }
            return "System";
        }

        /// <summary>
        /// Calculate completion rate based on completed stages count
        /// This method calculates progress based on how many stages are completed vs total stages
        /// Supports non-sequential stage completion
        /// </summary>
        private decimal CalculateCompletionRateByCompletedStages(List<OnboardingStageProgress> stagesProgress)
        {
            try
            {
                if (stagesProgress == null || !stagesProgress.Any())
                {
                    return 0;
                }

                var totalStagesCount = stagesProgress.Count;
                var completedStagesCount = stagesProgress.Count(s => s.IsCompleted);

                if (totalStagesCount == 0)
                {
                    return 0;
                }

                var completionRate = Math.Round((decimal)completedStagesCount / totalStagesCount * 100, 2);

                Console.WriteLine($"Completion Rate Calculation:");
                Console.WriteLine($"- Total Stages: {totalStagesCount}");
                Console.WriteLine($"- Completed Stages: {completedStagesCount}");
                Console.WriteLine($"- Completion Rate: {completionRate}%");
                Console.WriteLine($"- Completed Stages: [{string.Join(", ", stagesProgress.Where(s => s.IsCompleted).Select(s => $"{s.StageOrder}:{s.StageName}"))}]");
                Console.WriteLine($"- Incomplete Stages: [{string.Join(", ", stagesProgress.Where(s => !s.IsCompleted).Select(s => $"{s.StageOrder}:{s.StageName}"))}]");

                return completionRate;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating completion rate by completed stages: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 清理相关缓存数据
        /// </summary>
        private async Task ClearRelatedCacheAsync(long? workflowId = null, long? stageId = null)
        {
            try
            {
                // 安全获取租户ID
                var tenantId = !string.IsNullOrEmpty(_userContext?.TenantId)
                    ? _userContext.TenantId.ToLowerInvariant()
                    : "default";

                var tasks = new List<Task>();

                if (workflowId.HasValue)
                {
                    var workflowCacheKey = $"{WORKFLOW_CACHE_PREFIX}:{tenantId}:{workflowId.Value}";
                    // Redis缓存暂时禁用
            // tasks.Add(_redisService.KeyDelAsync(workflowCacheKey));
                }

                if (stageId.HasValue)
                {
                    var stageCacheKey = $"{STAGE_CACHE_PREFIX}:{tenantId}:{stageId.Value}";
                    // Redis缓存暂时禁用
            // tasks.Add(_redisService.KeyDelAsync(stageCacheKey));
                }

                if (tasks.Any())
                {
                    await Task.WhenAll(tasks);
#if DEBUG
                    Console.WriteLine($"Cleared cache for workflow:{workflowId}, stage:{stageId}");
#endif
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to clear cache: {ex.Message}");
                // 缓存清理失败不应影响主流程
            }
        }

        /// <summary>
        /// 批量清理工作流相关的所有缓存
        /// </summary>
        private async Task ClearWorkflowRelatedCacheAsync(long workflowId)
        {
            try
            {
                // 安全获取租户ID
                var tenantId = !string.IsNullOrEmpty(_userContext?.TenantId)
                    ? _userContext.TenantId.ToLowerInvariant()
                    : "default";

                // 清理工作流缓存
                var workflowCacheKey = $"{WORKFLOW_CACHE_PREFIX}:{tenantId}:{workflowId}";
                // Redis缓存暂时禁用
            await Task.CompletedTask;

                // 获取该工作流下的所有阶段并清理缓存
                var stages = await _stageRepository.GetByWorkflowIdAsync(workflowId);
                var stageCacheTasks = stages.Select(stage =>
                {
                    var stageCacheKey = $"{STAGE_CACHE_PREFIX}:{tenantId}:{stage.Id}";
                    // Redis缓存暂时禁用
            return Task.CompletedTask;
                });

                await Task.WhenAll(stageCacheTasks);

#if DEBUG
                Console.WriteLine($"Cleared all cache for workflow {workflowId} and its {stages.Count} stages");
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to clear workflow related cache: {ex.Message}");
            }
        }

        /// <summary>
        /// 清除 Onboarding 查询缓存
        /// </summary>
        private async Task ClearOnboardingQueryCacheAsync()
        {
            try
            {
                string tenantId = _userContext?.TenantId ?? "default";

                // 使用 Keys 方法获取所有匹配的键，然后批量删除
                var pattern = $"ow:onboarding:query:{tenantId}:*";
                // Redis缓存暂时禁用
            var keys = new List<string>();

                if (keys != null && keys.Any())
                {
                    // 批量删除所有匹配的键
                    // Redis缓存暂时禁用
                var deleteTasks = keys.Select(key => Task.CompletedTask);
                    await Task.WhenAll(deleteTasks);

#if DEBUG
                    Console.WriteLine($"Cleared onboarding query cache entries matching pattern: {pattern}");
#endif
                }
                else
                {
#if DEBUG
                    Console.WriteLine($"No cache entries found matching pattern: {pattern}");
#endif
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to clear onboarding query cache: {ex.Message}");
                // 缓存清理失败不应影响主流程
            }
        }
    }
}