# Context — 技术方案摘要

> 供下游阶段（test-verification）读取  
> 最后更新：2026-09-22

---

## 一、技术栈

| 层 | 技术 |
|----|------|
| 前端 | Vue 3.5 + Vite 5.4 + Element Plus 2.9 + Pinia 2.2 |
| 文档引擎 | Univer 1.0.0-rc.0（@univerjs/presets 系列） |
| 协同传输 | @univerjs/network（WebSocket）+ ASP.NET Core SignalR |
| 后端 | ASP.NET Core 8.0 + SqlSugar ORM + AutoMapper |
| 数据库 | PostgreSQL，JSONB 存文档快照 |

---

## 二、新增文件清单（测试覆盖范围参考）

**后端（18 个新增文件）：**
- 实体：`DocumentSnapshot.cs`、`DocumentOperationLog.cs`
- Repository：`IDocumentSnapshotRepository`、`IDocumentOperationLogRepository`（含实现）
- Service：`IDocumentSnapshotService`、`IDocumentOperationLogService`（含实现）
- Controller：`DocumentSnapshotController`、`DocumentOperationLogController`
- Hub：`CollabHub`
- Migration：`Migration_20260922000001_CreateDocumentTables`

**前端（12 个新增文件）：**
- 组件：`UniverSheet.vue`、`UniverDoc.vue`、`UniverSlide.vue`
- Composable：`useCollaboration.ts`
- API：`documentSnapshot.ts`、`documentOperationLog.ts`
- Store：`documentList.ts`、`documentEditor.ts`
- 视图：`list/index.vue`、`DocumentCard.vue`、`CreateDocumentDialog.vue`、`editor/index.vue`
- 路由：`document.ts`

---

## 三、关键 API 端点

| 方法 | 路径 | 用途 |
|------|------|------|
| GET | `ow/document-snapshots/v1/:unitId` | 加载快照 |
| POST | `ow/document-snapshots/v1` | 创建文档 |
| POST | `ow/document-snapshots/v1/:unitId/save` | Upsert 保存 |
| DELETE | `ow/document-snapshots/v1/:id` | 软删除 |
| POST | `ow/document-snapshots/v1/query` | 分页查询 |
| POST | `ow/document-operation-logs/v1` | 上报日志 |
| GET | `ow/document-operation-logs/v1/:unitId` | 查询历史 |
| WS | `/collab/:unitId` | 协同 Hub |

---

## 四、数据库摘要

| 表 | 关键约束 |
|----|---------|
| `ff_document_snapshot` | 复合唯一索引 (unit_id, tenant_id, app_code)，软删除 |
| `ff_document_operation_log` | 查询索引 (unit_id, tenant_id, app_code, create_date DESC) |

---

## 五、关键业务规则（测试必须覆盖）

| 规则 | 描述 |
|------|------|
| BR-01 | `SaveAsync` 按 unit_id Upsert：存在→UPDATE，不存在→INSERT |
| BR-02 | DELETE 为软删除：`is_valid = FALSE`，不物理删除 |
| BR-03 | 协同乐观锁：`baseRev != currentRev` 时返回 cs_rej |
| BR-04 | 操作日志 fire-and-forget：失败静默，不抛异常至前端 |
| BR-05 | 多租户隔离：所有查询自动附加 tenant_id + app_code 条件 |
| BR-06 | 标题空值保护：PATCH title 时若 title 为空，返回 400 |

---

## 六、风险摘要

| ID | 风险 | 缓解措施 |
|----|------|---------|
| R-01 | Tailwind 破坏 Univer UI | `important: '#app'`，Phase 3 优先处理 |
| R-02 | workbookData 误放 ref | Code Review checklist 加此项 |
| R-03 | 编辑器高度为 auto | 固定 `calc(100vh - 56px)` |
| R-06 | Univer 包体积大 | 路由级懒加载，编辑器页独立 chunk |
