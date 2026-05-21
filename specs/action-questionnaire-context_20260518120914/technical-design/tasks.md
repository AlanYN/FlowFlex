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

1. `questionnaireAnswerByQuestionId` 已确认按“Stage 完成时间后者覆盖前者”落地
2. 已确认由 `ActionContextBuilder` 自己查询已完成 Stage 并复用现有 `GetQuestionnaireDataAsync`
3. 已确认 v1 先只正式支持 map/path 访问，不主推数组索引路径
