using FlowFlex.Application.Contracts.Dtos.OW.Workflow;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.IServices.AI;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Services.AI
{
    /// <summary>
    /// Facade that implements IAIService by delegating to fine-grained service interfaces.
    /// Ensures backward compatibility for any code still injecting IAIService.
    /// </summary>
    public class AIServiceFacade : IAIService, IScopedService
    {
        private readonly IAIWorkflowGenerationService _workflowService;
        private readonly IAIQuestionnaireGenerationService _questionnaireService;
        private readonly IAIChecklistGenerationService _checklistService;
        private readonly IAIChatService _chatService;
        private readonly IAIActionService _actionService;
        private readonly IAISummaryService _summaryService;
        private readonly IAIRequirementsParsingService _requirementsService;

        public AIServiceFacade(
            IAIWorkflowGenerationService workflowService,
            IAIQuestionnaireGenerationService questionnaireService,
            IAIChecklistGenerationService checklistService,
            IAIChatService chatService,
            IAIActionService actionService,
            IAISummaryService summaryService,
            IAIRequirementsParsingService requirementsService)
        {
            _workflowService = workflowService;
            _questionnaireService = questionnaireService;
            _checklistService = checklistService;
            _chatService = chatService;
            _actionService = actionService;
            _summaryService = summaryService;
            _requirementsService = requirementsService;
        }

        // Workflow generation
        public Task<AIWorkflowGenerationResult> GenerateWorkflowAsync(AIWorkflowGenerationInput input)
            => _workflowService.GenerateWorkflowAsync(input);

        public IAsyncEnumerable<AIWorkflowStreamResult> StreamGenerateWorkflowAsync(AIWorkflowGenerationInput input)
            => _workflowService.StreamGenerateWorkflowAsync(input);

        public Task<AIWorkflowEnhancementResult> EnhanceWorkflowAsync(long workflowId, string enhancement)
            => _workflowService.EnhanceWorkflowAsync(workflowId, enhancement);

        public Task<AIWorkflowGenerationResult> EnhanceWorkflowAsync(AIWorkflowModificationInput input)
            => _workflowService.EnhanceWorkflowAsync(input);

        public Task<AIValidationResult> ValidateWorkflowAsync(WorkflowInputDto workflow)
            => _workflowService.ValidateWorkflowAsync(workflow);

        public Task<bool> CreateStageComponentsAsync(long workflowId, List<AIStageGenerationResult> stages,
            List<AIChecklistGenerationResult> checklists, List<AIQuestionnaireGenerationResult> questionnaires)
            => _workflowService.CreateStageComponentsAsync(workflowId, stages, checklists, questionnaires);

        // Questionnaire generation
        public Task<AIQuestionnaireGenerationResult> GenerateQuestionnaireAsync(AIQuestionnaireGenerationInput input)
            => _questionnaireService.GenerateQuestionnaireAsync(input);

        public IAsyncEnumerable<AIQuestionnaireStreamResult> StreamGenerateQuestionnaireAsync(AIQuestionnaireGenerationInput input)
            => _questionnaireService.StreamGenerateQuestionnaireAsync(input);

        // Checklist generation
        public Task<AIChecklistGenerationResult> GenerateChecklistAsync(AIChecklistGenerationInput input)
            => _checklistService.GenerateChecklistAsync(input);

        public IAsyncEnumerable<AIChecklistStreamResult> StreamGenerateChecklistAsync(AIChecklistGenerationInput input)
            => _checklistService.StreamGenerateChecklistAsync(input);

        // Chat
        public Task<AIChatResponse> SendChatMessageAsync(AIChatInput input)
            => _chatService.SendChatMessageAsync(input);

        public IAsyncEnumerable<AIChatStreamResult> StreamChatAsync(AIChatInput input)
            => _chatService.StreamChatAsync(input);

        // Summary
        public Task<AIStageSummaryResult> GenerateStageSummaryAsync(AIStageSummaryInput input)
            => _summaryService.GenerateStageSummaryAsync(input);

        // Action
        public Task<AIActionAnalysisResult> AnalyzeActionAsync(AIActionAnalysisInput input)
            => _actionService.AnalyzeActionAsync(input);

        public Task<AIActionCreationResult> CreateActionAsync(AIActionCreationInput input)
            => _actionService.CreateActionAsync(input);

        public IAsyncEnumerable<AIActionStreamResult> StreamAnalyzeActionAsync(AIActionAnalysisInput input)
            => _actionService.StreamAnalyzeActionAsync(input);

        public IAsyncEnumerable<AIActionStreamResult> StreamCreateActionAsync(AIActionCreationInput input)
            => _actionService.StreamCreateActionAsync(input);

        public IAsyncEnumerable<AIActionStreamResult> StreamGenerateHttpConfigAsync(AIHttpConfigGenerationInput input)
            => _actionService.StreamGenerateHttpConfigAsync(input);

        // Requirements parsing
        public Task<AIRequirementsParsingResult> ParseRequirementsAsync(string naturalLanguage)
            => _requirementsService.ParseRequirementsAsync(naturalLanguage);

        public Task<AIRequirementsParsingResult> ParseRequirementsAsync(string naturalLanguage,
            string? modelProvider, string? modelName, string? modelId)
            => _requirementsService.ParseRequirementsAsync(naturalLanguage, modelProvider, modelName, modelId);
    }
}
