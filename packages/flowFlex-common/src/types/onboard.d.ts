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
	stageId: string;
	status: string;
	structureJson: string;
	tagsJson: string | null;
	templateId: string | null;
	totalQuestions: number;
	type: string;
	version: number;
	workflowId: string;
}

// Onboarding相关类型定义
export interface OnboardingItem {
	id: string;
	workflowId: string;
	workflowName: string;
	currentStageId: string;
	currentStageName: string;
	currentStageOrder: number;
	leadId: string;
	leadName: string;
	leadEmail: string;
	leadPhone: string;
	contactPerson: string; // 联系人姓名
	contactEmail: string; // 联系人邮箱
	lifeCycleStageId: string | null;
	lifeCycleStageName: string;
	status: string;
	completionRate: number;
	startDate: string;
	estimatedCompletionDate: string | null;
	actualCompletionDate: string | null;
	currentAssigneeId: string | null;
	currentAssigneeName: string;
	currentTeam: string;
	stageUpdatedById: string;
	stageUpdatedBy: string;
	stageUpdatedByEmail: string;
	stageUpdatedTime: string;
	currentStageStartTime: string;
	timelineDays: number;
	timelineDisplay: string;
	priority: string;
	isPrioritySet: boolean;
	customFieldsJson: string;
	notes: string;
	isActive: boolean;
	createDate: string;
	createBy: string;
	modifyDate: string;
	modifyBy: string;
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
}

export interface OnboardingQueryRequest {
	leadId?: string;
	leadName?: string;
	lifeCycleStageName?: string;
	currentStageId?: string;
	updatedBy?: string;
	priority?: string;
	workFlowId?: string;
	page: number;
	size: number;
	sort: string;
	sortType: string;
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
