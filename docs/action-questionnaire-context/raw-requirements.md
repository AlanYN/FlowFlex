# 原始需求存档

## 2026-05-18 — 初始需求

### 背景

当前 WFE 的问卷答案虽然可以在规则判断链路中被读取，但在调用 TriggerAction / HTTP Action 时，无法稳定地作为参数传入外部系统调用。现状是 Dynamic Fields 可以传入 Action，但问卷类型的值不行，导致像 CRM Customer 创建这类场景无法直接复用问卷填写结果。

### 用户澄清后的关键决策

1. 本次需求聚焦：让问卷答案能够进入 Action 执行上下文，并被 HTTP Action 等执行器引用
2. v1 正式以 `questionId` 作为问卷取值主键，不使用 `questionText` 作为正式映射 key
3. 模板变量未命中时，按“替换为空字符串 + warning 日志”方向设计
4. 复杂题型（附件 / 复杂 grid）不纳入本期强承诺范围
5. **Action 取值存在跨 Stage 需求，v1 默认需要覆盖当前 Case 下所有已完成 Stage 的问卷答案**

### 核心需求

- Stage Condition 触发 TriggerAction 时，执行上下文中应包含问卷答案
- Action 配置应可通过模板路径引用问卷答案
- HTTP Action 模板解析需支持深路径
- 前端变量面板与自动补全必须和后端真实能力一致
- 保持现有 static field / previousActionResult 兼容
