/**
 * Integration Settings - TypeScript 类型定义
 * 定义所有集成设置相关的数据结构
 * 根据 Integration-API-Documentation.md 定义
 */

// ==================== 基础类型 ====================

/**
 * 系统类型（集成类型）
 */
export type SystemType = string; // CRM, ERP, Marketing, etc.

/**
 * 动作状态类型
 */
export type ActionStatus = 'active' | 'inactive';

/**
 * 连接配置接口
 */
export interface IConnectionConfig {
	systemName: string;
	endpointUrl: string;
	authMethod: AuthMethod | number; // 支持枚举和数字
	credentials: Record<string, any>;
}

/**
 * 实体映射接口
 */
export interface IEntityMapping {
	id?: string | number; // 实体映射 ID (long)
	integrationId?: string | number; // 集成 ID (long)
	externalEntityName: string; // 外部系统实体显示名称，最大 100 字符
	externalEntityType: string; // 外部系统实体技术标识符，最大 100 字符
	wfeEntityType: string; // WFE 实体类型，最大 100 字符
	workflowIds?: (string | number)[]; // 关联的工作流 ID 列表
	isActive?: boolean; // 是否激活（默认 true）
	createDate?: string; // 创建时间
	modifyDate?: string; // 修改时间
}

/**
 * 字段映射接口
 */
export interface IFieldMapping {
	id?: string | number; // 字段映射 ID (long)
	integrationId?: string | number; // 集成 ID (long)
	entityMappingId?: string | number; // 实体映射 ID (long)
	externalFieldName: string; // 外部系统字段名称，最大 100 字符
	wfeFieldId?: string | number; // WFE 字段 ID (long)
	syncDirection: SyncDirection | number; // 同步方向 (0=ViewOnly, 1=Editable, 2=OutboundOnly)
	isRequired?: boolean; // 是否必填
	defaultValue?: string; // 默认值
	transformRules?: Record<string, any>; // 转换规则（JSON 格式）
	createDate?: string; // 创建时间
	modifyDate?: string; // 修改时间
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
 * 集成配置主接口
 */
export interface IIntegrationConfig {
	authMethod: number;
	configuredEntityTypeNames: string[];
	configuredEntityTypes: number;
	createBy: string;
	createDate: string;
	endpointUrl: string;
	id: string | number;
	modifyBy: string;
	modifyDate: string;
	name: string;
	status: number;
	systemName: string;
	tenantId: string;
	type: SystemType;
	credentials: Record<string, any>; // 认证凭证（将被加密存储）
	// 详情接口返回的关联数据（可选）
	connection?: IConnectionConfig;
	entityMappings?: IEntityMapping[];
	inboundSettings?: IInboundSettings;
	outboundSettings?: IOutboundSettings;
	quickLinks?: IQuickLink[];
	inboundConfigurations?: IInboundConfiguration[];
	outboundConfigurations?: IOutboundConfiguration[];
}

/**
 * 创建集成请求接口
 */
export interface ICreateIntegrationRequest {
	type?: SystemType; // 集成类型 (CRM, ERP, Marketing, etc.)
	systemName: string; // 外部系统名称，最大 100 字符
	endpointUrl: string; // API 端点 URL，最大 500 字符
	authMethod: AuthMethod | number; // 认证方式 (0=ApiKey, 1=BasicAuth, 2=OAuth2, 3=BearerToken)
	credentials: Record<string, any>; // 认证凭证（将被加密存储）
	name?: string;
	status?: IntegrationStatus | number; // 连接状态 (0=Disconnected, 1=Connected, 2=Error, 3=InProgress)
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
	data?: boolean; // 连接测试结果
	message: string;
	details?: Record<string, any>;
	timestamp?: string;
}

/**
 * 分页响应接口
 */
export interface IPaginatedResponse<T> {
	items: T[];
	total: number;
	pageIndex: numbeIApiResponser;
	pageSize: number;
}

/**
 * API 标准响应接口
 */
export interface IApiResponse<T = any> {
	success: boolean;
	data: T;
	msg: string;
	code?: string;
	errorCode?: number;
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

/**
 * 获取集成列表查询参数
 */
export interface IGetIntegrationsParams {
	pageIndex?: number; // 页码（从 1 开始），默认 1
	pageSize?: number; // 每页数量，默认 15
	name?: string; // 按名称搜索
	type?: string; // 按类型筛选
	status?: string; // 按状态筛选
	sortField?: string; // 排序字段，默认 CreateDate
	sortDirection?: 'asc' | 'desc'; // 排序方向，默认 desc
}

/**
 * 快速链接接口
 */
export interface IQuickLink {
	id?: string | number; // 快速链接 ID (long)
	integrationId?: string | number; // 集成 ID (long)
	name: string; // 链接名称，最大 100 字符
	targetUrl: string; // 目标 URL 模板，最大 500 字符
	icon?: string; // 图标名称，最大 50 字符
	redirectType: RedirectType | number; // 重定向类型 (0=Direct, 1=PopupConfirmation)
	parameters?: Record<string, any>; // 参数映射配置
	createDate?: string; // 创建时间
	modifyDate?: string; // 修改时间
}

/**
 * 同步日志接口
 */
export interface ISyncLog {
	id: string | number; // 同步日志 ID (long)
	integrationId: string | number; // 集成 ID (long)
	syncDirection: 'Inbound' | 'Outbound'; // 同步方向
	entityType: string; // 实体类型
	externalId?: string; // 外部系统实体 ID
	internalId?: string | number; // WFE 实体 ID
	syncStatus: SyncStatus | string; // 同步状态
	startTime: string; // 开始时间
	endTime?: string; // 结束时间
	durationMs?: number; // 持续时间（毫秒）
	message?: string; // 消息
}

/**
 * 获取同步日志查询参数
 */
export interface IGetSyncLogsParams {
	integrationId: string | number; // 集成 ID (必填)
	pageIndex?: number; // 页码，默认 1
	pageSize?: number; // 每页数量，默认 15
	syncDirection?: 'Inbound' | 'Outbound'; // 同步方向
	status?: string; // 同步状态 (Success/Failed/Pending/InProgress)
}

/**
 * Inbound 配置接口
 */
export interface IInboundConfiguration {
	id?: string | number; // 配置 ID (long)
	integrationId: string | number; // 集成 ID (long)
	actionId: string | number; // Action ID (long)
	actionName?: string; // Action 名称
	attachmentSharingConfig?: {
		enabled: boolean;
		allowedTypes?: string[];
		maxSizeInMB?: number;
	};
	autoCreateEntities?: boolean; // 是否自动创建实体（默认 true）
	validationRules?: Record<string, any>; // 验证规则配置
	status?: number; // 状态
	lastSyncDate?: string; // 最后同步时间
	message?: string; // 消息
}

/**
 * Outbound 配置接口
 */
export interface IOutboundConfiguration {
	id?: string | number; // 配置 ID (long)
	integrationId: string | number; // 集成 ID (long)
	actionId: string | number; // Action ID (long)
	actionName?: string; // Action 名称
	sharedMasterDataTypes?: string[]; // 共享的主数据类型列表
	attachmentWorkflowIds?: (string | number)[]; // 附件工作流 ID 列表
	enableRealTimeSync?: boolean; // 是否启用实时同步
	webhookUrl?: string; // Webhook URL（最大 500 字符）
	retryAttempts?: number; // 失败重试次数（默认 3）
	retryDelaySeconds?: number; // 重试延迟秒数（默认 60）
	status?: number; // 状态
	lastSyncDate?: string; // 最后同步时间
	message?: string; // 消息
}

/**
 * 接收外部数据配置接口
 */
export interface IReceiveExternalDataConfig {
	id?: string | number; // 配置 ID (long)
	integrationId?: string | number; // 集成 ID (long)
	entityName: string; // 外部实体名称（用户自定义），最大 200 字符
	triggerWorkflowId: string | number; // 触发的工作流 ID (long)
	fieldMappings?: Record<string, string>; // 字段映射配置
	createDate?: string; // 创建时间
	modifyDate?: string; // 修改时间
}

/**
 * 获取可用工作流查询参数
 */
export interface IGetAvailableWorkflowsParams {
	integrationId: string | number; // 集成 ID (必填)
	search?: string; // 按名称搜索
	pageIndex?: number; // 页码，默认 1
	pageSize?: number; // 每页数量，默认 50
}

/**
 * 工作流选项接口
 */
export interface IWorkflowOption {
	workflowId: string | number; // 工作流 ID (long)
	workflowName: string; // 工作流名称
	workflowType?: string; // 工作流类型
	isActive: boolean; // 是否激活
	description?: string; // 工作流描述
}

/**
 * 获取实体映射列表查询参数
 */
export interface IGetEntityMappingsParams {
	integrationId: string | number; // 集成 ID (必填)
	pageIndex?: number; // 页码，默认 1
	pageSize?: number; // 每页数量，默认 15
}

/**
 * 更新集成状态请求
 */
export interface IUpdateIntegrationStatusRequest {
	status: IntegrationStatus | number; // 状态值 (0-3)
}

/**
 * 生成快速链接 URL 请求
 */
export interface IGenerateQuickLinkUrlRequest {
	[key: string]: any; // 参数映射，例如 { accountId: "ACC-12345", userId: "USR-67890" }
}
