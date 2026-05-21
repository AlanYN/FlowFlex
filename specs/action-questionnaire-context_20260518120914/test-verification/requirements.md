# Requirements: Action Questionnaire Context — 测试覆盖范围

## 测试目标

验证“问卷答案进入 Action 执行上下文”能力的正确性、可回归性和兼容性，确保：

1. TriggerAction 执行时能注入当前 Case 下所有已完成 Stage 的问卷答案
2. 多 Stage 聚合与覆盖规则稳定且可解释
3. HTTP Action 能正确解析问卷相关深路径模板
4. 现有 static field / integrationToken / previousActionResult 能力不回归
5. 前端变量提示与后端真实可用路径保持一致

---

## 测试覆盖范围

### 后端测试

| 模块 | 覆盖内容 |
|------|------|
| ActionContextBuilder | 基础字段注入、已完成 Stage 收集、问卷聚合、空问卷场景、覆盖优先级 |
| TemplateVariableResolver | 顶层字段、嵌套路径、数字 key 路径、未命中行为 |
| HttpApiActionExecutor | URL / Params / Headers / Body 模板替换，旧模板兼容 |
| ActionExecutor | TriggerAction 链路接入 Builder，问卷上下文透传，兼容 static field / previousActionResult |

### 前端测试

| 模块 | 覆盖内容 |
|------|------|
| VariablesPanel | 问卷变量展示是否改为真实可用结构 |
| VariableAutoComplete | 自动补全候选是否包含推荐问卷路径 |
| HttpConfig | 示例文案是否与后端真实上下文一致 |

---

## 质量目标

| 指标 | 目标 |
|------|------|
| 后端单元测试覆盖 | 覆盖 Builder / Resolver / HTTP 模板解析核心分支 |
| 链路测试覆盖 | 覆盖 TriggerAction → ActionExecutionService → HttpApiActionExecutor 关键链路 |
| 覆盖规则验证 | 覆盖同 questionId 的多 Stage 冲突场景 |
| 未命中路径验证 | 覆盖替换为空字符串 + warning 日志 |
| 兼容性验证 | 现有 static field / integrationToken / previousActionResult 行为不变 |
| 前端验证 | 手动验证变量提示、自动补全、示例文案三处对齐 |
