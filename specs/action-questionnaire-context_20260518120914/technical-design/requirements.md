# Technical Requirements: Action Questionnaire Context

## 技术方案需求

### TR-001: Action 上下文构建能力

**需求**：为 TriggerAction 提供统一的 Action 上下文构建能力，集中收集基础字段、Static Field、问卷答案、Integration Token、previousActionResult。

**验收标准**：
- AC-TR-001-1: 新增统一的 Action Context Builder 服务或等价职责封装，不再在 `ConditionActionExecutor` 中继续扩散上下文拼装逻辑
- AC-TR-001-2: 输出结构至少包含基础字段、`questionnaireAnswers`、`questionnaireAnswerMap`、`questionnaireAnswerByQuestionId`
- AC-TR-001-3: 保留现有 static field 顶层变量兼容能力
- AC-TR-001-4: 保留现有 integrationToken 注入能力
- AC-TR-001-5: 保留现有 previousActionResult / `prev_` 链式能力

### TR-002: 多 Stage 问卷收集能力

**需求**：默认收集当前 Case 下所有已完成 Stage 的问卷答案，并聚合为 Action 可消费结构。

**验收标准**：
- AC-TR-002-1: 能识别当前 Case 下所有已完成 Stage
- AC-TR-002-2: 对每个已完成 Stage 调用问卷数据读取逻辑，收集问卷答案
- AC-TR-002-3: 聚合结果包含明细数组和按 `questionId` / `questionnaireId+questionId` 索引的结构
- AC-TR-002-4: 当多个 Stage 出现同一 `questionId` 时，按 Stage 完成时间后者覆盖前者，且该规则可测试
- AC-TR-002-5: 无问卷或空答案时返回空结构，不抛异常中断

### TR-003: HTTP Action 深路径模板解析

**需求**：HTTP API Action 执行器必须支持从嵌套上下文中解析模板变量。

**验收标准**：
- AC-TR-003-1: 支持顶层变量，如 `{{CaseCode}}`
- AC-TR-003-2: 支持嵌套路径，如 `{{previousActionResult.data.customerCode}}`
- AC-TR-003-3: 支持数字字符串 key 路径，如 `{{questionnaireAnswerMap.1001.2001}}`
- AC-TR-003-3a: v1 不要求主推数组索引路径访问，正式使用 map/path 方案
- AC-TR-003-4: 至少覆盖 URL、Params、Headers、Body 四个位置
- AC-TR-003-5: 模板变量未命中时替换为空字符串，并记录 warning 日志

### TR-004: 前端变量提示对齐

**需求**：前端变量面板、自动补全和 HTTP 配置提示文案需要与后端真实上下文保持一致。

**验收标准**：
- AC-TR-004-1: VariablesPanel 展示问卷变量结构
- AC-TR-004-2: VariableAutoComplete 支持问卷变量候选
- AC-TR-004-3: HttpConfig 中的示例变量文案与后端真实字段一致
- AC-TR-004-4: 不再主推后端无法稳定解析的示例路径

### TR-005: 可测试性与回归保障

**需求**：为问卷进入 Action 的能力补齐单元测试与链路测试。

**验收标准**：
- AC-TR-005-1: 为上下文聚合逻辑新增单元测试
- AC-TR-005-2: 为模板路径解析新增单元测试
- AC-TR-005-3: 为 TriggerAction → ActionExecutionService → HttpApiActionExecutor 链路新增测试
- AC-TR-005-4: 现有 static field / previousActionResult 相关测试不得回归失败
