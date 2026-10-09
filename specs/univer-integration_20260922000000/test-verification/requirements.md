# Requirements — 测试覆盖范围

> 阶段：test-verification  
> 模块：univer-integration  
> 创建日期：2026-09-22

---

## 一、测试范围声明

### 纳入测试

| 范围 | 说明 |
|------|------|
| 后端 Service 层单元测试 | DocumentSnapshotService、DocumentOperationLogService 所有方法 |
| 后端 Controller 层集成测试 | 8 个 REST 端点的请求/响应/错误码 |
| 前端组件测试 | CreateDocumentDialog 表单验证、DocumentCard 交互、编辑器页导航守卫 |
| 前端 Store 测试 | documentListStore、documentEditorStore 关键 action |
| 业务规则验证 | BR-01 ~ BR-06 逐条验证 |
| 协同逻辑测试 | CollabHub 乐观锁、cs_ack/cs_rej 消息 |
| 多租户隔离测试 | 不同 tenant 间数据不互见 |
| 样式回归测试 | Tailwind 配置修改后现有页面样式无损 |

### 排除测试（本期不覆盖）

| 排除项 | 原因 |
|--------|------|
| Univer 编辑器内部功能（公式/条件格式等） | Univer 自身有完整测试，属第三方库 |
| 完整 OT 操作变换正确性 | 本期不实现 OT |
| 前端 E2E 测试（Playwright/Cypress） | 本期范围外，后续迭代补充 |
| Slide 编辑器功能 | P1 功能，延后测试 |

---

## 二、质量目标

| 指标 | 目标 |
|------|------|
| 后端单元测试覆盖率 | ≥ 80%（Service 层） |
| 所有 BR 业务规则 | 100% 覆盖 |
| 关键 API 端点 | 每个端点至少 1 个 happy path + 1 个 error case |
| 样式回归 | 现有页面样式无变化（视觉对比） |
