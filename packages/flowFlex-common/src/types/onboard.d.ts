export interface Questionnaire {
	allowDraft: boolean;
	allowMultipleSubmissions: boolean;
	category: string;
	createBy: string;
	createDate: string;
	description: string;
	estimatedMinutes: number;
	id: string;
	isActive: boolean;
	isTemplate: boolean;
	modifyBy: string;
	modifyDate: string;
	name: string;
	previewImageUrl: string | null;
	requiredQuestions: number;
	assignments: {
		workflowId: string;
		stageId: string;
	}[];
	status: string;
	structureJson: string;
	tagsJson: string | null;
	templateId: string | null;
	totalQuestions: number;
	type: string;
	version: number;
}

// Onboarding相关类型定义
export interface OnboardingItem {
	completionRate: number;
	contactEmail: string;
	contactPerson: string;
	createBy: string;
	createDate: string;
	currentStageId: string;
	currentStageName: string;
	currentStageOrder: number;
	currentStageStartTime: string;
	id: string;
	isActive: boolean;
	isOverdue: boolean;
	isPrioritySet: boolean;
	leadId: string;
	leadName: string;
	lifeCycleStageId: string;
	lifeCycleStageName: string;
	modifyBy: string;
	modifyDate: string;
	priority: string;
	startDate: string;
	workflowName: string;
	timelineDays: number;
	workflowId: string;
	stagesProgress: StageInfo[];
	estimatedCompletionDate: string;
}

export interface SearchParams {
	workFlowId: string;
	leadId: string;
	leadName: string;
	lifeCycleStageName: string;
	currentStageId: string;
	updatedBy: string;
	priority: string;
	page: number;
	size: number;
	// 新增标签字段
	leadIdTags?: string[];
	leadNameTags?: string[];
	updatedByTags?: string[];
}

export interface OnboardingQueryRequest {
	leadId?: string;
	leadName?: string;
	lifeCycleStageName?: string;
	currentStageId?: string;
	updatedBy?: string;
	priority?: string;
	workFlowId?: string;
	page?: number;
	size?: number;
	sort?: string;
	sortType?: string;
	allData?: boolean;
}

// API响应的实际结构
export interface ApiResponse<T> {
	data: {
		totalPage: number;
		pageCount: number;
		pageIndex: number;
		pageSize: number;
		total: number;
		dataCount: number;
		data: T[];
	};
	success: boolean;
	msg: string;
	code: string;
}

export interface PagedResult<T> {
	content: T[];
	totalElements: number;
	totalPages: number;
	size: number;
	number: number;
}

// 统计信息类型
export interface OnboardingStatistics {
	totalCount: number;
	activeCount: number;
	completedCount: number;
	overdueCount: number;
	highPriorityCount: number;
	mediumPriorityCount: number;
	lowPriorityCount: number;
	stageDistribution: Record<string, number>;
}

export interface DocumentItem {
	accessUrl: string;
	category: string;
	contentType: string;
	description: string;
	downloadUrl: string;
	fileExtension: string;
	fileSize: string;
	fileSizeFormatted: string;
	id: string;
	isRequired: boolean;
	lastModifiedDate: string | null;
	onboardingId: string;
	originalFileName: string;
	stageId: string;
	stageName: string | null;
	status: string;
	storedFileName: string;
	tags: string | null;
	tenantId: string;
	uploadedById: string;
	uploadedByName: string;
	uploadedDate: string;
}

// 入职阶段枚举
export type OnboardingStage =
	| 'Warm Lead Created'
	| 'Application Sent'
	| 'Application Filled'
	| 'Application Approved'
	| 'Deal Sent'
	| 'Customer Questionnaire'
	| 'Service Quote Signed'
	| 'MSA Signed'
	| 'Legal & Compliance Checked'
	| 'Deal Closed Won'
	| 'W9 Sent'
	| 'Customer Account Created'
	| 'Order Interface Setup'
	| 'WMS Process Rule Setup'
	| 'Item Master Setup'
	| 'Billing Setup'
	| 'System Settings Cross Check'
	| 'Client Portal Access Setup'
	| 'WMS Email Notification Setup'
	| 'System UAT Testing'
	| 'Operation Training'
	| 'Go Live';

// 优先级枚举
export type Priority = 'High' | 'Medium' | 'Low';

// 生命周期阶段枚举
export type LifeCycleStage = 'Lead' | 'Qualified' | 'Proposal' | 'Negotiation' | 'Closed';

// 状态枚举
export type OnboardingStatus = 'Active' | 'Paused' | 'Completed' | 'Cancelled';

export interface ComponentData {
	key: 'fields' | 'checklist' | 'questionnaires' | 'files';
	order: number;
	isEnabled: boolean;
	staticFields: string[];
	checklistIds: string[];
	questionnaireIds: string[];
}

export interface StageInfo {
	estimatedDays: number;
	isCompleted: boolean;
	isCurrent: boolean;
	stageId: string;
	stageName: string;
	stageOrder: number;
	startTime: string;
	status: string;
	components: ComponentData[];
}

export interface ComponentsData {
	components: ComponentData[];
	[key: string]: any;
}

export interface StaticField {
	label: string;
	vIfKey: string;
	formProp: string;
	category?: string;
}

export interface SelectedItem {
	id: string;
	name: string;
	description?: string;
	type: 'fields' | 'checklist' | 'questionnaires' | 'files';
	order: number;
	key: string;
}

export interface FieldTag {
	key: string;
	label: string;
}

// 类型定义
export interface Stage {
	id: string;
	name: string;
	description?: string;
	defaultAssignedGroup: string;
	defaultAssignee: string;
	estimatedDuration: number;
	requiredFieldsJson: string;
	staticFields?: string[];
	components: ComponentData[];
	order: number;
	selected?: boolean;
	color?: string;
}

export interface Workflow {
	id: string;
	name: string;
	description: string;
	startDate: string;
	endDate: string | null;
	status: 'active' | 'inactive';
	isDefault: boolean;
	isActive?: boolean;
	version: number;
	stages: Stage[];
}

export interface Checklist {
	id: string;
	name: string;
	description: string;
	team: string;
	type: string;
	status: string;
	isTemplate: boolean;
	completionRate: number;
	totalTasks: number;
	completedTasks: number;
	estimatedHours: number;
	isActive: boolean;
	createDate: string;
	createBy: string;
	assignments: {
		workflowId: string;
		stageId: string;
	}[];
	tasks: any[];
}

// 接口定义
export interface Stage {
	id: string;
	name: string;
	description?: string;
	defaultAssignedGroup: string;
	defaultAssignee: string;
	estimatedDuration: number;
	requiredFieldsJson: string;
	order: number;
	selected?: boolean;
	color?: string;
	components: ComponentData[];
}

// 检查清单任务完成记录相关类型定义
export interface ChecklistTaskCompletionInputDto {
	onboardingId: string | number;
	leadId?: string;
	checklistId: string | number;
	taskId: string | number;
	stageId?: string | number; // 新增 stageId 字段
	isCompleted: boolean;
	completionNotes?: string;
	// 支持字符串形式的ID输入，用于处理JavaScript大整数精度丢失问题
	onboardingIdString?: string;
	checklistIdString?: string;
	taskIdString?: string;
	stageIdString?: string; // 新增 stageIdString 字段
}

export interface ChecklistTaskCompletionOutputDto {
	id: string | number;
	onboardingId: string | number;
	leadId: string;
	checklistId: string | number;
	taskId: string | number;
	stageId?: string | number; // 新增 stageId 字段
	isCompleted: boolean;
	completedTime?: string;
	completionNotes: string;
	source: string;
	createDate: string;
	createBy: string;
}

// 任务数据结构
export interface TaskData {
	id: string;
	checklistId: string;
	name: string;
	description: string;
	taskType: string;
	isCompleted: boolean;
	isRequired: boolean;
	assigneeId: string | null;
	assigneeName: string | null;
	assignedTeam: string | null;
	priority: string;
	order: number;
	estimatedHours: number;
	actualHours: number;
	dueDate: string | null;
	completedDate: string | null;
	completionNotes: string | null;
	dependsOnTaskId: string | null;
	attachmentsJson: string | null;
	status: string;
	isActive: boolean;
	createDate: string;
	createBy: string;
}

// API返回的Checklist数据结构
export interface ChecklistData {
	id: string;
	name: string;
	description: string;
	team: string;
	type: string;
	status: string;
	isTemplate: boolean;
	templateId: string | null;
	completionRate: number;
	totalTasks: number;
	completedTasks: number;
	estimatedHours: number;
	isActive: boolean;
	createDate: string;
	createBy: string;
	workflowId: string;
	stageId: string;
	workflowName: string | null;
	stageName: string | null;
	tasks: TaskData[];
}
