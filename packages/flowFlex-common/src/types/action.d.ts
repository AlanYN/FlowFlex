export interface ActionDefinition {
	id?: string;
	actionCode?: string;
	name: string;
	description: string;
	actionType: number;
	actionConfig: string;
	isEnabled?: boolean;
	createdAt?: string;
	updatedAt?: string;
	triggerMappings?: TriggerMapping[];
	workflowId: string | null;
	triggerSourceId: string | null;
	triggerType: number | null;
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
	isTools?: boolean;
}

export interface ApiResponse<T> {
	data: T;
	success: boolean;
	msg: string;
	code: string;
}

export interface ActionConfig {
	sourceCode?: string;
	url?: string;
	method?: string;
	headers?: Record<string, string>;
	body?: string;
	timeout?: number;
	followRedirects?: boolean;
	[key: string]: any;
}

export interface ActionItem {
	id: string;
	name: string;
	type: 'python' | 'http';
	description: string;
	condition: string;
	actionConfig: ActionConfig;
	isTools: boolean;
}

export interface ActionListItem {
	actionCode: string;
	actionConfig: ActionConfig;
	actionType: number | string;
	createdAt: string;
	description: string;
	id: string;
	isEnabled: boolean;
	name: string;
	triggerMappings: [];
	updatedAt: string;
	actionName?: string;
	actionDefinitionId?: string;
}
