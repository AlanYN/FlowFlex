import { defHttp } from '@/apis/axios';
import { useGlobSetting } from '@/settings';

const globSetting = useGlobSetting();

const Api = () => {
	const base = `${globSetting.apiProName}/ow/trigger-graph/${globSetting.apiVersion}`;
	return {
		// Graph CRUD (OW-723)
		graph: (workflowId: string | number) => `${base}/${workflowId}`,
		graphSave: base,

		// Query interfaces (OW-725)
		allWorkflows: `${base}/workflows/all`,
		nodeInfo: (workflowId: string | number) => `${base}/workflows/${workflowId}/node-info`,

		// OW-729: Trigger History
		logsByOnboarding: (onboardingId: string | number) =>
			`${base}/logs/by-onboarding/${onboardingId}`,
		logsByWorkflow: (workflowId: string | number) => `${base}/logs/by-workflow/${workflowId}`,
	};
};

// ─── OW-723: Graph CRUD ──────────────────────────────────────────────────────

/**
 * 获取指定 Workflow 的 Trigger Graph（含 connections）
 * GET /ow/trigger-graph/v1/{workflowId}
 */
export function getTriggerGraph(workflowId: string | number) {
	return defHttp.get({ url: Api().graph(workflowId) });
}

export interface SaveTriggerGraphParams {
	workflowId: string | number;
	name?: string;
	/** canvas layout JSON string: {"workflowId": {x, y}, ...} */
	canvasLayout: string;
	/** canvas workflow IDs JSON string: ["id1", "id2", ...] */
	canvasWorkflowIds: string;
	connections: {
		id?: number;
		graphId?: number;
		sourceWorkflowId: string | number;
		targetWorkflowId: string | number;
		ruleName?: string;
		conditionSummary?: string;
		configJson?: string;
		isEnabled?: boolean;
		executionOrder?: number;
	}[];
}

/**
 * 保存 Trigger Graph（create-or-update，全量替换 connections）
 * POST /ow/trigger-graph/v1
 */
export function saveTriggerGraph(params: SaveTriggerGraphParams) {
	return defHttp.post({ url: Api().graphSave, params });
}

// ─── OW-725: Query interfaces ────────────────────────────────────────────────

/**
 * 获取所有 Workflow 列表（id + name + status），供左侧面板使用
 * GET /ow/trigger-graph/v1/workflows/all
 */
export function getTriggerGraphAllWorkflows() {
	return defHttp.get({ url: Api().allWorkflows });
}

/**
 * 获取指定 Workflow 的节点信息（Stage + Fields / Questions / Tasks）
 * 用于 ConnectionPanel 的条件三级联动
 * GET /ow/trigger-graph/v1/workflows/{workflowId}/node-info
 */
export function getWorkflowNodeInfo(workflowId: string | number) {
	return defHttp.get({ url: Api().nodeInfo(workflowId) });
}

// ─── OW-729: Trigger History ─────────────────────────────────────────────────

export function getTriggerLogsByOnboarding(onboardingId: string | number) {
	return defHttp.get({ url: Api().logsByOnboarding(onboardingId) });
}

export function getTriggerLogsByWorkflow(
	workflowId: string | number,
	params?: { pageIndex?: number; pageSize?: number; status?: string }
) {
	return defHttp.get({ url: Api().logsByWorkflow(workflowId), params });
}

/**
 * 手动重新触发 — 用于 Trigger History 里 Failed/Skipped 记录的 Retry
 * POST /ow/trigger-graph/v1/debug/fire/{onboardingId}/{workflowId}
 */
export function retryTrigger(onboardingId: string | number, workflowId: string | number) {
	return defHttp.post({
		url: `${globSetting.apiProName}/ow/trigger-graph/${globSetting.apiVersion}/debug/fire/${onboardingId}/${workflowId}`,
	});
}

/**
 * OW-728: Get upstream/downstream related Cases for a Case detail page
 * GET /ow/trigger-graph/v1/logs/related-cases/{onboardingId}
 */
export function getRelatedCases(onboardingId: string | number) {
	const { apiProName, apiVersion } = useGlobSetting();
	return defHttp.get({
		url: `${apiProName}/ow/trigger-graph/${apiVersion}/logs/related-cases/${onboardingId}`,
	});
}
