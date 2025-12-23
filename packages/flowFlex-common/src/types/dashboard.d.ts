/**
 * Dashboard 页面类型定义
 * @description 定义 Dashboard 页面所需的所有数据类型
 */

import type { TaskData } from './onboard';
import { MessageTag } from '@/enums/appEnum';

// ==================== API 通用类型 ====================

/** API 响应通用结构 */
export interface IApiResponse<T> {
	data: T;
	success: boolean;
	msg: string;
	code: string;
}

// ==================== Statistics API 类型 ====================

/** 统计项 */
export interface IStatisticItem {
	value: number;
	difference: number;
	trend: 'up' | 'down' | 'neutral';
	isPositive: boolean;
	suffix?: string;
}

/** 统计概览响应 */
export interface IDashboardStatistics {
	activeCases: IStatisticItem;
	completedThisMonth: IStatisticItem;
	overdueTasks: IStatisticItem;
	avgCompletionTime: IStatisticItem;
}

/** 获取统计数据参数 */
export interface IGetStatisticsParams {
	team?: string;
}

// ==================== Cases Overview API 类型 ====================

/** 阶段分布项 */
export interface IStageDistribution {
	stageId: number;
	stageName: string;
	caseCount: number;
	order: number;
	color: string;
	percentage: number;
}

/** 案例概览响应 */
export interface ICasesOverview {
	stages: IStageDistribution[];
	overallProgress: number;
}

/** 获取案例概览参数 */
export interface IGetCasesOverviewParams {
	workflowId?: number;
}

// ==================== Tasks API 类型 ====================

/** Dashboard 任务项 */
export interface IDashboardTask {
	id: number;
	name: string;
	description: string;
	priority: 'Low' | 'Medium' | 'High' | 'Critical';
	dueDate: string;
	isOverdue: boolean;
	daysUntilDue: number;
	dueDateDisplay: string;
	caseCode: string;
	caseName: string;
	onboardingId: string;
	assignedTeam: string;
	assigneeName: string;
	category: string;
	status: 'Pending' | 'InProgress' | 'Completed' | 'Blocked' | 'Cancelled';
	isRequired: boolean;
	/** 检查清单 ID - 用于任务完成/取消操作 */
	checklistId?: string;
	/** 阶段 ID - 用于任务完成/取消操作 */
	stageId?: string;
	checklistId: string;
}

/** 任务列表响应 */
export interface IDashboardTasksResponse {
	items: IDashboardTask[];
	totalCount: number;
	pageIndex: number;
	pageSize: number;
	totalPages: number;
	hasPreviousPage: boolean;
	hasNextPage: boolean;
}

/** 获取任务列表参数 */
export interface IGetTasksParams {
	category?: string;
	pageIndex?: number;
	pageSize?: number;
}

// ==================== Messages API 类型 ====================

/** Dashboard 消息项 */
export interface IDashboardMessage {
	id: string;
	senderName: string;
	senderInitials: string;
	subject: string;
	bodyPreview: string;
	isRead: boolean;
	isNew: boolean;
	labels: MessageTag[];
	receivedDate: string;
	receivedDateDisplay: string;
	relatedCaseCode: string;
	relatedCaseName: string;
	messageType: 'Internal' | 'Email' | 'Portal';
}

/** 消息摘要响应 */
export interface IDashboardMessagesResponse {
	messages: IDashboardMessage[];
	unreadCount: number;
}

/** 获取消息参数 */
export interface IGetMessagesParams {
	limit?: number;
}

// ==================== Achievements API 类型 ====================

/** 成就类型 */
export type AchievementType =
	| 'CaseCompleted'
	| 'StageCompleted'
	| 'MilestoneSigned'
	| 'SystemIntegration';

/** 成就项 */
export interface IAchievement {
	id: number;
	title: string;
	description: string;
	completionDate: string;
	completionDateDisplay: string;
	teams: string[];
	type: AchievementType;
	caseCode: string;
	caseName: string;
	daysToComplete: number;
}

/** 获取成就列表参数 */
export interface IGetAchievementsParams {
	limit?: number;
	team?: string;
}

// ==================== Deadlines API 类型 ====================

/** 紧急程度 */
export type DeadlineUrgency = 'overdue' | 'today' | 'tomorrow' | 'thisWeek' | 'upcoming';

/** 截止日期类型 */
export type DeadlineType = 'Task' | 'Milestone' | 'StageEstimate';

/** 截止日期项 */
export interface IDeadline {
	id: number;
	name: string;
	dueDate: string;
	dueDateDisplay: string;
	urgency: DeadlineUrgency;
	daysUntilDue: number;
	caseCode: string;
	caseName: string;
	onboardingId: number;
	type: DeadlineType;
	priority: 'Low' | 'Medium' | 'High' | 'Critical';
	assignedTeam: string;
}

/** 获取截止日期参数 */
export interface IGetDeadlinesParams {
	days?: number;
}

// ==================== Employee 类型 (暂无 API，使用 Mock 数据) ====================

/** 员工统计数据 */
export interface IEmployeeStats {
	total: number;
	active: number;
	onLeave: number;
	avgSalary: number;
	departmentCount: number;
}

/** 部门分布 */
export interface IDepartmentDistribution {
	name: string;
	count: number;
}

/** 最近入职员工 */
export interface IRecentHire {
	id: string;
	name: string;
	department: string;
	hireDate: string;
}

// ==================== 前端组件使用的类型 ====================

/** 待办任务优先级 */
export type TodoPriority = 'high' | 'medium' | 'low';

/** 消息来源类型 */
export type MessageSource = 'internal' | 'customer';

/** 待办任务项 - 使用 onboard 的 TaskData 类型 */
export type TodoItem = TaskData;

/** 消息项 */
export interface Message {
	id: string;
	senderName: string;
	senderAvatar?: string;
	timestamp: Date | string;
	subject: string;
	preview: string;
	isRead: boolean;
	source: MessageSource;
}

/** 员工统计数据 */
export interface EmployeeStats {
	total: number;
	active: number;
	onLeave: number;
	avgSalary: number;
	departmentCount: number;
}

/** 部门分布 */
export interface DepartmentDistribution {
	name: string;
	count: number;
}

/** 最近入职员工 */
export interface RecentHire {
	id: string;
	name: string;
	department: string;
	hireDate: Date | string;
}

/** 案例阶段 */
export interface CaseStage {
	name: string;
	count: number;
	color?: string;
}

/** 团队成员 */
export interface TeamMember {
	id: string;
	name: string;
	avatar?: string;
}

/** 成就项 */
export interface Achievement {
	id: string;
	title: string;
	date: Date | string;
	description: string;
	teamMembers: TeamMember[];
}

/** 截止日期项 */
export interface Deadline {
	id: string;
	title: string;
	dueDate: Date | string;
	daysRemaining: number;
}

/** Dashboard 统计数据 */
export interface DashboardStats {
	activeCases: number;
	activeCasesTrend: number;
	completedThisMonth: number;
	completedTrend: number;
	overdueTasks: number;
	overdueTrend: number;
	avgCompletionTime: number;
	avgCompletionTrend: number;
}

/** Dashboard 员工数据 */
export interface DashboardEmployees {
	stats: EmployeeStats;
	departments: DepartmentDistribution[];
	recentHires: RecentHire[];
}

/** Dashboard 案例数据 */
export interface DashboardCases {
	stages: CaseStage[];
	overallProgress: number;
}

/** Dashboard 完整数据结构 */
export interface DashboardData {
	stats: DashboardStats;
	todos: TodoItem[];
	messages: Message[];
	employees: DashboardEmployees;
	cases: DashboardCases;
	achievements: Achievement[];
	deadlines: Deadline[];
}
