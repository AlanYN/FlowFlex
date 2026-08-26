# Requirements Document

## Introduction

为 FlowFlex 的 Python Script Action 系统增加执行日志和主动阻断两项能力。

当前 Python 脚本（通过 Judge0 沙箱执行）只能通过 `raise Exception` 阻断 Stage Complete，且执行的业务结果（如"Tax ID 重复，跳过创建"）不会出现在 case 的 change log 展示面板。本次改造使脚本可返回结构化 JSON，C# 层解析后将业务消息写入 change log，并在 `success: false, shouldBlock: true` 时主动阻断 Stage。

---

## Glossary

- **Python Script Action**：FlowFlex 中一种 Action 类型，用户编写 Python 脚本，通过 Judge0 沙箱执行
- **Judge0**：外部代码沙箱服务，执行 Python 脚本并返回 stdout/stderr 及状态码
- **Stage Complete**：用户点击完成某个 Stage 后系统触发的流程，包含条件评估与 Action 执行
- **Action Chain**：一个 Stage Condition 下按 Order 顺序执行的多个 Action 序列
- **Change Log**：case 中记录操作历史的展示面板，当前已展示 `TriggerAction(actionName)` 条目
- **ConditionLogHelper**：组装 change log 文字的帮助类
- **ActionExecutionDetail**：内存对象，在 `ExecuteTriggerActionAsync` 里填充，供 `ConditionLogHelper` 读取
- **ResultData**：`ActionExecutionDetail` 上的 `Dictionary<string, object>`，存储供 change log 展示的键值对
- **shouldBlock**：Python 脚本返回结构中的布尔字段，`true` 表示要求阻断 Stage Complete
- **businessMessage**：脚本返回的 `message` 字段，用于 change log 展示和 Serilog 日志
- **Runner Script**：`PythonActionExecutor` 生成的包装脚本，注入参数后调用用户定义的 `main()` 函数
- **PythonActionExecutor**：执行 Python Action 的类，负责构造 Runner Script、提交 Judge0、解析结果
- **ActionExecutionService**：持久化 `ff_action_executions` 记录的服务
- **ConditionActionExecutor**：遍历执行 Action Chain 的执行器，包含 `CheckForBusinessError`、`ExecuteTriggerActionAsync`、`ExecuteActionsAsync`
- **AccumulatePrevFields**：将当前 Action 输出的 `data` 注入到后续 Action 的 context 参数中

---

## Requirements

---

### Requirement 1: Runner Script 强制输出合法 JSON

**User Story：** As a 系统工程师，I want Python 脚本的 stdout 始终是合法 JSON，so that C# 层能可靠地解析业务结果而不因格式差异解析失败。

#### Acceptance Criteria

1. THE PythonActionExecutor SHALL 在生成 Runner Script 时于脚本开头注入 `import json`
2. WHEN `main()` 函数返回非 None 值时，THE Runner Script SHALL 使用 `print(json.dumps(output_obj))` 输出结果
3. WHEN `main()` 函数返回 None 时，THE Runner Script SHALL 输出字面字符串 `null`
4. THE Runner Script SHALL 不使用 Python `print(dict)` 的默认格式（会输出 `True`/`False`/`None` 和单引号），确保输出符合 JSON 规范

---

### Requirement 2: 解析 stdout 中的结构化业务结果

**User Story：** As a 系统工程师，I want `PythonActionExecutor` 能从 Judge0 stdout 中解析业务结果，so that `success`、`shouldBlock`、`message`、`data` 字段可被后续逻辑使用。

#### Acceptance Criteria

1. WHEN Judge0 执行状态为 status == 3（Accepted）时，THE PythonActionExecutor SHALL 取 stdout 最后一行尝试解析为 JSON
2. WHEN 最后一行以 `{` 或 `[` 开头时，THE PythonActionExecutor SHALL 解析 `success`（bool）、`shouldBlock`（bool）、`message`（string）、`data`（object，可选）字段
3. WHEN 解析出 `success: false` 且 `shouldBlock` 字段缺失时，THE PythonActionExecutor SHALL 默认将 `shouldBlock` 视为 `true`（安全优先原则）
4. WHEN stdout 最后一行不是合法 JSON 时，THE PythonActionExecutor SHALL 保持默认值 `success: true, shouldBlock: false`，不影响脚本执行结果
5. THE PythonActionExecutor SHALL 将解析后的 `success`、`shouldBlock`、`message`、`data`、`stdout`、`stderr`、`executionTime`、`memoryUsage`、`status`、`token`、`timestamp` 组合为成功结果对象返回

---

### Requirement 3: Judge0 异常路径自动标记 shouldBlock

**User Story：** As a 系统工程师，I want Judge0 执行失败（status >= 4）时自动阻断 Stage，so that 脚本运行时错误或超时不会被误认为成功而放行 Stage Complete。

#### Acceptance Criteria

1. WHEN Judge0 返回 status >= 4（运行时错误、超时、内存超限等）时，THE PythonActionExecutor SHALL 构造包含 `success: false, shouldBlock: true` 的错误结果对象
2. WHEN 配置错误或参数解析失败导致脚本无法提交时，THE PythonActionExecutor SHALL 构造包含 `success: false, shouldBlock: true` 的错误结果对象
3. THE PythonActionExecutor SHALL 在错误结果对象中包含 `message`（取 `status.description` 或自定义文本）、`errorDetails`、`stdout`、`stderr`、`token`、`timestamp` 字段

---

### Requirement 4: 业务结果写入 Serilog 日志

**User Story：** As a 运维人员，I want Python Script Action 执行后的业务消息出现在 Serilog 日志中，so that 事后排查时能通过日志定位脚本执行的业务状态。

#### Acceptance Criteria

1. WHEN Action 执行完成且 `execution_output` 中包含非空 `message` 字段时，THE ActionExecutionService SHALL 写入 Serilog 日志
2. WHEN `success: true` 时，THE ActionExecutionService SHALL 以 `LogInformation` 级别记录 `ActionId`、`ActionName`、`Message`
3. WHEN `success: false` 时，THE ActionExecutionService SHALL 以 `LogWarning` 级别记录 `ActionId`、`ActionName`、`Message`、`ShouldBlock`
4. WHILE `message` 字段为空或不存在时，THE ActionExecutionService SHALL 不写入额外日志（避免空日志噪声）

---

### Requirement 5: Python Action 业务错误检测触发阻断

**User Story：** As a 业务用户，I want Python 脚本返回 `success: false, shouldBlock: true` 时 Stage 被阻断，so that 业务异常能够阻止 Stage Complete 而不需要脚本通过 `raise Exception` 实现。

#### Acceptance Criteria

1. WHEN Action 执行结果包含 `success: false` 且 `shouldBlock: true` 时，THE ConditionActionExecutor SHALL 识别为业务错误并阻断当前 Action Chain
2. THE ConditionActionExecutor SHALL 在 `CheckForBusinessError` 中优先检测 Python Action 结构（`success` + `shouldBlock` 字段），再检测 HTTP API Action 结构（`response` 字段）
3. WHEN Python Action 结果包含 `success: false, shouldBlock: true` 时，THE ConditionActionExecutor SHALL 返回包含 `[actionName] message` 格式的错误描述
4. WHEN Action 结果包含 `response` 字段时，THE ConditionActionExecutor SHALL 走原有 HTTP API 检测逻辑，不触发 Python 检测分支
5. IF `CheckForBusinessError` 解析过程中发生异常，THEN THE ConditionActionExecutor SHALL 记录 Debug 日志并返回 null（不阻断）

---

### Requirement 6: TriggerAction 失败时立刻中断 Action Chain

**User Story：** As a 系统工程师，I want TriggerAction 失败后立即停止后续 Action 的执行，so that 业务阻断语义明确，避免后续 Action 在错误状态下继续执行。

#### Acceptance Criteria

1. WHEN Action Chain 中某个 TriggerAction 的 `actionResult.Success == false` 时，THE ConditionActionExecutor SHALL 立即设置整体结果为失败并终止循环
2. WHEN 其他类型（非 TriggerAction）的 Action 失败时，THE ConditionActionExecutor SHALL 记录警告日志后继续执行后续 Action（保持原有行为）
3. THE ConditionActionExecutor SHALL 在中断时将已执行的所有 Action 结果（包括失败项）保存在 `result.Details` 中

---

### Requirement 7: 业务消息写入 ResultData 供 Change Log 展示

**User Story：** As a 业务用户，I want 在 Stage Condition 展示面板看到 Python 脚本执行的业务消息，so that 无需查看系统日志即可了解脚本执行的业务状态。

#### Acceptance Criteria

1. WHEN TriggerAction 执行成功（`CheckForBusinessError` 返回 null）后，THE ConditionActionExecutor SHALL 将执行结果中的 `message` 字段写入 `ResultData["businessMessage"]`
2. WHEN `message` 字段为空或不存在时，THE ConditionActionExecutor SHALL 不写入 `businessMessage` 键（不引入空字符串噪声）
3. THE ConditionActionExecutor SHALL 保持 `ResultData` 中原有字段（`actionDefinitionId`、`actionName`、`status`、`executionResult`）不变

---

### Requirement 8: Change Log 展示 businessMessage

**User Story：** As a 业务用户，I want change log 条目中展示 Python 脚本返回的业务消息，so that 历史记录能反映脚本执行的业务上下文（如"Customer created: CUST001"或"Tax ID already exists"）。

#### Acceptance Criteria

1. WHEN `ResultData` 中存在非空 `businessMessage` 且 `actionName` 非空时，THE ConditionLogHelper SHALL 格式化为 `actionName: businessMessage`
2. WHEN `ResultData` 中存在非空 `businessMessage` 且 `actionName` 为空时，THE ConditionLogHelper SHALL 仅展示 `businessMessage`
3. WHEN `ResultData` 中不存在 `businessMessage` 时，THE ConditionLogHelper SHALL 仅展示 `actionName`（保持原有行为）
4. THE ConditionLogHelper SHALL 通过独立的 `GetTriggerActionDetail` 方法实现上述逻辑，不修改其他 Action 类型的展示逻辑

---

### Requirement 9: Python Action 输出的 `data` 注入后续 Action 参数

**User Story：** As a 工作流配置人员，I want Python Action 返回的 `data` 字段中的属性能作为 `prev_xxx` 参数传递给 Action Chain 中后续的 Action，so that 多个 Action 可以链式传递业务数据（如将创建的 `customerCode` 传给下一步）。

#### Acceptance Criteria

1. WHEN Python Action 执行结果顶层包含 `data` 对象（且没有 `response` 字段）时，THE ConditionActionExecutor SHALL 将 `data` 中每个非 null 属性以 `prev_{propertyName}` 为键注入到 `accumulatedPrevFields`
2. WHEN Python Action 的 `data` 属性值为 Object 或 Array 类型时，THE ConditionActionExecutor SHALL 将其序列化为字符串后注入
3. WHEN 执行结果包含 `response` 字段时，THE ConditionActionExecutor SHALL 走原有 HTTP API 路径处理 `prev_` 注入，不走 Python 路径
4. IF `AccumulatePrevFields` 处理过程中发生解析异常，THEN THE ConditionActionExecutor SHALL 静默忽略该异常，不影响后续 Action 执行

---

### Requirement 10: 向后兼容——旧脚本行为不变

**User Story：** As a 工作流配置人员，I want 现有未返回结构化 JSON 的 Python 脚本在改造后行为不变，so that 存量脚本无需修改即可继续正常运行。

#### Acceptance Criteria

1. WHEN Python 脚本的 `main()` 函数 `return None` 或仅执行 `print("debug info")` 而无结构化输出时，THE PythonActionExecutor SHALL 保持 `success: true, shouldBlock: false` 默认值
2. WHEN Python 脚本使用 `raise Exception` 抛出异常时，THE 系统 SHALL 通过原有异常传播机制阻断 Stage（Judge0 status >= 4 → 抛异常路径），不依赖新的 shouldBlock 机制
3. THE 系统 SHALL 不修改 HTTP API Action 的检测和执行逻辑，HTTP API Action 的阻断行为保持不变
4. THE 系统 SHALL 不引入数据库 Migration，`ff_action_executions.execution_output` 的 JSONB 结构兼容新旧格式
