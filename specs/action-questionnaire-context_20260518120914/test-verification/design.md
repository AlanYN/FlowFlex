# Design: Action Questionnaire Context — 测试用例 + 问题清单

## 1. 后端单元测试用例

### 1.1 ActionContextBuilder 测试

| TC-ID | 测试场景 | 输入 | 预期结果 |
|------|------|------|------|
| TC-BE-001 | 构建基础上下文 | onboarding / stage / actionDefinition / integration 存在 | 输出包含 OnboardingId、StageId、ConditionId、CaseCode、CaseName、WorkflowId |
| TC-BE-002 | 注入 static fields | fieldData 含 2 个字段 | 顶层 context 包含对应字段，旧字段名保持兼容 |
| TC-BE-003 | 注入 integrationToken | integration 可解析 token | context 含 `integrationToken` |
| TC-BE-004 | 注入 previousActionResult | previousActionResult 非空 | context 含 `previousActionResult`，且现有 `prev_` flatten 行为保持可用 |
| TC-BE-005 | 聚合所有已完成 Stage 的问卷 | 当前 Case 下有多个 completed stage | context 含 `questionnaireAnswers`、`questionnaireAnswerMap`、`questionnaireAnswerByQuestionId` |
| TC-BE-006 | 忽略未完成 Stage | Case 下含 pending / in-progress stage | 仅 completed stage 的问卷被纳入聚合 |
| TC-BE-007 | 多 Stage 同 questionId 冲突覆盖 | 两个 completed stage 返回同 questionId 不同值 | `questionnaireAnswerByQuestionId` 取完成时间更晚的值 |
| TC-BE-008 | 覆盖规则回退 | completion time 不可用 | 按设计约定回退排序，结果稳定可测试 |
| TC-BE-009 | 无问卷数据 | completed stage 无 questionnaire 或答案为空 | 返回空结构，不抛异常 |
| TC-BE-010 | 单个 stage 问卷读取失败处理 | 某 stage 读取为空或异常可控 | 不因空答案中断整体构建；行为与设计一致 |

### 1.2 TemplateVariableResolver 测试

| TC-ID | 测试场景 | 输入 | 预期结果 |
|------|------|------|------|
| TC-BE-020 | 解析顶层变量 | `{{CaseCode}}` | 替换为顶层字段值 |
| TC-BE-021 | 解析嵌套 previousActionResult | `{{previousActionResult.data.customerCode}}` | 正确返回嵌套值 |
| TC-BE-022 | 解析 questionId map 路径 | `{{questionnaireAnswerByQuestionId.2001}}` | 返回对应答案 |
| TC-BE-023 | 解析 questionnaire+question 双层 map | `{{questionnaireAnswerMap.1001.2001}}` | 返回对应答案 |
| TC-BE-024 | 同一字符串多个模板 | URL/body 中包含多个占位符 | 所有模板都被替换 |
| TC-BE-025 | 未命中路径 | 不存在的 path | 替换为空字符串 |
| TC-BE-026 | 未命中日志 | 不存在的 path | 记录 warning 日志，但不抛异常 |
| TC-BE-027 | 旧浅层模板兼容 | `{{WorkflowId}}` 等旧变量 | 保持可用 |

### 1.3 HttpApiActionExecutor 测试

| TC-ID | 测试场景 | 输入 | 预期结果 |
|------|------|------|------|
| TC-BE-030 | URL 模板替换 | URL 含 `{{CaseCode}}` | 请求 URL 被正确替换 |
| TC-BE-031 | Query/Params 模板替换 | params 含问卷路径模板 | 参数值被正确替换 |
| TC-BE-032 | Headers 模板替换 | headers 含 previousActionResult 路径 | header 值被正确替换 |
| TC-BE-033 | Body 模板替换 | body 含 `questionnaireAnswerByQuestionId.2001` | body 中正确带出答案 |
| TC-BE-034 | 未命中模板不报错 | body 含不存在路径 | 对应位置为空字符串，执行器不中断 |
| TC-BE-035 | 混合旧新模板 | 同时包含 `{{CaseCode}}` 和深路径模板 | 两类模板都能生效 |

### 1.4 TriggerAction 链路测试

| TC-ID | 测试场景 | 输入 | 预期结果 |
|------|------|------|------|
| TC-BE-040 | ActionExecutor 改为使用 Builder | 触发 TriggerAction | Builder 被调用一次，原本本地拼装不再扩散 |
| TC-BE-041 | 问卷上下文透传到执行器 | Builder 返回问卷 context | ActionExecutionService 收到完整 context |
| TC-BE-042 | 与 static field 兼容 | Builder 同时返回 static field + questionnaire | HTTP executor 可同时消费两类字段 |
| TC-BE-043 | 与 previousActionResult 兼容 | context 含 previousActionResult | 现有链式变量不回归 |
| TC-BE-044 | 无问卷时行为兼容 | Builder 返回空问卷结构 | Action 仍可正常执行 |

---

## 2. 前端手动验证用例

| TC-ID | 测试场景 | 操作 | 预期结果 |
|------|------|------|------|
| TC-FE-001 | VariablesPanel 展示问卷变量 | 打开 Action 配置变量面板 | 能看到 `questionnaireAnswerByQuestionId.xxx` / `questionnaireAnswerMap.xxx.xxx` 推荐路径 |
| TC-FE-002 | VariablesPanel 不再主推误导示例 | 查看问卷变量区 | 不再主推后端不稳定或不推荐的旧示例 |
| TC-FE-003 | 自动补全候选对齐 | 在 HTTP 配置输入框输入问卷前缀 | 可补全真实可用问卷变量 |
| TC-FE-004 | HttpConfig 示例文案对齐 | 查看 URL/body 提示文案 | 示例改为 `{{CaseCode}}`、`{{questionnaireAnswerByQuestionId.2001}}`、`{{previousActionResult.data.customerCode}}` |

---

## 3. 兼容性 / 降级专项验证

| TC-ID | 场景 | 模拟方式 | 预期行为 |
|------|------|------|------|
| TC-DG-001 | 当前 Case 没有已完成 Stage | 构造无 completed stage 数据 | 返回空问卷结构，不阻断 Action |
| TC-DG-002 | 已完成 Stage 存在空问卷 | completed stage 返回 Pending/空 answers | 返回空结构或部分结构，不抛异常 |
| TC-DG-003 | 模板路径不存在 | HTTP 配置中写不存在路径 | 替换为空字符串 + warning 日志 |
| TC-DG-004 | 问卷答案包含复杂值 | answer 为对象/数组 | 以 raw value 透传，不额外格式化，不阻断执行 |
| TC-DG-005 | 无问卷能力的旧 Action | 配置只依赖 static field/CaseCode | 旧 Action 行为保持不变 |

---

## 4. 问题清单

| 问题 ID | 严重度 | 描述 | 状态 |
|------|------|------|------|
| — | — | 暂无已知问题 | — |

> 问题清单将在测试执行过程中更新。
