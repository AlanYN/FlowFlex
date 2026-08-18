// import { defHttp } from '@/apis/axios'; // TODO: 后端就绪后取消注释
// import { useGlobSetting } from '@/settings'; // TODO: 后端就绪后取消注释（用于 API URL 构建）

// TODO: 后端就绪后取消注释（用于真实 API 调用）
// const globSetting = useGlobSetting();

// ========================= 甘特图数据类型定义 =========================

/** Stage 状态枚举 */
export type GanttStageStatus =
	| 'NotStarted' // 未开始
	| 'InProgress' // 进行中
	| 'Completed' // 已完成
	| 'Overdue' // 超期（已开始但超过 ETA）
	| 'Delayed' // 延迟（未开始但超过 Planned Start）
	| 'Blocked'; // 阻塞（手动标记）

/** 甘特图 Stage 数据 */
export interface GanttStageItem {
	stageId: string;
	stageName: string;
	stageOrder: number;
	color?: string;
	status: GanttStageStatus;
	assignee: string[];
	coAssignees: string[];

	// 计划时间（案例启动时确定，不再变更）
	plannedStartDate: string; // ISO 8601
	plannedEndDate: string; // ISO 8601

	// 预测时间（随 Stage 完成动态更新）
	projectedStartDate: string | null; // ISO 8601
	projectedEndDate: string | null; // ISO 8601

	// 实际时间
	actualStartDate: string | null; // ISO 8601
	actualEndDate: string | null; // ISO 8601

	// 完成度（0-100）
	completionPercentage: number;

	// 预估天数
	estimatedDurationDays: number;

	// 阻塞信息
	isBlocked: boolean;
	blockedDays: number;
	blockReason?: string;
	expectedResolutionDate?: string | null;

	// 方差分析（Completed 状态时有值）
	inheritedDelayDays?: number; // = Actual Start - Planned Start
	ownPerformanceDays?: number; // = Actual Duration - Planned Duration
	totalVarianceDays?: number; // = Actual End - Planned End

	// Assignee 邮件（展示用）
	assigneeEmail?: string;

	// Components 完成统计（用于 Popover）
	components?: {
		checklistsTotal: number;
		checklistsCompleted: number;
		questionnairesTotal: number;
		questionnairesSubmitted: number;
		fieldsTotal: number;
		fieldsFilled: number;
		filesUploaded: number;
	};

	// 最后保存信息
	lastSavedBy?: string;
	lastSavedAt?: string; // ISO 8601

	// 已过去天数（实际开始到今天）
	daysElapsed?: number;
}

/** 甘特图 Case 汇总头部信息 */
export interface GanttCaseSummary {
	onboardingId: string;
	caseName: string;
	caseCode: string;
	workflowName: string;
	status: string;
	priority: string;

	// Case 级别时间
	plannedStartDate: string; // ISO 8601
	plannedEndDate: string; // ISO 8601
	projectedEndDate: string | null;
	actualStartDate: string | null;
	actualEndDate: string | null;

	// 整体完成度
	overallCompletionPercentage: number;

	// 统计
	totalStages: number;
	completedStages: number;
	overdueStages: number;
	delayedStages: number;
	blockedStages: number;
}

/** 完整甘特图响应 */
export interface GanttDataResponse {
	summary: GanttCaseSummary;
	stages: GanttStageItem[];
}

// ========================= Mock 数据 =========================

/** 生成基于今天的日期偏移（返回 ISO 8601 字符串） */
function daysFromNow(days: number): string {
	const date = new Date();
	date.setDate(date.getDate() + days);
	date.setHours(0, 0, 0, 0);
	return date.toISOString();
}

/** 创建 Mock 甘特图数据 */
function createMockGanttData(onboardingId: string): GanttDataResponse {
	const stages: GanttStageItem[] = [
		{
			stageId: '1001',
			stageName: 'Initial Review',
			stageOrder: 1,
			color: '#5b8cff',
			status: 'Completed',
			assignee: ['John Smith'],
			assigneeEmail: 'john.smith@example.com',
			coAssignees: [],
			plannedStartDate: daysFromNow(-30),
			plannedEndDate: daysFromNow(-23),
			projectedStartDate: daysFromNow(-30),
			projectedEndDate: daysFromNow(-22),
			actualStartDate: daysFromNow(-30),
			actualEndDate: daysFromNow(-22),
			completionPercentage: 100,
			estimatedDurationDays: 7,
			isBlocked: false,
			blockedDays: 0,
			inheritedDelayDays: 0,
			ownPerformanceDays: 1,
			totalVarianceDays: 1,
			daysElapsed: 8,
			lastSavedBy: 'John Smith',
			lastSavedAt: daysFromNow(-22),
			components: {
				checklistsTotal: 3,
				checklistsCompleted: 3,
				questionnairesTotal: 1,
				questionnairesSubmitted: 1,
				fieldsTotal: 4,
				fieldsFilled: 4,
				filesUploaded: 1,
			},
		},
		{
			stageId: '1002',
			stageName: 'Document Collection',
			stageOrder: 2,
			color: '#52c41a',
			status: 'Completed',
			assignee: ['Jane Doe'],
			assigneeEmail: 'jane.doe@example.com',
			coAssignees: ['Mike Chen'],
			plannedStartDate: daysFromNow(-22),
			plannedEndDate: daysFromNow(-15),
			projectedStartDate: daysFromNow(-21),
			projectedEndDate: daysFromNow(-13),
			actualStartDate: daysFromNow(-21),
			actualEndDate: daysFromNow(-13),
			completionPercentage: 100,
			estimatedDurationDays: 7,
			isBlocked: false,
			blockedDays: 0,
			inheritedDelayDays: 1,
			ownPerformanceDays: 1,
			totalVarianceDays: 2,
			daysElapsed: 8,
			lastSavedBy: 'Jane Doe',
			lastSavedAt: daysFromNow(-13),
			components: {
				checklistsTotal: 2,
				checklistsCompleted: 2,
				questionnairesTotal: 0,
				questionnairesSubmitted: 0,
				fieldsTotal: 3,
				fieldsFilled: 3,
				filesUploaded: 5,
			},
		},
		{
			stageId: '1003',
			stageName: 'Compliance Check',
			stageOrder: 3,
			color: '#faad14',
			status: 'InProgress',
			assignee: ['Sarah Johnson'],
			assigneeEmail: 'sarah.johnson@example.com',
			coAssignees: [],
			plannedStartDate: daysFromNow(-14),
			plannedEndDate: daysFromNow(-7),
			projectedStartDate: daysFromNow(-12),
			projectedEndDate: daysFromNow(-3),
			actualStartDate: daysFromNow(-12),
			actualEndDate: null,
			completionPercentage: 65,
			estimatedDurationDays: 7,
			isBlocked: false,
			blockedDays: 0,
			daysElapsed: 12,
			lastSavedBy: 'Sarah Johnson',
			lastSavedAt: daysFromNow(-1),
			components: {
				checklistsTotal: 5,
				checklistsCompleted: 2,
				questionnairesTotal: 1,
				questionnairesSubmitted: 0,
				fieldsTotal: 3,
				fieldsFilled: 3,
				filesUploaded: 2,
			},
		},
		{
			stageId: '1004',
			stageName: 'Technical Assessment',
			stageOrder: 4,
			color: '#ff4d4f',
			status: 'Overdue',
			assignee: ['David Lee'],
			assigneeEmail: 'david.lee@example.com',
			coAssignees: ['Anna Wang'],
			plannedStartDate: daysFromNow(-7),
			plannedEndDate: daysFromNow(0),
			projectedStartDate: daysFromNow(-3),
			projectedEndDate: daysFromNow(4),
			actualStartDate: daysFromNow(-3),
			actualEndDate: null,
			completionPercentage: 30,
			estimatedDurationDays: 7,
			isBlocked: false,
			blockedDays: 0,
			daysElapsed: 3,
			lastSavedBy: 'David Lee',
			lastSavedAt: daysFromNow(-1),
			components: {
				checklistsTotal: 4,
				checklistsCompleted: 1,
				questionnairesTotal: 1,
				questionnairesSubmitted: 0,
				fieldsTotal: 2,
				fieldsFilled: 1,
				filesUploaded: 0,
			},
		},
		{
			stageId: '1005',
			stageName: 'Legal Review',
			stageOrder: 5,
			color: '#722ed1',
			status: 'Blocked',
			assignee: ['Emily Brown'],
			coAssignees: [],
			plannedStartDate: daysFromNow(1),
			plannedEndDate: daysFromNow(8),
			projectedStartDate: daysFromNow(5),
			projectedEndDate: null,
			actualStartDate: null,
			actualEndDate: null,
			completionPercentage: 0,
			estimatedDurationDays: 7,
			isBlocked: true,
			blockedDays: 2,
			blockReason: 'Waiting for external legal counsel availability',
			expectedResolutionDate: daysFromNow(5),
		},
		{
			stageId: '1006',
			stageName: 'Final Approval',
			stageOrder: 6,
			color: '#13c2c2',
			status: 'Delayed',
			assignee: ['Robert Kim'],
			coAssignees: [],
			plannedStartDate: daysFromNow(9),
			plannedEndDate: daysFromNow(16),
			projectedStartDate: daysFromNow(14),
			projectedEndDate: daysFromNow(21),
			actualStartDate: null,
			actualEndDate: null,
			completionPercentage: 0,
			estimatedDurationDays: 7,
			isBlocked: false,
			blockedDays: 0,
		},
		{
			stageId: '1007',
			stageName: 'Onboarding Complete',
			stageOrder: 7,
			color: '#52c41a',
			status: 'NotStarted',
			assignee: ['John Smith'],
			coAssignees: [],
			plannedStartDate: daysFromNow(17),
			plannedEndDate: daysFromNow(21),
			projectedStartDate: daysFromNow(22),
			projectedEndDate: daysFromNow(26),
			actualStartDate: null,
			actualEndDate: null,
			completionPercentage: 0,
			estimatedDurationDays: 4,
			isBlocked: false,
			blockedDays: 0,
		},
	];

	const summary: GanttCaseSummary = {
		onboardingId,
		caseName: 'Mock Case - Gantt Preview',
		caseCode: 'OW-MOCK-001',
		workflowName: 'Standard Onboarding',
		status: 'InProgress',
		priority: 'High',
		plannedStartDate: daysFromNow(-30),
		plannedEndDate: daysFromNow(21),
		projectedEndDate: daysFromNow(26),
		actualStartDate: daysFromNow(-30),
		actualEndDate: null,
		overallCompletionPercentage: 38,
		totalStages: stages.length,
		completedStages: stages.filter((s) => s.status === 'Completed').length,
		overdueStages: stages.filter((s) => s.status === 'Overdue').length,
		delayedStages: stages.filter((s) => s.status === 'Delayed').length,
		blockedStages: stages.filter((s) => s.status === 'Blocked').length,
	};

	return { summary, stages };
}

// ========================= API 函数 =========================

// TODO: 后端就绪后取消注释以下内容，并删除 mock 数据
// const Api = (id?: string | number) => ({
// 	ganttData: `${globSetting.apiProName}/ow/onboardings/${globSetting.apiVersion}/${id}/gantt`,
// 	blockStage: `${globSetting.apiProName}/ow/onboardings/${globSetting.apiVersion}/${id}/block-stage`,
// 	unblockStage: `${globSetting.apiProName}/ow/onboardings/${globSetting.apiVersion}/${id}/unblock-stage`,
// });

/** 获取甘特图数据（当前使用 Mock，后端就绪后替换为真实 API） */
export async function getOnboardingGanttData(
	onboardingId: string | number
): Promise<GanttDataResponse> {
	// TODO: 后端就绪后替换为：
	// return defHttp.get({ url: Api(onboardingId).ganttData });

	// Mock：模拟网络延迟
	await new Promise((resolve) => setTimeout(resolve, 400));
	return createMockGanttData(String(onboardingId));
}

/** 标记 Stage 为 Blocked */
export function blockStage(
	onboardingId: string | number,
	params: {
		stageId: string;
		reason: string;
		expectedResolutionDate?: string;
	}
) {
	// TODO: return defHttp.post({ url: Api(onboardingId).blockStage, params });
	return Promise.resolve(true);
}

/** 解除 Stage 的 Blocked 状态 */
export function unblockStage(onboardingId: string | number, stageId: string) {
	// TODO: return defHttp.post({ url: Api(onboardingId).unblockStage, params: { stageId } });
	return Promise.resolve(true);
}
