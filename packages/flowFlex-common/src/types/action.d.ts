import { ActionType } from '@/apis/action';

export interface ActionDefinition {
	id?: string;
	actionCode?: string;
	name: string;
	description: string;
	actionType: number;
	actionConfig: string;
	isEnabled?: boolean;
	isAIGenerated?: boolean;
	actionTriggerType?: string; // Action本身的触发类型(Stage, Task, Question, Workflow)
	createdAt?: string;
	updatedAt?: string;
	triggerMappings?: TriggerMapping[];
	workflowId: string | null;
	triggerSourceId: string | null;
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
	isSystemTools?: boolean;
	actionIds?: string; // Comma-separated list of action IDs for selected export
	integrationId?: string;
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
	actionType: ActionType;
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

// Action execution result interface
export interface ActionExecutionResult {
	id: string;
	actionDefinitionId: string;
	actionCode: string;
	executionId: string;
	actionTriggerMappingId: string;
	actionName: string;
	actionType: string;
	triggerContext: string;
	executionStatus: string;
	startedAt: string;
	completedAt: string;
	executionInput: string;
	executionOutput: string;
	errorMessage: string;
	errorStackTrace: string;
	executorInfo: string;
	createdAt: string;
	createdBy: string;
	// Computed properties
	status?: string; // Derived from executionStatus
	duration?: number; // Computed from startedAt and completedAt
	triggerSource?: string; // Derived from triggerContext
}

// System action definition interface
export interface SystemActionDefinitionDto {
	actionName: string;
	displayName: string;
	description: string;
	configSchema: object;
	exampleConfig: string;
	triggerType: TriggerTypeEnum;
}

// System action template interface
export interface SystemActionTemplateDto {
	actionName: string;
	template: string;
	parameters: SystemActionParameterDto[];
}

// System action parameter interface
export interface SystemActionParameterDto {
	name: string;
	type: string;
	required: boolean;
	description: string;
	defaultValue?: any;
}
