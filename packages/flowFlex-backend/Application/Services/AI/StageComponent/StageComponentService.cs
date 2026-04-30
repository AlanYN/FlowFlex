using System.Text.Json;
using FlowFlex.Application.Contracts.Dtos.OW.Checklist;
using FlowFlex.Application.Contracts.Dtos.OW.ChecklistTask;
using FlowFlex.Application.Contracts.Dtos.OW.Questionnaire;
using FlowFlex.Application.Contracts.Dtos.OW.Stage;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.IServices.AI;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace FlowFlex.Application.Services.AI.StageComponent
{
    /// <summary>
    /// Service responsible for creating stage components (Checklists and Questionnaires)
    /// from AI generation results and associating them with workflow stages.
    /// </summary>
    public class StageComponentService : IStageComponentService, IScopedService
    {
        private readonly IChecklistService _checklistService;
        private readonly IQuestionnaireService _questionnaireService;
        private readonly IChecklistRepository _checklistRepository;
        private readonly IQuestionnaireRepository _questionnaireRepository;
        private readonly IChecklistTaskService _checklistTaskService;
        private readonly IComponentMappingService _componentMappingService;
        private readonly IStageRepository _stageRepository;
        private readonly IWorkflowService _workflowService;
        private readonly IAIResponseParser _responseParser;
        private readonly ILogger<StageComponentService> _logger;

        public StageComponentService(
            IChecklistService checklistService,
            IQuestionnaireService questionnaireService,
            IChecklistRepository checklistRepository,
            IQuestionnaireRepository questionnaireRepository,
            IChecklistTaskService checklistTaskService,
            IComponentMappingService componentMappingService,
            IStageRepository stageRepository,
            IWorkflowService workflowService,
            IAIResponseParser responseParser,
            ILogger<StageComponentService> logger)
        {
            _checklistService = checklistService;
            _questionnaireService = questionnaireService;
            _checklistRepository = checklistRepository;
            _questionnaireRepository = questionnaireRepository;
            _checklistTaskService = checklistTaskService;
            _componentMappingService = componentMappingService;
            _stageRepository = stageRepository;
            _workflowService = workflowService;
            _responseParser = responseParser;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<bool> CreateStageComponentsAsync(long workflowId, List<AIStageGenerationResult> stages, List<AIChecklistGenerationResult> checklists, List<AIQuestionnaireGenerationResult> questionnaires)
        {
            try
            {
                _logger.LogInformation("Creating stage components for workflow {WorkflowId}...", workflowId);

                // First try to get stages from database, but fallback to provided stages if database query fails
                var workflow = await _workflowService.GetByIdAsync(workflowId);
                List<StageOutputDto> createdStages = null;

                if (workflow?.Stages != null && workflow.Stages.Any())
                {
                    _logger.LogInformation("Using stages from database: {StageCount} stages found", workflow.Stages.Count);
                    createdStages = workflow.Stages;
                }
                else
                {
                    _logger.LogWarning("No stages found in database for workflow {WorkflowId}, using provided stages data", workflowId);

                    // Use provided stages data and try to find corresponding database records by name
                    if (stages == null || !stages.Any())
                    {
                        _logger.LogError("No stages provided in request and none found in database for workflow {WorkflowId}", workflowId);
                        return false;
                    }

                    // Try to find stages in database by name and order
                    createdStages = new List<StageOutputDto>();
                    foreach (var providedStage in stages.OrderBy(s => s.Order))
                    {
                        try
                        {
                            // Query stages by workflow ID and name
                            var dbStages = await _stageRepository.GetByWorkflowIdAsync(workflowId);
                            var matchingStage = dbStages?.FirstOrDefault(s =>
                                s.Name.Equals(providedStage.Name, StringComparison.OrdinalIgnoreCase) &&
                                s.Order == providedStage.Order);

                            if (matchingStage != null)
                            {
                                // Convert to StageOutputDto
                                var stageDto = new StageOutputDto
                                {
                                    Id = matchingStage.Id,
                                    Name = matchingStage.Name,
                                    Description = matchingStage.Description,
                                    Order = matchingStage.Order,
                                    DefaultAssignedGroup = matchingStage.DefaultAssignedGroup,
                                    EstimatedDuration = matchingStage.EstimatedDuration,
                                    IsActive = matchingStage.IsActive
                                };
                                createdStages.Add(stageDto);
                                _logger.LogInformation("Found matching stage in database: {StageName} (ID: {StageId})", matchingStage.Name, matchingStage.Id);
                            }
                            else
                            {
                                _logger.LogWarning("Could not find stage '{StageName}' in database for workflow {WorkflowId}", providedStage.Name, workflowId);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error finding stage '{StageName}' in database", providedStage.Name);
                        }
                    }

                    if (!createdStages.Any())
                    {
                        _logger.LogError("Could not find any matching stages in database for workflow {WorkflowId}", workflowId);
                        return false;
                    }

                    _logger.LogInformation("Using {StageCount} stages found by name matching", createdStages.Count);
                }

                // Create checklists and associate with stages
                await CreateChecklistsForStagesAsync(checklists, createdStages);

                // Create questionnaires and associate with stages
                await CreateQuestionnairesForStagesAsync(questionnaires, createdStages);

                // Update stage components to link the created checklists and questionnaires
                await UpdateStageComponentsAsync(createdStages, checklists, questionnaires);

                // Sync the component mappings for the entire workflow
                try
                {
                    await _componentMappingService.SyncWorkflowMappingsAsync(workflowId);
                    _logger.LogInformation("Successfully synced component mappings for AI-generated workflow {WorkflowId}", workflowId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to sync component mappings for workflow {WorkflowId}: {Error}", workflowId, ex.Message);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create stage components for workflow {WorkflowId}", workflowId);
                return false;
            }
        }

        #region Private Methods - Checklist/Questionnaire Creation

        private async Task CreateChecklistsForStagesAsync(List<AIChecklistGenerationResult> checklists, List<StageOutputDto> createdStages)
        {
            foreach (var checklist in checklists)
            {
                try
                {
                    // Find the corresponding stage using StageName and StageOrder from the checklist
                    StageOutputDto stage = null;

                    // First try to match by StageName if provided
                    if (!string.IsNullOrEmpty(checklist.StageName))
                    {
                        stage = createdStages.FirstOrDefault(s =>
                            s.Name.Equals(checklist.StageName, StringComparison.OrdinalIgnoreCase));
                        _logger.LogInformation("Looking for stage by name '{StageName}' for checklist '{ChecklistName}'",
                            checklist.StageName, checklist.GeneratedChecklist?.Name);
                    }

                    // If not found by name, try to match by StageOrder
                    if (stage == null && checklist.StageOrder > 0)
                    {
                        stage = createdStages.FirstOrDefault(s => s.Order == checklist.StageOrder);
                        _logger.LogInformation("Looking for stage by order {StageOrder} for checklist '{ChecklistName}'",
                            checklist.StageOrder, checklist.GeneratedChecklist?.Name);
                    }

                    // If still not found, skip this checklist
                    if (stage == null)
                    {
                        _logger.LogWarning("Could not find matching stage for checklist '{ChecklistName}' (StageName: '{StageName}', StageOrder: {StageOrder})",
                            checklist.GeneratedChecklist?.Name, checklist.StageName, checklist.StageOrder);
                        continue;
                    }

                    _logger.LogInformation("Found matching stage '{StageName}' (ID: {StageId}) for checklist '{ChecklistName}'",
                        stage.Name, stage.Id, checklist.GeneratedChecklist?.Name);

                    // Ensure GeneratedChecklist is not null
                    if (checklist.GeneratedChecklist == null)
                    {
                        _logger.LogWarning("Skipping checklist '{ChecklistName}' - GeneratedChecklist is null", checklist.GeneratedChecklist?.Name ?? "Unknown");
                        continue;
                    }

                    // Ensure unique checklist name
                    checklist.GeneratedChecklist.Name = await EnsureUniqueChecklistNameAsync(checklist.GeneratedChecklist.Name, checklist.GeneratedChecklist.Team);

                    // Set up assignments for the checklist (assignments are now managed through Stage Components)
                    checklist.GeneratedChecklist.Assignments = new List<FlowFlex.Application.Contracts.Dtos.OW.Common.AssignmentDto>();

                    // Create the checklist
                    var checklistId = await _checklistService.CreateAsync(checklist.GeneratedChecklist);
                    _logger.LogInformation("Created checklist {ChecklistId} for stage {StageId}", checklistId, stage.Id);

                    // Create tasks for the checklist
                    await CreateChecklistTasksAsync(checklist, checklistId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create checklist '{ChecklistName}' for stage", checklist.GeneratedChecklist?.Name ?? "Unknown");
                }
            }
        }

        private async Task CreateChecklistTasksAsync(AIChecklistGenerationResult checklist, long checklistId)
        {
            if (checklist.Tasks == null || !checklist.Tasks.Any())
                return;

            foreach (var task in checklist.Tasks)
            {
                try
                {
                    var taskInputDto = new ChecklistTaskInputDto
                    {
                        ChecklistId = checklistId,
                        Name = task.Title,
                        Description = task.Description,
                        IsRequired = task.IsRequired,
                        EstimatedHours = task.EstimatedMinutes > 0 ? task.EstimatedMinutes / 60 : 0, // Convert minutes to hours
                        TaskType = task.Category ?? "General",
                        Order = checklist.Tasks.ToList().IndexOf(task) + 1,
                        IsActive = true
                    };

                    var taskId = await _checklistTaskService.CreateAsync(taskInputDto);
                    _logger.LogInformation("Created task {TaskId} for checklist {ChecklistId}", taskId, checklistId);
                }
                catch (Exception taskEx)
                {
                    _logger.LogWarning(taskEx, "Failed to create task '{TaskTitle}' for checklist {ChecklistId}", task.Title, checklistId);
                }
            }
        }

        private async Task CreateQuestionnairesForStagesAsync(List<AIQuestionnaireGenerationResult> questionnaires, List<StageOutputDto> createdStages)
        {
            foreach (var questionnaire in questionnaires)
            {
                try
                {
                    // Find the corresponding stage using StageName and StageOrder from the questionnaire
                    StageOutputDto stage = null;

                    // First try to match by StageName if provided
                    if (!string.IsNullOrEmpty(questionnaire.StageName))
                    {
                        stage = createdStages.FirstOrDefault(s =>
                            s.Name.Equals(questionnaire.StageName, StringComparison.OrdinalIgnoreCase));
                        _logger.LogInformation("Looking for stage by name '{StageName}' for questionnaire '{QuestionnaireName}'",
                            questionnaire.StageName, questionnaire.GeneratedQuestionnaire?.Name);
                    }

                    // If not found by name, try to match by StageOrder
                    if (stage == null && questionnaire.StageOrder > 0)
                    {
                        stage = createdStages.FirstOrDefault(s => s.Order == questionnaire.StageOrder);
                        _logger.LogInformation("Looking for stage by order {StageOrder} for questionnaire '{QuestionnaireName}'",
                            questionnaire.StageOrder, questionnaire.GeneratedQuestionnaire?.Name);
                    }

                    // If still not found, skip this questionnaire
                    if (stage == null)
                    {
                        _logger.LogWarning("Could not find matching stage for questionnaire '{QuestionnaireName}' (StageName: '{StageName}', StageOrder: {StageOrder})",
                            questionnaire.GeneratedQuestionnaire?.Name, questionnaire.StageName, questionnaire.StageOrder);
                        continue;
                    }

                    _logger.LogInformation("Found matching stage '{StageName}' (ID: {StageId}) for questionnaire '{QuestionnaireName}'",
                        stage.Name, stage.Id, questionnaire.GeneratedQuestionnaire?.Name);

                    // Ensure GeneratedQuestionnaire is not null
                    if (questionnaire.GeneratedQuestionnaire == null)
                    {
                        _logger.LogWarning("Skipping questionnaire '{QuestionnaireName}' - GeneratedQuestionnaire is null", questionnaire.GeneratedQuestionnaire?.Name ?? "Unknown");
                        continue;
                    }

                    // Ensure unique questionnaire name
                    questionnaire.GeneratedQuestionnaire.Name = await EnsureUniqueQuestionnaireNameAsync(questionnaire.GeneratedQuestionnaire.Name);

                    // Build structure JSON from questions
                    BuildQuestionnaireStructureJson(questionnaire);

                    // Set up assignments for the questionnaire (assignments are now managed through Stage Components)
                    questionnaire.GeneratedQuestionnaire.Assignments = new List<FlowFlex.Application.Contracts.Dtos.OW.Common.AssignmentDto>();

                    // Create the questionnaire
                    var questionnaireId = await _questionnaireService.CreateAsync(questionnaire.GeneratedQuestionnaire);
                    _logger.LogInformation("Created questionnaire {QuestionnaireId} for stage {StageId} with {QuestionCount} questions",
                        questionnaireId, stage.Id, questionnaire.Questions?.Count ?? 0);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create questionnaire '{QuestionnaireName}' for stage", questionnaire.GeneratedQuestionnaire?.Name ?? "Unknown");
                }
            }
        }

        private void BuildQuestionnaireStructureJson(AIQuestionnaireGenerationResult questionnaire)
        {
            _logger.LogInformation("Processing questionnaire with {QuestionCount} questions", questionnaire.Questions?.Count ?? 0);

            if (questionnaire.Questions == null || !questionnaire.Questions.Any())
            {
                _logger.LogWarning("No questions found for questionnaire '{QuestionnaireName}'", questionnaire.GeneratedQuestionnaire?.Name ?? "Unknown");
                return;
            }

            _logger.LogInformation("Creating section with {QuestionCount} questions", questionnaire.Questions.Count);

            var section = new QuestionnaireSectionInputDto
            {
                Title = "Main Section",
                Description = "Generated questions",
                Order = 1,
                Questions = questionnaire.Questions.Select((q, index) => new QuestionInputDto
                {
                    Id = q.Id,
                    Text = q.Question,
                    Type = q.Type ?? "short_answer",
                    IsRequired = q.IsRequired,
                    Order = index + 1,
                    Options = _responseParser.ParseQuestionOptions(q.Options, index)
                }).ToList()
            };

            // Convert sections to JSON structure for storage
            var structureJson = new
            {
                sections = new[]
                {
                    new
                    {
                        title = section.Title,
                        description = section.Description,
                        order = section.Order,
                        questions = section.Questions.Select(q => new
                        {
                            id = q.Id,
                            title = q.Text,
                            type = q.Type,
                            required = q.IsRequired,
                            order = q.Order,
                            options = q.Options.Select(opt => new
                            {
                                id = opt.Id,
                                label = opt.Label,
                                value = opt.Value,
                                order = opt.Order
                            }).ToArray()
                        }).ToArray()
                    }
                }
            };

            questionnaire.GeneratedQuestionnaire.StructureJson = JsonSerializer.Serialize(structureJson);
            _logger.LogInformation("Added structure JSON with {QuestionCount} questions to questionnaire", section.Questions.Count);
        }

        #endregion

        #region Private Methods - Stage Component Linking

        private async Task UpdateStageComponentsAsync(List<StageOutputDto> createdStages, List<AIChecklistGenerationResult> checklists, List<AIQuestionnaireGenerationResult> questionnaires)
        {
            for (int i = 0; i < createdStages.Count; i++)
            {
                var stage = createdStages[i];
                var stageComponents = new List<dynamic>();

                // Add checklist component if we have a checklist for this stage
                if (i < checklists.Count && checklists[i].GeneratedChecklist != null)
                {
                    // Get the created checklist ID (find it by name)
                    var checklistName = checklists[i].GeneratedChecklist.Name;
                    var checklistEntities = await _checklistRepository.GetByNameAsync(checklistName);
                    var checklistEntity = checklistEntities?.FirstOrDefault();

                    if (checklistEntity != null)
                    {
                        stageComponents.Add(new
                        {
                            Key = "checklist",
                            Order = 1,
                            IsEnabled = true,
                            Configuration = (object)null,
                            StaticFields = new List<object>(),
                            ChecklistIds = new List<long> { checklistEntity.Id },
                            QuestionnaireIds = new List<long>(),
                            ChecklistNames = new List<string> { checklistEntity.Name },
                            QuestionnaireNames = new List<string>()
                        });
                    }
                }

                // Add questionnaire component if we have a questionnaire for this stage
                if (i < questionnaires.Count && questionnaires[i].GeneratedQuestionnaire != null)
                {
                    // Get the created questionnaire ID
                    var questionnaireName = questionnaires[i].GeneratedQuestionnaire.Name;
                    var questionnaireEntity = await _questionnaireRepository.GetFirstAsync(q => q.Name == questionnaireName && q.IsValid);

                    if (questionnaireEntity != null)
                    {
                        stageComponents.Add(new
                        {
                            Key = "questionnaires",
                            Order = stageComponents.Count + 1,
                            IsEnabled = true,
                            Configuration = (object)null,
                            StaticFields = new List<object>(),
                            ChecklistIds = new List<long>(),
                            QuestionnaireIds = new List<long> { questionnaireEntity.Id },
                            ChecklistNames = new List<string>(),
                            QuestionnaireNames = new List<string> { questionnaireEntity.Name }
                        });
                    }
                }

                // Update stage components if we have any
                if (stageComponents.Any())
                {
                    try
                    {
                        var stageEntity = await _stageRepository.GetByIdAsync(stage.Id);
                        if (stageEntity != null)
                        {
                            stageEntity.ComponentsJson = JsonSerializer.Serialize(stageComponents);
                            await _stageRepository.UpdateAsync(stageEntity);

                            _logger.LogInformation("Updated stage {StageId} components with {ComponentCount} components",
                                stage.Id, stageComponents.Count);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to update components for stage {StageId}: {Error}", stage.Id, ex.Message);
                    }
                }
            }
        }

        #endregion

        #region Distribution Logic

        /// <summary>
        /// Determine the number of checklists for a stage based on its complexity and characteristics
        /// </summary>
        internal int DetermineChecklistCount(AIStageGenerationResult stage, Random random)
        {
            var stageName = stage.Name.ToLower();
            var stageDesc = stage.Description.ToLower();
            var duration = stage.EstimatedDuration;

            // Complex stages get more checklists
            var baseCount = 1;

            // Increase count for complex/long stages
            if (duration > 5) baseCount++;
            if (stageName.Contains("implementation") || stageName.Contains("develop") || stageName.Contains("build")) baseCount++;
            if (stageName.Contains("testing") || stageName.Contains("qa")) baseCount++;
            if (stageName.Contains("deployment") || stageName.Contains("launch")) baseCount++;

            // Add randomness (0-2 additional checklists)
            var randomAddition = random.Next(0, 3);

            // Return between 1-4 checklists, weighted towards 1-2
            return Math.Min(4, Math.Max(1, baseCount + (randomAddition == 0 ? 0 : randomAddition - 1)));
        }

        /// <summary>
        /// Determine the number of questionnaires for a stage based on its complexity and characteristics
        /// </summary>
        internal int DetermineQuestionnaireCount(AIStageGenerationResult stage, Random random)
        {
            var stageName = stage.Name.ToLower();
            var stageDesc = stage.Description.ToLower();
            var duration = stage.EstimatedDuration;

            // Information-gathering stages get more questionnaires
            var baseCount = 1;

            // Increase count for information-heavy stages
            if (stageName.Contains("initial") || stageName.Contains("assessment") || stageName.Contains("analysis")) baseCount++;
            if (stageName.Contains("design") || stageName.Contains("planning")) baseCount++;
            if (stageName.Contains("review") || stageName.Contains("approval")) baseCount++;
            if (duration > 7) baseCount++;

            // Add randomness (0-1 additional questionnaires, less frequent than checklists)
            var randomAddition = random.Next(0, 4); // 0, 1, 2, 3 - only add if 3

            // Return between 1-3 questionnaires, weighted towards 1
            return Math.Min(3, Math.Max(1, baseCount + (randomAddition == 3 ? 1 : 0)));
        }

        /// <summary>
        /// Determine the number of tasks for a checklist
        /// </summary>
        internal int DetermineTaskCount(int availableTasks, int checklistIndex, int totalChecklists, Random random)
        {
            // Base count: 2-6 tasks, weighted towards 3-4
            var weights = new[] { 2, 3, 4, 4, 5, 6 }; // More weight on 3-4 tasks
            var baseCount = weights[random.Next(weights.Length)];

            // Ensure we don't exceed available tasks
            var maxCount = Math.Min(availableTasks, 6);
            var minCount = Math.Min(2, maxCount);

            return Math.Min(maxCount, Math.Max(minCount, baseCount));
        }

        /// <summary>
        /// Determine the number of questions for a questionnaire
        /// </summary>
        internal int DetermineQuestionCount(int availableQuestions, int questionnaireIndex, int totalQuestionnaires, Random random)
        {
            // Base count: 2-8 questions, weighted towards 4-6
            var weights = new[] { 2, 3, 4, 4, 5, 5, 6, 6, 7, 8 }; // More weight on 4-6 questions
            var baseCount = weights[random.Next(weights.Length)];

            // Ensure we don't exceed available questions
            var maxCount = Math.Min(availableQuestions, 8);
            var minCount = Math.Min(2, maxCount);

            return Math.Min(maxCount, Math.Max(minCount, baseCount));
        }

        #endregion

        #region Name and Description Generation

        /// <summary>
        /// Generate checklist name based on stage and index
        /// </summary>
        internal string GenerateChecklistName(AIStageGenerationResult stage, int checklistIndex, int totalChecklists)
        {
            if (totalChecklists == 1)
            {
                return $"{stage.Name} Checklist";
            }

            var stageName = stage.Name.ToLower();
            var suffixes = new[]
            {
                "Essential Tasks", "Core Activities", "Key Steps", "Primary Tasks",
                "Critical Actions", "Important Tasks", "Main Activities", "Required Steps",
                "Setup Tasks", "Execution Tasks", "Review Tasks", "Completion Tasks",
                "Pre-work", "Preparation", "Implementation", "Validation", "Follow-up"
            };

            var categoryNames = new Dictionary<string, string[]>
            {
                ["initial"] = new[] { "Assessment", "Requirements", "Planning", "Analysis" },
                ["planning"] = new[] { "Strategy", "Resource Planning", "Risk Management", "Timeline" },
                ["design"] = new[] { "Wireframes", "Prototyping", "UI/UX", "Architecture" },
                ["implementation"] = new[] { "Development", "Coding", "Integration", "Configuration" },
                ["testing"] = new[] { "Unit Tests", "Integration Tests", "User Testing", "Quality Assurance" },
                ["deployment"] = new[] { "Preparation", "Execution", "Monitoring", "Rollback" },
                ["training"] = new[] { "Material Preparation", "Delivery", "Assessment", "Support" }
            };

            // Try to use category-specific names first
            foreach (var category in categoryNames)
            {
                if (stageName.Contains(category.Key))
                {
                    var names = category.Value;
                    if (checklistIndex < names.Length)
                    {
                        return $"{stage.Name} - {names[checklistIndex]}";
                    }
                }
            }

            // Fallback to generic suffixes
            var suffix = suffixes[checklistIndex % suffixes.Length];
            return $"{stage.Name} - {suffix}";
        }

        /// <summary>
        /// Generate checklist description based on stage and index
        /// </summary>
        internal string GenerateChecklistDescription(AIStageGenerationResult stage, int checklistIndex, int totalChecklists)
        {
            if (totalChecklists == 1)
            {
                return $"Essential tasks to complete during the {stage.Name} stage";
            }

            var descriptions = new[]
            {
                $"Critical tasks for {stage.Name} execution",
                $"Key activities required in {stage.Name}",
                $"Important steps to ensure {stage.Name} success",
                $"Essential checklist for {stage.Name} completion",
                $"Primary tasks to accomplish during {stage.Name}",
                $"Core activities for effective {stage.Name}",
                $"Required actions for {stage.Name} stage",
                $"Key deliverables and tasks for {stage.Name}"
            };

            return descriptions[checklistIndex % descriptions.Length];
        }

        /// <summary>
        /// Generate questionnaire name based on stage and index
        /// </summary>
        internal string GenerateQuestionnaireName(AIStageGenerationResult stage, int questionnaireIndex, int totalQuestionnaires)
        {
            if (totalQuestionnaires == 1)
            {
                return $"{stage.Name} Questionnaire";
            }

            var suffixes = new[]
            {
                "Assessment", "Requirements Gathering", "Information Collection", "Evaluation",
                "Planning Questions", "Feedback Form", "Analysis Questions", "Review Form",
                "Pre-work Questions", "Preparation Survey", "Execution Questions", "Completion Review"
            };

            var suffix = suffixes[questionnaireIndex % suffixes.Length];
            return $"{stage.Name} - {suffix}";
        }

        /// <summary>
        /// Generate questionnaire description based on stage and index
        /// </summary>
        internal string GenerateQuestionnaireDescription(AIStageGenerationResult stage, int questionnaireIndex, int totalQuestionnaires)
        {
            if (totalQuestionnaires == 1)
            {
                return $"Key questions to gather information for the {stage.Name} stage";
            }

            var descriptions = new[]
            {
                $"Important questions to assess {stage.Name} requirements",
                $"Information gathering for effective {stage.Name} execution",
                $"Key questions to ensure {stage.Name} success",
                $"Assessment questions for {stage.Name} planning",
                $"Evaluation form for {stage.Name} stage",
                $"Required information collection for {stage.Name}",
                $"Planning questionnaire for {stage.Name} activities",
                $"Feedback and assessment for {stage.Name} completion"
            };

            return descriptions[questionnaireIndex % descriptions.Length];
        }

        #endregion

        #region Task and Question Generation

        /// <summary>
        /// Generate tasks for a stage based on its characteristics
        /// </summary>
        internal List<AITaskGenerationResult> GenerateTasksForStage(AIStageGenerationResult stage, int checklistIndex = 0, int totalChecklists = 1, Random random = null)
        {
            var stageName = stage.Name.ToLower();
            var stageDesc = stage.Description.ToLower();

            // Task templates based on stage characteristics
            var taskTemplates = new Dictionary<string, List<AITaskGenerationResult>>
            {
                ["initial"] = new List<AITaskGenerationResult>
                {
                    new AITaskGenerationResult { Id = "req-gather", Title = "Gather Requirements", Description = "Collect and document all necessary requirements", IsRequired = true, EstimatedMinutes = 120, Category = "Planning" },
                    new AITaskGenerationResult { Id = "stakeholder-id", Title = "Identify Stakeholders", Description = "List all key stakeholders and their roles", IsRequired = true, EstimatedMinutes = 60, Category = "Planning" },
                    new AITaskGenerationResult { Id = "timeline-est", Title = "Estimate Timeline", Description = "Create initial timeline estimates", IsRequired = false, EstimatedMinutes = 90, Category = "Planning" },
                    new AITaskGenerationResult { Id = "resource-check", Title = "Check Resource Availability", Description = "Verify required resources are available", IsRequired = true, EstimatedMinutes = 45, Category = "Planning" }
                },
                ["planning"] = new List<AITaskGenerationResult>
                {
                    new AITaskGenerationResult { Id = "plan-create", Title = "Create Detailed Plan", Description = "Develop comprehensive project plan", IsRequired = true, EstimatedMinutes = 180, Category = "Planning" },
                    new AITaskGenerationResult { Id = "risk-assess", Title = "Risk Assessment", Description = "Identify and assess potential risks", IsRequired = true, EstimatedMinutes = 120, Category = "Risk Management" },
                    new AITaskGenerationResult { Id = "budget-approve", Title = "Budget Approval", Description = "Get budget approval from management", IsRequired = true, EstimatedMinutes = 60, Category = "Finance" },
                    new AITaskGenerationResult { Id = "team-assign", Title = "Assign Team Members", Description = "Assign roles and responsibilities to team members", IsRequired = true, EstimatedMinutes = 90, Category = "Team Management" }
                },
                ["design"] = new List<AITaskGenerationResult>
                {
                    new AITaskGenerationResult { Id = "wireframe", Title = "Create Wireframes", Description = "Design initial wireframes and mockups", IsRequired = true, EstimatedMinutes = 240, Category = "Design" },
                    new AITaskGenerationResult { Id = "prototype", Title = "Build Prototype", Description = "Develop working prototype", IsRequired = false, EstimatedMinutes = 360, Category = "Development" },
                    new AITaskGenerationResult { Id = "design-review", Title = "Design Review", Description = "Conduct design review with stakeholders", IsRequired = true, EstimatedMinutes = 90, Category = "Review" },
                    new AITaskGenerationResult { Id = "spec-finalize", Title = "Finalize Specifications", Description = "Complete technical specifications", IsRequired = true, EstimatedMinutes = 150, Category = "Documentation" }
                },
                ["implementation"] = new List<AITaskGenerationResult>
                {
                    new AITaskGenerationResult { Id = "env-setup", Title = "Setup Environment", Description = "Configure development/production environment", IsRequired = true, EstimatedMinutes = 120, Category = "Infrastructure" },
                    new AITaskGenerationResult { Id = "code-develop", Title = "Develop Code", Description = "Write and implement code according to specifications", IsRequired = true, EstimatedMinutes = 480, Category = "Development" },
                    new AITaskGenerationResult { Id = "unit-test", Title = "Unit Testing", Description = "Perform unit testing on developed components", IsRequired = true, EstimatedMinutes = 180, Category = "Testing" },
                    new AITaskGenerationResult { Id = "code-review", Title = "Code Review", Description = "Conduct peer code review", IsRequired = true, EstimatedMinutes = 120, Category = "Quality Assurance" }
                },
                ["testing"] = new List<AITaskGenerationResult>
                {
                    new AITaskGenerationResult { Id = "test-plan", Title = "Create Test Plan", Description = "Develop comprehensive test plan", IsRequired = true, EstimatedMinutes = 120, Category = "Planning" },
                    new AITaskGenerationResult { Id = "test-cases", Title = "Write Test Cases", Description = "Create detailed test cases", IsRequired = true, EstimatedMinutes = 180, Category = "Testing" },
                    new AITaskGenerationResult { Id = "execute-tests", Title = "Execute Tests", Description = "Run all test cases and document results", IsRequired = true, EstimatedMinutes = 240, Category = "Testing" },
                    new AITaskGenerationResult { Id = "bug-fix", Title = "Fix Bugs", Description = "Address and fix identified issues", IsRequired = true, EstimatedMinutes = 300, Category = "Development" }
                },
                ["review"] = new List<AITaskGenerationResult>
                {
                    new AITaskGenerationResult { Id = "quality-check", Title = "Quality Assurance Check", Description = "Perform quality assurance review", IsRequired = true, EstimatedMinutes = 90, Category = "Quality Assurance" },
                    new AITaskGenerationResult { Id = "stakeholder-review", Title = "Stakeholder Review", Description = "Present to stakeholders for review", IsRequired = true, EstimatedMinutes = 120, Category = "Review" },
                    new AITaskGenerationResult { Id = "feedback-collect", Title = "Collect Feedback", Description = "Gather and document feedback", IsRequired = true, EstimatedMinutes = 60, Category = "Communication" },
                    new AITaskGenerationResult { Id = "approval-get", Title = "Get Final Approval", Description = "Obtain final approval to proceed", IsRequired = true, EstimatedMinutes = 45, Category = "Approval" }
                },
                ["deployment"] = new List<AITaskGenerationResult>
                {
                    new AITaskGenerationResult { Id = "deploy-prep", Title = "Prepare Deployment", Description = "Prepare all deployment materials", IsRequired = true, EstimatedMinutes = 90, Category = "Deployment" },
                    new AITaskGenerationResult { Id = "backup-create", Title = "Create Backup", Description = "Create system backup before deployment", IsRequired = true, EstimatedMinutes = 30, Category = "Infrastructure" },
                    new AITaskGenerationResult { Id = "deploy-execute", Title = "Execute Deployment", Description = "Deploy to production environment", IsRequired = true, EstimatedMinutes = 60, Category = "Deployment" },
                    new AITaskGenerationResult { Id = "smoke-test", Title = "Smoke Testing", Description = "Perform post-deployment smoke tests", IsRequired = true, EstimatedMinutes = 45, Category = "Testing" }
                },
                ["training"] = new List<AITaskGenerationResult>
                {
                    new AITaskGenerationResult { Id = "material-prep", Title = "Prepare Training Materials", Description = "Create training documentation and materials", IsRequired = true, EstimatedMinutes = 180, Category = "Training" },
                    new AITaskGenerationResult { Id = "schedule-training", Title = "Schedule Training Sessions", Description = "Organize training sessions with users", IsRequired = true, EstimatedMinutes = 60, Category = "Training" },
                    new AITaskGenerationResult { Id = "conduct-training", Title = "Conduct Training", Description = "Deliver training to end users", IsRequired = true, EstimatedMinutes = 240, Category = "Training" },
                    new AITaskGenerationResult { Id = "support-provide", Title = "Provide Support", Description = "Offer ongoing support during transition", IsRequired = true, EstimatedMinutes = 120, Category = "Support" }
                }
            };

            // Default tasks
            var defaultTasks = new List<AITaskGenerationResult>
            {
                new AITaskGenerationResult { Id = "task-plan", Title = "Plan Tasks", Description = $"Plan all tasks for {stage.Name}", IsRequired = true, EstimatedMinutes = 60, Category = "Planning" },
                new AITaskGenerationResult { Id = "resource-allocate", Title = "Allocate Resources", Description = "Ensure necessary resources are allocated", IsRequired = true, EstimatedMinutes = 45, Category = "Resource Management" },
                new AITaskGenerationResult { Id = "progress-monitor", Title = "Monitor Progress", Description = "Track and monitor stage progress", IsRequired = true, EstimatedMinutes = 30, Category = "Management" },
                new AITaskGenerationResult { Id = "deliverable-complete", Title = "Complete Deliverables", Description = "Finish all stage deliverables", IsRequired = true, EstimatedMinutes = 120, Category = "Execution" }
            };

            random = random ?? new Random();

            // Determine which template to use
            List<AITaskGenerationResult> selectedTasks = defaultTasks;

            if (stageName.Contains("initial") || stageName.Contains("assessment") || stageName.Contains("analysis"))
                selectedTasks = taskTemplates["initial"];
            else if (stageName.Contains("plan") || stageName.Contains("design") || stageDesc.Contains("plan"))
                selectedTasks = taskTemplates["planning"];
            else if (stageName.Contains("design") || stageName.Contains("prototype") || stageDesc.Contains("design"))
                selectedTasks = taskTemplates["design"];
            else if (stageName.Contains("implement") || stageName.Contains("develop") || stageName.Contains("build") || stageDesc.Contains("develop"))
                selectedTasks = taskTemplates["implementation"];
            else if (stageName.Contains("test") || stageName.Contains("qa") || stageDesc.Contains("test"))
                selectedTasks = taskTemplates["testing"];
            else if (stageName.Contains("review") || stageName.Contains("approval") || stageDesc.Contains("review"))
                selectedTasks = taskTemplates["review"];
            else if (stageName.Contains("deploy") || stageName.Contains("launch") || stageName.Contains("release"))
                selectedTasks = taskTemplates["deployment"];
            else if (stageName.Contains("training") || stageName.Contains("onboard") || stageDesc.Contains("training"))
                selectedTasks = taskTemplates["training"];

            // Determine the number of tasks for this checklist (2-6 tasks, weighted towards 3-4)
            var taskCount = DetermineTaskCount(selectedTasks.Count, checklistIndex, totalChecklists, random);

            // Shuffle and select random subset of tasks
            var shuffledTasks = selectedTasks.OrderBy(x => random.Next()).ToList();
            var finalTasks = shuffledTasks.Take(taskCount).ToList();

            // Ensure at least one required task
            if (!finalTasks.Any(t => t.IsRequired))
            {
                var requiredTasks = selectedTasks.Where(t => t.IsRequired).ToList();
                if (requiredTasks.Any())
                {
                    finalTasks[0] = requiredTasks[random.Next(requiredTasks.Count)];
                }
            }

            // Add unique IDs with stage and checklist prefix
            return finalTasks.Select((task, index) => new AITaskGenerationResult
            {
                Id = $"{stage.Name.ToLower().Replace(" ", "-")}-c{checklistIndex}-{task.Id}-{index}",
                Title = task.Title,
                Description = task.Description,
                IsRequired = task.IsRequired,
                Completed = task.Completed,
                EstimatedMinutes = task.EstimatedMinutes,
                Category = task.Category,
                Dependencies = task.Dependencies
            }).ToList();
        }

        /// <summary>
        /// Generate questions for a stage based on its characteristics
        /// </summary>
        internal List<AIQuestionGenerationResult> GenerateQuestionsForStage(AIStageGenerationResult stage, int questionnaireIndex = 0, int totalQuestionnaires = 1, Random random = null)
        {
            var stageName = stage.Name.ToLower();
            var stageDesc = stage.Description.ToLower();

            // Question templates based on stage characteristics
            var questionTemplates = new Dictionary<string, List<AIQuestionGenerationResult>>
            {
                ["initial"] = new List<AIQuestionGenerationResult>
                {
                    new AIQuestionGenerationResult { Id = "project-scope", Question = "What is the scope of this project?", Type = "text", IsRequired = true, Category = "Scope" },
                    new AIQuestionGenerationResult { Id = "success-criteria", Question = "What are the success criteria?", Type = "text", IsRequired = true, Category = "Goals" },
                    new AIQuestionGenerationResult { Id = "budget-range", Question = "What is the budget range?", Type = "select", Options = new List<string> { "< $10K", "$10K - $50K", "$50K - $100K", "> $100K" }, IsRequired = true, Category = "Budget" },
                    new AIQuestionGenerationResult { Id = "timeline-preference", Question = "What is your preferred timeline?", Type = "select", Options = new List<string> { "1-2 weeks", "1 month", "2-3 months", "6+ months" }, IsRequired = true, Category = "Timeline" }
                },
                ["planning"] = new List<AIQuestionGenerationResult>
                {
                    new AIQuestionGenerationResult { Id = "team-size", Question = "How many team members are needed?", Type = "number", IsRequired = true, Category = "Resources" },
                    new AIQuestionGenerationResult { Id = "key-milestones", Question = "What are the key milestones?", Type = "text", IsRequired = true, Category = "Planning" },
                    new AIQuestionGenerationResult { Id = "risk-tolerance", Question = "What is your risk tolerance level?", Type = "select", Options = new List<string> { "Low", "Medium", "High" }, IsRequired = true, Category = "Risk" },
                    new AIQuestionGenerationResult { Id = "communication-frequency", Question = "How often should progress be reported?", Type = "select", Options = new List<string> { "Daily", "Weekly", "Bi-weekly", "Monthly" }, IsRequired = false, Category = "Communication" }
                },
                ["design"] = new List<AIQuestionGenerationResult>
                {
                    new AIQuestionGenerationResult { Id = "design-style", Question = "What design style do you prefer?", Type = "select", Options = new List<string> { "Modern", "Classic", "Minimalist", "Bold" }, IsRequired = true, Category = "Design" },
                    new AIQuestionGenerationResult { Id = "target-audience", Question = "Who is the target audience?", Type = "text", IsRequired = true, Category = "Audience" },
                    new AIQuestionGenerationResult { Id = "brand-guidelines", Question = "Are there existing brand guidelines?", Type = "boolean", IsRequired = true, Category = "Branding" },
                    new AIQuestionGenerationResult { Id = "accessibility-requirements", Question = "Are there accessibility requirements?", Type = "multiselect", Options = new List<string> { "WCAG 2.1 AA", "Screen Reader Support", "Keyboard Navigation", "Color Contrast" }, IsRequired = false, Category = "Accessibility" }
                },
                ["implementation"] = new List<AIQuestionGenerationResult>
                {
                    new AIQuestionGenerationResult { Id = "tech-stack", Question = "What technology stack should be used?", Type = "multiselect", Options = new List<string> { "React", "Vue", "Angular", "Node.js", "Python", "Java", ".NET" }, IsRequired = true, Category = "Technology" },
                    new AIQuestionGenerationResult { Id = "performance-requirements", Question = "What are the performance requirements?", Type = "text", IsRequired = true, Category = "Performance" },
                    new AIQuestionGenerationResult { Id = "security-level", Question = "What security level is required?", Type = "select", Options = new List<string> { "Basic", "Standard", "High", "Enterprise" }, IsRequired = true, Category = "Security" },
                    new AIQuestionGenerationResult { Id = "integration-needs", Question = "What systems need integration?", Type = "text", IsRequired = false, Category = "Integration" }
                },
                ["testing"] = new List<AIQuestionGenerationResult>
                {
                    new AIQuestionGenerationResult { Id = "test-types", Question = "What types of testing are required?", Type = "multiselect", Options = new List<string> { "Unit Testing", "Integration Testing", "Performance Testing", "Security Testing", "User Acceptance Testing" }, IsRequired = true, Category = "Testing" },
                    new AIQuestionGenerationResult { Id = "test-environment", Question = "What test environment is available?", Type = "select", Options = new List<string> { "Development", "Staging", "Production-like", "Cloud-based" }, IsRequired = true, Category = "Environment" },
                    new AIQuestionGenerationResult { Id = "acceptance-criteria", Question = "What are the acceptance criteria?", Type = "text", IsRequired = true, Category = "Criteria" },
                    new AIQuestionGenerationResult { Id = "test-data", Question = "Is test data available?", Type = "boolean", IsRequired = true, Category = "Data" }
                },
                ["review"] = new List<AIQuestionGenerationResult>
                {
                    new AIQuestionGenerationResult { Id = "review-criteria", Question = "What are the review criteria?", Type = "text", IsRequired = true, Category = "Criteria" },
                    new AIQuestionGenerationResult { Id = "reviewers", Question = "Who are the key reviewers?", Type = "text", IsRequired = true, Category = "Stakeholders" },
                    new AIQuestionGenerationResult { Id = "approval-process", Question = "What is the approval process?", Type = "text", IsRequired = true, Category = "Process" },
                    new AIQuestionGenerationResult { Id = "feedback-timeline", Question = "What is the feedback timeline?", Type = "select", Options = new List<string> { "24 hours", "2-3 days", "1 week", "2 weeks" }, IsRequired = true, Category = "Timeline" }
                },
                ["deployment"] = new List<AIQuestionGenerationResult>
                {
                    new AIQuestionGenerationResult { Id = "deployment-strategy", Question = "What deployment strategy should be used?", Type = "select", Options = new List<string> { "Blue-Green", "Rolling", "Canary", "Big Bang" }, IsRequired = true, Category = "Strategy" },
                    new AIQuestionGenerationResult { Id = "rollback-plan", Question = "Is there a rollback plan?", Type = "boolean", IsRequired = true, Category = "Risk Management" },
                    new AIQuestionGenerationResult { Id = "monitoring-setup", Question = "What monitoring is needed?", Type = "multiselect", Options = new List<string> { "Performance Monitoring", "Error Tracking", "User Analytics", "Security Monitoring" }, IsRequired = true, Category = "Monitoring" },
                    new AIQuestionGenerationResult { Id = "maintenance-window", Question = "When is the maintenance window?", Type = "text", IsRequired = false, Category = "Schedule" }
                },
                ["training"] = new List<AIQuestionGenerationResult>
                {
                    new AIQuestionGenerationResult { Id = "training-format", Question = "What training format is preferred?", Type = "select", Options = new List<string> { "In-person", "Virtual", "Self-paced", "Hybrid" }, IsRequired = true, Category = "Format" },
                    new AIQuestionGenerationResult { Id = "audience-size", Question = "How many people need training?", Type = "number", IsRequired = true, Category = "Audience" },
                    new AIQuestionGenerationResult { Id = "skill-level", Question = "What is the current skill level?", Type = "select", Options = new List<string> { "Beginner", "Intermediate", "Advanced", "Mixed" }, IsRequired = true, Category = "Skills" },
                    new AIQuestionGenerationResult { Id = "training-materials", Question = "What training materials are needed?", Type = "multiselect", Options = new List<string> { "User Manual", "Video Tutorials", "Interactive Demos", "Quick Reference" }, IsRequired = true, Category = "Materials" }
                }
            };

            // Default questions
            var defaultQuestions = new List<AIQuestionGenerationResult>
            {
                new AIQuestionGenerationResult { Id = "stage-objectives", Question = $"What are the main objectives for {stage.Name}?", Type = "text", IsRequired = true, Category = "Objectives" },
                new AIQuestionGenerationResult { Id = "success-metrics", Question = "How will success be measured?", Type = "text", IsRequired = true, Category = "Metrics" },
                new AIQuestionGenerationResult { Id = "dependencies", Question = "Are there any dependencies?", Type = "text", IsRequired = false, Category = "Dependencies" },
                new AIQuestionGenerationResult { Id = "special-requirements", Question = "Are there any special requirements?", Type = "text", IsRequired = false, Category = "Requirements" }
            };

            random = random ?? new Random();

            // Determine which template to use
            List<AIQuestionGenerationResult> selectedQuestions = defaultQuestions;

            if (stageName.Contains("initial") || stageName.Contains("assessment") || stageName.Contains("analysis"))
                selectedQuestions = questionTemplates["initial"];
            else if (stageName.Contains("plan") || stageDesc.Contains("plan"))
                selectedQuestions = questionTemplates["planning"];
            else if (stageName.Contains("design") || stageDesc.Contains("design"))
                selectedQuestions = questionTemplates["design"];
            else if (stageName.Contains("implement") || stageName.Contains("develop") || stageName.Contains("build"))
                selectedQuestions = questionTemplates["implementation"];
            else if (stageName.Contains("test") || stageName.Contains("qa"))
                selectedQuestions = questionTemplates["testing"];
            else if (stageName.Contains("review") || stageName.Contains("approval"))
                selectedQuestions = questionTemplates["review"];
            else if (stageName.Contains("deploy") || stageName.Contains("launch"))
                selectedQuestions = questionTemplates["deployment"];
            else if (stageName.Contains("training") || stageName.Contains("onboard"))
                selectedQuestions = questionTemplates["training"];

            // Determine the number of questions for this questionnaire (2-8 questions, weighted towards 4-6)
            var questionCount = DetermineQuestionCount(selectedQuestions.Count, questionnaireIndex, totalQuestionnaires, random);

            // Shuffle and select random subset of questions
            var shuffledQuestions = selectedQuestions.OrderBy(x => random.Next()).ToList();
            var finalQuestions = shuffledQuestions.Take(questionCount).ToList();

            // Ensure at least one required question
            if (!finalQuestions.Any(q => q.IsRequired))
            {
                var requiredQuestions = selectedQuestions.Where(q => q.IsRequired).ToList();
                if (requiredQuestions.Any())
                {
                    finalQuestions[0] = requiredQuestions[random.Next(requiredQuestions.Count)];
                }
            }

            // Add unique IDs with stage and questionnaire prefix
            return finalQuestions.Select((question, index) => new AIQuestionGenerationResult
            {
                Id = $"{stage.Name.ToLower().Replace(" ", "-")}-q{questionnaireIndex}-{question.Id}-{index}",
                Question = question.Question,
                Type = question.Type,
                Options = question.Options,
                IsRequired = question.IsRequired,
                Category = question.Category,
                HelpText = question.HelpText,
                ValidationRule = question.ValidationRule,
                DefaultValue = question.DefaultValue
            }).ToList();
        }

        #endregion

        #region Unique Name Helpers

        /// <summary>
        /// Ensure unique checklist name within the same team scope
        /// </summary>
        internal async Task<string> EnsureUniqueChecklistNameAsync(string baseName, string team)
        {
            var originalName = baseName;
            var counter = 1;
            var currentName = baseName;

            while (true)
            {
                try
                {
                    // Check if the name exists using the ChecklistRepository
                    var exists = await _checklistRepository.IsNameExistsAsync(currentName, team);
                    if (!exists)
                    {
                        return currentName;
                    }

                    // Generate a new name with counter
                    counter++;
                    currentName = $"{originalName} ({counter})";
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error checking checklist name uniqueness for {Name}, using fallback", currentName);
                    // If there's an error checking, use timestamp as fallback
                    return $"{originalName} {DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}";
                }
            }
        }

        /// <summary>
        /// Ensure unique questionnaire name
        /// </summary>
        internal async Task<string> EnsureUniqueQuestionnaireNameAsync(string baseName)
        {
            var originalName = baseName;
            var counter = 1;
            var currentName = baseName;

            while (true)
            {
                try
                {
                    // Check if the name exists using the QuestionnaireRepository
                    var exists = await _questionnaireRepository.IsNameExistsAsync(currentName);
                    if (!exists)
                    {
                        return currentName;
                    }

                    // Generate a new name with counter
                    counter++;
                    currentName = $"{originalName} ({counter})";
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error checking questionnaire name uniqueness for {Name}, using fallback", currentName);
                    // If there's an error checking, use timestamp as fallback
                    return $"{originalName} {DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}";
                }
            }
        }

        #endregion

        #region Fallback Generation

        /// <summary>
        /// Generate a fallback checklist when AI generation fails
        /// </summary>
        internal AIChecklistGenerationResult GenerateFallbackChecklist(AIStageGenerationResult stage)
        {
            return new AIChecklistGenerationResult
            {
                Success = true,
                Message = $"Empty checklist for {stage.Name} - no AI tasks generated",
                GeneratedChecklist = new ChecklistInputDto
                {
                    Name = $"{stage.Name} Checklist",
                    Description = $"Tasks for {stage.Name}",
                    Team = stage.AssignedGroup,
                    IsActive = true
                },
                Tasks = new List<AITaskGenerationResult>(),
                ConfidenceScore = 0.0
            };
        }

        /// <summary>
        /// Generate a fallback questionnaire when AI generation fails
        /// </summary>
        internal AIQuestionnaireGenerationResult GenerateFallbackQuestionnaire(AIStageGenerationResult stage)
        {
            return new AIQuestionnaireGenerationResult
            {
                Success = true,
                Message = $"Empty questionnaire for {stage.Name} - no AI questions generated",
                GeneratedQuestionnaire = new QuestionnaireInputDto
                {
                    Name = $"{stage.Name} Questionnaire",
                    Description = $"Information gathering for {stage.Name}",
                    IsActive = true
                },
                Questions = new List<AIQuestionGenerationResult>(),
                ConfidenceScore = 0.0
            };
        }

        /// <summary>
        /// Generate checklists for stages using local templates (synchronous fallback)
        /// </summary>
        internal List<AIChecklistGenerationResult> GenerateChecklistsForStages(List<AIStageGenerationResult> stages)
        {
            var checklists = new List<AIChecklistGenerationResult>();
            var random = new Random();

            foreach (var stage in stages)
            {
                // Determine the number of checklists for this stage based on complexity and characteristics
                var checklistCount = DetermineChecklistCount(stage, random);

                for (int i = 0; i < checklistCount; i++)
                {
                    var checklistName = GenerateChecklistName(stage, i, checklistCount);
                    var checklistDescription = GenerateChecklistDescription(stage, i, checklistCount);

                    var checklist = new AIChecklistGenerationResult
                    {
                        Success = true,
                        Message = $"Checklist generated for {stage.Name}",
                        GeneratedChecklist = new ChecklistInputDto
                        {
                            Name = checklistName,
                            Description = checklistDescription,
                            Team = stage.AssignedGroup,
                            IsActive = true
                        },
                        Tasks = GenerateTasksForStage(stage, i, checklistCount, random),
                        ConfidenceScore = 0.85
                    };

                    checklists.Add(checklist);
                }
            }

            return checklists;
        }

        /// <summary>
        /// Generate questionnaires for stages using local templates (synchronous fallback)
        /// </summary>
        internal List<AIQuestionnaireGenerationResult> GenerateQuestionnairesForStages(List<AIStageGenerationResult> stages)
        {
            var questionnaires = new List<AIQuestionnaireGenerationResult>();
            var random = new Random();

            foreach (var stage in stages)
            {
                // Determine the number of questionnaires for this stage based on complexity and characteristics
                var questionnaireCount = DetermineQuestionnaireCount(stage, random);

                for (int i = 0; i < questionnaireCount; i++)
                {
                    var questionnaireName = GenerateQuestionnaireName(stage, i, questionnaireCount);
                    var questionnaireDescription = GenerateQuestionnaireDescription(stage, i, questionnaireCount);

                    var questionnaire = new AIQuestionnaireGenerationResult
                    {
                        Success = true,
                        Message = $"Questionnaire generated for {stage.Name}",
                        GeneratedQuestionnaire = new QuestionnaireInputDto
                        {
                            Name = questionnaireName,
                            Description = questionnaireDescription,
                            IsActive = true
                        },
                        Questions = GenerateQuestionsForStage(stage, i, questionnaireCount, random),
                        ConfidenceScore = 0.85
                    };

                    questionnaires.Add(questionnaire);
                }
            }

            return questionnaires;
        }

        #endregion
    }
}
