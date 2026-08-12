# Workflow Tour 交互引导规格

> 依据 Jira [OW-702 - Tooltip Guided Onboarding - Phase 2](https://jira.logisticsteam.com/browse/OW-702)（场景 1：Create Workflow）及原型 [guidepost-three.vercel.app](https://guidepost-three.vercel.app/) 更新。

## 设计原则

- **用户主导**：凡是需要切换状态（打开 dropdown、打开抽屉、跳转页面）的步骤，一律由用户主动点击触发，tour 只高亮目标元素并给出提示
- **不自动触发**：tour 不通过程序模拟点击来打开弹窗或切换视图
- **`waitForUserClick`**：需要用户点击按钮才能推进的步骤使用此机制，点击后 tour 隐藏遮罩，等待下一个状态就绪后再显示下一步
- **`beforeHighlight` 仅用于等待**：只用来等待元素出现（用户主动打开后的动画等待），不用来触发操作

---

## Segment 1：Workflow 列表页 Tour

**触发条件**：用户进入 `/onboard/workflow` 页面（列表视图）时自动启动

| Step | 锚点                                        | 标题                     | 内容                                                                                                          | 交互模式                                                                                                          |
| ---- | ------------------------------------------- | ------------------------ | ------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| 1    | `[data-tour="workflow-page-header"]`      | Welcome to Workflows     | This is where you design business processes. Each workflow is a reusable template made of ordered stages.     | 普通高亮，点 Next 推进                                                                                           |
| 2    | `[data-tour="workflow-new-btn"]`          | Create a new workflow    | Click **New Workflow** to start from a blank template. You'll give it a name and description, then build it up stage by stage. | 普通高亮（提示按钮位置），点 Next 推进                                                            |
| 3    | `[data-tour="workflow-list-row"]`         | Open an existing workflow | Each row is a workflow. Status and stage count are shown here, and the **Default** tag marks the template new cases use automatically. Click a row to configure its stages. | 普通高亮（提示行），点 Next 推进                                                                  |
| 4    | `[data-tour="workflow-row-more-btn"]`     | Row actions              | The actions menu on each row lets you edit, duplicate, or export a workflow — and open its visual **Workflow Chart** to set up conditional routing. | `waitForUserClick`：高亮行内 ⋯ 按钮，用户**必须点击**打开 dropdown；`afterUserClick` 等待 `[data-tour="workflow-row-chart-btn"]` 进入 DOM |
| 5    | `[data-tour="workflow-row-chart-btn"]`    | Workflow Chart           | Select **Workflow Chart** to open the visual diagram of the entire process flow.                              | `lazyElement: true`，`waitForUserClick`：用户**必须点击**菜单项跳转 Chart 页，tour 结束                        |

> **说明**：New Workflow / Add Stage 弹窗内的表单步骤不再进入引导主流程（与原型一致）。其中 Add/Edit Stage 弹窗提供独立的 "?" 重播 tour（见 Segment 4），由用户手动触发，不自动启动。

---

## Segment 2：Workflow Chart 页（condition-editor.vue）独立 Tour

**触发条件**：进入 `/onboard/workflow/:id/conditions` 页面时自动启动

| Step | 锚点                                                 | 标题                  | 内容                                                                                                                                                                        | 交互模式                                                                                                              |
| ---- | ---------------------------------------------------- | --------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| 6    | `[data-tour="workflow-canvas"]`                    | The workflow chart    | Workflow Chart shows your workflow as a flowchart — every stage in order, top to bottom. Branches to the right show conditions that reroute a case, skip stages, or trigger actions automatically. | 普通高亮，点 Next                                                                                    |
| 7    | `[data-tour="workflow-condition-node"]`            | Conditional branches  | A condition node hangs off the stage it evaluates. It shows how many rules and actions it holds, and a Go To / Skip tag when it reroutes the case. | `waitForUserClick`：高亮条件节点，用户**必须点击**打开 Condition 编辑器；`afterUserClick` 等抽屉动画完成 |
| 8    | `[data-tour="workflow-condition-rules"]`           | Build the condition rules | A rule compares a value to what's expected. You can reference any component from the current stage or any previous stage — pick the stage, the component, and the value to match. Add multiple rules to require them all. | `lazyElement: true`，普通高亮，点 Next                                                             |
| 9    | `[data-tour="workflow-condition-actions"]`         | Choose the actions    | When the rules match, actions fire. Pick from a variety of types — Go to Stage, Skip Stage, End Workflow, Send Notification, Update Field, Trigger Action, or Assign User — and chain several together. | `lazyElement: true`，普通高亮，点 Next                                                             |
| 10   | `[data-tour="workflow-condition-fallback"]`        | Set the fallback path | If none of the conditions match, the fallback decides where the case goes — continue to the next stage in order, or jump to a specific stage. Save when you're done.        | `lazyElement: true`，普通高亮，点 Done 结束                                                          |

> **适配说明**：原型中的 "Stage details & conditions" 步骤（点击 stage 打开详情面板）在真实应用中与 Condition 编辑器（el-drawer）互斥，无法同时展示，因此合并为 Step 7：直接点击条件节点打开编辑器，规则 / Actions / Fallback 均在编辑器内高亮。


## Segment 3：Workflow 详情页 Tour（补充）

**触发条件**：用户从列表页点击某行进入详情视图（`viewMode === 'detail'`）时自动启动（`workflow-detail-tour`），此后可通过右下角 "?" FAB 重播

| Step | 锚点 | 标题 | 内容 | 交互模式 |
| ---- | --- | --- | --- | --- |
| 11 | `[data-tour="workflow-detail-header"]` | Workflow details | This card shows the workflow name, status, and tags. Use ⋯ to edit, export, or open the Workflow Chart for conditional routing. | 普通高亮，点 Next |
| 12 | `[data-tour="workflow-add-stage-btn"]` | Add a stage | Click Add Stage to add a new stage to this workflow. Each stage collects its own components and has its own assignees — the form has its own guide ("?") when it opens. | 普通高亮，点 Next |
| 13 | `[data-tour="workflow-stages-area"]` | Manage stages | Stages run top to bottom. Use each row to edit, reorder, or delete a stage, and set up conditional routing from the Workflow Chart. | 普通高亮，点 Done 结束 |

> **适配说明**：详情页 ⋯ 菜单（Edit / Workflow Chart 跳转）已在列表页 tour Step 4-5 覆盖，此处不重复；Add/Edit Stage 弹窗内的表单步骤见 Segment 4。
## Segment 4：Add/Edit Stage 弹窗 Tour（补充）

**触发条件**：用户在 Workflow 详情页打开 Add/Edit Stage 弹窗后，点击右下角 "?" FAB 手动触发（`auto-start=false`，不自动启动）

| Step | 锚点 | 标题 | 内容 | 交互模式 |
| ---- | --- | --- | --- | --- |
| 14 | `[data-tour="stage-form-tabs"]` | Configure the stage | This is where you define what happens in this stage — who works on it, what information is collected, and how it fits into the workflow. | 普通高亮，点 Next |
| 15 | `[data-tour="stage-name-input"]` | Stage name | Give the stage a clear name so everyone knows what work happens here — e.g. Review or Approval. | 普通高亮，点 Next |
| 16 | `[data-tour="stage-assignee"]` | Default assignee | Pick who should work on this stage by default. Every case that reaches this stage is assigned to them unless it is changed manually. | 普通高亮，点 Next |
| 17 | `[data-tour="stage-required-toggle"]` | Required stage | Turn this on to make the stage mandatory — a case cannot move past it until the assignee completes all required components. | 普通高亮，点 Next |
| 18 | `[data-tour="stage-components-tab"]` | Stage components | The Components tab is where you attach the fields, checklists, questionnaires, and quick links the assignee must work through in this stage. | `waitForUserClick`：高亮 Components Tab，用户**必须点击**切换；`afterUserClick` 等待面板切换动画完成 |
| 19 | `[data-tour="stage-components-area"]` | Attach the right components | Search and select the fields, checklists, questionnaires, and quick links this stage needs. Only the components you add here are available to the assignee. | `lazyElement: true`，普通高亮，点 Next |
| 20 | `[data-tour="stage-save-btn"]` | Save the stage | When you're done, click Save to add the stage to the workflow. You can come back anytime to edit it. | 普通高亮，点 Done 结束 |

> **补充说明**：弹窗打开时 FAB 通过 `Teleport` 渲染到**该弹窗自己**的 `.el-overlay-dialog` 内部（`fab-container` 传入 getter：`document.querySelector(".stage-form-dialog")?.closest(".el-overlay-dialog")`，避免命中文档中第一个（可能是隐藏的）弹窗遮罩）。FAB 的 z-index（2000）低于 Element Plus 弹窗遮罩（`useZIndex` 从 2000 起递增），符合“? 的层级不应高于弹窗”的交互原则；同时低于 driver.js overlay（10000），不会遮挡引导层。
---

## 实现拆分

| Tour 实例     | persistKey                     | 挂载位置                                                | 步骤范围                              |
| ------------- | ------------------------------ | ------------------------------------------------------- | ------------------------------------- |
| 列表页 tour   | `workflow-list-tour`           | `index.vue`，`v-if="viewMode === 'list'"`           | Step 1-5                             |
| Chart 页 tour | `workflow-condition-tour-{id}` | `condition-editor.vue`                                 | Step 6-10                            |
| 详情页 tour   | `workflow-detail-tour`          | `index.vue`，`v-if="viewMode === 'detail'"`，与弹窗 tour 互斥         | Step 11-13                          |
| Stage 弹窗 tour | `workflow-stage-form-tour`   | `index.vue`，`v-if="dialogVisible.stageForm"`，`auto-start=false`（FAB 触发） | Step 14-20                          |

---

## 锚点清单

| 锚点                              | 文件                                              | 说明                              |
| --------------------------------- | ------------------------------------------------- | --------------------------------- |
| `workflow-page-header`          | `workflow/index.vue`                            | 列表页 PageHeader                  |
| `workflow-new-btn`              | `workflow/index.vue`                            | New Workflow 按钮                  |
| `workflow-list-row`             | `workflow/components/WorkflowListView.vue`      | 表格行名称区域（可点击进入详情）   |
| `workflow-row-more-btn`         | `workflow/components/WorkflowListView.vue`      | 行内 ⋯ 下拉触发器                  |
| `workflow-row-chart-btn`        | `workflow/components/WorkflowListView.vue`      | 行内 dropdown 的 Workflow Chart 项 |
| `workflow-detail-header`        | `workflow/index.vue`                            | 详情页 Workflow 卡片头部          |
| `workflow-add-stage-btn`        | `workflow/index.vue`                            | 详情页 Add Stage 按钮             |
| `workflow-stages-area`          | `workflow/index.vue`                            | 详情页 Stages 列表区域            |
| `workflow-canvas`               | `workflow/condition-editor.vue`                 | Chart 画布区域                     |
| `workflow-condition-node`       | `workflow-canvas/nodes/ConditionNode.vue`       | 画布上的条件节点                   |
| `workflow-condition-rules`      | `workflow/components/condition/ConditionCard.vue` | 条件卡片内 Condition Rules 区      |
| `workflow-condition-actions`    | `workflow/components/condition/ConditionCard.vue` | 条件卡片内 Actions 区              |
| `workflow-condition-fallback`   | `workflow/components/condition/StageConditionEditor.vue` | 抽屉内 Fallback Stage 区           |
| `stage-form-tabs`              | `workflow/components/StageForm.vue`                   | 弹窗内 PrototypeTabs 容器          |
| `stage-name-input`             | `workflow/components/StageForm.vue`                   | 弹窗内 Stage Name 输入框           |
| `stage-assignee`               | `workflow/components/StageForm.vue`                   | 弹窗内 Default Assignee 选择器     |
| `stage-required-toggle`        | `workflow/components/StageForm.vue`                   | 弹窗内 Required Stage 开关         |
| `stage-components-tab`         | `workflow/components/StageForm.vue`                   | 弹窗内 Components Tab 按钮（onMounted 注入） |
| `stage-components-area`        | `workflow/components/StageComponentsSelector.vue`     | Components 面板容器                |
| `stage-save-btn`               | `workflow/components/StageForm.vue`                   | 弹窗内 Save/Update 按钮            |

---

## 待解决问题

1. **无条件节点的工作流**：Step 7 依赖画布上存在条件节点。若 workflow 没有任何 Condition，Step 7 会被过滤掉；Step 8-10（`lazyElement`）依赖前置的 waitForUserClick 步骤（点击条件节点打开抽屉），该前置步骤不存在时它们也会在启动阶段被一并剔除——因此只显示 Step 6（The workflow chart，1 of 1，点 Done 结束），不会出现 "1 of 4" 之类的错误计数，也不会展示无高亮的 popover。后续如需要，可给 Chart 页 tour 增加"无条件时先引导创建条件"的分支。
2. **列表页默认视图**：锚点 `workflow-list-row` / `workflow-row-more-btn` 位于表格视图（`activeView === 'list'`）。若用户切到卡片视图，对应步骤会被过滤。
