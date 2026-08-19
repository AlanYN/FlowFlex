/**
 * useGanttTourSteps
 *
 * Gantt Chart 弹窗的 Tour 引导步骤定义。
 *
 * 步骤说明（来源：产品规格图表）：
 *   Step 1 — Case Summary         案例进度与当前阶段状态
 *   Step 2 — Toolbar / Filters    Day/Week/Month 视图切换 + Status/Assignee 筛选
 *   Step 3 — Gantt Chart Body     条形图颜色含义 + Planned vs Projected 双时间线
 *   Step 4 — Today 线             今日标记线
 *   Step 5 — Legend               颜色与术语说明
 *
 * persistKey: "gantt-modal-tour"
 * 挂载位置：GanttModal.vue（el-dialog 打开后）
 * auto-start: true（首次打开时自动触发）
 */

import { TourStep } from '#/config';

export const ganttModalTourSteps: TourStep[] = [
	// ── Step 1: Case Summary ──────────────────────────────────────────────
	{
		element: '[data-tour="gantt-case-summary"]',
		title: 'Case Summary',
		description:
			'This shows case progress and current stage status — including total stages completed, overall completion percentage, planned start, and estimated end date.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 2: Toolbar & Filters ────────────────────────────────────────
	{
		element: '[data-tour="gantt-toolbar"]',
		title: 'View Controls & Filters',
		description:
			'Switch between <strong>Day / Week / Month</strong> views to zoom in or out. Use <strong>All statuses</strong> and <strong>All assignees</strong> filters to focus on specific stages. Use the navigation arrows to scroll the timeline.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 3: Gantt Chart Body ──────────────────────────────────────────
	{
		element: '[data-tour="gantt-body"]',
		title: 'Stage Timeline',
		description:
			'Each row is a workflow stage. The <strong>dashed bar</strong> shows the original plan (Planned). The <strong>solid colored bar</strong> shows the current forecast (Projected). Bar colors indicate status: <span style="color:#52c41a;font-weight:600">green</span> = done, <span style="color:#5b8cff;font-weight:600">blue</span> = in progress, <span style="color:#d9d9d9;font-weight:600">gray</span> = not started, <span style="color:#ff4d4f;font-weight:600">red</span> = overdue. Hover a bar to see details.',
		side: 'right',
		align: 'start',
	},

	// ── Step 4: Today 线 ─────────────────────────────────────────────────
	// beforeHighlight 在 GanttModal 里动态注入（调用 scrollToToday 后再定位）
	// lazyElement: true 避免 Tour 启动时元素不在视口内导致步骤被跳过
	{
		element: '.g-grid-current-time',
		lazyElement: true,
		title: "Today's Date",
		description:
			"This vertical red line marks today's date on the timeline — so you can instantly see which stages are ahead of or behind schedule. Click <strong>Today</strong> in the toolbar to bring it into view at any time.",
		side: 'right',
		align: 'start',
	},

	// ── Step 5: Legend ───────────────────────────────────────────────────
	{
		element: '[data-tour="gantt-legend"]',
		title: 'Legend',
		description:
			'Refer to this legend for color and term explanations. <strong>Planned</strong> bars show the original schedule; <strong>Projected</strong> bars show the current forecast based on actual progress.',
		side: 'top',
		align: 'start',
	},
];
