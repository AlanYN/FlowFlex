/**
 * 认证方式枚举 (AuthenticationMethod)
 */
export enum AuthMethod {
	ApiKey = 0, // API 密钥认证
	BasicAuth = 1, // 基础认证（用户名/密码）
	OAuth2 = 2, // OAuth 2.0 认证
	BearerToken = 3, // Bearer 令牌认证
}

/**
 * 集成状态枚举 (IntegrationStatus)
 */
export enum IntegrationStatus {
	Disconnected = 0, // 已断开连接
	Connected = 1, // 已连接
	Error = 2, // 连接错误
	InProgress = 3, // 连接中
}

/**
 * 同步方向枚举 (SyncDirection)
 */
export enum SyncDirection {
	ViewOnly = 0, // 仅查看（仅入站）
	Editable = 1, // 可编辑（双向同步）
	OutboundOnly = 2, // 仅出站
}

/**
 * 同步状态枚举 (SyncStatus)
 */
export enum SyncStatus {
	Pending = 0, // 等待中
	Success = 1, // 成功
	Failed = 2, // 失败
	PartialSuccess = 3, // 部分成功
	InProgress = 4, // 进行中
}

/**
 * 重定向类型枚举 (RedirectType)
 */
export enum RedirectType {
	Direct = 0, // 直接跳转
	PopupConfirmation = 1, // 弹窗确认后跳转
}

/**
 * 实体键类型枚举 (EntityKeyType)
 */
export enum EntityKeyType {
	String = 0, // 字符串类型
	Integer = 1, // 整数类型
	Guid = 2, // GUID 类型
}

export enum ValueSource {
	PageParameter = 0,
	LoginUserInfo = 1,
	FixedValue = 2,
	SystemVariable = 3,
}

/**
 * 页面参数详情枚举 (PageParameterDetail)
 */
export enum PageParameterDetail {
	CaseId = 'caseId', // Case ID
	CustomerId = 'customerId', // Customer ID
	OrderNumber = 'orderNumber', // Order Number
}

/**
 * 登录用户信息详情枚举 (LoginUserInfoDetail)
 */
export enum LoginUserInfoDetail {
	UserId = 'userId', // User ID
	Username = 'username', // Username
	Email = 'email', // Email
}

/**
 * 系统变量详情枚举 (SystemVariableDetail)
 */
export enum SystemVariableDetail {
	CurrentTimestamp = 'currentTimestamp', // Current Timestamp
	CurrentDate = 'currentDate', // Current Date
}
