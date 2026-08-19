import { defHttp } from '@/apis/axios';
import { useGlobSetting } from '@/settings';

const globSetting = useGlobSetting();

const Api = (onboardingId?: string | number) => ({
    ganttData: `${globSetting.apiProName}/ow/gantt/${globSetting.apiVersion}/${onboardingId}`,
    blockStage: `${globSetting.apiProName}/ow/gantt/${globSetting.apiVersion}/${onboardingId}/block`,
    unblockStage: `${globSetting.apiProName}/ow/gantt/${globSetting.apiVersion}/${onboardingId}/unblock`,
    tourSeen: `${globSetting.apiProName}/ow/gantt/${globSetting.apiVersion}/tour/seen`,
    tourMarkSeen: `${globSetting.apiProName}/ow/gantt/${globSetting.apiVersion}/tour/mark-seen`,
});

// ========================= 类型定义 =========================

/** Gantt Stage 状态枚举（与后端 DeriveGanttStageStatus 对齐） */
export type GanttStageStatus =
    | 'NotStarted'
    | 'InProgress'
    | 'Completed'
    | 'Overdue'
    | 'Delayed';

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
