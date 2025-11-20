/**
 * Integration Settings - TypeScript 类型定义
 * 定义所有集成设置相关的数据结构
 */

// 认证方式类型
export type AuthMethod = 'api_key' | 'basic' | 'oauth2' | 'bearer';

// 同步方向类型
export type SyncDirection = 'view_only' | 'editable';

// 连接状态类型
export type ConnectionStatus = 'connected' | 'disconnected';

// 动作状态类型
export type ActionStatus = 'active' | 'inactive';

// 系统类型
export type SystemType = 'salesforce' | 'hubspot' | 'zoho' | 'dynamics' | 'custom';

/**
 * 连接配置接口
 */
export interface IConnectionConfig {
	systemName: string;
	endpointUrl: string;
	authMethod: AuthMethod;
	credentials: Record<string, any>;
}

/**
 * 实体映射接口
 */
export interface IEntityMapping {
	id?: string;
	crmEntity: string;
	wfeEntity: string;
	workflows: string[];
}

/**
 * 字段映射接口
 */
export interface IFieldMapping {
	id?: string;
	crmField: string;
	wfeField: string;
	type: string;
	syncDirection: SyncDirection;
	workflows: string[];
}

/**
 * 附件共享接口
 */
export interface IAttachmentSharing {
	id?: string;
	module: string;
	workflows: string[];
}

/**
 * 入站设置接口
 */
export interface IInboundSettings {
	entityMappings: IEntityMapping[];
	fieldMappings: IFieldMapping[];
	attachmentSharing: IAttachmentSharing[];
}

/**
 * 出站设置接口
 */
export interface IOutboundSettings {
	masterData: string[];
	fields: string[];
	attachmentWorkflows: string[];
}

/**
 * 动作接口
 */
export interface IAction {
	id: string;
	name: string;
	type: string;
	status: ActionStatus;
	workflows: string[];
}

/**
 * 集成配置主接口
 */
export interface IIntegrationConfig {
	id: string;
	type: SystemType;
	name: string;
	status: ConnectionStatus;
	connection: IConnectionConfig;
	inboundSettings: IInboundSettings;
	outboundSettings: IOutboundSettings;
	actions: IAction[];
	createdAt?: string;
	updatedAt?: string;
}

/**
 * 创建集成请求接口
 */
export interface ICreateIntegrationRequest {
	type: SystemType;
	name: string;
}

/**
 * 更新集成请求接口
 */
export interface IUpdateIntegrationRequest {
	name?: string;
	connection?: Partial<IConnectionConfig>;
	inboundSettings?: Partial<IInboundSettings>;
	outboundSettings?: Partial<IOutboundSettings>;
}

/**
 * 测试连接响应接口
 */
export interface ITestConnectionResponse {
	success: boolean;
	message: string;
	details?: Record<string, any>;
}

/**
 * 系统类型选项
 */
export interface ISystemTypeOption {
	value: SystemType;
	label: string;
	icon?: string;
	description?: string;
}

/**
 * 工作流选项
 */
export interface IWorkflowOption {
	id: string;
	name: string;
	type?: string;
}

/**
 * WFE 实体选项
 */
export interface IWfeEntityOption {
	value: string;
	label: string;
	type?: string;
}

/**
 * 字段选项
 */
export interface IFieldOption {
	value: string;
	label: string;
	type: string;
	category?: 'basic' | 'dynamic';
}
