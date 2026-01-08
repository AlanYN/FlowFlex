# Stage Condition 设计文档

## 概述

本文档描述 Stage Condition（阶段条件）功能的技术设计，包括数据模型、组件接口、API 设计和 RulesEngine 集成方案。

---

## 架构概览

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              API Layer                                       │
│  ┌─────────────────────┐  ┌─────────────────────┐                           │
│  │ StageConditionController │  │ OnboardingController │                     │
│  └──────────┬──────────┘  └──────────┬──────────┘                           │
└─────────────┼────────────────────────┼──────────────────────────────────────┘
              │                        │
┌─────────────┼────────────────────────┼──────────────────────────────────────┐
│             ▼                        ▼           Service Layer              │
│  ┌─────────────────────┐  ┌─────────────────────┐                           │
│  │ StageConditionService │  │  OnboardingService  │                         │
│  └──────────┬──────────┘  └──────────┬──────────┘                           │
│             │                        │                                       │
│             ▼                        ▼                                       │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                    RulesEngine Service Layer                         │    │
│  │  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐   │    │
│  │  │ RulesEngineService │  │  ActionExecutor  │  │ComponentDataService│  │    │
│  │  └──────────────────┘  └──────────────────┘  └──────────────────┘   │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────────────┘
              │
┌─────────────┼───────────────────────────────────────────────────────────────┐
│             ▼                        Data Layer                              │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐           │
│  │ StageCondition   │  │   Onboarding     │  │ OperationChangeLog│          │
│  │   Repository     │  │   Repository     │  │   Repository     │           │
│  └──────────────────┘  └──────────────────┘  └──────────────────┘           │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 数据模型

### StageCondition 实体

```csharp
/// <summary>
/// Stage Condition Entity - Defines condition rules and actions for a stage
/// </summary>
[SugarTable("ff_stage_condition")]
public class StageCondition : OwEntityBase
{
    /// <summary>
    /// Associated Stage ID
    /// </summary>
    [SugarColumn(ColumnName = "stage_id")]
    public long StageId { get; set; }

    /// <summary>
    /// Associated Workflow ID (denormalized for query performance)
    /// </summary>
    [SugarColumn(ColumnName = "workflow_id")]
    public long WorkflowId { get; set; }

    /// <summary>
    /// Condition Name
    /// </summary>
    [Required]
    [StringLength(100)]
    [SugarColumn(ColumnName = "name")]
    public string Name { get; set; }

    /// <summary>
    /// Condition Description
    /// </summary>
    [StringLength(500)]
    [SugarColumn(ColumnName = "description")]
    public string Description { get; set; }

    /// <summary>
    /// RulesEngine Workflow JSON (Microsoft RulesEngine format)
    /// </summary>
    [SugarColumn(ColumnName = "rules_json", ColumnDataType = "jsonb", IsJson = true)]
    public string RulesJson { get; set; }

    /// <summary>
    /// Actions JSON (array of action configurations)
    /// </summary>
    [SugarColumn(ColumnName = "actions_json", ColumnDataType = "jsonb", IsJson = true)]
    public string ActionsJson { get; set; }

    /// <summary>
    /// Fallback Stage ID (when condition is not met)
    /// </summary>
    [SugarColumn(ColumnName = "fallback_stage_id")]
    public long? FallbackStageId { get; set; }

    /// <summary>
    /// Is Active
    /// </summary>
    [SugarColumn(ColumnName = "is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Condition Status (Valid, Invalid, Draft)
    /// </summary>
    [StringLength(20)]
    [SugarColumn(ColumnName = "status")]
    public string Status { get; set; } = "Valid";
}
```

### RulesJson 格式 (Microsoft RulesEngine)

```json
[
  {
    "WorkflowName": "StageCondition",
    "Rules": [
      {
        "RuleName": "ChecklistCompleted",
        "Expression": "input.checklist.status == \"Completed\""
      },
      {
        "RuleName": "HighScore",
        "Expression": "input.questionnaire.backgroundScore >= 90"
      }
    ]
  }
]
```

### ActionsJson 格式

```json
[
  {
    "type": "GoToStage",
    "targetStageId": 12345,
    "order": 1
  },
  {
    "type": "SendNotification",
    "recipientType": "User",
    "recipientId": "user-123",
    "templateId": "stage-skip-notification",
    "order": 2
  },
  {
    "type": "TriggerAction",
    "actionDefinitionId": 67890,
    "parameters": { "key": "value" },
    "order": 3
  }
]
```

---

## 组件和接口

### IStageConditionService

```csharp
/// <summary>
/// Stage Condition service interface
/// </summary>
public interface IStageConditionService : IScopedService
{
    // CRUD Operations
    Task<long> CreateAsync(StageConditionInputDto input);
    Task<bool> UpdateAsync(long id, StageConditionInputDto input);
    Task<bool> DeleteAsync(long id);
    Task<StageConditionOutputDto> GetByIdAsync(long id);
    
    // Query Operations
    Task<StageConditionOutputDto> GetByStageIdAsync(long stageId);
    Task<List<StageConditionOutputDto>> GetByWorkflowIdAsync(long workflowId);
    
    // Validation
    Task<ConditionValidationResult> ValidateAsync(long id);
    Task<ConditionValidationResult> ValidateRulesJsonAsync(string rulesJson);
}
```

### IRulesEngineService

```csharp
/// <summary>
/// RulesEngine evaluation service interface
/// </summary>
public interface IRulesEngineService : IScopedService
{
    /// <summary>
    /// Evaluate condition for a completed stage
    /// </summary>
    Task<ConditionEvaluationResult> EvaluateConditionAsync(
        long onboardingId, 
        long stageId);
    
    /// <summary>
    /// Build input data object for RulesEngine
    /// </summary>
    Task<RuleParameter> BuildInputDataAsync(
        long onboardingId, 
        long stageId);
}
```

### IComponentDataService

```csharp
/// <summary>
/// Component data retrieval service interface
/// </summary>
public interface IComponentDataService : IScopedService
{
    Task<ChecklistData> GetChecklistDataAsync(long onboardingId, long stageId);
    Task<QuestionnaireData> GetQuestionnaireDataAsync(long onboardingId, long stageId);
    Task<AttachmentData> GetAttachmentDataAsync(long onboardingId, long stageId);
    Task<Dictionary<string, object>> GetFieldsDataAsync(long onboardingId);
    
    /// <summary>
    /// Get available components for a stage (for condition configuration UI)
    /// </summary>
    Task<List<AvailableComponent>> GetAvailableComponentsAsync(long stageId);
    
    /// <summary>
    /// Get available fields for a component
    /// </summary>
    Task<List<AvailableField>> GetAvailableFieldsAsync(long componentId, string componentType);
}
```

### IActionExecutor

```csharp
/// <summary>
/// Condition action executor interface
/// </summary>
public interface IActionExecutor : IScopedService
{
    /// <summary>
    /// Execute all actions for a condition
    /// </summary>
    Task<ActionExecutionResult> ExecuteActionsAsync(
        string actionsJson, 
        ActionExecutionContext context);
}

/// <summary>
/// Action execution context
/// </summary>
public class ActionExecutionContext
{
    public long OnboardingId { get; set; }
    public long StageId { get; set; }
    public long ConditionId { get; set; }
    public string TenantId { get; set; }
    public long UserId { get; set; }
}
```

---

## DTO 定义

### StageConditionInputDto

```csharp
public class StageConditionInputDto
{
    [Required]
    public long StageId { get; set; }
    
    public long WorkflowId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; }
    
    [StringLength(500)]
    public string Description { get; set; }
    
    /// <summary>
    /// RulesEngine Workflow JSON
    /// </summary>
    [Required]
    public string RulesJson { get; set; }
    
    /// <summary>
    /// Actions JSON array
    /// </summary>
    [Required]
    public string ActionsJson { get; set; }
    
    public long? FallbackStageId { get; set; }
    
    public bool IsActive { get; set; } = true;
}
```

### StageConditionOutputDto

```csharp
public class StageConditionOutputDto
{
    public long Id { get; set; }
    public long StageId { get; set; }
    public long WorkflowId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string RulesJson { get; set; }
    public string ActionsJson { get; set; }
    public long? FallbackStageId { get; set; }
    public bool IsActive { get; set; }
    public string Status { get; set; }
    public DateTimeOffset CreateDate { get; set; }
    public DateTimeOffset ModifyDate { get; set; }
    
    // Parsed for convenience
    public List<ConditionRule> Rules { get; set; }
    public List<ConditionAction> Actions { get; set; }
}
```

### ConditionEvaluationResult

```csharp
public class ConditionEvaluationResult
{
    public bool IsConditionMet { get; set; }
    public List<RuleEvaluationDetail> RuleResults { get; set; }
    public string ErrorMessage { get; set; }
    public long? NextStageId { get; set; }
    public List<ActionExecutionDetail> ActionResults { get; set; }
}

public class RuleEvaluationDetail
{
    public string RuleName { get; set; }
    public bool IsSuccess { get; set; }
    public string Expression { get; set; }
    public object ActualValue { get; set; }
    public string ErrorMessage { get; set; }
}
```

---

## API 设计

### StageConditionController

| 方法 | 路由 | 描述 | 权限 |
|------|------|------|------|
| POST | `/api/stage-conditions` | 创建条件 | WorkflowWrite |
| PUT | `/api/stage-conditions/{id}` | 更新条件 | WorkflowWrite |
| DELETE | `/api/stage-conditions/{id}` | 删除条件 | WorkflowWrite |
| GET | `/api/stage-conditions/{id}` | 获取条件详情 | WorkflowRead |
| GET | `/api/stages/{stageId}/condition` | 获取 Stage 的条件 | WorkflowRead |
| GET | `/api/workflows/{workflowId}/conditions` | 获取 Workflow 所有条件 | WorkflowRead |
| POST | `/api/stage-conditions/{id}/validate` | 验证条件配置 | WorkflowRead |
| GET | `/api/stages/{stageId}/available-components` | 获取可用组件列表 | WorkflowRead |
| GET | `/api/components/{componentId}/available-fields` | 获取可用字段列表 | WorkflowRead |

### API 示例

#### 创建条件

```http
POST /api/stage-conditions
Content-Type: application/json

{
  "stageId": 12345,
  "workflowId": 67890,
  "name": "优秀候选人快速通道",
  "description": "背调评分>=90且任务全部完成时跳过面试阶段",
  "rulesJson": "[{\"WorkflowName\":\"StageCondition\",\"Rules\":[{\"RuleName\":\"HighScore\",\"Expression\":\"input.questionnaire.backgroundScore >= 90\"},{\"RuleName\":\"ChecklistDone\",\"Expression\":\"input.checklist.status == \\\"Completed\\\"\"}]}]",
  "actionsJson": "[{\"type\":\"GoToStage\",\"targetStageId\":99999,\"order\":1}]",
  "fallbackStageId": null,
  "isActive": true
}
```

#### 响应

```json
{
  "success": true,
  "data": {
    "id": "1234567890123456789",
    "stageId": "12345",
    "workflowId": "67890",
    "name": "优秀候选人快速通道",
    "status": "Valid"
  }
}
```

---

## RulesEngine 集成

### RulesEngine 配置

```csharp
public class RulesEngineServiceImpl : IRulesEngineService
{
    private readonly ReSettings _reSettings;
    
    public RulesEngineServiceImpl()
    {
        _reSettings = new ReSettings
        {
            CustomTypes = new[] { typeof(RuleUtils) }
        };
    }
    
    public async Task<ConditionEvaluationResult> EvaluateConditionAsync(
        long onboardingId, long stageId)
    {
        // 1. Load condition
        var condition = await _conditionRepository
            .GetByStageIdAsync(stageId, _userContext.TenantId);
        
        if (condition == null)
        {
            return new ConditionEvaluationResult 
            { 
                IsConditionMet = false,
                NextStageId = await GetNextStageIdAsync(stageId)
            };
        }
        
        // 2. Build input data
        var inputData = await BuildInputDataAsync(onboardingId, stageId);
        
        // 3. Execute RulesEngine
        var workflow = JsonSerializer.Deserialize<Workflow[]>(condition.RulesJson);
        var rulesEngine = new RulesEngine.RulesEngine(workflow, _reSettings);
        var results = await rulesEngine.ExecuteAllRulesAsync(
            "StageCondition", inputData);
        
        // 4. Evaluate results
        bool isConditionMet = results.All(r => r.IsSuccess);
        
        // 5. Execute actions or fallback
        if (isConditionMet)
        {
            var actionResults = await _actionExecutor.ExecuteActionsAsync(
                condition.ActionsJson, 
                new ActionExecutionContext { ... });
            return new ConditionEvaluationResult
            {
                IsConditionMet = true,
                ActionResults = actionResults.Details
            };
        }
        
        return new ConditionEvaluationResult
        {
            IsConditionMet = false,
            NextStageId = condition.FallbackStageId ?? await GetNextStageIdAsync(stageId)
        };
    }
}
```

### RuleUtils 自定义函数

```csharp
/// <summary>
/// Custom utility functions for RulesEngine expressions
/// </summary>
public static class RuleUtils
{
    public static DateTime Today() => DateTime.Today;
    
    public static int DaysBetween(DateTime start, DateTime end) 
        => (end - start).Days;
    
    public static bool IsWorkday(DateTime date) 
        => date.DayOfWeek != DayOfWeek.Saturday 
           && date.DayOfWeek != DayOfWeek.Sunday;
    
    public static bool InList(string value, params string[] list) 
        => list.Contains(value);
    
    public static bool IsEmpty(string value) 
        => string.IsNullOrWhiteSpace(value);
    
    public static bool HasValue(object value) 
        => value != null;
}
```

### 输入数据结构

```csharp
// RulesEngine input data structure
var inputData = new
{
    questionnaire = new
    {
        backgroundScore = 95,
        riskLevel = "Low",
        answers = new Dictionary<string, object>()
    },
    checklist = new
    {
        status = "Completed",
        completedCount = 5,
        totalCount = 5,
        tasks = new List<TaskStatus>()
    },
    fields = onboarding.DynamicData,  // Dictionary<string, object>
    attachments = new
    {
        fileCount = 3,
        hasAttachment = true,
        totalSize = 1024000
    }
};
```

---

## Action 执行器

### ActionExecutor 实现

```csharp
public class ActionExecutor : IActionExecutor
{
    public async Task<ActionExecutionResult> ExecuteActionsAsync(
        string actionsJson, ActionExecutionContext context)
    {
        var actions = JsonSerializer.Deserialize<List<ConditionAction>>(actionsJson);
        var results = new List<ActionExecutionDetail>();
        
        // Sort by order and execute sequentially
        foreach (var action in actions.OrderBy(a => a.Order))
        {
            try
            {
                var result = action.Type switch
                {
                    "GoToStage" => await ExecuteGoToStageAsync(action, context),
                    "SkipStage" => await ExecuteSkipStageAsync(action, context),
                    "EndWorkflow" => await ExecuteEndWorkflowAsync(action, context),
                    "SendNotification" => await ExecuteSendNotificationAsync(action, context),
                    "UpdateField" => await ExecuteUpdateFieldAsync(action, context),
                    "TriggerAction" => await ExecuteTriggerActionAsync(action, context),
                    "AssignUser" => await ExecuteAssignUserAsync(action, context),
                    _ => throw new NotSupportedException($"Action type '{action.Type}' not supported")
                };
                results.Add(result);
            }
            catch (Exception ex)
            {
                // Log error but continue with next action
                results.Add(new ActionExecutionDetail
                {
                    ActionType = action.Type,
                    Success = false,
                    ErrorMessage = ex.Message
                });
                _logger.LogError(ex, "Action execution failed: {ActionType}", action.Type);
            }
        }
        
        return new ActionExecutionResult { Details = results };
    }
}
```

---

## 日志记录

### OperationChangeLog 集成

条件评估和动作执行的日志统一记录到 `api/ow/change-logs`：

```csharp
// Condition evaluation log
await _changeLogService.LogAsync(new OperationChangeLogInputDto
{
    OperationType = "ConditionEvaluate",
    BusinessModule = "StageCondition",
    BusinessId = conditionId,
    OnboardingId = onboardingId,
    StageId = stageId,
    OperationStatus = isConditionMet ? "Success" : "NotMet",
    OperationTitle = "Condition Evaluated",
    OperationDescription = $"Condition '{conditionName}' evaluated: {(isConditionMet ? "Met" : "Not Met")}",
    ExtendedData = JsonSerializer.Serialize(new
    {
        conditionId,
        conditionName,
        result = isConditionMet,
        ruleEvaluations = ruleResults
    })
});

// Action execution log
await _changeLogService.LogAsync(new OperationChangeLogInputDto
{
    OperationType = "ConditionActionExecute",
    BusinessModule = "StageCondition",
    BusinessId = conditionId,
    OnboardingId = onboardingId,
    StageId = stageId,
    OperationStatus = actionResult.Success ? "Success" : "Failed",
    OperationTitle = "Action Executed",
    BeforeData = JsonSerializer.Serialize(beforeState),
    AfterData = JsonSerializer.Serialize(afterState),
    ChangedFields = JsonSerializer.Serialize(changedFields),
    ExtendedData = JsonSerializer.Serialize(new
    {
        actionType = action.Type,
        actionParameters = action.Parameters
    }),
    ErrorMessage = actionResult.ErrorMessage
});
```

---

## 并发控制

### 事务和行级锁

```csharp
public async Task<ConditionEvaluationResult> EvaluateConditionWithLockAsync(
    long onboardingId, long stageId)
{
    return await _db.Ado.UseTranAsync(async () =>
    {
        // Row-level lock on onboarding record
        var onboarding = await _db.Queryable<Onboarding>()
            .Where(o => o.Id == onboardingId)
            .With(SqlWith.UpdLock)  // SELECT ... FOR UPDATE
            .FirstAsync();
        
        if (onboarding == null)
            throw new BusinessException("Onboarding not found");
        
        // Check if stage is already completed
        var stageProgress = GetStageProgress(onboarding, stageId);
        if (stageProgress?.Status == "Completed")
            throw new BusinessException("Stage already completed");
        
        // Evaluate condition
        var result = await EvaluateConditionAsync(onboardingId, stageId);
        
        // Update onboarding state
        await UpdateOnboardingStateAsync(onboarding, result);
        
        // Log changes (within transaction)
        await LogConditionEvaluationAsync(onboarding, stageId, result);
        
        return result;
    });
}
```

---

## 错误处理

### 错误码定义

| 错误码 | 描述 |
|--------|------|
| `CONDITION_NOT_FOUND` | 条件配置不存在 |
| `INVALID_RULES_JSON` | RulesJson 格式无效 |
| `INVALID_ACTIONS_JSON` | ActionsJson 格式无效 |
| `STAGE_NOT_FOUND` | 引用的 Stage 不存在 |
| `ACTION_NOT_FOUND` | 引用的 ActionDefinition 不存在 |
| `DUPLICATE_CONDITION` | Stage 已存在条件配置 |
| `CIRCULAR_REFERENCE` | 检测到循环引用 |
| `EVALUATION_ERROR` | 条件评估执行错误 |
| `ACTION_EXECUTION_ERROR` | 动作执行错误 |

### 容错策略

```csharp
public async Task<ConditionEvaluationResult> EvaluateConditionSafeAsync(
    long onboardingId, long stageId)
{
    try
    {
        return await EvaluateConditionAsync(onboardingId, stageId);
    }
    catch (JsonException ex)
    {
        _logger.LogWarning(ex, "Invalid RulesJson format for stage {StageId}", stageId);
        // Mark condition as invalid and proceed to fallback
        await MarkConditionInvalidAsync(stageId);
        return CreateFallbackResult(stageId);
    }
    catch (RulesEngineException ex)
    {
        _logger.LogWarning(ex, "RulesEngine evaluation error for stage {StageId}", stageId);
        return CreateFallbackResult(stageId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error evaluating condition for stage {StageId}", stageId);
        return CreateFallbackResult(stageId);
    }
}

private ConditionEvaluationResult CreateFallbackResult(long stageId)
{
    return new ConditionEvaluationResult
    {
        IsConditionMet = false,
        NextStageId = GetNextStageIdSync(stageId),
        ErrorMessage = "Condition evaluation failed, proceeding to fallback stage"
    };
}
```

---

## 数据库设计

### ff_stage_condition 表

```sql
CREATE TABLE ff_stage_condition (
    id BIGINT PRIMARY KEY,
    stage_id BIGINT NOT NULL,
    workflow_id BIGINT NOT NULL,
    name VARCHAR(100) NOT NULL,
    description VARCHAR(500),
    rules_json JSONB NOT NULL,
    actions_json JSONB NOT NULL,
    fallback_stage_id BIGINT,
    is_active BOOLEAN DEFAULT TRUE,
    status VARCHAR(20) DEFAULT 'Valid',
    tenant_id VARCHAR(32) NOT NULL DEFAULT 'DEFAULT',
    app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT',
    is_valid BOOLEAN DEFAULT TRUE,
    create_date TIMESTAMPTZ DEFAULT NOW(),
    modify_date TIMESTAMPTZ DEFAULT NOW(),
    create_by VARCHAR(50) DEFAULT 'SYSTEM',
    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
    create_user_id BIGINT DEFAULT 0,
    modify_user_id BIGINT DEFAULT 0,
    
    CONSTRAINT fk_stage FOREIGN KEY (stage_id) REFERENCES ff_stage(id),
    CONSTRAINT fk_workflow FOREIGN KEY (workflow_id) REFERENCES ff_workflow(id),
    CONSTRAINT fk_fallback_stage FOREIGN KEY (fallback_stage_id) REFERENCES ff_stage(id),
    CONSTRAINT uq_stage_condition UNIQUE (stage_id, tenant_id)  -- One condition per stage
);

CREATE INDEX idx_stage_condition_stage ON ff_stage_condition(stage_id);
CREATE INDEX idx_stage_condition_workflow ON ff_stage_condition(workflow_id);
CREATE INDEX idx_stage_condition_tenant ON ff_stage_condition(tenant_id);
```

---

## 测试策略

### 单元测试

1. **RulesEngineService 测试**
   - 测试各种表达式评估（比较、字符串、空值、数学运算）
   - 测试嵌套规则（AND/OR 组合）
   - 测试自定义函数调用
   - 测试无效 JSON 处理

2. **ActionExecutor 测试**
   - 测试各种动作类型执行
   - 测试动作顺序执行
   - 测试动作失败后继续执行
   - 测试事务回滚

3. **ComponentDataService 测试**
   - 测试各组件数据获取
   - 测试数据格式转换
   - 测试空数据处理

### 集成测试

1. **完整流程测试**
   - Stage 完成 → 条件评估 → 动作执行 → 状态更新 → 日志记录

2. **并发测试**
   - 多用户同时完成同一 Stage
   - 验证行级锁正确工作

3. **边界条件测试**
   - 无条件配置的 Stage
   - 条件为 Invalid 状态
   - 引用的 Stage/Action 被删除

---

## 正确性属性

1. **每个 Stage 最多一个 Condition** - 通过数据库唯一约束保证
2. **条件评估原子性** - 通过数据库事务保证
3. **并发安全** - 通过行级锁防止并发修改
4. **租户隔离** - 所有查询和操作都包含 TenantId 过滤
5. **日志完整性** - 所有评估和执行都记录到 OperationChangeLog
6. **容错性** - 评估失败时自动进入 Fallback Stage
