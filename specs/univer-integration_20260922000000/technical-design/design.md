# Design — 技术方案与架构设计

> 阶段：technical-design  
> 模块：univer-integration  
> 创建日期：2026-09-22

---

## 一、技术选型

| 层 | 技术 | 决策依据 |
|----|------|---------|
| 前端框架 | Vue 3.5 + Vite 5.4（现有） | 复用现有技术栈，无需引入新框架 |
| UI 组件库 | Element Plus 2.9（现有） | 弹窗/表单/分页全部用 ElPlus 现有组件 |
| 在线文档引擎 | Univer 1.0.0-rc.0（新引入） | Apache 2.0，免费商用，支持 Sheet/Doc/Slide |
| 状态管理 | Pinia 2.2（现有） | 两个 Store（documentList / documentEditor） |
| 协同传输 | @univerjs/network + SignalR（新引入） | Univer 官方网络层 + .NET 原生协同方案 |
| 后端框架 | ASP.NET Core 8.0（现有） | Controller→Service→Repository 标准分层 |
| ORM | SqlSugar（现有） | 实体标注 SugarColumn，JSONB 直接存字符串 |
| 数据库 | PostgreSQL（现有） | JSONB 存文档快照，btree 索引优化查询 |

---

## 二、整体架构

### 前端分层

```
views/document/list/index.vue          ← 列表页（使用 documentListStore）
views/document/list/CreateDocumentDialog.vue
views/document/list/DocumentCard.vue
views/document/editor/index.vue        ← 编辑器页（使用 documentEditorStore）
        │
components/UniverEditor/
  UniverSheet.vue  UniverDoc.vue  UniverSlide.vue
        │
composables/useCollaboration.ts        ← WebSocket 协同接线
        │
stores/modules/documentList.ts         ← id: item-wfe-document-list
stores/modules/documentEditor.ts       ← id: item-wfe-document-editor
        │
apis/ow/documentSnapshot.ts
apis/ow/documentOperationLog.ts
        │
router/routers/modules/document.ts     ← /documents 独立菜单
```

### 后端分层

```
DocumentSnapshotController  DocumentOperationLogController
  (ow/document-snapshots/v1)  (ow/document-operation-logs/v1)
        │
IDocumentSnapshotService    IDocumentOperationLogService
        │
DocumentSnapshotService     DocumentOperationLogService
        │
IDocumentSnapshotRepository IDocumentOperationLogRepository
        │
DocumentSnapshotRepository  DocumentOperationLogRepository  ← BaseRepository<T>
        │
PostgreSQL: ff_document_snapshot  ff_document_operation_log

CollabHub (SignalR /collab/{unitId})
  ← Join / Ingest（乐观锁）/ UpdateCursor / OnDisconnectedAsync
```

---

## 三、关键架构决策

### DD-01：文档数据不解析，直存 JSONB 字符串

**决策：** `data_json` 字段声明 `ColumnDataType = "jsonb"` 但 C# 类型为 `string`，不加 `IsJson = true`。

**理由：** Univer 的 `IWorkbookData` / `IDocumentData` 结构由前端版本管理，后端无需理解内容，直接存取字符串即可。加 `IsJson = true` 会触发 SqlSugar 自动反序列化为 JToken，反而增加不必要的开销和格式化风险。

### DD-02：协同方案选简单乐观锁，不做完整 OT

**决策：** SignalR Hub 用 `baseRev == currentRev` 做冲突检测，冲突时返回 `cs_rej`，前端自动重试。

**理由：** 完整 OT（操作变换）工程量极大（Google Docs 级别），对低并发业务场景（同时编辑同一文档的人数通常 ≤5）过度设计。乐观锁 + 重试在冲突率低时体验无损。

### DD-03：操作日志写入异步 fire-and-forget

**决策：** 前端每次 MUTATION 后调用 `addOperationLog`，不 await，不处理异常。

**理由：** 操作日志是审计用途，不影响编辑主流程。若日志上报失败，宁可丢一条日志也不应让编辑卡顿。

### DD-04：列表 Store 和编辑器 Store 分离

**决策：** `documentList.ts`（列表状态）和 `documentEditor.ts`（编辑器状态）分为两个独立 Store。

**理由：** 列表页和编辑器页生命周期不同，编辑器页离开时需 dispose Univer 实例，状态不共用更清晰。

### DD-05：Tailwind 隔离使用 `important: '#app'`

**决策：** `tailwind.config.ts` 添加 `important: '#app'`，不用 `prefix` 方案。

**理由：** 项目现有代码大量使用无前缀的 Tailwind 类，加 prefix 会破坏所有已有样式；`important: '#app'` 只提升 Tailwind 规则优先级，不改变类名，改动最小。

---

## 四、风险登记表

| ID | 风险描述 | 概率 | 影响 | 缓解措施 |
|----|---------|------|------|---------|
| R-01 | Tailwind base reset 破坏 Univer UI | 高 | 高 | 优先在 Phase 3 配置 `important: '#app'`，上线前回归所有已有页面样式 |
| R-02 | `workbookData` 误放入 `ref()` 导致性能崩溃 | 中 | 高 | 组件内明确注释「禁止用 ref() 包裹」；Code Review checklist 加此项 |
| R-03 | Univer 容器高度为 auto 导致编辑器不渲染 | 中 | 高 | 固定高度 `calc(100vh - 56px)` 写入 KD-01，UniverSheet 组件 prop 有 default 值 |
| R-04 | 生产环境 Kestrel 不返回正确 JS MIME | 低 | 中 | Web Worker 功能依赖此配置，上线前确认静态文件 MIME 映射 |
| R-05 | 协同乐观锁高并发时重试风暴 | 低 | 中 | 前端重试加指数退避（最多 3 次，间隔 100/200/400ms） |
| R-06 | Univer 包体积较大影响首屏 | 中 | 中 | 路由级懒加载，编辑器页单独 chunk；列表页不引入 Univer 包 |
