# OW-621 Lina 追加反馈修复计划

## 背景

OW-621 原始 13 点需求已完成并上线（标记为 Ready for PO Review）。Lina Gao 于 2026-06-05 在 comment 中追加了 4 点反馈，其中 3 点是对已完成功能的细节修补，1 点是新增的 UX 优化。

---

## 修改项汇总

| #   | 来源         | 类型                 | 范围                           | 难度 |
| --- | ------------ | -------------------- | ------------------------------ | ---- |
| 1   | 针对原第5点  | 前端 CSS/布局        | detail.vue                     | 简单 |
| 2   | 针对原第7点  | 前端（复用已有模式） | DynamicFieldRenderer/index.vue | 简单 |
| 3   | 针对原第13点 | 后端 Bug             | ChecklistService.cs            | 简单 |
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

## 修改项 3：Checklist Notes 计数在无 Completion 记录时为 0 的 Bug

### 问题描述

用户通过 TaskDetailsDialog 点击 Add Note 后，note 立即通过 `POST /ow/checklist-task-notes/v1` 保存到数据库（`ff_checklist_task_note` 表）。但外面 Checklist 列表的 notes 计数 badge 始终显示 0。**即使刷新整个页面也是 0。**

### Root Cause

前端 `loadCheckListData` 拿 notes count 有两条路径：

1. `getCheckListIsCompleted`（→ `ChecklistTaskCompletionService`）— 只在用户点过 Save Changes 后才有 completion 记录，此路径能正确返回 `notesCount`
2. `getCheckListIds`（→ `ChecklistService.GetByIdsAsync` → `FillAssignmentsAndTasksAsync`）— 这是 fallback 路径，但此方法**没有调用 `FillFilesAndNotesCountAsync`**，导致 `ChecklistTaskOutputDto.NotesCount` 始终为 0（AutoMapper 配置为 Ignore）

所以如果用户从来没点过 Save Changes（即无 completion 记录），哪怕 note 已经存到数据库了，前端拿到的 count 永远是 0。

### 改法

在 `ChecklistService.FillAssignmentsAndTasksAsync` 方法中，对已 map 的 `taskDtos` 调用 `FillFilesAndNotesCountAsync`（或内联同等逻辑），确保通过 `getCheckListIds` 路径返回的 task DTO 也包含正确的 `NotesCount` 和 `FilesCount`。

### 涉及文件

- `packages/flowFlex-backend/Application/Services/OW/ChecklistService.cs` — `FillAssignmentsAndTasksAsync` 方法

### 注意事项

- `FillFilesAndNotesCountAsync` 方法定义在 `ChecklistTaskService` 中且是 private。有两种方式：
  - **方案A**：将该逻辑内联到 `ChecklistService.FillAssignmentsAndTasksAsync` 中（直接注入 `IChecklistTaskNoteRepository` 做 count）
  - **方案B**：在 `IChecklistTaskService` 接口上暴露一个 public 方法来填充 count，然后在 `ChecklistService` 中调用
  - **推荐方案A**，因为逻辑简单（就是按 taskId batch count），且避免循环依赖风险

### 验证 SQL（staging 已确认可复现）

```sql
-- 确认 note 存在
SELECT COUNT(*) FROM ff_checklist_task_note
WHERE task_id = 2062783930465128448
  AND onboarding_id = 2063831302691491841
  AND is_deleted = false AND note_type = 'General';
-- 返回 2

-- 确认无 completion 记录
SELECT * FROM ff_checklist_task_completion
WHERE task_id = 2062783930465128448
  AND onboarding_id = 2063831302691491841;
-- 返回 0 行
```

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

1. **修改项 3**（后端 bug fix）→ 优先级最高，因为影响数据准确性
2. **修改项 4**（前端 UX）→ 简单且用户体感明显
3. **修改项 1**（前端布局）→ 纯 CSS
4. **修改项 2**（前端文件 metadata）→ 复用已有模式，最后做

总预计工时：AI 辅助约 2-3 小时。
