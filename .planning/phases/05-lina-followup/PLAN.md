# OW-621 Lina 追加反馈修复计划

## 背景

OW-621 原始 13 点需求已完成并上线（标记为 Ready for PO Review）。Lina Gao 于 2026-06-05 在 comment 中追加了 4 点反馈，其中 3 点是对已完成功能的细节修补，1 点是新增的 UX 优化。

---

## 修改项汇总

| #   | 来源         | 类型                 | 范围                           | 难度 |
| --- | ------------ | -------------------- | ------------------------------ | ---- |
| 1   | 针对原第5点  | 前端 CSS/布局        | detail.vue                     | 简单 |
| 2   | 针对原第7点  | 前端（复用已有模式） | DynamicFieldRenderer/index.vue | 简单 |
| 3   | 针对原第13点 | 前端逻辑             | TaskDetailsDialog.vue          | 简单 |
| 4   | 新增补充     | 前端逻辑             | detail.vue                     | 简单 |

---

## 修改项 1：Stage Detail 收缩后表单区域高度自适应扩展

### 问题描述

左侧顶部 `EditableStageHeader`（显示 Stage 名称、Reassign/Add Co-assignee 按钮的区域）被折叠后，下方的表单滚动区域（`el-scrollbar`）没有扩展来占用释放的垂直空间，导致表单可视区域不变，留下空白。

### 改法

确保左侧列容器使用 flex 纵向布局：

- `EditableStageHeader` 高度为 auto（折叠时缩小）
- 下方 `el-scrollbar` 使用 `flex: 1` 或等效方式自动填充剩余高度

### 涉及文件

- `packages/flowFlex-common/src/app/views/onboard/onboardingList/detail.vue` — 左侧列的 CSS 布局调整

### 不涉及

- 右侧面板宽度不变
- 后端无改动

---

## 修改项 2：Field 类型文件上传显示 Uploaded By + Date

### 问题描述

原第7点只做了 Questionnaire 文件类型的上传人/时间展示。当 Stage 使用 Field（Static Form）且字段配置为 File 类型时，上传的文件列表只显示文件名和大小，缺少上传人和上传时间。

### 改法（复用 Questionnaire 已有模式）

1. **上传时注入 metadata**：在 `DynamicFieldRenderer` 的文件上传成功回调里，将 API 返回的 `uploadedBy` 和 `uploadTime` 注入到存储的文件对象中（JSON 结构变为 `{id, fileName, fileSize, uploadedBy, uploadDate}`）
2. **渲染时显示**：在已上传文件列表中，文件名下方添加条件渲染的 metadata 行：`Uploaded by {name}, MM/DD/YYYY HH:mm:ss`
3. **优雅降级**：用 `v-if="file.uploadedBy || file.uploadDate"` 确保历史数据不报错

### 涉及文件

- `packages/flowFlex-common/src/app/components/dynamicFieldRenderer/index.vue` — 上传回调 + 文件列表渲染

### 不涉及

- 后端无改动（上传接口已经返回 `uploadedBy`，在修改项原第7点 Task1 时已加）
- Questionnaire 文件类型不需要改（已经做了）

### 关键上下文

- 文件上传 API：同一个 `uploadQuestionFile` 接口
- 存储方式：文件信息以 JSON 数组存在 `fieldValueJson` 字段（`saveQuestionnaireStatic` API）
- `timeZoneConvert` 和 `projectTenMinutesSsecondsDate` 需要 import（如果没有的话）

---

## 修改项 3：Checklist Notes 计数在 Add Note 后不实时更新

### 问题描述

用户通过 TaskDetailsDialog 点击 Add Note 后，note 立即通过 `POST /ow/checklist-task-notes/v1` 保存到数据库（`ff_checklist_task_note` 表），API 返回成功。但外面 Checklist 列表的 notes 计数 badge 没有更新，仍然显示 0。

### Root Cause

前端 `loadCheckListData` 拿 notes count 依赖 `getCheckListIsCompleted` API（→ `ChecklistTaskCompletionService`）返回的 completion 记录中的 `notesCount` 字段。**但 completion 记录只在用户点 TaskDetailsDialog 底部 "Save Changes" 按钮时才创建（调用 `saveCheckListTask` API）。** 在此之前 completion API 返回空数组，没有载体携带 `notesCount`。

另一条路径 `getCheckListIds`（→ `ChecklistService.GetByIdsAsync`）返回的是 checklist 定义数据，不含 onboarding 上下文，无法正确统计当前 case 的 notes（checklist 是跨 case 共享的，同一 task ID 在不同 case 下有不同的 notes）。

**核心问题：completion 记录是 notesCount 的唯一正确载体，但它的创建时机太晚了。**

### 改法

在 `TaskDetailsDialog.vue` 中，`addNote()` 成功后，**静默调用 `saveCheckListTask`**（使用当前 task 的现有状态），确保 completion 记录存在。然后 emit `refreshChecklist` 让父组件重新 fetch checklist 数据（此时 completion API 能返回正确的 `notesCount`）。

具体步骤：

1. `addNote()` API 返回成功后
2. 静默调用 `saveCheckListTask({ checklistId, isCompleted: props.task.isCompleted, onboardingId, stageId, taskId, filesJson: 当前值 })`
   - 不关闭 dialog
   - 不弹额外 toast（note 的 toast 已经弹过了）
   - 不 emit `update:task`（不触发关闭流程）
   - `isCompleted` 用的是 `props.task.isCompleted`（当前状态原样传递，不会改变完成状态）
3. 静默 save 完成后，emit `refreshChecklist`（或等价事件）让父组件 refetch

同理，`deleteNote()` 成功后也做同样的静默 save + refresh。

### 涉及文件

- `packages/flowFlex-common/src/app/views/onboard/onboardingList/components/TaskDetailsDialog.vue` — `addNote` 和 `deleteNote` 方法

### 不涉及

- 后端无改动
- `CheckList.vue` 不需要改（已有 `refreshChecklist` 事件监听）
- `detail.vue` 不需要改（已有 `@refresh-checklist="loadCheckListData"` 绑定）

### 优势

- completion 记录一旦创建，后续所有路径（刷新页面、completion API）都能拿到正确 count
- 不需要前端维护 count 增减逻辑，数据来源单一可靠
- `isCompleted` 原样传递，不会意外改变 task 完成状态
- 代价仅是多一次 API 调用（首次 add note 时创建记录，后续更新已有记录）

---

## 修改项 4：Save Stage 后保留当前选中的 Stage

### 问题描述

用户选中非第一个 Stage（例如 Stage 3）后点击 Save，保存完成后页面自动跳到"第一个未完成的 Stage"（可能是 Stage 1 或 Stage 2），而不是停留在用户刚保存的 Stage 3。

### Root Cause

`saveQuestionnaireAndField` 成功后调用 `loadOnboardingDetail()`，该方法内部 `processOnboardingData` 会根据 `currentStageId` 或"第一个未完成 stage"的逻辑重新设定 `activeStage`，覆盖了用户当前的选择。

### 改法

在 `saveQuestionnaireAndField` 函数中：

1. `loadOnboardingDetail()` 调用前保存当前 `activeStage` 的值
2. `loadOnboardingDetail()` 完成后，将 `activeStage` 强制恢复为保存前的值（前提是该 stage 仍存在于 stages 列表中）
3. 手动调用 `setActiveStage(savedStageId)` 来加载对应 stage 的数据

### 涉及文件

- `packages/flowFlex-common/src/app/views/onboard/onboardingList/detail.vue` — `saveQuestionnaireAndField` 函数

### 不涉及

- 后端无改动
- Complete Stage 操作不受影响（complete 后跳到下一个 stage 是合理的）

---

## 执行顺序建议

1. **修改项 3**（notes count fix）→ 优先级最高，因为影响数据准确性
2. **修改项 4**（前端 UX）→ 简单且用户体感明显
3. **修改项 1**（前端布局）→ 纯 CSS
4. **修改项 2**（前端文件 metadata）→ 复用已有模式，最后做

总预计工时：AI 辅助约 2-3 小时。
