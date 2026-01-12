import { defHttp } from '@/apis/axios';
import { useGlobSetting } from '@/settings';
import { ApiResponse, TriggerMapping } from '#/action';

const globSetting = useGlobSetting();

const Api = (id?: string | number) => {
	return {
		// 工作流相关API
		workflows: `${globSetting.apiProName}/ow/workflows/${globSetting.apiVersion}`,
		workflow: `${globSetting.apiProName}/ow/workflows/${globSetting.apiVersion}/${id}`,
		historyWorkflow: `${globSetting.apiProName}/ow/workflows/${globSetting.apiVersion}/create-from-version`,
		workflowQuery: `${globSetting.apiProName}/ow/workflows/${globSetting.apiVersion}/query`,
		workflowDeactivate: `${globSetting.apiProName}/ow/workflows/${globSetting.apiVersion}/${id}/deactivate`,
		workflowActivate: `${globSetting.apiProName}/ow/workflows/${globSetting.apiVersion}/${id}/activate`,
		workflowSetDefault: `${globSetting.apiProName}/ow/workflows/${globSetting.apiVersion}/${id}/set-default`,
		workflowDuplicate: `${globSetting.apiProName}/ow/workflows/${globSetting.apiVersion}/${id}/duplicate`,
		workflowVersions: `${globSetting.apiProName}/ow/workflows/${globSetting.apiVersion}/${id}/versions`,
		workflowCreateVersion: `${globSetting.apiProName}/ow/workflows/${globSetting.apiVersion}/${id}/create-version`,
		workflowExportExcel: `${globSetting.apiProName}/ow/workflows/${globSetting.apiVersion}/${id}/export-detailed-excel`,

		// 阶段相关API
		stages: `${globSetting.apiProName}/ow/stages/${globSetting.apiVersion}`,
		stage: `${globSetting.apiProName}/ow/stages/${globSetting.apiVersion}/${id}`,
		stagesByWorkflow: `${globSetting.apiProName}/ow/workflows/${globSetting.apiVersion}/${id}/stages`,
		stageSort: `${globSetting.apiProName}/ow/stages/${globSetting.apiVersion}/sort`,
		stageColor: `${globSetting.apiProName}/ow/stages/${globSetting.apiVersion}/${id}/color`,
		stageRequiredFields: `${globSetting.apiProName}/ow/stages/${globSetting.apiVersion}/${id}/required-fields`,
		stageDuplicate: `${globSetting.apiProName}/ow/stages/${globSetting.apiVersion}/${id}/duplicate`,
		stageQuery: `${globSetting.apiProName}/ow/stages/${globSetting.apiVersion}/query`,

		allWorkflows: `${globSetting.apiProName}/ow/workflows/${globSetting.apiVersion}/all`,
		allStages: `${globSetting.apiProName}/ow/stages/${globSetting.apiVersion}/all`,

		// 权限检查API
		permissionCheck: `${globSetting.apiProName}/ow/permissions/${globSetting.apiVersion}/check`,

		components: `${globSetting.apiProName}/ow/components/${globSetting.apiVersion}`,

		stageConditions: `${globSetting.apiProName}/ow/stage-conditions/${globSetting.apiVersion}`,

		conditionAction: `${globSetting.apiProName}/action/${globSetting.apiVersion}/definitions/all`,
	};
};

// ========================= 工作流相关接口 =========================

/**
 * 创建工作流 [W01]
 * @param params WorkflowInputDto
 * @returns long (工作流ID)
 */
export function createWorkflow(params: any) {
	return defHttp.post({ url: `${Api().workflows}`, params });
}

/**
 * 获取工作流列表 [W02]
 * @returns List<WorkflowOutputDto>
 */
export function getWorkflowList(params?: any) {
	return defHttp.get({ url: `${Api().workflows}`, params });
}

/**
 * 获取工作流详情 [W03]
 * @param id 工作流ID
 * @returns WorkflowOutputDto
 */
export function getWorkflowDetail(id: string | number) {
	return defHttp.get({ url: `${Api(id).workflow}` });
}

/**
 * 更新工作流 [W04]
 * @param id 工作流ID
 * @param params WorkflowInputDto
 * @returns bool
 */
export function updateWorkflow(id: string | number, params: any) {
	return defHttp.put({ url: `${Api(id).workflow}`, params });
}

/**
 * 分页查询工作流 [W05]
 * @param params WorkflowQueryRequest
 * @returns PagedResult<WorkflowOutputDto>
 */
export function queryWorkflows(params: any) {
	return defHttp.post({ url: `${Api().workflowQuery}`, params });
}

/**
 * 停用工作流 [W06]
 * @param id 工作流ID
 * @returns bool
 */
export function deactivateWorkflow(id: string | number) {
	return defHttp.post({ url: `${Api(id).workflowDeactivate}` });
}

/**
 * 激活工作流 [W07]
 * @param id 工作流ID
 * @returns bool
 */
export function activateWorkflow(id: string | number) {
	return defHttp.post({ url: `${Api(id).workflowActivate}` });
}

/**
 * 设置默认工作流 [W08]
 * @param id 工作流ID
 * @returns bool
 */
export function setDefaultWorkflow(id: string | number) {
	return defHttp.post({ url: `${Api(id).workflowSetDefault}` });
}

/**
 * 复制工作流 [W09]
 * @param id 工作流ID
 * @param params DuplicateWorkflowInputDto
 * @returns long (新工作流ID)
 */
export function duplicateWorkflow(id: string | number, params: any) {
	return defHttp.post({ url: `${Api(id).workflowDuplicate}`, params });
}

/**
 * 导出工作流到Excel [W13]
 * @param params WorkflowExportSearch
 * @returns Excel文件流
 */
export function exportWorkflowToExcel(workflowId: string) {
	return defHttp.get({ url: `${Api(workflowId).workflowExportExcel}`, responseType: 'blob' });
}

// ========================= 阶段相关接口 =========================

/**
 * 创建阶段 [S01]
 * @param params StageInputDto
 * @returns long (阶段ID)
 */
export function createStage(params: any) {
	return defHttp.post({ url: `${Api().stages}`, params });
}

/**
 * 获取工作流的所有阶段 [S02]
 * @param workflowId 工作流ID
 * @returns List<StageOutputDto>
 */
export function getStagesByWorkflow(workflowId: string | number) {
	return defHttp.get({ url: `${Api(workflowId).stagesByWorkflow}` });
}

/**
 * 阶段排序 [S04]
 * @param params SortStagesInputDto
 * @returns bool
 */
export function sortStages(params: any) {
	return defHttp.post({ url: `${Api().stageSort}`, params });
}

/**
 * 更新阶段 [S05]
 * @param id 阶段ID
 * @param params StageInputDto
 * @returns bool
 */
export function updateStage(id: string | number, params: any) {
	return defHttp.put({ url: `${Api(id).stage}`, params });
}

/**
 * 设置阶段颜色 [S06]
 * @param id 阶段ID
 * @param params SetStageColorInputDto
 * @returns bool
 */
export function setStageColor(id: string | number, params: any) {
	return defHttp.post({ url: `${Api(id).stageColor}`, params });
}

/**
 * 更新阶段必填字段 [S07]
 * @param id 阶段ID
 * @param params UpdateRequiredFieldsInputDto
 * @returns bool
 */
export function updateStageRequiredFields(id: string | number, params: any) {
	return defHttp.post({ url: `${Api(id).stageRequiredFields}`, params });
}

/**
 * 复制阶段 [S08]
 * @param id 阶段ID
 * @param params DuplicateStageInputDto
 * @returns long (新阶段ID)
 */
export function duplicateStage(id: string | number, params: any) {
	return defHttp.post({ url: `${Api(id).stageDuplicate}`, params });
}

/**
 * 分页查询阶段 [S09]
 * @param params StageQueryRequest
 * @returns PagedResult<StageOutputDto>
 */
export function queryStages(params: any) {
	return defHttp.post({ url: `${Api().stageQuery}`, params });
}

/**
 * 删除阶段 [S10]
 * @param id 阶段ID
 * @param confirm 是否确认删除
 * @returns bool
 */
export function deleteStage(id: string | number, confirm: boolean) {
	return defHttp.delete({
		url: `${Api(id).stage}?confirm=${confirm}`,
	});
}

/**
 * 获取所有工作流
 * @returns List<WorkflowOutputDto>
 */
export function getWorkflows() {
	return defHttp.get({ url: `${Api().allWorkflows}` });
}

/**
 * 获取所有阶段
 * @returns List<StageOutputDto>
 */
export function getAllStages() {
	return defHttp.get({ url: `${Api().allStages}` });
}

// ========================= 权限检查相关接口 =========================

/**
 * 检查用户权限
 * @param params { resourceId: string, resourceType: 1 | 2 | 3 }
 * @returns { success: boolean, data: { canView: boolean, canOperate: boolean, grantReason: string, errorMessage: string | null } }
 */
export function checkPermission(params: {
	resourceId: string;
	resourceType: 1 | 2 | 3; // 1: Workflow, 2: Stage, 3: Case
}) {
	return defHttp.post({ url: `${Api().permissionCheck}`, params });
}

// ========================= Stage Condition 相关接口 =========================
// 基础路径: /api/ow/stage-conditions/v1

/**
 * 按工作流查询条件
 * GET /stage-conditions/v1/by-workflow/{workflowId}
 */
export function getConditionsByWorkflow(workflowId: string | number) {
	return defHttp.get({
		url: `${Api().stageConditions}/by-workflow/${workflowId}`,
	});
}

/**
 * 按阶段查询条件
 * GET /stage-conditions/v1/by-stage/{stageId}
 */
export function getConditionByStage(stageId: string | number) {
	return defHttp.get({
		url: `${Api().stageConditions}/by-stage/${stageId}`,
	});
}

/**
 * 获取条件详情
 * GET /stage-conditions/v1/{id}
 */
export function getConditionById(id: string | number) {
	return defHttp.get({
		url: `${Api().stageConditions}/${id}`,
	});
}

/**
 * 创建条件
 * POST /stage-conditions/v1
 */
export function createCondition(params: {
	stageId: string;
	workflowId?: string;
	name: string;
	description?: string;
	rulesJson: string;
	actionsJson: string;
	fallbackStageId?: string;
	isActive?: boolean;
}) {
	return defHttp.post({
		url: Api().stageConditions,
		params,
	});
}

/**
 * 更新条件
 * PUT /stage-conditions/v1/{id}
 */
export function updateCondition(
	id: string | number,
	params: {
		stageId: string;
		workflowId?: string;
		name: string;
		description?: string;
		rulesJson: string;
		actionsJson: string;
		fallbackStageId?: string;
		isActive?: boolean;
	}
) {
	return defHttp.put({
		url: `${Api().stageConditions}/${id}`,
		params,
	});
}

/**
 * 删除条件
 * DELETE /stage-conditions/v1/{id}
 */
export function deleteCondition(id: string | number) {
	return defHttp.delete({
		url: `${Api().stageConditions}/${id}`,
	});
}

/**
 * 验证规则 JSON（语法验证，保存前使用）
 * POST /stage-conditions/v1/validate-rules
 */
export function validateRules(rulesJson: string) {
	return defHttp.post({
		url: `${Api().stageConditions}/validate-rules`,
		params: { rulesJson },
	});
}

/**
 * 验证条件配置（完整验证，保存后使用）
 * POST /stage-conditions/v1/{id}/validate
 */
export function validateCondition(id: string | number) {
	return defHttp.post({
		url: `${Api().stageConditions}/${id}/validate`,
	});
}

/**
 *
 * @returns List<TriggerMapping>
 */
export function conditionAction(): Promise<ApiResponse<TriggerMapping[]>> {
	return defHttp.get({
		url: `${Api().conditionAction}`,
	});
}
