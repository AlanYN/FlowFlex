import { defHttp } from '@/apis/axios';
import { useGlobSetting } from '@/settings';
import { getTokenobj } from '@/utils/auth';
import { useUserStoreWithOut } from '@/stores/modules/user';
import { getTimeZoneInfo } from '@/hooks/time';

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

		getWorkflows: `${globSetting.apiProName}/ow/workflows/${globSetting.apiVersion}`,
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
		responseType: 'stream',
	});
}

/**
 * 真正的流式生成工作流（使用原生fetch）
 * @param params AI工作流生成输入参数
 * @param onChunk 流式数据回调
 * @param onComplete 完成回调
 * @param onError 错误回调
 * @param abortController 可选的AbortController用于取消请求
 */
export async function streamGenerateAIWorkflowNative(
	params: AIWorkflowGenerationInput,
	onChunk: (chunk: string) => void,
	onComplete: (data: any) => void,
	onError: (error: any) => void,
	abortController?: AbortController
) {
	const { apiProName, apiVersion } = useGlobSetting();
	const url = `${apiProName}/ai/workflows/${apiVersion}/generate/stream`;

	console.log('🌐 Making stream request to:', url);
	console.log('📤 Request params:', params);

	// 获取认证信息
	const tokenObj = getTokenobj();
	const userStore = useUserStoreWithOut();
	const userInfo = userStore.getUserInfo;

	console.log('🔍 Debug - tokenObj:', tokenObj);
	console.log('🔍 Debug - userInfo:', userInfo);

	// 构建请求头
	const headers: Record<string, string> = {
		'Content-Type': 'application/json',
		Accept: 'text/event-stream',
		'Time-Zone': getTimeZoneInfo().timeZone,
		'Application-code': globSetting.ssoCode || '',
	};

	// 添加认证头
	if (tokenObj?.accessToken?.token) {
		const token = tokenObj.accessToken.token;
		const tokenType = tokenObj.accessToken.tokenType || 'Bearer';
		headers.Authorization = `${tokenType} ${token}`;
		console.log('✅ Added Authorization header:', `${tokenType} ${token.substring(0, 20)}...`);
	} else {
		console.warn('❌ No token found in tokenObj');
	}

	// 添加用户相关头信息
	if (userInfo?.appCode) {
		headers['X-App-Code'] = userInfo.appCode;
		console.log('✅ Added X-App-Code:', userInfo.appCode);
	}
	if (userInfo?.tenantId) {
		headers['X-Tenant-Id'] = userInfo.tenantId;
		console.log('✅ Added X-Tenant-Id:', userInfo.tenantId);
	}

	console.log('🔑 Final request headers:', headers);

	try {
		const response = await fetch(url, {
			method: 'POST',
			headers,
			body: JSON.stringify(params),
			signal: abortController?.signal,
		});

		console.log('📥 Stream response status:', response.status, response.statusText);

		if (!response.ok) {
			throw new Error(`HTTP error! status: ${response.status}`);
		}

		const reader = response.body?.getReader();
		if (!reader) {
			throw new Error('No response body reader available');
		}

		const decoder = new TextDecoder();
		let buffer = '';
		let finalData: any = null;
		let workflowData: any = null;
		const stagesData: any[] = [];

		try {
			while (true) {
				const { done, value } = await reader.read();

				if (done) {
					break;
				}

				// 解码数据块
				const chunk = decoder.decode(value, { stream: true });
				buffer += chunk;

				// 处理SSE格式的数据
				const lines = buffer.split('\n');
				buffer = lines.pop() || ''; // 保留不完整的行

				for (const line of lines) {
					if (line.startsWith('data: ')) {
						const data = line.slice(6); // 移除 'data: ' 前缀

						if (data === '[DONE]') {
							// 流式传输完成，构建最终数据
							if (finalData) {
								onComplete(finalData);
							} else if (workflowData || stagesData.length > 0) {
								// 从收集的数据构建最终结果
								const constructedData = {
									success: true,
									message: 'Workflow generated successfully',
									generatedWorkflow: workflowData,
									stages: stagesData,
									suggestions: [
										'Consider adding approval stages',
										'Review stage assignments',
									],
									confidenceScore: 0.8,
								};
								console.log(
									'🔧 Constructed final data from stream:',
									constructedData
								);
								onComplete(constructedData);
							}
							return;
						}

						try {
							const parsed = JSON.parse(data);
							const messageType = parsed.Type || parsed.type; // 支持大小写

							if (messageType === 'start' || messageType === 'progress') {
								// 开始和进度消息
								onChunk(parsed.Message || parsed.message || '');
							} else if (messageType === 'workflow') {
								// 工作流基本信息
								workflowData = parsed.Data || parsed.data;
								console.log('📋 Collected workflow data:', workflowData);
								const message = parsed.Message || parsed.message || '';
								if (message) {
									onChunk(message);
								}
							} else if (messageType === 'stage') {
								// 阶段信息 - 收集所有stage数据
								const stageData = parsed.Data || parsed.data;
								if (stageData) {
									stagesData.push(stageData);
									console.log(
										`📊 Collected stage ${stagesData.length}:`,
										stageData.Name
									);
								}
								const message = parsed.Message || parsed.message || '';
								if (message) {
									onChunk(message);
								}
							} else if (messageType === 'chunk' || messageType === 'delta') {
								// 流式数据块
								onChunk(
									parsed.Content ||
										parsed.content ||
										parsed.Message ||
										parsed.message ||
										''
								);
							} else if (messageType === 'complete') {
								// 最终结果
								finalData = parsed.Data || parsed.data || parsed;
								console.log('✅ Received complete data:', finalData);
							} else if (messageType === 'error') {
								// 错误信息
								onError(
									new Error(parsed.Message || parsed.message || 'Stream error')
								);
								return;
							}
						} catch (parseError) {
							// 如果不是JSON，可能是纯文本流式数据
							onChunk(data);
						}
					}
				}
			}

			// 如果没有收到完成信号，但流已结束
			if (finalData) {
				onComplete(finalData);
			} else if (workflowData || stagesData.length > 0) {
				// 从收集的数据构建最终结果
				const constructedData = {
					success: true,
					message: 'Workflow generated successfully',
					generatedWorkflow: workflowData,
					stages: stagesData,
					suggestions: ['Consider adding approval stages', 'Review stage assignments'],
					confidenceScore: 0.8,
				};
				console.log('🔧 Fallback: Constructed final data from stream:', constructedData);
				onComplete(constructedData);
			}
		} finally {
			reader.releaseLock();
		}
	} catch (error) {
		console.error('Stream API error:', error);
		onError(error);
	}
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
		params: { enhancement },
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
export function parseAIRequirements(
	naturalLanguage: string,
	opts?: { modelProvider?: string; modelName?: string; modelId?: string }
) {
	return defHttp.post({
		url: Api().aiWorkflowParseRequirements,
		params: { naturalLanguage, ...(opts || {}) },
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
 * 获取当前租户可用的AI模型配置列表
 */
export function getAIModels() {
	return defHttp.get({
		url: `${globSetting.apiProName}/ai/config/${globSetting.apiVersion}/models`,
	});
}

/**
 * 获取默认AI模型配置
 */
export function getDefaultAIModel() {
	return defHttp.get({
		url: `${globSetting.apiProName}/ai/config/${globSetting.apiVersion}/models/default`,
	});
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
	mode?: 'workflow_planning' | 'general' | 'generate_code';
	// 添加模型相关字段
	modelId?: string;
	modelProvider?: string;
	modelName?: string;
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

	return defHttp
		.post<AIChatResponse>({ url: Api().aiChat, params })
		.then((response) => {
			console.log('📡 API: AI chat response received:', response);
			// 后端返回的是标准API响应格式 { data: AIChatResponse, success: boolean, ... }
			// 但defHttp已经解包了data字段，所以response就是AIChatResponse
			return response;
		})
		.catch((error) => {
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
		responseType: 'stream',
	});
}

/**
 * 真正的流式聊天（使用原生fetch）
 * @param params AI聊天输入参数
 * @param onChunk 流式数据回调
 * @param onComplete 完成回调
 * @param onError 错误回调
 * @param abortController 可选的AbortController用于取消请求
 */
export async function streamAIChatMessageNative(
	params: AIChatInput,
	onChunk: (chunk: string) => void,
	onComplete: (data: any) => void,
	onError: (error: any) => void,
	abortController?: AbortController
) {
	const { apiProName, apiVersion } = useGlobSetting();
	const url = `${apiProName}/ai/chat/${apiVersion}/conversation/stream`;

	console.log('💬 Making stream chat request to:', url);
	console.log('📤 Chat params:', params);

	// 获取认证信息
	const tokenObj = getTokenobj();
	const userStore = useUserStoreWithOut();
	const userInfo = userStore.getUserInfo;

	console.log('🔍 Debug - tokenObj:', tokenObj);
	console.log('🔍 Debug - userInfo:', userInfo);

	// 构建请求头
	const headers: Record<string, string> = {
		'Content-Type': 'application/json',
		Accept: 'text/event-stream',
		'Time-Zone': getTimeZoneInfo().timeZone,
		'Application-code': globSetting.ssoCode || '',
	};

	// 添加认证头
	if (tokenObj?.accessToken?.token) {
		const token = tokenObj.accessToken.token;
		const tokenType = tokenObj.accessToken.tokenType || 'Bearer';
		headers.Authorization = `${tokenType} ${token}`;
		console.log('✅ Added Authorization header:', `${tokenType} ${token.substring(0, 20)}...`);
	} else {
		console.warn('❌ No token found in tokenObj');
	}

	// 添加用户相关头信息
	if (userInfo?.appCode) {
		headers['X-App-Code'] = userInfo.appCode;
		console.log('✅ Added X-App-Code:', userInfo.appCode);
	}
	if (userInfo?.tenantId) {
		headers['X-Tenant-Id'] = userInfo.tenantId;
		console.log('✅ Added X-Tenant-Id:', userInfo.tenantId);
	}

	console.log('🔑 Final chat request headers:', headers);

	try {
		const response = await fetch(url, {
			method: 'POST',
			headers,
			body: JSON.stringify(params),
			signal: abortController?.signal,
		});

		console.log('📥 Stream chat response status:', response.status, response.statusText);

		if (!response.ok) {
			throw new Error(`HTTP error! status: ${response.status}`);
		}

		const reader = response.body?.getReader();
		if (!reader) {
			throw new Error('No response body reader available');
		}

		const decoder = new TextDecoder();
		let buffer = '';
		let finalData: any = null;

		try {
			while (true) {
				const { done, value } = await reader.read();

				if (done) {
					break;
				}

				// 解码数据块
				const chunk = decoder.decode(value, { stream: true });
				buffer += chunk;

				// 处理SSE格式的数据
				const lines = buffer.split('\n');
				buffer = lines.pop() || ''; // 保留不完整的行

				for (const line of lines) {
					if (line.startsWith('data: ')) {
						const data = line.slice(6); // 移除 'data: ' 前缀

						if (data === '[DONE]') {
							// 流式传输完成
							if (finalData) {
								onComplete(finalData);
							}
							return;
						}

						try {
							const parsed = JSON.parse(data);

							if (
								parsed.type === 'chunk' ||
								parsed.type === 'content' ||
								parsed.type === 'delta'
							) {
								// 流式数据块
								onChunk(parsed.content || parsed.message || '');
							} else if (parsed.type === 'complete') {
								// 最终结果
								finalData = parsed.data || parsed;
							} else if (parsed.type === 'error') {
								// 错误信息
								onError(new Error(parsed.message || 'Stream error'));
								return;
							}
						} catch (parseError) {
							// 如果不是JSON，可能是纯文本流式数据
							onChunk(data);
						}
					}
				}
			}

			// 如果没有收到完成信号，但流已结束
			if (finalData) {
				onComplete(finalData);
			}
		} finally {
			reader.releaseLock();
		}
	} catch (error) {
		console.error('Stream chat API error:', error);
		onError(error);
	}
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
export function storeMCPContext(
	contextId: string,
	content: string,
	metadata?: Record<string, any>
) {
	return defHttp.post({
		url: Api().mcpStoreContext,
		params: { contextId, content, metadata },
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
		params: { query, limit },
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
	// AI模型信息
	modelId?: string;
	modelProvider?: string;
	modelName?: string;
	// 对话历史信息
	conversationHistory?: AIChatMessage[];
	sessionId?: string;
	// 额外的上下文信息
	conversationMetadata?: {
		totalMessages?: number;
		conversationStartTime?: string;
		conversationEndTime?: string;
		conversationMode?: string;
	};
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
