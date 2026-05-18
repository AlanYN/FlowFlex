# 实施计划：AI 服务层重构

## 概述

将巨型 AIService partial class 拆分为多个独立服务，采用自底向上的策略：先提取共享组件（Provider 抽象层、Prompt 构建器、响应解析器、横切关注点基类），再拆分业务服务，最后更新 Controller 注入并提供向后兼容 Facade。

## 任务

- [x] 1. 创建共享基础组件
  - [x] 1.1 创建 AIProviderRequest 和 AIChatProviderRequest 数据模型
    - 在 `Application.Contracts/IServices/AI/` 目录下创建 `IAIProviderAdapter.cs`
    - 定义 `IAIProviderAdapter` 接口（CallAsync, CallChatAsync, StreamChatAsync, CallWithFallbackAsync）
    - 定义 `AIProviderRequest` 和 `AIChatProviderRequest` DTO 类
    - 接口继承 `IScopedService`
    - _Requirements: 1.1, 1.2, 1.3, 1.6_

  - [x] 1.2 实现 AIProviderAdapter
    - 在 `Application/Services/AI/Providers/` 目录下创建 `AIProviderAdapter.cs`
    - 从 `AIService.Main.cs` 提取 `CallAIProviderAsync` 及所有 Provider 调用方法（CallZhipuAIAsync, CallOpenAIAsync, CallGeminiAsync, CallClaudeAsync, CallDeepSeekAsync, CallGenericOpenAICompatibleAsync, CallItemGatewayForMainAsync）
    - 从 `AIService.Chat.cs` 提取 Chat 版本的 Provider 调用方法（CallZhipuAIWithConfigAsync, CallOpenAIWithConfigAsync, CallGeminiWithConfigAsync, CallClaudeWithConfigAsync, CallDeepSeekWithConfigAsync, CallLLMGatewayWithConfigAsync）
    - 从 `AIService.Chat.cs` 提取流式调用方法（CallAIProviderForStreamChatAsync, CallOpenAIStreamAsync, CallDeepSeekStreamAsync, CallLLMGatewayStreamAsync, CallItemGatewayStreamAsync）
    - 实现 Provider 路由逻辑（switch on provider name）
    - 实现 Fallback 逻辑（从 CallAIProviderWithFallbackForActionAsync 和 CallAIProviderWithFallbackForSummaryAsync 提取）
    - 实现默认配置解析（从 IAIModelConfigService 获取租户默认配置）
    - _Requirements: 1.4, 1.5, 1.7, 1.8_

  - [ ]* 1.3 编写 AIProviderAdapter 属性测试
    - **Property 1: Provider 路由正确性**
    - **Validates: Requirements 1.4, 1.5**

  - [ ]* 1.4 编写 AIProviderAdapter 单元测试
    - 测试各 Provider 的请求构造
    - 测试 Fallback 场景
    - **Property 2: Provider Fallback 机制**
    - **Validates: Requirements 1.7**

- [x] 2. 创建 Prompt 构建器和响应解析器
  - [x] 2.1 创建 IAIPromptBuilder 接口和实现
    - 在 `Application.Contracts/IServices/AI/` 下创建 `IAIPromptBuilder.cs` 接口
    - 在 `Application/Services/AI/Prompts/` 下创建 `AIPromptBuilder.cs` 实现
    - 从 `AIService.Generation.cs` 提取：BuildWorkflowGenerationPrompt, BuildQuestionnaireGenerationPrompt, BuildChecklistGenerationPrompt, BuildBatchChecklistGenerationPrompt, BuildBatchQuestionnaireGenerationPrompt, BuildWorkflowModificationPromptAsync
    - 从 `AIService.Chat.cs` 提取：BuildChatPrompt, GetChatSystemPrompt, GetGenerateCodePrompt, ProcessTemplateVariables
    - 从 `AIService.ActionAndHttp.cs` 提取：BuildActionAnalysisPrompt, BuildActionCreationPrompt
    - 从 `AIService.Summary.cs` 提取：BuildStageSummaryPrompt
    - 接口继承 `IScopedService`
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 4.8, 4.9_

  - [x] 2.2 创建 IAIResponseParser 接口和实现
    - 在 `Application.Contracts/IServices/AI/` 下创建 `IAIResponseParser.cs` 接口
    - 在 `Application/Services/AI/Parsing/` 下创建 `AIResponseParser.cs` 实现
    - 从 `AIService.Generation.cs` 提取：ParseWorkflowGenerationResponse, TryRepairAndParseWorkflow, GenerateFallbackWorkflow, ParseQuestionnaireGenerationResponse, ParseChecklistGenerationResponse, ParseAIChecklistResponse, ParseAIQuestionnaireResponse, ParseBatchChecklistResponse, ParseBatchQuestionnaireResponse, FixJsonContent, ParseQuestionOptions, ParseWorkflowFromJsonElement, ParseEmbeddedChecklist, ParseEmbeddedQuestionnaire
    - 从 `AIService.Generation.cs` 提取：CalculateConfidenceScore, CalculateQuestionnaireConfidenceScore, CalculateChecklistConfidenceScore, CalculateWorkflowQualityScore
    - 从 `AIService.Chat.cs` 提取：ParseChatResponse, GenerateFallbackChatResponse, GenerateErrorChatResponse, DetermineChatCompletion, ExtractSuggestions, ExtractNextQuestions
    - 从 `AIService.Summary.cs` 提取：ParseStageSummaryResponse, DetermineEffectiveLanguage, ContainsCjk
    - 接口继承 `IScopedService`
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8_

  - [ ]* 2.3 编写 AIResponseParser 属性测试
    - **Property 5: 响应解析鲁棒性**
    - **Property 6: JSON 修复幂等性**
    - **Property 7: 置信度分数范围不变量**
    - **Validates: Requirements 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8**

  - [ ]* 2.4 编写 AIResponseParser 单元测试
    - 测试具体的 JSON 修复场景（trailing comma, single quotes, missing comma）
    - 测试各类响应的解析边界情况
    - _Requirements: 5.6_

- [x] 3. 创建横切关注点基类和 Stage 组件服务
  - [x] 3.1 创建 AIServiceBase 抽象基类
    - 在 `Application/Services/AI/` 下创建 `AIServiceBase.cs`
    - 从 `AIService.Main.cs` 提取：SavePromptHistoryAsync, GetClientIpAddress, GetUserAgent
    - 从 `AIService.Generation.cs` 提取：QueuePromptHistorySave, QueueFailedPromptHistorySave, ExecuteGenerationAsync, CreateFailedResult, SetConfidenceScore
    - 基类依赖：ILogger, IAIPromptHistoryRepository, IOperatorContextService, IHttpContextAccessor, IBackgroundTaskQueue
    - _Requirements: 8.1, 8.2, 8.3, 8.4_

  - [x] 3.2 创建 IStageComponentService 接口和实现
    - 在 `Application.Contracts/IServices/AI/` 下创建 `IStageComponentService.cs` 接口
    - 在 `Application/Services/AI/StageComponent/` 下创建 `StageComponentService.cs` 实现
    - 从 `AIService.Generation.cs` 提取：CreateStageComponentsAsync, DetermineChecklistCount, DetermineQuestionnaireCount, DetermineTaskCount, DetermineQuestionCount, GenerateChecklistName, GenerateChecklistDescription, GenerateQuestionnaireName, GenerateQuestionnaireDescription, GenerateTasksForStage, GenerateQuestionsForStage, EnsureUniqueChecklistNameAsync, EnsureUniqueQuestionnaireNameAsync
    - 从 `AIService.Generation.cs` 提取 Fallback 生成：GenerateFallbackChecklist, GenerateFallbackQuestionnaire, GenerateChecklistsForStages, GenerateQuestionnairesForStages
    - 接口继承 `IScopedService`
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6_

  - [ ]* 3.3 编写 StageComponentService 属性测试
    - **Property 8: Stage 组件分配不变量**
    - **Property 9: Stage 组件命名唯一性**
    - **Validates: Requirements 6.2, 6.3, 6.5**

  - [ ]* 3.4 编写 StageComponentService 单元测试
    - 测试边界情况：0 个 stage, 1 个 stage, stage 无 checklist/questionnaire
    - 测试名称生成逻辑
    - _Requirements: 6.4, 6.6_

- [x] 4. Checkpoint - 确保共享组件测试通过
  - 确保所有测试通过，如有问题请咨询用户。

- [x] 5. 创建细粒度服务接口
  - [x] 5.1 创建所有细粒度服务接口文件
    - 在 `Application.Contracts/IServices/AI/` 下创建：
      - `IAIWorkflowGenerationService.cs` — GenerateWorkflowAsync, StreamGenerateWorkflowAsync, EnhanceWorkflowAsync (2 overloads), ValidateWorkflowAsync, CreateStageComponentsAsync
      - `IAIQuestionnaireGenerationService.cs` — GenerateQuestionnaireAsync, StreamGenerateQuestionnaireAsync
      - `IAIChecklistGenerationService.cs` — GenerateChecklistAsync, StreamGenerateChecklistAsync
      - `IAIChatService.cs` — SendChatMessageAsync, StreamChatAsync
      - `IAIActionService.cs` — AnalyzeActionAsync, CreateActionAsync, StreamAnalyzeActionAsync, StreamCreateActionAsync, StreamGenerateHttpConfigAsync
      - `IAISummaryService.cs` — GenerateStageSummaryAsync
      - `IAIRequirementsParsingService.cs` — ParseRequirementsAsync (2 overloads)
    - 所有接口继承 `IScopedService`
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8_

  - [ ]* 5.2 编写服务接口架构属性测试
    - **Property 3: 服务接口 DI 注册**
    - **Property 4: 构造函数依赖数量约束**
    - **Validates: Requirements 2.8, 3.10**

- [x] 6. 实现业务服务类
  - [x] 6.1 实现 AIWorkflowGenerationService
    - 在 `Application/Services/AI/Workflow/` 下创建 `AIWorkflowGenerationService.cs`
    - 继承 `AIServiceBase`，实现 `IAIWorkflowGenerationService`
    - 从 `AIService.Generation.cs` 迁移：GenerateWorkflowAsync, StreamGenerateWorkflowAsync, ProduceWorkflowStreamAsync, EnhanceWorkflowAsync (2 overloads), ValidateWorkflowAsync, GetExistingWorkflowAsync, CallAIProviderWithRetryAsync
    - 委托 CreateStageComponentsAsync 到 IStageComponentService
    - 委托 AI 调用到 IAIProviderAdapter，Prompt 构建到 IAIPromptBuilder，响应解析到 IAIResponseParser
    - 依赖：IAIProviderAdapter, IAIPromptBuilder, IAIResponseParser, IWorkflowService, IStageComponentService, IStageRepository + base(5)
    - _Requirements: 3.1, 3.7, 3.8, 3.9, 3.10_

  - [x] 6.2 实现 AIQuestionnaireGenerationService
    - 在 `Application/Services/AI/Questionnaire/` 下创建 `AIQuestionnaireGenerationService.cs`
    - 继承 `AIServiceBase`，实现 `IAIQuestionnaireGenerationService`
    - 从 `AIService.Generation.cs` 迁移：GenerateQuestionnaireAsync, StreamGenerateQuestionnaireAsync
    - 委托 AI 调用到 IAIProviderAdapter，Prompt 构建到 IAIPromptBuilder，响应解析到 IAIResponseParser
    - _Requirements: 3.2, 3.7, 3.8, 3.9, 3.10_

  - [x] 6.3 实现 AIChecklistGenerationService
    - 在 `Application/Services/AI/Checklist/` 下创建 `AIChecklistGenerationService.cs`
    - 继承 `AIServiceBase`，实现 `IAIChecklistGenerationService`
    - 从 `AIService.Generation.cs` 迁移：GenerateChecklistAsync, StreamGenerateChecklistAsync
    - 委托 AI 调用到 IAIProviderAdapter，Prompt 构建到 IAIPromptBuilder，响应解析到 IAIResponseParser
    - _Requirements: 3.3, 3.7, 3.8, 3.9, 3.10_

  - [x] 6.4 实现 AIChatService
    - 在 `Application/Services/AI/Chat/` 下创建 `AIChatService.cs`
    - 继承 `AIServiceBase`，实现 `IAIChatService`
    - 从 `AIService.Chat.cs` 迁移：SendChatMessageAsync, StreamChatAsync, ProduceChatStreamAsync, CallAIProviderForChatAsync, GetNativeModelName
    - 委托 AI 调用到 IAIProviderAdapter，Prompt 构建到 IAIPromptBuilder，响应解析到 IAIResponseParser
    - 依赖：IAIProviderAdapter, IAIPromptBuilder, IAIResponseParser, IAIModelConfigService + base(5)
    - _Requirements: 3.4, 3.7, 3.8, 3.9, 3.10_

  - [x] 6.5 实现 AIActionService
    - 在 `Application/Services/AI/Action/` 下创建 `AIActionService.cs`
    - 继承 `AIServiceBase`，实现 `IAIActionService`
    - 从 `AIService.ActionAndHttp.cs` 迁移：AnalyzeActionAsync, CreateActionAsync, StreamAnalyzeActionAsync, StreamCreateActionAsync, StreamGenerateHttpConfigAsync
    - 委托 AI 调用到 IAIProviderAdapter，Prompt 构建到 IAIPromptBuilder
    - _Requirements: 3.5, 3.7, 3.8, 3.10_

  - [x] 6.6 实现 AISummaryService
    - 在 `Application/Services/AI/Summary/` 下创建 `AISummaryService.cs`
    - 继承 `AIServiceBase`，实现 `IAISummaryService`
    - 从 `AIService.Summary.cs` 迁移：GenerateStageSummaryAsync, CallAIProviderWithFallbackForSummaryAsync
    - 委托 AI 调用到 IAIProviderAdapter，Prompt 构建到 IAIPromptBuilder，响应解析到 IAIResponseParser
    - _Requirements: 3.6, 3.7, 3.8, 3.9, 3.10_

  - [x] 6.7 实现 AIRequirementsParsingService
    - 在 `Application/Services/AI/Requirements/` 下创建 `AIRequirementsParsingService.cs`
    - 继承 `AIServiceBase`，实现 `IAIRequirementsParsingService`
    - 从 `AIService.Main.cs` 迁移：ParseRequirementsAsync (2 overloads)
    - 委托 AI 调用到 IAIProviderAdapter，Prompt 构建到 IAIPromptBuilder，响应解析到 IAIResponseParser
    - _Requirements: 3.7, 3.8, 3.9, 3.10_

- [x] 7. Checkpoint - 确保所有服务实现编译通过
  - 确保所有测试通过，如有问题请咨询用户。

- [x] 8. 创建 Facade 并更新 Controller 注入
  - [x] 8.1 创建 AIServiceFacade
    - 在 `Application/Services/AI/` 下创建 `AIServiceFacade.cs`
    - 实现 `IAIService` 接口，注入所有 7 个细粒度服务接口
    - 每个方法直接委托到对应的细粒度服务
    - 实现 `IScopedService` 标记接口
    - _Requirements: 7.5_

  - [ ]* 8.2 编写 AIServiceFacade 属性测试
    - **Property 10: Facade 委托等价性**
    - **Validates: Requirements 7.5**

  - [x] 8.3 更新 Controller 注入
    - 更新 `AIWorkflowController` 注入 `IAIWorkflowGenerationService`（替代 IAIService）
    - 更新 `AIChatController` 注入 `IAIChatService`（替代 IAIService）
    - 更新 `AIController` 注入 `IAIActionService`（替代 IAIService）
    - 保留 `AIConfigController` 注入 `IAIModelConfigService`（不变）
    - 更新各 Controller 的方法调用，使用新接口方法
    - 保持所有 API 路由、请求/响应契约不变
    - _Requirements: 7.1, 7.2, 7.3, 7.4_

- [x] 9. 清理旧代码
  - [x] 9.1 删除旧的 AIService partial class 文件
    - 确认所有功能已迁移到新服务后，删除：
      - `AIService.Main.cs`
      - `AIService.Chat.cs`
      - `AIService.Generation.cs`
      - `AIService.ActionAndHttp.cs`
      - `AIService.Summary.cs`
    - 保留 `UserAIModelConfigService.cs`（不在重构范围内）
    - 保留 `IAIService.cs` 中的接口定义和所有 DTO 类
    - _Requirements: 7.3_

- [x] 10. 最终 Checkpoint - 确保所有测试通过
  - 确保所有测试通过，如有问题请咨询用户。

## 备注

- 标记 `*` 的任务为可选任务，可跳过以加速 MVP
- 每个任务引用了具体的需求编号以确保可追溯性
- Checkpoint 确保增量验证
- 属性测试验证通用正确性属性，单元测试验证具体示例和边界情况
- 重构过程中应保持 `git commit` 的原子性，每完成一个主要任务（1-3, 5-6, 8-9）后提交
