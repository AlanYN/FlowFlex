# Implementation Plan: OW-720 What's New

## Overview

按依赖顺序从数据库 Migration 到前端组件逐层实现 What's New 功能。后端遵循 Domain → Repository → Application.Contracts → Application → WebApi 的标准分层顺序，前端遵循 Types → API → 用户端组件 → 管理端的顺序。测试任务作为各层的子任务内联。

---

## Tasks

- [x] 1. 数据库 Migration
  - 新建 Migration 文件，创建 `ff_whats_new` 和 `ff_whats_new_read_status` 两张表，含所有索引和唯一约束
  - 在 `MigrationManager.cs` 的 migrations 数组末尾注册本次 Migration
  - 使用 `IF NOT EXISTS` 保证幂等性
  - **Files (new):** `SqlSugarDB/Migrations/Migration_20260828001_AddWhatsNew.cs`
  - **Files (modify):** `SqlSugarDB/Migrations/MigrationManager.cs`
  - **Implements:** Requirement 11

  - [x] 1.1 创建 `ff_whats_new` 表 DDL
    - `id` BIGINT PK、`app_code`、`tenant_id`、`title` varchar(100)、`summary` varchar(200)、`content` text、`category` varchar(50)、`status` int DEFAULT 0、`publish_time` timestamptz、`scheduled_time` timestamptz、标准审计字段、`is_valid` bool
    - 创建联合索引 `idx_ff_whats_new_status_publish_time (status, publish_time DESC) WHERE is_valid = TRUE`
    - 创建索引 `idx_ff_whats_new_app_tenant (app_code, tenant_id) WHERE is_valid = TRUE`
    - _Requirements: 11.1_

  - [x] 1.2 创建 `ff_whats_new_read_status` 表 DDL
    - `id` BIGINT PK、`whats_new_id` BIGINT、`user_id` BIGINT、`read_time` timestamptz、`app_code` varchar(50)、`tenant_id` varchar(50)
    - 创建唯一约束 `uidx_ff_whats_new_read_status_unique (whats_new_id, user_id, app_code, tenant_id)`
    - 创建索引 `idx_ff_whats_new_read_status_user (user_id, app_code, tenant_id)`
    - 创建索引 `idx_ff_whats_new_read_status_whats_new (whats_new_id)`
    - _Requirements: 11.2, 11.3_

  - [x] 1.3 在 MigrationManager 注册
    - 在 migrations 数组末尾追加 `("20260828001_AddWhatsNew", (Action)(() => Migration_20260828001_AddWhatsNew.Up(_db)))`
    - _Requirements: 11.4_

---

- [x] 2. Domain 层 — Entity + Repository 接口
  - 新建两个 Entity 类和两个 Repository 接口；Entity 字段需与 Migration DDL 对齐
  - **Files (new):**
    - `Domain/Entities/OW/WhatsNew.cs`
    - `Domain/Entities/OW/WhatsNewReadStatus.cs`
    - `Domain/Repository/OW/IWhatsNewRepository.cs`
    - `Domain/Repository/OW/IWhatsNewReadStatusRepository.cs`
  - **Depends on:** Task 1
  - **Implements:** Requirement 9, 10, 11

  - [x] 2.1 WhatsNew Entity
    - 继承 `EntityBaseCreateInfo`，标注 `[SugarTable("ff_whats_new")]`
    - 字段：`Title`、`Summary`、`Content`（text）、`Category`、`Status`（默认 0）、`PublishTime`（nullable）、`ScheduledTime`（nullable）
    - _Requirements: 11.1_

  - [x] 2.2 WhatsNewReadStatus Entity
    - 继承 `IdEntityBase`（或 `EntityBase`），标注 `[SugarTable("ff_whats_new_read_status")]`
    - 字段：`WhatsNewId`、`UserId`、`ReadTime`、`AppCode`、`TenantId`
    - _Requirements: 11.2_

  - [x] 2.3 IWhatsNewRepository 接口
    - 继承 `IBaseRepository<WhatsNew>`
    - 声明：`GetPublishedListAsync(int limit = 10)`、`GetAdminListAsync(int? statusFilter = null)`、`GetStatusCountsAsync()`
    - 定义 `WhatsNewAdminItemProjection` 投影类（含 ReadCount）
    - _Requirements: 9.2, 10.2_

  - [x] 2.4 IWhatsNewReadStatusRepository 接口
    - 继承 `IBaseRepository<WhatsNewReadStatus>`
    - 声明：`MarkReadAsync`、`MarkAllReadAsync`、`GetReadIdsAsync`、`GetReadCountAsync`、`GetUnreadCountAsync`
    - _Requirements: 9.1, 9.4, 9.5_

---

- [x] 3. SqlSugarDB 层 — Repository 实现
  - 实现两个 Repository，重点关注幂等 INSERT 和 JOIN 查询
  - **Files (new):**
    - `SqlSugarDB/Repositories/OW/WhatsNewRepository.cs`
    - `SqlSugarDB/Repositories/OW/WhatsNewReadStatusRepository.cs`
  - **Depends on:** Task 2
  - **Implements:** Requirement 9, 10, 11

  - [x] 3.1 WhatsNewRepository 实现
    - 继承 `BaseRepository<WhatsNew>`，实现接口全部方法
    - `GetPublishedListAsync`：过滤 `status = 1 AND is_valid = true`，按 `publish_time DESC` 取 limit 条
    - `GetAdminListAsync`：LEFT JOIN `ff_whats_new_read_status` 按 `whats_new_id` 聚合 COUNT，支持 `statusFilter` 条件
    - `GetStatusCountsAsync`：GROUP BY status 返回 published / draft 计数
    - _Requirements: 9.2, 10.2_

  - [x] 3.2 WhatsNewReadStatusRepository 实现
    - 继承 `BaseRepository<WhatsNewReadStatus>`，实现接口全部方法
    - `MarkReadAsync`：使用 `db.Ado.ExecuteCommandAsync` 执行 `INSERT ... ON CONFLICT (whats_new_id, user_id, app_code, tenant_id) DO NOTHING`
    - `MarkAllReadAsync`：批量构建 Insert 记录列表，使用 `InsertRangeAsync` + CONFLICT DO NOTHING
    - `GetReadIdsAsync`：查询 user 已读 id 集合（HashSet<long>）
    - `GetUnreadCountAsync`：基于 publishedIds 集合过滤，统计未在 read_status 中的数量
    - _Requirements: 9.4, 9.5_

---

- [x] 4. Application.Contracts 层 — DTO + Service 接口
  - 新建所有 DTO、Request/Response 类型，以及 `IWhatsNewService` 接口
  - **Files (new):**
    - `Application.Contracts/Dtos/OW/WhatsNew/WhatsNewPanelItemDto.cs`
    - `Application.Contracts/Dtos/OW/WhatsNew/WhatsNewPanelResponseDto.cs`
    - `Application.Contracts/Dtos/OW/WhatsNew/WhatsNewDetailDto.cs`
    - `Application.Contracts/Dtos/OW/WhatsNew/WhatsNewAdminItemDto.cs`
    - `Application.Contracts/Dtos/OW/WhatsNew/WhatsNewAdminListResponseDto.cs`
    - `Application.Contracts/Dtos/OW/WhatsNew/CreateWhatsNewRequest.cs`
    - `Application.Contracts/Dtos/OW/WhatsNew/UpdateWhatsNewRequest.cs`
    - `Application.Contracts/IServices/OW/IWhatsNewService.cs`
  - **Depends on:** Task 2
  - **Implements:** Requirement 9, 10

  - [x] 4.1 DTO 类
    - `WhatsNewPanelItemDto`：Id（LongToStringConverter）、Title、Summary、Category、PublishTime、IsRead
    - `WhatsNewPanelResponseDto`：Items + UnreadCount
    - `WhatsNewDetailDto`：继承 PanelItem 字段 + Content（XSS 过滤后 HTML）
    - `WhatsNewAdminItemDto`：Id、Title、Summary、Category、Status、PublishTime、ReadCount
    - `WhatsNewAdminListResponseDto`：Items + PublishedCount + DraftCount
    - _Requirements: 9.2, 9.3, 10.2_

  - [x] 4.2 Request 类 + FluentValidation
    - `CreateWhatsNewRequest`：Title（max 100）、Summary（max 200）、Content、Category、Status（默认 0）
    - `UpdateWhatsNewRequest`：同上字段
    - 两个 Request 类均使用 `[Required]` / `[MaxLength]` 标注，或配套 FluentValidation Validator
    - _Requirements: 10.6_

  - [x] 4.3 IWhatsNewService 接口
    - 继承 `IScopedService`
    - 声明用户端 5 个方法：`GetUnreadCountAsync`、`GetPanelAsync`、`GetDetailAsync`、`MarkReadAsync`、`MarkAllReadAsync`
    - 声明管理端 4 个方法：`GetAdminListAsync`、`CreateAsync`、`UpdateAsync`、`DeleteAsync`
    - _Requirements: 9, 10_

---

- [x] 5. Application 层 — Service 实现 + AutoMapper Profile
  - 实现 `WhatsNewService`（含 Redis 缓存、XSS 过滤、publish_time 赋值逻辑）并注册 AutoMapper Profile
  - **Files (new):**
    - `Application/Services/OW/WhatsNewService.cs`
    - `Application/Maps/WhatsNewMapProfile.cs`
  - **Depends on:** Task 3, 4
  - **Implements:** Requirement 9, 10, 13

  - [x] 5.1 WhatsNewService 骨架 + 构造函数注入
    - 注入：`IWhatsNewRepository`、`IWhatsNewReadStatusRepository`、`IDistributedCacheService`、`UserContext`、`ILogger<WhatsNewService>`
    - _Requirements: 9_

  - [x] 5.2 Redis 缓存逻辑（GetUnreadCountAsync）
    - Key 格式：`whats-new:unread:{appCode}:{tenantId}:{userId}`
    - 先 `GetAsync<string>(key)`；Miss 时查 DB 并 `SetAsync(key, count.ToString(), 10min)`
    - _Requirements: 9.1_

  - [x] 5.3 GetPanelAsync
    - 查 status=1 最新 10 条；`GetReadIdsAsync` 获取已读集合；注入 `isRead`；返回 `UnreadCount`
    - _Requirements: 9.2_

  - [x] 5.4 MarkReadAsync + MarkAllReadAsync（缓存失效）
    - `MarkReadAsync`：调 Repository 幂等 INSERT → `RemoveAsync(cacheKey)`
    - `MarkAllReadAsync`：查所有 Published id → 批量 INSERT → `RemoveAsync(cacheKey)`
    - _Requirements: 9.4, 9.5_

  - [x] 5.5 XSS 过滤辅助方法 `SanitizeHtml`
    - 使用 `HtmlSanitizer`（或项目已有工具）配置白名单标签和属性
    - 禁止 `javascript:` 协议、`<script>`、`on*` 属性
    - _Requirements: 13.1_

  - [x] 5.6 CreateAsync + UpdateAsync（含 publish_time 赋值）
    - `CreateAsync`：调 `SanitizeHtml`；`status == 1` 时设 `PublishTime = DateTimeOffset.UtcNow`
    - `UpdateAsync`：加载现有记录；XSS 过滤；Draft → Published 时设 `PublishTime = now`
    - _Requirements: 10.3, 10.4_

  - [x] 5.7 GetAdminListAsync + DeleteAsync
    - `GetAdminListAsync`：调 Repository projection 查询；映射 DTO；附加统计
    - `DeleteAsync`：软删除 `IsValid = false`；不删 read_status
    - _Requirements: 10.2, 10.5_

  - [x] 5.8 AutoMapper Profile
    - `WhatsNewMapProfile`：注册 WhatsNew ↔ DTO、Request → WhatsNew 的映射
    - 确认在 `Program.cs` 或程序集扫描范围内自动加载
    - _Requirements: 9, 10_

  - [x] 5.9 后端单元测试 — WhatsNewServiceTests
    - 测试文件：`Tests/FlowFlex.Tests/OW/WhatsNewServiceTests.cs`
    - 覆盖 9 个用例（见 design.md 测试策略表）：
      - `GetUnreadCountAsync_CacheHit_ReturnsCachedValue`
      - `GetUnreadCountAsync_CacheMiss_QueriesDbAndCaches`
      - `MarkReadAsync_Success_InvalidatesCache`
      - `MarkAllReadAsync_Success_InvalidatesCache`
      - `CreateAsync_WithPublishNow_SetsPublishTime`
      - `CreateAsync_WithDraft_NoPublishTime`
      - `UpdateAsync_DraftToPublished_SetsPublishTime`
      - `DeleteAsync_SetsIsValidFalse`
      - `CreateAsync_SanitizesHtmlContent`
    - _Requirements: 9, 10, 13_

---

- [x] 6. WebApi 层 — WhatsNewController
  - 新建 Controller，路由前缀 `ow/whats-new/v{version:apiVersion}`，用户端继承类级 `[Authorize]`，管理端额外加 `[WFEAuthorize]`
  - **Files (new):** `WebApi/Controllers/OW/WhatsNewController.cs`
  - **Depends on:** Task 5
  - **Implements:** Requirement 9, 10, 12

  - [x] 6.1 用户端 5 个接口
    - `GET unread-count` → `GetUnreadCountAsync()`
    - `GET panel` → `GetPanelAsync()`
    - `GET {id:long}` → `GetDetailAsync(id)`
    - `POST {id:long}/read` → `MarkReadAsync(id)`
    - `POST read-all` → `MarkAllReadAsync()`
    - 所有接口返回 `Success<T>(data)` 包装
    - _Requirements: 9.1–9.5_

  - [x] 6.2 管理端 4 个接口（[WFEAuthorize]）
    - `GET admin` → `GetAdminListAsync([FromQuery] int? status)`
    - `POST admin` → `CreateAsync([FromBody] CreateWhatsNewRequest)`
    - `PUT admin/{id:long}` → `UpdateAsync(id, [FromBody] UpdateWhatsNewRequest)`
    - `DELETE admin/{id:long}` → `DeleteAsync(id)`
    - _Requirements: 10.1–10.5_

---

- [x] 7. 前端 TypeScript 类型 + API 模块
  - 定义所有前端类型和 API 函数，供后续组件使用
  - **Files (new):**
    - `src/app/types/whatsNew.d.ts`（或 `types/whatsNew.d.ts`）
    - `src/app/apis/whatsNew/index.ts`
  - **Depends on:** Task 6
  - **Implements:** Requirement 1, 2, 3, 4, 6, 7, 8

  - [x] 7.1 TypeScript 类型定义
    - `WhatsNewPanelItem`：id(string)、title、summary、category、publishTime、isRead
    - `WhatsNewPanelResponse`：items + unreadCount
    - `WhatsNewDetail`：继承 PanelItem + content(string)
    - `WhatsNewAdminItem`：id、title、summary、category、status(0|1)、publishTime、readCount
    - `WhatsNewAdminListResponse`：items + publishedCount + draftCount
    - `CreateWhatsNewRequest`、`UpdateWhatsNewRequest`
    - _Requirements: 9, 10_

  - [x] 7.2 API 函数实现
    - 使用 `defHttp` + `useGlobSetting()` 构建 Api 对象（统一管理 URL）
    - 用户端：`getUnreadCount`、`getPanel`、`getDetail`、`markRead`、`markAllRead`
    - 管理端：`getAdminList`、`createWhatsNew`、`updateWhatsNew`、`deleteWhatsNew`
    - _Requirements: 9, 10_

---

- [x] 8. WhatsNewBell 组件 + navbar 集成
  - 实现铃铛图标、红点、el-popover 面板及 provide/inject 状态管理；修改 navbar.vue 插入组件
  - **Files (new):**
    - `src/app/components/navbarCompanents/WhatsNewBell.vue`
  - **Files (modify):**
    - `src/app/components/layout/components/navbar.vue`
  - **Depends on:** Task 7
  - **Implements:** Requirement 1, 2, 4

  - [x] 8.1 铃铛图标 + 红点
    - `onMounted` 调用 `getUnreadCount()`，写入 `unreadCount` ref
    - `WHILE unreadCount > 0` → 渲染红色圆点（无数字），使用 `el-badge` 或自定义 CSS
    - `WHILE unreadCount === 0` → 不渲染任何徽章
    - 不做轮询，仅 mount 时请求一次
    - _Requirements: 1.2, 1.3, 1.4, 1.5_

  - [x] 8.2 el-popover 面板（WhatsNewPanel 内联）
    - 点击铃铛展开 `el-popover`（宽 360px，右对齐）
    - 展开时调用 `getPanel()`，渲染最多 10 条，按 `publish_time` 倒序
    - 每条显示：Category Tag（按颜色映射）、标题（粗体）、摘要（2 行截断）、相对时间（dayjs）
    - `isRead=false` 条目左侧显示蓝色圆点；`isRead=true` 不显示
    - 面板展开时不触发任何已读标记
    - 空状态显示 "No updates yet"
    - _Requirements: 2.1–2.6, 2.9, 2.11_

  - [x] 8.3 "Mark all as read" 逻辑
    - `WHILE unreadCount > 0` 显示 "Mark all as read" 链接；`unreadCount = 0` 时隐藏
    - 点击 → 调用 `markAllRead()` → 成功后：所有条目 `isRead = true`、`unreadCount = 0`、移除红点
    - 失败 → `ElMessage.error`，不更新本地状态
    - _Requirements: 2.7, 2.8, 4.1–4.4_

  - [x] 8.4 provide/inject 状态契约
    - `provide('whatsNewState', { unreadCount, panelItems, decrementUnread, clearAllUnread, markItemAsRead })`
    - 保证 `decrementUnread` 不低于 0（`Math.max(0, ...)`）
    - _Requirements: 1.6, 3.5, 3.6_

  - [x] 8.5 修改 navbar.vue 插入 WhatsNewBell
    - 在 `<Setting />` 之后、`<UserLayout />` 之前插入 `<WhatsNewBell />`
    - _Requirements: 1.1_

---

- [x] 9. WhatsNewDetail 组件
  - 详情弹窗，含 DOMPurify 渲染 + 已读回调
  - **Files (new):** `src/app/components/navbarCompanents/WhatsNewDetail.vue`
  - **Depends on:** Task 7, 8
  - **Implements:** Requirement 3

  - [x] 9.1 el-dialog 布局
    - 居中全屏遮罩 `el-dialog`
    - 显示：大字体加粗标题、Category Tag + 发布日期、富文本正文区域、右上角关闭按钮
    - _Requirements: 3.1, 3.2_

  - [x] 9.2 DOMPurify 富文本渲染
    - `computed sanitizedContent`：`DOMPurify.sanitize(detail.content, { ALLOWED_TAGS: [...], ALLOWED_ATTR: [...] })`
    - 模板使用 `v-html="sanitizedContent"`，任何情况不跳过 sanitize
    - _Requirements: 3.3_

  - [x] 9.3 自动标记已读
    - `open` 事件触发 → 调用 `getDetail(id)` 加载详情 → 调用 `markRead(id)`（静默失败）
    - 成功后 `inject('whatsNewState')` → `markItemAsRead(id)` + `decrementUnread()`（仅当原 `isRead=false`）
    - _Requirements: 3.4, 3.5, 3.6, 3.7_

---

- [x] 10. 管理端路由 + 权限守卫 + userLayout 菜单项
  - 新建路由模块，添加 System Admin 权限守卫，修改 userLayout 菜单
  - **Files (new):** `src/app/router/routers/modules/whatsNewManagement.ts`
  - **Files (modify):**
    - `src/app/router/routers/index.ts`（或路由汇总文件）
    - `src/app/components/navbarCompanents/userLayout.vue`
  - **Depends on:** Task 7
  - **Implements:** Requirement 5, 6, 12

  - [x] 10.1 路由模块
    - 路径 `/whats-new-management`，`hidden: true`（不出现在侧边栏）
    - 子路由 `index` 懒加载 `@/views/whatsNewManagement/index.vue`
    - 导入并注册至路由汇总文件
    - _Requirements: 6.1_

  - [x] 10.2 权限守卫（index.vue `onMounted`）
    - `useUserStore().getUserInfo?.userType !== 1` → `router.replace('/')`
    - _Requirements: 6.2, 12.1_

  - [x] 10.3 userLayout 菜单项
    - 在"My Profile"之后添加"Manage What's New"菜单项
    - `v-if="userStore.getUserInfo?.userType === 1"`（非 Admin 不渲染）
    - 点击 → `router.push('/whats-new-management')`
    - _Requirements: 5.1, 5.2, 5.3_

---

- [x] 11. 管理列表页
  - 实现 `whatsNewManagement/index.vue`，含统计卡片、条目列表、删除确认弹窗
  - **Files (new):**
    - `src/app/views/whatsNewManagement/index.vue`
  - **Depends on:** Task 7, 10
  - **Implements:** Requirement 6, 8

  - [ ] 11.1 统计卡片 + 列表数据加载 + 卡片筛选
    - `onMounted` 调用 `getAdminList()`，从响应直接取 `publishedCount` / `draftCount`
    - 渲染两个统计卡片：Published 数量、Drafts 数量
    - 卡片同时作为筛选按钮：本地维护 `activeFilter ref`
      - 点击 Published 卡片 → `getAdminList(1)`，卡片高亮
      - 点击 Drafts 卡片 → `getAdminList(0)`，卡片高亮
      - 再次点击已激活卡片 → `getAdminList()`（无参数），取消筛选，恢复默认样式
    - _Requirements: 6.3, 6.6, 6.7, 6.8_

  - [ ] 11.2 条目列表渲染
    - 每行展示：Category Tag（颜色映射）、Status Tag（Draft=灰/Published=绿）、标题（粗体）、摘要（1-2 行）、发布时间（或"Not published"）、编辑图标、删除图标
    - _Requirements: 6.4, 6.5_

  - [ ] 11.3 删除确认弹窗
    - 点击删除图标 → 读本地列表中对应条目的 `readCount`（无额外请求）
    - 弹窗文案："This update has been viewed by {readCount} users. Deleting it will remove it from all users' What's New panel."
    - 确认 → `deleteWhatsNew(id)` → 成功后从本地列表移除条目并更新统计卡片
    - 失败 → `ElMessage.error`，列表保持不变
    - 取消 → 关闭弹窗，不发请求
    - _Requirements: 8.1–8.6_

  - [ ] 11.4 触发 WhatsNewFormModal
    - "＋ New update" 按钮 → 以 `mode='create'` 打开 Modal
    - 编辑图标 → 以 `mode='edit'` + item 数据打开 Modal
    - Modal `success` 事件 → 调用 `loadList()` 刷新列表
    - _Requirements: 7.1, 7.2, 7.10_

---

- [x] 12. 创建/编辑弹窗（WhatsNewFormModal）
  - 实现表单弹窗，含字段校验、RichTextEditor、Publishing 选项（含 Schedule disabled 状态）
  - **Files (new):**
    - `src/app/views/whatsNewManagement/components/WhatsNewFormModal.vue`
  - **Depends on:** Task 7, 11
  - **Implements:** Requirement 7

  - [x] 12.1 表单字段
    - 必填字段：Title（max 100）、Category（下拉，4 值枚举）、Summary（max 200）、Content（RichTextEditor，默认完整工具栏）
    - ElForm 校验规则，提交时有空字段 → 阻止提交并显示校验提示
    - create 模式所有字段清空；edit 模式预填充现有数据
    - _Requirements: 7.3, 7.7_

  - [x] 12.2 Publishing 选项组
    - 三个互斥选项：Publish Now / Schedule（disabled） / Save as draft
    - Schedule 显示灰色禁用样式，hover 显示 tooltip "Coming soon"
    - 根据选择切换底部按钮文字："Publish update" / "Save as draft"
    - _Requirements: 7.4, 7.5, 7.6_

  - [x] 12.3 提交逻辑
    - Publish Now → `status = 1`；Save as draft → `status = 0`
    - create 模式 → `createWhatsNew(form)`；edit 模式 → `updateWhatsNew(id, form)`
    - 成功 → 关闭 Modal，emit `'success'`
    - 失败 → `ElMessage.error`
    - 点击 Cancel → 关闭 Modal，不保存
    - _Requirements: 7.8, 7.9, 7.10, 7.11_

---

- [ ] 13. Checkpoint — 端到端验收
  - 确认所有单元测试通过（`dotnet test` + `pnpm test`）
  - 确认路由守卫：非 Admin 访问 `/whats-new-management` 被重定向
  - 确认 XSS：提交含 `<script>alert(1)</script>` 的 content，数据库中该标签被过滤
  - 确认幂等性：重复调用 `POST /{id}/read` 不报错、不重复插入
  - 如有问题，返回对应任务修复后再继续

---

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- 后端无需 PBT（属性测试），Service 层测试均为 Mock-based 单元测试（见 design.md 测试策略）
- `WhatsNewReadStatus` 不走多租户全局过滤，Repository 实现中所有查询须手动加 `app_code + tenant_id` 条件
- `IDistributedCacheService.GetAsync<T>` 要求 class 约束，int 计数以 string 存储，避免装箱
- `HtmlSanitizer` 如项目未安装，Task 5.5 实施前需先 `dotnet add package HtmlSanitizer`
- `dompurify` 已在前端项目中安装，可直接 `import DOMPurify from 'dompurify'`
- 管理端所有接口的权限依赖 `WFEAuthorize` → `IsSystemAdmin` bypass，无需额外 PermissionConsts
- 已发布更新（status=1）再次被编辑时，`publish_time` 不变，`ff_whats_new_read_status` 中的已读记录不受影响，用户不会收到重复的未读提示
- 管理列表筛选通过点击统计卡片实现（方案 A），`activeFilter` 为本地 ref，切换时重新调用 `getAdminList(status?)` 接口

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2"] },
    { "id": 1, "tasks": ["1.3"] },
    { "id": 2, "tasks": ["2.1", "2.2"] },
    { "id": 3, "tasks": ["2.3", "2.4"] },
    { "id": 4, "tasks": ["3.1", "3.2"] },
    { "id": 5, "tasks": ["4.1", "4.2", "4.3"] },
    { "id": 6, "tasks": ["5.1"] },
    { "id": 7, "tasks": ["5.2", "5.3", "5.4", "5.5"] },
    { "id": 8, "tasks": ["5.6", "5.7"] },
    { "id": 9, "tasks": ["5.8", "5.9"] },
    { "id": 10, "tasks": ["6.1", "6.2"] },
    { "id": 11, "tasks": ["7.1", "7.2"] },
    { "id": 12, "tasks": ["8.1", "8.2", "8.3"] },
    { "id": 13, "tasks": ["8.4", "8.5"] },
    { "id": 14, "tasks": ["9.1", "9.2"] },
    { "id": 15, "tasks": ["9.3"] },
    { "id": 16, "tasks": ["10.1", "10.3"] },
    { "id": 17, "tasks": ["10.2"] },
    { "id": 18, "tasks": ["11.1", "11.2"] },
    { "id": 19, "tasks": ["11.3", "11.4"] },
    { "id": 20, "tasks": ["12.1"] },
    { "id": 21, "tasks": ["12.2"] },
    { "id": 22, "tasks": ["12.3"] }
  ]
}
```
