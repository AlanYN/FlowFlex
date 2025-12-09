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
 * 连接配置接口
 */
export interface IConnectionConfig {
	systemName: string;
	endpointUrl: string;
	authMethod: AuthMethod | number; // 支持枚举和数字
	credentials: Record<string, any>;
	description?: string;
	isValid: boolean;
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
export interface InboundAttachmentIteml {
	integrationId?: string;
	id?: string;
	moduleName: string; //外部模块名称
	workflowId: string; //工作流ID
	actionId: string; //action ID
}

export interface OutboundAttachmentItem1 {
	id?: string;
	workflowId: string; //工作流ID
	stageIds: string[]; //阶段 ID
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
	description?: string;
	// 详情接口返回的关联数据（可选）
	connection?: IConnectionConfig;
	entityMappings?: IEntityMapping[];
	quickLinks?: IQuickLink[];
	inboundFieldMappings?: FieldMapping[];
	outboundFieldMappings?: FieldMapping[];
	inboundAttachments?: InboundAttachmentIteml[]; //Inbound 附件配置
	outboundAttachments?: OutboundAttachmentItem1[]; //0utbound 附件配
	lastDaysSeconds: {
		[key: string]: string;
	};
	isValid: boolean;
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

// /**
//  * 更新集成请求接口
//  */
// export interface IUpdateIntegrationRequest {
// 	name?: string;
// 	connection?: Partial<IConnectionConfig>;
// 	inboundSettings?: Partial<IInboundSettings>;
// 	outboundSettings?: Partial<IOutboundSettings>;
// }

/**
 * 测试连接响应接口
 */
export interface ITestConnectionResponse {
	success: boolean;
	data?: {
		success: boolean;
		msg: string;
	}; // 连接测试结果
	msg: string;
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
 * WFE 实体选项
 */
export interface IWfeEntityOption {
	value: string;
	label: string;
	type?: string;
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
	id?: string; // 快速链接 ID (long)
	integrationId?: string; // 集成 ID (long)
	linkName: string; // 链接名称，最大 100 字符
	targetUrl: string; // 目标 URL 模板，最大 500 字符
	displayIcon?: string; // 图标名称，最大 50 字符
	redirectType: RedirectType | number; // 重定向类型 (0=Direct, 1=PopupConfirmation)
	urlParameters?: Array<{
		name: string;
		valueSource: string; // Value Source: PageParameter, LoginUserInfo, FixedValue, SystemVariable
		valueDetail: string; // Value Detail based on valueSource
	}>; // 参数映射配置
	createDate?: string; // 创建时间
	modifyDate?: string; // 修改时间
	isActive?: boolean; // 是否激活
	description?: string; // 描述
	status?: number; // 状态
}

/**
 * Inbound 配置接口
 */
export interface FieldMapping {
	id: string;
	integrationId: string;
	entityMappingId: string;
	actionId: string;
	externalFieldName: string;
	wfeFieldId: string;
	fieldType: number;
	syncDirection: number;
	actionName: string;
	transformRules: Record<string, any>;
	sortOrder: number;
	isRequired: boolean;
	wfeFieldName: string;
	isStaticField: boolean;
	createDate: string;
	modifyDate: string;
}

export interface IntegrationAttachment {
	id: string;
	fileName: string;
	fileSize: string;
	fileType: string;
	fileExt: string;
	createDate: string;
	downloadLink: string;
	integrationName: string;
	moduleName: string;
}
