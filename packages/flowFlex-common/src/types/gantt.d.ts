/** Gantt Stage 状态枚举（与后端 DeriveGanttStageStatus 对齐） */
export type GanttStageStatus = 'NotStarted' | 'InProgress' | 'Completed' | 'Overdue' | 'Delayed';

/** Assignee 信息 */
export interface GanttAssignee {
	name: string;
	email?: string;
}

/** Components 完成统计 */
export interface GanttComponents {
	checklistsTotal: number;
	checklistsCompleted: number;
	questionnairesTotal: number;
	questionnairesSubmitted: number;
	fieldsTotal: number;
	fieldsFilled: number;
	filesUploaded: number;
}

/** 单个 Stage 甘特图数据（与后端 GanttStageItemDto 字段对齐） */
export interface GanttStageItem {
	stageId: string;
	stageName: string;
	stageOrder: number;
	color?: string;
	isRequired: boolean;
	/** 后端派生状态：NotStarted | InProgress | Completed | Overdue | Delayed */
	ganttStatus: GanttStageStatus;
	isBlocked: boolean;
	assignee: GanttAssignee[];
	coAssignees: GanttAssignee[];

	// 三套时间
	plannedStartDate: string | null;
	plannedEndDate: string | null;
	projectedStartDate: string | null;
	projectedEndDate: string | null;
	actualStartDate: string | null;
	actualEndDate: string | null;

	estimatedDurationDays: number;
	completionPercentage: number;
	daysElapsed?: number | null;

	// 方差（Completed 时有值）
	inheritedDelayDays?: number | null;
	ownVarianceDays?: number | null;
	totalVarianceDays?: number | null;

	// 阻塞信息
	blockedDays: number;
	blockReason?: string | null;
	blockedByName?: string | null;
	blockedAt?: string | null;
	expectedResolutionDate?: string | null;

	// Components 统计
	components?: GanttComponents;

	// 审计
	lastSavedBy?: string | null;
	lastSavedAt?: string | null;
}

/** Case 级别汇总（与后端 GanttCaseSummaryDto 对齐） */
export interface GanttCaseSummary {
	onboardingId: string;
	caseName: string;
	caseCode: string;
	workflowName: string;
	status: string;
	priority: string;

	plannedStartDate: string | null;
	plannedEndDate: string | null;
	projectedEndDate: string | null;
	actualStartDate: string | null;
	actualEndDate: string | null;

	overallCompletionPercentage: number;
	totalStages: number;
	completedStages: number;
	overdueStages: number;
	delayedStages: number;
	blockedStages: number;

	currentStageName?: string | null;
	currentStageOrder?: number;
}

/** 完整甘特图响应（与后端 GanttDataResponseDto 对齐） */
export interface GanttDataResponse {
	summary: GanttCaseSummary;
	stages: GanttStageItem[];
}
