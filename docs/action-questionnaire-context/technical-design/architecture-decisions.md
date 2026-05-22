# Architecture Decisions: Action Questionnaire Context

## ADR-001: 问卷上下文构建集中到 ActionContextBuilder

**决策**：新增 `ActionContextBuilder`，统一负责 TriggerAction 执行上下文构建。

**原因**：
- 当前 `ConditionActionExecutor` 已承担过多职责
- 问卷、Static Field、Token、previousActionResult 聚合逻辑不应继续散落在执行器里
- 未来其他 TriggerSource 也可复用该能力

---

## ADR-002: 默认读取当前 Case 下所有已完成 Stage 的问卷答案

**决策**：v1 默认读取当前 Case 下所有已完成 Stage 的问卷答案，而非仅当前触发 Stage。

**原因**：
- 当前业务场景存在跨 Stage 取值需求
- 只读当前 Stage 会直接限制 OW-630 的可用性
- “所有已完成 Stage”是比“显式逐个选 Stage”更快落地的 v1 方案

---

## ADR-003: 多 Stage 冲突按完成时间后者覆盖前者

**决策**：当多个已完成 Stage 提供同一 `questionId` 的值时，按 Stage 完成时间后者覆盖前者。

**原因**：
- 规则简单、可测试、可解释
- 更符合“后续阶段修正前序信息”的业务直觉
- 明细数组 `questionnaireAnswers` 仍保留，便于排查被覆盖过程

---

## ADR-004: v1 以 questionId 作为正式问卷取值主键

**决策**：v1 使用 `questionId`，不使用 `questionText` 作为正式取值 key。

**原因**：
- `questionText` 会因文案调整、多语言、重复命名而不稳定
- 当前系统已有稳定可读到的字段是 `questionId`
- 虽然未来更理想的是 `questionKey`，但本期不扩张到问卷建模改造

---

## ADR-005: HTTP Action 通过统一 TemplateVariableResolver 解析模板

**决策**：从 `HttpApiActionExecutor` 中抽出通用模板解析器，支持深路径解析。

**原因**：
- 当前执行器只支持浅层属性，不足以支撑问卷上下文
- 路径解析逻辑适合作为共享能力，而不是散落在每个 executor 中
- 便于后续其他 executor 复用

---

## ADR-006: v1 主推 map/path 访问，不主推数组索引

**决策**：正式支持 `questionnaireAnswerByQuestionId.xxx` 与 `questionnaireAnswerMap.a.b`，不主推 `questionnaireAnswers[0].answer`。

**原因**：
- CRM / 外部系统对接更需要稳定路径，不需要依赖数组顺序
- map/path 方案更适合配置场景，也更容易在 VariablesPanel 中引导用户使用
- 可降低模板解析复杂度
