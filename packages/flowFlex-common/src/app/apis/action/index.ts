import { defHttp } from '@/apis/axios';

// Action Type Enum
export enum ActionType {
	PYTHON_SCRIPT = 1,
	HTTP_API = 2,
	SEND_EMAIL = 3,
}

// Action Type Mapping
export const ACTION_TYPE_MAPPING = {
	[ActionType.PYTHON_SCRIPT]: 'Python Script',
	[ActionType.HTTP_API]: 'HTTP API',
	[ActionType.SEND_EMAIL]: 'Send Email',
} as const;

// Frontend to Backend Type Mapping
export const FRONTEND_TO_BACKEND_TYPE_MAPPING = {
	'python': 'PythonScript',
	'http': 'HttpApi',
	'email': 'SendEmail',
} as const;

export interface ActionDefinition {
	id: string;
	actionCode: string;
	name: string;
	description: string;
	actionType: number;
	actionConfig: string;
	isEnabled: boolean;
	createdAt: string;
	updatedAt: string;
	triggerMappings: TriggerMapping[];
}

export interface TriggerMapping {
	id: string;
	triggerType: string;
	triggerSourceId: string;
	triggerSourceName: string;
	workFlowId: string;
	workFlowName: string;
	stageId: string;
	stageName: string;
	triggerEvent: string;
	isEnabled: boolean;
	executionOrder: number;
	description: string;
	lastApplied: string;
}

export interface ActionQueryRequest {
	search?: string;
	actionType?: string;
	isAssignmentWorkflow?: boolean;
	isAssignmentStage?: boolean;
	isAssignmentChecklist?: boolean;
	isAssignmentQuestionnaire?: boolean;
	pageIndex?: number;
	pageSize?: number;
}

export interface ActionQueryResponse {
	totalPage: number;
	pageCount: number;
	pageIndex: number;
	pageSize: number;
	total: number;
	dataCount: number;
	data: ActionDefinition[];
}

export interface ApiResponse<T> {
	data: T;
	success: boolean;
	msg: string;
	code: string;
}

/**
 * Get Actions list
 */
export function getActionDefinitions(params: ActionQueryRequest) {
	return defHttp.get({ 
		url: '/api/action/v1/definitions',
		params,
	});
}

/**
 * Delete Action
 */
export function deleteAction(id: string) {
	return defHttp.delete({ 
		url: `/api/action/v1/definitions/${id}`,
	});
}

/**
 * Export Actions
 */
export function exportActions(params: ActionQueryRequest) {
	return defHttp.get({ 
		url: '/api/action/v1/definitions/export',
		params,
		responseType: 'blob',
	});
}