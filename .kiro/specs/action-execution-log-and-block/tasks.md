# Implementation Plan: action-execution-log-and-block

## Overview

纯后端改造，9 个改动点分布在 4 个文件。按依赖关系从底层（PythonActionExecutor）到上层（ConditionLogHelper）分阶段实施，每阶段完成后可独立编译验证。

## Tasks

- [x] 1. 修改 PythonActionExecutor — 三个方法改造
  - [x] 1.1 修改 `CreateRunnerScript`：将末尾 `print(output_obj)` 改为 `print(json.dumps(output_obj) if output_obj is not None else 'null')`
    - 文件：`Application/Services/Action/Executors/PythonActionExecutor.cs`
    - 确认脚本开头已有 `import json`（当前代码已注入，仅改 print 这一行）
    - _Requirements: 1.1, 1.2, 1.3, 1.4_

  - [x] 1.2 修改 `CreateSuccessResult`：从 stdout 最后一行解析 `success`、`shouldBlock`、`message`、`data` 字段
    - 文件：`Application/Services/Action/Executors/PythonActionExecutor.cs`
    - 取 stdout trim 后按 `\n` 分割的最后一行；以 `{` 或 `[` 开头时解析；解析失败时 catch 静默忽略，保持默认 `success=true, shouldBlock=false`
    - `success: false` 且 `shouldBlock` 字段缺失时，默认 `shouldBlock = true`（安全优先）
    - 返回对象新增 `shouldBlock`、`data` 字段，其余原有字段（`stdout`、`stderr`、`executionTime`、`memoryUsage`、`status`、`token`、`timestamp`）保持不变
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_

  - [x] 1.3 修改两个 `CreateErrorResult` 重载：均增加 `shouldBlock = true` 字段
    - 文件：`Application/Services/Action/Executors/PythonActionExecutor.cs`
    - `CreateErrorResult(Judge0SubmissionResultDto?, string?)` 和 `CreateErrorResult(string)` 均加 `shouldBlock = true`
    - _Requirements: 3.1, 3.2, 3.3_

  - [ ]\* 1.4 为 `PythonActionExecutor` 编写单元测试
    - 文件：`Tests/FlowFlex.Tests/Services/Action/PythonActionExecutorTests.cs`（新建）
    - 测试 `CreateSuccessResult` 的 stdout 解析（通过 `internal` + `InternalsVisibleTo` 或 reflection 调用私有方法）：
      - `CreateSuccessResult_WhenLastLineIsValidJsonWithSuccessTrue_ShouldExtractMessage`
      - `CreateSuccessResult_WhenSuccessFalseAndShouldBlockMissing_ShouldDefaultShouldBlockToTrue`
      - `CreateSuccessResult_WhenSuccessFalseAndShouldBlockExplicitFalse_ShouldRespectFalse`
      - `CreateSuccessResult_WhenLastLineIsNotJson_ShouldDefaultToSuccessTrueNoBlock`
      - `CreateSuccessResult_WhenStdoutIsEmpty_ShouldReturnDefaults`
    - 测试 `CreateErrorResult`：
      - `CreateErrorResult_WithJudge0Result_ShouldSetShouldBlockTrue`
      - `CreateErrorResult_WithStringMessage_ShouldSetShouldBlockTrue`
    - 测试 `CreateRunnerScript`（通过 reflection 或提取为 internal 测试）：
      - `CreateRunnerScript_ShouldUseJsonDumpsNotDirectPrint`
      - `CreateRunnerScript_WhenNoParams_ShouldHandleNullOutputCorrectly`
    - _Requirements: 1.2, 1.3, 2.1–2.5, 3.1, 3.2_

- [x] 2. 修改 ActionExecutionService — 写业务 Serilog 日志
  - [x] 2.1 在 `ExecuteActionAsync` 的成功路径中，`await _actionExecutionRepository.UpdateAsync(execution)` 之后，提取并写入业务日志
    - 文件：`Application/Services/Action/ActionExecutionService.cs`
    - 从 `execution.ExecutionOutput` 读取 `message`、`success`、`shouldBlock`；`message` 非空时写 Serilog
    - `success: true` → `LogInformation`；`success: false` → `LogWarning`（含 `ShouldBlock` 字段）
    - `message` 为空时不写日志（避免空噪声）
    - _Requirements: 4.1, 4.2, 4.3, 4.4_

- [x] 3. 修改 ConditionActionExecutor — 四个方法改造
  - [x] 3.1 修改 `CheckForBusinessError`：在原有 HTTP API 检测逻辑之前，增加 Python Action 检测分支
    - 文件：`Application/Services/OW/StageCondition/ActionExecutor.cs`
    - 检测条件：`executionResult["response"] == null && executionResult["success"] == false && executionResult["shouldBlock"] == true`
    - 返回格式 `[actionName] message`，与原有 HTTP API 路径返回格式一致
    - 原有 HTTP API 检测路径（检测 `response` 字段）**完整保留**，不做任何修改
    - 解析异常时 catch → `LogDebug` → 返回 null（不阻断）
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

  - [x] 3.2 修改 `ExecuteTriggerActionAsync`：在 `CheckForBusinessError` 返回 null（成功）后提取 `businessMessage` 写入 `ResultData`
    - 文件：`Application/Services/OW/StageCondition/ActionExecutor.cs`
    - 从 `executionResult?["message"]` 取值；非空时写 `result.ResultData["businessMessage"]`
    - 空值时不写入（不引入空字符串键）
    - 原有 `ResultData` 字段（`actionDefinitionId`、`actionName`、`status`、`executionResult`）保持不变
    - _Requirements: 7.1, 7.2, 7.3_

  - [x] 3.3 修改 `ExecuteActionsAsync`：TriggerAction 失败时立刻中断 Action Chain
    - 文件：`Application/Services/OW/StageCondition/ActionExecutor.cs`
    - 在 `!actionResult.Success` 分支内，判断 `action.Type` 是否为 `triggeraction`（忽略大小写），是则 `result.Success = false; break`
    - 其他 Action 类型失败后继续执行（原有行为不变）
    - 已执行的 Action 结果（包括失败项）保留在 `result.Details` 中
    - _Requirements: 6.1, 6.2, 6.3_

  - [x] 3.4 修改 `AccumulatePrevFields`：在原有 HTTP API 路径之前，增加 Python Action 路径处理
    - 文件：`Application/Services/OW/StageCondition/ActionExecutor.cs`
    - Python 检测条件：`actionResult?["response"] == null && actionResult?["data"] is JObject pythonDataObj`
    - 将 `pythonDataObj` 的每个非 null 属性以 `prev_{propertyName}` 注入 `accumulated`；Object/Array 类型序列化为字符串
    - Python 路径处理完后 `return`，不再走 HTTP API 路径
    - 原有 HTTP API 路径（以 `var responseStr = actionResult?["response"]?.ToString()` 开头）**完整保留**
    - 解析异常静默忽略
    - _Requirements: 9.1, 9.2, 9.3, 9.4_

  - [ ]\* 3.5 为 `CheckForBusinessError` 编写单元测试（在已有 `ActionExecutorTests.cs` 中追加）
    - 文件：`Tests/FlowFlex.Tests/Services/OW/ActionExecutorTests.cs`
    - 测试场景：
      - `CheckForBusinessError_WhenPythonSuccessFalseAndShouldBlockTrue_ShouldReturnError`
      - `CheckForBusinessError_WhenPythonSuccessFalseAndShouldBlockFalse_ShouldReturnNull`
      - `CheckForBusinessError_WhenPythonSuccessTrueAndShouldBlockTrue_ShouldReturnNull`（success=true 不阻断）
      - `CheckForBusinessError_WhenHasResponseField_ShouldUseHttpApiPath_NotPythonPath`
      - `CheckForBusinessError_WhenResponseFieldPresentAndSuccessFalse_ShouldCheckResponseContent`
      - `CheckForBusinessError_WhenNullResult_ShouldReturnNull`
    - _Requirements: 5.1–5.5_

  - [ ]\* 3.6 为 `AccumulatePrevFields` 和 `ExecuteActionsAsync` 编写单元测试（在已有 `ActionExecutorTests.cs` 中追加）
    - 文件：`Tests/FlowFlex.Tests/Services/OW/ActionExecutorTests.cs`
    - `AccumulatePrevFields` 测试场景：
      - `AccumulatePrevFields_PythonResult_ExtractsPrevFieldsFromData`
      - `AccumulatePrevFields_PythonResult_SerializesObjectValuesToString`
      - `AccumulatePrevFields_HttpApiResult_UsesOriginalPath`
      - `AccumulatePrevFields_PythonResult_SkipsNullValues`
    - `ExecuteActionsAsync` 中断逻辑测试：
      - `ExecuteActionsAsync_WhenTriggerActionFails_ShouldStopChain`
      - `ExecuteActionsAsync_WhenNonTriggerActionFails_ShouldContinueChain`
    - _Requirements: 6.1, 6.2, 9.1–9.4_

- [x] 4. 修改 ConditionLogHelper — GetActionResultDetail 及新增 GetTriggerActionDetail
  - [x] 4.1 在 `GetActionResultDetail` 的 `switch` 表达式中，将 `"triggeraction"` 分支由直接读 `actionName` 改为调用新方法 `GetTriggerActionDetail(action.ResultData)`
    - 文件：`Application/Helpers/ConditionLogHelper.cs`
    - 仅修改 `"triggeraction"` 分支，其他所有分支和方法保持不变
    - _Requirements: 8.4_

  - [x] 4.2 在 `ConditionLogHelper` 中新增私有方法 `GetTriggerActionDetail`
    - 文件：`Application/Helpers/ConditionLogHelper.cs`
    - 逻辑：读 `actionName` 和 `businessMessage`；两者均非空时返回 `$"{actionName}: {businessMessage}"`；仅有 `businessMessage` 时返回 `businessMessage`；否则返回 `actionName`
    - _Requirements: 8.1, 8.2, 8.3_

  - [ ]\* 4.3 为 `ConditionLogHelper` 编写单元测试（新建测试文件）
    - 文件：`Tests/FlowFlex.Tests/Helpers/ConditionLogHelperTests.cs`（新建）
    - 测试场景：
      - `GetTriggerActionDetail_WhenBothActionNameAndBusinessMessage_ShouldFormatWithColon`
      - `GetTriggerActionDetail_WhenOnlyActionName_ShouldReturnActionName`
      - `GetTriggerActionDetail_WhenOnlyBusinessMessage_ShouldReturnMessageOnly`
      - `GetTriggerActionDetail_WhenBusinessMessageIsEmpty_ShouldReturnActionName`
      - `GetTriggerActionDetail_WhenBothEmpty_ShouldReturnEmpty`
      - `GetActionResultDetail_TriggerActionType_ShouldUseTriggerActionDetail`
    - _Requirements: 8.1, 8.2, 8.3, 8.4_

- [ ] 5. 最终验证检查点
  - 确保 `dotnet build` 无编译错误
  - 确保 `dotnet test` 中本次新增/修改的测试全部通过
  - 验证旧脚本（stdout 无结构化 JSON）路径：`success=true, shouldBlock=false` 默认值不变
  - 验证 HTTP API Action（含 `response` 字段）检测路径不受 Python 检测分支影响

## Notes

- 任务标有 `*` 的为可选测试任务，可跳过以加快 MVP 交付
- 改造为**纯后端**，无前端变更、无数据库 Migration
- 所有改动均向后兼容：旧 Python 脚本（无结构化返回）行为不变；HTTP API Action 路径不受影响
- `PythonActionExecutor` 中的私有方法（`CreateSuccessResult`、`CreateErrorResult`、`CreateRunnerScript`）若测试需要访问，可改为 `internal` 并在测试项目中加 `[assembly: InternalsVisibleTo("FlowFlex.Tests")]`
- `CheckForBusinessError` 是私有方法，可通过 reflection 调用或将方法改为 `internal` 供测试访问

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "1.3"] },
    { "id": 1, "tasks": ["2.1", "3.1", "3.4"] },
    { "id": 2, "tasks": ["3.2", "3.3", "4.1", "4.2"] },
    { "id": 3, "tasks": ["1.4", "3.5", "3.6", "4.3"] }
  ]
}
```
