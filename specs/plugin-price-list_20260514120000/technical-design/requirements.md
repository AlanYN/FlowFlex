# Technical Requirements: Plugin Price List

## 技术方案需求

### TR-001: 后端 API 层

**需求**：在 FlowFlex 后端新增 Plugin Price List 模块，提供 3 个 REST API 端点

**验收标准**：
- AC-TR-001-1: GET `/api/ow/plugin-price-lists/v1?caseCode={caseCode}` 返回 Price List 数据
- AC-TR-001-2: POST `/api/ow/plugin-price-lists/v1` 保存/更新 Price List
- AC-TR-001-3: POST `/api/ow/plugin-price-lists/v1/submit` 标记为已提交
- AC-TR-001-4: 所有 API 需要 JWT 认证（`[Authorize]`）
- AC-TR-001-5: 响应格式遵循 FlowFlex 统一响应模式（`Success<T>(data)`）

### TR-002: 数据持久化层

**需求**：新建 PostgreSQL 表存储 Price List 数据

**验收标准**：
- AC-TR-002-1: 表名 `ff_plugin_price_lists`，遵循项目命名规范
- AC-TR-002-2: 主键使用雪花 ID（long）
- AC-TR-002-3: `data` 字段使用 JSONB 类型存储 sections 数据
- AC-TR-002-4: 同一租户 + 应用下，`case_code` 唯一（软删除记录除外）
- AC-TR-002-5: 包含完整审计字段和租户隔离字段
- AC-TR-002-6: 通过 Migration 自动建表

### TR-003: 权限集成

**需求**：复用 WFE 现有权限体系，基于 Case 权限判断读写权限

**验收标准**：
- AC-TR-003-1: 通过 `caseCode` 查找对应的 Onboarding 记录
- AC-TR-003-2: 调用 `IPermissionService.CheckCaseAccessAsync()` 检查用户权限
- AC-TR-003-3: `CanOperate = true` → 返回 `permission: "write"`
- AC-TR-003-4: `CanView = true && CanOperate = false` → 返回 `permission: "read"`
- AC-TR-003-5: `CanView = false` → 返回 403 Forbidden
- AC-TR-003-6: GET API 响应中包含 `permission` 字段

### TR-004: 前端 API 对接

**需求**：将 Amanda 的前端原型从 localStorage 改为调用后端 API

**验收标准**：
- AC-TR-004-1: `onMounted` 时调用 GET API 加载数据
- AC-TR-004-2: `handleSave` 时调用 POST API 保存数据
- AC-TR-004-3: `handleSubmit` 时调用 POST submit API
- AC-TR-004-4: 请求头自动携带 JWT token（同域名 cookie）
- AC-TR-004-5: 根据 `permission` 字段切换只读/编辑模式
- AC-TR-004-6: 加载/保存/提交时显示 loading 状态
- AC-TR-004-7: 错误时显示友好提示

### TR-005: 静态文件部署

**需求**：将前端 dist 文件部署到 WFE 前端项目的 public 目录

**验收标准**：
- AC-TR-005-1: `vite.config.js` 的 `base` 配置为 `/plugins/price-list/`
- AC-TR-005-2: dist 文件放置在 `packages/flowFlex-common/public/plugins/price-list/`
- AC-TR-005-3: 通过 `/plugins/price-list/index.html?caseCode=xxx` 可访问
- AC-TR-005-4: Nginx `try_files $uri` 直接匹配静态文件，不走 WFE SPA Router
