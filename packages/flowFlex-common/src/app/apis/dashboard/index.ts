/**
 * Dashboard - API 接口定义
 * 所有与仪表盘相关的 API 请求
 * 根据 Dashboard-API.md 定义
 */

import { defHttp } from '@/apis/axios';
import { useGlobSetting } from '@/settings';
import type {
	IApiResponse,
	IDashboardStatistics,
	ICasesOverview,
	IDashboardTasksResponse,
	IDashboardMessagesResponse,
	IAchievement,
	IDeadline,
	IGetTasksParams,
	IGetMessagesParams,
	IGetAchievementsParams,
	IGetDeadlinesParams,
	IGetStatisticsParams,
	IGetCasesOverviewParams,
} from '#/dashboard';

const globSetting = useGlobSetting();

const Api = {
	dashboard: `${globSetting.apiProName}/ow/dashboard/${globSetting.apiVersion}`,
};

// ==================== Statistics API ====================

/**
 * 获取统计概览
 * GET /api/ow/dashboard/v1/statistics
 */
export function getStatistics(
	params?: IGetStatisticsParams
): Promise<IApiResponse<IDashboardStatistics>> {
	return defHttp.get({ url: `${Api.dashboard}/statistics`, params });
}

// ==================== Cases Overview API ====================

/**
 * 获取案例概览
 * GET /api/ow/dashboard/v1/cases-overview
 */
export function getCasesOverview(
	params?: IGetCasesOverviewParams
): Promise<IApiResponse<ICasesOverview>> {
	return defHttp.get({ url: `${Api.dashboard}/cases-overview`, params });
}

// ==================== Tasks API ====================

/**
 * 获取待办任务列表
 * GET /api/ow/dashboard/v1/tasks
 */
export function getTasks(params?: IGetTasksParams): Promise<IApiResponse<IDashboardTasksResponse>> {
	return defHttp.get({ url: `${Api.dashboard}/tasks`, params });
}

// ==================== Messages API ====================

/**
 * 获取消息摘要
 * GET /api/ow/dashboard/v1/messages
 */
export function getMessages(
	params?: IGetMessagesParams
): Promise<IApiResponse<IDashboardMessagesResponse>> {
	return defHttp.get({ url: `${Api.dashboard}/messages`, params });
}

// ==================== Achievements API ====================

/**
 * 获取成就列表
 * GET /api/ow/dashboard/v1/achievements
 */
export function getAchievements(
	params?: IGetAchievementsParams
): Promise<IApiResponse<IAchievement[]>> {
	return defHttp.get({ url: `${Api.dashboard}/achievements`, params });
}

// ==================== Deadlines API ====================

/**
 * 获取截止日期列表
 * GET /api/ow/dashboard/v1/deadlines
 */
export function getDeadlines(params?: IGetDeadlinesParams): Promise<IApiResponse<IDeadline[]>> {
	return defHttp.get({ url: `${Api.dashboard}/deadlines`, params });
}
