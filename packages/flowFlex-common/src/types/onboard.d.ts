import { ActionListItem } from './action';

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
	totalQuestions: number;
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
	id: string;
	isActive: boolean;
	isOverdue: boolean;
	isPrioritySet: boolean;
	leadId: string;
	caseName: string;
	lifeCycleStageId: string;
	lifeCycleStageName: string;
	modifyBy: string;
	modifyDate: string;
	priority: string;
	status: string; // 添加状态字段
	workflowName: string;
	timelineDays: number;
	workflowId: string;
	stagesProgress: StageInfo[];
	currentStageStartTime: string;
	currentStageEndTime: string;
	currentStageEstimatedDays: string;
	isDisabled: boolean;
	caseCode: string;
	systemId?: string;
	entityId?: string;
	permission?: {
		canView: boolean;
		canOperate: boolean;
		errorMessage?: string;
	};
}

export interface SearchParams {
	workFlowId: string;
	caseCode: string;
	caseName: string;
	lifeCycleStageName: string;
	currentStageId: string;
	updatedBy: string;
	priority: string;
	page: number;
	size: number;
	// 新增标签字段
	leadIdTags?: string[];
	updatedByTags?: string[];
}

export interface OnboardingQueryRequest {
	leadId?: string;
	caseName?: string;
	lifeCycleStageName?: string;
	currentStageId?: string;
	updatedBy?: string;
	priority?: string;
	workFlowId?: string;
	pageIndex?: number;
	pageSize?: number;
	sortField?: string;
	sortDirection?: string;
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

export interface ComponentData {
	key: 'fields' | 'checklist' | 'questionnaires' | 'files' | 'quickLink';
	order: number;
	isEnabled: boolean;
	staticFields: {
		id: string;
		isRequired: boolean;
		order: number;
	}[];
	checklistIds: string[];
	questionnaireIds: string[];
	checklistNames?: string[];
	questionnaireNames?: string[];
	allowDraft: boolean;
	allowMultipleSubmissions: false;
	assignments: {
		stageId: string;
		workflowId: string;
	}[];
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
	previewImageUrl: string;
	requiredQuestions: number;
	sections: [];
	status: string;
	structureJson: string;
	tagsJson: string;
	totalQuestions: number;
	type: string;
	version: number;
	hasError?: boolean;
	customerPortalAccess?: number;
}

export type StageComponentData = {
	key: 'fields' | 'checklist' | 'questionnaires' | 'files' | 'quickLink';
	order: number;
	isEnabled: boolean;
	staticFields: {
		id: string;
		isRequired: boolean;
		order: number;
	}[];
	checklistIds: string[];
	checklistNames?: string[];
	questionnaireIds: string[];
	quickLinkIds: string[];
	quickLinkNames?: string[];
	questionnaireNames?: string[];
	files?: string[];
	customerPortalAccess?: number;
};

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
	aiSummary?: string;
	aiSummaryGeneratedAt?: string;
	aiSummaryConfidence?: number;
	aiSummaryModel?: string;
	aiSummaryData?: string;
	visibleInPortal?: boolean;
}

export interface ComponentsData {
	components: StageComponentData[];
	visibleInPortal?: boolean;
	portalPermission?: number;
	attachmentManagementNeeded?: boolean;
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
	type: 'fields' | 'checklist' | 'questionnaires' | 'files' | 'quickLink';
	order: number;
	key: string;
	customerPortalAccess?: number;
}

export interface FieldTag {
	key: string;
	label: string;
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
	viewPermissionMode: number;
	viewTeams: string[];
	operateTeams: string[];
	useSameTeamForOperate: boolean;
	permission?: {
		canView: boolean;
		canOperate: boolean;
		errorMessage?: string;
	};
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
	staticFields?: {
		id: string;
		isRequired: boolean;
		order: number;
	}[];
	order: number;
	selected?: boolean;
	color?: string;
	components: StageComponentData[];
	visibleInPortal?: boolean;
	portalPermission?: number; // PortalPermissionEnum value
	attachmentManagementNeeded?: boolean;
	// AI summary fields (optional)
	aiSummary?: string;
	aiSummaryGeneratedAt?: string;
	aiSummaryConfidence?: number;
	aiSummaryModel?: string;
	aiSummaryData?: any;
	endTime?: string;
	estimatedDays?: number;
	isCompleted?: boolean;
	isCurrent?: boolean;
	isSaved?: boolean;
	savedById?: string;
	stageDescription?: string;
	stageId?: string;
	stageName?: string;
	stageOrder?: number;
	startTime?: string;
	status?: string;
	saveTime?: string;
	savedBy?: string;
	completionTime?: string;
	completedBy?: string;
	actions?: ActionListItem[];
	customEndTime?: string;
	permission?: {
		canView: boolean;
		canOperate: boolean;
		errorMessage?: string;
	};
	assignee?: string[];
	coAssignees?: string[];
	required: boolean;
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

// 任务附件数据结构
export interface TaskAttachment {
	id: string;
	name: string;
	size?: number;
	url?: string;
	uploadDate?: string;
	uploadBy?: string;
}

// 任务笔记数据结构
export interface TaskNote {
	content: string;
	createdAt: string;
	createdBy: string;
	createdByName: string;
	id: string;
	isDeleted: boolean;
	isPinned: boolean;
	modifiedAt: string;
	noteType: string;
	onboardingId: string;
	priority: string;
	taskId: string;
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
	completedBy?: string | null; // 新增完成者字段
	completionNotes: string | null;
	dependsOnTaskId: string | null;
	attachmentsJson: string | null;
	status: string;
	isActive: boolean;
	createDate: string;
	createBy: string;
	filesCount: number;
	notesCount: number;
	actionId?: string | null; // Action绑定ID
	actionName?: string | null; // Action名称
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

// 类型定义
export interface QuestionnaireAnswer {
	lastModifiedAt?: string;
	lastModifiedBy?: string;
	question: string;
	questionId: string;
	responseText: string;
	type: string;
	answer: string;
	changeHistory?: {
		action: string;
		timestamp: string;
		timestampUtc: string;
		user: string;
	}[];
}

export interface QuestionnaireData {
	questionnaireId: string;
	stageId: string;
	answerJson: QuestionnaireAnswer[];
}

export interface Assignment {
	workflowId: string | null;
	stageId: string | null;
}

export interface ExtendedAssignment extends Assignment {
	stages: Array<{ id: string; name: string }>;
	stagesLoading: boolean;
}

export interface Workflow {
	id: string;
	name: string;
	isDefault?: boolean;
	status?: string;
	modifyBy: string;
	modifyDate: string;
	isAIGenerated: boolean;
}

export interface SectionAnswer {
	answer: QuestionnaireAnswer[];
	completionRate: number;
	createBy: string;
	createDate: string;
	id: string;
	ipAddress: string;
	isLatest: boolean;
	modifyBy: string;
	modifyDate: string;
	onboardingId: string;
	questionnaireId: string;
	reviewNotes: string;
	stageId: string;
	status: string;
	tenantId: string;
	userAgent: string;
	version: number;
	currentSectionIndex: number;
}
