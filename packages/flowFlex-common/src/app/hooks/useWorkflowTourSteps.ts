/**
 * useWorkflowTourSteps
 *
 * Workflow Admin 配置端引导（OW-702 Tooltip Guided Onboarding - Phase 2，场景 1：Create Workflow）。
 *
 * 流程对齐 Jira OW-702 的原型设计（guidepost-three.vercel.app）：
 *
 *  Tour 1 — 列表页 (persistKey: "workflow-list-tour")
 *    Step 1  workflow-page-header      普通高亮，Next 推进
 *    Step 2  workflow-new-btn          waitForUserClick → 用户点击打开 New Workflow 弹窗
 *    Step 3  workflow-form-name-input  lazyElement，弹窗内 Workflow Name 输入框
 *    Step 4  workflow-form-submit-btn  lazyElement，弹窗内 Create Workflow 按钮
 *    Step 5  workflow-list-row         lazyElement，已有 workflow 行（有数据时显示）
 *    Step 6  workflow-row-more-btn     lazyElement + waitForUserClick → 用户点击 ⋯ 打开行内 dropdown
 *    Step 7  workflow-row-chart-btn    lazyElement，waitForUserClick → 用户点击跳转 Chart 页
 *
 *  Tour 2 — Chart 页 (persistKey: "workflow-condition-tour-{id}")
 *    Step 1  workflow-canvas                   普通高亮，Next 推进
 *    Step 2  workflow-condition-node           waitForUserClick → 用户点击条件节点打开 Condition 编辑器
 *    Step 3  workflow-condition-rules          lazyElement，规则区高亮，Next 推进
 *    Step 4  workflow-condition-actions        lazyElement，Actions 区高亮，Next 推进
 *    Step 5  workflow-condition-fallback       lazyElement，Fallback 区高亮，Done 结束
 *
 * 交互原则（与 workflow-tour-interaction-spec.md 一致）：
 *   - 用户主导：所有需要切换状态（打开 dropdown / 抽屉 / 跳转页面）的步骤都由用户主动点击触发
 *   - waitForUserClick 步骤只高亮目标元素并等待用户点击，不模拟点击
 *   - lazyElement 步骤的元素在用户点击后才进入 DOM（弹窗内容 / dropdown 菜单项 / 抽屉内容）
 */

import { TourStep } from '#/config';

// ─── Helpers ──────────────────────────────────────────────────────────────────

/** Wait for an element to appear in the DOM (up to `timeout` ms). */
function waitForElement(selector: string, timeout = 2500): Promise<void> {
	return new Promise((resolve) => {
		const deadline = Date.now() + timeout;
		const check = () => {
			if (document.querySelector(selector)) {
				resolve();
			} else if (Date.now() < deadline) {
				requestAnimationFrame(check);
			} else {
				resolve();
			}
		};
		check();
	});
}

// ─── Tour 1: 列表页 ───────────────────────────────────────────────────────────

/**
 * 列表页 tour — 挂载在 index.vue 的 v-if="viewMode === 'list' && !loading.workflows" 区域
 * persistKey: "workflow-list-tour"
 *
 * 流程：页面总览 → New Workflow 按钮（waitForUserClick）→
 *        弹窗内 Name 输入框 → Create 按钮 →
 *        已有行（有数据时）→ ⋯ 按钮 → Workflow Chart 菜单项
 */
export const workflowListTourSteps: TourStep[] = [
	// ── Step 1: 页面总览 ───────────────────────────────────────────────────
	{
		element: '[data-tour="workflow-page-header"]',
		title: 'Welcome to Workflows',
		description:
			'This is where you design business processes. Each workflow is a reusable template made of ordered stages — every case runs through these stages from start to finish.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 2: New Workflow 按钮 — 用户必须点击打开弹窗 ─────────────────
	{
		element: '[data-tour="workflow-new-btn"]',
		title: 'Create a new workflow',
		description:
			'Click <strong>New Workflow</strong> to create a workflow. A dialog will open where you can set the name and configure permissions.',
		side: 'bottom',
		align: 'end',
		waitForUserClick: true,
		afterUserClick: async () => {
			await waitForElement('[data-tour="workflow-form-name-input"]', 2000);
		},
	},

	// ── Step 3: 弹窗内 — Workflow Name (lazyElement) ─────────────────────
	{
		element: '[data-tour="workflow-form-name-input"]',
		title: 'Workflow Name',
		description:
			'Give the workflow a clear name that reflects the business process it manages — e.g. <strong>Customer Onboarding</strong> or <strong>Contract Renewal</strong>.',
		side: 'bottom',
		align: 'start',
		lazyElement: true,
	},

	// ── Step 4: 弹窗内 — Create Workflow 按钮 (lazyElement) ──────────────
	{
		element: '[data-tour="workflow-form-submit-btn"]',
		title: 'Create Workflow',
		description:
			'Click <strong>Create Workflow</strong> to save. Once created, you can add Stages to define each step of the process.',
		side: 'top',
		align: 'end',
		lazyElement: true,
	},

	// ── Step 5: 弹窗内 — Cancel 按钮（lazyElement + waitForUserClick）────
	// 仅当有列表数据时后续步骤（Step 6-8）才存在，引擎会自动决定在用户关闭弹窗后
	// 继续展示列表行步骤（有数据）或直接结束（无数据）。
	{
		element: '[data-tour="workflow-form-cancel-btn"]',
		title: 'Close the dialog',
		description:
			'Click <strong>Cancel</strong> to close this dialog and return to the workflow list — where you can explore existing workflows.',
		side: 'top',
		align: 'start',
		lazyElement: true,
		waitForUserClick: true,
		afterUserClick: async () => {
			// 等弹窗关闭动画完成，确保列表行锚点可见
			await new Promise<void>((resolve) => {
				const deadline = Date.now() + 1500;
				const check = () => {
					const dlg = document.querySelector<HTMLElement>(
						'.workflow-form-dialog .el-dialog'
					);
					if (!dlg || dlg.getBoundingClientRect().height === 0) {
						resolve();
					} else if (Date.now() < deadline) {
						requestAnimationFrame(check);
					} else {
						resolve();
					}
				};
				setTimeout(check, 100);
			});
		},
	},

	// ── Step 6: 已有 workflow 行（有数据时出现，无数据时自动跳过）──────────
	{
		element: '[data-tour="workflow-list-row"]',
		title: 'Open an existing workflow',
		description:
			'Each row is a workflow. Status and stage count are shown here, and the <strong>Default</strong> tag marks the template new cases use automatically. Click a row to configure its stages.',
		side: 'top',
		align: 'start',
		lazyElement: true,
	},

	// ── Step 7: 行内 ⋯ 按钮 — 用户必须点击打开 dropdown ─────────────────
	{
		element: '[data-tour="workflow-row-more-btn"]',
		title: 'Row actions',
		description:
			'The actions menu on each row lets you edit, duplicate, or export a workflow — and open its visual <strong>Workflow Chart</strong> to set up conditional routing. Click <strong>⋯</strong> to open the menu.',
		side: 'left',
		align: 'start',
		lazyElement: true,
		waitForUserClick: true,
		afterUserClick: async () => {
			// 等待 dropdown 打开，直到 Workflow Chart 菜单项出现在 DOM 中
			await waitForElement('[data-tour="workflow-row-chart-btn"]', 1500);
		},
	},

	// ── Step 8: Workflow Chart 菜单项 — 用户必须点击跳转 ─────────────────
	{
		element: '[data-tour="workflow-row-chart-btn"]',
		title: 'Workflow Chart',
		description:
			'Select <strong>Workflow Chart</strong> to open the visual diagram of the entire process flow, where you can set stage transition conditions.',
		side: 'left',
		align: 'start',
		lazyElement: true,
		waitForUserClick: true,
		// 用户点击后页面跳转到 /onboard/workflow/:id/conditions，tour 自然结束
	},
];

// ─── Tour 2: Workflow Chart 页 ────────────────────────────────────────────────

/**
 * Chart 页 tour — 挂载在 condition-editor.vue
 * persistKey: "workflow-condition-tour-{workflowId}"
 *
 * 对应原型 Step 5-10：画布总览 → 条件分支节点 → 条件规则 → Actions → Fallback。
 * 真实应用中条件编辑器（el-drawer）只能通过点击画布上的条件节点打开，
 * 因此用 waitForUserClick 让用户主动点击条件节点，随后 lazyElement 高亮抽屉内步骤。
 */
export const workflowConditionTourSteps: TourStep[] = [
	// ── Step 1: Canvas 总览 ────────────────────────────────────────────────
	{
		element: '[data-tour="workflow-canvas"]',
		title: 'The workflow chart',
		description:
			'Workflow Chart shows your workflow as a flowchart — every stage in order, top to bottom. Branches to the right show conditions that reroute a case, skip stages, or trigger actions automatically.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 2: 条件节点 — 用户必须点击打开 Condition 编辑器 ────────────────
	{
		element: '[data-tour="workflow-condition-node"]',
		title: 'Conditional branches',
		description:
			'A condition node hangs off the stage it evaluates. It shows how many rules and actions it holds, and a <strong>Go To</strong> / <strong>Skip</strong> tag when it reroutes the case. Click the condition node to open the editor.',
		side: 'right',
		align: 'start',
		waitForUserClick: true,
		afterUserClick: async () => {
			// 等 Condition Rules 区域出现即可推进，不等 drawer 动画完成
			await waitForElement('[data-tour="workflow-condition-rules"]', 2000);
		},
	},

	// ── Step 3: 抽屉内 — Condition Rules ───────────────────────────────────
	{
		element: '[data-tour="workflow-condition-rules"]',
		title: 'Build the condition rules',
		description:
			"A rule compares a value to what's expected. You can reference any component from the current stage or any previous stage — pick the stage, the component, and the value to match. Add multiple rules to require them all.",
		side: 'bottom',
		align: 'start',
		lazyElement: true,
	},

	// ── Step 4: 抽屉内 — Actions ───────────────────────────────────────────
	{
		element: '[data-tour="workflow-condition-actions"]',
		title: 'Choose the actions',
		description:
			'When the rules match, actions fire. Pick from a variety of types — <strong>Go to Stage</strong>, <strong>Skip Stage</strong>, <strong>End Workflow</strong>, <strong>Send Notification</strong>, <strong>Update Field</strong>, <strong>Trigger Action</strong>, or <strong>Assign User</strong> — and chain several together.',
		side: 'bottom',
		align: 'start',
		lazyElement: true,
	},

	// ── Step 5: 抽屉内 — Fallback ──────────────────────────────────────────
	{
		element: '[data-tour="workflow-condition-fallback"]',
		title: 'Set the fallback path',
		description:
			"If none of the conditions match, the fallback decides where the case goes — continue to the next stage in order, or jump to a specific stage. <strong>Save</strong> when you're done.",
		side: 'top',
		align: 'start',
		lazyElement: true,
	},
];
// ─── Tour 2b: Workflow Chart 页（无 condition 分支） ─────────────────────────

/**
 * 无 condition 时的引导 — 挂载在 condition-editor.vue
 * persistKey: "workflow-condition-tour-{workflowId}"（与有 condition 版共用同一 key，
 * 两个分支只会执行其中一个，完成后记录相同的 key 即可）
 *
 * 引导用户点击 stage 节点打开 NodePanel，再点击 Add Condition 进入创建流程。
 */
export const workflowNoConditionTourSteps: TourStep[] = [
	// ── Step 1: Canvas 总览（与有 condition 版相同） ───────────────────────
	{
		element: '[data-tour="workflow-canvas"]',
		title: 'The workflow chart',
		description:
			"Workflow Chart shows your workflow as a flowchart — every stage in order, top to bottom. Right now there are no conditions yet. Let's add one so you can see how conditional routing works.",
		side: 'bottom',
		align: 'start',
	},

	// ── Step 2: Stage 节点 — 用户必须点击打开 NodePanel ─────────────────────
	{
		element: '[data-tour="workflow-stage-node"]',
		title: 'Click a stage to get started',
		description:
			'Click any stage node to open its details panel. From there you can add a condition that controls what happens when a case completes this stage.',
		side: 'right',
		align: 'start',
		waitForUserClick: true,
		afterUserClick: async () => {
			// 等 NodePanel 内的 Add Condition 按钮出现即可推进，不等 drawer 动画完成
			await waitForElement('[data-tour="workflow-add-condition-btn"]', 2000);
		},
	},

	// ── Step 3: NodePanel 内 Add Condition — 用户必须点击打开编辑器抽屉 ─────
	{
		element: '[data-tour="workflow-add-condition-btn"]',
		title: 'Open the condition editor',
		description:
			'Click <strong>Add Condition</strong> to open the condition editor for this stage.',
		side: 'left',
		align: 'start',
		lazyElement: true,
		waitForUserClick: true,
		afterUserClick: async () => {
			// 等编辑器内 + Add Condition 按钮出现即可推进，不等 drawer 动画完成
			await waitForElement('[data-tour="workflow-editor-add-condition-btn"]', 2000);
		},
	},

	// ── Step 4: 编辑器内 + Add Condition — 用户必须点击展开 ConditionCard ──
	{
		element: '[data-tour="workflow-editor-add-condition-btn"]',
		title: 'Create your first condition',
		description:
			"Click <strong>+ Add Condition</strong> to create a condition. You'll then define the rules that trigger it and the actions it performs.",
		side: 'left',
		align: 'start',
		lazyElement: true,
		waitForUserClick: true,
		afterUserClick: async () => {
			// 等 ConditionCard 展开后 condition name 字段出现即可推进
			await waitForElement('[data-tour="workflow-condition-name"]', 2000);
		},
	},

	// ── Step 5: Condition Name ────────────────────────────────────────────
	{
		element: '[data-tour="workflow-condition-name"]',
		title: 'Name your condition',
		description:
			"Give this condition a clear name so it's easy to identify — e.g. <strong>Approved</strong> or <strong>Score above 80</strong>. A good name makes the Workflow Chart much easier to read.",
		side: 'left',
		align: 'start',
		lazyElement: true,
	},

	// ── Step 6: Condition Rules ───────────────────────────────────────────
	{
		element: '[data-tour="workflow-condition-rules"]',
		title: 'Build the condition rules',
		description:
			"A rule compares a value to what's expected. Pick a stage, a component, and the value to match. Add multiple rules to require them all (AND) or any one of them (OR).",
		side: 'left',
		align: 'start',
		lazyElement: true,
	},

	// ── Step 7: Actions ───────────────────────────────────────────────────
	{
		element: '[data-tour="workflow-condition-actions"]',
		title: 'Choose the actions',
		description:
			'When the rules match, actions fire. Pick from <strong>Go to Stage</strong>, <strong>Skip Stage</strong>, <strong>End Workflow</strong>, <strong>Send Notification</strong>, <strong>Update Field</strong>, <strong>Trigger Action</strong>, or <strong>Assign User</strong> — and chain several together.',
		side: 'left',
		align: 'start',
		lazyElement: true,
	},

	// ── Step 8: Fallback ──────────────────────────────────────────────────
	{
		element: '[data-tour="workflow-condition-fallback"]',
		title: 'Set the fallback path',
		description:
			"If none of the conditions match, the fallback decides where the case goes — continue to the next stage in order, or jump to a specific stage. Click <strong>Save</strong> when you're done.",
		side: 'top',
		align: 'start',
		lazyElement: true,
	},
];

// ─── Tour 3: Add/Edit Stage 弹窗 ─────────────────────────────────────────────

/**
 * Add/Edit Stage 弹窗 tour — 挂载在 workflow/index.vue 的 stage-form-dialog
 * persistKey: "workflow-stage-form-tour"
 *
 * 补充流程：用户在详情页打开 Add/Edit Stage 弹窗后，通过右下角 "?" FAB 手动触发。
 * 覆盖表单关键字段：基本信息 Tab → 名称 → 默认处理人 → 必填 → Components Tab → 保存。
 * 遵循用户主导原则：切换 Components Tab 由用户主动点击（waitForUserClick），tour 不自动切换。
 */
export const workflowStageFormTourSteps: TourStep[] = [
	// ── Step 1: 表单总览 ───────────────────────────────────────────────────
	{
		element: '[data-tour="stage-form-tabs"]',
		title: 'Configure the stage',
		description:
			'This is where you define what happens in this stage — who works on it, what information is collected, and how it fits into the workflow.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 2: 阶段名称 ───────────────────────────────────────────────────
	{
		element: '[data-tour="stage-name-input"]',
		title: 'Stage name',
		description:
			'Give the stage a clear name so everyone knows what work happens here — e.g. <strong>Review</strong> or <strong>Approval</strong>.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 3: 默认处理人 ─────────────────────────────────────────────────
	{
		element: '[data-tour="stage-assignee"]',
		title: 'Default assignee',
		description:
			'Pick who should work on this stage by default. Every case that reaches this stage is assigned to them unless it is changed manually.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 4: 必填阶段 ───────────────────────────────────────────────────
	{
		element: '[data-tour="stage-required-toggle"]',
		title: 'Required stage',
		description:
			'Turn this on to make the stage mandatory — a case cannot move past it until the assignee completes all required components.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 5: Components Tab — 用户必须点击切换 ───────────────────────────
	{
		element: '[data-tour="stage-components-tab"]',
		title: 'Stage components',
		description:
			'The <strong>Components</strong> tab is where you attach the fields, checklists, questionnaires, and quick links the assignee must work through in this stage.',
		side: 'bottom',
		align: 'start',
		waitForUserClick: true,
		afterUserClick: async () => {
			// 等待用户点击后 Components 面板切换到可见状态
			await new Promise<void>((resolve) => {
				const deadline = Date.now() + 2500;
				const check = () => {
					const el = document.querySelector<HTMLElement>(
						'[data-tour="stage-components-area"]'
					);
					if (el && el.getClientRects().length > 0) {
						setTimeout(resolve, 200);
					} else if (Date.now() < deadline) {
						requestAnimationFrame(check);
					} else {
						resolve();
					}
				};
				check();
			});
		},
	},

	// ── Step 6: Components 面板 ────────────────────────────────────────────
	{
		element: '[data-tour="stage-components-area"]',
		title: 'Attach the right components',
		description:
			'Search and select the fields, checklists, questionnaires, and quick links this stage needs. Only the components you add here are available to the assignee.',
		side: 'bottom',
		align: 'start',
		lazyElement: true,
	},

	// ── Step 7: 保存 ───────────────────────────────────────────────────────
	{
		element: '[data-tour="stage-save-btn"]',
		title: 'Save the stage',
		description:
			"When you're done, click <strong>Save</strong> to add the stage to the workflow. You can come back anytime to edit it.",
		side: 'top',
		align: 'start',
	},
];
// ─── Tour 4: Workflow 详情页 ───────────────────────────────────────────────

/**
 * 详情页 tour — 挂载在 workflow/index.vue 的详情视图
 * persistKey: "workflow-detail-tour"
 *
 * 补充流程：用户在列表页点击某行进入详情后自动引导（或通过右下角 "?" 重播）。
 * 覆盖详情页核心操作：Workflow 卡片总览 → Add Stage 按钮 → Stages 列表区域。
 * ⋯ 菜单与 Workflow Chart 跳转已在列表页 tour（Step 6-7）覆盖，此处不重复。
 */
export const workflowDetailTourSteps: TourStep[] = [
	// ── Step 1: Workflow 卡片总览 ────────────────────────────────────────
	{
		element: '[data-tour="workflow-detail-header"]',
		title: 'Workflow details',
		description:
			'This card shows the workflow name, status, and tags. Use <strong>⋯</strong> to edit, export, or open the <strong>Workflow Chart</strong> for conditional routing.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 2: Add Stage 按钮 ───────────────────────────────────────────
	{
		element: '[data-tour="workflow-add-stage-btn"]',
		title: 'Add a stage',
		description:
			'Click <strong>Add Stage</strong> to add a new stage to this workflow. Each stage collects its own components and has its own assignees — the form has its own guide ("?") when it opens.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 3: Stages 列表区域 ──────────────────────────────────────────
	{
		element: '[data-tour="workflow-stages-area"]',
		title: 'Manage stages',
		description:
			'Stages run top to bottom. Use each row to edit, reorder, or delete a stage, and set up conditional routing from the <strong>Workflow Chart</strong>.',
		side: 'top',
		align: 'start',
	},
];
