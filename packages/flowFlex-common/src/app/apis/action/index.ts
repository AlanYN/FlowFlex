import { defHttp } from '@/apis/axios';
import {
	ActionDefinition,
	ActionQueryRequest,
	SystemActionDefinitionDto,
	SystemActionTemplateDto,
} from '#/action';
import { useGlobSetting } from '@/settings';

// Change History interface
export interface ActionChangeHistoryItem {
	type: string;
	changes: string;
	description: string;
	status: 'Success' | 'Failed' | 'Warning';
	updatedBy: string;
	dateTime: string;
}

const globSetting = useGlobSetting();

// Action Type Enum
export enum ActionType {
	PYTHON_SCRIPT = 1,
	HTTP_API = 2,
	SEND_EMAIL = 3,
	SYSTEM_TOOLS = 4,
}

// Action Type Mapping
export const ACTION_TYPE_MAPPING = {
	[ActionType.PYTHON_SCRIPT]: 'Python Script',
	[ActionType.HTTP_API]: 'HTTP API',
	[ActionType.SEND_EMAIL]: 'Send Email',
	[ActionType.SYSTEM_TOOLS]: 'System Tools',
} as const;

// Frontend to Backend Type Mapping
export const FRONTEND_TO_BACKEND_TYPE_MAPPING = {
	python: 'PythonScript',
	http: 'HttpApi',
	email: 'SendEmail',
} as const;

// Test result interface for different action types
export interface TestResult {
	success: boolean;
	message?: string;
	stdout?: string;
	stderr?: string;
	executionTime?: string;
	memoryUsage?: number;
	status?: string;
	token?: string;
	timestamp?: string;
	// For HTTP API responses
	statusCode?: number;
	responseBody?: string;
	responseHeaders?: Record<string, string>;
	// For Email actions
	emailSent?: boolean;
	recipients?: string[];
	// Generic data for other action types
	data?: any;
}

const Api = (id?: string) => {
	return {
		action: `${globSetting.apiProName}/action/${globSetting.apiVersion}/definitions`,

		textRun: `${globSetting.apiProName}/action/${globSetting.apiVersion}/test/direct`,

		actionList: `${globSetting.apiProName}/action/${globSetting.apiVersion}/mappings/action`,
		stageAction: `${globSetting.apiProName}/action/${globSetting.apiVersion}/mappings/trigger-source`,

		mappingAction: `${globSetting.apiProName}/action/${globSetting.apiVersion}/mappings`,

		actionResult: `${globSetting.apiProName}/action/${globSetting.apiVersion}/executions/trigger-source/${id}/search`,

		// Change History API
		actionChangeHistory: `${globSetting.apiProName}/ow/change-logs/${globSetting.apiVersion}/action`,
	};
};

export function addAction(data: ActionDefinition) {
	return defHttp.post({
		url: Api().action,
		data,
	});
}

/**
 * Get Actions list
 */
export function getActionDefinitions(params: ActionQueryRequest) {
	return defHttp.get({
		url: Api().action,
		params,
	});
}

/**
 * Delete Action
 */
export function deleteAction(id: string) {
	return defHttp.delete({
		url: `${Api().action}/${id}`,
	});
}

/**
 * Get system predefined actions
 */
export function getSystemPredefinedActions() {
	return defHttp.get<SystemActionDefinitionDto[]>({
		url: `${Api().action}/system/predefined`,
	});
}

/**
 * Get system action configuration template
 */
export function getSystemActionTemplate(actionName: string) {
	return defHttp.get<SystemActionTemplateDto>({
		url: `${Api().action}/system/template/${actionName}`,
	});
}

/**
 * Export Actions
 */
export function exportActions(params: ActionQueryRequest) {
	return defHttp.get({
		url: `${Api().action}/export`,
		params,
		responseType: 'blob',
	});
}

/**
 * Get Action Detail
 */
export function getActionDetail(id: string) {
	return defHttp.get({
		url: `${Api().action}/${id}`,
	});
}

/**
 * Update Action
 */
export function updateAction(id: string, data: Partial<ActionDefinition>) {
	return defHttp.put({
		url: `${Api().action}/${id}`,
		data,
	});
}

export function testRunActionNoId(data: {
	actionType: number;
	actionConfig: string;
	contextData?: string;
}) {
	return defHttp.post({
		url: `${Api().textRun}`,
		data,
	});
}

/**
 * Test Action
 */
export function testAction(id: string) {
	return defHttp.post({
		url: `${Api().action}/${id}/test`,
	});
}

export function getActionList(stageId: string) {
	return defHttp.get({
		url: `${Api().actionList}/${stageId}`,
	});
}

export function getStageAction(stageId: string) {
	return defHttp.get({
		url: `${Api().stageAction}/${stageId}`,
	});
}

export function deleteMappingAction(id: string) {
	return defHttp.delete({
		url: `${Api().mappingAction}/${id}`,
	});
}

export function addMappingAction(data: any) {
	return defHttp.post({
		url: `${Api().mappingAction}`,
		data,
	});
}

export function getActionResult(
	id: string,
	params: {
		pageIndex: number;
		pageSize: number;
		jsonConditions: { jsonPath: string; operator: string; value: string }[];
	}
) {
	return defHttp.post({
		url: `${Api(id).actionResult}`,
		params,
	});
}

/**
 * Get Action Change History
 */
export function getActionChangeHistory(
	actionId: string,
	params: {
		pageIndex: number;
		pageSize: number;
	}
) {
	return defHttp.get<{
		data: {
			items: ActionChangeHistoryItem[];
			totalCount: number;
			pageIndex: number;
			pageSize: number;
		};
		success: boolean;
		code: string;
		msg?: string;
	}>({
		url: `${Api().actionChangeHistory}/${actionId}`,
		params,
	});
}
