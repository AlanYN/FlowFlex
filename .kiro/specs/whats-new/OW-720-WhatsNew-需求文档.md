# OW-720 — What's New 功能需求文档

> **票号**：OW-720  
> **优先级**：P0  
> **类型**：Story  
> **状态**：In Dev  
> **创建人**：Amanda Li  
> **负责人**：Kai Li  
> **当前冲刺**：OW.2026.08/21-09/03  
> **原型参考**：https://guidepost-three.vercel.app/whats-new  
> **最后评审**：2026-08-28（已与 Kai Li 逐项确认）

---

## 决策记录（已确认）

| #    | 问题              | 决策                                                                                       |
| ---- | ----------------- | ------------------------------------------------------------------------------------------ |
| D-01 | 管理端入口        | 用户头像下拉菜单"Manage What's New"，独立路由 `/whats-new-management`，非 Settings         |
| D-02 | Category 字段     | Phase 1 包含，4 个枚举值                                                                   |
| D-03 | Category 枚举值   | `NewFeature` / `Improvement` / `BugFix` / `Announcement`，原型中"News"为脏数据忽略         |
| D-04 | Summary 字段      | 独立必填字段，最多 200 字符，创建表单单独输入框                                            |
| D-05 | Schedule          | Phase 1 显示但 disabled，Phase 2 再实现                                                    |
| D-06 | 富文本工具栏      | 直接复用现有 `RichTextEditor` 组件，默认完整工具栏，不做限制                               |
| D-07 | 红点徽章          | 有未读就显示红点，全部已读后消失，Phase 1 不显示数字                                       |
| D-08 | 已读触发时机      | 点开详情弹窗 / 点击 Mark all as read；点开面板本身不触发已读                               |
| D-09 | unread-count 刷新 | 页面加载时拉一次；标记已读后前端本地更新计数，零轮询                                       |
| D-10 | 统计卡片          | Phase 1 只显示 Published / Drafts 两个，Scheduled 卡片 Phase 2 再加                        |
| D-11 | 删除确认已读人数  | 管理列表接口直接返回 `readCount`，删除确认弹窗读本地数据，无额外请求                       |
| D-12 | View all 链接     | Phase 1 面板底部不渲染此链接                                                               |
| D-13 | 权限判断          | 前端：`userStore.userInfo.userType === 1`；后端：`[WFEAuthorize]` + `IsSystemAdmin` bypass |

---

## 一、背景与问题

WFE 系统持续迭代，但缺乏统一的用户通知机制，造成以下问题：

| 问题       | 说明                             |
| ---------- | -------------------------------- |
| 用户无感知 | 新功能上线后用户不知道有哪些变化 |
| 培训成本高 | 需要逐个通知用户，效率低         |
| 无法追踪   | 无法得知哪些用户已了解新功能     |
| 缺乏后台   | 管理员没有统一的内容维护入口     |

---

## 二、解决方案概述

- **用户端**：页面右上角铃铛图标 + 未读红点 + 下拉面板 + 详情弹窗
- **管理端**：System Admin 专属管理页面，支持富文本创建、编辑、删除、发布

**设计原则：**

- 非侵入式 — 用户主动点击，不打断工作流
- 已读追踪 — 按用户独立记录已读状态
- 权限分离 — 仅 System Admin 可管理内容
- 时间排序 — 最新内容优先展示

---

## 三、功能范围（Phase 划分）

### ✅ Phase 1（MVP，本次实现）

| 编号  | 功能                                                              |
| ----- | ----------------------------------------------------------------- |
| P1-01 | 页面右上角铃铛通知图标（头像左侧）                                |
| P1-02 | 有未读时图标显示红色小圆点（无数字）                              |
| P1-03 | 点击图标展开 What's New 面板（最多 10 条）                        |
| P1-04 | 面板内显示 Category Tag、标题、摘要（≤2行）、相对时间 + 未读标识  |
| P1-05 | 面板顶部"Mark all as read"按钮（有未读时显示）                    |
| P1-06 | 点击某条更新弹出详情弹窗（富文本正文）                            |
| P1-07 | 打开详情弹窗后自动标记该条为已读                                  |
| P1-08 | 空状态显示"No updates yet"                                        |
| P1-09 | System Admin 头像下拉菜单可见"Manage What's New"入口              |
| P1-10 | 管理列表页（显示所有条目 + 状态筛选）                             |
| P1-11 | 管理列表顶部统计卡片（Published / Drafts 两个）                   |
| P1-12 | 创建/编辑更新（Title、Summary、Category、Content）                |
| P1-13 | 发布方式：Publish Now / Save as draft（Schedule 显示但 disabled） |
| P1-14 | 删除更新（带确认弹窗，已读人数从列表缓存读取）                    |
| P1-15 | 权限控制：管理端仅 System Admin 可访问                            |

### 🟡 Phase 2（后续迭代，本次不实现）

| 编号  | 功能                                                               |
| ----- | ------------------------------------------------------------------ |
| P2-01 | 数字徽章（未读数 > 0 时显示数字，最大 9+）                         |
| P2-02 | "View All" 完整历史页面（分页 + 时间筛选）+ 面板底部"View all"链接 |
| P2-03 | 定时发布（Schedule，指定未来时间自动发布，Hangfire 调度）          |
| P2-04 | 管理列表标题搜索框                                                 |
| P2-05 | 已读人数统计（128/500 格式，可点击查看用户列表）                   |
| P2-06 | 按角色/Team 指定发布范围                                           |
| P2-07 | 富文本支持图片上传（拖拽，接入 BlobStore）                         |
| P2-08 | 管理列表统计卡片增加 Scheduled                                     |

---

## 四、UI 交互详细说明

### 4.1 通知图标（Header）

**位置**：`navbar.vue` 右侧区域，`<Setting>` 与 `<UserLayout>` 之间插入新组件

```
┌──────────────────────────────────────────────────────┐
│  Logo    Navigation       主题 语言 设置 公司  🔔  👤 │
│                                             ↑        │
│                                         铃铛图标      │
│                                    (有未读时显示红点)  │
└──────────────────────────────────────────────────────┘
```

**行为：**

- 页面加载时调一次 `unread-count` 接口，有未读则显示红点
- 标记已读后前端本地更新计数，不重新请求，零轮询
- 点击后展开 What's New 面板（`el-popover`）

---

### 4.2 What's New 面板

**触发**：点击铃铛图标  
**形式**：`el-popover`，右对齐，宽度约 360px  
**面板打开时**：加载 panel 列表，**不触发已读**

```
┌─────────────────────────────────────┐
│  What's New  ●   Mark all as read   │  ← 有未读时显示右侧链接
├─────────────────────────────────────┤
│  ● [New Feature]  4m ago            │  ← 未读（左侧蓝色圆点）
│  标题文本（粗体）                    │
│  摘要文本（最多2行截断）             │
├─────────────────────────────────────┤
│    [Improvement]  30m ago           │  ← 已读（无圆点）
│  标题文本                           │
│  摘要文本                           │
├─────────────────────────────────────┤
│    [Bug Fix]  4h ago                │
│  标题文本                           │
│  摘要文本                           │
└─────────────────────────────────────┘
（Phase 1 不渲染"View all"链接）
```

**Category Tag 颜色映射：**

| Category     | Tag 颜色 |
| ------------ | -------- |
| NewFeature   | 蓝色     |
| Improvement  | 橙色     |
| BugFix       | 红色     |
| Announcement | 紫色     |

**时间格式**：dayjs `relativeTime` 插件（项目已有 dayjs 依赖）

**列表规则：**

- 只显示 `status = Published` 的内容
- 按 `publish_time` 倒序
- 最多 10 条
- 空状态显示"No updates yet"

---

### 4.3 更新详情弹窗

**触发**：点击面板中的某条更新  
**形式**：居中全屏遮罩 `el-dialog`

**内容：**

- 标题（大字体加粗）
- Category Tag + 发布日期
- 正文富文本（`v-html`，**必须先经过 `DOMPurify.sanitize()` 处理**）
- 右上角关闭按钮 ×

**副作用**：弹窗 `open` 事件触发时，立即调用 `POST /ow/whats-new/v1/{id}/read`，同时前端本地 unreadCount - 1，该条目 `isRead` 置为 true

---

### 4.4 用户头像下拉菜单

修改 `userLayout.vue`，在"My Profile"和"Sign Out"之间加入菜单项：

```
┌───────────────────┐
│  Amanda Li        │
│  amanda.li@...    │
├───────────────────┤
│  My Profile       │
│  Manage What's New│  ← v-if="userType === UserType.SystemAdmin"
├───────────────────┤
│  Sign out         │
└───────────────────┘
```

点击"Manage What's New"跳转至 `/whats-new-management`。

---

### 4.5 管理列表页

**路由**：`/whats-new-management`  
**权限**：`userType === 1`，非 System Admin 跳转首页

**页面结构：**

```
┌──────────────────────────────────────────────────────────────────┐
│  🔔  What's New                                  [+ New update]  │
│  Create and schedule product updates. System Admin only.         │
├──────────────────────────────────────────────────────────────────┤
│  [ 6 Published ]    [ 1 Drafts ]                                 │  ← 两个统计卡片（Phase 1）
├──────────────────────────────────────────────────────────────────┤
│  [New Feature] [Published]  标题（粗体）           ✎  🗑         │
│  摘要文本（1-2行）                                                │
│  🕐 Published Aug 20, 2026 - 8:30 AM                             │
├──────────────────────────────────────────────────────────────────┤
│  [Improvement] [Draft]  标题                       ✎  🗑         │
│  摘要文本                                                         │
│  🕐 Not published                                                 │
└──────────────────────────────────────────────────────────────────┘
```

**状态 Tag 颜色：**

- Draft → 灰色
- Published → 绿色

**管理列表接口返回字段（含 `readCount`，用于删除确认）：**

```json
{
  "id": "123456789",
  "title": "...",
  "summary": "...",
  "category": "NewFeature",
  "status": 1,
  "publishTime": "2026-08-20T08:30:00Z",
  "readCount": 128
}
```

---

### 4.6 创建/编辑弹窗

**触发**：点击"+ New update" / 点击编辑图标  
**形式**：居中 Modal，宽约 600px

**字段：**

| 字段     | 类型         | 必填 | 限制                                                                   |
| -------- | ------------ | ---- | ---------------------------------------------------------------------- |
| Title    | 文本输入框   | ✅   | 最多 100 字符，placeholder: "e.g. Scheduled publishing for What's New" |
| Category | 下拉选择     | ✅   | New feature / Improvement / Bug fix / Announcement                     |
| Summary  | 文本域       | ✅   | 最多 200 字符，用于面板列表摘要展示                                    |
| Content  | 富文本编辑器 | ✅   | 复用现有 `RichTextEditor` 组件，默认完整工具栏                         |

**Publishing 区域（三个选项卡互斥）：**

```
┌─────────────────┐  ┌──────────────────────┐  ┌─────────────────┐
│ 🔵  Publish now │  │  Schedule  [disabled] │  │  Save as draft  │
│ Visible to      │  │  Goes live auto-      │  │  Only you can   │
│ everyone imm.   │  │  matically at a set   │  │  see it         │
│                 │  │  time  [Phase 2]      │  │                 │
└─────────────────┘  └──────────────────────┘  └─────────────────┘
```

Schedule 选项卡：显示但 `disabled`（灰色，cursor not-allowed），hover tooltip："Coming soon"。

**底部按钮：**

- [Cancel] — 关闭弹窗，不保存
- [Publish update] / [Save as draft] — 根据 Publishing 选择动态切换按钮文字

---

### 4.7 删除确认弹窗

`readCount` 直接读管理列表本地数据，无需额外请求：

```
┌───────────────────────────────────────┐
│  Delete this update?                  │
│                                       │
│  "[更新标题]"                          │
│                                       │
│  This update has been viewed by       │
│  128 users. Deleting it will remove   │
│  it from all users' What's New panel. │
│                                       │
│         [Cancel]    [Delete]          │
└───────────────────────────────────────┘
```

---

## 五、数据模型

### 5.1 表：ff_whats_new

| 字段             | 类型               | 说明                                                       |
| ---------------- | ------------------ | ---------------------------------------------------------- |
| `id`             | bigint (snowflake) | 主键                                                       |
| `app_code`       | varchar            | 多租户 App Code                                            |
| `tenant_id`      | bigint             | 租户 ID                                                    |
| `title`          | varchar(100)       | 标题，必填                                                 |
| `summary`        | varchar(200)       | 摘要，必填，用于面板列表展示                               |
| `content`        | text               | 正文 HTML 内容，必填，存储前需 XSS 过滤                    |
| `category`       | varchar(50)        | 分类枚举：NewFeature / Improvement / BugFix / Announcement |
| `status`         | int                | 0=Draft, 1=Published（Phase 2 加 2=Scheduled）             |
| `publish_time`   | timestamptz        | 实际发布时间（Publish Now 时写入当前时间）                 |
| `scheduled_time` | timestamptz        | 计划发布时间（Phase 2 使用，暂留字段）                     |
| `create_date`    | timestamptz        | 创建时间                                                   |
| `modify_date`    | timestamptz        | 修改时间                                                   |
| `create_by`      | varchar            | 创建人名称                                                 |
| `modify_by`      | varchar            | 修改人名称                                                 |
| `create_user_id` | bigint             | 创建人 ID                                                  |
| `modify_user_id` | bigint             | 修改人 ID                                                  |
| `is_valid`       | bool               | 软删除标志（true=有效）                                    |

> **Phase 2 扩展字段**（暂不加）：`target_audience`、`target_roles`（jsonb）、`target_teams`（jsonb）

### 5.2 表：ff_whats_new_read_status

| 字段           | 类型               | 说明                 |
| -------------- | ------------------ | -------------------- |
| `id`           | bigint (snowflake) | 主键                 |
| `whats_new_id` | bigint             | 关联 ff_whats_new.id |
| `user_id`      | bigint             | 用户 ID              |
| `read_time`    | timestamptz        | 阅读时间             |
| `app_code`     | varchar            | 多租户 App Code      |
| `tenant_id`    | bigint             | 租户 ID              |

**唯一约束**：`(whats_new_id, user_id, app_code, tenant_id)`  
**插入策略**：`INSERT ... ON CONFLICT DO NOTHING`（幂等，防并发重复）

---

## 六、API 设计

路由前缀遵循项目规范：`ow/`

### 6.1 用户端 API（所有登录用户）

| Method | 路径                           | 说明                                                                                      |
| ------ | ------------------------------ | ----------------------------------------------------------------------------------------- |
| GET    | `ow/whats-new/v1/unread-count` | 获取当前用户未读数量（Redis 缓存，key: `whats-new:unread:{appCode}:{tenantId}:{userId}`） |
| GET    | `ow/whats-new/v1/panel`        | 获取面板列表（最多10条 Published，含 `isRead`）                                           |
| GET    | `ow/whats-new/v1/{id}`         | 获取单条详情（含完整富文本 content）                                                      |
| POST   | `ow/whats-new/v1/{id}/read`    | 标记单条为已读（幂等）                                                                    |
| POST   | `ow/whats-new/v1/read-all`     | 标记所有为已读                                                                            |

**GET panel 返回：**

```json
{
  "items": [
    {
      "id": "123456789",
      "title": "Notification bell in the top bar",
      "summary": "A new bell icon lives next to your avatar...",
      "category": "NewFeature",
      "publishTime": "2026-08-19T08:30:00Z",
      "isRead": false
    }
  ],
  "unreadCount": 2
}
```

### 6.2 管理端 API（System Admin Only，加 `[WFEAuthorize]`）

| Method | 路径                         | 说明                                               |
| ------ | ---------------------------- | -------------------------------------------------- |
| GET    | `ow/whats-new/v1/admin`      | 管理列表（支持 `status` 筛选，返回含 `readCount`） |
| POST   | `ow/whats-new/v1/admin`      | 创建更新                                           |
| PUT    | `ow/whats-new/v1/admin/{id}` | 编辑更新                                           |
| DELETE | `ow/whats-new/v1/admin/{id}` | 删除（软删除）                                     |

> `admin/stats` 单独统计接口取消，改为管理列表接口同时返回 Published/Draft 计数。

**POST 创建 Body：**

```json
{
  "title": "Notification bell in the top bar",
  "summary": "A new bell icon lives next to your avatar.",
  "content": "<p>A new <strong>bell icon</strong>...</p>",
  "category": "NewFeature",
  "status": 1
}
```

---

## 七、后端实现路径

### 7.1 数据库 Migration

```
SqlSugarDB/Migrations/Migration_20260828001_AddWhatsNew.cs
```

- 创建 `ff_whats_new` 表
- 创建 `ff_whats_new_read_status` 表（含唯一索引）
- 在 `MigrationManager.cs` 末尾注册

### 7.2 Domain 层

```
Domain/Entities/OW/WhatsNew.cs
Domain/Entities/OW/WhatsNewReadStatus.cs
Domain/Repository/OW/IWhatsNewRepository.cs
Domain/Repository/OW/IWhatsNewReadStatusRepository.cs
```

### 7.3 SqlSugarDB 层

```
SqlSugarDB/Repositories/OW/WhatsNewRepository.cs
SqlSugarDB/Repositories/OW/WhatsNewReadStatusRepository.cs
```

### 7.4 Application.Contracts 层

```
Application.Contracts/Dtos/OW/WhatsNew/WhatsNewPanelItemDto.cs
Application.Contracts/Dtos/OW/WhatsNew/WhatsNewDetailDto.cs
Application.Contracts/Dtos/OW/WhatsNew/WhatsNewAdminItemDto.cs
Application.Contracts/Dtos/OW/WhatsNew/CreateWhatsNewRequest.cs
Application.Contracts/Dtos/OW/WhatsNew/UpdateWhatsNewRequest.cs
Application.Contracts/IServices/OW/IWhatsNewService.cs
```

### 7.5 Application 层

```
Application/Services/OW/WhatsNewService.cs
Application/Maps/WhatsNewMapProfile.cs
```

**Service 关键逻辑：**

| 方法                  | 逻辑                                                                                             |
| --------------------- | ------------------------------------------------------------------------------------------------ |
| `GetPanelAsync`       | 查 status=1(Published)，Left Join read_status 表，注入 isRead，取最新10条                        |
| `GetUnreadCountAsync` | 先查 Redis，未命中再查 DB；缓存 key `whats-new:unread:{appCode}:{tenantId}:{userId}`，TTL 10分钟 |
| `MarkReadAsync`       | INSERT ON CONFLICT DO NOTHING；删除该用户 Redis 缓存 key                                         |
| `MarkAllReadAsync`    | 批量 INSERT，删除 Redis 缓存 key                                                                 |
| `CreateAsync` (Admin) | content 存入前调 HTML 白名单过滤（XSS 防护）                                                     |
| `DeleteAsync` (Admin) | 软删除（is_valid = false），read_status 保留（历史数据）                                         |
| `GetAdminListAsync`   | 返回所有条目 + readCount（LEFT JOIN COUNT read_status） + publishedCount/draftCount 统计         |

### 7.6 WebApi 层

```
WebApi/Controllers/OW/WhatsNewController.cs
```

- 用户端方法：`[Authorize]`，无额外权限
- 管理端方法：`[WFEAuthorize]`（`IsSystemAdmin` 自动 bypass）

---

## 八、前端实现路径

### 8.1 需要修改的现有文件

| 文件                                                 | 改动                                                                   |
| ---------------------------------------------------- | ---------------------------------------------------------------------- |
| `src/app/components/layout/components/navbar.vue`    | 在 `<Setting>` 后、`<UserLayout>` 前插入 `<WhatsNewBell>` 组件         |
| `src/app/components/navbarCompanents/userLayout.vue` | 在"My Profile"后加"Manage What's New"菜单项（`v-if="userType === 1"`） |

### 8.2 新增文件

```
src/app/apis/whatsNew/index.ts                                    （API 调用层）
src/app/components/navbarCompanents/WhatsNewBell.vue              （铃铛图标 + 红点 + 面板）
src/app/components/navbarCompanents/WhatsNewDetail.vue            （详情弹窗）
src/app/views/whatsNewManagement/index.vue                        （管理列表页）
src/app/views/whatsNewManagement/components/WhatsNewFormModal.vue （创建/编辑弹窗）
src/app/router/routers/modules/whatsNewManagement.ts              （路由配置）
```

### 8.3 关键实现细节

**WhatsNewBell.vue：**

- `onMounted` 调用 `getUnreadCount`，结果存入本地 `ref<number>`
- `markRead(id)` 后：本地 `unreadCount--`，对应条目 `isRead = true`
- `markAllRead()` 后：本地 `unreadCount = 0`，所有条目 `isRead = true`
- 面板使用 `el-popover`，打开时调 `getPanel`（每次打开都刷新列表）

**WhatsNewDetail.vue：**

- 富文本渲染：`v-html="DOMPurify.sanitize(content)"`
- `onOpen` 回调中调 `markRead(id)`

**whatsNewManagement/index.vue：**

- 路由守卫：`userType !== 1` 时 `router.replace('/')`
- 统计卡片数据从列表接口同步返回，不单独请求

**路由配置：**

```ts
{
  path: '/whats-new-management',
  component: () => import('@/views/whatsNewManagement/index.vue'),
  meta: { title: "What's New Management", requiresSystemAdmin: true }
}
```

### 8.4 富文本编辑器

直接复用现有 `src/app/components/RichTextEditor/index.vue`，默认完整工具栏，无需修改。

---

## 九、权限矩阵

| 操作                         | 普通用户       | System Admin |
| ---------------------------- | -------------- | ------------ |
| 查看铃铛图标和红点           | ✅             | ✅           |
| 查看 What's New 面板         | ✅             | ✅           |
| 查看更新详情                 | ✅             | ✅           |
| 标记已读                     | ✅             | ✅           |
| 头像菜单"Manage What's New"  | ❌             | ✅           |
| 访问 `/whats-new-management` | ❌（重定向）   | ✅           |
| 创建/编辑/删除更新           | ❌（后端 403） | ✅           |

**前端判断**：`userStore.userInfo.userType === 1`（`UserType.SystemAdmin`）  
**后端判断**：`[WFEAuthorize]` → `IsSystemAdmin`（`UserPermissions.Any(p => p.UserType == 1)`）

---

## 十、验收标准（Phase 1 MVP）

| #   | 场景                      | 验收标准                                                                            |
| --- | ------------------------- | ----------------------------------------------------------------------------------- |
| 1   | 有未读更新                | 铃铛图标右上角出现红色小圆点                                                        |
| 2   | 无未读更新                | 铃铛图标无红点，无数字                                                              |
| 3   | 点击铃铛                  | 展开面板，加载最新 10 条 Published 内容，**不触发已读**                             |
| 4   | 面板展示                  | 每条显示：Category Tag、标题、摘要（≤2行）、相对时间                                |
| 5   | 未读标识                  | 未读条目左侧有蓝色圆点，已读无圆点                                                  |
| 6   | 点击某条                  | 弹出详情弹窗，显示完整富文本内容                                                    |
| 7   | 弹窗打开后                | 该条立即标记为已读，面板圆点消失，unreadCount - 1                                   |
| 8   | Mark all as read          | 点击后所有条目标记为已读，红点消失                                                  |
| 9   | 空状态                    | 无 Published 内容时显示"No updates yet"                                             |
| 10  | 管理入口可见性            | System Admin 头像菜单可见"Manage What's New"，普通用户不可见                        |
| 11  | 管理列表                  | 显示所有条目，顶部两个统计卡片：Published / Drafts 数量正确                         |
| 12  | 创建更新（Publish Now）   | 填写 Title + Summary + Category + Content，选 Publish Now，保存后立即出现在用户面板 |
| 13  | 创建更新（Save as draft） | 选 Save as draft，保存后不出现在用户面板，管理列表显示 Draft 状态                   |
| 14  | Schedule 选项             | Schedule 选项卡可见但不可点击（disabled），hover 提示"Coming soon"                  |
| 15  | 编辑更新                  | 编辑已发布条目保存后，用户侧面板内容同步更新                                        |
| 16  | 删除更新                  | 确认弹窗显示标题和已读人数（读列表缓存），确认后用户面板不再显示该条                |
| 17  | XSS 防护                  | 管理员输入 `<script>alert(1)</script>`，前端渲染时不执行                            |
| 18  | 路由权限                  | 普通用户直接访问 `/whats-new-management` 被重定向到首页                             |
| 19  | 后端权限                  | 普通用户直接调管理端 API，返回 403                                                  |
| 20  | Category 显示             | 四种 Tag（New Feature / Improvement / Bug Fix / Announcement）颜色区分正确          |

---

## 十一、风险与注意事项

| 风险                   | 等级 | 缓解方案                                                                   |
| ---------------------- | ---- | -------------------------------------------------------------------------- |
| XSS 攻击（富文本）     | 高   | 后端存储前 HTML 白名单过滤；前端 `v-html` 前 `DOMPurify.sanitize()`        |
| 并发标记已读冲突       | 低   | read_status 唯一约束 + `INSERT ON CONFLICT DO NOTHING`                     |
| unread-count 过期      | 低   | 零轮询，页面加载时拉一次，标记后本地更新，Redis 作为加速层不影响正确性     |
| 富文本图片 base64 体积 | 低   | Phase 1 允许 base64，`RichTextEditor` 默认限制 5MB；Phase 2 接入 BlobStore |

---

## 十二、Phase 2 预留设计（存档）

### 12.1 定时发布（Schedule）

- 后端：Hangfire 调度任务，到达 `scheduled_time` 自动将 status 改为 Published，失败时告警
- 前端：Publishing 区域 Schedule 选项卡激活，选中后展示 `Publish date & time` 日期时间选择器

### 12.2 View All 页面

- 路由：`/whats-new`
- 面板底部"View all"链接激活
- 分页 20 条/页，支持时间范围筛选

### 12.3 数字徽章

- 铃铛图标改用 `el-badge`，显示未读数字，最大 9+

### 12.4 已读人数统计

- 管理列表每条显示"128 / 500"（已读/活跃用户）
- 点击可查看已读用户列表

### 12.5 按角色/Team 发布

- `ff_whats_new` 表增加 `target_audience`、`target_roles`（jsonb）、`target_teams`（jsonb）
- 用户端查询时后端按当前用户角色/Team 过滤，OR 逻辑

---

## 十三、预估工时参考

| 工作项                               | 工时（人天）  |
| ------------------------------------ | ------------- |
| Migration + 实体 + Repository        | 0.5           |
| Service 层（含缓存、XSS 过滤）       | 1.5           |
| Controller + DTO（管理端 + 用户端）  | 1.0           |
| 前端：navbar 改动 + userLayout 改动  | 0.5           |
| 前端：WhatsNewBell + 面板 + 详情弹窗 | 1.5           |
| 前端：管理列表页                     | 1.0           |
| 前端：创建/编辑弹窗                  | 1.0           |
| 前端：路由 + 权限守卫                | 0.5           |
| 联调 + Bug Fix                       | 1.0           |
| **Phase 1 合计**                     | **~8.5 人天** |
