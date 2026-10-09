# Tasks — 完整开发任务清单

> 来源：technical-design/tasks.md + test-verification/tasks.md  
> 按执行顺序排列，同组内任务可并行

---

## 第一组：环境搭建与基础配置

- [ ] **T-ENV-01**：安装 Univer 全套 npm 包
  - 目录：`packages/flowFlex-common/`
  - 包：`@univerjs/presets` + Sheets 全套（11个）+ Docs 全套（4个）+ Slides 底层（5个）+ `@univerjs/network`
  - 完整命令见 `docs/univer-integration/technical-design/dev-setup.md` 第二章

- [ ] **T-ENV-02**：配置 Tailwind CSS 隔离
  - 文件：`packages/flowFlex-common/tailwind.config.ts`
  - 修改：添加 `important: '#app'`
  - ⚠️ 改后立即执行 TV-STYLE-01 回归检查

- [ ] **T-ENV-03**：配置 Vite optimizeDeps
  - 文件：`packages/flowFlex-common/vite.config.ts`
  - 追加：`'@univerjs/presets'`、`'@univerjs/preset-sheets-core'`、`'@univerjs/preset-docs-core'`

- [ ] **T-ENV-04**：后端新增 DOCUMENTS 权限常量
  - 文件：`Domain/Shared/Const/PermissionConsts.cs`
  - 新增 `Document` 静态类：`DOCUMENT:READ / CREATE / UPDATE / DELETE`

- [ ] **T-ENV-05**：前端 i18n 新增路由文案
  - 新增：`sys.router.document: '文档管理'`

---

## 第二组：后端数据层

- [ ] **T-BE-01**：创建 `DocumentSnapshot.cs` 实体
  - 路径：`Domain/Entities/OW/DocumentSnapshot.cs`
  - 继承 `EntityBaseCreateInfo`，表名 `ff_document_snapshot`
  - 字段：UnitId / Title / DocType / DataJson(string, ColumnDataType=jsonb) / Revision / EntityType / EntityId

- [ ] **T-BE-02**：创建 `DocumentOperationLog.cs` 实体
  - 路径：`Domain/Entities/OW/DocumentOperationLog.cs`
  - 表名 `ff_document_operation_log`
  - 字段：UnitId / OperatorUserId / OperatorUserName / OpTimestamp / MutationId / ParamsJson(string, jsonb) / Revision

- [ ] **T-BE-03**：创建 Repository 接口（2个）
  - `Domain/Repository/OW/IDocumentSnapshotRepository.cs`
  - `Domain/Repository/OW/IDocumentOperationLogRepository.cs`

- [ ] **T-BE-04**：创建 Repository 实现（2个）
  - `SqlSugarDB/Repositories/OW/DocumentSnapshotRepository.cs`（继承 BaseRepository<DocumentSnapshot>）
  - `SqlSugarDB/Repositories/OW/DocumentOperationLogRepository.cs`

- [ ] **T-BE-05**：创建并注册 Migration
  - 文件：`SqlSugarDB/Migrations/Migration_20260922000001_CreateDocumentTables.cs`
  - 建 `ff_document_snapshot`（含复合唯一索引）和 `ff_document_operation_log`（含查询索引）
  - 在 `MigrationManager.cs` 末尾追加注册
  - DDL 见 `docs/univer-integration/technical-design/database-schema.md`

---

## 第三组：后端 API 层

- [ ] **T-BE-06**：创建 DTO（5个文件）
  - 目录：`Application.Contracts/Dtos/OW/DocumentSnapshot/`
  - 文件：InputDto / OutputDto / QueryRequest / LogInputDto / LogOutputDto

- [ ] **T-BE-07**：创建 Service 接口（2个）
  - `Application.Contracts/IServices/OW/IDocumentSnapshotService.cs`（继承 IScopedService）
  - `Application.Contracts/IServices/OW/IDocumentOperationLogService.cs`（继承 IScopedService）

- [ ] **T-BE-08**：创建 Service 实现（2个）
  - `Application/Services/OW/DocumentSnapshotService.cs`
    - 方法：CreateAsync / SaveAsync（Upsert，BR-01）/ GetByUnitIdAsync / QueryAsync / DeleteAsync（软删除，BR-02）
  - `Application/Services/OW/DocumentOperationLogService.cs`
    - 方法：AddLogAsync / GetLogsByUnitIdAsync / GetCurrentRevisionAsync

- [ ] **T-BE-09**：创建 AutoMapper Profile
  - 文件：`Application/Maps/DocumentSnapshotMapProfile.cs`

- [ ] **T-BE-10**：创建 `DocumentSnapshotController`
  - 路由：`ow/document-snapshots/v{version:apiVersion}`
  - Action：GET /:unitId / POST / POST /:unitId/save / DELETE /:id / POST /query / GET /by-entity/:type/:id
  - 详细规格见 `docs/univer-integration/technical-design/api-spec.md`

- [ ] **T-BE-11**：创建 `DocumentOperationLogController`
  - 路由：`ow/document-operation-logs/v{version:apiVersion}`
  - Action：POST / GET /:unitId / GET /:unitId/revision

- [ ] **T-BE-12**：创建 `CollabHub`（SignalR）
  - 文件：`WebApi/Hubs/CollabHub.cs`
  - 方法：Join / Ingest（乐观锁，BR-03）/ UpdateCursor / OnDisconnectedAsync
  - 在 `Program.cs` 注册：`AddSignalR()` + `MapHub<CollabHub>("/collab/{unitId}")`

---

## 第四组：前端 UI 层

- [ ] **T-FE-01**：创建 `UniverSheet.vue`
  - 路径：`src/app/components/UniverEditor/UniverSheet.vue`
  - Props：workbookData?(非响应式，直接 pass), height?(默认calc(100vh-56px)), readonly?
  - Emits：mutation(mutationId,params,timestamp), ready
  - defineExpose：getWorkbookData(), getInstance()
  - ⚠️ `workbookData` 不得用 `ref()` 包裹（KD-02）
  - onMounted：createUniver + 所有 Sheets Preset + ZH_CN + onCommandExecuted 监听
  - onBeforeUnmount：univerInstance.univer.dispose()

- [ ] **T-FE-02**：创建 `UniverDoc.vue`
  - 路径：`src/app/components/UniverEditor/UniverDoc.vue`
  - 与 UniverSheet 结构一致，额外注册 `UniverDocsLayoutWorkerPlugin`
  - defineExpose：getDocumentData()

- [ ] **T-FE-03**：创建 `UniverSlide.vue`（P1）
  - 路径：`src/app/components/UniverEditor/UniverSlide.vue`
  - 底层 `new Univer()` + registerPlugin 模式（无 Preset）

- [ ] **T-FE-04**：创建 `components/UniverEditor/index.ts` 统一导出

- [ ] **T-FE-05**：创建 `useCollaboration.ts`
  - 路径：`src/app/composables/useCollaboration.ts`
  - 先 `import '@univerjs/network/facade'`，再 `univerAPI.createSocket(wsUrl)`
  - 处理：open$→JOIN，message$→new_cs/cs_ack/cs_rej/join/leave，addEvent→INGEST
  - cs_rej：指数退避重试（100/200/400ms，最多 3 次）
  - 返回：{ ws, baseRev, isConnected, collaborators, sendCursor, disconnect }

- [ ] **T-FE-06**：创建 `documentSnapshot.ts` API 模块
  - 路径：`src/app/apis/ow/documentSnapshot.ts`
  - 导出：getSnapshotByUnitId / createSnapshot / saveSnapshot / deleteSnapshot / querySnapshots

- [ ] **T-FE-07**：创建 `documentOperationLog.ts` API 模块
  - 路径：`src/app/apis/ow/documentOperationLog.ts`
  - 导出：addOperationLog / getOperationLogs / getCurrentRevision

- [ ] **T-FE-08**：创建 `documentList` Pinia Store
  - 路径：`src/app/stores/modules/documentList.ts`
  - id：`item-wfe-document-list`
  - state：list / total / page(1) / pageSize(20) / keyword / docType / sortBy / loading
  - actions：loadList / deleteDocument / renameDocument / resetFilter

- [ ] **T-FE-09**：创建 `documentEditor` Pinia Store
  - 路径：`src/app/stores/modules/documentEditor.ts`
  - id：`item-wfe-document-editor`
  - state：unitId / docType / snapshotData(非响应式) / title / loading / saving / hasUnsaved / collaborators / currentRevision
  - actions：loadDocument / saveDocument / logMutation(fire-and-forget) / setCollaborator / removeCollaborator

- [ ] **T-FE-10**：创建文档列表页
  - 路径：`src/app/views/document/list/index.vue`
  - 搜索（300ms debounce）+ 类型筛选 + 排序 + DocumentCard 网格 + 空状态 + el-pagination + 新建按钮

- [ ] **T-FE-11**：创建 `DocumentCard.vue`
  - 路径：`src/app/views/document/list/DocumentCard.vue`
  - 类型图标（Sheet=绿/Doc=蓝/Slide=橙）+ 标题（2行截断）+ 时间/作者 + hover 操作菜单

- [ ] **T-FE-12**：创建 `CreateDocumentDialog.vue`
  - 路径：`src/app/views/document/list/CreateDocumentDialog.vue`
  - el-dialog(480px) + autofocus 输入框 + 空值校验 + 三类型卡片（默认 Sheet）+ loading 确认按钮

- [ ] **T-FE-13**：创建文档编辑器页
  - 路径：`src/app/views/document/editor/index.vue`
  - 顶栏（56px）：返回 + 标题行内编辑（空值还原）+ 协同头像（最多5个+折叠）+ 保存按钮
  - 编辑区：v-if 按 docType 渲染 UniverSheet/Doc/Slide，height=calc(100vh-56px)
  - Ctrl+S 全局监听
  - onBeforeRouteLeave：hasUnsaved=true 时三选确认

- [ ] **T-FE-14**：创建路由模块并注册
  - 路径：`src/app/router/routers/modules/document.ts`
  - 顶层路径 `/documents`，code: DOCUMENTS，ordinal: 7，hideChildrenInMenu: true
  - 子路由：`list`（keepAlive: true）和 `:unitId/edit`（hidden: true，activeMenu: '/documents/list'）

---

## 第五组：测试任务

- [ ] **TV-BE-01**：实现 `DocumentSnapshotServiceTests.cs`（TC-BE-01~09，9个用例）
- [ ] **TV-BE-02**：实现 `DocumentOperationLogServiceTests.cs`（TC-BE-10~12，3个用例）
- [ ] **TV-BE-03**：运行 `dotnet test` 确认全部通过，覆盖率 ≥ 80%
- [ ] **TV-API-01**：验证 DocumentSnapshotController 端点（TC-API-01~07）
- [ ] **TV-API-02**：验证 DocumentOperationLogController 端点（TC-API-08~10）
- [ ] **TV-API-03**：验证未认证请求返回 401（TC-API-11）
- [ ] **TV-HUB-01**：SignalR Hub 测试（TC-HUB-01~05，重点 cs_ack/cs_rej）
- [ ] **TV-FE-01**：`CreateDocumentDialog` 组件测试（TC-FE-01~05）
- [ ] **TV-FE-02**：`DocumentCard` 组件测试（TC-FE-06~09）
- [ ] **TV-FE-03**：`DocumentEditor` 离开确认测试（TC-FE-10~13）
- [ ] **TV-FE-04**：`documentEditorStore` fire-and-forget 测试（TC-FE-14~15）
- [ ] **TV-STYLE-01**：现有页面样式回归（工作流/问卷/清单页）
- [ ] **TV-STYLE-02**：文档模块样式验证（列表/编辑器/Univer 内部样式）
- [ ] **TV-ISS-01**：验证 ElPlus 浮层 z-index 正常（ISS-01）
- [ ] **TV-ISS-02**：Staging 环境验证 Web Worker MIME（ISS-02）
