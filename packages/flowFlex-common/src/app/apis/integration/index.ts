/**
 * Integration Settings - API 接口定义
 * 所有与集成设置相关的 API 请求
 */

import { defHttp } from '@/apis/axios';
import type {
	IIntegrationConfig,
	ICreateIntegrationRequest,
	IUpdateIntegrationRequest,
	ITestConnectionResponse,
	IFieldMapping,
	IAction,
} from '#/integration';

const API_PREFIX = '/api/integrations';

// 是否使用 Mock 数据（开发环境可设置为 true）
const USE_MOCK = true;

// Mock 数据
const mockIntegrations: IIntegrationConfig[] = [
	{
		id: 'int-001',
		type: 'salesforce',
		name: 'Salesforce Production',
		status: 'connected',
		connection: {
			systemName: 'Salesforce Production',
			endpointUrl: 'https://api.salesforce.com/v1',
			authMethod: 'api_key',
			credentials: {
				apiKey: '***************',
			},
		},
		inboundSettings: {
			entityMappings: [
				{
					id: 'em-001',
					crmEntity: 'Account',
					wfeEntity: 'customer',
					workflows: ['wf-1', 'wf-2'],
				},
				{
					id: 'em-002',
					crmEntity: 'Lead',
					wfeEntity: 'lead',
					workflows: ['wf-3'],
				},
			],
			fieldMappings: [
				{
					id: 'fm-001',
					crmField: 'AccountName',
					wfeField: 'name',
					type: 'text',
					syncDirection: 'editable',
					workflows: ['wf-1'],
				},
				{
					id: 'fm-002',
					crmField: 'Email',
					wfeField: 'email',
					type: 'email',
					syncDirection: 'view_only',
					workflows: ['wf-1', 'wf-2'],
				},
				{
					id: 'fm-003',
					crmField: 'Phone',
					wfeField: 'phone',
					type: 'phone',
					syncDirection: 'editable',
					workflows: ['wf-1'],
				},
			],
			attachmentSharing: [
				{
					id: 'as-001',
					module: 'Documents',
					workflows: ['wf-1', 'wf-2'],
				},
			],
		},
		outboundSettings: {
			masterData: ['cases', 'customers', 'leads'],
			fields: ['name', 'email', 'phone', 'status'],
			attachmentWorkflows: ['wf-1', 'wf-2'],
		},
		actions: [
			{
				id: 'act-001',
				name: 'Create Case in Salesforce',
				type: 'create',
				status: 'active',
				workflows: ['wf-1'],
			},
			{
				id: 'act-002',
				name: 'Update Customer Info',
				type: 'update',
				status: 'active',
				workflows: ['wf-1', 'wf-2'],
			},
		],
		createdAt: '2024-01-15T10:30:00Z',
		updatedAt: '2024-03-20T14:45:00Z',
	},
	{
		id: 'int-002',
		type: 'hubspot',
		name: 'HubSpot Marketing',
		status: 'disconnected',
		connection: {
			systemName: 'HubSpot Marketing',
			endpointUrl: 'https://api.hubapi.com/v3',
			authMethod: 'bearer',
			credentials: {
				token: '***************',
			},
		},
		inboundSettings: {
			entityMappings: [
				{
					id: 'em-003',
					crmEntity: 'Contact',
					wfeEntity: 'contact',
					workflows: ['wf-2'],
				},
			],
			fieldMappings: [
				{
					id: 'fm-004',
					crmField: 'firstname',
					wfeField: 'name',
					type: 'text',
					syncDirection: 'view_only',
					workflows: ['wf-2'],
				},
			],
			attachmentSharing: [],
		},
		outboundSettings: {
			masterData: ['contacts', 'leads'],
			fields: ['name', 'email'],
			attachmentWorkflows: [],
		},
		actions: [
			{
				id: 'act-003',
				name: 'Sync Contact to HubSpot',
				type: 'sync',
				status: 'inactive',
				workflows: ['wf-2'],
			},
		],
		createdAt: '2024-02-10T09:15:00Z',
		updatedAt: '2024-02-10T09:15:00Z',
	},
	{
		id: 'int-003',
		type: 'zoho',
		name: 'Zoho CRM Integration',
		status: 'connected',
		connection: {
			systemName: 'Zoho CRM',
			endpointUrl: 'https://www.zohoapis.com/crm/v2',
			authMethod: 'basic',
			credentials: {
				username: 'admin@company.com',
				password: '***************',
			},
		},
		inboundSettings: {
			entityMappings: [],
			fieldMappings: [],
			attachmentSharing: [],
		},
		outboundSettings: {
			masterData: ['cases'],
			fields: ['name', 'status'],
			attachmentWorkflows: ['wf-3'],
		},
		actions: [],
		createdAt: '2024-03-01T11:20:00Z',
		updatedAt: '2024-03-15T16:30:00Z',
	},
];

let mockIdCounter = 4;

/**
 * Mock 延迟函数（模拟网络请求）
 */
const mockDelay = (ms: number = 500) => new Promise((resolve) => setTimeout(resolve, ms));

/**
 * 集成设置 API 类
 */
export class IntegrationAPI {
	/**
	 * 获取所有集成列表
	 */
	static async getIntegrations(): Promise<IIntegrationConfig[]> {
		if (USE_MOCK) {
			await mockDelay();
			return Promise.resolve([...mockIntegrations]);
		}
		return defHttp.get({ url: API_PREFIX });
	}

	/**
	 * 创建新集成
	 */
	static async createIntegration(data: ICreateIntegrationRequest): Promise<IIntegrationConfig> {
		if (USE_MOCK) {
			await mockDelay();
			const newIntegration: IIntegrationConfig = {
				id: `int-${String(mockIdCounter++).padStart(3, '0')}`,
				type: data.type,
				name: data.name,
				status: 'disconnected',
				connection: {
					systemName: '',
					endpointUrl: '',
					authMethod: 'api_key',
					credentials: {},
				},
				inboundSettings: {
					entityMappings: [],
					fieldMappings: [],
					attachmentSharing: [],
				},
				outboundSettings: {
					masterData: [],
					fields: [],
					attachmentWorkflows: [],
				},
				actions: [],
				createdAt: new Date().toISOString(),
				updatedAt: new Date().toISOString(),
			};
			mockIntegrations.push(newIntegration);
			return Promise.resolve(newIntegration);
		}
		return defHttp.post({ url: API_PREFIX, data });
	}

	/**
	 * 获取单个集成详情
	 */
	static async getIntegration(id: string): Promise<IIntegrationConfig> {
		if (USE_MOCK) {
			await mockDelay();
			const integration = mockIntegrations.find((item) => item.id === id);
			if (!integration) {
				return Promise.reject(new Error('Integration not found'));
			}
			return Promise.resolve({ ...integration });
		}
		return defHttp.get({ url: `${API_PREFIX}/${id}` });
	}

	/**
	 * 更新集成配置
	 */
	static async updateIntegration(
		id: string,
		data: IUpdateIntegrationRequest
	): Promise<IIntegrationConfig> {
		if (USE_MOCK) {
			await mockDelay();
			const index = mockIntegrations.findIndex((item) => item.id === id);
			if (index === -1) {
				return Promise.reject(new Error('Integration not found'));
			}
			mockIntegrations[index] = {
				...mockIntegrations[index],
				...data,
				updatedAt: new Date().toISOString(),
			};
			return Promise.resolve({ ...mockIntegrations[index] });
		}
		return defHttp.put({ url: `${API_PREFIX}/${id}`, data });
	}

	/**
	 * 删除集成
	 */
	static async deleteIntegration(id: string): Promise<void> {
		if (USE_MOCK) {
			await mockDelay();
			const index = mockIntegrations.findIndex((item) => item.id === id);
			if (index === -1) {
				return Promise.reject(new Error('Integration not found'));
			}
			mockIntegrations.splice(index, 1);
			return Promise.resolve();
		}
		return defHttp.delete({ url: `${API_PREFIX}/${id}` });
	}

	/**
	 * 测试连接
	 */
	static async testConnection(id: string): Promise<ITestConnectionResponse> {
		if (USE_MOCK) {
			await mockDelay(1000);
			const integration = mockIntegrations.find((item) => item.id === id);
			if (!integration) {
				return Promise.reject(new Error('Integration not found'));
			}

			// 模拟随机成功/失败
			const isSuccess = Math.random() > 0.3;

			if (isSuccess) {
				// 更新状态为已连接
				integration.status = 'connected';
				return Promise.resolve({
					success: true,
					message: 'Connection test successful! All systems are operational.',
					details: {
						responseTime: '245ms',
						apiVersion: 'v2.1',
						timestamp: new Date().toISOString(),
					},
				});
			} else {
				return Promise.resolve({
					success: false,
					message:
						'Connection test failed. Please check your credentials and endpoint URL.',
					details: {
						error: 'Authentication failed',
						errorCode: 'AUTH_001',
					},
				});
			}
		}
		return defHttp.post({ url: `${API_PREFIX}/${id}/test` });
	}

	/**
	 * 获取字段映射列表
	 */
	static async getFieldMappings(id: string): Promise<IFieldMapping[]> {
		if (USE_MOCK) {
			await mockDelay();
			const integration = mockIntegrations.find((item) => item.id === id);
			if (!integration) {
				return Promise.reject(new Error('Integration not found'));
			}
			return Promise.resolve([...(integration.inboundSettings?.fieldMappings || [])]);
		}
		return defHttp.get({ url: `${API_PREFIX}/${id}/field-mappings` });
	}

	/**
	 * 创建字段映射
	 */
	static async createFieldMapping(
		id: string,
		data: Omit<IFieldMapping, 'id'>
	): Promise<IFieldMapping> {
		if (USE_MOCK) {
			await mockDelay();
			const integration = mockIntegrations.find((item) => item.id === id);
			if (!integration) {
				return Promise.reject(new Error('Integration not found'));
			}
			const newMapping: IFieldMapping = {
				...data,
				id: `fm-${Date.now()}`,
			};
			integration.inboundSettings.fieldMappings.push(newMapping);
			return Promise.resolve(newMapping);
		}
		return defHttp.post({ url: `${API_PREFIX}/${id}/field-mappings`, data });
	}

	/**
	 * 更新字段映射
	 */
	static async updateFieldMapping(
		id: string,
		fieldId: string,
		data: Partial<IFieldMapping>
	): Promise<IFieldMapping> {
		if (USE_MOCK) {
			await mockDelay();
			const integration = mockIntegrations.find((item) => item.id === id);
			if (!integration) {
				return Promise.reject(new Error('Integration not found'));
			}
			const fieldIndex = integration.inboundSettings.fieldMappings.findIndex(
				(f) => f.id === fieldId
			);
			if (fieldIndex === -1) {
				return Promise.reject(new Error('Field mapping not found'));
			}
			integration.inboundSettings.fieldMappings[fieldIndex] = {
				...integration.inboundSettings.fieldMappings[fieldIndex],
				...data,
			};
			return Promise.resolve({ ...integration.inboundSettings.fieldMappings[fieldIndex] });
		}
		return defHttp.put({
			url: `${API_PREFIX}/${id}/field-mappings/${fieldId}`,
			data,
		});
	}

	/**
	 * 删除字段映射
	 */
	static async deleteFieldMapping(id: string, fieldId: string): Promise<void> {
		if (USE_MOCK) {
			await mockDelay();
			const integration = mockIntegrations.find((item) => item.id === id);
			if (!integration) {
				return Promise.reject(new Error('Integration not found'));
			}
			const fieldIndex = integration.inboundSettings.fieldMappings.findIndex(
				(f) => f.id === fieldId
			);
			if (fieldIndex === -1) {
				return Promise.reject(new Error('Field mapping not found'));
			}
			integration.inboundSettings.fieldMappings.splice(fieldIndex, 1);
			return Promise.resolve();
		}
		return defHttp.delete({
			url: `${API_PREFIX}/${id}/field-mappings/${fieldId}`,
		});
	}

	/**
	 * 获取动作列表
	 */
	static async getActions(id: string): Promise<IAction[]> {
		if (USE_MOCK) {
			await mockDelay();
			const integration = mockIntegrations.find((item) => item.id === id);
			if (!integration) {
				return Promise.reject(new Error('Integration not found'));
			}
			return Promise.resolve([...(integration.actions || [])]);
		}
		return defHttp.get({ url: `${API_PREFIX}/${id}/actions` });
	}
}

// 导出便捷方法
export const {
	getIntegrations,
	createIntegration,
	getIntegration,
	updateIntegration,
	deleteIntegration,
	testConnection,
	getFieldMappings,
	createFieldMapping,
	updateFieldMapping,
	deleteFieldMapping,
	getActions,
} = IntegrationAPI;
