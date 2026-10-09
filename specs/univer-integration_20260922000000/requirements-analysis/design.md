# Design — Univer 集成技术分析与功能规格

> 阶段：requirements-analysis  
> 模块：univer-integration  
> 创建日期：2026-09-22

---

## 一、功能优先级矩阵（MoSCoW）

| 功能 | 优先级 | 说明 |
|------|--------|------|
| 文档列表页（搜索/筛选/分页） | Must | 基础功能入口 |
| 新建文档弹窗 | Must | 创建流程必要 |
| Sheet 编辑器（全功能） | Must | 核心 P0 需求 |
| Doc 编辑器（全功能） | Must | 核心 P0 需求 |
| 文档快照存储（Save/Load） | Must | 数据持久化 |
| 操作日志记录 | Must | 审计追溯 |
| 多人实时协同（SignalR + 乐观锁） | Must | 协作核心 |
| Slide 编辑器 | Should | P1，可延后 |
| 完整 OT 操作变换 | Won't | 超出范围，简单乐观锁够用 |

---

## 二、功能模块分解

### 2.1 文档管理模块（前端）

| 模块 | 文件 | 说明 |
|------|------|------|
| 文档列表页 | `views/document/list/index.vue` | 卡片展示、搜索、筛选、排序、分页 |
| 新建文档弹窗 | `views/document/list/CreateDocumentDialog.vue` | 名称 + 类型选择 |
| 文档编辑器页 | `views/document/editor/index.vue` | 顶栏 + Univer 编辑区 |
| 列表 Store | `stores/modules/documentList.ts` | 列表状态管理 |
| 编辑器 Store | `stores/modules/documentEditor.ts` | 编辑器状态管理 |
| 路由模块 | `router/routers/modules/document.ts` | `/documents` 独立菜单 |

### 2.2 Univer 编辑器组件（前端）

| 组件 | 文件 | 说明 |
|------|------|------|
| Sheet 编辑器 | `components/UniverEditor/UniverSheet.vue` | 封装 createUniver + 全套 Sheets Preset |
| Doc 编辑器 | `components/UniverEditor/UniverDoc.vue` | 封装 createUniver + 全套 Docs Preset + Worker |
| Slide 编辑器 | `components/UniverEditor/UniverSlide.vue` | 底层插件模式，无 Preset |
| 协同 Composable | `composables/useCollaboration.ts` | WebSocket 接线 + 乐观锁 |

### 2.3 API 层（前端）

| 文件 | 接口功能 |
|------|---------|
| `apis/ow/documentSnapshot.ts` | 增删改查快照、分页查询 |
| `apis/ow/documentOperationLog.ts` | 上报操作日志、查询历史 |

### 2.4 后端服务层

| 层级 | 文件 | 说明 |
|------|------|------|
| 实体 | `DocumentSnapshot.cs` / `DocumentOperationLog.cs` | ORM 映射，`ff_document_snapshot` / `ff_document_operation_log` |
| Repository | `IDocumentSnapshotRepository` / 实现 | 数据访问层 |
| Service | `IDocumentSnapshotService` / 实现 | 业务逻辑，含 Upsert |
| Service | `IDocumentOperationLogService` / 实现 | 操作日志写入和查询 |
| Controller | `DocumentSnapshotController` | 路由 `ow/document-snapshots/v1` |
| Controller | `DocumentOperationLogController` | 路由 `ow/document-operation-logs/v1` |
| SignalR Hub | `CollabHub.cs` | 协同编辑，路由 `/collab/{unitId}` |
| Migration | `Migration_20260922000001_CreateDocumentTables.cs` | 建表 SQL |

---

## 三、数据模型

### 3.1 ff_document_snapshot

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | BIGINT (Snowflake) | 主键 |
| `unit_id` | VARCHAR(100) | Univer 文档唯一 ID（前端 UUID） |
| `title` | VARCHAR(200) | 文档标题 |
| `doc_type` | VARCHAR(20) | Sheet / Doc / Slide |
| `data_json` | JSONB | IWorkbookData / IDocumentData JSON |
| `revision` | INT | 版本号（协同用，从 1 递增） |
| `entity_type` | VARCHAR(50) | 关联业务实体类型（可选） |
| `entity_id` | BIGINT | 关联业务实体 ID（可选） |
| `tenant_id` | VARCHAR(100) | 多租户 |
| `app_code` | VARCHAR(100) | 多租户 |
| `is_valid` | BOOLEAN | 软删除 |
| 审计字段 | — | create_date / modify_date / create_by 等 |

唯一索引：`(unit_id, tenant_id, app_code) WHERE is_valid = TRUE`

### 3.2 ff_document_operation_log

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | BIGINT (Snowflake) | 主键 |
| `unit_id` | VARCHAR(100) | 关联文档 |
| `operator_user_id` | VARCHAR(100) | 操作用户 ID |
| `operator_user_name` | VARCHAR(100) | 操作用户名 |
| `op_timestamp` | BIGINT | 操作时间戳（毫秒） |
| `mutation_id` | VARCHAR(200) | Univer MutationId |
| `params_json` | JSONB | 操作参数 |
| `revision` | INT | 协同版本号 |
| 多租户 + 审计 | — | 同上 |

索引：`(unit_id, tenant_id, app_code, create_date DESC)`

---

## 四、关键 API 端点

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `ow/document-snapshots/v1/:unitId` | 加载文档快照 |
| POST | `ow/document-snapshots/v1` | 创建新文档 |
| POST | `ow/document-snapshots/v1/:unitId/save` | 保存/更新快照（Upsert） |
| DELETE | `ow/document-snapshots/v1/:id` | 删除文档 |
| POST | `ow/document-snapshots/v1/query` | 分页查询（含关键词/类型/排序） |
| POST | `ow/document-operation-logs/v1` | 上报操作日志 |
| GET | `ow/document-operation-logs/v1/:unitId` | 查询操作历史 |
| GET | `ow/document-operation-logs/v1/:unitId/revision` | 获取当前版本号 |
| WS | `/collab/:unitId` | SignalR 协同编辑 WebSocket |

---

## 五、风险与约束

| 风险 | 说明 | 缓解措施 |
|------|------|---------|
| Tailwind base reset 破坏 Univer UI | Univer 内部有完整样式系统 | `important: '#app'` 方案，Phase 3 优先处理 |
| workbookData 进响应式系统性能差 | 大 JSON 对象 | 组件内直接 pass，不用 `ref()` 包裹 |
| Web Worker MIME 类型 | 生产环境 Kestrel | 确认静态文件 MIME 配置 |
| SignalR WebSocket 兼容性 | `@univerjs/network` 用原生 WebSocket | SignalR Hub 暴露 WebSocket 端点即可兼容 |
| 协同并发冲突 | 多人同时编辑 | 简单乐观锁 + 重试，不做完整 OT |
