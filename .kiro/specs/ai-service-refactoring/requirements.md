# 需求文档

## 简介

FlowFlex 后端的 AI 服务层（`AIService`）当前是一个巨型 partial class，包含约 8000+ 行代码，构造函数有 16 个依赖注入，严重违反单一职责原则。本次重构旨在将 `AIService` 按职责拆分为多个独立服务，提取统一的 AI Provider 抽象层，统一 Prompt 管理和响应解析逻辑，同时保持 Controller 层的向后兼容性。

## 术语表

- **AIService**: 当前的巨型 AI 服务 partial class，分布在 `AIService.Main.cs`、`AIService.Chat.cs`、`AIService.Generation.cs`、`AIService.ActionAndHttp.cs`、`AIService.Summary.cs` 五个文件中
- **AI_Provider_Adapter**: 统一的 AI 提供商调用抽象层，封装对 ZhipuAI、OpenAI、Gemini、Claude、DeepSeek、LLMGateway 等提供商的调用
- **AIProviderResponse**: AI 提供商返回的统一响应数据结构
- **Prompt_Builder**: 统一的 Prompt 构建组件，负责管理和构建各类 AI 提示词
- **Response_Parser**: 统一的 AI 响应解析组件，负责 JSON 解析、修复和 Fallback 生成
- **Workflow_Generation_Service**: 负责 Workflow 生成（同步+流式）、增强、验证的独立服务
- **Questionnaire_Generation_Service**: 负责 Questionnaire 生成（同步+流式）的独立服务
- **Checklist_Generation_Service**: 负责 Checklist 生成（同步+流式）的独立服务
- **Chat_Service**: 负责 AI 聊天功能（消息发送、流式聊天）的独立服务
- **Action_Service**: 负责 Action 分析和创建的独立服务
- **Summary_Service**: 负责 Stage 摘要生成的独立服务
- **Stage_Component_Service**: 负责根据 AI 生成结果创建 Stage 组件（Checklist、Questionnaire）的独立服务
- **IAIService**: 当前的单一大接口，定义了所有 AI 服务方法
- **IScopedService**: DI 生命周期标记接口，用于自动注册 Scoped 服务

## 需求

### 需求 1：提取统一的 AI Provider 抽象层

**用户故事：** 作为后端开发者，我希望有一个统一的 AI Provider 抽象层，以便消除 Main.cs 和 Chat.cs 中重复的提供商调用代码，并使新增 Provider 更加简单。

#### 验收标准

1. THE AI_Provider_Adapter SHALL provide a unified interface for calling AI providers with a single-prompt input and returning an AIProviderResponse
2. THE AI_Provider_Adapter SHALL provide a unified interface for calling AI providers with a multi-message (chat) input and returning an AIProviderResponse
3. THE AI_Provider_Adapter SHALL provide a unified interface for streaming AI provider responses with a multi-message input and returning an IAsyncEnumerable of string chunks
4. WHEN a provider name is specified, THE AI_Provider_Adapter SHALL route the request to the corresponding provider implementation (ZhipuAI, OpenAI, Gemini, Claude, DeepSeek, LLMGateway)
5. WHEN an unsupported provider name is specified, THEN THE AI_Provider_Adapter SHALL throw a CRMException with an appropriate error code
6. THE AI_Provider_Adapter SHALL accept optional parameters for model ID, model name, and max token override to allow caller customization
7. WHEN a preferred provider fails, THE AI_Provider_Adapter SHALL support fallback to an alternative provider based on configuration
8. THE AI_Provider_Adapter SHALL encapsulate all HTTP client creation, request construction, and response deserialization for each provider

### 需求 2：拆分 IAIService 接口为细粒度接口

**用户故事：** 作为后端开发者，我希望将单一的 `IAIService` 接口拆分为多个职责明确的小接口，以便遵循接口隔离原则，降低服务间耦合。

#### 验收标准

1. THE system SHALL define an IAIWorkflowGenerationService interface containing Workflow generation, enhancement, validation, and stage component creation methods
2. THE system SHALL define an IAIQuestionnaireGenerationService interface containing Questionnaire generation methods (synchronous and streaming)
3. THE system SHALL define an IAIChecklistGenerationService interface containing Checklist generation methods (synchronous and streaming)
4. THE system SHALL define an IAIChatService interface containing chat message sending and streaming methods
5. THE system SHALL define an IAIActionService interface containing Action analysis and creation methods (synchronous and streaming)
6. THE system SHALL define an IAISummaryService interface containing Stage summary generation methods
7. THE system SHALL define an IAIRequirementsParsingService interface containing requirements parsing methods
8. WHEN a new interface is created, THE system SHALL ensure the interface extends IScopedService for automatic DI registration

### 需求 3：拆分 AIService 为独立服务实现

**用户故事：** 作为后端开发者，我希望将 AIService 的各个 partial class 文件拆分为独立的服务类，以便每个服务类只关注单一职责，构造函数依赖数量大幅减少。

#### 验收标准

1. THE Workflow_Generation_Service SHALL implement IAIWorkflowGenerationService and contain all Workflow generation, enhancement, validation logic
2. THE Questionnaire_Generation_Service SHALL implement IAIQuestionnaireGenerationService and contain all Questionnaire generation logic
3. THE Checklist_Generation_Service SHALL implement IAIChecklistGenerationService and contain all Checklist generation logic
4. THE Chat_Service SHALL implement IAIChatService and contain all chat messaging and streaming logic
5. THE Action_Service SHALL implement IAIActionService and contain all Action analysis and creation logic
6. THE Summary_Service SHALL implement IAISummaryService and contain all Stage summary generation logic
7. WHEN a new service is created, THE service SHALL depend on AI_Provider_Adapter for AI provider calls instead of directly calling provider-specific methods
8. WHEN a new service is created, THE service SHALL depend on Prompt_Builder for prompt construction instead of containing inline prompt building methods
9. WHEN a new service is created, THE service SHALL depend on Response_Parser for response parsing instead of containing inline parsing logic
10. WHEN a new service is created, THE service constructor SHALL have no more than 8 dependency injection parameters

### 需求 4：统一 Prompt 管理

**用户故事：** 作为后端开发者，我希望将散落在各个 partial class 文件中的 Prompt 构建方法集中管理，以便统一维护和复用 Prompt 模板。

#### 验收标准

1. THE Prompt_Builder SHALL provide methods for building Workflow generation prompts from AIWorkflowGenerationInput
2. THE Prompt_Builder SHALL provide methods for building Questionnaire generation prompts from AIQuestionnaireGenerationInput
3. THE Prompt_Builder SHALL provide methods for building Checklist generation prompts from AIChecklistGenerationInput
4. THE Prompt_Builder SHALL provide methods for building chat system prompts and user prompts from AIChatInput
5. THE Prompt_Builder SHALL provide methods for building Action analysis and creation prompts
6. THE Prompt_Builder SHALL provide methods for building Stage summary prompts from AIStageSummaryInput
7. THE Prompt_Builder SHALL provide methods for building Workflow modification prompts from AIWorkflowModificationInput
8. THE Prompt_Builder SHALL provide methods for building batch Checklist and batch Questionnaire generation prompts
9. THE Prompt_Builder SHALL provide a method for processing template variables in prompt templates

### 需求 5：统一响应解析逻辑

**用户故事：** 作为后端开发者，我希望将重复的 JSON 解析、修复和 Fallback 生成逻辑提取为统一的响应解析组件，以便减少代码重复并提高解析的可靠性。

#### 验收标准

1. THE Response_Parser SHALL provide a method to parse AI responses into AIWorkflowGenerationResult, including JSON repair and fallback generation
2. THE Response_Parser SHALL provide a method to parse AI responses into AIQuestionnaireGenerationResult
3. THE Response_Parser SHALL provide a method to parse AI responses into AIChecklistGenerationResult
4. THE Response_Parser SHALL provide a method to parse AI responses into AIStageSummaryResult
5. THE Response_Parser SHALL provide a method to parse chat responses into AIChatResponse
6. THE Response_Parser SHALL provide a generic JSON repair method that fixes common AI response JSON formatting issues (trailing commas, unescaped characters, incomplete JSON)
7. WHEN JSON parsing fails after repair attempts, THEN THE Response_Parser SHALL generate a fallback result with a reduced confidence score
8. THE Response_Parser SHALL provide methods to calculate confidence scores for Workflow, Questionnaire, and Checklist results

### 需求 6：提取 Stage 组件创建为独立服务

**用户故事：** 作为后端开发者，我希望将 `CreateStageComponentsAsync` 及其相关的 Checklist/Questionnaire 分配和创建逻辑提取为独立服务，以便与 AI 生成逻辑解耦。

#### 验收标准

1. THE Stage_Component_Service SHALL provide a method to create Stage components (Checklists and Questionnaires) from AI generation results
2. THE Stage_Component_Service SHALL handle the distribution logic for determining how many Checklists and Questionnaires each Stage receives
3. THE Stage_Component_Service SHALL handle the distribution logic for determining how many Tasks each Checklist receives and how many Questions each Questionnaire receives
4. THE Stage_Component_Service SHALL generate appropriate names and descriptions for Checklists and Questionnaires based on Stage information
5. THE Stage_Component_Service SHALL ensure unique naming for Checklists and Questionnaires within the same scope
6. WHEN Stage component creation fails for a specific Stage, THEN THE Stage_Component_Service SHALL log the error and continue processing remaining Stages

### 需求 7：保持 Controller 层向后兼容

**用户故事：** 作为后端开发者，我希望重构后 Controller 层的改动最小化，以便现有 API 契约不受影响，前端无需修改。

#### 验收标准

1. WHEN the refactoring is complete, THE system SHALL maintain all existing API endpoints with identical request and response contracts
2. WHEN the refactoring is complete, THE Controller classes SHALL inject the new fine-grained service interfaces instead of IAIService
3. THE system SHALL keep all existing DTO classes (input and output) unchanged in their namespace and structure
4. WHEN a Controller currently calls a method on IAIService, THE Controller SHALL call the equivalent method on the corresponding new service interface after refactoring
5. IF the original IAIService interface is retained for backward compatibility, THEN THE system SHALL implement it as a facade that delegates to the new fine-grained services

### 需求 8：Prompt 历史记录和横切关注点

**用户故事：** 作为后端开发者，我希望 Prompt 历史保存、IP/UA 获取等横切关注点被统一管理，以便各个拆分后的服务不需要各自重复实现这些逻辑。

#### 验收标准

1. THE system SHALL provide a shared service or base class for saving AI prompt history records asynchronously via the background task queue
2. THE system SHALL provide a shared utility for obtaining client IP address and User-Agent from HttpContext
3. WHEN any AI service completes a provider call, THE service SHALL save the prompt history including prompt type, entity type, entity ID, provider information, token usage, and response time
4. WHEN prompt history saving fails, THEN THE system SHALL log the error without affecting the main AI operation flow
