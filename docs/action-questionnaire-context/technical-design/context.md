# Context: Action Questionnaire Context — 技术方案设计阶段

> 本文件为下游阶段（测试验证）提供输入摘要。

---

## 技术栈

| 层级 | 技术 |
|------|------|
| 后端 | C# / ASP.NET Core / SqlSugar |
| JSON | Newtonsoft.Json + JToken |
| 前端 | Vue 3 + TypeScript + Element Plus |
| 测试 | xUnit / 现有 FlowFlex.Tests |

---

## 核心方案摘要

- 新增 `ActionContextBuilder`，统一构建 TriggerAction 执行上下文
- 问卷来源默认覆盖 **当前 Case 下所有已完成 Stage**
- 输出三种问卷结构：
  - `questionnaireAnswers`
  - `questionnaireAnswerMap`
  - `questionnaireAnswerByQuestionId`
- 新增 `TemplateVariableResolver`，用于 HTTP Action 的深路径模板解析
- 现有 static field / previousActionResult / integrationToken 保持兼容

---

## 已确认关键决策

| 决策项 | 结论 |
|--------|------|
| 多 Stage 同 questionId 覆盖规则 | 按 Stage 完成时间后者覆盖前者 |
| 多 Stage 聚合实现位置 | `ActionContextBuilder` 自查已完成 Stage，复用现有 `GetQuestionnaireDataAsync` |
| 模板访问形式 | v1 主推 map/path 访问，不主推数组索引路径 |
| 模板未命中行为 | 替换为空字符串 + warning 日志 |

---

## 关键文件摘要

**后端新增**：
- `Application.Contracts/Dtos/Action/ActionQuestionnaireContextDto.cs`
- `Application.Contracts/IServices/Action/IActionContextBuilder.cs`
- `Application/Services/Action/ActionContextBuilder.cs`
- `Application/Services/Action/TemplateVariableResolver.cs`

**后端修改**：
- `Application/Services/OW/StageCondition/ActionExecutor.cs`
- `Application/Services/Action/Executors/HttpApiActionExecutor.cs`
- `Application/Services/OW/ComponentDataService.cs`
- `Application.Contracts/IServices/OW/IComponentDataService.cs`

**前端修改**：
- `packages/flowFlex-common/src/app/components/actionTools/VariablesPanel.vue`
- `packages/flowFlex-common/src/app/components/actionTools/VariableAutoComplete.vue`
- `packages/flowFlex-common/src/app/components/actionTools/HttpConfig.vue`
