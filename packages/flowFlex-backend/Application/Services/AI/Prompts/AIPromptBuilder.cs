using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.IServices.AI;
using FlowFlex.Application.Contracts.Options;
using FlowFlex.Domain.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace FlowFlex.Application.Services.AI.Prompts
{
    /// <summary>
    /// Unified prompt builder implementation
    /// Centralizes all prompt construction logic extracted from AIService partial classes
    /// </summary>
    public class AIPromptBuilder : IAIPromptBuilder, IScopedService
    {
        private readonly ILogger<AIPromptBuilder> _logger;
        private readonly AIOptions _aiOptions;

        public AIPromptBuilder(
            ILogger<AIPromptBuilder> logger,
            IOptions<AIOptions> aiOptions)
        {
            _logger = logger;
            _aiOptions = aiOptions.Value;
        }

        #region Workflow Prompts

        /// <inheritdoc />
        public string BuildWorkflowGenerationPrompt(AIWorkflowGenerationInput input)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine($"{_aiOptions.Prompts.WorkflowSystem}");
            promptBuilder.AppendLine("Output the result according to the language input by the user.");
            promptBuilder.AppendLine();

            // Check if this is a conversation-based workflow generation
            if (input.ConversationHistory != null && input.ConversationHistory.Any())
            {
                promptBuilder.AppendLine("=== Generate Workflow Based on Detailed Conversation ===");
                promptBuilder.AppendLine("Below is the complete conversation history with the user. Please generate an accurate workflow based on these detailed information:");
                promptBuilder.AppendLine();

                // Add conversation context
                if (input.ConversationMetadata != null)
                {
                    promptBuilder.AppendLine($"Session Information:");
                    promptBuilder.AppendLine($"- Session ID: {input.SessionId}");
                    promptBuilder.AppendLine($"- Total Messages: {input.ConversationMetadata.TotalMessages}");
                    promptBuilder.AppendLine($"- Conversation Mode: {input.ConversationMetadata.ConversationMode}");
                    promptBuilder.AppendLine();
                }

                // Add full conversation history
                promptBuilder.AppendLine("Complete Conversation Content:");
                foreach (var message in input.ConversationHistory)
                {
                    var role = message.Role == "user" ? "👤 User" : "🤖 AI Assistant";
                    promptBuilder.AppendLine($"{role}:");
                    promptBuilder.AppendLine(message.Content);
                    promptBuilder.AppendLine();
                }

                promptBuilder.AppendLine("Please pay special attention to:");
                promptBuilder.AppendLine("1. Extract all key requirements and details from the conversation");
                promptBuilder.AppendLine("2. Use the specific suggestions and detailed information provided by the AI assistant in the conversation");
                promptBuilder.AppendLine("3. Ensure the workflow reflects the user's specific needs and preferences");
                promptBuilder.AppendLine("4. If the AI assistant provided detailed itineraries, plans, or steps, convert them into workflow stages");
                promptBuilder.AppendLine();
            }
            else
            {
                // Fallback to traditional prompt building
                promptBuilder.AppendLine("Please generate a complete workflow definition based on the following requirements:");
                promptBuilder.AppendLine($"Description: {input.Description}");
            }

            if (!string.IsNullOrEmpty(input.Context))
                promptBuilder.AppendLine($"Context: {input.Context}");

            if (!string.IsNullOrEmpty(input.Industry))
                promptBuilder.AppendLine($"Industry: {input.Industry}");

            if (!string.IsNullOrEmpty(input.ProcessType))
                promptBuilder.AppendLine($"Process Type: {input.ProcessType}");

            if (input.Requirements.Any())
            {
                promptBuilder.AppendLine("Specific Requirements:");
                foreach (var req in input.Requirements)
                {
                    promptBuilder.AppendLine($"- {req}");
                }
            }

            // Add AI model information if available
            if (!string.IsNullOrEmpty(input.ModelProvider))
            {
                promptBuilder.AppendLine();
                promptBuilder.AppendLine($"AI Model Used: {input.ModelProvider} {input.ModelName}");
            }

            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Generate complete workflow, must include checklist and questionnaire for each stage. Each stage should have 3-10 tasks and 3-10 questions:");
            promptBuilder.AppendLine(@"{
  ""name"": ""Workflow Name"",
  ""description"": ""Workflow Description"",
  ""stages"": [
    {
      ""name"": ""Stage Name"",
      ""description"": ""Stage Description"",
      ""assignedGroup"": ""Assigned Team"",
      ""estimatedDuration"": 1,
      ""checklist"": {
        ""name"": ""Stage Name Checklist"",
        ""tasks"": [
          { ""title"": ""Task Name"", ""description"": ""Description"", ""isRequired"": true, ""estimatedMinutes"": 60, ""category"": ""Execution"" }
        ]
      },
      ""questionnaire"": {
        ""name"": ""Stage Name Questionnaire"",
        ""questions"": [
          { ""question"": ""Key Question?"", ""type"": ""short_answer"", ""isRequired"": true, ""category"": ""Requirements"" },
          { ""question"": ""Priority?"", ""type"": ""multiple_choice"", ""isRequired"": true, ""options"": [{""id"": ""1"", ""value"": ""high"", ""label"": ""High""}, {""id"": ""2"", ""value"": ""medium"", ""label"": ""Medium""}, {""id"": ""3"", ""value"": ""low"", ""label"": ""Low""}] }
        ]
      }
    }
  ]
}
IMPORTANT: 
1. Each stage must contain both checklist and questionnaire fields!
2. Checklist name MUST include the stage name (e.g., ""Requirements Analysis Checklist"")
3. Questionnaire name MUST include the stage name (e.g., ""Requirements Analysis Questionnaire"")
4. For multiple_choice, checkboxes, dropdown types, options MUST be array of objects with id, value, label fields!");

            return promptBuilder.ToString();
        }

        /// <inheritdoc />
        public Task<string> BuildWorkflowModificationPromptAsync(AIWorkflowModificationInput input, WorkflowInfo existingWorkflowInfo)
        {
            var modificationContext = input.PreserveExisting ?
                "Please modify based on maintaining the core structure and existing stages of the current workflow. Only add, modify, or delete stages according to specific requirements." :
                "If needed, you can completely redesign the workflow.";

            var prompt = $@"CRITICAL: This is a MODIFICATION task, NOT a creation task.
Output the result according to the language input by the user.

MANDATORY RULES - DO NOT VIOLATE:
1. Workflow name MUST remain EXACTLY: ""{existingWorkflowInfo.Name}""
2. DO NOT change the workflow name under any circumstances
3. DO NOT create a new workflow
4. ONLY modify existing stages or add new stages

EXISTING WORKFLOW TO MODIFY:
Name: {existingWorkflowInfo.Name}
Description: {existingWorkflowInfo.Description}
Current Stages:
{string.Join("\n", existingWorkflowInfo.Stages.Select((stage, index) =>
    $"{index + 1}. {stage.Name} - {stage.Description} (Duration: {stage.EstimatedDuration} days, Team: {stage.AssignedGroup})"))}

USER MODIFICATION REQUEST: {input.Description}

REQUIRED OUTPUT FORMAT - Use EXACT name ""{existingWorkflowInfo.Name}"":
{{
    ""name"": ""{existingWorkflowInfo.Name}"",
    ""description"": ""{existingWorkflowInfo.Description} - Modified based on user requirements"",
    ""isActive"": true,
    ""stages"": [
{string.Join(",\n", existingWorkflowInfo.Stages.Select((stage, index) =>
    $@"        {{
            ""name"": ""{stage.Name}"",
            ""description"": ""{stage.Description}"",
            ""order"": {index + 1},
            ""assignedGroup"": ""{stage.AssignedGroup}"",
            ""estimatedDuration"": {stage.EstimatedDuration}
        }}"))}
    ]
}}

VERIFICATION CHECKLIST:
✓ Workflow name is EXACTLY: ""{existingWorkflowInfo.Name}""
✓ Based on existing stages
✓ Added modifications as requested
✓ JSON format only

RETURN ONLY THE JSON - NO EXPLANATORY TEXT.";

            return Task.FromResult(prompt);
        }

        #endregion

        #region Questionnaire Prompts

        /// <inheritdoc />
        public string BuildQuestionnaireGenerationPrompt(AIQuestionnaireGenerationInput input)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine($"{_aiOptions.Prompts.QuestionnaireSystem}");
            promptBuilder.AppendLine("Output the result according to the language input by the user.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Please generate a complete questionnaire based on the following requirements:");
            promptBuilder.AppendLine($"Purpose: {input.Purpose}");
            promptBuilder.AppendLine($"Target Audience: {input.TargetAudience}");
            promptBuilder.AppendLine($"Complexity: {input.Complexity}");
            promptBuilder.AppendLine($"Estimated Number of Questions: {input.EstimatedQuestions}");

            if (input.Topics.Any())
            {
                promptBuilder.AppendLine("Topics Covered:");
                foreach (var topic in input.Topics)
                {
                    promptBuilder.AppendLine($"- {topic}");
                }
            }

            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Please generate a JSON format response containing the following information:");
            promptBuilder.AppendLine("1. Basic questionnaire information (name, description)");
            promptBuilder.AppendLine("2. Question sections (sections)");
            promptBuilder.AppendLine("3. Specific question list, including question types, options, etc.");

            return promptBuilder.ToString();
        }

        /// <inheritdoc />
        public string BuildQuestionnaireGenerationPrompt(AIStageGenerationResult stage, string originalDescription)
        {
            return $@"
Based on the following stage information and original project description, generate relevant questions to gather information needed for this stage.

Original Project Description: {originalDescription}

Stage Information:
- Name: {stage.Name}
- Description: {stage.Description}
- Assigned Group: {stage.AssignedGroup}
- Estimated Duration: {stage.EstimatedDuration} days

Please provide a JSON response with the following structure:
{{
    ""name"": ""Specific questionnaire name"",
    ""description"": ""Description of information this questionnaire gathers"",
    ""questions"": [
        {{
            ""question"": ""Question text"",
            ""type"": ""short_answer"" | ""paragraph"" | ""multiple_choice"" | ""checkboxes"" | ""dropdown"" | ""date"" | ""time"",
            ""isRequired"": true/false,
            ""category"": ""Question category"",
            ""options"": [{{""id"": ""1"", ""value"": ""option1"", ""label"": ""Option 1""}}, {{""id"": ""2"", ""value"": ""option2"", ""label"": ""Option 2""}}] // only for multiple_choice/checkboxes/dropdown types, MUST be array of objects with id, value, label
        }}
    ]
}}

Generate 3-8 relevant questions that help gather the information needed to successfully complete this stage.
Focus on questions that are specific to the project context and this particular stage's requirements.
";
        }

        /// <inheritdoc />
        public string BuildBatchQuestionnaireGenerationPrompt(List<AIStageGenerationResult> stages, string originalDescription)
        {
            var stagesInfo = string.Join("\n\n", stages.Select((stage, index) => $@"
Stage {index + 1}:
- Name: {stage.Name}
- Description: {stage.Description}
- Assigned Group: {stage.AssignedGroup}
- Estimated Duration: {stage.EstimatedDuration} days"));

            return $@"
You are a business analyst expert. Generate relevant questionnaires for the project stages described below.

Project: {originalDescription}

Stages:
{stagesInfo}

IMPORTANT: Respond ONLY with valid JSON in the exact format below, no additional text:

{{
    ""questionnaires"": [
        {{
            ""stageIndex"": 0,
            ""name"": ""Questionnaire for {stages.FirstOrDefault()?.Name ?? "Stage 1"}"",
            ""description"": ""Information gathering for {stages.FirstOrDefault()?.Name ?? "Stage 1"}"",
            ""questions"": [
                {{
                    ""question"": ""Sample question about the stage requirements?"",
                    ""type"": ""text"",
                    ""isRequired"": true,
                    ""category"": ""Requirements""
                }}
            ]
        }}
    ]
}}

Create {stages.Count} questionnaire entries (stageIndex 0 to {stages.Count - 1}), each with 3-6 specific questions.
Use question types: short_answer, paragraph, multiple_choice, checkboxes, dropdown, file_upload, linear_scale, rating, date, time.
Include ""options"" array only for select/multiselect types.
Make questions relevant to the project context and stage objectives.";
        }

        #endregion


        #region Checklist Prompts

        /// <inheritdoc />
        public string BuildChecklistGenerationPrompt(AIChecklistGenerationInput input)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine($"{_aiOptions.Prompts.ChecklistSystem}");
            promptBuilder.AppendLine("Output the result according to the language input by the user.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Please generate a complete checklist based on the following requirements:");
            promptBuilder.AppendLine($"Process Name: {input.ProcessName}");
            promptBuilder.AppendLine($"Description: {input.Description}");
            promptBuilder.AppendLine($"Responsible Team: {input.Team}");

            if (input.RequiredSteps.Any())
            {
                promptBuilder.AppendLine("Required Steps:");
                foreach (var step in input.RequiredSteps)
                {
                    promptBuilder.AppendLine($"- {step}");
                }
            }

            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Please generate a JSON format response containing the following information:");
            promptBuilder.AppendLine("1. Basic checklist information (name, description, team)");
            promptBuilder.AppendLine("2. Task list, including task name, description, estimated time, whether required");
            if (input.IncludeDependencies)
                promptBuilder.AppendLine("3. Task dependencies");

            return promptBuilder.ToString();
        }

        /// <inheritdoc />
        public string BuildChecklistGenerationPrompt(AIStageGenerationResult stage, string originalDescription)
        {
            return $@"
Based on the following stage information and original project description, generate a comprehensive checklist with specific tasks.

Original Project Description: {originalDescription}

Stage Information:
- Name: {stage.Name}
- Description: {stage.Description}
- Assigned Group: {stage.AssignedGroup}
- Estimated Duration: {stage.EstimatedDuration} days

Please provide a JSON response with the following structure:
{{
    ""name"": ""Specific checklist name"",
    ""description"": ""Detailed description of what this checklist covers"",
    ""tasks"": [
        {{
            ""title"": ""Task title"",
            ""description"": ""Detailed task description"",
            ""isRequired"": true/false,
            ""estimatedMinutes"": number,
            ""category"": ""Task category""
        }}
    ]
}}

Generate 3-6 specific, actionable tasks that are directly relevant to this stage and the overall project description.
Focus on concrete deliverables and ensure tasks are specific to the project context.
";
        }

        /// <inheritdoc />
        public string BuildBatchChecklistGenerationPrompt(List<AIStageGenerationResult> stages, string originalDescription)
        {
            var stagesInfo = string.Join("\n\n", stages.Select((stage, index) => $@"
Stage {index + 1}:
- Name: {stage.Name}
- Description: {stage.Description}
- Assigned Group: {stage.AssignedGroup}
- Estimated Duration: {stage.EstimatedDuration} days"));

            return $@"
You are a project management expert. Generate comprehensive checklists for the project stages described below.

Project: {originalDescription}

Stages:
{stagesInfo}

IMPORTANT: Respond ONLY with valid JSON in the exact format below, no additional text:

{{
    ""checklists"": [
        {{
            ""stageIndex"": 0,
            ""name"": ""Checklist for {stages.FirstOrDefault()?.Name ?? "Stage 1"}"",
            ""description"": ""Tasks for {stages.FirstOrDefault()?.Name ?? "Stage 1"}"",
            ""tasks"": [
                {{
                    ""title"": ""Sample task title"",
                    ""description"": ""Detailed task description"",
                    ""isRequired"": true,
                    ""estimatedMinutes"": 120,
                    ""category"": ""Planning""
                }}
            ]
        }}
    ]
}}

Create {stages.Count} checklist entries (stageIndex 0 to {stages.Count - 1}), each with 3-5 specific tasks.
Make tasks relevant to the project context and stage purpose.
Use realistic time estimates (30-480 minutes per task).";
        }

        #endregion

        #region Chat Prompts

        /// <inheritdoc />
        public string BuildChatPrompt(AIChatInput input)
        {
            var prompt = new StringBuilder();

            if (input.Mode == "workflow_planning")
            {
                prompt.AppendLine("You are an AI Workflow Assistant helping users design business workflows.");
                prompt.AppendLine("Your goal is to understand their requirements through conversation and help them create effective workflows.");
                prompt.AppendLine();
            }

            prompt.AppendLine("Conversation History:");
            foreach (var message in input.Messages.TakeLast(10)) // Limit context to last 10 messages
            {
                prompt.AppendLine($"{message.Role}: {message.Content}");
            }

            if (!string.IsNullOrEmpty(input.Context))
            {
                prompt.AppendLine();
                prompt.AppendLine($"Context: {input.Context}");
            }

            prompt.AppendLine();
            prompt.AppendLine("Please provide a helpful, conversational response that continues the discussion and gathers more information about their workflow needs.");

            return prompt.ToString();
        }

        /// <inheritdoc />
        public string GetChatSystemPrompt(string mode, string input)
        {
            return mode switch
            {
                "workflow_planning" => @"You are an expert AI Workflow Assistant specialized in business process design. Output the result according to the language input by the user. Your role is to:

1. **Understand User Needs**: Engage in natural conversation to deeply understand the user's workflow requirements
2. **Ask Smart Questions**: Ask relevant, specific questions to gather essential information about:
   - Process type and business context
   - Key stakeholders and their roles
   - Timeline and urgency requirements
   - Specific requirements, documents, or approvals needed
   - Compliance or regulatory considerations
3. **Provide Expert Guidance**: Offer professional insights and best practices for workflow design
4. **Be Conversational**: Maintain a friendly, helpful tone while being thorough and professional
5. **Progressive Discovery**: Gradually build understanding through multiple exchanges rather than overwhelming with too many questions at once
6. **Completion Detection**: When you have sufficient information (typically after 3-4 meaningful exchanges), indicate readiness to proceed with workflow creation

Guidelines:
- Ask 1-2 focused questions per response
- Acknowledge and build upon previous answers
- Provide brief explanations of why certain information is important
- Use business terminology appropriately
- Be encouraging and supportive throughout the conversation
- Respond in the same language as the user's input

Remember: Your goal is to collect enough detailed information to create a comprehensive, practical workflow that meets the user's specific needs.",

                "generate_code" => GetGenerateCodePrompt(input),

                _ => @"You are a helpful, knowledgeable AI assistant. Output the result according to the language input by the user. Provide clear, accurate, and helpful responses to user questions. Be conversational, friendly, and thorough in your explanations."
            };
        }

        /// <inheritdoc />
        public string GetGenerateCodePrompt(string instruction, string codeLanguage = "python")
        {
            var promptBuilder = new StringBuilder();

            promptBuilder.AppendLine("You are an expert programmer. Generate code based on the following instructions:");
            promptBuilder.AppendLine();

            promptBuilder.AppendLine("Instructions: {{INSTRUCTION}}");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Write the code in {{CODE_LANGUAGE}}.");
            promptBuilder.AppendLine();

            promptBuilder.AppendLine("Please ensure that you meet the following requirements:");
            promptBuilder.AppendLine("1. Define a function named 'main'.");
            promptBuilder.AppendLine("2. The 'main' function must return a dictionary (dict).");
            promptBuilder.AppendLine("3. You may modify the arguments of the 'main' function, but include appropriate type hints.");
            promptBuilder.AppendLine("4. The returned dictionary should contain at least one key-value pair.");
            promptBuilder.AppendLine();

            promptBuilder.AppendLine("5. You may ONLY use the following libraries in your code:");
            var allowedLibraries = new[]
            {
                "json", "datetime", "math", "random", "re", "string", "sys", "time", "traceback",
                "uuid", "os", "base64", "hashlib", "hmac", "binascii", "collections", "functools",
                "operator", "itertools", "urllib.request", "urllib.parse"
            };

            foreach (var library in allowedLibraries)
            {
                promptBuilder.AppendLine($"- {library}");
            }
            promptBuilder.AppendLine();

            promptBuilder.AppendLine("Example:");
            promptBuilder.AppendLine("def main(arg1: str, arg2: int) -> dict:");
            promptBuilder.AppendLine("    return {");
            promptBuilder.AppendLine("        \"result\": arg1 * arg2,");
            promptBuilder.AppendLine("    }");
            promptBuilder.AppendLine();

            promptBuilder.AppendLine("IMPORTANT:");
            promptBuilder.AppendLine("- Provide ONLY the code without any additional explanations, comments, or markdown formatting.");
            promptBuilder.AppendLine("- DO NOT use markdown code blocks (``` or ``` python). Return the raw code directly.");
            promptBuilder.AppendLine("- The code should start immediately after this instruction, without any preceding newlines or spaces.");
            promptBuilder.AppendLine("- The code should be complete, functional, and follow best practices for {{CODE_LANGUAGE}}.");
            promptBuilder.AppendLine("- Always use the format return {'result': ...} for the output.");
            promptBuilder.AppendLine();

            promptBuilder.AppendLine("Generated Code:");

            return ProcessTemplateVariables(promptBuilder.ToString(), instruction, codeLanguage);
        }

        /// <inheritdoc />
        public string ProcessTemplateVariables(string template, string instruction, string codeLanguage)
        {
            if (string.IsNullOrEmpty(template))
                return template;

            var result = template;

            var variableMappings = new Dictionary<string, string>
            {
                { "{{INSTRUCTION}}", instruction },
                { "{{CODE_LANGUAGE}}", codeLanguage }
            };

            foreach (var mapping in variableMappings)
            {
                result = result.Replace(mapping.Key, mapping.Value);
            }

            return result;
        }

        #endregion


        #region Action Prompts

        /// <inheritdoc />
        public string BuildActionAnalysisPrompt(AIActionAnalysisInput input)
        {
            // Optimize conversation text - limit to last 10 messages and truncate long messages
            var recentMessages = input.ConversationHistory.TakeLast(10).ToList();
            var conversationText = string.Join("\n", recentMessages.Select(m =>
            {
                var content = m.Content.Length > 500 ? m.Content.Substring(0, 500) + "..." : m.Content;
                return $"{m.Role}: {content}";
            }));

            var focusAreasText = input.FocusAreas.Any() ? string.Join(", ", input.FocusAreas) : "general action items";
            var contextText = !string.IsNullOrEmpty(input.Context) && input.Context.Length > 200
                ? input.Context.Substring(0, 200) + "..."
                : input.Context ?? "No additional context provided";

            return $@"Analyze this conversation and extract actionable insights (be concise):

CONVERSATION:
{conversationText}

CONTEXT: {contextText}
FOCUS: {focusAreasText}

Please analyze and extract:
1. Action Items: Specific tasks or actions that need to be taken
2. Key Insights: Important observations or conclusions from the conversation
3. Next Steps: Recommended follow-up actions
4. Stakeholders: People or roles involved or affected
5. Priority Level: Overall urgency (Low, Medium, High, Critical)

For each action item, include:
- Title: Brief descriptive title
- Description: Detailed description of what needs to be done
- Category: Type of action (task, decision, communication, etc.)
- Priority: Individual priority level
- Assigned To: Suggested person or role (if mentioned)
- Dependencies: Other actions this depends on

Please return the results in JSON format with the following structure:
{{
    ""actionItems"": [
        {{
            ""id"": ""unique_id"",
            ""title"": ""action title"",
            ""description"": ""detailed description"",
            ""category"": ""category"",
            ""priority"": ""priority level"",
            ""assignedTo"": ""person or role"",
            ""dependencies"": [""dependency1"", ""dependency2""],
            ""tags"": [""tag1"", ""tag2""]
        }}
    ],
    ""keyInsights"": [""insight1"", ""insight2""],
    ""nextSteps"": [""step1"", ""step2""],
    ""stakeholders"": [""stakeholder1"", ""stakeholder2""],
    ""priority"": ""overall priority"",
    ""confidenceScore"": 0.85
}}";
        }

        /// <inheritdoc />
        public string BuildActionCreationPrompt(AIActionCreationInput input)
        {
            var basePrompt = "";

            if (input.AnalysisResult != null)
            {
                // Optimize analysis result - only include essential fields
                var essentialAnalysis = new
                {
                    actionItems = input.AnalysisResult.ActionItems?.Take(5).Select(a => new
                    {
                        title = a.Title?.Length > 100 ? a.Title.Substring(0, 100) + "..." : a.Title,
                        description = a.Description?.Length > 200 ? a.Description.Substring(0, 200) + "..." : a.Description,
                        priority = a.Priority
                    }),
                    keyInsights = input.AnalysisResult.KeyInsights?.Take(3).Select(i =>
                        i.Length > 150 ? i.Substring(0, 150) + "..." : i),
                    nextSteps = input.AnalysisResult.NextSteps?.Take(5).Select(s =>
                        s.Length > 100 ? s.Substring(0, 100) + "..." : s)
                };

                var analysisJson = JsonSerializer.Serialize(essentialAnalysis, new JsonSerializerOptions { WriteIndented = false });
                var contextText = !string.IsNullOrEmpty(input.Context) && input.Context.Length > 300
                    ? input.Context.Substring(0, 300) + "..."
                    : input.Context ?? "";

                basePrompt = $"""
                    Create action plan from analysis (be concise):

                    ANALYSIS: {analysisJson}
                    CONTEXT: {contextText}
                    """;
            }
            else
            {
                var actionDesc = input.ActionDescription?.Length > 500
                    ? input.ActionDescription.Substring(0, 500) + "..."
                    : input.ActionDescription ?? "";
                var contextText = !string.IsNullOrEmpty(input.Context) && input.Context.Length > 300
                    ? input.Context.Substring(0, 300) + "..."
                    : input.Context ?? "";

                basePrompt = $"""
                    Create action plan (be concise):

                    DESCRIPTION: {actionDesc}
                    CONTEXT: {contextText}
                    """;
            }

            var stakeholdersText = input.Stakeholders.Any() ? string.Join(", ", input.Stakeholders) : "to be determined";
            var requirementsText = input.Requirements.Any() ? string.Join("\n", input.Requirements) : "none specified";

            return basePrompt + $@"

STAKEHOLDERS:
{stakeholdersText}

PRIORITY:
{input.Priority}

DUE DATE:
{(input.DueDate?.ToString("yyyy-MM-dd") ?? "not specified")}

REQUIREMENTS:
{requirementsText}

Please create a detailed action plan that includes:
1. Action Plan Overview: Title, description, objective
2. Individual Actions: Specific tasks with details
3. Implementation Steps: How to execute the plan
4. Risk Factors: Potential challenges or obstacles
5. Success Metrics: How to measure success

Please return the results in JSON format with the following structure:
{{
    ""actionPlan"": {{
        ""id"": ""unique_plan_id"",
        ""title"": ""action plan title"",
        ""description"": ""detailed description"",
        ""objective"": ""main objective"",
        ""actions"": [
            {{
                ""id"": ""action_id"",
                ""title"": ""action title"",
                ""description"": ""action description"",
                ""category"": ""category"",
                ""priority"": ""priority"",
                ""status"": ""Pending"",
                ""assignedTo"": ""person or role"",
                ""dependencies"": [""dependency1""],
                ""tags"": [""tag1"", ""tag2""]
            }}
        ],
        ""stakeholders"": [""stakeholder1"", ""stakeholder2""],
        ""priority"": ""{input.Priority}"",
        ""startDate"": ""{DateTime.UtcNow:yyyy-MM-dd}"",
        ""endDate"": ""{(input.DueDate?.ToString("yyyy-MM-dd") ?? "")}"",
        ""status"": ""Draft""
    }},
    ""implementationSteps"": [""step1"", ""step2"", ""step3""],
    ""riskFactors"": [""risk1"", ""risk2""],
    ""successMetrics"": [""metric1"", ""metric2""],
    ""confidenceScore"": 0.85
}}";
        }

        #endregion

        #region Summary Prompts

        /// <inheritdoc />
        public string BuildStageSummaryPrompt(AIStageSummaryInput input)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("Generate a concise stage summary in English.");

            promptBuilder.AppendLine();
            promptBuilder.AppendLine("=== Stage Summary Generation Task ===");
            promptBuilder.AppendLine($"Stage Name: {input.StageName}");
            promptBuilder.AppendLine($"Stage Description: {input.StageDescription}");

            if (!string.IsNullOrEmpty(input.AdditionalContext))
            {
                promptBuilder.AppendLine($"Additional Context: {input.AdditionalContext}");
            }

            promptBuilder.AppendLine();

            // Check if we have any actual data to analyze
            bool hasTaskData = input.ChecklistTasks.Any();
            bool hasQuestionData = input.QuestionnaireQuestions.Any();
            bool hasFieldData = input.StaticFields.Any();

            if (!hasTaskData && !hasQuestionData && !hasFieldData)
            {
                promptBuilder.AppendLine("=== Data Status ===");
                promptBuilder.AppendLine("No checklist tasks, questionnaire responses, or field data available for this stage.");
                promptBuilder.AppendLine("This appears to be a stage without configured components or data collection.");
                promptBuilder.AppendLine();
            }

            // Add checklist tasks information
            if (input.ChecklistTasks.Any())
            {
                promptBuilder.AppendLine("=== Checklist Tasks Analysis ===");
                var completedTasks = input.ChecklistTasks.Count(t => t.IsCompleted);
                var totalTasks = input.ChecklistTasks.Count;
                var requiredTasks = input.ChecklistTasks.Count(t => t.IsRequired);
                var completedRequiredTasks = input.ChecklistTasks.Count(t => t.IsRequired && t.IsCompleted);

                // Provide completion statistics for accurate assessment
                promptBuilder.AppendLine($"Completion Status: {completedTasks}/{totalTasks} tasks completed ({(totalTasks > 0 ? (decimal)completedTasks / totalTasks * 100 : 0):F0}%)");
                if (requiredTasks > 0)
                {
                    promptBuilder.AppendLine($"Required Tasks: {completedRequiredTasks}/{requiredTasks} completed");
                }
                promptBuilder.AppendLine();

                promptBuilder.AppendLine("Tasks:");
                foreach (var task in input.ChecklistTasks)
                {
                    var priority = task.IsRequired ? " [Required]" : "";
                    promptBuilder.AppendLine($"- [{(task.IsCompleted ? "✓" : "○")}] {task.TaskName}{priority}");
                    if (task.IsCompleted && !string.IsNullOrEmpty(task.CompletionNotes))
                    {
                        promptBuilder.AppendLine($"  Notes: {task.CompletionNotes}");
                    }
                    else if (!task.IsCompleted && task.IsRequired)
                    {
                        promptBuilder.AppendLine($"  Status: Pending (Required)");
                    }
                }
                promptBuilder.AppendLine();
            }

            // Add questionnaire information
            if (input.QuestionnaireQuestions.Any())
            {
                promptBuilder.AppendLine("=== Questionnaire Analysis ===");
                var answeredQuestions = input.QuestionnaireQuestions.Count(q => q.IsAnswered);
                var totalQuestions = input.QuestionnaireQuestions.Count;
                var requiredQuestions = input.QuestionnaireQuestions.Count(q => q.IsRequired);
                var answeredRequiredQuestions = input.QuestionnaireQuestions.Count(q => q.IsRequired && q.IsAnswered);

                // Provide completion statistics for accurate assessment
                promptBuilder.AppendLine($"Response Status: {answeredQuestions}/{totalQuestions} questions answered ({(totalQuestions > 0 ? (decimal)answeredQuestions / totalQuestions * 100 : 0):F0}%)");
                if (requiredQuestions > 0)
                {
                    promptBuilder.AppendLine($"Required Questions: {answeredRequiredQuestions}/{requiredQuestions} answered");
                }
                promptBuilder.AppendLine();

                promptBuilder.AppendLine("Questions:");
                foreach (var question in input.QuestionnaireQuestions)
                {
                    var priority = question.IsRequired ? " [Required]" : "";
                    promptBuilder.AppendLine($"- [{(question.IsAnswered ? "✓" : "○")}] {question.QuestionText}{priority}");
                    if (question.IsAnswered && question.Answer != null)
                    {
                        promptBuilder.AppendLine($"  Answer: {question.Answer}");
                    }
                    else if (!question.IsAnswered && question.IsRequired)
                    {
                        promptBuilder.AppendLine($"  Status: Pending (Required)");
                    }
                }
                promptBuilder.AppendLine();
            }

            // Add static fields information
            if (input.StaticFields.Any())
            {
                promptBuilder.AppendLine("=== Static Fields ===");
                foreach (var field in input.StaticFields.Where(f => f.IsRequired || !string.IsNullOrEmpty(f.Description)))
                {
                    promptBuilder.AppendLine($"- {field.DisplayName ?? field.FieldName}");
                }
                promptBuilder.AppendLine();
            }

            // Enhanced summary requirements with data-driven guidance
            promptBuilder.AppendLine("=== Summary Requirements ===");
            if (hasTaskData || hasQuestionData || hasFieldData)
            {
                promptBuilder.AppendLine("Provide a comprehensive stage summary in maximum 150 words with two paragraphs:");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("First Paragraph - Stage Function:");
                promptBuilder.AppendLine("   - Describe the main purpose and core activities of this stage");
                promptBuilder.AppendLine("   - Explain what this stage aims to accomplish");
                promptBuilder.AppendLine("   - Outline the key areas of focus and primary deliverables");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("Second Paragraph - Progress Status:");
                promptBuilder.AppendLine("   - Report actual completion rates (use specific percentages and counts shown above)");
                promptBuilder.AppendLine("   - Highlight completed achievements and pending requirements");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("Output Rules:");
                promptBuilder.AppendLine("- Write as two natural paragraphs without section titles or labels");
                promptBuilder.AppendLine("- Start with stage function/content, then progress status");
                promptBuilder.AppendLine("- Base summary ONLY on the provided data above");
                promptBuilder.AppendLine("- Use specific numbers and completion rates shown");
                promptBuilder.AppendLine("- Plain text only, no formatting or headings");
                promptBuilder.AppendLine("- Maximum 150 words total");
                promptBuilder.AppendLine("- Do NOT invent information not present in the data");
            }
            else
            {
                promptBuilder.AppendLine("Provide a stage summary in maximum 150 words with two paragraphs:");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("First Paragraph - Stage Function:");
                promptBuilder.AppendLine("   - Describe the intended purpose and activities of this stage");
                promptBuilder.AppendLine("   - Explain what this stage is designed to accomplish");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("Second Paragraph - Current Status:");
                promptBuilder.AppendLine("   - Note that no specific tasks or questions are currently configured");
                promptBuilder.AppendLine("   - Suggest what components might be needed to make this stage actionable");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("Output Rules:");
                promptBuilder.AppendLine("- Write as two natural paragraphs without section titles or labels");
                promptBuilder.AppendLine("- Start with stage function/content, then current status");
                promptBuilder.AppendLine("- Do NOT invent completion percentages or fake data");
                promptBuilder.AppendLine("- Plain text only, no formatting or headings");
                promptBuilder.AppendLine("- Maximum 150 words");
                promptBuilder.AppendLine("- Be honest about the lack of configured activities");
            }

            return promptBuilder.ToString();
        }

        #endregion

        #region Requirements Parsing Prompts

        /// <inheritdoc />
        public string BuildRequirementsParsingPrompt(string naturalLanguage)
        {
            return $"""
                Please analyze the following natural language description and extract structured requirement information:

                Description: {naturalLanguage}

                Please extract:
                1. Process type
                2. Involved personnel
                3. Key steps
                4. Approval processes
                5. Notification requirements

                Please return the results in JSON format.
                """;
        }

        #endregion
    }
}
