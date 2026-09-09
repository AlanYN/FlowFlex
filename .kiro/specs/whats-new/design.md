# Design Document — OW-720 What's New

## Overview

为 FlowFlex WFE 系统引入轻量级产品更新通知机制。用户通过顶部导航栏铃铛图标感知新内容，System Admin 通过专属管理页面维护更新记录。本文档覆盖 Phase 1 MVP 的完整技术设计。

---

## Architecture

### 系统分层架构

```
┌──────────────────────────────────────────────────────────────────┐
│                     Frontend (Vue 3 SPA)                          │
│                                                                    │
│  navbar.vue ──────── WhatsNewBell.vue                             │
│                           │                                        │
│                    WhatsNewDetail.vue (el-dialog)                  │
│                                                                    │
│  userLayout.vue ─── "Manage What's New" menu item                 │
│                                                                    │
│  views/whatsNewManagement/                                         │
│    index.vue                                                       │
│    components/WhatsNewFormModal.vue                                │
│                                                                    │
│  apis/whatsNew/index.ts ──────────── Axios (JWT + AppCode)        │
└─────────────────────────────┬────────────────────────────────────┘
                              │ HTTP/REST (Bearer JWT)
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│                      WebApi Layer                                  │
│  WhatsNewController (ow/whats-new/v1/*)                           │
│  [Authorize] 用户端 + [WFEAuthorize] 管理端                        │
└──────────────────┬───────────────────────────────────────────────┘
                   │
                   ▼
┌──────────────────────────────────────────────────────────────────┐
│                    Application Layer                               │
│  IWhatsNewService / WhatsNewService                               │
│  (XSS 白名单过滤、Redis 缓存、已读计数逻辑)                          │
└──────────────────┬───────────────────────────────────────────────┘
                   │
          ┌────────┴────────┐
          ▼                 ▼
┌─────────────────┐   ┌──────────────────┐
│  WhatsNew       │   │ WhatsNewReadStatus│
│  Repository     │   │ Repository        │
│  (SqlSugar ORM) │   │ (SqlSugar ORM)    │
└────────┬────────┘   └────────┬─────────┘
         │                     │
         ▼                     ▼
┌──────────────────────────────────────┐   ┌─────────────────┐
│  PostgreSQL                           │   │   Redis Cache   │
│  ff_whats_new                         │   │ unread-count    │
│  ff_whats_new_read_status             │   │ (TTL 10 min)    │
└──────────────────────────────────────┘   └─────────────────┘
```

### 核心数据流

**用户端（查看通知）：**

1. 页面加载 → `WhatsNewBell.onMounted` → `GET ow/whats-new/v1/unread-count`（Redis 优先）
2. 点击铃铛 → `GET ow/whats-new/v1/panel` → 渲染最多 10 条 Published 更新
3. 点击某条目 → `WhatsNewDetail` 打开 → `POST ow/whats-new/v1/{id}/read` → 前端本地 unreadCount--
4. Mark all as read → `POST ow/whats-new/v1/read-all` → 前端本地 unreadCount = 0

**管理端（System Admin）：**

1. 头像菜单点击"Manage What's New" → 路由跳转 `/whats-new-management`
2. 路由守卫校验 `userType === 1`，非 Admin 重定向到 `/`
3. `GET ow/whats-new/v1/admin` → 列表 + 统计卡片数据
4. 创建/编辑 → `WhatsNewFormModal` → `POST/PUT ow/whats-new/v1/admin[/{id}]`
5. 删除 → 二次确认弹窗（读本地 `readCount`）→ `DELETE ow/whats-new/v1/admin/{id}`

---

## Components and Interfaces

### 后端组件

#### 1. Domain Entities

**`Domain/Entities/OW/WhatsNew.cs`**

```csharp
[SugarTable("ff_whats_new")]
public class WhatsNew : EntityBaseCreateInfo
{
    [SugarColumn(ColumnName = "title", Length = 100)]
    public string Title { get; set; }

    [SugarColumn(ColumnName = "summary", Length = 200)]
    public string Summary { get; set; }

    /// <summary>HTML 正文，存储前已完成 XSS 白名单过滤</summary>
    [SugarColumn(ColumnName = "content", ColumnDataType = "text")]
    public string Content { get; set; }

    /// <summary>NewFeature / Improvement / BugFix / Announcement</summary>
    [SugarColumn(ColumnName = "category", Length = 50)]
    public string Category { get; set; }

    /// <summary>0=Draft, 1=Published（Phase 2 加 2=Scheduled）</summary>
    [SugarColumn(ColumnName = "status")]
    public int Status { get; set; } = 0;

    /// <summary>实际发布时间，Publish Now 时写入</summary>
    [SugarColumn(ColumnName = "publish_time", IsNullable = true)]
    public DateTimeOffset? PublishTime { get; set; }

    /// <summary>计划发布时间，Phase 2 使用，暂留字段</summary>
    [SugarColumn(ColumnName = "scheduled_time", IsNullable = true)]
    public DateTimeOffset? ScheduledTime { get; set; }

    // EntityBaseCreateInfo 提供：
    // CreateDate, ModifyDate, CreateBy, ModifyBy, CreateUserId, ModifyUserId
    // EntityBase 提供：
    // Id (snowflake long), AppCode, TenantId, IsValid (软删除)
}
```

**`Domain/Entities/OW/WhatsNewReadStatus.cs`**

```csharp
/// <summary>
/// 每位用户对每条更新的已读状态记录。
/// 不继承 EntityBaseCreateInfo（无审计字段需求），只继承 EntityBase。
/// 注意：此表不走多租户全局过滤（查询时需手动加 app_code + tenant_id 条件）。
/// </summary>
[SugarTable("ff_whats_new_read_status")]
public class WhatsNewReadStatus : IdEntityBase
{
    [SugarColumn(ColumnName = "whats_new_id")]
    public long WhatsNewId { get; set; }

    [SugarColumn(ColumnName = "user_id")]
    public long UserId { get; set; }

    [SugarColumn(ColumnName = "read_time")]
    public DateTimeOffset ReadTime { get; set; }

    [SugarColumn(ColumnName = "app_code", Length = 50)]
    public string AppCode { get; set; }

    [SugarColumn(ColumnName = "tenant_id", Length = 50)]
    public string TenantId { get; set; }
}
```

> **注意**：`WhatsNewReadStatus` 继承 `IdEntityBase`（提供 snowflake `Id`），不继承 `EntityBaseCreateInfo`（避免自动注入不需要的审计字段）。实际继承链需确认项目 `IdEntityBase` 是否存在，若无则直接继承 `EntityBase`。

---

#### 2. Repository 接口

**`Domain/Repository/OW/IWhatsNewRepository.cs`**

```csharp
public interface IWhatsNewRepository : IBaseRepository<WhatsNew>
{
    /// <summary>获取 Published 状态的更新列表（面板用）</summary>
    Task<List<WhatsNew>> GetPublishedListAsync(int limit = 10);

    /// <summary>获取管理列表（含 readCount），支持 status 过滤</summary>
    Task<List<WhatsNewAdminItemProjection>> GetAdminListAsync(int? statusFilter = null);

    /// <summary>获取 Published / Draft 统计计数</summary>
    Task<(int publishedCount, int draftCount)> GetStatusCountsAsync();
}

/// <summary>管理列表投影，含 readCount</summary>
public class WhatsNewAdminItemProjection
{
    public long Id { get; set; }
    public string Title { get; set; }
    public string Summary { get; set; }
    public string Category { get; set; }
    public int Status { get; set; }
    public DateTimeOffset? PublishTime { get; set; }
    public int ReadCount { get; set; }
}
```

**`Domain/Repository/OW/IWhatsNewReadStatusRepository.cs`**

```csharp
public interface IWhatsNewReadStatusRepository : IBaseRepository<WhatsNewReadStatus>
{
    /// <summary>
    /// 幂等标记单条为已读。
    /// 底层使用 INSERT ... ON CONFLICT DO NOTHING。
    /// </summary>
    Task MarkReadAsync(long whatsNewId, long userId, string appCode, string tenantId);

    /// <summary>批量标记所有 Published 条目为已读</summary>
    Task MarkAllReadAsync(List<long> whatsNewIds, long userId, string appCode, string tenantId);

    /// <summary>查询当前用户已读的 whatsNewId 集合</summary>
    Task<HashSet<long>> GetReadIdsAsync(long userId, string appCode, string tenantId);

    /// <summary>获取某条更新的已读用户数（管理端用）</summary>
    Task<int> GetReadCountAsync(long whatsNewId, string appCode, string tenantId);

    /// <summary>获取当前用户的未读已发布更新数量</summary>
    Task<int> GetUnreadCountAsync(
        long userId,
        string appCode,
        string tenantId,
        List<long> publishedIds);
}
```

---

#### 3. DTOs & Request/Response 类

**`Application.Contracts/Dtos/OW/WhatsNew/`**

```csharp
// 面板列表条目 DTO（用户端）
public class WhatsNewPanelItemDto
{
    [JsonConverter(typeof(LongToStringConverter))]
    public long Id { get; set; }
    public string Title { get; set; }
    public string Summary { get; set; }
    public string Category { get; set; }
    public DateTimeOffset? PublishTime { get; set; }
    public bool IsRead { get; set; }
}

// 面板响应 DTO
public class WhatsNewPanelResponseDto
{
    public List<WhatsNewPanelItemDto> Items { get; set; }
    public int UnreadCount { get; set; }
}

// 详情 DTO（用户端，含富文本 content）
public class WhatsNewDetailDto
{
    [JsonConverter(typeof(LongToStringConverter))]
    public long Id { get; set; }
    public string Title { get; set; }
    public string Summary { get; set; }
    public string Content { get; set; }  // XSS 过滤后的 HTML
    public string Category { get; set; }
    public DateTimeOffset? PublishTime { get; set; }
    public bool IsRead { get; set; }
}

// 管理端列表条目 DTO
public class WhatsNewAdminItemDto
{
    [JsonConverter(typeof(LongToStringConverter))]
    public long Id { get; set; }
    public string Title { get; set; }
    public string Summary { get; set; }
    public string Category { get; set; }
    public int Status { get; set; }
    public DateTimeOffset? PublishTime { get; set; }
    public int ReadCount { get; set; }
}

// 管理端列表响应 DTO（含统计）
public class WhatsNewAdminListResponseDto
{
    public List<WhatsNewAdminItemDto> Items { get; set; }
    public int PublishedCount { get; set; }
    public int DraftCount { get; set; }
}

// 创建请求 DTO（管理端）
public class CreateWhatsNewRequest
{
    [Required, MaxLength(100)]
    public string Title { get; set; }

    [Required, MaxLength(200)]
    public string Summary { get; set; }

    [Required]
    public string Content { get; set; }

    [Required]
    public string Category { get; set; }

    /// <summary>0=Draft, 1=Published</summary>
    public int Status { get; set; } = 0;
}

// 更新请求 DTO（管理端）
public class UpdateWhatsNewRequest
{
    [Required, MaxLength(100)]
    public string Title { get; set; }

    [Required, MaxLength(200)]
    public string Summary { get; set; }

    [Required]
    public string Content { get; set; }

    [Required]
    public string Category { get; set; }

    public int Status { get; set; }
}
```

---

#### 4. Service 接口

**`Application.Contracts/IServices/OW/IWhatsNewService.cs`**

```csharp
public interface IWhatsNewService : IScopedService
{
    // ── 用户端 ──────────────────────────────────────────────────────

    /// <summary>获取当前用户未读数（Redis 优先，Miss 时查 DB）</summary>
    Task<int> GetUnreadCountAsync();

    /// <summary>获取面板列表（最多10条 Published，含 isRead）</summary>
    Task<WhatsNewPanelResponseDto> GetPanelAsync();

    /// <summary>获取单条详情（含完整 content）</summary>
    Task<WhatsNewDetailDto> GetDetailAsync(long id);

    /// <summary>标记单条为已读（幂等）并清除 Redis 缓存</summary>
    Task MarkReadAsync(long id);

    /// <summary>标记所有 Published 为已读并清除 Redis 缓存</summary>
    Task MarkAllReadAsync();

    // ── 管理端 ──────────────────────────────────────────────────────

    /// <summary>获取管理列表（含统计计数，支持 status 过滤）</summary>
    Task<WhatsNewAdminListResponseDto> GetAdminListAsync(int? status = null);

    /// <summary>创建更新（XSS 过滤 content，status=1 时写 publish_time）</summary>
    Task<long> CreateAsync(CreateWhatsNewRequest request);

    /// <summary>编辑更新（同上，Draft→Published 时写 publish_time）</summary>
    Task<bool> UpdateAsync(long id, UpdateWhatsNewRequest request);

    /// <summary>软删除（is_valid = false），保留 read_status 历史</summary>
    Task<bool> DeleteAsync(long id);
}
```

---

#### 5. Service 实现要点

**`Application/Services/OW/WhatsNewService.cs`**

```
实现类签名：
  public class WhatsNewService : IWhatsNewService, IScopedService

构造函数注入：
  - IWhatsNewRepository
  - IWhatsNewReadStatusRepository
  - IDistributedCacheService
  - UserContext
  - ILogger<WhatsNewService>
```

**关键方法逻辑：**

| 方法                  | 实现逻辑                                                                                                                                                                                |
| --------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `GetUnreadCountAsync` | 构建 Redis key `whats-new:unread:{appCode}:{tenantId}:{userId}`；先 `_cacheService.GetAsync<int?>(key)`；Miss 时查 DB 并 `_cacheService.SetAsync(key, count, TimeSpan.FromMinutes(10))` |
| `GetPanelAsync`       | 查 status=1 的最新 10 条（按 publish_time DESC）；`GetReadIdsAsync` 获取已读集合；注入 `isRead`；同时返回 `UnreadCount`                                                                 |
| `MarkReadAsync(id)`   | `_readStatusRepo.MarkReadAsync(id, userId, ...)` → `_cacheService.RemoveAsync(cacheKey)`                                                                                                |
| `MarkAllReadAsync`    | 先查所有 Published id → 批量 INSERT → `_cacheService.RemoveAsync(cacheKey)`                                                                                                             |
| `CreateAsync`         | 调用 XSS 白名单过滤（见"XSS 防护"章节）；`status == 1` 时设 `PublishTime = DateTimeOffset.UtcNow`；save                                                                                 |
| `UpdateAsync`         | 加载现有记录；XSS 过滤 content；若 `status` 从 0 → 1 则设 `PublishTime = now`；**若原 status 已为 1（Published），`publish_time` 不变，已读状态不重置**；update                         |
| `DeleteAsync`         | `entity.IsValid = false`；`UpdateAsync`（软删除）；不删 `ff_whats_new_read_status`                                                                                                      |
| `GetAdminListAsync`   | 调用 `_repo.GetAdminListAsync(status)`（返回含 readCount 的 projection）；计算 publishedCount / draftCount                                                                              |

---

#### 6. Controller

**`WebApi/Controllers/OW/WhatsNewController.cs`**

```csharp
[Route("ow/whats-new/v{version:apiVersion}")]
[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Authorize]
public class WhatsNewController : Controllers.ControllerBase
{
    // ── 用户端（[Authorize] 继承类级别） ───────────────────────────

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()

    [HttpGet("panel")]
    public async Task<IActionResult> GetPanel()

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetDetail(long id)

    [HttpPost("{id:long}/read")]
    public async Task<IActionResult> MarkRead(long id)

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()

    // ── 管理端（额外加 [WFEAuthorize]） ───────────────────────────

    [HttpGet("admin")]
    [WFEAuthorize]
    public async Task<IActionResult> GetAdminList([FromQuery] int? status = null)

    [HttpPost("admin")]
    [WFEAuthorize]
    public async Task<IActionResult> Create([FromBody] CreateWhatsNewRequest request)

    [HttpPut("admin/{id:long}")]
    [WFEAuthorize]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateWhatsNewRequest request)

    [HttpDelete("admin/{id:long}")]
    [WFEAuthorize]
    public async Task<IActionResult> Delete(long id)
}
```

> **权限说明**：管理端方法使用 `[WFEAuthorize]`（无 PermissionConsts 参数），依赖 `WfeAuthorizationHandler` 中 `IsSystemAdmin == true` 的 bypass 逻辑。只有 `UserType = 1` 的 System Admin 才能通过。

---

#### 7. AutoMapper Profile

**`Application/Maps/WhatsNewMapProfile.cs`**

```csharp
public class WhatsNewMapProfile : Profile
{
    public WhatsNewMapProfile()
    {
        CreateMap<WhatsNew, WhatsNewPanelItemDto>();
        CreateMap<WhatsNew, WhatsNewDetailDto>();
        CreateMap<WhatsNew, WhatsNewAdminItemDto>();
        CreateMap<CreateWhatsNewRequest, WhatsNew>();
        CreateMap<UpdateWhatsNewRequest, WhatsNew>();
    }
}
```

> 注册在 `Program.cs` 或已有的 AutoMapper 扫描程序集范围内，无需额外配置。

---

### 前端组件

#### 文件清单

**新增文件：**

| 文件路径                                                            | 说明                                |
| ------------------------------------------------------------------- | ----------------------------------- |
| `src/app/apis/whatsNew/index.ts`                                    | API 请求层，所有 whats-new 相关接口 |
| `src/app/components/navbarCompanents/WhatsNewBell.vue`              | 铃铛图标 + 红点 + el-popover 面板   |
| `src/app/components/navbarCompanents/WhatsNewDetail.vue`            | 更新详情 el-dialog                  |
| `src/app/views/whatsNewManagement/index.vue`                        | 管理列表页                          |
| `src/app/views/whatsNewManagement/components/WhatsNewFormModal.vue` | 创建/编辑弹窗                       |
| `src/app/router/routers/modules/whatsNewManagement.ts`              | 路由模块配置                        |

**修改文件：**

| 文件路径                                             | 改动                                                                  |
| ---------------------------------------------------- | --------------------------------------------------------------------- |
| `src/app/components/layout/components/navbar.vue`    | 在 `<Setting />` 后、`<UserLayout />` 前插入 `<WhatsNewBell />`       |
| `src/app/components/navbarCompanents/userLayout.vue` | 在"My Profile"菜单项后加"Manage What's New"（`v-if="isSystemAdmin"`） |
| `src/app/router/routers/index.ts`（或路由汇总文件）  | 导入并注册 `whatsNewManagement` 路由模块                              |

---

#### 组件接口定义

**`WhatsNewBell.vue`**

```typescript
// Props：无（内部管理状态）
// Emits：无

// 内部状态
const unreadCount = ref<number>(0);
const panelVisible = ref<boolean>(false);
const panelItems = ref<WhatsNewPanelItem[]>([]);
const panelLoading = ref<boolean>(false);

// 对外暴露（供 WhatsNewDetail 回调）
// 通过 provide/inject 向子组件共享 unreadCount 和 markRead 方法
provide("whatsNewState", {
  unreadCount,
  decrementUnread: () => {
    unreadCount.value = Math.max(0, unreadCount.value - 1);
  },
  markItemAsRead: (id: string) => {
    /* 将 panelItems 中对应条目 isRead 置 true */
  },
  clearAllUnread: () => {
    unreadCount.value = 0;
    panelItems.value.forEach((i) => (i.isRead = true));
  },
});
```

**`WhatsNewDetail.vue`**

```typescript
// Props
interface Props {
  item: WhatsNewPanelItem | null; // 从 panel 传入的基础信息
}

// Emits
defineEmits<{
  closed: [];
  read: [id: string]; // 标记已读成功后触发，传递 id
}>();

// 通过 inject 消费 whatsNewState
const { decrementUnread, markItemAsRead } = inject("whatsNewState");

// 内部状态
const visible = ref<boolean>(false);
const detail = ref<WhatsNewDetailResponse | null>(null);
const loading = ref<boolean>(false);

// open(item) 方法：加载详情 + 触发 markRead
```

**`WhatsNewFormModal.vue`**

```typescript
// Props
interface Props {
  mode: "create" | "edit";
  item?: WhatsNewAdminItem | null; // edit 时传入
}

// Emits
defineEmits<{
  success: []; // 提交成功后触发，父组件刷新列表
  close: [];
}>();

// 内部表单状态
const form = reactive({
  title: "",
  summary: "",
  content: "",
  category: "",
  publishingMode: "draft" as "publish" | "schedule" | "draft",
});

const rules = {
  /* ElForm 校验规则 */
};
```

---

#### API 函数签名

**`src/app/apis/whatsNew/index.ts`**

```typescript
import { defHttp } from "@/apis/axios";
import { useGlobSetting } from "@/settings";

const globSetting = useGlobSetting();

const Api = (id?: string | number) => ({
  unreadCount: `${globSetting.apiProName}/ow/whats-new/${globSetting.apiVersion}/unread-count`,
  panel: `${globSetting.apiProName}/ow/whats-new/${globSetting.apiVersion}/panel`,
  detail: `${globSetting.apiProName}/ow/whats-new/${globSetting.apiVersion}/${id}`,
  read: `${globSetting.apiProName}/ow/whats-new/${globSetting.apiVersion}/${id}/read`,
  readAll: `${globSetting.apiProName}/ow/whats-new/${globSetting.apiVersion}/read-all`,
  adminList: `${globSetting.apiProName}/ow/whats-new/${globSetting.apiVersion}/admin`,
  adminItem: `${globSetting.apiProName}/ow/whats-new/${globSetting.apiVersion}/admin/${id}`,
});

// 用户端
export function getUnreadCount(): Promise<number>;
export function getPanel(): Promise<WhatsNewPanelResponse>;
export function getDetail(id: string): Promise<WhatsNewDetail>;
export function markRead(id: string): Promise<boolean>;
export function markAllRead(): Promise<boolean>;

// 管理端
export function getAdminList(
  status?: number,
): Promise<WhatsNewAdminListResponse>;
export function createWhatsNew(data: CreateWhatsNewRequest): Promise<string>;
export function updateWhatsNew(
  id: string,
  data: UpdateWhatsNewRequest,
): Promise<boolean>;
export function deleteWhatsNew(id: string): Promise<boolean>;
```

**TypeScript 类型定义（建议放于 `types/whatsNew.d.ts`）：**

```typescript
export interface WhatsNewPanelItem {
  id: string;
  title: string;
  summary: string;
  category: "NewFeature" | "Improvement" | "BugFix" | "Announcement";
  publishTime: string;
  isRead: boolean;
}

export interface WhatsNewPanelResponse {
  items: WhatsNewPanelItem[];
  unreadCount: number;
}

export interface WhatsNewDetail extends WhatsNewPanelItem {
  content: string; // 原始 HTML，渲染前须 DOMPurify.sanitize()
}

export interface WhatsNewAdminItem {
  id: string;
  title: string;
  summary: string;
  category: string;
  status: 0 | 1;
  publishTime: string | null;
  readCount: number;
}

export interface WhatsNewAdminListResponse {
  items: WhatsNewAdminItem[];
  publishedCount: number;
  draftCount: number;
}

export interface CreateWhatsNewRequest {
  title: string;
  summary: string;
  content: string;
  category: string;
  status: 0 | 1;
}

export interface UpdateWhatsNewRequest extends CreateWhatsNewRequest {}
```

---

#### 路由模块

**`src/app/router/routers/modules/whatsNewManagement.ts`**

```typescript
import type { AppRouteModule } from "@/router/types";
import { LAYOUT } from "@/router/constant";

const whatsNewManagement: AppRouteModule = {
  path: "/whats-new-management",
  name: "WhatsNewManagement",
  component: LAYOUT,
  redirect: "/whats-new-management/index",
  meta: {
    title: "What's New Management",
    hidden: true, // 不出现在侧边栏菜单
    status: true,
  },
  children: [
    {
      path: "index",
      name: "WhatsNewManagementIndex",
      component: () => import("@/views/whatsNewManagement/index.vue"),
      meta: {
        title: "What's New Management",
        hidden: true,
        status: true,
      },
    },
  ],
};

export default whatsNewManagement;
```

**路由守卫逻辑（在 `whatsNewManagement/index.vue` 的 `onBeforeMount` 或路由 `beforeEnter`）：**

```typescript
// 方案：在 index.vue 的 onMounted 中处理
const userStore = useUserStore();
onMounted(() => {
  if (userStore.getUserInfo?.userType !== 1) {
    router.replace("/");
  }
});
```

---

## Data Models

### 数据库 DDL（PostgreSQL）

#### 表 1：`ff_whats_new`

```sql
CREATE TABLE IF NOT EXISTS ff_whats_new (
    id              BIGINT          NOT NULL PRIMARY KEY,
    app_code        VARCHAR(50)     NOT NULL DEFAULT '',
    tenant_id       VARCHAR(50)     NOT NULL DEFAULT '',
    title           VARCHAR(100)    NOT NULL,
    summary         VARCHAR(200)    NOT NULL,
    content         TEXT            NOT NULL,
    category        VARCHAR(50)     NOT NULL,
    status          INT             NOT NULL DEFAULT 0,
    publish_time    TIMESTAMPTZ,
    scheduled_time  TIMESTAMPTZ,
    create_date     TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    modify_date     TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    create_by       VARCHAR(100)    NOT NULL DEFAULT '',
    modify_by       VARCHAR(100)    NOT NULL DEFAULT '',
    create_user_id  BIGINT          NOT NULL DEFAULT 0,
    modify_user_id  BIGINT          NOT NULL DEFAULT 0,
    is_valid        BOOLEAN         NOT NULL DEFAULT TRUE
);

CREATE INDEX IF NOT EXISTS idx_ff_whats_new_status_publish_time
    ON ff_whats_new (status, publish_time DESC)
    WHERE is_valid = TRUE;

CREATE INDEX IF NOT EXISTS idx_ff_whats_new_app_tenant
    ON ff_whats_new (app_code, tenant_id)
    WHERE is_valid = TRUE;
```

#### 表 2：`ff_whats_new_read_status`

```sql
CREATE TABLE IF NOT EXISTS ff_whats_new_read_status (
    id              BIGINT          NOT NULL PRIMARY KEY,
    whats_new_id    BIGINT          NOT NULL,
    user_id         BIGINT          NOT NULL,
    read_time       TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    app_code        VARCHAR(50)     NOT NULL DEFAULT '',
    tenant_id       VARCHAR(50)     NOT NULL DEFAULT ''
);

-- 唯一约束：防止同一用户对同一条更新重复插入
CREATE UNIQUE INDEX IF NOT EXISTS uidx_ff_whats_new_read_status_unique
    ON ff_whats_new_read_status (whats_new_id, user_id, app_code, tenant_id);

CREATE INDEX IF NOT EXISTS idx_ff_whats_new_read_status_user
    ON ff_whats_new_read_status (user_id, app_code, tenant_id);

CREATE INDEX IF NOT EXISTS idx_ff_whats_new_read_status_whats_new
    ON ff_whats_new_read_status (whats_new_id);
```

### Migration 文件

**文件名**：`SqlSugarDB/Migrations/Migration_20260828001_AddWhatsNew.cs`

```csharp
public static class Migration_20260828001_AddWhatsNew
{
    public static void Up(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ff_whats_new ( ... );
            CREATE INDEX IF NOT EXISTS ...;
            CREATE TABLE IF NOT EXISTS ff_whats_new_read_status ( ... );
            CREATE UNIQUE INDEX IF NOT EXISTS ...;
            CREATE INDEX IF NOT EXISTS ...;
        ");
    }

    public static void Down(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"
            DROP TABLE IF EXISTS ff_whats_new_read_status;
            DROP TABLE IF EXISTS ff_whats_new;
        ");
    }
}
```

**MigrationManager.cs 注册（在 migrations 数组末尾追加）：**

```csharp
("20260828001_AddWhatsNew", (Action)(() => Migration_20260828001_AddWhatsNew.Up(_db))),
```

---

## State Management

### 前端状态共享策略

WhatsNew 功能的核心状态为 `unreadCount`，需要在以下组件间共享：

```
navbar.vue
  └── WhatsNewBell.vue      ← 拥有状态，provide 给子组件
        ├── WhatsNewPanel (内联于 el-popover)
        │     └── 列表条目（isRead 状态）
        └── WhatsNewDetail.vue  ← inject 消费状态
```

**选用 `provide/inject` 而非 Pinia Store 的理由：**

- 该状态生命周期与 `WhatsNewBell` 组件绑定（页面导航时自然销毁）
- 不需要跨路由持久化
- 组件层级明确，不超过3层

**Provide 契约（`WhatsNewBell.vue` 内）：**

```typescript
interface WhatsNewStateContext {
  unreadCount: Ref<number>;
  panelItems: Ref<WhatsNewPanelItem[]>;
  decrementUnread: () => void;
  clearAllUnread: () => void;
  markItemAsRead: (id: string) => void;
}

provide<WhatsNewStateContext>("whatsNewState", {
  unreadCount,
  panelItems,
  decrementUnread: () => {
    unreadCount.value = Math.max(0, unreadCount.value - 1);
  },
  clearAllUnread: () => {
    unreadCount.value = 0;
    panelItems.value.forEach((item) => {
      item.isRead = true;
    });
  },
  markItemAsRead: (id: string) => {
    const item = panelItems.value.find((i) => i.id === id);
    if (item) item.isRead = true;
  },
});
```

**WhatsNewDetail.vue 中消费：**

```typescript
const state = inject<WhatsNewStateContext>("whatsNewState");

// 详情弹窗 open 事件回调
const onDialogOpen = async () => {
  if (!props.item) return;
  loading.value = true;
  try {
    detail.value = await getDetail(props.item.id);
    // 只有原本未读才调接口 + 更新计数
    if (!props.item.isRead) {
      await markRead(props.item.id); // 静默失败
      state?.markItemAsRead(props.item.id);
      state?.decrementUnread();
    }
  } finally {
    loading.value = false;
  }
};
```

**管理列表页状态：**
管理页为独立路由，状态完全本地，不需要与铃铛共享。

```typescript
// whatsNewManagement/index.vue 内部
const listData = ref<WhatsNewAdminItem[]>([]);
const publishedCount = ref<number>(0);
const draftCount = ref<number>(0);
const loading = ref<boolean>(false);

const activeFilter = ref<number | null>(null);

const loadList = async (status?: number) => {
  loading.value = true;
  const res = await getAdminList(status);
  listData.value = res.items;
  publishedCount.value = res.publishedCount;
  draftCount.value = res.draftCount;
  loading.value = false;
};

const handleCardClick = (status: 0 | 1) => {
  if (activeFilter.value === status) {
    // 再次点击取消筛选
    activeFilter.value = null;
    loadList();
  } else {
    activeFilter.value = status;
    loadList(status);
  }
};
```

**统计卡片筛选交互：**

- 每个卡片同时作为筛选按钮
- 点击"Published"卡片 → `activeFilter = 'published'`，调用 `getAdminList(1)`，卡片高亮（如加粗边框或背景色变化）
- 点击"Drafts"卡片 → `activeFilter = 'draft'`，调用 `getAdminList(0)`，卡片高亮
- 再次点击已激活卡片 → `activeFilter = null`，调用 `getAdminList()`（无 status 参数），所有卡片恢复默认样式
- `activeFilter` 为本地 ref 状态，不持久化

---

## Cache Strategy

### Redis 缓存设计

**缓存 Key 格式：**

```
whats-new:unread:{appCode}:{tenantId}:{userId}
```

示例：`whats-new:unread:WFE:100001:123456789`

**TTL**：10 分钟（`TimeSpan.FromMinutes(10)`）

**缓存读取流程（`GetUnreadCountAsync`）：**

```
1. 构建 cacheKey
2. _cacheService.GetAsync<string>(cacheKey)
3. if (cached != null) return int.Parse(cached)
4. 查 DB：count WHERE status=Published AND NOT IN (user's read_status)
5. _cacheService.SetAsync(cacheKey, count.ToString(), 10min)
6. return count
```

**缓存失效触发点：**

| 操作                      | 失效动作                                                                           |
| ------------------------- | ---------------------------------------------------------------------------------- |
| `MarkReadAsync(id)`       | `RemoveAsync(cacheKey)`                                                            |
| `MarkAllReadAsync()`      | `RemoveAsync(cacheKey)`                                                            |
| Admin 创建 Published 更新 | 不主动清除（等 TTL 自然过期，或可选 `RemoveByPatternAsync("whats-new:unread:*")`） |
| Admin 删除更新（软删除）  | 不主动清除（等 TTL 过期）                                                          |

> **决策**：Admin 创建/删除操作不主动清除全量缓存，依赖 10 分钟 TTL 自然过期。这符合"零轮询 + 低运维成本"的原则，且 unread-count 的轻微延迟（最多 10 分钟）对用户体验无负面影响。

**缓存值类型**：
`IDistributedCacheService.GetAsync<T>` 要求 `class` 约束，因此 `int` 计数需包装为 `string` 或自定义包装类：

```csharp
// 推荐：用 string 存储 int，避免装箱
await _cacheService.SetAsync<string>(cacheKey, count.ToString(), TimeSpan.FromMinutes(10));
var cached = await _cacheService.GetAsync<string>(cacheKey);
return cached != null ? int.Parse(cached) : (int?)null;
```

---

## Error Handling

### 后端错误处理

| 场景                           | 处理方式                                                        |
| ------------------------------ | --------------------------------------------------------------- |
| 非 System Admin 调用管理端 API | `WFEAuthorize` 返回 HTTP 403                                    |
| 创建/编辑请求字段校验失败      | FluentValidation 返回 HTTP 400，含字段级错误信息                |
| `MarkReadAsync` 幂等冲突       | `INSERT ON CONFLICT DO NOTHING` 静默处理，不抛异常              |
| `GetDetailAsync` 找不到记录    | 抛 `CRMException(ErrorCodeEnum.NotFound, "WhatsNew not found")` |
| `DeleteAsync` 找不到记录       | 抛 `CRMException(ErrorCodeEnum.NotFound, "WhatsNew not found")` |
| Redis 不可用                   | `IDistributedCacheService` 内部 try-catch，降级为直接查 DB      |

### 前端错误处理

| 场景                      | 处理方式                                                   |
| ------------------------- | ---------------------------------------------------------- |
| `markRead` 接口报错       | 静默处理（`try-catch` 不 `ElMessage.error`），弹窗正常显示 |
| `markAllRead` 接口报错    | `ElMessage.error`，不更新本地状态                          |
| `deleteWhatsNew` 接口报错 | `ElMessage.error`，列表状态保持不变                        |
| 创建/编辑提交报错         | `ElMessage.error(errorMessage)`                            |
| 面板加载失败              | 控制台 warn，面板显示空状态（"No updates yet"）            |

---

## XSS Security

### 后端 HTML 白名单过滤

**实现位置**：`WhatsNewService.CreateAsync` 和 `UpdateAsync` 方法中，在 `_repo.InsertAsync` / `_repo.UpdateAsync` 之前。

**推荐使用 `Ganss.Xss.HtmlSanitizer` NuGet 包（项目通用方案）：**

```csharp
// 若项目已有 HtmlSanitizer，直接复用
private string SanitizeHtml(string html)
{
    if (string.IsNullOrEmpty(html)) return html;
    var sanitizer = new HtmlSanitizer();
    // 白名单配置：保留 Quill 富文本常用标签和属性
    sanitizer.AllowedTags.UnionWith(new[]
    {
        "p", "br", "strong", "em", "u", "s", "h1", "h2", "h3", "h4", "h5", "h6",
        "ul", "ol", "li", "blockquote", "pre", "code", "a", "img", "span",
        "div", "table", "thead", "tbody", "tr", "th", "td"
    });
    sanitizer.AllowedAttributes.UnionWith(new[]
    {
        "href", "src", "alt", "class", "style", "target", "rel",
        "data-row", "data-cell"  // Quill table 插件属性
    });
    // 禁止危险协议
    sanitizer.AllowedSchemes.Clear();
    sanitizer.AllowedSchemes.UnionWith(new[] { "http", "https", "data" });
    return sanitizer.Sanitize(html);
}
```

> **注意**：若项目尚未安装 `HtmlSanitizer`，需在 `flowFlex-backend` 中 `dotnet add package HtmlSanitizer`。如果项目有其他 HTML 过滤工具，可替换实现，核心原则不变：移除 `<script>`、`on*` 事件属性、`javascript:` 协议。

### 前端 DOMPurify 过滤

**实现位置**：`WhatsNewDetail.vue` 的模板中，凡是使用 `v-html` 绑定富文本的地方。

```vue
<template>
  <!-- 正确做法：先过滤，后绑定 -->
  <div v-html="sanitizedContent" class="whats-new-content" />
</template>

<script setup lang="ts">
import DOMPurify from "dompurify";

const sanitizedContent = computed(() => {
  if (!detail.value?.content) return "";
  return DOMPurify.sanitize(detail.value.content, {
    ALLOWED_TAGS: [
      "p",
      "br",
      "strong",
      "em",
      "u",
      "s",
      "h1",
      "h2",
      "h3",
      "h4",
      "h5",
      "h6",
      "ul",
      "ol",
      "li",
      "blockquote",
      "pre",
      "code",
      "a",
      "img",
      "span",
      "div",
      "table",
      "thead",
      "tbody",
      "tr",
      "th",
      "td",
    ],
    ALLOWED_ATTR: ["href", "src", "alt", "class", "style", "target", "rel"],
    FORCE_BODY: true,
  });
});
</script>
```

> `dompurify` 已在项目中安装（requirements 中明确），可直接 `import DOMPurify from 'dompurify'`。

---

## Testing Strategy

本功能涉及的主要逻辑为：**数据库 CRUD + Redis 缓存读写 + XSS 过滤 + 权限校验**。这些属于具体场景和集成行为，不具备通用属性可跨输入空间验证，PBT 不适用。

> **PBT 不适用原因**：
>
> - Service 方法依赖 Repository（数据库）和 Redis，属于 I/O 密集型，不是纯函数
> - 权限校验行为固定（只有两种 userType），不需要 100 次随机迭代
> - 已读幂等性由 DB 唯一约束保证，不是应用层逻辑
>
> 应使用 **Mock-based 单元测试** + **集成测试** 代替 PBT。

### 单元测试策略（xUnit + Moq + FluentAssertions）

**测试文件**：`Tests/FlowFlex.Tests/OW/WhatsNewServiceTests.cs`

覆盖以下场景：

| 测试方法名                                         | 测试场景                                  |
| -------------------------------------------------- | ----------------------------------------- |
| `GetUnreadCountAsync_CacheHit_ReturnsCachedValue`  | Redis 命中时不查 DB                       |
| `GetUnreadCountAsync_CacheMiss_QueriesDbAndCaches` | Redis 未命中时查 DB 并写缓存              |
| `MarkReadAsync_Success_InvalidatesCache`           | 标记已读后 RemoveAsync 被调用             |
| `MarkAllReadAsync_Success_InvalidatesCache`        | 全部已读后 RemoveAsync 被调用             |
| `CreateAsync_WithPublishNow_SetsPublishTime`       | status=1 时 PublishTime 不为 null         |
| `CreateAsync_WithDraft_NoPublishTime`              | status=0 时 PublishTime 为 null           |
| `UpdateAsync_DraftToPublished_SetsPublishTime`     | 从 Draft 变为 Published 时写 publish_time |
| `DeleteAsync_SetsIsValidFalse`                     | 软删除不物理删除记录                      |
| `CreateAsync_SanitizesHtmlContent`                 | XSS 内容被过滤（script 标签被移除）       |

**Mock 设置示例：**

```csharp
// Arrange
var mockRepo = new Mock<IWhatsNewRepository>();
var mockReadRepo = new Mock<IWhatsNewReadStatusRepository>();
var mockCache = new Mock<IDistributedCacheService>();
var userContext = new UserContext { UserId = "123", AppCode = "WFE", TenantId = "001" };

var service = new WhatsNewService(
    mockRepo.Object,
    mockReadRepo.Object,
    mockCache.Object,
    userContext,
    Mock.Of<ILogger<WhatsNewService>>());
```

### 前端测试策略（Jest + @vue/test-utils）

**测试文件**：`src/app/components/navbarCompanents/__tests__/WhatsNewBell.spec.ts`

覆盖以下场景：

| 测试场景                         | 验证点                            |
| -------------------------------- | --------------------------------- |
| unreadCount > 0 时渲染红点       | el-badge 或自定义红点可见         |
| unreadCount = 0 时不渲染红点     | 红点 DOM 不存在                   |
| 点击铃铛展开面板                 | panel visible                     |
| markRead 成功后 unreadCount 减 1 | count 变化正确                    |
| markAllRead 后 unreadCount 为 0  | count = 0，所有条目 isRead = true |
| DOMPurify 过滤 script 标签       | `<script>` 不出现在渲染 HTML 中   |

---

## Diagrams

### 已读状态流程图

```mermaid
sequenceDiagram
    participant U as User
    participant Bell as WhatsNewBell
    participant Panel as WhatsNewPanel
    participant Detail as WhatsNewDetail
    participant API as Backend API
    participant Cache as Redis

    U->>Bell: 页面加载
    Bell->>API: GET /unread-count
    API->>Cache: GET whats-new:unread:{key}
    alt Cache Hit
        Cache-->>API: count
    else Cache Miss
        API->>API: 查 DB 计算未读数
        API->>Cache: SET (TTL 10min)
    end
    API-->>Bell: unreadCount
    Bell->>Bell: unreadCount > 0 → 显示红点

    U->>Bell: 点击铃铛
    Bell->>API: GET /panel
    API-->>Panel: items (含 isRead)
    Panel->>Panel: 渲染列表（不触发已读）

    U->>Panel: 点击某条目
    Panel->>Detail: open(item)
    Detail->>API: GET /{id} (加载详情)
    Detail->>API: POST /{id}/read
    API->>Cache: DEL whats-new:unread:{key}
    Detail->>Bell: markItemAsRead(id) + decrementUnread()
    Bell->>Bell: unreadCount-- ; item.isRead = true
```

### Category Tag 颜色映射

```
NewFeature   → el-tag type="primary"  (蓝色  #409EFF)
Improvement  → el-tag type="warning"  (橙色  #E6A23C)
BugFix       → el-tag type="danger"   (红色  #F56C6C)
Announcement → el-tag type=""（自定义紫色 #722ED1 via CSS）
```

### 管理端 Status Tag 颜色映射

```
Draft     → el-tag type="info"     (灰色  #909399)
Published → el-tag type="success"  (绿色  #67C23A)
```
