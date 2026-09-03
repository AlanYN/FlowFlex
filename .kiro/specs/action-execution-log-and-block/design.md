# Design Document

## Feature: action-execution-log-and-block

---

## Overview

本功能为 FlowFlex 的 Python Script Action 系统增加两项能力：

1. **执行日志**：Python 脚本执行的业务结果（如"Tax ID 重复，跳过创建"）写入 Serilog 日志，并通过 Change Log 展示面板呈现给业务用户。
2. **主动阻断**：脚本返回 `success: false, shouldBlock: true` 时，C# 层主动阻断 Stage Complete，无需依赖 Python `raise Exception`。

这是一次**纯后端改造**，无前端变更，无数据库 Migration。所有改动集中在 3 个 C# 文件和 1 个 Helper 类。

---

## Architecture

### 整体执行链路（改造后）

```
用户点击 Complete Stage
    │
    ▼
OnboardingStageManagementService.CompleteCurrentStageAsync()
    │
    ▼
EvaluateAndExecuteStageConditionAsync()
    │
    ▼
ConditionActionExecutor.ExecuteActionsAsync()           [ActionExecutor.cs]
    │  foreach action in order:
    │    ExecuteActionAsync()
    │    │
    │    ▼ (type == TriggerAction)
    │  ExecuteTriggerActionAsync()
    │    │
    │    ▼
    │  ActionExecutionService.ExecuteActionAsync()      [ActionExecutionService.cs]
    │    │
    │    ▼
    │  PythonActionExecutor.ExecuteAsync()              [PythonActionExecutor.cs]
    │    │  1. CreateRunnerScript() → print(json.dumps(output_obj))
    │    │  2. Judge0 执行
    │    │  3. CreateSuccessResult() → 解析 stdout 末行 JSON
    │    │     或
    │    │  3. CreateErrorResult()  → shouldBlock = true
    │    │
    │    ▼ 返回 JToken result
    │  写入 ff_action_executions.execution_output
    │  写业务 Serilog 日志（新增）
    │    │
    │    ▼ 返回 execution.ExecutionOutput 给 ConditionActionExecutor
    │  CheckForBusinessError()                         [ActionExecutor.cs 改造]
    │    │  检测 Python 结构 (success + shouldBlock)
    │    │  或检测 HTTP API 结构 (response 字段)
    │    │
    │    ├── 有业务错误 → result.Success = false, break（TriggerAction 立刻中断）
    │    └── 无业务错误 → 提取 message 写入 ResultData["businessMessage"]
    │
    ▼
ConditionLogHelper.GetActionResultDetail()             [ConditionLogHelper.cs 改造]
    → triggeraction 分支 → GetTriggerActionDetail()
    → "actionName: businessMessage" 格式化
    → 写入 Change Log
```

### 关键设计决策

| 决策 | 选择 | 理由 |
|---|---|---|
| stdout 解析位置 | `PythonActionExecutor.CreateSuccessResult()` | 贴近 Judge0 结果处理，隔离 Python-specific 逻辑 |
| shouldBlock 默认值 | `success:false` 且 `shouldBlock` 缺失时默认 `true` | 安全优先：业务失败且未明确声明不阻断时，默认阻断 |
| businessMessage 传递 | 内存 `ResultData` 字典 | 无需改数据库结构；`ConditionLogHelper` 已有此读取模式 |
| Python 检测与 HTTP API 检测顺序 | Python 检测在前 | Python 结果无 `response` 字段，不会误触发 HTTP API 检测 |
| Action Chain 中断策略 | 仅 TriggerAction 失败立刻中断 | 其他类型 Action 失败不中断，保持原有语义 |

---

## Components and Interfaces

### 1. PythonActionExecutor（`Application/Services/Action/Executors/PythonActionExecutor.cs`）

**改动方法：**

#### `CreateRunnerScript()`
将末尾 `print(output_obj)` 改为：
```python
print(json.dumps(output_obj) if output_obj is not None else 'null')
```
`import json` 已在脚本开头注入（当前代码已有），确保合法 JSON 输出。

#### `CreateSuccessResult(Judge0SubmissionResultDto judge0Result)`
新增 stdout 解析逻辑。从 stdout 最后一行尝试解析业务结果：

```csharp
// 返回结构新增字段：
{
    success     // bool：从脚本返回值解析，默认 true
    shouldBlock // bool：从脚本返回值解析，默认 false（success=false 时默认 true）
    message     // string：业务消息，供 Change Log 展示
    data        // JToken?：可选，供后续 Action 链 prev_ 注入使用
    stdout, stderr, executionTime, memoryUsage, status, token, timestamp  // 原有字段保留
}
```

**解析规则：**
- 取 stdout trim 后按 `\n` 分割的最后一行
- 若该行以 `{` 或 `[` 开头，`JToken.Parse` 解析
- 解析失败则静默忽略，保持 `success=true, shouldBlock=false` 默认值

#### `CreateErrorResult(Judge0SubmissionResultDto?, string?)` 和 `CreateErrorResult(string)`
两个重载均增加 `shouldBlock = true`：
- Judge0 status >= 4（运行时错误/超时）→ `shouldBlock = true`
- 配置/参数错误导致脚本无法提交 → `shouldBlock = true`

---

### 2. ActionExecutionService（`Application/Services/Action/ActionExecutionService.cs`）

**改动方法：`ExecuteActionAsync()`**

在 `await _actionExecutionRepository.UpdateAsync(execution)` 之后，增加业务日志写入：

```csharp
var businessMsg = execution.ExecutionOutput?["message"]?.ToString();
var scriptSuccess = execution.ExecutionOutput?["success"]?.Value<bool>() ?? true;
var shouldBlock = execution.ExecutionOutput?["shouldBlock"]?.Value<bool>() ?? false;

if (!string.IsNullOrEmpty(businessMsg))
{
    if (scriptSuccess)
        _logger.LogInformation("Action business result: ActionId={}, ActionName={}, Message={}", ...);
    else
        _logger.LogWarning("Action business failure: ActionId={}, ActionName={}, Message={}, ShouldBlock={}", ...);
}
```

`message` 为空时不写日志，避免空日志噪声。

---

### 3. ConditionActionExecutor（`Application/Services/OW/StageCondition/ActionExecutor.cs`）

**改动方法 A：`CheckForBusinessError(JToken, string)`**

在原有 HTTP API 检测逻辑之前，增加 Python Action 检测分支：

```
Python 检测（新增）：
  if executionResult["response"] == null AND executionResult["success"] == false AND executionResult["shouldBlock"] == true
  → 返回 "[actionName] message"

HTTP API 检测（保持不变）：
  if executionResult["response"] != null
  → 原有 JObject.Parse(responseStr) 检测 success/code/statusCode
```

互斥判断依据：Python 结果有 `success` 字段但无 `response` 字段；HTTP API 结果有 `response` 字段。两者不会相互干扰。

**改动方法 B：`ExecuteTriggerActionAsync()`**

在 `CheckForBusinessError` 返回 null（成功）后，新增提取 businessMessage：

```csharp
var businessMsg = executionResult?["message"]?.ToString();
if (!string.IsNullOrEmpty(businessMsg))
{
    result.ResultData["businessMessage"] = businessMsg;
}
```

原有 `ResultData` 字段（`actionDefinitionId`、`actionName`、`status`、`executionResult`）保持不变。

**改动方法 C：`ExecuteActionsAsync()`**

在 Action 失败处理中，增加 TriggerAction 立刻中断逻辑：

```csharp
if (!actionResult.Success)
{
    _logger.LogWarning(...);
    if (action.Type?.Equals("triggeraction", StringComparison.OrdinalIgnoreCase) == true)
    {
        result.Success = false;
        break;  // 立刻中断，已执行结果保留在 result.Details
    }
    // 其他类型继续执行（原有行为）
}
```

**改动方法 D：`AccumulatePrevFields(JToken, Dictionary<string, object>)`**

在原有 HTTP API 路径之前，增加 Python Action 路径：

```csharp
// Python Action 路径：没有 response 字段，直接从顶层 data 提取
if (actionResult?["response"] == null && actionResult?["data"] is JObject pythonDataObj)
{
    foreach (var prop in pythonDataObj.Properties())
    {
        if (prop.Value.Type == JTokenType.Null) continue;
        accumulated[$"prev_{prop.Name}"] = (Object or Array) ? prop.Value.ToString() : prop.Value.ToObject<object>();
    }
    return;  // Python 路径处理完毕，不走 HTTP API 路径
}
// 原有 HTTP API 路径（保持不变）
```

---

### 4. ConditionLogHelper（`Application/Helpers/ConditionLogHelper.cs`）

**改动方法：`GetActionResultDetail()`**

`triggeraction` 分支由直接读 `actionName` 改为调用新方法：

```csharp
"triggeraction" => GetTriggerActionDetail(action.ResultData),
```

**新增私有方法：`GetTriggerActionDetail()`**

```csharp
private static string GetTriggerActionDetail(Dictionary<string, object> resultData)
{
    var actionName = GetResultDataString(resultData, "actionName") ?? "";
    var businessMessage = GetResultDataString(resultData, "businessMessage") ?? "";

    if (!string.IsNullOrEmpty(businessMessage))
        return string.IsNullOrEmpty(actionName) ? businessMessage : $"{actionName}: {businessMessage}";

    return actionName;
}
```

**展示效果变化：**

| 场景 | 改造前 | 改造后 |
|---|---|---|
| 成功，有业务消息 | `TriggerAction(create customer full)` | `TriggerAction(create customer full: Customer created: CUST001)` |
| 失败，有业务消息 | `TriggerAction(create customer full)` | `TriggerAction(create customer full: Tax ID already exists)` |
| 无业务消息（旧脚本） | `TriggerAction(create customer full)` | `TriggerAction(create customer full)`（不变） |

---

## Data Models

### Python 脚本约定的返回值结构

改造后，Python 脚本 `main()` 函数应返回以下结构化 dict：

```python
# 成功，有业务数据
return {
    "success": True,
    "message": "Customer created: CUST001",   # 写入 Change Log
    "data": { "customerCode": "CUST001" }     # 可选，供 prev_ 链式传递
}

# 业务失败，阻断 Stage
return {
    "success": False,
    "shouldBlock": True,                      # 明确声明阻断
    "message": "Tax ID already exists"
}

# 业务跳过，不阻断 Stage
return {
    "success": False,
    "shouldBlock": False,                     # 明确声明不阻断
    "message": "Address creation skipped: all address fields empty"
}
```

**shouldBlock 默认值规则：**

| 脚本返回 | shouldBlock 推断值 | Stage 结果 |
|---|---|---|
| `{"success": true, ...}` | false | Stage Complete |
| `{"success": false, "shouldBlock": true}` | true | 阻断 |
| `{"success": false, "shouldBlock": false}` | false | Stage Complete |
| `{"success": false}`（缺 shouldBlock） | **true**（安全优先） | 阻断 |
| stdout 无合法 JSON（旧脚本） | false（保持默认） | Stage Complete |
| Judge0 status >= 4 | true（自动） | 阻断 |

### ActionExecutionDetail.ResultData 变更

```
改造前：
{
    "actionDefinitionId": long,
    "actionName":         string,
    "status":             string,
    "executionResult":    JToken
}

改造后（新增可选字段）：
{
    "actionDefinitionId": long,
    "actionName":         string,
    "status":             string,
    "executionResult":    JToken,
    "businessMessage":    string?   // 仅当脚本返回 message 时存在
}
```

### ff_action_executions.execution_output 字段变化

无 Migration 需求。execution_output 是 JSONB 列，新旧格式兼容。

```json
// 改造后 Python Action 成功写入示例：
{
    "success":       true,
    "shouldBlock":   false,
    "message":       "Customer created: CUST001",
    "data":          { "customerCode": "CUST001" },
    "stdout":        "...",
    "stderr":        null,
    "executionTime": "0.123",
    "memoryUsage":   5120,
    "status":        "Accepted",
    "token":         "abc123",
    "timestamp":     "2024-01-01T00:00:00+00:00"
}

// 旧脚本输出（无结构化 JSON）：
{
    "success":       true,
    "shouldBlock":   false,
    "message":       "Python script executed successfully",
    "data":          null,
    "stdout":        "hello\n",
    ...
}
```

---

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

本功能的核心逻辑属于**数据转换和条件分支**，适合属性测试。Python Action 输出的解析（Requirement 2）、shouldBlock 推断（Requirement 3, 5）、businessMessage 传递（Requirement 7, 8）均有清晰的输入/输出关系，可以通过生成随机输入验证。

### Property 1: stdout JSON 解析保持向后兼容

*For any* Python 脚本 stdout，若最后一行不是合法 JSON（不以 `{` 或 `[` 开头），则 `CreateSuccessResult` 解析后 `success` 必须为 `true`，`shouldBlock` 必须为 `false`。

**Validates: Requirements 10.1, 2.4**

### Property 2: shouldBlock 安全优先推断

*For any* 包含 `"success": false` 且**不含** `"shouldBlock"` 字段的合法 JSON stdout，`CreateSuccessResult` 解析后 `shouldBlock` 必须为 `true`。

**Validates: Requirements 2.3, 5.1**

### Property 3: shouldBlock 显式声明优先

*For any* 包含 `"success": false, "shouldBlock": false` 的合法 JSON stdout，`CreateSuccessResult` 解析后 `shouldBlock` 必须为 `false`（即显式声明的 `shouldBlock:false` 优先于安全优先推断）。

**Validates: Requirements 2.2, 10.1**

### Property 4: businessMessage 无空值注入

*For any* `ResultData`，若 `businessMessage` 键存在，其值必须为非空字符串；`GetTriggerActionDetail` 永远不向 `ResultData` 写入空字符串的 `businessMessage`。

**Validates: Requirements 7.2, 8.1**

### Property 5: Change Log 格式完整性

*For any* 包含非空 `actionName` 和非空 `businessMessage` 的 `ResultData`，`GetTriggerActionDetail` 的返回值必须同时包含 `actionName` 和 `businessMessage`，且以 `: ` 分隔。

**Validates: Requirements 8.1**

### Property 6: Python/HTTP 检测路径互斥

*For any* 包含 `"response"` 字段的执行结果，`CheckForBusinessError` 必须走 HTTP API 路径，而不触发 Python 检测分支（即结果有 `"response"` 字段时，`success` 和 `shouldBlock` 字段不被用于阻断判断）。

**Validates: Requirements 5.4, 9.3, 10.3**

### Property 7: prev_ 字段提取不覆盖已有字段

*For any* `accumulatedPrevFields` 字典，`AccumulatePrevFields` 在 HTTP API 路径下不得用后续 `responseObj` 顶层属性覆盖已有的 `prev_xxx` 键（先到先得，`!accumulated.ContainsKey(key)` 条件保证）。

**Validates: Requirements 9.1, 9.4**

---

## Error Handling

### PythonActionExecutor 错误处理矩阵

| 错误场景 | 处理方式 | shouldBlock | Stage 结果 |
|---|---|---|---|
| stdout 解析失败（`JToken.Parse` 抛异常） | catch 静默忽略，保持默认值 | false | Complete |
| stdout 最后一行不是 JSON | 不尝试解析，保持默认值 | false | Complete |
| Judge0 status >= 4 | `CreateErrorResult()` 返回 `shouldBlock=true` | true | 阻断 |
| `ParseConfig` 反序列化失败 | `CreateErrorResult("Invalid configuration format")` | true | 阻断 |
| `ProcessTriggerContextData` 参数缺失 | 抛 `InvalidOperationException`，外层 catch 捕获 | true | 阻断（原有异常路径） |
| Python `raise Exception` | Judge0 status >= 4，走 `CreateErrorResult` | true | 阻断 |

### ConditionActionExecutor 错误处理

| 错误场景 | 处理方式 |
|---|---|
| `CheckForBusinessError` 内部抛异常 | catch → `_logger.LogDebug` → 返回 null（不阻断） |
| `AccumulatePrevFields` 内部抛异常 | catch 静默忽略，不影响后续 Action |
| TriggerAction 失败 | `result.Success = false; break;` 立刻中断 Action Chain |
| 其他 Action 类型失败 | `LogWarning` 后继续执行（原有行为不变） |

### ActionExecutionService 错误处理

Serilog 日志写入是纯内存操作，不会抛异常，无需额外 try/catch。仅在 `businessMsg` 非空时写入，避免空日志噪声。

---

## Testing Strategy

### 单元测试覆盖（Backend，xUnit + Moq + FluentAssertions）

**PythonActionExecutorTests**

测试 `CreateSuccessResult` 的 stdout 解析逻辑（通过反射或提取为 internal 方法测试）：

```
ParseSuccessResult_WhenLastLineIsValidJson_ShouldExtractSuccessAndMessage
ParseSuccessResult_WhenSuccessIsFalseAndShouldBlockMissing_ShouldDefaultShouldBlockToTrue
ParseSuccessResult_WhenSuccessIsFalseAndShouldBlockFalse_ShouldRespectExplicitFalse
ParseSuccessResult_WhenLastLineIsNotJson_ShouldDefaultToSuccessTrueNoBlock
ParseSuccessResult_WhenStdoutIsEmpty_ShouldReturnDefaults
```

测试 `CreateErrorResult`：
```
CreateErrorResult_WithJudge0Result_ShouldAlwaysSetShouldBlockTrue
CreateErrorResult_WithStringMessage_ShouldAlwaysSetShouldBlockTrue
```

测试 `CreateRunnerScript`（验证输出的 Python 脚本内容）：
```
CreateRunnerScript_ShouldUseJsonDumps_NotDirectPrint
CreateRunnerScript_WhenNoParams_ShouldHandleNullOutput
```

**ConditionActionExecutorTests（CheckForBusinessError）**

```
CheckForBusinessError_WhenPythonSuccessFalseAndShouldBlockTrue_ShouldReturnError
CheckForBusinessError_WhenPythonSuccessFalseAndShouldBlockFalse_ShouldReturnNull
CheckForBusinessError_WhenHasResponseField_ShouldUseHttpApiPath
CheckForBusinessError_WhenResponseFieldPresentAndSuccessFalse_ShouldNotTriggerPythonPath
CheckForBusinessError_WhenNullResult_ShouldReturnNull
CheckForBusinessError_WhenParseThrows_ShouldReturnNull
```

**ConditionLogHelperTests**

```
GetTriggerActionDetail_WhenBothActionNameAndBusinessMessage_ShouldFormatWithColon
GetTriggerActionDetail_WhenOnlyActionName_ShouldReturnActionName
GetTriggerActionDetail_WhenOnlyBusinessMessage_ShouldReturnMessageOnly
GetTriggerActionDetail_WhenBusinessMessageIsEmpty_ShouldReturnActionName
GetTriggerActionDetail_WhenBothEmpty_ShouldReturnEmpty
```

### 属性测试（配合 FsCheck 或 CsCheck for .NET）

使用属性测试覆盖 Correctness Properties 章节定义的 7 条属性。每条属性最少运行 100 次迭代。

**示例（Property 1 和 2）：**

```csharp
// Feature: action-execution-log-and-block, Property 1: stdout JSON 解析保持向后兼容
[Property(Arbitrary = new[] { typeof(NonJsonLastLineArb) })]
public bool NonJsonStdout_AlwaysDefaultsToSuccessTrue(string stdout)
{
    var result = ParseSuccessResult(stdout);
    return (bool)result.GetProperty("success") == true
        && (bool)result.GetProperty("shouldBlock") == false;
}

// Feature: action-execution-log-and-block, Property 2: shouldBlock 安全优先推断
[Property]
public bool SuccessFalseWithoutShouldBlock_DefaultsToShouldBlockTrue(string message)
{
    var stdout = $"{{\"success\": false, \"message\": \"{message}\"}}";
    var result = ParseSuccessResult(stdout);
    return (bool)result.GetProperty("shouldBlock") == true;
}
```

### 集成测试建议

由于涉及 Judge0 外部调用和 PostgreSQL 写入，集成测试使用示例（1~3 个代表性用例），不使用属性测试：

```
Integration_PythonAction_SuccessResult_WritesBusinessMessageToChangeLog
Integration_PythonAction_ShouldBlockTrue_AbortsStageComplete
Integration_PythonAction_OldScript_NoStructuredReturn_StageCompletes
```

### 测试框架

- 属性测试库：[FsCheck.Xunit](https://fscheck.github.io/FsCheck/) 或 [CsCheck](https://github.com/AnthonyLloyd/CsCheck)
- 单元测试：xUnit + Moq + FluentAssertions（项目已有）
- 属性测试最少迭代次数：100 次/属性
- 每条属性测试注释格式：`// Feature: action-execution-log-and-block, Property {N}: {property_text}`
