# Tasks: Action Questionnaire Context — 技术方案设计阶段

## 开发任务列表

### 第一组：后端契约与基础设施

| Task ID | 任务 | 文件路径 | 说明 | 状态 |
|---------|------|---------|------|------|
| TD-T01 | 新增问卷上下文 DTO | `packages/flowFlex-backend/Application.Contracts/Dtos/Action/ActionQuestionnaireContextDto.cs` | 定义问卷明细和 map 结构 | ⏳ pending |
| TD-T02 | 新增 IActionContextBuilder 接口 | `packages/flowFlex-backend/Application.Contracts/IServices/Action/IActionContextBuilder.cs` | 定义 StageCondition Trigger 上下文构建接口 | ⏳ pending |
| TD-T03 | 新增 TemplateVariableResolver 接口/实现 | `packages/flowFlex-backend/Application/Services/Action/TemplateVariableResolver.cs` | 提供深路径模板解析能力 | ⏳ pending |

### 第二组：后端业务实现

| Task ID | 任务 | 文件路径 | 说明 | 状态 |
|---------|------|---------|------|------|
| TD-T04 | 实现 ActionContextBuilder | `packages/flowFlex-backend/Application/Services/Action/ActionContextBuilder.cs` | 聚合基础字段、static fields、问卷答案、token、previousActionResult | ⏳ pending |
| TD-T05 | 修改 TriggerAction 执行链路 | `packages/flowFlex-backend/Application/Services/OW/StageCondition/ActionExecutor.cs` | 使用 ActionContextBuilder 替代本地上下文拼装 | ⏳ pending |
| TD-T06 | 扩展问卷多 Stage 聚合能力 | `packages/flowFlex-backend/Application/Services/OW/ComponentDataService.cs` / `IComponentDataService.cs` | 提供按已完成 Stage 收集问卷答案的辅助能力或复用方案 | ⏳ pending |
| TD-T07 | 改造 HTTP Action 模板解析 | `packages/flowFlex-backend/Application/Services/Action/Executors/HttpApiActionExecutor.cs` | 接入深路径解析器，覆盖 URL/Params/Headers/Body | ⏳ pending |

### 第三组：前端对齐

| Task ID | 任务 | 文件路径 | 说明 | 状态 |
|---------|------|---------|------|------|
| TD-T08 | 更新 VariablesPanel 问卷变量展示 | `packages/flowFlex-common/src/app/components/actionTools/VariablesPanel.vue` | 展示真实可用的问卷变量结构 | ⏳ pending |
| TD-T09 | 更新 VariableAutoComplete 候选 | `packages/flowFlex-common/src/app/components/actionTools/VariableAutoComplete.vue` | 支持问卷变量自动补全 | ⏳ pending |
| TD-T10 | 更新 HttpConfig 示例文案 | `packages/flowFlex-common/src/app/components/actionTools/HttpConfig.vue` | 将示例改为真实字段路径 | ⏳ pending |

### 第四组：测试与回归

| Task ID | 任务 | 文件路径 | 说明 | 状态 |
|---------|------|---------|------|------|
| TD-T11 | 新增模板解析器单元测试 | `packages/flowFlex-backend/Tests/FlowFlex.Tests/Services/Action/TemplateVariableResolverTests.cs` | 覆盖顶层、嵌套、map key、未命中场景 | ⏳ pending |
| TD-T12 | 扩展 TriggerAction 相关测试 | `packages/flowFlex-backend/Tests/FlowFlex.Tests/Services/OW/ActionExecutorTests.cs` | 覆盖多 Stage 问卷上下文注入 | ⏳ pending |
| TD-T13 | 验证现有兼容能力不回归 | 现有相关测试文件 | 验证 static field / previousActionResult 相关能力不被破坏 | ⏳ pending |

---

## 执行顺序

```text
第一组（TD-T01~T03）
→ 第二组（TD-T04~T07）
→ 第三组（TD-T08~TD-T10）
→ 第四组（TD-T11~TD-T13）
```

同组内可部分并行，但 `TD-T04` 依赖 `TD-T01~T03`，`TD-T07` 依赖 `TD-T03`，测试组依赖实现完成。

---

## 关键实现决策待确认

1. `questionnaireAnswerByQuestionId` 已确认按”Stage 完成时间后者覆盖前者”落地
2. 已确认由 `ActionContextBuilder` 自己查询已完成 Stage 并复用现有 `GetQuestionnaireDataAsync`
3. 已确认 v1 先只正式支持 map/path 访问，不主推数组索引路径

---

## 补充开发记录（2026-05-22）

### 已完成的 Bug 修复

| 修复项 | 文件路径 | 说明 |
|--------|---------|------|
| 数组值展开 | `packages/flowFlex-backend/Application/Services/Action/TemplateVariableResolver.cs` | checkboxes 等数组类型答案（如 `[“wise”]`）现在展开为逗号分隔字符串（如 `wise`），而非返回原始 JSON 数组 |
| 扁平 key 优先匹配 | `packages/flowFlex-backend/Application/Services/Action/TemplateVariableResolver.cs` | `ResolvePathFromToken` 先尝试用完整带点号路径作为扁平 key 直接匹配（AI match 结果），找不到再 fallback 到逐层点号解析，解决 AI lookup 结果被原始问卷答案覆盖的问题 |

### 新增功能：Grid 表格类型问卷支持（方案 2 — 嵌套对象）

| Task ID | 任务 | 文件路径 | 说明 | 状态 |
|---------|------|---------|------|------|
| TD-T14 | Grid 答案解析为嵌套对象 | `packages/flowFlex-backend/Application/Services/OW/ComponentDataService.cs` | 对 `short_answer_grid` / `multiple_choice_grid` / `checkbox_grid` 类型，解析 `responseText` 为 `{rowId: {columnId: value}}` 嵌套结构存入 answerDict | ✅ done |
| TD-T15 | ActionContextBuilder 透传嵌套对象 | `packages/flowFlex-backend/Application/Services/Action/ActionContextBuilder.cs` | 无需修改 — 已有逻辑直接将 object 透传到 `answerByQuestionId`，序列化时自然成为嵌套 JObject | ✅ done (无改动) |
| TD-T16 | TemplateVariableResolver 数组展开 | `packages/flowFlex-backend/Application/Services/Action/TemplateVariableResolver.cs` | Array 类型 resolved 值展开为逗号分隔字符串 | ✅ done |
| TD-T17 | TemplateVariableResolver 扁平 key 优先 | `packages/flowFlex-backend/Application/Services/Action/TemplateVariableResolver.cs` | 先匹配完整路径扁平 key，再 fallback 逐层解析 | ✅ done |

### Grid 变量引用格式

Action 模板中引用 grid 单元格值的语法：

```
{{questionnaireAnswerByQuestionId.<questionId>.<rowId>.<columnId>}}
```

示例（Person of Contact 表格）：
- Primary Contact 的 Contact Name：`{{questionnaireAnswerByQuestionId.2050329392391000074.2050329392391000069.2050329392391000071}}`
- AP Contact 的 Email Address：`{{questionnaireAnswerByQuestionId.2050329392391000074.2050329392391000070.2050329392391000072}}`

其中：
- `2050329392391000074` = questionId（Person of Contact 问题）
- `2050329392391000069` = rowId（Primary Contact 行）
- `2050329392391000071` = columnId（Contact Name 列）

### 兼容性说明

- 普通问题（short_answer, checkboxes, dropdown 等）引用方式不变：`{{questionnaireAnswerByQuestionId.<questionId>}}`
- 非问卷变量不受影响：`{{customername}}`、`{{companyName}}` 等照常工作
- AI lookup 扁平 key（如 `questionnaireAnswerByQuestionId.xxx` 作为顶层 key）优先级高于逐层解析
