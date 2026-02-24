using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.IServices.AI;
using Microsoft.Extensions.DependencyInjection;

namespace FlowFlex.Application.Services.AI
{
    /// <summary>
    /// Extension methods for registering fine-grained AI services in DI container.
    /// Uses typeof() references to avoid namespace conflicts with System.Action.
    /// </summary>
    public static class AIServiceRegistration
    {
        public static IServiceCollection AddFineGrainedAIServices(this IServiceCollection services)
        {
            // Get implementation types by reflection from this assembly
            var assembly = typeof(AIServiceRegistration).Assembly;

            var registrations = new (Type serviceType, string implTypeName)[]
            {
                (typeof(IAIProviderAdapter), "FlowFlex.Application.Services.AI.Providers.AIProviderAdapter"),
                (typeof(IAIPromptBuilder), "FlowFlex.Application.Services.AI.Prompts.AIPromptBuilder"),
                (typeof(IAIResponseParser), "FlowFlex.Application.Services.AI.Parsing.AIResponseParser"),
                (typeof(IStageComponentService), "FlowFlex.Application.Services.AI.StageComponent.StageComponentService"),
                (typeof(IAIWorkflowGenerationService), "FlowFlex.Application.Services.AI.Workflow.AIWorkflowGenerationService"),
                (typeof(IAIQuestionnaireGenerationService), "FlowFlex.Application.Services.AI.Questionnaire.AIQuestionnaireGenerationService"),
                (typeof(IAIChecklistGenerationService), "FlowFlex.Application.Services.AI.Checklist.AIChecklistGenerationService"),
                (typeof(IAIChatService), "FlowFlex.Application.Services.AI.Chat.AIChatService"),
                (typeof(IAIActionService), "FlowFlex.Application.Services.AI.Action.AIActionService"),
                (typeof(IAISummaryService), "FlowFlex.Application.Services.AI.Summary.AISummaryService"),
                (typeof(IAIRequirementsParsingService), "FlowFlex.Application.Services.AI.Requirements.AIRequirementsParsingService"),
                (typeof(IAIService), "FlowFlex.Application.Services.AI.AIServiceFacade"),
            };

            foreach (var (serviceType, implTypeName) in registrations)
            {
                var implType = assembly.GetType(implTypeName);
                if (implType != null)
                {
                    services.AddScoped(serviceType, implType);
                }
                else
                {
                    throw new InvalidOperationException($"AI service implementation type not found: {implTypeName}");
                }
            }

            return services;
        }
    }
}
