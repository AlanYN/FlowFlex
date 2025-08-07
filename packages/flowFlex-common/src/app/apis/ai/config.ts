import { defHttp } from '@/apis/axios';
import { useGlobSetting } from '@/settings';

const globSetting = useGlobSetting();

// API endpoints
export const apiEndpoints = {
	models: `${globSetting.apiProName}/ai/config/${globSetting.apiVersion}/models`,
	defaultModel: `${globSetting.apiProName}/ai/config/${globSetting.apiVersion}/models/default`,
	testConnection: `${globSetting.apiProName}/ai/config/${globSetting.apiVersion}/models/test`,
	providers: `${globSetting.apiProName}/ai/config/${globSetting.apiVersion}/providers`,
};

/**
 * 通用API响应接口
 */
export interface ApiResponse<T = any> {
	success: boolean;
	data: T;
	message?: string;
	code?: number;
}

/**
 * AI模型配置接口
 */
export interface AIModelConfig {
	id: number;
	userId: number;
	provider: string;
	apiKey: string;
	baseUrl: string;
	apiVersion?: string;
	modelName: string;
	temperature?: number;
	maxTokens?: number;
	enableStreaming: boolean;
	isDefault: boolean;
	isAvailable: boolean;
	lastCheckTime?: string;
	remarks?: string;
	createdTime: string;
	updatedTime: string;
}

/**
 * AI模型测试结果接口
 */
export interface AIModelTestResult {
	success: boolean;
	message: string;
	modelInfo: string;
	responseTime: number;
}

/**
 * AI提供商信息接口
 */
export interface AIProviderInfo {
	name: string;
	displayName: string;
	icon: string;
	description: string;
	website: string;
	supportedModels: string[];
}

/**
 * 获取用户的AI模型配置列表
 */
export function getUserAIModels() {
	return defHttp.get<ApiResponse<AIModelConfig[]>>({
		url: apiEndpoints.models,
	});
}

/**
 * 获取用户的默认AI模型配置
 */
export function getDefaultAIModel() {
	return defHttp.get<ApiResponse<AIModelConfig>>({
		url: apiEndpoints.defaultModel,
	});
}

/**
 * 创建AI模型配置
 */
export function createAIModel(config: Partial<AIModelConfig>) {
	return defHttp.post<ApiResponse<number>>({
		url: apiEndpoints.models,
		data: config,
	});
}

/**
 * 更新AI模型配置
 */
export function updateAIModel(config: AIModelConfig) {
	return defHttp.put<ApiResponse<boolean>>({
		url: `${apiEndpoints.models}/${config.id}`,
		data: config,
	});
}

/**
 * 删除AI模型配置
 */
export function deleteAIModel(configId: number) {
	return defHttp.delete<ApiResponse<boolean>>({
		url: `${apiEndpoints.models}/${configId}`,
	});
}

/**
 * 测试AI模型连接
 */
export function testAIModelConnection(config: AIModelConfig) {
	return defHttp.post<ApiResponse<AIModelTestResult>>({
		url: apiEndpoints.testConnection,
		data: config,
	});
}

/**
 * 设置默认AI模型
 */
export function setDefaultAIModel(configId: number) {
	return defHttp.put<ApiResponse<boolean>>({
		url: `${apiEndpoints.models}/${configId}/default`,
	});
}

/**
 * 获取支持的AI提供商列表
 */
export function getAIProviders() {
	return defHttp.get<ApiResponse<AIProviderInfo[]>>({
		url: apiEndpoints.providers,
	});
}
