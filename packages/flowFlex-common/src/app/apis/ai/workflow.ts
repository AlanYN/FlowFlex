import { defHttp } from '@/apis/axios';
import { useGlobSetting } from '@/settings';

const globSetting = useGlobSetting();

// AI API路径配置
const Api = (id?: string | number) => {
	return {
		// AI工作流生成API
		aiWorkflowGenerate: `${globSetting.apiProName}/ai/workflows/${globSetting.apiVersion}/generate`,
		aiWorkflowGenerateStream: `${globSetting.apiProName}/ai/workflows/${globSetting.apiVersion}/generate/stream`,
		aiWorkflowEnhance: `${globSetting.apiProName}/ai/workflows/${globSetting.apiVersion}/${id}/enhance`,
		aiWorkflowModify: `${globSetting.apiProName}/ai/workflows/${globSetting.apiVersion}/${id}/modify`,
		aiWorkflowValidate: `${globSetting.apiProName}/ai/workflows/${globSetting.apiVersion}/validate`,
		aiWorkflowParseRequirements: `${globSetting.apiProName}/ai/workflows/${globSetting.apiVersion}/parse-requirements`,
		aiWorkflowStatus: `${globSetting.apiProName}/ai/workflows/${globSetting.apiVersion}/status`,
		
		// AI对话API
		aiChat: `${globSetting.apiProName}/ai/chat/${globSetting.apiVersion}/conversation`,
		aiChatStream: `${globSetting.apiProName}/ai/chat/${globSetting.apiVersion}/conversation/stream`,

		// MCP服务API
		mcpStoreContext: `${globSetting.apiProName}/mcp/${globSetting.apiVersion}/contexts`,
		mcpGetContext: `${globSetting.apiProName}/mcp/${globSetting.apiVersion}/contexts/${id}`,
		mcpSearchContexts: `${globSetting.apiProName}/mcp/${globSetting.apiVersion}/contexts/search`,
		mcpCreateEntity: `${globSetting.apiProName}/mcp/${globSetting.apiVersion}/entities`,
		mcpCreateRelationship: `${globSetting.apiProName}/mcp/${globSetting.apiVersion}/relationships`,
		mcpQueryGraph: `${globSetting.apiProName}/mcp/${globSetting.apiVersion}/graph/query`,
		mcpGenerateWorkflow: `${globSetting.apiProName}/mcp/${globSetting.apiVersion}/tools/generate-workflow`,
		mcpGenerateQuestionnaire: `${globSetting.apiProName}/mcp/${globSetting.apiVersion}/tools/generate-questionnaire`,
		mcpGenerateChecklist: `${globSetting.apiProName}/mcp/${globSetting.apiVersion}/tools/generate-checklist`,
		mcpStatus: `${globSetting.apiProName}/mcp/${globSetting.apiVersion}/status`,
	};
};

// ========================= AI工作流生成接口 =========================

/**
 * 生成工作流
 * @param params AI工作流生成输入参数
 * @returns 生成结果
 */
export function generateAIWorkflow(params: AIWorkflowGenerationInput) {
	return defHttp.post({ url: Api().aiWorkflowGenerate, params });
}

/**
 * 流式生成工作流
 * @param params AI工作流生成输入参数
 * @returns 流式生成结果
 */
export function streamGenerateAIWorkflow(params: AIWorkflowGenerationInput) {
	return defHttp.post({ 
		url: Api().aiWorkflowGenerateStream, 
		params,
		responseType: 'stream'
	});
}

/**
 * 增强现有工作流
 * @param workflowId 工作流ID
 * @param enhancement 增强描述
 * @returns 增强建议
 */
export function enhanceAIWorkflow(workflowId: string | number, enhancement: string) {
	return defHttp.post({ 
		url: Api(workflowId).aiWorkflowEnhance, 
		params: { enhancement } 
	});
}

/**
 * 验证工作流
 * @param workflow 工作流数据
 * @returns 验证结果
 */
export function validateAIWorkflow(workflow: any) {
	return defHttp.post({ url: Api().aiWorkflowValidate, params: workflow });
}

/**
 * 解析自然语言需求
 * @param naturalLanguage 自然语言描述
 * @returns 结构化需求
 */
export function parseAIRequirements(naturalLanguage: string) {
	return defHttp.post({ 
		url: Api().aiWorkflowParseRequirements, 
		params: { naturalLanguage } 
	});
}

/**
 * 获取AI服务状态
 * @returns AI服务状态
 */
export function getAIWorkflowStatus() {
	return defHttp.get({ url: Api().aiWorkflowStatus });
}

/**
 * 获取可用的工作流列表
 * @returns 工作流列表
 */
export function getAvailableWorkflows() {
	return defHttp.get({ url: `${globSetting.apiProName}/ow/workflows/${globSetting.apiVersion}` });
}

/**
 * 获取工作流详情
 * @param workflowId 工作流ID
 * @returns 工作流详情
 */
export function getWorkflowDetails(workflowId: number) {
	return defHttp.get({ url: Api().getWorkflows + `/${workflowId}` });
}

// ========================= AI对话接口 =========================

/**
 * AI对话消息类型
 */
export interface AIChatMessage {
	role: 'user' | 'assistant' | 'system';
	content: string;
	timestamp?: string;
}

/**
 * AI对话输入参数
 */
export interface AIChatInput {
	messages: AIChatMessage[];
	context?: string;
	sessionId?: string;
	mode?: 'workflow_planning' | 'general';
}

/**
 * AI对话响应
 */
export interface AIChatResponse {
	success: boolean;
	message: string;
	response: {
		content: string;
		suggestions?: string[];
		isComplete?: boolean;
		nextQuestions?: string[];
	};
	sessionId: string;
}

/**
 * 发送AI对话消息
 * @param params AI对话输入参数
 * @returns 对话响应
 */
export function sendAIChatMessage(params: AIChatInput) {
	console.log('📡 API: Sending AI chat message to:', Api().aiChat);
	console.log('📡 API: Chat params:', params);
	
	return defHttp.post<AIChatResponse>({ url: Api().aiChat, params })
		.then(response => {
			console.log('📡 API: AI chat response received:', response);
			// 后端返回的是标准API响应格式 { data: AIChatResponse, success: boolean, ... }
			// 但defHttp已经解包了data字段，所以response就是AIChatResponse
			return response;
		})
		.catch(error => {
			console.error('📡 API: AI chat request failed:', error);
			throw error;
		});
}

/**
 * 流式AI对话
 * @param params AI对话输入参数
 * @returns 流式对话响应
 */
export function streamAIChatMessage(params: AIChatInput) {
	return defHttp.post({ 
		url: Api().aiChatStream, 
		params,
		responseType: 'stream'
	});
}

/**
 * 修改现有工作流
 * @param params 工作流修改输入参数
 * @returns 修改结果
 */
export function modifyAIWorkflow(params: AIWorkflowModificationInput) {
	return defHttp.post({ url: Api(params.workflowId).aiWorkflowModify, params });
}

// ========================= MCP服务接口 =========================

/**
 * 存储上下文信息
 * @param contextId 上下文ID
 * @param content 内容
 * @param metadata 元数据
 * @returns 成功状态
 */
export function storeMCPContext(contextId: string, content: string, metadata?: Record<string, any>) {
	return defHttp.post({ 
		url: Api().mcpStoreContext, 
		params: { contextId, content, metadata } 
	});
}

/**
 * 获取上下文信息
 * @param contextId 上下文ID
 * @returns 上下文信息
 */
export function getMCPContext(contextId: string) {
	return defHttp.get({ url: Api(contextId).mcpGetContext });
}

/**
 * 搜索上下文
 * @param query 搜索查询
 * @param limit 结果限制
 * @returns 搜索结果
 */
export function searchMCPContexts(query: string, limit: number = 10) {
	return defHttp.get({ 
		url: Api().mcpSearchContexts, 
		params: { query, limit } 
	});
}

/**
 * 创建知识图谱实体
 * @param entity 实体数据
 * @returns 实体ID
 */
export function createMCPEntity(entity: MCPEntity) {
	return defHttp.post({ url: Api().mcpCreateEntity, params: entity });
}

/**
 * 创建实体关系
 * @param relationship 关系数据
 * @returns 关系ID
 */
export function createMCPRelationship(relationship: MCPRelationship) {
	return defHttp.post({ url: Api().mcpCreateRelationship, params: relationship });
}

/**
 * 查询知识图谱
 * @param query 图查询
 * @returns 查询结果
 */
export function queryMCPGraph(query: string) {
	return defHttp.post({ url: Api().mcpQueryGraph, params: { query } });
}

/**
 * MCP工具：生成工作流（带上下文记忆）
 * @param params 工作流生成请求
 * @returns 生成结果
 */
export function mcpGenerateWorkflow(params: MCPWorkflowGenerationRequest) {
	return defHttp.post({ url: Api().mcpGenerateWorkflow, params });
}

/**
 * MCP工具：生成问卷（带上下文记忆）
 * @param params 问卷生成请求
 * @returns 生成结果
 */
export function mcpGenerateQuestionnaire(params: MCPQuestionnaireGenerationRequest) {
	return defHttp.post({ url: Api().mcpGenerateQuestionnaire, params });
}

/**
 * MCP工具：生成检查清单（带上下文记忆）
 * @param params 检查清单生成请求
 * @returns 生成结果
 */
export function mcpGenerateChecklist(params: MCPChecklistGenerationRequest) {
	return defHttp.post({ url: Api().mcpGenerateChecklist, params });
}

/**
 * 获取MCP服务状态
 * @returns MCP服务状态
 */
export function getMCPStatus() {
	return defHttp.get({ url: Api().mcpStatus });
}

// ========================= 类型定义 =========================

export interface AIWorkflowGenerationInput {
	description: string;
	context?: string;
	requirements?: string[];
	industry?: string;
	processType?: string;
	includeApprovals?: boolean;
	includeNotifications?: boolean;
	estimatedDuration?: number;
}

export interface AIWorkflowGenerationResult {
	success: boolean;
	message: string;
	generatedWorkflow: any;
	stages: AIStageGenerationResult[];
	suggestions: string[];
	confidenceScore: number;
}

export interface AIStageGenerationResult {
	name: string;
	description: string;
	order: number;
	assignedGroup: string;
	requiredFields: string[];
	checklistIds: number[];
	questionnaireIds: number[];
	estimatedDuration: number;
}

export interface MCPEntity {
	id?: string;
	type: string;
	name: string;
	properties?: Record<string, any>;
	tags?: string[];
}

export interface MCPRelationship {
	id?: string;
	fromEntityId: string;
	toEntityId: string;
	relationType: string;
	properties?: Record<string, any>;
}

export interface MCPWorkflowGenerationRequest {
	description: string;
	context?: string;
	requirements?: string[];
	industry?: string;
	processType?: string;
	includeApprovals?: boolean;
	includeNotifications?: boolean;
	userId?: string;
	sessionId?: string;
}

export interface MCPQuestionnaireGenerationRequest {
	purpose: string;
	targetAudience?: string;
	topics?: string[];
	context?: string;
	estimatedQuestions?: number;
	includeValidation?: boolean;
	complexity?: string;
}

export interface MCPChecklistGenerationRequest {
	processName: string;
	description?: string;
	team?: string;
	requiredSteps?: string[];
	context?: string;
	includeDependencies?: boolean;
	includeEstimates?: boolean;
}

export interface AIWorkflowModificationInput {
	workflowId: number;
	description: string;
	context?: string;
	requirements?: string[];
	preserveExisting?: boolean;
	modificationMode?: 'add' | 'modify' | 'remove' | 'replace';
} 