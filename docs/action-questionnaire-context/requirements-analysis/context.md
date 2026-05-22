# Context: Action Questionnaire Context — 需求分析（最终版）

> 更新于 2026-05-18，反映当前已确认的需求边界。

## 业务目标

为 WFE Action 执行链路补齐问卷答案上下文能力，使 Action 在调用 CRM 等外部系统时，可以稳定读取当前 Case 下所有已完成 Stage 的问卷答案，避免手工转录或先同步成 Dynamic Fields。

---

## 核心用户故事摘要

| ID | 角色 | 目标 | 关键实现 |
|----|------|------|----------|
| US-001 | 管理员 / 集成人员 | 在 Action 中引用问卷答案 | TriggerAction 执行上下文注入问卷数据 |
| US-002 | 管理员 / 集成人员 | 按稳定标识取值 | 以 `questionId` 为主键，而非 `questionText` |
| US-003 | 管理员 / 集成人员 | 在 HTTP Action 中使用深路径模板 | 模板解析支持 `a.b.c` 和 map key 访问 |
| US-004 | 管理员 / 集成人员 | 配置时看到真实可用变量 | Variables Panel / 自动补全与后端能力对齐 |
| US-005 | 系统 | 保持现有链路兼容 | static field / previousActionResult 不破坏 |

---

## 默认上下文边界

| 项目 | v1 决策 |
|------|---------|
| 问卷来源范围 | 当前 Case 下所有已完成 Stage |
| 正式问卷取值主键 | `questionId` |
| 展示用途字段 | `questionText` |
| 模板未命中行为 | 替换为空字符串 + warning 日志 |
| 复杂题型承诺范围 | 不纳入本期强承诺 |

---

## 推荐问卷上下文结构

```json
{
  "questionnaireAnswers": [
    {
      "stageId": "456",
      "questionnaireId": "1001",
      "questionId": "2001",
      "questionText": "Company Name",
      "questionType": "text",
      "answer": "ABC Logistics"
    }
  ],
  "questionnaireAnswerMap": {
    "1001": {
      "2001": "ABC Logistics"
    }
  },
  "questionnaireAnswerByQuestionId": {
    "2001": "ABC Logistics"
  }
}
```

---

## 范围边界

| In Scope | Out of Scope |
|----------|--------------|
| TriggerAction 注入问卷答案上下文 | 问卷 `questionKey` 产品化能力 |
| HTTP Action 深路径模板解析 | 复杂题型到外部系统格式转换产品化 |
| 前端变量提示对齐 | 可视化字段映射器 |
| 兼容现有 static field / previousActionResult | Python Action 参数范式重构 |
| 单元测试和链路验证 | 多 Stage 冲突的高级策略配置 |
