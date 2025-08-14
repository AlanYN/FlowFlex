import { defHttp } from '@/apis/axios';

// Types
export interface AIWorkflow {
	id?: number;
	name: string;
	description: string;
	stages: AIWorkflowStage[];
	isActive?: boolean;
}

export interface AIWorkflowStage {
	name: string;
	description: string;
	order: number;
	assignedGroup: string;
	requiredFields: string[];
	estimatedDuration: number;
}

export interface AIWorkflowGenerationResult {
	generatedWorkflow: AIWorkflow;
	stages: AIWorkflowStage[];
	operationMode: string;
	selectedWorkflowId?: number;
}

export interface AIWorkflowEnhancementResult {
	suggestions: string[];
	enhancedStages?: AIWorkflowStage[];
}

export interface AIWorkflowValidationResult {
	isValid: boolean;
	qualityScore: number;
	issues: ValidationIssue[];
}

export interface ValidationIssue {
	severity: 'Error' | 'Warning' | 'Info';
	message: string;
	stageIndex?: number;
}

export interface ApiResponse<T = any> {
	success: boolean;
	data: T;
	message?: string;
}

// API Functions
export const generateAIWorkflow = (
	prompt: string
): Promise<ApiResponse<AIWorkflowGenerationResult>> => {
	return defHttp.post({
		url: '/api/ai/workflow/generate',
		data: { prompt },
	});
};

export const enhanceAIWorkflow = (
	workflowId: number,
	enhancementRequest: string
): Promise<ApiResponse<AIWorkflowEnhancementResult>> => {
	return defHttp.post({
		url: `/api/ai/workflow/${workflowId}/enhance`,
		data: { enhancementRequest },
	});
};

export const validateAIWorkflow = (
	workflow: AIWorkflow
): Promise<ApiResponse<AIWorkflowValidationResult>> => {
	return defHttp.post({
		url: '/api/ai/workflow/validate',
		data: workflow,
	});
};

export const getAIWorkflowStatus = (): Promise<ApiResponse<any>> => {
	return defHttp.get({
		url: '/api/ai/workflow/status',
	});
};

export const parseAIRequirements = (content: string): Promise<ApiResponse<any>> => {
	return defHttp.post({
		url: '/api/ai/workflow/parse-requirements',
		data: { content },
	});
};

export const chatWithAI = (
	message: string,
	conversationId?: string
): Promise<ApiResponse<{ response: string; conversationId: string }>> => {
	return defHttp.post({
		url: '/api/ai/chat',
		data: {
			message,
			conversationId,
			context: 'workflow-assistant',
		},
	});
};
