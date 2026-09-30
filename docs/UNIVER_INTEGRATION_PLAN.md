# Univer 集成任务规划

> 文档类型：开发任务规划  
> 版本：1.1  
> 更新日期：2026-09-22  
> 参考文档：[INTEGRATION_GUIDE.md](./INTEGRATION_GUIDE.md)

---

## 一、目标概述

将 Univer（开源类 Office 在线文档编辑框架，Apache 2.0 协议）集成到 FlowFlex 项目，支持以下三种文档类型：

| 类型       | 功能                                                     | 优先级     |
| ---------- | -------------------------------------------------------- | ---------- |
| **Sheets** | 类 Excel 电子表格，含公式/条件格式/数据验证/筛选等全功能 | P0（必须） |
| **Docs**   | 类 Word 富文本文档，含超链接/绘图/批注等                 | P0（必须） |
| **Slides** | 类 PPT 演示文稿（底层插件组合）                          | P1（可选） |

完整功能包括：文档快照存储、操作日志记录、多人实时协同编辑（SignalR + 乐观锁）。

---

## 二、架构设计

### 2.1 后端架构

```
ff_document_snapshot    ff_document_operation_log
       │                          │
       │                          │
DocumentSnapshotEntity   OperationLogEntity
       │                          │
IDocumentSnapshotRepository  IDocumentOperationLogRepository
       │                          │
IDocumentSnapshotService   IDocumentOperationLogService
       │                          │
DocumentSnapshotController (ow/document-snapshots/v1)
DocumentOperationLogController (ow/document-operation-logs/v1)
CollabHub (SignalR → /collab/{unitId})
```

### 2.2 前端架构

独立菜单，路径 `/documents`，下含两个层级页面：

```
菜单：文档管理（/documents）
  ├── 文档列表页  /documents/list         ← 菜单入口，展示所有文档卡片/表格
  └── 文档编辑页  /documents/:unitId/edit ← 从列表进入，全屏编辑器，隐藏菜单栏

UniverSheet.vue / UniverDoc.vue / UniverSlide.vue  (编辑器组件)
       │
useCollaboration.ts  (composable，WebSocket 协同接线)
       │
src/app/apis/ow/documentSnapshot.ts    (HTTP API 封装)
src/app/apis/ow/documentOperationLog.ts
       │
src/app/stores/modules/documentList.ts   (列表页 Store)
src/app/stores/modules/documentEditor.ts (编辑器页 Store)
       │
router/routers/modules/document.ts  (路由注册)
```

### 2.3 数据流

```
打开文档页面
  → GET ow/document-snapshots/v1/{unitId}
      → C# 返回 IWorkbookData / IDocumentData JSON
          → univerAPI.createWorkbook() / univer.createUnit() → 渲染编辑器

用户编辑
  → onCommandExecuted（MUTATION 事件）
      → 本地：POST ow/document-operation-logs/v1（记录操作日志）
      → 协同：SignalR INGEST → 乐观锁校验 → cs_ack + 广播 new_cs 给其他人

用户保存
  → getWorkbookData() / getDocumentData()
      → POST ow/document-snapshots/v1/{unitId}（更新快照）
```

---

## 三、详细任务列表

---

### Phase 1：后端基础层（实体 + Repository + Migration）

---

#### Task 1.1：创建 DocumentSnapshot 实体

**文件位置：** `packages/flowFlex-backend/Domain/Entities/OW/DocumentSnapshot.cs`

**实体设计：**

```csharp
[SugarTable("ff_document_snapshot")]
public class DocumentSnapshot : EntityBaseCreateInfo
{
    // Univer 文档唯一 ID（前端生成，UUID 格式）
    [StringLength(100)]
    [SugarColumn(ColumnName = "unit_id")]
    public string UnitId { get; set; }

    // 文档标题
    [StringLength(200)]
    public string Title { get; set; }

    // 文档类型：Sheet / Doc / Slide
    [StringLength(20)]
    [SugarColumn(ColumnName = "doc_type")]
    public string DocType { get; set; }

    // 文档快照 JSON（IWorkbookData / IDocumentData），存 JSONB
    [SugarColumn(ColumnName = "data_json", ColumnDataType = "jsonb")]
    public string DataJson { get; set; }

    // 当前版本号（协同用，从 1 开始递增）
    public int Revision { get; set; } = 1;

    // 所属业务实体类型（如 "Onboarding"、"Stage"）
    [StringLength(50)]
    [SugarColumn(ColumnName = "entity_type")]
    public string EntityType { get; set; }

    // 所属业务实体 ID（如 onboardingId）
    [SugarColumn(ColumnName = "entity_id")]
    [JsonConverter(typeof(LongToStringConverter))]
    public long EntityId { get; set; }
}
```

**注意事项：**

- 继承 `EntityBaseCreateInfo`（获得 `Id`、`TenantId`、`AppCode`、`IsValid`、审计字段）
- `DataJson` 直接存 JSON 字符串，使用 `ColumnDataType = "jsonb"` 但不加 `IsJson = true`（避免 SqlSugar 自动反序列化，保持 string 类型操作）
- `UnitId` 建唯一索引（同一 tenant 下唯一）

---

#### Task 1.2：创建 DocumentOperationLog 实体

**文件位置：** `packages/flowFlex-backend/Domain/Entities/OW/DocumentOperationLog.cs`

**实体设计：**

```csharp
[SugarTable("ff_document_operation_log")]
public class DocumentOperationLog : EntityBaseCreateInfo
{
    // 关联的文档 unitId
    [StringLength(100)]
    [SugarColumn(ColumnName = "unit_id")]
    public string UnitId { get; set; }

    // 操作用户 ID（字符串，兼容 IDM 用户系统）
    [StringLength(100)]
    [SugarColumn(ColumnName = "operator_user_id")]
    public string OperatorUserId { get; set; }

    // 操作用户名
    [StringLength(100)]
    [SugarColumn(ColumnName = "operator_user_name")]
    public string OperatorUserName { get; set; }

    // 操作时间戳（毫秒，来自前端 Date.now()）
    [SugarColumn(ColumnName = "op_timestamp")]
    public long OpTimestamp { get; set; }

    // Univer MutationId（如 sheet.mutation.set-range-values）
    [StringLength(200)]
    [SugarColumn(ColumnName = "mutation_id")]
    public string MutationId { get; set; }

    // 操作参数 JSON（JSONB 存储）
    [SugarColumn(ColumnName = "params_json", ColumnDataType = "jsonb")]
    public string ParamsJson { get; set; }

    // 协同版本号
    [SugarColumn(ColumnName = "revision")]
    public int Revision { get; set; }
}
```

---

#### Task 1.3：创建 Repository 接口和实现

**接口文件：**

- `Domain/Repository/OW/IDocumentSnapshotRepository.cs`
- `Domain/Repository/OW/IDocumentOperationLogRepository.cs`

**实现文件：**

- `SqlSugarDB/Repositories/OW/DocumentSnapshotRepository.cs`
- `SqlSugarDB/Repositories/OW/DocumentOperationLogRepository.cs`

均继承 `BaseRepository<TEntity>` 并实现对应接口，接口方法至少包含：

- `IDocumentSnapshotRepository`：`GetByUnitIdAsync(string unitId)`、`GetByEntityAsync(string entityType, long entityId)`、`UpsertByUnitIdAsync(DocumentSnapshot entity)`
- `IDocumentOperationLogRepository`：`GetLogsByUnitIdAsync(string unitId, DateTime? from, DateTime? to)`、`GetRevisionAsync(string unitId)`

---

#### Task 1.4：数据库 Migration

**新建文件：** `SqlSugarDB/Migrations/Migration_20260922000001_CreateDocumentTables.cs`

**SQL 内容：**

```sql
-- Up
CREATE TABLE IF NOT EXISTS ff_document_snapshot (
    id                 BIGINT PRIMARY KEY,
    unit_id            VARCHAR(100) NOT NULL,
    title              VARCHAR(200),
    doc_type           VARCHAR(20) NOT NULL DEFAULT 'Sheet',
    data_json          JSONB,
    revision           INT NOT NULL DEFAULT 1,
    entity_type        VARCHAR(50),
    entity_id          BIGINT,
    tenant_id          VARCHAR(100) NOT NULL DEFAULT '',
    app_code           VARCHAR(100) NOT NULL DEFAULT '',
    is_valid           BOOLEAN NOT NULL DEFAULT TRUE,
    create_date        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    modify_date        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    create_by          VARCHAR(200),
    modify_by          VARCHAR(200),
    create_user_id     BIGINT NOT NULL DEFAULT 0,
    modify_user_id     BIGINT NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_ff_document_snapshot_unit_id_tenant
    ON ff_document_snapshot (unit_id, tenant_id, app_code)
    WHERE is_valid = TRUE;

CREATE TABLE IF NOT EXISTS ff_document_operation_log (
    id                   BIGINT PRIMARY KEY,
    unit_id              VARCHAR(100) NOT NULL,
    operator_user_id     VARCHAR(100),
    operator_user_name   VARCHAR(100),
    op_timestamp         BIGINT NOT NULL DEFAULT 0,
    mutation_id          VARCHAR(200),
    params_json          JSONB,
    revision             INT NOT NULL DEFAULT 0,
    tenant_id            VARCHAR(100) NOT NULL DEFAULT '',
    app_code             VARCHAR(100) NOT NULL DEFAULT '',
    is_valid             BOOLEAN NOT NULL DEFAULT TRUE,
    create_date          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    modify_date          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    create_by            VARCHAR(200),
    modify_by            VARCHAR(200),
    create_user_id       BIGINT NOT NULL DEFAULT 0,
    modify_user_id       BIGINT NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS idx_ff_document_operation_log_unit_id
    ON ff_document_operation_log (unit_id, tenant_id, app_code, create_date DESC);

-- Down
DROP TABLE IF EXISTS ff_document_operation_log;
DROP TABLE IF EXISTS ff_document_snapshot;
```

**注册到 MigrationManager.cs 末尾：**

```csharp
("20260922000001_CreateDocumentTables",
 (Action)(() => Migration_20260922000001_CreateDocumentTables.Up(_db)))
```

---

### Phase 2：后端应用层（Service + DTO + AutoMapper + Controller）

---

#### Task 2.1：创建 DTO

**目录：** `Application.Contracts/Dtos/OW/DocumentSnapshot/`

需要创建：

- `DocumentSnapshotInputDto.cs`（创建/更新快照）
- `DocumentSnapshotOutputDto.cs`（返回给前端）
- `DocumentOperationLogInputDto.cs`（前端上报操作日志）
- `DocumentOperationLogOutputDto.cs`（返回操作历史）

**关键字段（InputDto）：**

```csharp
public class DocumentSnapshotInputDto
{
    public string UnitId { get; set; }
    public string Title { get; set; }
    public string DocType { get; set; }  // "Sheet" | "Doc" | "Slide"
    public string DataJson { get; set; }  // JSON 字符串，前端直传，后端不解析
    public string EntityType { get; set; }
    public long EntityId { get; set; }
}

public class DocumentOperationLogInputDto
{
    public string UnitId { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }
    public long Timestamp { get; set; }
    public string MutationId { get; set; }
    public string ParamsJson { get; set; }  // JSON string
    public int Revision { get; set; }
}
```

---

#### Task 2.2：创建 Service 接口

**文件：** `Application.Contracts/IServices/OW/IDocumentSnapshotService.cs`

```csharp
public interface IDocumentSnapshotService : IScopedService
{
    Task<long> CreateAsync(DocumentSnapshotInputDto input);
    Task<bool> SaveAsync(string unitId, DocumentSnapshotInputDto input);  // Upsert
    Task<DocumentSnapshotOutputDto> GetByUnitIdAsync(string unitId);
    Task<List<DocumentSnapshotOutputDto>> GetByEntityAsync(string entityType, long entityId);
    Task<bool> DeleteAsync(long id);
}
```

**文件：** `Application.Contracts/IServices/OW/IDocumentOperationLogService.cs`

```csharp
public interface IDocumentOperationLogService : IScopedService
{
    Task<long> AddLogAsync(DocumentOperationLogInputDto input);
    Task<List<DocumentOperationLogOutputDto>> GetLogsByUnitIdAsync(
        string unitId, DateTime? from = null, DateTime? to = null, string userId = null);
    Task<int> GetCurrentRevisionAsync(string unitId);
}
```

---

#### Task 2.3：创建 Service 实现

**文件：** `Application/Services/OW/DocumentSnapshotService.cs`
**文件：** `Application/Services/OW/DocumentOperationLogService.cs`

`DocumentSnapshotService.SaveAsync` 是核心方法，逻辑：

1. 查询 `UnitId` 是否已存在
2. 存在则 `Update`，不存在则 `Insert`
3. 返回操作结果

两个 Service 均实现对应 `IXxxService, IScopedService`，通过 DI 自动注册。

---

#### Task 2.4：创建 AutoMapper Profile

**文件：** `Application/Maps/DocumentSnapshotMapProfile.cs`

```csharp
public class DocumentSnapshotMapProfile : Profile
{
    public DocumentSnapshotMapProfile()
    {
        CreateMap<DocumentSnapshot, DocumentSnapshotOutputDto>();
        CreateMap<DocumentSnapshotInputDto, DocumentSnapshot>();
        CreateMap<DocumentOperationLog, DocumentOperationLogOutputDto>();
        CreateMap<DocumentOperationLogInputDto, DocumentOperationLog>();
    }
}
```

Profile 类注册方式：在 `Program.cs` 中 `AddAutoMapper` 的扫描路径已包含 `Application/Maps/`，无需额外注册。

---

#### Task 2.5：创建 Controller

**文件：** `WebApi/Controllers/OW/DocumentSnapshotController.cs`

路由：`ow/document-snapshots/v{version:apiVersion}`

关键 Action：

```csharp
[HttpGet("{unitId}")]      // 加载文档快照
[HttpPost]                 // 创建新文档
[HttpPost("{unitId}/save")]// 保存/更新快照（Upsert）
[HttpDelete("{id}")]       // 删除快照
[HttpGet("by-entity/{entityType}/{entityId}")]  // 按业务实体查询
```

**文件：** `WebApi/Controllers/OW/DocumentOperationLogController.cs`

路由：`ow/document-operation-logs/v{version:apiVersion}`

关键 Action：

```csharp
[HttpPost]                       // 记录操作日志（前端每次 mutation 调用）
[HttpGet("{unitId}")]            // 查询操作历史（支持 from/to/userId 过滤）
[HttpGet("{unitId}/revision")]   // 获取当前版本号（协同用）
```

---

#### Task 2.6：SignalR 协同 Hub

**文件：** `WebApi/Hubs/CollabHub.cs`

```csharp
[Authorize]
public class CollabHub : Hub
{
    // Join(unitId, userId)      — 加入房间
    // Ingest(ChangesetDto cs)   — 接收并广播 Changeset（乐观锁版本校验）
    // UpdateCursor(unitId, selection) — 广播光标位置
    // OnDisconnectedAsync       — 通知其他人离开
}
```

**在 `Program.cs` 注册：**

```csharp
builder.Services.AddSignalR();
app.MapHub<CollabHub>("/collab/{unitId}");
```

---

### Phase 3：前端依赖安装与基础配置

---

#### Task 3.1：安装 Univer 依赖

**工作目录：** `packages/flowFlex-common/`

```bash
# 基础框架
npm install @univerjs/presets

# Sheets 全功能
npm install @univerjs/preset-sheets-core
npm install @univerjs/preset-sheets-drawing
npm install @univerjs/preset-sheets-conditional-formatting
npm install @univerjs/preset-sheets-filter
npm install @univerjs/preset-sheets-hyper-link
npm install @univerjs/preset-sheets-data-validation
npm install @univerjs/preset-sheets-find-replace
npm install @univerjs/preset-sheets-note
npm install @univerjs/preset-sheets-sort
npm install @univerjs/preset-sheets-table
npm install @univerjs/preset-sheets-thread-comment

# Docs 全功能
npm install @univerjs/preset-docs-core
npm install @univerjs/preset-docs-drawing
npm install @univerjs/preset-docs-hyper-link
npm install @univerjs/preset-docs-thread-comment

# Slides（底层包）
npm install @univerjs/slides @univerjs/slides-ui
npm install @univerjs/docs @univerjs/docs-ui
npm install @univerjs/drawing @univerjs/engine-render @univerjs/ui

# 协同编辑
npm install @univerjs/network
```

---

#### Task 3.2：Tailwind CSS 前缀配置

**修改文件：** `packages/flowFlex-common/tailwind.config.js`（或 `tailwind.config.ts`）

在 `export default` 对象中添加 `prefix: 'tw-'`，防止 Tailwind base reset 样式破坏 Univer 内部 UI。

**同步更新：** 确认现有代码中的 Tailwind 类是否已使用 `tw-` 前缀，如果尚未使用前缀，则改用 `important: '#app'` 方案（限制影响范围）。

> ⚠️ **注意**：修改前先确认项目现有 Tailwind 配置，不可无脑添加前缀，避免破坏已有样式。

---

#### Task 3.3：Vite 配置优化

**修改文件：** `packages/flowFlex-common/vite.config.ts`

在 `optimizeDeps.include` 数组中添加：

```ts
'@univerjs/presets',
'@univerjs/preset-sheets-core',
'@univerjs/preset-docs-core',
```

---

### Phase 4：前端核心组件

---

#### Task 4.1：封装 UniverSheet 组件

**文件：** `packages/flowFlex-common/src/app/components/UniverEditor/UniverSheet.vue`

Props：

```typescript
interface Props {
  workbookData?: object; // 从后端加载的 IWorkbookData JSON
  height?: string; // 容器高度，默认 '600px'
  readonly?: boolean; // 只读模式
}
```

Emits：

```typescript
defineEmits<{
  mutation: [info: MutationInfo]; // 每次编辑操作
  ready: []; // 编辑器就绪
}>();
```

`defineExpose`：

```typescript
defineExpose({
  getWorkbookData: () => univerInstance?.univerAPI.getActiveWorkbook()?.save(),
  getInstance: () => univerInstance,
});
```

关键实现：

- `onMounted` 内调用 `createUniver(...)` + 所有 Preset
- 设置 `ZH_CN` 语言包
- 监听 `onCommandExecuted` → 过滤 `MUTATION` 类型 → emit `mutation`
- `onBeforeUnmount` 调用 `univerInstance.univer.dispose()`

---

#### Task 4.2：封装 UniverDoc 组件

**文件：** `packages/flowFlex-common/src/app/components/UniverEditor/UniverDoc.vue`

Props、Emits、defineExpose 结构与 UniverSheet 对应。

额外处理：注册 `UniverDocsLayoutWorkerPlugin`（Web Worker 排版），提升大文档性能。

---

#### Task 4.3：封装 UniverSlide 组件

**文件：** `packages/flowFlex-common/src/app/components/UniverEditor/UniverSlide.vue`

Slides 无 Preset，使用底层 `Univer` 实例直接 `registerPlugin(...)` 的方式。

---

#### Task 4.4：导出组件 index.ts

**文件：** `packages/flowFlex-common/src/app/components/UniverEditor/index.ts`

```typescript
export { default as UniverSheet } from "./UniverSheet.vue";
export { default as UniverDoc } from "./UniverDoc.vue";
export { default as UniverSlide } from "./UniverSlide.vue";
```

---

### Phase 5：前端协同 Composable

---

#### Task 5.1：创建协同 Composable

**文件：** `packages/flowFlex-common/src/app/composables/useCollaboration.ts`

功能：封装 WebSocket（`@univerjs/network`）到 Univer 的协同接线逻辑。

接口：

```typescript
export function useCollaboration(
  univerAPI: FUniver,
  unitId: string,
  userId: string,
  options?: {
    wsUrl?: string; // 默认 wss://{location.host}/collab/{unitId}
    onJoin?: (userId: string) => void;
    onLeave?: (userId: string) => void;
    onCursorUpdate?: (userId: string, selection: string) => void;
  },
): {
  ws: ISocket;
  sendCursor: (selection: string) => void;
  disconnect: () => void;
  baseRev: Ref<number>;
  isConnected: Ref<boolean>;
};
```

核心逻辑：

1. `univerAPI.createSocket(wsUrl)` 建立 WebSocket（需先 `import '@univerjs/network/facade'`）
2. `ws.open$.subscribe` → 发送 JOIN 消息
3. `ws.message$.subscribe` → 处理 `new_cs`、`cs_ack`、`cs_rej`、`update_cursor`、`join`、`leave`
4. `univerAPI.addEvent(CommandExecuted)` → 过滤本地 MUTATION → 发送 INGEST
5. `cs_rej` 时自动重试

---

### Phase 6：前端 API 层 + Store

---

#### Task 6.1：创建 API 模块

**文件：** `packages/flowFlex-common/src/app/apis/ow/documentSnapshot.ts`

```typescript
const Api = (id?: string | number) => ({
  snapshots: `${globSetting.apiProName}/ow/document-snapshots/${globSetting.apiVersion}`,
  snapshotByUnitId: `${globSetting.apiProName}/ow/document-snapshots/${globSetting.apiVersion}/${id}`,
  snapshotSave: `${globSetting.apiProName}/ow/document-snapshots/${globSetting.apiVersion}/${id}/save`,
  snapshotByEntity: `${globSetting.apiProName}/ow/document-snapshots/${globSetting.apiVersion}/by-entity/${id}`,
});
```

导出函数：

- `getSnapshotByUnitId(unitId)`
- `createSnapshot(params)`
- `saveSnapshot(unitId, params)`（Upsert）
- `deleteSnapshot(id)`
- `getSnapshotsByEntity(entityType, entityId)`

**文件：** `packages/flowFlex-common/src/app/apis/ow/documentOperationLog.ts`

导出函数：

- `addOperationLog(params)`
- `getOperationLogs(unitId, query?)`
- `getCurrentRevision(unitId)`

---

#### Task 6.2：创建 Pinia Store

**文件：** `packages/flowFlex-common/src/app/stores/modules/documentEditor.ts`

```typescript
export const useDocumentEditorStore = defineStore({
    id: 'item-wfe-document-editor',
    state: (): DocumentEditorState => ({
        unitId: null,
        docType: 'Sheet',  // 'Sheet' | 'Doc' | 'Slide'
        snapshotData: null,
        title: '',
        loading: false,
        saving: false,
        collaborators: [],  // 当前协同者列表
        currentRevision: 0,
        error: null,
    }),
    actions: {
        async loadDocument(unitId: string): Promise<void>,
        async saveDocument(data: object): Promise<void>,
        async logMutation(info: MutationInfo): Promise<void>,
        setCollaborator(userId: string, info: CollaboratorInfo): void,
        removeCollaborator(userId: string): void,
    }
});
```

---

### Phase 7：前端视图层 + 路由（列表 → 详情模式）

---

#### Task 7.1：补充后端分页查询接口

文档列表页需要分页，在 `IDocumentSnapshotService` 和 `DocumentSnapshotController` 补充：

**Service 接口新增：**

```csharp
Task<PagedResult<DocumentSnapshotOutputDto>> QueryAsync(DocumentSnapshotQueryRequest query);
```

**QueryRequest 字段：**

```csharp
public class DocumentSnapshotQueryRequest : PagedRequest
{
    public string Keyword { get; set; }    // 标题关键词
    public string DocType { get; set; }    // 类型过滤：Sheet / Doc / Slide
    public string SortBy { get; set; }     // create_date / modify_date / title
    public string SortOrder { get; set; }  // asc / desc，默认 desc
}
```

**Controller 新增：**

```csharp
[HttpPost("query")]
[WFEAuthorize(PermissionConsts.Document.Read)]
public async Task<IActionResult> Query([FromBody] DocumentSnapshotQueryRequest query)
    => Success(await _service.QueryAsync(query));
```

---

#### Task 7.2：列表页 Store

**文件：** `packages/flowFlex-common/src/app/stores/modules/documentList.ts`

```typescript
export const useDocumentListStore = defineStore({
    id: 'item-wfe-document-list',
    state: (): DocumentListState => ({
        list: [],
        total: 0,
        page: 1,
        pageSize: 20,
        keyword: '',
        docType: '',       // '' | 'Sheet' | 'Doc' | 'Slide'
        sortBy: 'modify_date',
        loading: false,
    }),
    actions: {
        async loadList(): Promise<void>,
        async deleteDocument(id: string): Promise<void>,
        async renameDocument(id: string, title: string): Promise<void>,
        resetFilter(): void,
    }
});
```

---

#### Task 7.3：新建文档弹窗组件

**文件：** `packages/flowFlex-common/src/app/views/document/list/CreateDocumentDialog.vue`

弹窗内容：文档名称输入 + 三选一类型选择（电子表格 / 文档 / 演示）。

**创建流程：**

1. 调用 `createSnapshot({ title, docType, dataJson: '{}' })`
2. 后端返回新文档的 `unitId`
3. 前端 `router.push({ name: 'DocumentEditor', params: { unitId } })`

---

#### Task 7.4：文档列表页

**文件：** `packages/flowFlex-common/src/app/views/document/list/index.vue`

这是菜单入口页，展示所有文档卡片。

**UI 布局：**

```
┌──────────────────────────────────────────────┐
│  文档管理                     [+ 新建文档]     │
│  ┌────────────────────────────────────────┐  │
│  │ 搜索框  │  类型筛选  │  排序           │  │
│  └────────────────────────────────────────┘  │
│                                              │
│  ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐       │
│  │ 卡片 │ │ 卡片 │ │ 卡片 │ │ 卡片 │  ...  │
│  └──────┘ └──────┘ └──────┘ └──────┘       │
│                                              │
│  分页组件                                    │
└──────────────────────────────────────────────┘
```

**卡片信息：** 文档类型图标、标题、类型 Tag（Sheet/Doc/Slide）、创建人、最近修改时间、操作菜单（重命名 / 删除）

**交互：**

- 点击卡片 → `router.push({ name: 'DocumentEditor', params: { unitId } })`
- 删除：二次确认弹窗 → 调用 store `deleteDocument`
- 搜索/筛选变化时重置 page = 1 重新加载

---

#### Task 7.5：文档编辑器页

**文件：** `packages/flowFlex-common/src/app/views/document/editor/index.vue`

**UI 布局：**

```
┌───────────────────────────────────────────────────────┐
│ ← 返回列表  │  [文档标题（行内编辑）]  │ 🟢🔵头像  [保存] │
├───────────────────────────────────────────────────────┤
│                                                       │
│            Univer 编辑器区域                           │
│         (height: calc(100vh - 56px))                  │
│                                                       │
└───────────────────────────────────────────────────────┘
```

**功能：**

- 路由参数 `:unitId`，query 带 `docType` 减少一次请求
- 根据 `docType` 渲染 `UniverSheet` / `UniverDoc` / `UniverSlide`
- 标题行内编辑，失焦后自动保存标题（仅 PATCH title 字段）
- `Ctrl+S` 快捷键触发全量保存
- 离开页面前有未保存变更 → `onBeforeRouteLeave` 确认弹窗
- 协同：注入 `useCollaboration`
- 操作日志：每次 `mutation` 异步上报（fire and forget，不阻塞编辑）

---

#### Task 7.6：菜单路由模块

**文件：** `packages/flowFlex-common/src/app/router/routers/modules/document.ts`

```typescript
import type { AppRouteModule } from "@/router/types";
import { LAYOUT } from "@/router/constant";
import documentIcon from "@assets/svg/menu/management.svg"; // 暂用 management 图标

const documentModule: AppRouteModule = {
  path: "/documents",
  name: "Documents",
  component: LAYOUT,
  redirect: "/documents/list",
  meta: {
    hideChildrenInMenu: true,
    icon: documentIcon,
    title: "文档管理", // i18n key: t('sys.router.document')
    code: "DOCUMENTS",
    ordinal: 7,
    hidden: false,
    status: true,
  },
  children: [
    {
      path: "list",
      name: "DocumentList",
      component: () => import("@/views/document/list/index.vue"),
      meta: {
        title: "文档管理",
        code: "DOCUMENTS",
        ordinal: 1,
        hidden: false,
        status: true,
        keepAlive: true,
      },
    },
    {
      path: ":unitId/edit",
      name: "DocumentEditor",
      component: () => import("@/views/document/editor/index.vue"),
      meta: {
        title: "文档编辑",
        code: "DOCUMENTS",
        hidden: true,
        status: true,
        activeMenu: "/documents/list",
      },
    },
  ],
};

export default documentModule;
```

**同步操作：**

- 在后端 `PermissionConsts.cs` 新增 `Document` 权限常量类（`DOCUMENT:READ/CREATE/UPDATE/DELETE`）
- 在 i18n 语言包中新增 `sys.router.document: '文档管理'`
- 如有需要，新建 `document.svg` 菜单图标放入 `assets/svg/menu/`

---

### Phase 8：集成测试与收尾

---

#### Task 8.1：后端单元测试

**文件：** `packages/flowFlex-backend/Tests/FlowFlex.Tests/OW/DocumentSnapshotServiceTests.cs`

测试用例（参考 `// Arrange / Act / Assert` 模式）：

- `SaveAsync_NewDocument_CreatesRecord()`
- `SaveAsync_ExistingDocument_UpdatesRecord()`
- `GetByUnitIdAsync_NotFound_ThrowsCRMException()`
- `AddLogAsync_ValidInput_ReturnsId()`
- `GetCurrentRevisionAsync_Returns_CorrectRevision()`

---

#### Task 8.2：前端组件联调

清单：

- [ ] 单独打开 Sheets 编辑器，确认公式/条件格式/筛选等功能可用
- [ ] 单独打开 Docs 编辑器，确认富文本/超链接/批注可用
- [ ] 保存功能：数据写入 `ff_document_snapshot` 正确
- [ ] 操作日志：每次 mutation 写入 `ff_document_operation_log`
- [ ] 协同编辑：开两个标签页，确认一端的修改能实时同步到另一端
- [ ] 乐观锁：模拟并发修改，确认 `cs_rej` 重试逻辑正常
- [ ] Tailwind 样式不破坏 Univer UI

---

#### Task 8.3：代码整理

- [ ] 确认 `DataJson` 字段不放入 Vue 响应式系统（只在 `onMounted` 中传给 Univer，之后通过 `.save()` 获取）
- [ ] 确认 Univer 容器 `div` 有明确 `height`，不能是 `auto`
- [ ] Worker 文件确认服务器返回 `application/javascript` MIME 类型
- [ ] 清理 `packages/flowFlex-common/` 安装的 Univer 包，确认 `package.json` 版本固定（使用 `^` 限定 minor 范围，不使用 `*`）

---

## 四、任务依赖关系

```
Task 1.1 (实体)
    ↓
Task 1.2 (实体)
    ↓
Task 1.3 (Repository)
    ↓
Task 1.4 (Migration) ── 需等 Task 1.1~1.3 完成
    ↓
Task 2.1 (DTO)
    ↓
Task 2.2 (IService 接口)
    ↓
Task 2.3 (Service 实现)
    ↓
Task 2.4 (AutoMapper)
    ↓
Task 2.5 (Controller) ── 后端主链路结束
Task 2.6 (SignalR Hub) ── 可并行

Task 3.1 (npm install)
    ↓
Task 3.2 (Tailwind 配置) ── 先配置，防止样式冲突
    ↓
Task 3.3 (Vite 配置)
    ↓
Task 4.1~4.4 (组件封装) ── 需 Phase 3 完成
    ↓
Task 5.1 (协同 Composable) ── 依赖 Task 4.x + 后端 SignalR Hub
    ↓
Task 6.1 (API 层) ── 可并行于 Task 4.x，依赖后端 Phase 2
    ↓
Task 6.2 (Store：documentEditor + documentList)
    ↓
Task 7.1 (后端补充分页查询接口) ── 依赖 Task 2.x 后端完成
Task 7.2 (列表页 Store：documentList.ts)
    ↓
Task 7.3 (新建文档弹窗：CreateDocumentDialog.vue) ── 依赖 Task 7.2
    ↓
Task 7.4 (文档列表页：/documents/list) ── 依赖 Task 7.2 + 7.3
Task 7.5 (文档编辑器页：/documents/:unitId/edit) ── 依赖 Task 4.x + 6.2 + 5.1
    ↓
Task 7.6 (菜单路由注册 + 权限码 + i18n) ── 依赖 Task 7.4 + 7.5
    ↓
Task 8.1~8.3 (测试 + 收尾)
```

---

## 五、工作量估算

| Phase    | 任务                                 | 估算工时 |
| -------- | ------------------------------------ | -------- |
| Phase 1  | 实体 + Repository + Migration        | 4h       |
| Phase 2  | DTO + Service + Controller + SignalR | 8h       |
| Phase 3  | 前端依赖 + 配置                      | 1h       |
| Phase 4  | 三个 Univer 组件封装                 | 4h       |
| Phase 5  | 协同 Composable                      | 4h       |
| Phase 6  | API 层 + Store                       | 2h       |
| Phase 7  | 列表页 + 编辑器页 + 弹窗 + 路由      | **6h**   |
| Phase 8  | 测试 + 收尾                          | 4h       |
| **合计** |                                      | **~33h** |

---

## 六、注意事项与风险

| 事项                     | 说明                                                      | 处置                                                    |
| ------------------------ | --------------------------------------------------------- | ------------------------------------------------------- |
| **Tailwind 样式冲突**    | Tailwind base reset 会破坏 Univer UI                      | Task 3.2 优先处理，使用 `prefix` 或 `important: '#app'` |
| **响应式性能**           | `workbookData` 不能放入 `ref/reactive`                    | 组件内直接 pass，保存用 `.save()` 获取                  |
| **容器高度**             | Univer 渲染依赖明确高度                                   | 所有容器传明确像素值，不用 `auto`                       |
| **Web Worker MIME**      | Vite/Kestrel 需正确返回 JS MIME 类型                      | 本地 Vite 自动处理，生产环境 Kestrel 需确认静态文件配置 |
| **SignalR vs WebSocket** | 前端 `@univerjs/network` 用原生 WebSocket，后端用 SignalR | SignalR 在 Hub 内暴露 WebSocket 端点即可兼容            |
| **协同 OT 变换**         | 当前方案是简单乐观锁 + 重试，不做完整 OT                  | 对大多数业务场景足够，并发冲突率低时可接受              |
| **Long ID 序列化**       | `EntityId` 等 long 字段需加 `LongToStringConverter`       | 已在实体设计中标注                                      |
| **多租户隔离**           | `UnitId` 唯一索引需包含 `tenant_id + app_code`            | Migration SQL 中已包含复合唯一索引                      |

---

## 七、文件清单（全部新增文件）

### 后端

```
Domain/Entities/OW/
  DocumentSnapshot.cs
  DocumentOperationLog.cs

Domain/Repository/OW/
  IDocumentSnapshotRepository.cs
  IDocumentOperationLogRepository.cs

SqlSugarDB/Repositories/OW/
  DocumentSnapshotRepository.cs
  DocumentOperationLogRepository.cs

SqlSugarDB/Migrations/
  Migration_20260922000001_CreateDocumentTables.cs

Application.Contracts/Dtos/OW/DocumentSnapshot/
  DocumentSnapshotInputDto.cs
  DocumentSnapshotOutputDto.cs
  DocumentOperationLogInputDto.cs
  DocumentOperationLogOutputDto.cs

Application.Contracts/IServices/OW/
  IDocumentSnapshotService.cs
  IDocumentOperationLogService.cs

Application/Services/OW/
  DocumentSnapshotService.cs
  DocumentOperationLogService.cs

Application/Maps/
  DocumentSnapshotMapProfile.cs

WebApi/Controllers/OW/
  DocumentSnapshotController.cs
  DocumentOperationLogController.cs

WebApi/Hubs/
  CollabHub.cs

Tests/FlowFlex.Tests/OW/
  DocumentSnapshotServiceTests.cs
```

### 前端

```
src/app/components/UniverEditor/
  UniverSheet.vue
  UniverDoc.vue
  UniverSlide.vue
  index.ts

src/app/composables/
  useCollaboration.ts

src/app/apis/ow/
  documentSnapshot.ts
  documentOperationLog.ts

src/app/stores/modules/
  documentList.ts      ← 列表页 Store
  documentEditor.ts    ← 编辑器页 Store

src/app/views/document/
  list/
    index.vue                  ← 文档列表页
    CreateDocumentDialog.vue   ← 新建文档弹窗
  editor/
    index.vue                  ← 文档编辑器页

src/app/router/routers/modules/
  document.ts          ← 独立菜单路由模块

src/assets/svg/menu/
  document.svg         ← 菜单图标（可选，暂用 management.svg 替代）
```

**i18n 变更：**

- 在语言包中新增 `sys.router.document: '文档管理'`

**权限码新增（后端）：**

- `WebApi/Domain/Shared/Const/PermissionConsts.cs` 新增 `Document` 静态类

---

_文档由 Kiro AI 生成，基于 INTEGRATION_GUIDE.md 及项目现有代码结构分析。_
