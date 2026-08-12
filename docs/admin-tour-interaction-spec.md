# Admin Tour 交互引导规格

> 本文档覆盖 FlowFlex 管理配置端（Admin）各功能页面的 Tour 引导内容及实现规格。
> 与 `workflow-tour-interaction-spec.md` 配套，共同构成完整的引导体系。

---

## 设计原则（继承 Workflow Tour）

- **用户主导**：凡需要切换状态（打开弹窗、跳转页面、切换 Tab）的步骤，一律由用户主动点击触发，tour 只高亮目标元素并给出提示
- **不自动触发**：tour 不通过程序模拟点击来打开弹窗或切换视图
- **`waitForUserClick`**：用户必须点击才能推进的步骤使用此机制，点击后 tour 隐藏遮罩并等待下一个状态就绪
- **`lazyElement`**：元素在用户操作后才进入 DOM 时（弹窗内容、dropdown 菜单项）使用此标记
- **弹窗内步骤**：创建/编辑弹窗内的步骤，通过 `auto-start=false` + 右下角 "?" FAB 手动触发，或在列表页 tour 中通过 `waitForUserClick` 引导用户打开弹窗后继续

---

## 一、Checklist Tour（8 步）

### 页面结构说明

Checklist 引导分两段：

- **Segment A（列表页，2 步）**：页面概览 → New 按钮（引导用户打开创建弹窗）
- **Segment B（创建弹窗内，5 步）**：弹窗基本信息 → 保存
- **Segment C（列表页回视，1 步）**：Assignments 列说明

**文件**：`views/onboard/checkList/index.vue`

### Tour 实例拆分

| Tour 实例       | persistKey             | 挂载位置                                      | auto-start          | 步骤        |
| --------------- | ---------------------- | --------------------------------------------- | ------------------- | ----------- |
| 列表页 tour     | `checklist-list-tour`  | `checkList/index.vue`                         | `true`              | Step 1–2, 8 |
| 创建弹窗 tour   | `checklist-form-tour`  | `checkList/index.vue` `v-if="showDialog"`     | `false`（FAB 触发） | Step 3–4, 7 |
| Tasks 弹窗 tour | `checklist-tasks-tour` | `checkList/index.vue` `v-if="showTaskDialog"` | `false`（FAB 触发） | Step 5–6    |

### Step 详情

| Step | Anchor                                    | Title                | Content                                                                                                        | 交互模式                          |
| ---- | ----------------------------------------- | -------------------- | -------------------------------------------------------------------------------------------------------------- | --------------------------------- |
| 1    | `[data-tour="checklist-page-header"]`     | Checklist Management | This is where you manage all checklist templates. Checklists track tasks that need to be completed one by one. | 普通高亮，点 Next                 |
| 2    | `[data-tour="checklist-new-btn"]`         | Create Checklist     | Click **New** to create a new checklist template.                                                              | 普通高亮，点 Next                 |
| 3    | `[data-tour="checklist-name-input"]`      | Basic Info           | Enter the Checklist name and description.                                                                      | 普通高亮（创建弹窗内），点 Next   |
| 4    | `[data-tour="checklist-team-dropdown"]`   | Assign Team          | Select the team responsible for this Checklist.                                                                | 普通高亮，点 Next                 |
| 5    | `[data-tour="checklist-tasks-area"]`      | Add Tasks            | Add task items one by one. Assignees will check off each task in the Case.                                     | 普通高亮（Tasks 弹窗内），点 Next |
| 6    | `[data-tour="checklist-task-input"]`      | Task Details         | Enter the task name. Each task supports Notes and Attachments.                                                 | 普通高亮，点 Done                 |
| 7    | `[data-tour="checklist-save-btn"]`        | Save Checklist       | After saving, this Checklist can be added to Workflow Stages.                                                  | 普通高亮（创建弹窗内），点 Done   |
| 8    | `[data-tour="checklist-assignments-col"]` | View Assignments     | The Assignments column shows which Workflow Stages this Checklist is assigned to.                              | 普通高亮（列表页），点 Done       |

> **Tasks 弹窗说明（Step 5–6）**：已确认 Tasks 通过 "View Tasks" 按钮打开独立弹窗（`showTaskDialog`）管理，与创建/编辑弹窗分离。`checklist-tasks-tour` 在 Tasks 弹窗打开时挂载，`auto-start=false`，由 FAB 手动触发；FAB `fabContainer` 指向 Tasks 弹窗自己的 `.el-overlay-dialog`。

---

## 二、Questionnaire Tour（9 步）

### 页面结构说明

Questionnaire 引导分两段：

- **Segment A（列表页，2 步）**：页面概览 → New 按钮（点击后跳转到 `createQuestion.vue`）
- **Segment B（编辑页 createQuestion.vue，7 步）**：名称 → Sections → 添加 Section → Questions → Question Type → Required → Save

**文件**：

- 列表页：`views/onboard/questionnaire/index.vue`
- 编辑页：`views/onboard/questionnaire/createQuestion.vue`

### Tour 实例拆分

| Tour 实例   | persistKey                  | 挂载位置                           | auto-start                   | 步骤     |
| ----------- | --------------------------- | ---------------------------------- | ---------------------------- | -------- |
| 列表页 tour | `questionnaire-list-tour`   | `questionnaire/index.vue`          | `true`                       | Step 1–2 |
| 编辑页 tour | `questionnaire-editor-tour` | `questionnaire/createQuestion.vue` | `true`（进入编辑页自动启动） | Step 3–9 |

### Step 详情

| Step | Anchor                                               | Title                    | Content                                                                                              | 交互模式                                                                   |
| ---- | ---------------------------------------------------- | ------------------------ | ---------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------- |
| 1    | `[data-tour="questionnaire-page-header"]`            | Questionnaire Management | This is where you manage all questionnaire templates. Questionnaires collect structured information. | 普通高亮，点 Next                                                          |
| 2    | `[data-tour="questionnaire-new-btn"]`                | Create Questionnaire     | Click **New** to create a new questionnaire template.                                                | `waitForUserClick`：高亮 New 按钮，用户点击后跳转编辑页，tour 在列表页结束 |
| 3    | `[data-tour="questionnaire-name-input"]`             | Basic Info               | Enter the Questionnaire name and description.                                                        | 普通高亮（编辑页），点 Next                                                |
| 4    | `[data-tour="questionnaire-sections-area"]`          | Add Section              | A questionnaire consists of multiple Sections to organize questions.                                 | 普通高亮，点 Next                                                          |
| 5    | `[data-tour="questionnaire-add-section-btn"]`        | Add Section              | Click to add a Section and group questions.                                                          | 普通高亮，点 Next                                                          |
| 6    | `[data-tour="questionnaire-questions-area"]`         | Add Questions            | Add Questions within each Section.                                                                   | 普通高亮，点 Next                                                          |
| 7    | `[data-tour="questionnaire-question-type-dropdown"]` | Question Type            | Supports multiple types: Single-line Text, People, Date Picker, Number, File, etc.                   | 普通高亮，点 Next                                                          |
| 8    | `[data-tour="questionnaire-required-toggle"]`        | Mark as Required         | Questions marked as Required must be filled before submitting.                                       | 普通高亮，点 Next                                                          |
| 9    | `[data-tour="questionnaire-save-btn"]`               | Save Questionnaire       | After saving, this Questionnaire can be added to Workflow Stages.                                    | 普通高亮，点 Done                                                          |

> **跨页说明**：列表页 tour（Step 1–2）与编辑页 tour（Step 3–9）是两个独立的 TourGuide 实例，共用 `checkSeenRemote` / `markSeenRemote` 机制分别记录状态。Step 2 使用 `waitForUserClick` 让用户主动点击 New 跳转，列表页 tour 随之结束；编辑页 tour 在 `createQuestion.vue` 挂载时自动启动。

---

## 三、Dynamic Field Tour（5 步）

### 页面结构说明

Dynamic Field 引导分两段：

- **Segment A（列表页，2 步）**：页面概览 → New 按钮
- **Segment B（创建/编辑侧抽屉或弹窗，3 步）**：Field Name → Field Type → Save

**文件**：`views/dynamicFields/index.vue`

### Tour 实例拆分

| Tour 实例     | persistKey                | 挂载位置                                     | auto-start          | 步骤     |
| ------------- | ------------------------- | -------------------------------------------- | ------------------- | -------- |
| 列表页 tour   | `dynamic-field-list-tour` | `dynamicFields/index.vue`                    | `true`              | Step 1–2 |
| 创建弹窗 tour | `dynamic-field-form-tour` | `dynamicFields/index.vue`（抽屉/弹窗打开时） | `false`（FAB 触发） | Step 3–5 |

### Step 详情

| Step | Anchor                                      | Title                    | Content                                                                                           | 交互模式                         |
| ---- | ------------------------------------------- | ------------------------ | ------------------------------------------------------------------------------------------------- | -------------------------------- |
| 1    | `[data-tour="dynamic-field-page-header"]`   | Dynamic Field Management | This is where you manage all dynamic fields. Dynamic Fields collect simple information in Stages. | 普通高亮，点 Next                |
| 2    | `[data-tour="dynamic-field-new-btn"]`       | Create Field             | Click **New** to create a new dynamic field.                                                      | 普通高亮，点 Next                |
| 3    | `[data-tour="dynamic-field-name-input"]`    | Field Name               | Enter the field name that will be displayed to Assignees.                                         | 普通高亮（弹窗/抽屉内），点 Next |
| 4    | `[data-tour="dynamic-field-type-dropdown"]` | Field Type               | Select the field type: Text, Number, Date, People, Dropdown, etc.                                 | 普通高亮，点 Next                |
| 5    | `[data-tour="dynamic-field-save-btn"]`      | Save Field               | After saving, this field can be added to Workflow Stages.                                         | 普通高亮，点 Done                |

---

## 四、Integration Settings Tour（3 步）

### 页面结构说明

Integration Settings 的 Quick Links 和 Attachment Sharing 均在主页面可见，无需跨页或跨弹窗，为最简单的 tour 场景。

**文件**：`views/integration-settings/index.vue`

### Tour 实例拆分

| Tour 实例 | persistKey                  | 挂载位置                         | auto-start | 步骤     |
| --------- | --------------------------- | -------------------------------- | ---------- | -------- |
| 主页 tour | `integration-settings-tour` | `integration-settings/index.vue` | `true`     | Step 1–3 |

### Step 详情

| Step | Anchor                                              | Title                | Content                                                                                             | 交互模式          |
| ---- | --------------------------------------------------- | -------------------- | --------------------------------------------------------------------------------------------------- | ----------------- |
| 1    | `[data-tour="integration-page-header"]`             | Integration Settings | Configure integrations with external systems, including Quick Links and Attachment Sharing.         | 普通高亮，点 Next |
| 2    | `[data-tour="integration-quick-links-area"]`        | External Links       | Configure Quick Links so Assignees can quickly jump to external systems (e.g., pricing tools, CRM). | 普通高亮，点 Next |
| 3    | `[data-tour="integration-attachment-sharing-area"]` | Attachment Sharing   | Configure attachment sharing rules to control how files sync between systems.                       | 普通高亮，点 Done |

---

## 五、Tools Tour（4 步）

### 页面结构说明

Tools 引导分两段：

- **Segment A（列表页，2 步）**：页面概览 → New 按钮
- **Segment B（创建/编辑弹窗，2 步）**：HTTP 配置 → Link to Workflow

**文件**：`views/actions/index.vue`

### Tour 实例拆分

| Tour 实例     | persistKey        | 挂载位置                          | auto-start          | 步骤     |
| ------------- | ----------------- | --------------------------------- | ------------------- | -------- |
| 列表页 tour   | `tools-list-tour` | `actions/index.vue`               | `true`              | Step 1–2 |
| 创建弹窗 tour | `tools-form-tour` | `actions/index.vue`（弹窗打开时） | `false`（FAB 触发） | Step 3–4 |

### Step 详情

| Step | Anchor                                 | Title             | Content                                                                                   | 交互模式                                         |
| ---- | -------------------------------------- | ----------------- | ----------------------------------------------------------------------------------------- | ------------------------------------------------ |
| 1    | `[data-tour="tools-page-header"]`      | Tool Management   | This is where you manage all Tools. Tools integrate external APIs or automate operations. | 普通高亮，点 Next                                |
| 2    | `[data-tour="tools-new-btn"]`          | Create Tool       | Click **New** to configure a new Tool.                                                    | 普通高亮，点 Next                                |
| 3    | `[data-tour="tools-http-config-area"]` | API Configuration | Configure the HTTP request: URL, Method, Headers, Body, etc.                              | `lazyElement: true`，普通高亮（弹窗内），点 Next |
| 4    | `[data-tour="tools-link-to-workflow"]` | Link to Workflow  | Link this Tool to a specific Workflow to trigger automatically in the process.            | `lazyElement: true`，普通高亮，点 Done           |

---

## 六、Manage Teams Tour（5 步）

### ⚠️ 特殊限制：iframe 页面

Manage Teams 页面（`views/authorityManagement/teams.vue`）通过 `<iframe>` 嵌入 IDM 系统，**无法在 iframe 内部元素上添加 `data-tour` 属性**，driver.js 也无法穿透 iframe 边界高亮其内部元素。

### 处理方案

仅在 iframe **外部**（`teams.vue` 本身）展示一个覆盖整个 iframe 的说明性 tour，通过文字描述引导用户操作，不做精确元素高亮。每一步高亮整个 iframe 区域，内容文字对应操作说明。

**文件**：`views/authorityManagement/teams.vue`

### Tour 实例拆分

| Tour 实例     | persistKey          | 挂载位置                        | auto-start | 步骤     |
| ------------- | ------------------- | ------------------------------- | ---------- | -------- |
| Teams 页 tour | `manage-teams-tour` | `authorityManagement/teams.vue` | `true`     | Step 1–5 |

### Step 详情

| Step | Anchor                                                | Title           | Content                                                                                     | 交互模式                              |
| ---- | ----------------------------------------------------- | --------------- | ------------------------------------------------------------------------------------------- | ------------------------------------- |
| 1    | `[data-tour="teams-iframe-container"]`（iframe 外框） | Team Management | This is where you manage all teams. Teams are used to assign responsibility for Checklists. | 普通高亮（整个 iframe 区域），点 Next |
| 2    | `[data-tour="teams-iframe-container"]`                | Create Team     | Click **Add New Team** inside the panel to create a new team.                               | 普通高亮，点 Next                     |
| 3    | `[data-tour="teams-iframe-container"]`                | Team Name       | Enter the team name (e.g., Implementation, Billing, WMS Support).                           | 普通高亮，点 Next                     |
| 4    | `[data-tour="teams-iframe-container"]`                | Add Members     | Optionally add team members. Members indicate who is responsible for the team's tasks.      | 普通高亮，点 Next                     |
| 5    | `[data-tour="teams-iframe-container"]`                | Save Team       | After saving, this Team can be selected in the Checklist configuration.                     | 普通高亮，点 Done                     |

> **实现备注**：所有步骤共用同一个锚点 `teams-iframe-container`（`<div data-tour="teams-iframe-container">` 包裹 `<iframe>`），仅通过内容文字引导，不做精确高亮。如后续 IDM 侧支持 postMessage 通信，可考虑升级为跨 iframe 联动引导。

---

## 七、邮件通知优化（非 Tour，后端改造）

### 需求描述

在任务分配邮件中增加 **"What you need to do"** 简要说明区块，减少被分配人进入页面前的困惑，让用户在收到邮件时就清楚自己需要完成哪些操作。

### 优化后邮件结构示例

```
You have been assigned to Stage 4: Billing Setup of Case: Acme Corp

What you need to do:
• Complete the Billing Setup checklist (4 tasks)
• Fill out the Price List questionnaire
• Click 'Advance to Next Stage' when done
```

### 内容生成规则

邮件发送时，后端需根据该阶段的组件配置动态生成 "What you need to do" 列表：

| 组件类型         | 生成规则                                              | 示例文案                                       |
| ---------------- | ----------------------------------------------------- | ---------------------------------------------- |
| Checklist        | `Complete the {checklist name} checklist ({n} tasks)` | Complete the Billing Setup checklist (4 tasks) |
| Questionnaire    | `Fill out the {questionnaire name} questionnaire`     | Fill out the Price List questionnaire          |
| Dynamic Fields   | `Fill in the required fields`                         | Fill in the required fields                    |
| Quick Link       | `Complete the action at: {link name}`                 | Complete the action at: Pricing Tool           |
| File Upload      | `Upload the required documents`                       | Upload the required documents                  |
| 最后一行（始终） | `Click 'Advance to Next Stage' when done`             | Click 'Advance to Next Stage' when done        |

---

### 现有实现情况（已核查代码）

| 项目             | 内容                                                                                                         |
| ---------------- | ------------------------------------------------------------------------------------------------------------ |
| **模板引擎**     | 自研 Handlebars-like（`{{variable}}`、`{{#if var}}...{{/if}}`），纯字符串替换，无外部依赖                    |
| **模板文件**     | `Application/Templates/Email/stage_assigned_notification_en.html`（Embedded Resource）                       |
| **发送方法**     | `EmailService.SendStageAssignedNotificationAsync(to, assigneeName, caseName, stageName, priority, caseLink)` |
| **调用方**       | `OnboardingStageProgressService.SendStageAssignedNotificationIfChangedAsync`                                 |
| **现有模板变量** | `assigneeName`, `caseName`, `stageName`, `priority`, `caseLink`, `year`                                      |

**调用链路：**

```
OnboardingStageProgressService.SendStageAssignedNotificationIfChangedAsync
  → EmailService.SendStageAssignedNotificationAsync(to, allNames, caseName, stageName, priority, caseLink)
    → EmailTemplateService.Render("stage_assigned_notification_en", variables)
      → 读取 Application/Templates/Email/stage_assigned_notification_en.html
```

**组件数据来源：** `stageProgress.Components`（已在 `SendStageAssignedNotificationIfChangedAsync` 可访问的 `OnboardingStageProgress` 对象上，无需额外查询）

---

### 改造范围

#### 1. `EmailService`（`Application/Services/MessageCenter/EmailService.cs`）

新增可选参数 `whatToDoHtml`，向后兼容：

```csharp
public async Task<bool> SendStageAssignedNotificationAsync(
    string to, string assigneeName, string caseName,
    string stageName, string priority, string caseLink,
    string? whatToDoHtml = null)
```

模板变量字典新增：

```csharp
["whatToDoHtml"] = whatToDoHtml ?? string.Empty,
["hasWhatToDo"]  = !string.IsNullOrEmpty(whatToDoHtml)
```

#### 2. `IEmailService` 接口（`Application.Contracts/IServices/OW/IEmailService.cs`）

同步更新方法签名，增加可选参数 `whatToDoHtml`。

#### 3. `OnboardingStageProgressService`（`Application/Services/OW/OnboardingServices/OnboardingStageProgressService.cs`）

在调用 `SendStageAssignedNotificationAsync` 前，从 `stageProgress.Components` 生成 HTML 列表字符串：

```csharp
// 伪代码
var whatToDoHtml = BuildWhatToDoHtml(stageProgress.Components);
return await _emailService.SendStageAssignedNotificationAsync(
    user.Email, allNames, caseName, stageName, priority, caseLink, whatToDoHtml);
```

新增私有方法 `BuildWhatToDoHtml()`，按组件类型生成 `<ul><li>…</li></ul>` HTML，最后追加固定文案 `Click 'Advance to Next Stage' when done`。

#### 4. 邮件模板（`Application/Templates/Email/stage_assigned_notification_en.html`）

在 "Case Details" 卡片下方、"Access the Case" 按钮上方插入：

```html
{{#if hasWhatToDo}}
<div
  style="background-color: #f0f4ff; border-left: 4px solid #7e22ce; border-radius: 4px; padding: 20px 25px; margin-bottom: 30px;"
>
  <h3 style="color: #495057; margin-bottom: 15px; font-size: 15px;">
    What you need to do
  </h3>
  {{{whatToDoHtml}}}
</div>
{{/if}}
```

注意：`whatToDoHtml` 使用三重花括号 `{{{...}}}` 输出未转义 HTML（模板引擎已支持此语法）。

---

### 无需改动

- `Domain/Entities/OW/` 实体层（组件数据已在 `OnboardingStageProgress.Components` 上）
- 数据库 Migration（无新字段）
- 其他邮件方法

---

## 实现优先级

| 优先级       | 项目                 | 复杂度 | 说明                                                                        |
| ------------ | -------------------- | ------ | --------------------------------------------------------------------------- |
| 低（最先做） | Integration Settings | ★☆☆    | 3 步，全部在主页面可见，无跨页/弹窗                                         |
| 低           | Dynamic Field        | ★☆☆    | 5 步，主页面 2 步 + `el-dialog` 弹窗 3 步                                   |
| 中           | Checklist            | ★★☆    | 8 步；Step 3–7 在创建弹窗内；Step 5–6 在 "View Tasks" 弹窗内（lazyElement） |
| 中           | Tools                | ★★☆    | 4 步；弹窗内有 Tab 切换，Step 3–4 在同一弹窗不同 Tab（lazyElement）         |
| 中           | Questionnaire        | ★★☆    | 9 步，跨页（列表 → createQuestion.vue 编辑页）两段 tour                     |
| 高           | Manage Teams         | ★★★    | iframe 限制，只能外框高亮 + 文字引导，体验受限                              |
| 中           | 邮件通知优化         | ★★☆    | 后端改造，调用链路和模板引擎已确认，改动范围清晰可控                        |

---

## 锚点清单汇总

| 锚点                                   | 文件                                          | 说明                               |
| -------------------------------------- | --------------------------------------------- | ---------------------------------- |
| `checklist-page-header`                | `checkList/index.vue`                         | Checklist 列表页 PageHeader        |
| `checklist-new-btn`                    | `checkList/index.vue`                         | New Checklist 按钮                 |
| `checklist-name-input`                 | `checkList/index.vue`（创建弹窗内）           | Checklist Name 输入框              |
| `checklist-team-dropdown`              | `checkList/index.vue`（创建弹窗内）           | Team 下拉选择                      |
| `checklist-tasks-area`                 | `checkList/index.vue`（tasks 弹窗或同层区域） | Tasks 列表区域                     |
| `checklist-task-input`                 | `checkList/index.vue`（tasks 弹窗内）         | 任务名称输入框                     |
| `checklist-save-btn`                   | `checkList/index.vue`（创建弹窗内）           | Save / Create 按钮                 |
| `checklist-assignments-col`            | `checkList/index.vue`（列表表格）             | Assignments 列头或列内容           |
| `questionnaire-page-header`            | `questionnaire/index.vue`                     | Questionnaire 列表页 PageHeader    |
| `questionnaire-new-btn`                | `questionnaire/index.vue`                     | New Questionnaire 按钮             |
| `questionnaire-name-input`             | `questionnaire/createQuestion.vue`            | 问卷名称输入框                     |
| `questionnaire-sections-area`          | `questionnaire/createQuestion.vue`            | Sections 管理区域                  |
| `questionnaire-add-section-btn`        | `questionnaire/createQuestion.vue`            | 添加 Section 按钮                  |
| `questionnaire-questions-area`         | `questionnaire/createQuestion.vue`            | Questions 区域（Section 内）       |
| `questionnaire-question-type-dropdown` | `questionnaire/createQuestion.vue`            | Question Type 下拉                 |
| `questionnaire-required-toggle`        | `questionnaire/createQuestion.vue`            | Required 开关                      |
| `questionnaire-save-btn`               | `questionnaire/createQuestion.vue`            | Save Questionnaire 按钮            |
| `dynamic-field-page-header`            | `dynamicFields/index.vue`                     | Dynamic Fields 列表页 PageHeader   |
| `dynamic-field-new-btn`                | `dynamicFields/index.vue`                     | Add New Field 按钮                 |
| `dynamic-field-name-input`             | `dynamicFields/index.vue`（弹窗/抽屉内）      | 字段名称输入框                     |
| `dynamic-field-type-dropdown`          | `dynamicFields/index.vue`（弹窗/抽屉内）      | 字段类型下拉                       |
| `dynamic-field-save-btn`               | `dynamicFields/index.vue`（弹窗/抽屉内）      | Save 按钮                          |
| `integration-page-header`              | `integration-settings/index.vue`              | Integration Settings 页 PageHeader |
| `integration-quick-links-area`         | `integration-settings/index.vue`              | Quick Links 卡片区域               |
| `integration-attachment-sharing-area`  | `integration-settings/index.vue`              | Attachment Sharing 卡片区域        |
| `tools-page-header`                    | `actions/index.vue`                           | Tools 列表页 PageHeader            |
| `tools-new-btn`                        | `actions/index.vue`                           | New Tool 按钮                      |
| `tools-http-config-area`               | `actions/index.vue`（创建弹窗内）             | HTTP 配置区域                      |
| `tools-link-to-workflow`               | `actions/index.vue`（创建弹窗内）             | Link to Workflow 区域              |
| `teams-iframe-container`               | `authorityManagement/teams.vue`               | 包裹 iframe 的外层 div             |

---

## 待确认问题

1. ~~**Checklist Tasks 区域层级**~~ ✅ **已确认**：Tasks 通过 "View Tasks" 弹窗单独管理，与创建弹窗分离。Step 5–6 改为独立的第三个 TourGuide 实例（`checklist-tasks-tour`），在 Tasks 弹窗打开时挂载。
2. ~~**Dynamic Field 新建 UI**~~ ✅ **已确认**：点击 "Add New Field" 后为 `el-dialog` 弹窗。FAB 配置 `fabContainer` 指向该弹窗的 `.el-overlay-dialog`，与 Workflow Stage 弹窗 tour 方案一致。
3. ~~**Tools 创建弹窗**~~ ✅ **已确认**：HTTP 配置和 Link to Workflow 在同一弹窗，有 Tab 切换（从截图可见 Params / Headers / Body Tab）。Step 3 高亮 HTTP Configuration 整体区域，Step 4 通过 `waitForUserClick` 或 `lazyElement` 处理。
4. ~~**邮件模板引擎**~~ ✅ **已确认**：自研 Handlebars-like，支持 `{{variable}}`、`{{{unescaped}}}`、`{{#if}}`，模板为 Embedded HTML Resource，无外部依赖。详见"七、邮件通知优化"改造范围。
5. ~~**Questionnaire 编辑页 tour 首次进入检测**~~ ✅ **已确认**：`questionnaire-editor-tour` persistKey 首次进入自动启动，后续编辑已有问卷不再自动启动，行为符合预期。
