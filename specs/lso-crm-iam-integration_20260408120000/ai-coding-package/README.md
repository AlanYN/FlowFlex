# AI Coding Package: LSO Parcel CRM & IAM Integration Actions

## 项目概述

在 WFE（FlowFlex）的 LSO Parcel 公司下，当 "LSO Customer Onboarding" Workflow 的 Case Complete 时，通过 Stage Condition → TriggerAction 机制，依次调用 CRM API 创建 Customer 和 IAM API 创建 External User。

## 技术栈
- C# / ASP.NET Core / SqlSugar ORM / PostgreSQL
- 外部 API: CRM (crm-dev.item.pub), IAM (id-dev.item.pub)
- 认证: OAuth2 Client Credentials

## 如何使用本规格包

1. 阅读 `requirements.md` 了解完整需求和验收标准
2. 阅读 `design.md` 了解代码修改方案和 Action Config
3. 按照 `tasks.md` 中的任务顺序执行开发

## 文件阅读顺序

`requirements.md` → `design.md` → `tasks.md`

## 子规格来源索引

| 文件 | 来源阶段 |
|------|---------|
| requirements.md | requirements-analysis + technical-design + test-verification |
| design.md | technical-design + test-verification |
| tasks.md | technical-design + test-verification |

## 已有配置 ID（重要）

| 实体 | ID |
|------|-----|
| Workflow | 2042115982671089664 |
| Stage | 2042116465028632576 |
| Stage Condition | 2042118279430017024 |
| Integration | 1994239810054787072 |
| 占位 ActionDefinition（需替换） | 2004482114174717952 |
