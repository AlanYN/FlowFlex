import { defHttp } from '@/apis/axios';
import { useGlobSetting } from '@/settings';
import { GanttDataResponse } from '#/gantt';

const globSetting = useGlobSetting();

const Api = (onboardingId?: string | number) => ({
	ganttData: `${globSetting.apiProName}/ow/gantt/${globSetting.apiVersion}/${onboardingId}`,
	blockStage: `${globSetting.apiProName}/ow/gantt/${globSetting.apiVersion}/${onboardingId}/block`,
	unblockStage: `${globSetting.apiProName}/ow/gantt/${globSetting.apiVersion}/${onboardingId}/unblock`,
	tourSeen: `${globSetting.apiProName}/ow/gantt/${globSetting.apiVersion}/tour/seen`,
	tourMarkSeen: `${globSetting.apiProName}/ow/gantt/${globSetting.apiVersion}/tour/mark-seen`,
});

// ========================= API 函数 =========================

/** 获取甘特图数据 */
export function getOnboardingGanttData(
	onboardingId: string | number
): Promise<{ data: GanttDataResponse }> {
	return defHttp.get({ url: Api(onboardingId).ganttData });
}

/** 标记 Stage 为 Blocked */
export function blockStage(
	onboardingId: string | number,
	params: {
		stageId: string | number;
		blockerReason: string;
		expectedResolutionDate?: string | null;
	}
) {
	return defHttp.post<boolean>({ url: Api(onboardingId).blockStage, data: params });
}

/** 解除 Stage 的 Blocked 状态 */
export function unblockStage(
	onboardingId: string | number,
	params: {
		stageId: string | number;
		resolutionNotes?: string | null;
	}
) {
	return defHttp.post<boolean>({ url: Api(onboardingId).unblockStage, data: params });
}

/** 查询当前用户是否已看过 Gantt 引导 */
export function getGanttTourSeen(): Promise<boolean> {
	return defHttp.get({ url: Api().tourSeen });
}

/** 标记 Gantt 引导已看过 */
export function markGanttTourSeen(): Promise<boolean> {
	return defHttp.post({ url: Api().tourMarkSeen });
}
