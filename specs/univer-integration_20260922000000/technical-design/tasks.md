# Tasks — 开发任务列表

> 阶段：technical-design  
> 模块：univer-integration  
> 创建日期：2026-09-22

---

## 第一组：环境搭建与基础配置

- [ ] T-ENV-01：安装 Univer 依赖包（@univerjs/presets + Sheets/Docs/Slides 全套 + @univerjs/network）
  - 工作目录：`packages/flowFlex-common/`
  - 命令：`npm install @univerjs/presets @univerjs/preset-sheets-core @univerjs/preset-sheets-drawing @univerjs/preset-sheets-conditional-formatting @univerjs/preset-sheets-filter @univerjs/preset-sheets-hyper-link @univerjs/preset-sheets-data-validation @univerjs/preset-sheets-find-replace @univerjs/preset-sheets-note @univerjs/preset-sheets-sort @univerjs/preset-sheets-table @univerjs/preset-sheets-thread-comment @univerjs/preset-docs-core @univerjs/preset-docs-drawing @univerjs/preset-docs-hyper-link @univerjs/preset-docs-thread-comment @univerjs/slides @univerjs/slides-ui @univerjs/docs @univerjs/docs-ui @univerjs/drawing @univerjs/engine-render @univerjs/ui @univerjs/network`

- [ ] T-ENV-02：配置 Tailwind CSS 隔离
  - 文件：`packages/flowFlex-common/tailwind.config.ts`
  - 修改：在 `export default` 中添加 `important: '#app'`
  - 验证：确认现有页面样式不变，Univer 内部样式不被覆盖

- [ ] T-ENV-03：配置 Vite optimizeDeps
  - 文件：`packages/flowFlex-common/vite.config.ts`
  - 修改：`optimizeDeps.include` 追加 `'@univerjs/presets'`、`'@univerjs/preset-sheets-core'`、`'@univerjs/preset-docs-core'`

- [ ] T-ENV-04：在后端 PermissionConsts.cs 新增 DOCUMENTS 权限常量
  - 文件：`packages/flowFlex-backend/Domain/Shared/Const/PermissionConsts.cs`
  - 新增：`public static class Document { public const string Read = "DOCUMENT:READ"; public const string Create = "DOCUMENT:CREATE"; public const string Update = "DOCUMENT:UPDATE"; public const string Delete = "DOCUMENT:DELETE"; }`

- [ ] T-ENV-05：在前端 i18n 语言包中新增文档管理路由文案
  - 新增：`sys.router.document: '文档管理'`

---

## 第二组：后端数据层

- [ ] T-BE-01：创建 DocumentSnapshot 实体
  - 文件：`packages/flowFlex-backend/Domain/Entities/OW/DocumentSnapshot.cs`
  - 继承：`EntityBaseCreateInfo`
  - 表名：`ff_document_snapshot`
  - 字段：UnitId(varchar100)、Title(varchar200)、DocType(varchar20)、DataJson(jsonb string)、Revision(int=1)、EntityType(varchar50)、EntityId(long)

- [ ] T-BE-02：创建 DocumentOperationLog 实体
  - 文件：`packages/flowFlex-backend/Domain/Entities/OW/DocumentOperationLog.cs`
  - 继承：`EntityBaseCreateInfo`
  - 表名：`ff_document_operation_log`
  - 字段：UnitId(varchar100)、OperatorUserId(varchar100)、OperatorUserName(varchar100)、OpTimestamp(long)、MutationId(varchar200)、ParamsJson(jsonb string)、Revision(int)

- [ ] T-BE-03：创建 Repository 接口
  - `Domain/Repository/OW/IDocumentSnapshotRepository.cs`：方法 GetByUnitIdAsync / GetByEntityAsync / UpsertByUnitIdAsync / QueryAsync
  - `Domain/Repository/OW/IDocumentOperationLogRepository.cs`：方法 GetLogsByUnitIdAsync / GetRevisionAsync / AddAsync

- [ ] T-BE-04：创建 Repository 实现
  - `SqlSugarDB/Repositories/OW/DocumentSnapshotRepository.cs`（继承 BaseRepository<DocumentSnapshot>）
  - `SqlSugarDB/Repositories/OW/DocumentOperationLogRepository.cs`（继承 BaseRepository<DocumentOperationLog>）

- [ ] T-BE-05：创建 Migration 并注册
  - 文件：`SqlSugarDB/Migrations/Migration_20260922000001_CreateDocumentTables.cs`
  - 内容：建表 ff_document_snapshot + ff_document_operation_log + 复合唯一索引 + 查询索引
  - 注册：在 MigrationManager.cs migrations 数组末尾追加注册

---

## 第三组：后端 API 层

- [ ] T-BE-06：创建 DTO
  - 目录：`Application.Contracts/Dtos/OW/DocumentSnapshot/`
  - 文件：DocumentSnapshotInputDto.cs / DocumentSnapshotOutputDto.cs / DocumentSnapshotQueryRequest.cs / DocumentOperationLogInputDto.cs / DocumentOperationLogOutputDto.cs

- [ ] T-BE-07：创建 Service 接口
  - `Application.Contracts/IServices/OW/IDocumentSnapshotService.cs`（继承 IScopedService）
  - `Application.Contracts/IServices/OW/IDocumentOperationLogService.cs`（继承 IScopedService）

- [ ] T-BE-08：创建 Service 实现
  - `Application/Services/OW/DocumentSnapshotService.cs`
    - CreateAsync、SaveAsync（Upsert）、GetByUnitIdAsync、QueryAsync、DeleteAsync
  - `Application/Services/OW/DocumentOperationLogService.cs`
    - AddLogAsync、GetLogsByUnitIdAsync、GetCurrentRevisionAsync

- [ ] T-BE-09：创建 AutoMapper Profile
  - 文件：`Application/Maps/DocumentSnapshotMapProfile.cs`
  - 映射：DocumentSnapshot↔OutputDto、InputDto→DocumentSnapshot、DocumentOperationLog↔OutputDto、InputDto→DocumentOperationLog

- [ ] T-BE-10：创建 DocumentSnapshotController
  - 文件：`WebApi/Controllers/OW/DocumentSnapshotController.cs`
  - 路由：`ow/document-snapshots/v{version:apiVersion}`
  - Action：GET /:unitId、POST /、POST /:unitId/save、DELETE /:id、POST /query、GET /by-entity/:entityType/:entityId

- [ ] T-BE-11：创建 DocumentOperationLogController
  - 文件：`WebApi/Controllers/OW/DocumentOperationLogController.cs`
  - 路由：`ow/document-operation-logs/v{version:apiVersion}`
  - Action：POST /（上报日志）、GET /:unitId（查询历史）、GET /:unitId/revision（获取版本号）

- [ ] T-BE-12：创建 CollabHub（SignalR）
  - 文件：`WebApi/Hubs/CollabHub.cs`
  - 方法：Join(unitId, userId)、Ingest(ChangesetDto cs)、UpdateCursor(unitId, selection)、OnDisconnectedAsync
  - 在 Program.cs 注册：`builder.Services.AddSignalR()` + `app.MapHub<CollabHub>("/collab/{unitId}")`

---

## 第四组：前端 UI 层

- [ ] T-FE-01：创建 UniverSheet.vue 组件
  - 文件：`src/app/components/UniverEditor/UniverSheet.vue`
  - Props：workbookData?, height?（默认 calc(100vh-56px)）, readonly?
  - Emits：mutation(MutationInfo), ready
  - defineExpose：getWorkbookData(), getInstance()
  - 实现：onMounted 内 createUniver + 全套 Sheets Preset + ZH_CN + onCommandExecuted 监听；onBeforeUnmount dispose

- [ ] T-FE-02：创建 UniverDoc.vue 组件
  - 文件：`src/app/components/UniverEditor/UniverDoc.vue`
  - 与 UniverSheet 结构一致，额外注册 UniverDocsLayoutWorkerPlugin
  - defineExpose：getDocumentData()

- [ ] T-FE-03：创建 UniverSlide.vue 组件（P1，可延后）
  - 文件：`src/app/components/UniverEditor/UniverSlide.vue`
  - 底层 Univer 实例直接 registerPlugin 模式

- [ ] T-FE-04：创建组件导出 index.ts
  - 文件：`src/app/components/UniverEditor/index.ts`
  - 导出：UniverSheet、UniverDoc、UniverSlide

- [ ] T-FE-05：创建 useCollaboration composable
  - 文件：`src/app/composables/useCollaboration.ts`
  - 参数：univerAPI, unitId, userId, options?
  - 返回：{ ws, sendCursor, disconnect, baseRev, isConnected, collaborators }
  - 实现：import '@univerjs/network/facade' → createSocket → open$.subscribe JOIN → message$.subscribe 处理 new_cs/cs_ack/cs_rej/update_cursor/join/leave → addEvent MUTATION 发 INGEST

- [ ] T-FE-06：创建 documentSnapshot API 模块
  - 文件：`src/app/apis/ow/documentSnapshot.ts`
  - 导出：getSnapshotByUnitId / createSnapshot / saveSnapshot / deleteSnapshot / querySnapshots / getSnapshotsByEntity

- [ ] T-FE-07：创建 documentOperationLog API 模块
  - 文件：`src/app/apis/ow/documentOperationLog.ts`
  - 导出：addOperationLog / getOperationLogs / getCurrentRevision

- [ ] T-FE-08：创建 documentList Pinia Store
  - 文件：`src/app/stores/modules/documentList.ts`
  - id：`item-wfe-document-list`
  - state：list, total, page(1), pageSize(20), keyword(''), docType(''), sortBy('modify_date'), loading
  - actions：loadList / deleteDocument / renameDocument / resetFilter

- [ ] T-FE-09：创建 documentEditor Pinia Store
  - 文件：`src/app/stores/modules/documentEditor.ts`
  - id：`item-wfe-document-editor`
  - state：unitId, docType, snapshotData(非响应式，直接赋值), title, loading, saving, hasUnsaved, collaborators, currentRevision, error
  - actions：loadDocument / saveDocument / logMutation（fire-and-forget）/ setCollaborator / removeCollaborator

- [ ] T-FE-10：创建文档列表页
  - 文件：`src/app/views/document/list/index.vue`
  - 实现：DocumentCard 网格 + 搜索 debounce + 类型筛选 + 排序 + 分页 + 空状态 + Skeleton 加载

- [ ] T-FE-11：创建文档卡片组件
  - 文件：`src/app/views/document/list/DocumentCard.vue`
  - 实现：类型图标 + 标题（2行截断）+ 时间/作者 + hover 操作菜单

- [ ] T-FE-12：创建新建文档弹窗
  - 文件：`src/app/views/document/list/CreateDocumentDialog.vue`
  - 实现：el-dialog + 名称输入（autofocus + 空值校验）+ 三类型卡片选择（默认 Sheet）+ loading 确认按钮

- [ ] T-FE-13：创建文档编辑器页
  - 文件：`src/app/views/document/editor/index.vue`
  - 实现：56px 顶栏（返回 + 标题行内编辑 + 协同头像 + 保存）+ 编辑器区域 calc(100vh-56px) + onBeforeRouteLeave 三选确认 + Ctrl+S

- [ ] T-FE-14：创建路由模块并注册
  - 文件：`src/app/router/routers/modules/document.ts`
  - 路由：`/documents` 顶层菜单，code: DOCUMENTS，ordinal: 7
  - 子路由：list（keepAlive: true）和 :unitId/edit（hidden: true，activeMenu 指向 list）

---

## 第五组：后端单元测试

- [ ] T-TEST-01：DocumentSnapshotService 单元测试
  - 文件：`Tests/FlowFlex.Tests/OW/DocumentSnapshotServiceTests.cs`
  - 用例：SaveAsync_NewDocument_CreatesRecord / SaveAsync_ExistingDocument_UpdatesRecord / GetByUnitIdAsync_NotFound_ThrowsCRMException / QueryAsync_WithFilter_ReturnsMatchingResults

- [ ] T-TEST-02：DocumentOperationLogService 单元测试
  - 文件：`Tests/FlowFlex.Tests/OW/DocumentOperationLogServiceTests.cs`
  - 用例：AddLogAsync_ValidInput_ReturnsId / GetCurrentRevisionAsync_Returns_CorrectRevision
