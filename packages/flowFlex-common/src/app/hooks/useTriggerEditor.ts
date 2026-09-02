import { ref, computed, readonly } from 'vue';
import { ElMessage } from 'element-plus';
import { getTriggerGraph, saveTriggerGraph, getTriggerGraphAllWorkflows } from '@/apis/ow/triggers';

// ========================= 类型定义 =========================

export interface WorkflowItem {
	id: string;
	name: string;
	status: 'active' | 'inactive';
	stageCount?: number;
}

export interface CanvasCard {
	workflowId: string;
	name: string;
	status: 'active' | 'inactive';
	/** 画布坐标（px） */
	x: number;
	y: number;
}

export interface TriggerConnection {
	id: string;
	sourceWorkflowId: string;
	targetWorkflowId: string;
	/** 规则名称 */
	ruleName?: string;
	/** 条件摘要文本，用于连线标签 */
	conditionSummary?: string;
	/** Trigger Conditions（多条，AND/OR 关系） */
	conditions?: TriggerCondition[];
	/** Data Mappings */
	mappings?: DataMapping[];
	/** 完整配置 JSON，供后端存储 */
	configJson?: string;
}

/** 单条触发条件 */
export interface TriggerCondition {
	id: string;
	/** AND / OR */
	logic: 'AND' | 'OR';
	/** 条件来源：stage_complete / field / component */
	type: 'stage_complete' | 'field' | 'component';
	/** 对应资源 id（stageId / fieldId / componentId） */
	resourceId?: string;
	resourceName?: string;
	/** 操作符 */
	operator?: '==' | '!=' | '>' | '>=' | '<' | '<=' | 'contains' | 'not_contains';
	/** 对比值 */
	value?: string;
}

/** 单条数据映射 */
export interface DataMapping {
	id: string;
	/** 来源类型 */
	sourceType: 'dynamic_field' | 'questionnaire' | 'static';
	sourceId?: string;
	sourceName?: string;
	/** 目标字段 id */
	targetFieldId?: string;
	targetFieldName?: string;
	/** static 时的固定值 */
	staticValue?: string;
}

// ========================= Composable =========================

export function useTriggerEditor(workflowId: string) {
	// ----- 数据 -----
	const allWorkflows = ref<WorkflowItem[]>([]);
	const cards = ref<CanvasCard[]>([]);
	const connections = ref<TriggerConnection[]>([]);

	// ----- 当前操作状态 -----
	const selectedConnectionId = ref<string | null>(null);
	const connectingFrom = ref<string | null>(null);

	// ----- 加载 / 错误 -----
	const loading = ref(false);
	const saving = ref(false);
	const error = ref<string | null>(null);
	const hasUnsavedChanges = ref(false);

	// ========================= 计算属性 =========================

	const canvasWorkflowIds = computed(() => new Set(cards.value.map((c) => c.workflowId)));

	const onCanvasCount = computed(() => cards.value.length);
	const connectionCount = computed(() => connections.value.length);

	const selectedConnection = computed(
		() => connections.value.find((c) => c.id === selectedConnectionId.value) ?? null
	);

	// ========================= 初始化 =========================

	const init = async () => {
		loading.value = true;
		error.value = null;
		try {
			// Workflow 列表：使用 OW-725 专用查询接口
			const workflowsRes = await getTriggerGraphAllWorkflows();
			allWorkflows.value = (workflowsRes?.data ?? []).map((w: any) => ({
				id: String(w.id),
				name: w.name,
				status: w.isActive ? 'active' : 'inactive',
				stageCount: w.stageCount ?? 0,
			}));

			// Trigger Graph（卡片位置 + 连线）：使用真实接口
			const graphRes = await getTriggerGraph(workflowId);
			const graphData = graphRes?.data;
			if (graphData) {
				// 解析 canvasLayout → CanvasCard 列表
				let layoutMap: Record<string, { x: number; y: number }> = {};
				try {
					layoutMap = JSON.parse(graphData.canvasLayout || '{}');
				} catch {
					/* ignore */
				}

				let canvasIds: string[] = [];
				try {
					canvasIds = JSON.parse(graphData.canvasWorkflowIds || '[]');
				} catch {
					/* ignore */
				}

				// 全局 graph：canvas 里的 workflow 由后端数据决定，
				// workflowId 仅作为当前入口标识（用于卡片高亮），不强制加入 canvas。
				// 如果 canvasWorkflowIds 里没有当前 workflow，也不自动添加——
				// 用户需要手动从左侧列表拖入。
				const allCanvasIds = new Set(canvasIds.map(String));

				cards.value = Array.from(allCanvasIds).map((wfId) => {
					const wf = allWorkflows.value.find((w) => w.id === wfId);
					const pos = layoutMap[wfId] ?? { x: 80 + cards.value.length * 20, y: 80 };
					return {
						workflowId: wfId,
						name: wf?.name ?? wfId,
						status: wf?.status ?? 'active',
						x: pos.x,
						y: pos.y,
					};
				});

				// Normalise card positions: if any card has x < 80 or y < 80, shift all cards
				// so the leftmost/topmost card sits at (80, 80). This prevents cards from
				// being placed off the visible area of the canvas.
				const cardList = cards.value;
				if (cardList.length > 0) {
					const minX = Math.min(...cardList.map((c) => c.x));
					const minY = Math.min(...cardList.map((c) => c.y));
					const shiftX = minX < 80 ? 80 - minX : 0;
					const shiftY = minY < 80 ? 80 - minY : 0;
					if (shiftX > 0 || shiftY > 0) {
						cards.value = cardList.map((c) => ({
							...c,
							x: c.x + shiftX,
							y: c.y + shiftY,
						}));
					}
				}

				// connections 直接映射
				connections.value = (graphData.connections ?? []).map((c: any) => ({
					id: String(c.id),
					sourceWorkflowId: String(c.sourceWorkflowId),
					targetWorkflowId: String(c.targetWorkflowId),
					ruleName: c.ruleName ?? '',
					conditionSummary: c.conditionSummary ?? '',
					configJson: c.configJson ?? '{}',
				}));
			} else {
				// 全局 graph 还不存在，从空白开始，用户从左侧拖入 workflow
				cards.value = [];
				connections.value = [];
			}
		} catch (e: any) {
			error.value = e?.message ?? 'Failed to load trigger graph';
		} finally {
			loading.value = false;
		}
	};

	/** 撤销未保存的改动，重新从服务器加载 graph（不重新加载 workflow 列表） */
	const revert = async () => {
		loading.value = true;
		try {
			const graphRes = await getTriggerGraph(workflowId);
			const graphData = graphRes?.data;
			if (graphData) {
				let layoutMap: Record<string, { x: number; y: number }> = {};
				try {
					layoutMap = JSON.parse(graphData.canvasLayout || '{}');
				} catch {
					/* ignore */
				}

				let canvasIds: string[] = [];
				try {
					canvasIds = JSON.parse(graphData.canvasWorkflowIds || '[]');
				} catch {
					/* ignore */
				}

				const allCanvasIds = new Set(canvasIds.map(String));
				cards.value = Array.from(allCanvasIds).map((wfId) => {
					const wf = allWorkflows.value.find((w) => w.id === wfId);
					const pos = layoutMap[wfId] ?? { x: 80, y: 80 };
					return {
						workflowId: wfId,
						name: wf?.name ?? wfId,
						status: wf?.status ?? 'active',
						x: pos.x,
						y: pos.y,
					};
				});

				connections.value = (graphData.connections ?? []).map((c: any) => ({
					id: String(c.id),
					sourceWorkflowId: String(c.sourceWorkflowId),
					targetWorkflowId: String(c.targetWorkflowId),
					ruleName: c.ruleName ?? '',
					conditionSummary: c.conditionSummary ?? '',
					configJson: c.configJson ?? '{}',
				}));
			} else {
				cards.value = [];
				connections.value = [];
			}

			hasUnsavedChanges.value = false;
			selectedConnectionId.value = null;
		} catch {
			// ignore — keep current state
		} finally {
			loading.value = false;
		}
	};

	// ========================= 画布操作 =========================

	/** 将 Workflow 加入画布 */
	const addToCanvas = (id: string) => {
		if (canvasWorkflowIds.value.has(id)) return;
		const wf = allWorkflows.value.find((w) => w.id === id);
		if (!wf) return;
		const offset = cards.value.length * 20;
		cards.value.push({
			workflowId: wf.id,
			name: wf.name,
			status: wf.status,
			x: 80 + offset,
			y: 80 + offset,
		});
		hasUnsavedChanges.value = true;
	};

	/** 将 Workflow 移出画布（同时删除相关连线） */
	const removeFromCanvas = (id: string) => {
		// Current workflow 不可移除
		if (id === workflowId) return;
		cards.value = cards.value.filter((c) => c.workflowId !== id);
		connections.value = connections.value.filter(
			(c) => c.sourceWorkflowId !== id && c.targetWorkflowId !== id
		);
		if (selectedConnectionId.value) {
			const still = connections.value.find((c) => c.id === selectedConnectionId.value);
			if (!still) selectedConnectionId.value = null;
		}
		hasUnsavedChanges.value = true;
	};

	/** 更新卡片位置（拖拽结束后调用） */
	const updateCardPosition = (id: string, x: number, y: number) => {
		const card = cards.value.find((c) => c.workflowId === id);
		if (card) {
			card.x = x;
			card.y = y;
			hasUnsavedChanges.value = true;
		}
	};

	/** 添加连线（一个 workflow 只能有一条 input） */
	const addConnection = (sourceId: string, targetId: string) => {
		// 不允许自连
		if (sourceId === targetId) return;

		// 不允许重复连线
		const exists = connections.value.some(
			(c) => c.sourceWorkflowId === sourceId && c.targetWorkflowId === targetId
		);
		if (exists) return;

		// 一个 workflow 只能有一条 input 连线
		const hasInput = connections.value.some((c) => c.targetWorkflowId === targetId);
		if (hasInput) {
			const targetName = cards.value.find((c) => c.workflowId === targetId)?.name ?? targetId;
			ElMessage.warning(
				`"${targetName}" already has an incoming connection. A workflow can only have one input.`
			);
			return;
		}

		const id = `conn_${Date.now()}`;
		connections.value.push({
			id,
			sourceWorkflowId: sourceId,
			targetWorkflowId: targetId,
			conditionSummary: 'Completed',
		});
		hasUnsavedChanges.value = true;
	};

	/** 删除连线 */
	const removeConnection = (connectionId: string) => {
		connections.value = connections.value.filter((c) => c.id !== connectionId);
		if (selectedConnectionId.value === connectionId) {
			selectedConnectionId.value = null;
		}
		hasUnsavedChanges.value = true;
	};

	/** 选中连线（供 OW-727 配置面板使用） */
	const selectConnection = (id: string | null) => {
		selectedConnectionId.value = id;
	};

	/** 更新连线配置（OW-727 保存后回调） */
	const updateConnectionConfig = (
		id: string,
		conditionSummary: string,
		configJson: string,
		ruleName?: string
	) => {
		const conn = connections.value.find((c) => c.id === id);
		if (conn) {
			conn.conditionSummary = conditionSummary;
			conn.configJson = configJson;
			if (ruleName !== undefined) conn.ruleName = ruleName;
			try {
				const cfg = JSON.parse(configJson);
				conn.conditions = cfg.conditions ?? [];
				conn.mappings = cfg.mappings ?? [];
			} catch {
				/* ignore */
			}
			hasUnsavedChanges.value = true;
		}
	};

	// ========================= 保存 =========================

	const save = async () => {
		saving.value = true;
		try {
			// 序列化 canvasLayout
			const layoutMap: Record<string, { x: number; y: number }> = {};
			for (const card of cards.value) {
				layoutMap[card.workflowId] = { x: card.x, y: card.y };
			}

			// canvasWorkflowIds = 所有 canvas 上的 workflow id（全局 graph，不排除任何一个）
			const canvasIds = cards.value.map((c) => c.workflowId);

			await saveTriggerGraph({
				workflowId,
				canvasLayout: JSON.stringify(layoutMap),
				canvasWorkflowIds: JSON.stringify(canvasIds),
				connections: connections.value.map((c, i) => ({
					sourceWorkflowId: c.sourceWorkflowId,
					targetWorkflowId: c.targetWorkflowId,
					ruleName: c.ruleName ?? '',
					conditionSummary: c.conditionSummary ?? '',
					configJson: c.configJson ?? '{}',
					isEnabled: true,
					executionOrder: i,
				})),
			});

			hasUnsavedChanges.value = false;
		} finally {
			saving.value = false;
		}
	};

	// ========================= 连线拖拽状态 =========================

	const startConnecting = (fromWorkflowId: string) => {
		connectingFrom.value = fromWorkflowId;
	};

	const finishConnecting = (toWorkflowId: string) => {
		if (connectingFrom.value && connectingFrom.value !== toWorkflowId) {
			addConnection(connectingFrom.value, toWorkflowId);
		}
		connectingFrom.value = null;
	};

	const cancelConnecting = () => {
		connectingFrom.value = null;
	};

	return {
		// state (readonly for external safety)
		allWorkflows: readonly(allWorkflows),
		cards: readonly(cards),
		connections: readonly(connections),
		loading: readonly(loading),
		saving: readonly(saving),
		error: readonly(error),
		hasUnsavedChanges: readonly(hasUnsavedChanges),
		selectedConnectionId: readonly(selectedConnectionId),
		connectingFrom: readonly(connectingFrom),

		// computed
		canvasWorkflowIds,
		onCanvasCount,
		connectionCount,
		selectedConnection,

		// actions
		init,
		revert,
		addToCanvas,
		removeFromCanvas,
		updateCardPosition,
		addConnection,
		removeConnection,
		selectConnection,
		updateConnectionConfig,
		save,
		startConnecting,
		finishConnecting,
		cancelConnecting,
	};
}
