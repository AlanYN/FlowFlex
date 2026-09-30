# 架构决策记录 — Univer 集成

> 最后更新：2026-09-22

---

## DD-01：文档数据不解析，直存 JSONB 字符串

**状态：** ✅ 已确认  
**决策：** `data_json` 字段 C# 类型为 `string`，SqlSugar 注解用 `ColumnDataType = "jsonb"` 但**不**加 `IsJson = true`。

**理由：**
- Univer 的 `IWorkbookData` / `IDocumentData` 结构由前端版本管理，后端无需理解
- 加 `IsJson = true` 会触发 SqlSugar 自动反序列化为 JToken，增加开销且格式化存在风险
- C# 接口透传字符串，零解析开销

**影响范围：** `DocumentSnapshot.cs`、`DocumentSnapshotService.cs`（SaveAsync 直接传字符串）

---

## DD-02：协同方案选简单乐观锁，不做完整 OT

**状态：** ✅ 已确认  
**决策：** SignalR Hub 用 `baseRev == currentRev` 做冲突检测，冲突返回 `cs_rej`，前端指数退避重试（最多 3 次：100 / 200 / 400ms）。

**理由：**
- 完整 OT（操作变换）工程量极大，属过度设计
- 业务场景：同时编辑同一文档通常 ≤5 人，冲突率低
- 重试机制足以覆盖绝大多数并发场景

**影响范围：** `CollabHub.cs`（Ingest 方法）、`useCollaboration.ts`（cs_rej 处理逻辑）

---

## DD-03：操作日志写入异步 fire-and-forget

**状态：** ✅ 已确认  
**决策：** 前端每次 MUTATION 后调用 `addOperationLog`，不 await，catch 静默。

**理由：**
- 操作日志是审计用途，不在编辑主流程的关键路径上
- 失败静默比让编辑卡顿更可接受
- 后续如需可靠性可升级为本地 IndexedDB 缓冲 + 批量上报

**影响范围：** `documentEditor.ts`（logMutation action）、`documentOperationLog.ts`（addOperationLog 函数）

---

## DD-04：列表 Store 与编辑器 Store 分离

**状态：** ✅ 已确认  
**决策：** `documentList.ts` 和 `documentEditor.ts` 分为两个独立 Pinia Store。

**理由：**
- 列表页（keepAlive）和编辑器页生命周期不同
- 编辑器离开时需 dispose Univer 实例，状态独立更清晰
- 避免编辑器的 `snapshotData`（大 JSON）污染列表页状态

**影响范围：** `documentList.ts`、`documentEditor.ts`

---

## DD-05：Tailwind 隔离使用 `important: '#app'`

**状态：** ✅ 已确认  
**决策：** `tailwind.config.ts` 添加 `important: '#app'`，不用 `prefix` 方案。

**理由：**
- 项目现有代码大量使用无前缀 Tailwind 类，加 prefix 会破坏所有已有样式
- `important: '#app'` 只提升优先级，不改变类名，改动最小、风险最低
- Univer 内部样式有自己的命名空间，不受影响

**影响范围：** `tailwind.config.ts`（一行改动）

---

## DD-06：DocumentCard 和新建弹窗放在视图目录而非全局组件

**状态：** ✅ 已确认  
**决策：** `DocumentCard.vue` 和 `CreateDocumentDialog.vue` 放在 `views/document/list/`，不放 `components/global/`。

**理由：**
- 两者仅在文档管理模块使用，无跨模块复用需求
- 全局组件应满足"多处复用"条件，否则增加不必要的全局注册开销

**影响范围：** 组件文件路径
