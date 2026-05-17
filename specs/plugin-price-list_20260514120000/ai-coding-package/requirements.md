# Requirements: Plugin Price List — AI Coding Package

<!-- 来源: requirements-analysis/requirements.md -->

## 用户故事与验收标准

### US-001: 加载已保存的 Price List

**As a** WFE Case 的 Stage Assignee / Co-assignee  
**I want to** 从 Billing Setup Stage 的 Quick Link 进入 Price List 页面时，自动加载该 Case 已保存的数据  
**So that** 我可以继续编辑之前的工作

**AC：**
- AC-001-1: 页面加载时，根据 URL 中的 caseCode 参数调用 GET API 获取数据
- AC-001-2: 如果该 Case 有已保存的数据，自动填充到页面所有字段
- AC-001-3: 如果该 Case 无数据（data=null），显示空白页面
- AC-001-4: 加载过程中显示 loading 状态

### US-002: 保存 Price List

**As a** 有写权限的用户  
**I want to** 点击 Save 按钮将当前配置保存到服务器  
**So that** 数据不会因浏览器刷新或切换用户而丢失

**AC：**
- AC-002-1: 点击 Save 时，将完整 Price List JSON 通过 POST API 发送到后端
- AC-002-2: 保存成功后显示成功提示
- AC-002-3: 保存失败时显示错误提示，数据不丢失
- AC-002-4: 同一 Case 重复保存为更新操作（upsert）

### US-003: 提交 Price List

**As a** 有写权限的用户  
**I want to** 点击 Submit 按钮将 Price List 标记为已提交  
**So that** 下游团队知道这份价目表已确认

**AC：**
- AC-003-1: Submit 时调用 POST /submit API，状态从 draft 改为 submitted
- AC-003-2: 提交成功后，页面切换为只读模式
- AC-003-3: 提交失败时显示错误提示，状态不变
- AC-003-4: 数据库 status 字段预留 draft → submitted → approved 状态流转空间

### US-004: 权限控制 — 只读模式

**AC：**
- AC-004-1: API 返回当前用户的权限级别（write / read）
- AC-004-2: read 权限：所有 input/select/textarea 设为 disabled
- AC-004-3: read 权限：隐藏 Save、Submit、Add Item、Delete 按钮

### US-005: 权限控制 — 无权限用户

**AC：**
- AC-005-1: 无权限用户访问 API 时返回 403 Forbidden
- AC-005-2: 前端收到 403 后显示"无权限访问"提示页面

### US-006: 后端数据持久化

**AC：**
- AC-006-1: 数据库表 `ff_plugin_price_lists`，一个 Case 对应一条记录
- AC-006-2: 同一租户 + 应用下，case_code 唯一（软删除记录除外）
- AC-006-3: 记录包含完整审计字段
- AC-006-4: 支持租户隔离（tenant_id + app_code 过滤）

### US-007: WFE 集成 — Quick Link 入口

**AC：**
- AC-007-1: 前端静态文件部署在 WFE 前端 `public/plugins/price-list/` 目录
- AC-007-2: 通过 `/plugins/price-list/index.html?caseCode={caseCode}&customerCode={code}&customerName={name}` 访问
- AC-007-3: 页面共享 WFE 的 JWT 认证

---

<!-- 来源: technical-design/requirements.md -->

## 技术需求

### TR-001: 后端 API 层

- GET `/api/ow/plugin-price-lists/v1?caseCode={caseCode}`
- POST `/api/ow/plugin-price-lists/v1`
- POST `/api/ow/plugin-price-lists/v1/submit`
- 所有 API 需要 JWT 认证 + 统一响应格式

### TR-002: 数据持久化层

- 表名 `ff_plugin_price_lists`，雪花 ID 主键
- `data` 字段 JSONB，唯一约束 `(tenant_id, app_code, case_code) WHERE is_valid = TRUE`

### TR-003: 权限集成

- caseCode → 查 ff_onboarding → IPermissionService.CheckCaseAccessAsync()
- CanOperate → "write"，CanView only → "read"，无权限 → 403

### TR-004: 前端 API 对接

- localStorage → fetch API，请求头自动携带 JWT
- 根据 permission 字段切换只读/编辑模式

### TR-005: 静态文件部署

- Vite base = `/plugins/price-list/`
- dist 放 `packages/flowFlex-common/public/plugins/price-list/`

---

<!-- 来源: test-verification/requirements.md -->

## 测试覆盖范围

- 后端 API：14 个测试用例（GET 6 + POST Save 4 + POST Submit 3 + 租户隔离 1）
- 前端交互：4 个测试用例
- 全部手动验证（Swagger + 浏览器）
