/**
 * useAdminTourSteps
 *
 * Admin 配置端各功能页面 Tour 引导步骤定义。
 * 覆盖：Integration Settings、Dynamic Field、Checklist、Tools（Actions）、
 *        Questionnaire（列表页 + 编辑页）、Manage Teams（iframe 外框）。
 *
 * 规格来源：docs/admin-tour-interaction-spec.md
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

// ─── Integration Settings Tour（3 步）────────────────────────────────────────

/**
 * Integration Settings 主页 tour
 * persistKey: "integration-settings-tour"
 * 挂载位置：integration-settings/index.vue
 * auto-start: true
 *
 * 因页面是纯卡片网格，Quick Links 实际在 detail.vue 的 Tab 中，
 * 所以步骤聚焦在列表页可见的元素：页面总览 → Add New Integration 卡片 → 已有集成卡片。
 */
export const integrationSettingsTourSteps: TourStep[] = [
	// ── Step 1: 页面总览 ──────────────────────────────────────────────────
	{
		element: '[data-tour="integration-page-header"]',
		title: 'Integration Settings',
		description:
			'Configure integrations with external systems. Each integration card connects WFE to an external service for data exchange, quick links, and attachment sharing.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 2: Add New Integration 卡片 ────────────────────────────────
	{
		element: '[data-tour="integration-add-new-card"]',
		title: 'Add New Integration',
		description:
			'Click here to connect a new external system. You can configure authentication, field mappings, and Quick Links after creation.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 3: Quick Links tab 提示（在详情页） ─────────────────────────
	{
		element: '[data-tour="integration-card-list"]',
		title: 'Manage Integrations',
		description:
			'Each card shows an existing integration. Click a card to open its settings — including <strong>Quick Links</strong> (so Assignees can jump to external systems) and <strong>Attachment Sharing</strong> (file sync rules).',
		side: 'bottom',
		align: 'start',
	},
];

// ─── Integration 详情页 Tour（5 步）──────────────────────────────────────────

/**
 * Integration 详情页 tour
 * persistKey: "integration-detail-tour"
 * 挂载位置：integration-settings/detail.vue
 * auto-start: true（进入已保存的集成详情页自动启动）
 *
 * 流程：Connection Auth → Entity Type Mapping → Tabs 总览 →
 *   Inbound Tab 按钮（waitForUserClick）→ Inbound 内容区 →
 *   Outbound Tab 按钮（waitForUserClick）→ Outbound 内容区 →
 *   Actions Tab 按钮（waitForUserClick）→ Actions 内容区 →
 *   Quick Links Tab 按钮（waitForUserClick）→ Quick Links 内容区
 */
export const integrationDetailTourSteps: TourStep[] = [
	// ── Step 1: Connection Auth ───────────────────────────────────────────
	{
		element: '[data-tour="integration-connection-auth"]',
		title: 'Connection Auth',
		description:
			'This section shows the authentication credentials for this integration. Use <strong>Test Connection</strong> to verify the connection is active at any time.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 2: Entity Type Mapping ───────────────────────────────────────
	{
		element: '[data-tour="integration-entity-mapping"]',
		title: 'Entity Type Mapping',
		description:
			'Map WFE Workflows to entity types in the external system. This tells the integration which workflow handles which object type from the other side.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 3: Tabs 总览 ─────────────────────────────────────────────────
	{
		element: '[data-tour="integration-detail-tabs"]',
		title: 'Configuration Tabs',
		description:
			'Four tabs control different aspects of the integration. Click each Tab below to explore — <strong>Inbound</strong>, <strong>Outbound</strong>, <strong>Actions</strong>, and <strong>Quick Links</strong>.',
		side: 'top',
		align: 'start',
	},

	// ── Step 4: Inbound Tab 按钮 — 用户必须点击切换 ───────────────────────
	{
		element: '[data-tour="integration-tab-inbound"]',
		title: 'Inbound Settings',
		description:
			'Click <strong>Inbound Settings</strong> to configure how data flows from the external system into WFE — including field mappings and attachment sync.',
		side: 'bottom',
		align: 'start',
		waitForUserClick: true,
		afterUserClick: async () => {
			await waitForElement('[data-tour="integration-content-inbound"]', 1500);
		},
	},

	// ── Step 5: Inbound 内容区 ────────────────────────────────────────────
	{
		element: '[data-tour="integration-content-inbound"]',
		title: 'Inbound Field Mappings',
		description:
			'Define which fields from the external system map to WFE case fields. When a record is pushed in, WFE reads these mappings to populate the case correctly.',
		side: 'top',
		align: 'start',
		lazyElement: true,
	},

	// ── Step 6: Outbound Tab 按钮 — 用户必须点击切换 ─────────────────────
	{
		element: '[data-tour="integration-tab-outbound"]',
		title: 'Outbound Settings',
		description:
			'Click <strong>Outbound Settings</strong> to configure what data WFE sends back to the external system after a stage completes.',
		side: 'bottom',
		align: 'start',
		waitForUserClick: true,
		afterUserClick: async () => {
			await waitForElement('[data-tour="integration-content-outbound"]', 1500);
		},
	},

	// ── Step 7: Outbound 内容区 ───────────────────────────────────────────
	{
		element: '[data-tour="integration-content-outbound"]',
		title: 'Outbound Field Mappings',
		description:
			"Map WFE fields back to the external system's fields. You can also configure <strong>Attachment Sharing</strong> rules here to control how files sync between systems.",
		side: 'top',
		align: 'start',
		lazyElement: true,
	},

	// ── Step 8: Actions Tab 按钮 — 用户必须点击切换 ───────────────────────
	{
		element: '[data-tour="integration-tab-actions"]',
		title: 'Actions',
		description:
			'Click <strong>Actions</strong> to configure automated Tools that trigger when integration events occur.',
		side: 'bottom',
		align: 'start',
		waitForUserClick: true,
		afterUserClick: async () => {
			await waitForElement('[data-tour="integration-content-actions"]', 1500);
		},
	},

	// ── Step 9: Actions 内容区 ────────────────────────────────────────────
	{
		element: '[data-tour="integration-content-actions"]',
		title: 'Integration Actions',
		description:
			'Link existing Tools to this integration. When the integration receives or sends data, these Tools execute automatically — e.g. updating a CRM record or sending a notification.',
		side: 'top',
		align: 'start',
		lazyElement: true,
	},

	// ── Step 10: Quick Links Tab 按钮 — 用户必须点击切换 ─────────────────
	{
		element: '[data-tour="integration-tab-quicklinks"]',
		title: 'Quick Links',
		description:
			'Click <strong>Quick Links</strong> to configure shortcut links that appear inside Case Stages for Assignees.',
		side: 'bottom',
		align: 'start',
		waitForUserClick: true,
		afterUserClick: async () => {
			await waitForElement('[data-tour="integration-content-quicklinks"]', 1500);
		},
	},

	// ── Step 11: Quick Links 内容区 ───────────────────────────────────────
	{
		element: '[data-tour="integration-content-quicklinks"]',
		title: 'Quick Links Configuration',
		description:
			'Add links to records in the external system. Each link appears as a clickable button inside the Case Stage, letting Assignees jump directly to the related record — e.g. a customer profile in your CRM.',
		side: 'top',
		align: 'start',
		lazyElement: true,
	},
];

// ─── Integration 新建页 Tour（4 步）──────────────────────────────────────────

/**
 * Integration 新建页 tour（connection-auth 表单）
 * persistKey: "integration-new-tour"
 * 挂载位置：integration-settings/detail.vue（v-if="integrationId === 'new'"）
 * auto-start: true
 *
 * 步骤：System Name → Endpoint URL → Auth Method → Create Integration 按钮
 */
export const integrationNewTourSteps: TourStep[] = [
	// ── Step 1: 页面总览（Connection Auth 区域） ──────────────────────────
	{
		element: '[data-tour="integration-connection-auth"]',
		title: 'Create a New Integration',
		description:
			'This form connects WFE to an external system. Fill in the system name, endpoint URL, and authentication credentials, then click <strong>Create Integration</strong> to save.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 2: System Name ───────────────────────────────────────────────
	{
		element: '[data-tour="integration-system-name"]',
		title: 'System Name',
		description:
			'Enter a name that identifies the external system — e.g. <strong>BNP CRM</strong> or <strong>WMS Inventory</strong>. This name appears in the integration list and in Quick Links.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 3: Endpoint URL ──────────────────────────────────────────────
	{
		element: '[data-tour="integration-endpoint-url"]',
		title: 'Endpoint URL',
		description:
			'Enter the base URL of the external API — e.g. <code>https://api.example.com</code>. All requests from this integration will be sent to this endpoint.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 4: Authentication Method ────────────────────────────────────
	{
		element: '[data-tour="integration-auth-method"]',
		title: 'Authentication Method',
		description:
			'Choose how WFE authenticates with the external system: <strong>API Key</strong>, <strong>Bearer Token</strong>, <strong>Basic Auth</strong>, or <strong>OAuth 2.0</strong>. Fill in the required credentials below.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 5: Create Integration 按钮 ──────────────────────────────────
	{
		element: '[data-tour="integration-create-btn"]',
		title: 'Create Integration',
		description:
			"Once all fields are filled in, click <strong>Create Integration</strong> to save. After creation, you'll be able to configure field mappings, Quick Links, and automated actions.",
		side: 'top',
		align: 'end',
	},
];

// ─── Dynamic Field Tour（5 步）────────────────────────────────────────────────

/**
 * Dynamic Fields 列表页 tour
 * persistKey: "dynamic-field-list-tour"
 * 挂载位置：dynamicFields/index.vue
 * auto-start: true
 */
export const dynamicFieldListTourSteps: TourStep[] = [
	// ── Step 1: 页面总览 ──────────────────────────────────────────────────
	{
		element: '[data-tour="dynamic-field-page-header"]',
		title: 'Dynamic Field Management',
		description:
			'This is where you manage all dynamic fields. Dynamic Fields collect simple information in Stages — they show up as form inputs when an Assignee works on a case.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 2: Add New Field 按钮 — 用户必须点击打开弹窗 ────────────────
	{
		element: '[data-tour="dynamic-field-new-btn"]',
		title: 'Create Field',
		description:
			'Click <strong>Add New Field</strong> to open the creation dialog and configure the field name, type, and validation.',
		side: 'bottom',
		align: 'end',
		waitForUserClick: true,
		afterUserClick: async () => {
			await waitForElement('[data-tour="dynamic-field-name-input"]', 2000);
		},
	},
];

/**
 * Dynamic Fields 创建/编辑弹窗 tour
 * persistKey: "dynamic-field-form-tour"
 * 挂载位置：dynamicFields/index.vue（v-if="dialogVisible"）
 * auto-start: false（FAB 手动触发）
 */
export const dynamicFieldFormTourSteps: TourStep[] = [
	// ── Step 1: 字段名称 ───────────────────────────────────────────────────
	{
		element: '[data-tour="dynamic-field-name-input"]',
		title: 'Field Name',
		description:
			'Enter the field name that will be displayed to Assignees when they fill in the case — e.g. <strong>Customer Email</strong> or <strong>Contract Value</strong>.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 2: 字段类型 ───────────────────────────────────────────────────
	{
		element: '[data-tour="dynamic-field-type-dropdown"]',
		title: 'Field Type',
		description:
			'Select the field type: <strong>Text</strong>, <strong>Number</strong>, <strong>Date</strong>, <strong>People</strong>, <strong>Dropdown</strong>, and more. The type controls what input the Assignee sees.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 3: Save 按钮 ──────────────────────────────────────────────────
	{
		element: '[data-tour="dynamic-field-save-btn"]',
		title: 'Save Field',
		description:
			'After configuring, click <strong>Add Field</strong> to save. Once saved, this field can be added to Workflow Stages so Assignees can fill it in.',
		side: 'top',
		align: 'end',
	},
];

// ─── Checklist Tour（統一版，列表 + 创建弹窗合并）────────────────────────────

/**
 * Checklist 列表页 + 创建弹窗统一 tour
 * persistKey: "checklist-list-tour"
 * 挂载位置：checkList/index.vue
 * auto-start: true
 *
 * 步骤：页面总览 → Assignments 列 → New 按钮（waitForUserClick）→
 *       弹窗内：Name → Team → Save（lazyElement，弹窗打开后自动继续）
 */
export const checklistListTourSteps: TourStep[] = [
	// ── Step 1: 页面总览 ──────────────────────────────────────────────────
	{
		element: '[data-tour="checklist-page-header"]',
		title: 'Checklist Management',
		description:
			'This is where you manage all checklist templates. Checklists track tasks that need to be completed one by one — Assignees check off each task when working on a case.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 2: Assignments 列（先介绍，避免弹窗打开后遮挡） ─────────────
	{
		element: '[data-tour="checklist-assignments-col"]',
		title: 'View Assignments',
		description:
			'The <strong>Assignments</strong> column shows which Workflow Stages this Checklist is assigned to. A checklist can be reused across multiple stages and workflows.',
		side: 'left',
		align: 'start',
	},

	// ── Step 3: New Checklist 按钮 — 用户必须点击打开弹窗 ───────────────
	{
		element: '[data-tour="checklist-new-btn"]',
		title: 'Create Checklist',
		description:
			"Click <strong>New Checklist</strong> to open the creation dialog. You'll set the name and team here.",
		side: 'bottom',
		align: 'end',
		waitForUserClick: true,
		afterUserClick: async () => {
			await waitForElement('[data-tour="checklist-name-input"]', 2000);
		},
	},

	// ── Step 4: 弹窗内 — 名称输入（lazyElement，弹窗打开后自动显示） ──────
	{
		element: '[data-tour="checklist-name-input"]',
		title: 'Basic Info',
		description:
			'Enter the Checklist name and an optional description. Use a clear name so team members can identify it when configuring Workflow Stages.',
		side: 'bottom',
		align: 'start',
		lazyElement: true,
	},

	// ── Step 5: 弹窗内 — Team 下拉 ──────────────────────────────────────
	{
		element: '[data-tour="checklist-team-dropdown"]',
		title: 'Assign Team',
		description:
			"Select the team responsible for this Checklist. Only that team's members will see it by default when working on assigned stages.",
		side: 'bottom',
		align: 'start',
		lazyElement: true,
	},

	// ── Step 6: 弹窗内 — Save 按钮 ───────────────────────────────────────
	{
		element: '[data-tour="checklist-save-btn"]',
		title: 'Save Checklist',
		description:
			'Click <strong>Create Checklist</strong> to save. After saving, use the <strong>⋯ → View Tasks</strong> button on the row to add task items that Assignees will check off.',
		side: 'top',
		align: 'end',
		lazyElement: true,
	},
];

/**
 * @deprecated 弹窗步骤已合并进 checklistListTourSteps，保留此导出避免编译报错
 * 可在后续清理时删除
 */
export const checklistFormTourSteps: TourStep[] = [];

/**
 * Checklist Tasks 弹窗 tour
 * persistKey: "checklist-tasks-tour"
 * 挂载位置：checkList/index.vue（v-if="showTaskDialog"）
 * auto-start: false（FAB 手动触发）
 * 步骤：Tasks 区域 + Task 输入框
 */
export const checklistTasksTourSteps: TourStep[] = [
	// ── Step 1: Tasks 区域总览 ────────────────────────────────────────────
	{
		element: '[data-tour="checklist-tasks-area"]',
		title: 'Add Tasks',
		description:
			'Add task items one by one. Assignees will check off each task in the Case — and can add <strong>Notes</strong> and <strong>Attachments</strong> to provide evidence.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 2: Task 输入框 ───────────────────────────────────────────────
	{
		element: '[data-tour="checklist-task-input"]',
		title: 'Task Details',
		description:
			'Enter the task name clearly so Assignees know exactly what to do. You can also set an optional assignee per task for accountability.',
		side: 'bottom',
		align: 'start',
	},
];

// ─── Tools Tour（4 步，2 个实例）─────────────────────────────────────────────

/**
 * Tools 列表页 tour
 * persistKey: "tools-list-tour"
 * 挂载位置：actions/index.vue
 * auto-start: true
 */
export const toolsListTourSteps: TourStep[] = [
	// ── Step 1: 页面总览 ──────────────────────────────────────────────────
	{
		element: '[data-tour="tools-page-header"]',
		title: 'Tool Management',
		description:
			'This is where you manage all Tools. Tools integrate external APIs or automate operations — they can be triggered automatically when a Workflow Stage is completed.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 2: New Tool 按钮 — 用户必须点击打开 drawer ────────────────
	{
		element: '[data-tour="tools-new-btn"]',
		title: 'Create Tool',
		description:
			"Click <strong>New Tool</strong> to open the configuration panel. You'll set the HTTP request details and link it to a Workflow to trigger automatically.",
		side: 'bottom',
		align: 'end',
		waitForUserClick: true,
		afterUserClick: async () => {
			// el-drawer 动画比 el-dialog 慢，等 Action Name 表单项出现
			await waitForElement('[data-tour="tools-action-name"]', 2500);
		},
	},
];

/**
 * Tools 创建/编辑 el-drawer tour（统一版，HTTP + Python 合并）
 * persistKey: "tools-form-tour"
 * 挂载位置：actions/index.vue（toolsDrawerVisible 为 true 时）
 * auto-start: 由 actions/index.vue 强制 replayTour，不依赖 persistKey 的 seen 状态
 * 注：ActionConfigDialog 是 el-drawer，append-to-body，FAB 挂 body
 *
 * 设计原则（利用引擎的 lazyElement + hasLiveWaitStep 机制）：
 *   1. Action Type 步骤用 waitForUserClick，等用户选完类型
 *   2. afterUserClick 等待对应配置区出现（HTTP 或 Python）
 *   3. HTTP 和 Python 步骤全部标记 lazyElement: true
 *   4. 引擎预过滤：waitForUserClick 存活 → hasLiveWaitStep=true → lazyElement 保留
 *   5. 运行时：用户选 HTTP → Python 步骤元素不在 DOM → 自动跳过；反之亦然
 */
export const toolsFormTourSteps: TourStep[] = [
	// ── Step 1: Action Name ───────────────────────────────────────────────
	{
		element: '[data-tour="tools-action-name"]',
		title: 'Tool Name',
		description:
			'Give the Tool a clear, descriptive name — e.g. <strong>Update CRM Status</strong> or <strong>Validate Contract Data</strong>.',
		side: 'left',
		align: 'start',
		lazyElement: true,
	},

	// ── Step 2: Action Type — 用户必须点击选择一种类型 ───────────────────
	{
		element: '[data-tour="tools-action-type"]',
		title: 'Choose Action Type',
		description:
			'Select the type: <strong>HTTP API</strong> to call an external REST endpoint, or <strong>Python Script</strong> to run custom logic. Your choice determines the configuration section below.',
		side: 'left',
		align: 'start',
		lazyElement: true,
		waitForUserClick: true,
		afterUserClick: async () => {
			// 等 Vue 完成 v-if 切换：
			// 策略：等到 tools-request-url 和 tools-python-editor 中
			// 恰好只有一个存在（即切换稳定后），再推进
			// 超时 2500ms 后强制继续
			await new Promise<void>((resolve) => {
				const deadline = Date.now() + 2500;
				const check = () => {
					const hasHttp = !!document.querySelector('[data-tour="tools-request-url"]');
					const hasPython = !!document.querySelector('[data-tour="tools-python-editor"]');
					// 稳定状态：恰好有一个（互斥），或者两个都有/都没（继续等）
					if ((hasHttp || hasPython) && !(hasHttp && hasPython)) {
						resolve();
					} else if (Date.now() >= deadline) {
						resolve();
					} else {
						requestAnimationFrame(check);
					}
				};
				// 先等 300ms 给 Vue scheduler 时间 flush
				setTimeout(() => requestAnimationFrame(check), 300);
			});
		},
	},

	// ── HTTP 分支：以下步骤仅 HTTP_API 模式下 DOM 可见，Python 模式自动跳过 ──

	{
		element: '[data-tour="tools-request-url"]',
		title: 'Request URL & Method',
		description:
			'Enter the API endpoint URL and choose the HTTP method — <strong>GET</strong>, <strong>POST</strong>, <strong>PUT</strong>, etc. Use <code>{{CaseCode}}</code> or other <code>{{variables}}</code> to inject dynamic case data.',
		side: 'left',
		align: 'start',
		lazyElement: true,
	},
	{
		element: '[data-tour="tools-request-tabs"]',
		title: 'Params, Headers & Body',
		description:
			'Use <strong>Params</strong> for query parameters, <strong>Headers</strong> for auth tokens or content-type, and <strong>Body</strong> for POST/PUT payloads. Type <code>/</code> in any value field to insert case variables.',
		side: 'left',
		align: 'start',
		lazyElement: true,
	},
	{
		element: '[data-tour="tools-test-send"]',
		title: 'Test the Request',
		description:
			'Click <strong>Test Send</strong> to fire a live request and see the response immediately below. Verify the API returns what you expect before saving.',
		side: 'left',
		align: 'start',
		lazyElement: true,
	},

	// ── Python 分支：以下步骤仅 PYTHON_SCRIPT 模式下 DOM 可见，HTTP 模式自动跳过 ──

	{
		element: '[data-tour="tools-python-editor"]',
		title: 'Python Script Editor',
		description:
			'Write your Python code here. The script receives a <code>context</code> parameter with full case data — check the <strong>Context Structure</strong> tab in the Variables Panel for the complete schema.',
		side: 'left',
		align: 'start',
		lazyElement: true,
	},
	{
		element: '[data-tour="tools-python-test"]',
		title: 'AI Generate & Test Run',
		description:
			'Click <strong>AI Generate</strong> to describe what you need in plain English and let AI write the script. Use <strong>Test Run</strong> to execute it immediately and see stdout output.',
		side: 'left',
		align: 'start',
		lazyElement: true,
	},

	// ── 共用最后一步 ──────────────────────────────────────────────────────

	{
		element: '[data-tour="tools-save-btn"]',
		title: 'Save the Tool',
		description:
			'Click <strong>Add Action</strong> to save. Once saved, this Tool can be linked to a Workflow Stage and will trigger automatically when that stage completes.',
		side: 'top',
		align: 'end',
		lazyElement: true,
	},
];

// ─── Questionnaire Tour（9 步，2 个实例，跨页）────────────────────────────────

/**
 * Questionnaire 列表页 tour
 * persistKey: "questionnaire-list-tour"
 * 挂载位置：questionnaire/index.vue
 * auto-start: true
 */
export const questionnaireListTourSteps: TourStep[] = [
	// ── Step 1: 页面总览 ──────────────────────────────────────────────────
	{
		element: '[data-tour="questionnaire-page-header"]',
		title: 'Questionnaire Management',
		description:
			'This is where you manage all questionnaire templates. Questionnaires collect structured information from Assignees — organized into sections and questions.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 2: New Questionnaire 按钮 ───────────────────────────────────
	{
		element: '[data-tour="questionnaire-new-btn"]',
		title: 'Create Questionnaire',
		description:
			"Click <strong>New Questionnaire</strong> to create a new template. You'll be taken to the editor where you can add sections and questions.",
		side: 'bottom',
		align: 'end',
		waitForUserClick: true,
		// 用户点击后跳转到 createQuestion.vue，列表页 tour 结束
	},
];

/**
 * Questionnaire 编辑页 tour（createQuestion.vue）
 * persistKey: "questionnaire-editor-tour"
 * 挂载位置：questionnaire/createQuestion.vue
 * auto-start: true（首次进入编辑页自动启动，后续编辑已有问卷不再自动启动）
 */
export const questionnaireEditorTourSteps: TourStep[] = [
	// ── Step 1: 名称输入 ───────────────────────────────────────────────────
	{
		element: '[data-tour="questionnaire-name-input"]',
		title: 'Basic Info',
		description:
			"Enter the Questionnaire name and an optional description. Use a clear name so it's easy to identify when attaching to Workflow Stages.",
		side: 'bottom',
		align: 'start',
	},

	// ── Step 2: Sections 区域 ─────────────────────────────────────────────
	{
		element: '[data-tour="questionnaire-sections-area"]',
		title: 'Add Section',
		description:
			'A questionnaire consists of multiple <strong>Sections</strong> to organize related questions. Each section has a title and can hold multiple questions.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 3: 添加 Section 按钮 ────────────────────────────────────────
	{
		element: '[data-tour="questionnaire-add-section-btn"]',
		title: 'Add Section',
		description:
			'Click to add a new Section and group your questions. Sections help Assignees navigate longer questionnaires step by step.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 4: Questions 区域 ────────────────────────────────────────────
	{
		element: '[data-tour="questionnaire-questions-area"]',
		title: 'Add Questions',
		description:
			'Add <strong>Questions</strong> within each Section. Each question can have its own type, instructions, and required setting.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 5: Question Type 下拉 ────────────────────────────────────────
	{
		element: '[data-tour="questionnaire-question-type-dropdown"]',
		title: 'Question Type',
		description:
			'Choose the question type: <strong>Single-line Text</strong>, <strong>People</strong>, <strong>Date Picker</strong>, <strong>Number</strong>, <strong>File</strong>, and more. The type controls what input the Assignee sees.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 6: Required 开关 ─────────────────────────────────────────────
	{
		element: '[data-tour="questionnaire-required-toggle"]',
		title: 'Mark as Required',
		description:
			'Toggle this on to make the question mandatory. Questions marked as <strong>Required</strong> must be filled before the Assignee can submit the questionnaire.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 7: Question Text 输入框 ─────────────────────────────────────
	{
		element: '[data-tour="questionnaire-question-text"]',
		title: 'Question Text',
		description:
			'Enter the question text — this is exactly what the Assignee will read. Be specific and clear, e.g. <em>"What is the customer\'s annual contract value?"</em>',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 8: Add Question 按钮 ─────────────────────────────────────────
	{
		element: '[data-tour="questionnaire-add-question-btn"]',
		title: 'Add Question',
		description:
			'Click <strong>Add Question</strong> to add it to the current section. You can add as many questions as needed — each appears in the section list above.',
		side: 'top',
		align: 'start',
	},

	// ── Step 9: Save 按钮 ─────────────────────────────────────────────────
	{
		element: '[data-tour="questionnaire-save-btn"]',
		title: 'Save Questionnaire',
		description:
			'Click <strong>Save Questionnaire</strong> when done. After saving, this Questionnaire can be added to Workflow Stages for Assignees to complete.',
		side: 'bottom',
		align: 'end',
	},
];

// ─── Manage Teams Tour（5 步，iframe 外框）───────────────────────────────────

/**
 * Manage Teams 页 tour（iframe 外框高亮）
 * persistKey: "manage-teams-tour"
 * 挂载位置：authorityManagement/teams.vue
 * auto-start: true
 *
 * ⚠️ Teams 页面是嵌套 iframe（IDM 系统），无法高亮 iframe 内部元素。
 * 所有步骤共用 teams-iframe-container 锚点（包裹 iframe 的外层 div），
 * 通过文字内容引导用户操作。
 */
export const manageTeamsTourSteps: TourStep[] = [
	// ── Step 1: 页面总览 ──────────────────────────────────────────────────
	{
		element: '[data-tour="teams-iframe-container"]',
		title: 'Team Management',
		description:
			'This is where you manage all teams. Teams are used to assign responsibility for Checklists — each Checklist is owned by a team.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 2: Create Team 提示 ─────────────────────────────────────────
	{
		element: '[data-tour="teams-iframe-container"]',
		title: 'Create Team',
		description:
			'Click <strong>Add New Team</strong> inside the panel to create a new team. A form will appear to configure the team details.',
		side: 'bottom',
		align: 'start',
	},

	// ── Step 3: Team Name 提示 ────────────────────────────────────────────
	{
		element: '[data-tour="teams-iframe-container"]',
		title: 'Team Name',
		description:
			"Enter the team name (e.g. <strong>Implementation</strong>, <strong>Billing</strong>, <strong>WMS Support</strong>). Use a name that clearly identifies the team's function.",
		side: 'bottom',
		align: 'start',
	},

	// ── Step 4: Add Members 提示 ─────────────────────────────────────────
	{
		element: '[data-tour="teams-iframe-container"]',
		title: 'Add Members',
		description:
			"Optionally add team members. Members indicate who is responsible for the team's tasks — they will appear as assignee options in Checklist configurations.",
		side: 'bottom',
		align: 'start',
	},

	// ── Step 5: Save Team 提示 ────────────────────────────────────────────
	{
		element: '[data-tour="teams-iframe-container"]',
		title: 'Save Team',
		description:
			'Click <strong>Save</strong> to create the team. After saving, this Team can be selected in the Checklist configuration to assign responsibility.',
		side: 'bottom',
		align: 'start',
	},
];
