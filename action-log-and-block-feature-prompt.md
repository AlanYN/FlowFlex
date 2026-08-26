# Action 执行日志 & 主动阻断 Stage 功能改造

> 本文档是经过代码验证和决策确认后的最终改造方案，供开发直接执行。
> 原始草稿中存在若干与实际代码不符的描述，已在本版本中全部修正。

---

## 一、需求背景

当 Python Script Action 执行时（例如调用 CRM 创建 Customer），需要满足以下两个诉求：

1. **日志记录**：脚本执行的业务结果（如"Tax ID 重复，跳过创建"）需要被记录到 case 的 change log，方便事后排查。
2. **主动阻断**：脚本内部检测到业务异常时（如 Tax ID 重复），需要能够**主动阻断 Stage Complete**，而不仅依赖 Python `raise Exception`。

---

## 二、现状分析（已验证）

### 2.1 执行链路

```
用户点击 Complete Stage
    ↓
OnboardingStageManagementService.CompleteCurrentStageAsync()
    ↓
EvaluateAndExecuteStageConditionAsync()
    ↓ 调用 RulesEngineService.EvaluateAndExecuteWithTransactionAsync()
    ↓
ConditionActionExecutor.ExecuteActionsAsync()  ← ActionExecutor.cs
    ↓ 循环每个 action，调用 ExecuteActionAsync()
    ↓
ExecuteTriggerActionAsync()
    ↓
ActionExecutionService.ExecuteActionAsync()
    ↓
PythonActionExecutor.ExecuteAsync()
    ↓
Judge0 沙箱执行 Python 代码
    ↓
结果返回，存入 ff_action_executions 表
```

### 2.2 Python 脚本输出的实际格式

- Judge0 返回的 `stdout` 由 `IdeClient` **自动 base64 decode**，C# 拿到的是原始文本字符串
- 当前 `CreateRunnerScript` 末尾生成的是 `print(output_obj)`，Python `print(dict)` 输出的是 **Python 格式**（`True`/`False`/`None`，单引号），**不是 JSON**，C# 端无法直接 `JToken.Parse`
- **必须改为 `print(json.dumps(output_obj))`** 才能让 C# 解析

### 2.3 日志问题根因

`ActionExecutionService.ExecuteActionAsync` 执行完后将结果存入 `ff_action_executions.execution_output`（JSONB），数据已在库里，但：

- Python 脚本返回的业务 `message` 没有被提取写入 Serilog 日志
- 没有进入 case 的 change log 展示链路（`ConditionLogHelper` 只从 `ActionExecutionDetail.ResultData` 读数据，而 `message` 存在 `execution_output` 里，两个来源不通）

### 2.4 阻断问题根因（已验证，原草稿有误）

**问题一：Python 返回 `success: false` 不会阻断**

`CheckForBusinessError`（ActionExecutor.cs ~1679行）只检测 **HTTP API Action** 的 `response` 字段：

```csharp
var responseStr = executionResult["response"]?.ToString(); // Python 结果没有 response 字段
```

Python Action 返回的结构是 `{success, message, stdout, ...}`，永远不会被这个方法检测到。

**问题二：Judge0 status >= 4 也不会阻断（原草稿描述有误）**

`PythonActionExecutor` 在 Judge0 status >= 4 时调用 `CreateErrorResult` 返回 `{ success: false, ... }` 的 object，**不抛异常**。`ActionExecutionService` 收到后走正常成功路径，`ExecutionStatus` 写 `"Completed"`，`CheckForBusinessError` 因为 Python 结构不匹配也返回 null，Stage 照样 Complete。

**现有的唯一阻断方式**：Python 脚本 `raise Exception` → Judge0 非 0 退出码 → `status.id >= 4` → `PythonActionExecutor.ExecuteAsync` 内部 catch 到异常 → 向上抛 → `ActionExecutionService` catch → `ExecutionStatus` 写 `"Failed"` → 继续向上抛 → `ExecuteTriggerActionAsync` catch → `ActionExecutionDetail.Success = false` → `EvaluateAndExecuteStageConditionAsync` 收集 failedActions → 抛 `CRMException` → Stage 被阻断。

**注意**：`CreateErrorResult` 返回 object 的路径（不抛异常）实际上不走上面的流程，不能阻断。

### 2.5 Change Log 展示链路

截图中 `TriggerAction(create customer full)` 的生成路径：

```
ConditionLogHelper.GetActionResultDetail()
    → triggeraction 分支
    → 读 ResultData["actionName"]
    → 格式化为 "TriggerAction(create customer full)"
```

`ResultData` 是 `ActionExecutionDetail` 上的内存字典，在 `ExecuteTriggerActionAsync` 里填充。业务 `message` 目前不在 `ResultData` 里，所以不会出现在 change log UI 中。

---

## 三、涉及的关键文件

| 文件                                                            | 关键位置                      | 改动                                 |
| --------------------------------------------------------------- | ----------------------------- | ------------------------------------ |
| `Application/Services/Action/Executors/PythonActionExecutor.cs` | `CreateRunnerScript()`        | 改 print 为 json.dumps               |
| `Application/Services/Action/Executors/PythonActionExecutor.cs` | `CreateSuccessResult()`       | 解析 stdout 中的业务结果             |
| `Application/Services/Action/Executors/PythonActionExecutor.cs` | `CreateErrorResult()`         | 加 shouldBlock = true                |
| `Application/Services/Action/ActionExecutionService.cs`         | `ExecuteActionAsync()`        | 执行完后写 Serilog 业务日志          |
| `Application/Services/OW/StageCondition/ActionExecutor.cs`      | `CheckForBusinessError()`     | 增加 Python Action 结果检测          |
| `Application/Services/OW/StageCondition/ActionExecutor.cs`      | `ExecuteTriggerActionAsync()` | 提取 message 写入 ResultData         |
| `Application/Services/OW/StageCondition/ActionExecutor.cs`      | `ExecuteActionsAsync()`       | shouldBlock 时立刻中断 action 链     |
| `Application/Helpers/ConditionLogHelper.cs`                     | `GetActionResultDetail()`     | triggeraction 分支加 businessMessage |

---

## 四、改造方案

### 改动点 1：`CreateRunnerScript` — 强制 JSON 输出

**文件**：`PythonActionExecutor.cs`

```csharp
private string CreateRunnerScript(string userSourceCode, List<string> parameterValues)
{
    var scriptBuilder = new System.Text.StringBuilder();

    scriptBuilder.AppendLine("import json");
    scriptBuilder.AppendLine();

    scriptBuilder.AppendLine("# declare main function");
    scriptBuilder.AppendLine(userSourceCode);
    scriptBuilder.AppendLine();

    scriptBuilder.AppendLine("# parse parameters");
    for (int i = 0; i < parameterValues.Count; i++)
    {
        scriptBuilder.AppendLine($"param_{i} = {parameterValues[i]}");
    }
    scriptBuilder.AppendLine();

    scriptBuilder.AppendLine("# execute main function");
    var parameterNames = string.Join(", ", Enumerable.Range(0, parameterValues.Count).Select(i => $"param_{i}"));
    scriptBuilder.AppendLine($"output_obj = main({parameterNames})");
    // 改动：print(json.dumps(...)) 确保输出合法 JSON，而不是 Python repr 格式
    scriptBuilder.AppendLine("print(json.dumps(output_obj) if output_obj is not None else 'null')");

    return scriptBuilder.ToString();
}
```

### 改动点 2：`CreateSuccessResult` — 解析 stdout 中的业务结果

**文件**：`PythonActionExecutor.cs`

Judge0 status == 3 时，从 stdout 最后一行解析业务结果：

```csharp
private object CreateSuccessResult(Judge0SubmissionResultDto judge0Result)
{
    var stdout = judge0Result.Stdout ?? "";
    bool scriptSuccess = true;
    bool shouldBlock = false;
    string businessMessage = "Python script executed successfully";
    JToken? scriptData = null;

    try
    {
        // stdout 已由 IdeClient 完成 base64 decode，直接解析最后一行 JSON
        var lastLine = stdout.Trim().Split('\n').Last().Trim();
        if (lastLine.StartsWith("{") || lastLine.StartsWith("["))
        {
            var parsed = JToken.Parse(lastLine);
            scriptSuccess = parsed["success"]?.Value<bool>() ?? true;
            // shouldBlock 默认：success=false 时默认阻断（安全优先）
            shouldBlock = parsed["shouldBlock"]?.Value<bool>() ?? !scriptSuccess;
            businessMessage = parsed["message"]?.ToString() ?? businessMessage;
            scriptData = parsed["data"];
        }
    }
    catch { /* 解析失败：脚本没有返回结构化 JSON，当作普通脚本处理，保持默认值 */ }

    return new
    {
        success = scriptSuccess,
        shouldBlock = shouldBlock,
        message = businessMessage,
        stdout = stdout,
        stderr = judge0Result.Stderr,
        executionTime = judge0Result.Time,
        memoryUsage = judge0Result.Memory,
        status = judge0Result.Status?.Description,
        token = judge0Result.Token,
        data = scriptData,
        timestamp = DateTimeOffset.UtcNow
    };
}
```

### 改动点 3：`CreateErrorResult` — Judge0 崩溃时自动标记 shouldBlock

**文件**：`PythonActionExecutor.cs`

Judge0 status >= 4（运行时错误/超时）时，自动视为 `shouldBlock = true`：

```csharp
private object CreateErrorResult(Judge0SubmissionResultDto? judge0Result = null, string? customMessage = null)
{
    var message = customMessage ?? judge0Result?.Status?.Description ?? "Execution failed";

    return new
    {
        success = false,
        shouldBlock = true,   // Judge0 执行失败，自动阻断 Stage
        message,
        stdout = judge0Result?.Stdout,
        stderr = judge0Result?.Stderr,
        executionTime = judge0Result?.Time,
        memoryUsage = judge0Result?.Memory,
        status = judge0Result?.Status?.Description,
        token = judge0Result?.Token,
        errorDetails = judge0Result?.Message ?? message,
        timestamp = DateTimeOffset.UtcNow
    };
}

private object CreateErrorResult(string message)
{
    return new
    {
        success = false,
        shouldBlock = true,   // 配置/参数错误，自动阻断 Stage
        message,
        errorDetails = message,
        timestamp = DateTimeOffset.UtcNow
    };
}
```

### 改动点 4：`ActionExecutionService.ExecuteActionAsync` — 写业务 Serilog 日志

**文件**：`ActionExecutionService.cs`

在 `execution.ExecutionOutput = ...` 之后添加：

```csharp
execution.ExecutionStatus = ActionExecutionStatusEnum.Completed.ToString();
execution.CompletedAt = DateTime.UtcNow;
execution.ExecutionOutput = result != null ? SafeCreateJToken(result) : new JObject();
execution.InitUpdateInfo(_userContext);
await _actionExecutionRepository.UpdateAsync(execution);

// === 新增：写业务日志到 Serilog ===
var businessMsg = execution.ExecutionOutput?["message"]?.ToString();
var scriptSuccess = execution.ExecutionOutput?["success"]?.Value<bool>() ?? true;
var shouldBlock = execution.ExecutionOutput?["shouldBlock"]?.Value<bool>() ?? false;

if (!string.IsNullOrEmpty(businessMsg))
{
    if (scriptSuccess)
    {
        _logger.LogInformation(
            "Action business result: ActionId={ActionId}, ActionName={ActionName}, Message={Message}",
            actionDefinitionId, execution.ActionName, businessMsg);
    }
    else
    {
        _logger.LogWarning(
            "Action business failure: ActionId={ActionId}, ActionName={ActionName}, Message={Message}, ShouldBlock={ShouldBlock}",
            actionDefinitionId, execution.ActionName, businessMsg, shouldBlock);
    }
}
// === 新增结束 ===
```

### 改动点 5：`CheckForBusinessError` — 增加 Python Action 结果检测

**文件**：`ActionExecutor.cs`（`ConditionActionExecutor`）

在方法开头，原有 HTTP API 检测逻辑之前加：

```csharp
private string? CheckForBusinessError(JToken executionResult, string actionName)
{
    if (executionResult == null) return null;

    try
    {
        // === 新增：Python Action 业务错误检测 ===
        // Python Action 返回结构：{ success, shouldBlock, message, stdout, ... }
        // 注意：此检测也覆盖 Judge0 status >= 4 的场景（CreateErrorResult 已设 shouldBlock=true）
        var scriptSuccess = executionResult["success"]?.Value<bool>();
        var shouldBlock = executionResult["shouldBlock"]?.Value<bool>();

        if (scriptSuccess == false && shouldBlock == true)
        {
            var msg = executionResult["message"]?.ToString()
                   ?? executionResult["errorDetails"]?.ToString()
                   ?? "Script returned business error";
            return $"[{actionName}] {msg}";
        }
        // === 新增结束 ===

        // === 原有：HTTP API Action 业务错误检测（保持不变）===
        var responseStr = executionResult["response"]?.ToString();
        if (string.IsNullOrEmpty(responseStr)) return null;

        JObject responseObj;
        try { responseObj = JObject.Parse(responseStr); }
        catch { return null; }

        var successField = responseObj["success"];
        if (successField != null && successField.Type == JTokenType.Boolean && !successField.Value<bool>())
        {
            var msg = responseObj["msg"]?.ToString()
                   ?? responseObj["message"]?.ToString()
                   ?? responseObj["error"]?.ToString()
                   ?? "External API returned business error";
            var code = responseObj["code"]?.ToString();
            return code != null ? $"[{actionName}] {msg} (code: {code})" : $"[{actionName}] {msg}";
        }

        var statusCode = executionResult["statusCode"]?.Value<int>();
        if (statusCode.HasValue && statusCode.Value >= 400)
        {
            var msg = responseObj["msg"]?.ToString()
                   ?? responseObj["message"]?.ToString()
                   ?? $"HTTP {statusCode}";
            return $"[{actionName}] {msg}";
        }
    }
    catch (Exception ex)
    {
        _logger.LogDebug(ex, "Error checking business error in execution result for action {ActionName}", actionName);
    }

    return null;
}
```

### 改动点 6：`ExecuteTriggerActionAsync` — 提取 message 写入 ResultData

**文件**：`ActionExecutor.cs`（`ConditionActionExecutor`）

在执行成功后，把 `message` 写入 `ResultData`，供 `ConditionLogHelper` 展示：

```csharp
// 执行成功后（CheckForBusinessError 返回 null 之后）
result.Success = true;
result.ResultData["actionDefinitionId"] = action.ActionDefinitionId.Value;
result.ResultData["actionName"] = actionDefinition.ActionName;
result.ResultData["status"] = "Executed";
result.ResultData["executionResult"] = executionResult ?? (object)"null";
// === 新增：把业务 message 写入 ResultData，供 ConditionLogHelper 展示 ===
// executionResult 类型为 JToken?，直接索引即可
var businessMsg = executionResult?["message"]?.ToString();
if (!string.IsNullOrEmpty(businessMsg))
{
    result.ResultData["businessMessage"] = businessMsg;
}
// === 新增结束 ===
```

### 改动点 7：`ExecuteActionsAsync` — shouldBlock 时立刻中断 action 链

**文件**：`ActionExecutor.cs`（`ConditionActionExecutor`）

当前循环遇到失败会继续执行后续 action，需要在 TriggerAction 失败时立刻中断：

```csharp
foreach (var action in actions.OrderBy(a => a.Order))
{
    var actionResult = await ExecuteActionAsync(action, context, previousActionResult, accumulatedPrevFields);
    result.Details.Add(actionResult);

    if (!actionResult.Success)
    {
        _logger.LogWarning("Action {ActionType} failed for condition {ConditionId}: {ErrorMessage}",
            action.Type, context.ConditionId, actionResult.ErrorMessage);

        // === 新增：TriggerAction 失败（业务阻断）时立刻中断整个 action 链 ===
        if (action.Type?.Equals(StageConditionConstants.ActionTypeTriggerAction, StringComparison.OrdinalIgnoreCase) == true)
        {
            result.Success = false;
            break;
        }
        // 其他 action 类型失败后继续执行（保持原有行为）
    }

    // ... 原有的 chain data 传递逻辑不变
}
```

### 改动点 8：`ConditionLogHelper.GetActionResultDetail` — 展示 businessMessage

**文件**：`Application/Helpers/ConditionLogHelper.cs`

`triggeraction` 分支改为调用新的辅助方法：

```csharp
public static string GetActionResultDetail(ActionExecutionDetail action)
{
    if (action.ResultData == null || !action.ResultData.Any())
        return string.Empty;

    return action.ActionType.ToLower() switch
    {
        "gotostage" => GetResultDataString(action.ResultData, "targetStageName") ?? "",
        "skipstage" => GetSkipStageDetail(action.ResultData),
        "endworkflow" => GetEndWorkflowDetail(action.ResultData),
        "sendnotification" => GetSendNotificationDetail(action.ResultData),
        "updatefield" => GetUpdateFieldDetail(action.ResultData),
        // === 改动：加入 businessMessage 展示 ===
        "triggeraction" => GetTriggerActionDetail(action.ResultData),
        // === 改动结束 ===
        "assignuser" => GetAssignUserDetail(action.ResultData),
        _ => ""
    };
}

// === 新增方法 ===
private static string GetTriggerActionDetail(Dictionary<string, object> resultData)
{
    var actionName = GetResultDataString(resultData, "actionName") ?? "";
    var businessMessage = GetResultDataString(resultData, "businessMessage") ?? "";

    if (!string.IsNullOrEmpty(businessMessage))
    {
        return string.IsNullOrEmpty(actionName)
            ? businessMessage
            : $"{actionName}: {businessMessage}";
    }
    return actionName;
}
// === 新增结束 ===
```

展示效果变化：

- 成功前：`TriggerAction(create customer full)`
- 成功后：`TriggerAction(create customer full: Customer created: CUST001)`
- 失败前：`TriggerAction(create customer full)` + errorMessage 字段
- 失败后：`TriggerAction(create customer full: Tax ID already exists)` + errorMessage 字段

### 改动点 9：`AccumulatePrevFields` — 支持从 Python Action 的 `data` 提取 `prev_xxx`

**文件**：`ActionExecutor.cs`（`ConditionActionExecutor`）

**背景**：`AccumulatePrevFields` 目前只处理 HTTP API Action 的结构（`{ response: '{"data": {...}}' }`），Python Action 没有 `response` 字段，所以第一行就 return，`prev_xxx` 链式传递对 Python Action 完全不工作。

改动：在原有逻辑之前，增加对 Python Action 顶层 `data` 字段的处理：

```csharp
private static void AccumulatePrevFields(JToken actionResult, Dictionary<string, object> accumulated)
{
    try
    {
        // === 新增：Python Action 路径 ===
        // Python Action 返回结构：{ success, message, data: { customerCode, ... }, stdout, ... }
        // 没有 response 字段，直接从顶层 data 提取 prev_ 字段
        if (actionResult?["response"] == null && actionResult?["data"] is JObject pythonDataObj)
        {
            foreach (var prop in pythonDataObj.Properties())
            {
                if (prop.Value.Type == JTokenType.Null) continue;
                var key = $"prev_{prop.Name}";
                accumulated[key] = prop.Value.Type == JTokenType.Object || prop.Value.Type == JTokenType.Array
                    ? prop.Value.ToString()
                    : (object)prop.Value.ToObject<object>();
            }
            return; // Python Action 处理完毕，不走 HTTP API 路径
        }
        // === 新增结束 ===

        // === 原有：HTTP API Action 路径（保持不变）===
        var responseStr = actionResult?["response"]?.ToString();
        if (string.IsNullOrEmpty(responseStr)) return;

        JObject responseObj;
        try { responseObj = JObject.Parse(responseStr); }
        catch { return; }

        var dataObj = responseObj["data"] as JObject;
        if (dataObj != null)
        {
            foreach (var prop in dataObj.Properties())
            {
                if (prop.Value.Type == JTokenType.Null) continue;
                var key = $"prev_{prop.Name}";
                var value = prop.Value.Type == JTokenType.Object || prop.Value.Type == JTokenType.Array
                    ? prop.Value.ToString()
                    : (object)prop.Value.ToObject<object>();
                accumulated[key] = value;
            }
        }

        foreach (var prop in responseObj.Properties())
        {
            if (prop.Name == "data" || prop.Name == "success" || prop.Name == "code" || prop.Name == "msg") continue;
            var key = $"prev_{prop.Name}";
            if (!accumulated.ContainsKey(key) && prop.Value.Type != JTokenType.Null)
            {
                accumulated[key] = prop.Value.Type == JTokenType.Object || prop.Value.Type == JTokenType.Array
                    ? prop.Value.ToString()
                    : (object)prop.Value.ToObject<object>();
            }
        }
    }
    catch
    {
        // Silently ignore parse errors
    }
}
```

**效果**：Python 脚本返回 `{ "data": { "customerCode": "CUST001", "customerId": "12345" } }` 后，下一个 action 的 context 里会自动注入 `prev_customerCode = "CUST001"` 和 `prev_customerId = "12345"`。

---

## 五、Python 脚本约定的返回值结构

改造后，Python 脚本的 `main()` 函数应返回以下结构：

```python
# 成功
return {
    "success": True,
    "message": "Customer created: CUST001",  # 展示在 change log 里
    "data": { "customerCode": "CUST001" }    # 可选，供后续 action 链使用
}

# 业务失败，阻断 Stage
return {
    "success": False,
    "shouldBlock": True,                     # 明确标识：阻断 Stage
    "message": "Tax ID already exists",      # 展示给用户的错误信息
}

# 业务跳过，不阻断 Stage
return {
    "success": False,
    "shouldBlock": False,                    # 不阻断，只记录日志
    "message": "Address creation skipped: all address fields empty",
}
```

**默认值规则**：

- 如果返回 `{"success": false}` 但没有 `shouldBlock` 字段，默认视为 `shouldBlock: true`（安全优先）
- 如果 stdout 没有合法 JSON（纯脚本/调试输出），不影响执行结果，`success` 保持 `true`

---

## 六、改造后的完整行为对照表

| 场景                   | Python 返回值                                                                           | C# 解析结果              | Stage 结果               | Change Log                                                                                       |
| ---------------------- | --------------------------------------------------------------------------------------- | ------------------------ | ------------------------ | ------------------------------------------------------------------------------------------------ |
| 正常成功               | `{"success": true, "message": "Customer created", "data": {"customerCode": "CUST001"}}` | success=true             | Stage Complete           | `TriggerAction(create customer full: Customer created)` + 下一个 action 可用 `prev_customerCode` |
| 业务拒绝（阻断）       | `{"success": false, "shouldBlock": true, "message": "Tax ID duplicate"}`                | shouldBlock=true         | Stage 被阻断             | `TriggerAction(create customer full: Tax ID duplicate)` + errorMessage                           |
| 业务跳过（不阻断）     | `{"success": false, "shouldBlock": false, "message": "Address skipped"}`                | shouldBlock=false        | Stage Complete           | `TriggerAction(create customer full: Address skipped)`                                           |
| Python raise Exception | `raise Exception("...")`                                                                | 异常传递到上层           | Stage 被阻断（原有机制） | Failed + errorMessage                                                                            |
| Judge0 崩溃/超时       | Judge0 status >= 4                                                                      | shouldBlock=true（自动） | Stage 被阻断             | `TriggerAction(create customer full: Execution failed)`                                          |
| 旧脚本（无结构化返回） | `print("hello")` 或 `return None`                                                       | success=true（默认）     | Stage Complete           | `TriggerAction(create customer full)`                                                            |

---

## 七、向后兼容说明

1. **旧脚本无需修改**：stdout 解析失败时保持 `success=true` 默认值，旧脚本行为不变
2. **`raise Exception` 仍然有效**：原有的异常阻断机制完全保留，不依赖新机制
3. **非 Python Action 不受影响**：`CheckForBusinessError` 新增的 Python 检测在 HTTP API 路径之前，HTTP API 结构（有 `response` 字段）不会误触发 Python 检测分支

---

## 八、不需要改动的内容

- **数据库结构**：无需 Migration，`ff_action_executions.execution_output` 已是 JSONB，可存任意结构
- **StageService 的 `LogActionExecutionAsync`**：change log 文字通过 `ConditionLogHelper` 组装，StageService 不需要改
- **`EvaluateAndExecuteStageConditionAsync`**：TriggerAction 失败后抛 `CRMException` 的逻辑不变，改动点 7 只是让 action 链更早中断，不影响最终阻断判断
