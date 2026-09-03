# Requirements Document

## Introduction

OW-720 What's New 功能为 FlowFlex WFE 系统增加一套轻量级产品更新通知机制。用户可通过页面右上角铃铛图标感知最新功能变更，System Admin 可在专属管理页面维护更新内容。本文档覆盖 Phase 1（MVP）范围。

---

## Glossary

- **WhatsNew**：单条产品更新记录，包含标题、摘要、正文、分类和状态。
- **WhatsNewPanel**：点击铃铛图标展开的下拉面板，最多展示 10 条已发布更新。
- **WhatsNewBell**：顶部导航栏铃铛图标组件，承载未读红点和面板入口。
- **Category**：更新分类枚举，共 4 个值：`NewFeature` / `Improvement` / `BugFix` / `Announcement`。
- **Status**：更新状态，Phase 1 有两个值：`Draft`（0）/ `Published`（1）。
- **UnreadCount**：当前用户未读已发布更新的数量。
- **ReadStatus**：每条更新针对每位用户的已读/未读记录。
- **System_Admin**：`userType === 1` 的用户，拥有内容管理权限。
- **RichTextEditor**：项目现有富文本编辑器组件（基于 @vueup/vue-quill）。
- **DOMPurify**：前端 HTML XSS 过滤库。
- **Snowflake_ID**：系统使用的 64 位雪花 ID，前端以字符串传输。

---

## Requirements

### Requirement 1：通知图标与未读红点

**User Story：** 作为登录用户，我希望在页面右上角看到铃铛图标，当有未读更新时图标上显示红点，这样我能第一时间感知到有新内容。

#### Acceptance Criteria

1. THE WhatsNewBell SHALL 渲染于 `navbar.vue` 右侧区域，位于 Setting 组件之后、UserLayout 组件之前。
2. WHEN 页面加载完成，THE WhatsNewBell SHALL 调用 `GET ow/whats-new/v1/unread-count` 接口获取当前用户未读数。
3. WHILE UnreadCount 大于 0，THE WhatsNewBell SHALL 在铃铛图标右上角显示红色小圆点（无数字）。
4. WHILE UnreadCount 等于 0，THE WhatsNewBell SHALL 不渲染任何徽章或数字。
5. THE WhatsNewBell SHALL 在页面加载后仅请求一次 unread-count，不做轮询。
6. WHEN 用户标记已读后，THE WhatsNewBell SHALL 在前端本地将 UnreadCount 减去已读数量，不重新请求接口。

---

### Requirement 2：What's New 面板

**User Story：** 作为登录用户，我希望点击铃铛图标后展开一个面板，查看最新的产品更新列表。

#### Acceptance Criteria

1. WHEN 用户点击 WhatsNewBell 图标，THE WhatsNewPanel SHALL 以 `el-popover` 形式展开，右对齐，宽度约 360px。
2. WHEN 面板展开时，THE WhatsNewPanel SHALL 调用 `GET ow/whats-new/v1/panel` 加载最多 10 条 Published 状态的更新，按 `publish_time` 倒序排列。
3. WHEN 面板展开时，THE WhatsNewPanel SHALL 不触发任何条目的已读标记。
4. THE WhatsNewPanel SHALL 对每条更新显示：Category Tag、标题（粗体）、摘要（最多 2 行截断）、相对时间（dayjs relativeTime）。
5. WHILE 某条更新的 `isRead` 为 false，THE WhatsNewPanel SHALL 在该条目左侧渲染蓝色圆点作为未读标识。
6. WHILE 某条更新的 `isRead` 为 true，THE WhatsNewPanel SHALL 不渲染圆点。
7. WHILE UnreadCount 大于 0，THE WhatsNewPanel SHALL 在面板顶部显示"Mark all as read"链接/按钮。
8. WHILE UnreadCount 等于 0，THE WhatsNewPanel SHALL 不渲染"Mark all as read"。
9. WHILE 面板内无任何 Published 更新，THE WhatsNewPanel SHALL 显示空状态文案"No updates yet"。
10. THE WhatsNewPanel SHALL Phase 1 不渲染"View all"链接。
11. THE Category_Tag SHALL 按以下颜色映射渲染：`NewFeature` → 蓝色，`Improvement` → 橙色，`BugFix` → 红色，`Announcement` → 紫色。

---

### Requirement 3：更新详情弹窗

**User Story：** 作为登录用户，我希望点击面板中某条更新后弹出详情弹窗，查看完整的富文本正文，并自动将该条标记为已读。

#### Acceptance Criteria

1. WHEN 用户点击 WhatsNewPanel 中的某条更新，THE WhatsNewDetail SHALL 以居中全屏遮罩 `el-dialog` 展开。
2. THE WhatsNewDetail SHALL 显示：大字体加粗标题、Category Tag + 发布日期、完整富文本正文、右上角关闭按钮。
3. THE WhatsNewDetail SHALL 使用 `v-html` 渲染富文本正文，渲染前必须通过 `DOMPurify.sanitize()` 过滤。
4. WHEN WhatsNewDetail 弹窗的 `open` 事件触发，THE System SHALL 立即调用 `POST ow/whats-new/v1/{id}/read` 标记该条为已读。
5. WHEN 标记已读成功后，THE WhatsNewPanel SHALL 将该条目 `isRead` 置为 true 并移除其圆点标识。
6. WHEN 标记已读成功后，THE WhatsNewBell SHALL 将本地 UnreadCount 减 1（若该条原本 `isRead` 为 false）。
7. IF `POST ow/whats-new/v1/{id}/read` 接口返回错误，THEN THE System SHALL 静默处理，不影响弹窗显示。

---

### Requirement 4：Mark All as Read

**User Story：** 作为登录用户，我希望通过一次点击将所有更新标记为已读，方便快速清除未读状态。

#### Acceptance Criteria

1. WHEN 用户点击面板顶部"Mark all as read"，THE System SHALL 调用 `POST ow/whats-new/v1/read-all`。
2. WHEN 调用成功后，THE WhatsNewPanel SHALL 将所有条目 `isRead` 置为 true，移除所有圆点标识，并隐藏"Mark all as read"按钮。
3. WHEN 调用成功后，THE WhatsNewBell SHALL 将本地 UnreadCount 置为 0，移除红点。
4. IF `POST ow/whats-new/v1/read-all` 调用失败，THEN THE System SHALL 通过 `ElMessage.error` 提示错误，不改变本地状态。

---

### Requirement 5：System Admin 管理入口

**User Story：** 作为 System Admin，我希望在头像下拉菜单中看到"Manage What's New"入口，方便进入内容管理页面。

#### Acceptance Criteria

1. WHILE 当前用户 `userType === 1`（System_Admin），THE UserLayout SHALL 在"My Profile"菜单项之后渲染"Manage What's New"菜单项。
2. WHILE 当前用户 `userType !== 1`，THE UserLayout SHALL 不渲染"Manage What's New"菜单项。
3. WHEN 用户点击"Manage What's New"，THE System SHALL 导航至路由 `/whats-new-management`。

---

### Requirement 6：管理列表页

**User Story：** 作为 System Admin，我希望在管理列表页查看所有更新条目及统计卡片，并能按状态筛选。

#### Acceptance Criteria

1. THE WhatsNewManagement_Page SHALL 挂载于路由 `/whats-new-management`。
2. WHEN 非 System_Admin 用户直接访问 `/whats-new-management`，THE System SHALL 将其重定向至首页 `/`。
3. THE WhatsNewManagement_Page SHALL 在页面顶部显示两个统计卡片：Published 数量 和 Drafts 数量，数据由管理列表接口同步返回。
4. THE WhatsNewManagement_Page SHALL 显示所有 `is_valid = true` 的更新条目，每条展示：Category Tag、Status Tag、标题（粗体）、摘要（1-2 行）、发布时间或"Not published"、编辑图标、删除图标。
5. THE Status_Tag SHALL 按以下颜色渲染：`Draft` → 灰色，`Published` → 绿色。
6. WHEN 数据加载完成，THE WhatsNewManagement_Page SHALL 同步更新统计卡片（Published / Drafts 计数）。
7. THE WhatsNewManagement_Page SHALL 支持通过点击统计卡片进行状态筛选：点击"Published"卡片时列表只显示已发布条目，点击"Drafts"卡片时列表只显示草稿条目，再次点击已激活的卡片则取消筛选恢复显示全部。
8. WHEN 筛选激活时，THE WhatsNewManagement_Page SHALL 以高亮样式标识当前激活的统计卡片；WHEN 无筛选时，所有卡片恢复默认样式。

---

### Requirement 7：创建与编辑更新

**User Story：** 作为 System Admin，我希望通过表单创建和编辑更新内容，支持富文本正文编辑，并选择立即发布或保存为草稿。

#### Acceptance Criteria

1. WHEN 用户点击"+ New update"，THE WhatsNewFormModal SHALL 以居中 Modal（宽约 600px）展开，所有字段清空。
2. WHEN 用户点击编辑图标，THE WhatsNewFormModal SHALL 展开并预填充该条目的已有数据。
3. THE WhatsNewFormModal SHALL 包含以下必填字段：Title（最多 100 字符）、Category（下拉选择，4 个枚举值）、Summary（最多 200 字符）、Content（使用现有 RichTextEditor 组件，默认完整工具栏）。
4. THE WhatsNewFormModal SHALL 展示三个互斥的 Publishing 选项：Publish Now、Schedule（disabled）、Save as draft。
5. WHILE Schedule 选项被渲染，THE WhatsNewFormModal SHALL 将其显示为灰色禁用状态（cursor: not-allowed），hover 时显示 tooltip "Coming soon"。
6. WHEN 用户选择"Publish Now"，THE WhatsNewFormModal SHALL 将底部按钮文字显示为"Publish update"；WHEN 用户选择"Save as draft"，THE WhatsNewFormModal SHALL 将按钮文字显示为"Save as draft"。
7. WHEN 用户点击确认按钮时有任意必填字段为空，THE WhatsNewFormModal SHALL 阻止提交并显示字段校验错误提示。
8. WHEN 用户选择"Publish Now"并提交，THE System SHALL 调用创建或更新接口，`status` 设为 1（Published）。
9. WHEN 用户选择"Save as draft"并提交，THE System SHALL 调用创建或更新接口，`status` 设为 0（Draft）。
10. WHEN 提交成功后，THE WhatsNewFormModal SHALL 关闭并触发管理列表刷新。
11. WHEN 用户点击"Cancel"，THE WhatsNewFormModal SHALL 关闭，不保存任何数据。
12. WHEN System_Admin 编辑一条 `status = Published` 的更新并保存，THE System SHALL 静默更新内容和元数据，不重置任何用户的已读状态，`publish_time` 保持不变。

---

### Requirement 8：删除更新

**User Story：** 作为 System Admin，我希望删除某条更新时看到确认弹窗，弹窗显示该条目已被多少用户阅读，防止误操作。

#### Acceptance Criteria

1. WHEN 用户点击某条目的删除图标，THE System SHALL 弹出确认弹窗，显示该条目标题和 `readCount`（读取本地管理列表缓存，无额外请求）。
2. THE Delete_Confirm_Dialog SHALL 显示文案："This update has been viewed by {readCount} users. Deleting it will remove it from all users' What's New panel."
3. WHEN 用户点击确认删除，THE System SHALL 调用 `DELETE ow/whats-new/v1/admin/{id}`。
4. WHEN 删除成功后，THE WhatsNewManagement_Page SHALL 从本地列表移除该条目并更新统计卡片。
5. WHEN 用户点击取消，THE Delete_Confirm_Dialog SHALL 关闭，不发起任何请求。
6. IF 删除接口返回错误，THEN THE System SHALL 通过 `ElMessage.error` 提示错误，列表状态保持不变。

---

### Requirement 9：后端 API — 用户端

**User Story：** 作为系统，我需要提供用户端 API，让所有已登录用户可查询更新列表、获取未读数和标记已读。

#### Acceptance Criteria

1. THE WhatsNew_API SHALL 提供 `GET ow/whats-new/v1/unread-count` 接口，返回当前用户的未读已发布更新数量；优先从 Redis 缓存读取（key: `whats-new:unread:{appCode}:{tenantId}:{userId}`，TTL 10 分钟），未命中时查询数据库。
2. THE WhatsNew_API SHALL 提供 `GET ow/whats-new/v1/panel` 接口，返回最多 10 条 `status = Published` 的更新，含 `isRead` 字段，按 `publish_time` 倒序排列。
3. THE WhatsNew_API SHALL 提供 `GET ow/whats-new/v1/{id}` 接口，返回单条更新的完整信息，含富文本 `content` 字段。
4. THE WhatsNew_API SHALL 提供 `POST ow/whats-new/v1/{id}/read` 接口，幂等地标记该条目为当前用户已读（`INSERT ... ON CONFLICT DO NOTHING`），并清除该用户的 Redis unread-count 缓存。
5. THE WhatsNew_API SHALL 提供 `POST ow/whats-new/v1/read-all` 接口，批量标记所有 Published 条目为当前用户已读，并清除 Redis 缓存。
6. WHEN `content` 字段存入数据库之前，THE WhatsNew_API SHALL 对 HTML 内容进行白名单过滤（XSS 防护）。

---

### Requirement 10：后端 API — 管理端

**User Story：** 作为系统，我需要提供管理端 API，仅允许 System Admin 创建、编辑、查询和删除更新内容。

#### Acceptance Criteria

1. THE WhatsNew_Admin_API SHALL 在所有管理端接口上应用 `[WFEAuthorize]` 特性，非 System_Admin 请求返回 403。
2. THE WhatsNew_Admin_API SHALL 提供 `GET ow/whats-new/v1/admin` 接口，返回所有 `is_valid = true` 的更新条目，支持可选 `status` 查询参数筛选，每条含 `readCount`（已读用户数），响应同时包含 `publishedCount` 和 `draftCount` 统计字段。
3. THE WhatsNew_Admin_API SHALL 提供 `POST ow/whats-new/v1/admin` 接口，创建新更新；`status = 1` 时自动写入 `publish_time = now()`。
4. THE WhatsNew_Admin_API SHALL 提供 `PUT ow/whats-new/v1/admin/{id}` 接口，编辑已有更新；若 `status` 从 Draft 改为 Published，则写入 `publish_time = now()`。
5. THE WhatsNew_Admin_API SHALL 提供 `DELETE ow/whats-new/v1/admin/{id}` 接口，执行软删除（`is_valid = false`），保留 `ff_whats_new_read_status` 历史记录。
6. IF 创建或编辑请求中 Title、Summary、Category、Content 任意一个为空，THEN THE WhatsNew_Admin_API SHALL 返回验证错误（400）。

---

### Requirement 11：数据库结构

**User Story：** 作为系统，我需要两张数据库表持久化更新内容和用户已读状态，支持多租户隔离。

#### Acceptance Criteria

1. THE Database SHALL 包含 `ff_whats_new` 表，字段包括：`id`（bigint snowflake 主键）、`app_code`、`tenant_id`、`title`（varchar 100）、`summary`（varchar 200）、`content`（text）、`category`（varchar 50）、`status`（int）、`publish_time`（timestamptz）、`scheduled_time`（timestamptz，暂留 Phase 2）、标准审计字段（`create_date`、`modify_date`、`create_by`、`modify_by`、`create_user_id`、`modify_user_id`）、`is_valid`（bool）。
2. THE Database SHALL 包含 `ff_whats_new_read_status` 表，字段包括：`id`（bigint snowflake 主键）、`whats_new_id`（bigint）、`user_id`（bigint）、`read_time`（timestamptz）、`app_code`、`tenant_id`。
3. THE `ff_whats_new_read_status` 表 SHALL 在 `(whats_new_id, user_id, app_code, tenant_id)` 上设置唯一约束，防止重复已读记录。
4. THE Database Migration SHALL 使用 `IF NOT EXISTS` / `IF EXISTS` 保证幂等性，并在 `MigrationManager.cs` 的 migrations 数组末尾注册。

---

### Requirement 12：权限控制

**User Story：** 作为系统，我需要严格控制管理端功能的访问权限，确保只有 System Admin 能够管理内容。

#### Acceptance Criteria

1. WHEN 非 System_Admin 用户访问路由 `/whats-new-management`，THE Frontend SHALL 通过路由守卫将其重定向至 `/`。
2. WHEN 非 System_Admin 用户调用任意管理端 API，THE Backend SHALL 返回 HTTP 403。
3. THE Frontend SHALL 通过 `userStore.userInfo.userType === 1` 判断是否为 System_Admin。
4. THE Backend SHALL 通过 `[WFEAuthorize]` 特性配合 `IsSystemAdmin` bypass 执行权限校验。

---

### Requirement 13：XSS 安全防护

**User Story：** 作为系统，我需要防止管理员在富文本内容中注入恶意脚本，确保所有用户的浏览安全。

#### Acceptance Criteria

1. WHEN System_Admin 通过管理端 API 提交 `content` 字段，THE Backend SHALL 在持久化之前对 HTML 内容进行白名单过滤，移除 `<script>`、`onerror` 等危险标签和属性。
2. WHEN Frontend 渲染 WhatsNewDetail 中的富文本 `content`，THE Frontend SHALL 先通过 `DOMPurify.sanitize()` 处理后再绑定至 `v-html`，任何情况下不跳过此步骤。
