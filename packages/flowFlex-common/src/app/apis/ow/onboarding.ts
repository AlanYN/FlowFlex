import { defHttp } from '@/apis/axios';
import { useGlobSetting } from '@/settings';

const globSetting = useGlobSetting();

const Api = (id?: string | number) => {
	return {
		// 客户入职相关API
		onboardings: `${globSetting.apiProName}/ow/onboardings/${globSetting.apiVersion}`,
		onboarding: `${globSetting.apiProName}/ow/onboardings/${globSetting.apiVersion}/${id}`,
		onboardingQuery: `${globSetting.apiProName}/ow/onboardings/${globSetting.apiVersion}/query`,
		onboardingStatistics: `${globSetting.apiProName}/ow/onboardings/${globSetting.apiVersion}/statistics`,
		onboardingOverdue: `${globSetting.apiProName}/ow/onboardings/${globSetting.apiVersion}/overdue`,
		onboardingNextStage: `${globSetting.apiProName}/ow/onboardings/${globSetting.apiVersion}/${id}/next-stage`,
		onboardingPreviousStage: `${globSetting.apiProName}/ow/onboardings/${globSetting.apiVersion}/${id}/previous-stage`,
		onboardingMoveToStage: `${globSetting.apiProName}/ow/onboardings/${globSetting.apiVersion}/${id}/move-to-stage`,
		onboardingCompleteStage: `${globSetting.apiProName}/ow/onboardings/${globSetting.apiVersion}/${id}/complete-stage-with-validation`,
		onboardingPause: `${globSetting.apiProName}/ow/onboardings/${globSetting.apiVersion}/${id}/pause`,
		onboardingResume: `${globSetting.apiProName}/ow/onboardings/${globSetting.apiVersion}/${id}/resume`,
		onboardingCancel: `${globSetting.apiProName}/ow/onboardings/${globSetting.apiVersion}/${id}/cancel`,
		onboardingAssign: `${globSetting.apiProName}/ow/onboardings/${globSetting.apiVersion}/${id}/assign`,
		onboardingUpdateCompletionRate: `${globSetting.apiProName}/ow/onboardings/${globSetting.apiVersion}/${id}/update-completion-rate`,
		onboardingSetPriority: `${globSetting.apiProName}/ow/onboardings/${globSetting.apiVersion}/${id}/set-priority`,
		onboardingBatchUpdateStatus: `${globSetting.apiProName}/ow/onboardings/${globSetting.apiVersion}/batch-update-status`,

		// Lead同步相关API
		leadSyncShouldCreate: `${globSetting.apiProName}/ow/lead-sync/${globSetting.apiVersion}/should-create`,
		leadSync: `${globSetting.apiProName}/ow/lead-sync/${globSetting.apiVersion}/sync`,
		leadSyncInfo: `${globSetting.apiProName}/ow/lead-sync/${globSetting.apiVersion}/sync-info`,
		leadBatchSync: `${globSetting.apiProName}/ow/lead-sync/${globSetting.apiVersion}/batch-sync`,
		leadSyncStatus: `${globSetting.apiProName}/ow/lead-sync/${globSetting.apiVersion}/sync-status/${id}`,

		// 清单任务完成相关API
		checklistTaskCompletions: `${globSetting.apiProName}/ow/checklist-task-completions/${globSetting.apiVersion}`,
		checklistTaskCompletionsBatch: `${globSetting.apiProName}/ow/checklist-task-completions/${globSetting.apiVersion}/batch`,
		checklistTaskCompletionsByOnboardingAndChecklist: `${globSetting.apiProName}/ow/checklist-task-completions/${globSetting.apiVersion}/onboarding/${id}/checklist`,
		checklistTaskCompletionsStats: `${globSetting.apiProName}/ow/checklist-task-completions/${globSetting.apiVersion}/onboarding/${id}/checklist`,

		// 阶段完成日志相关API
		stageCompletionLogs: `${globSetting.apiProName}/ow/logs/stage-completion/${globSetting.apiVersion}/list`,
		stageCompletionLogsByOnboarding: `${globSetting.apiProName}/ow/logs/stage-completion/${globSetting.apiVersion}/onboarding/${id}`,
		stageCompletionLogsStatistics: `${globSetting.apiProName}/ow/logs/stage-completion/${globSetting.apiVersion}/statistics`,
		stageCompletionLogsBatch: `${globSetting.apiProName}/ow/logs/stage-completion/${globSetting.apiVersion}/batch`,

		// 内部备注相关API
		internalNotesPaged: `${globSetting.apiProName}/ow/internal-notes/${globSetting.apiVersion}/paged`,
		internalNotes: `${globSetting.apiProName}/ow/internal-notes/${globSetting.apiVersion}`,
		internalNotesByOnboarding: `${globSetting.apiProName}/ow/internal-notes/${globSetting.apiVersion}/onboarding/${id}`,
		internalNotesUnresolved: `${globSetting.apiProName}/ow/internal-notes/${globSetting.apiVersion}/onboarding/${id}/unresolved`,

		// 问卷答案相关API
		questionnaireAnswers: `${globSetting.apiProName}/ow/questionnaire-answers/${globSetting.apiVersion}/${id}/answers`,
		questionnaireAnswer: `${globSetting.apiProName}/ow/questionnaire-answers/${globSetting.apiVersion}/${id}/stage`,
		questionnaireAnswerSubmit: `${globSetting.apiProName}/ow/questionnaire-answers/${globSetting.apiVersion}/${id}/stage`,
		questionnaireAnswerHistory: `${globSetting.apiProName}/ow/questionnaire-answers/${globSetting.apiVersion}/${id}/stage`,
		questionnaireAnswersStatistics: `${globSetting.apiProName}/ow/questionnaire-answers/${globSetting.apiVersion}/statistics`,
		questionnaireAnswerReview: `${globSetting.apiProName}/ow/questionnaire-answers/${globSetting.apiVersion}/review`,
		questionnaireAnswersByStatus: `${globSetting.apiProName}/ow/questionnaire-answers/${globSetting.apiVersion}/by-status/${id}`,

		// 静态问卷答案相关API
		staticQuestionnaireAnswer: `${globSetting.apiProName}/ow/static-field-values/${globSetting.apiVersion}/batch`,

		// checkList
		checkList: `${globSetting.apiProName}/ow/checklists/${globSetting.apiVersion}/by-stage/${id}`,
		checkListTask: `${globSetting.apiProName}/ow/checklist-task-completions/${globSetting.apiVersion}`,
		ckeckListIds: `${globSetting.apiProName}/ow/checklists/${globSetting.apiVersion}/batch/by-ids`,

		// 文件管理相关API
		onboardingFiles: `${globSetting.apiProName}/ow/onboardings/${id}/files/${globSetting.apiVersion}`,
		onboardingFilesBatch: `${globSetting.apiProName}/ow/onboardings/${id}/files/${globSetting.apiVersion}/batch`,
		onboardingFile: `${globSetting.apiProName}/ow/onboardings/${id}/files/${globSetting.apiVersion}`,

		// export
		exportOnboarding: `${globSetting.apiProName}/ow/onboardings/${globSetting.apiVersion}/export-excel?`,

		// 预览
		perviewOnboardingFile: `${globSetting.apiProName}/ow/onboardings/${id}/files/${globSetting.apiVersion}`,
		questionIds: `${globSetting.apiProName}/ow/questionnaires/${globSetting.apiVersion}/batch/by-ids`,

		filedForm: `${globSetting.apiProName}/ow/static-field-values/${globSetting.apiVersion}/onboarding/${id}`,
		staticFieldValuesByOnboarding: `${globSetting.apiProName}/ow/static-field-values/${globSetting.apiVersion}/by-onboarding/${id}`,
	};
};

// ========================= 客户入职相关接口 =========================

/**
 * 获取入职列表 [O01]
 * @returns List<OnboardingOutputDto>
 */
export function getOnboardingList() {
	return defHttp.get({ url: `${Api().onboardings}` });
}

/**
 * 获取统计信息 [O02]
 * @returns OnboardingStatisticsDto
 */
export function getOnboardingStatistics() {
	return defHttp.get({ url: `${Api().onboardingStatistics}` });
}

/**
 * 获取过期列表 [O03]
 * @returns List<OnboardingOutputDto>
 */
export function getOverdueOnboardings() {
	return defHttp.get({ url: `${Api().onboardingOverdue}` });
}

/**
 * 分页查询入职 [O04]
 * @param params OnboardingQueryRequest
 * @returns PagedResult<OnboardingOutputDto>
 */
export function queryOnboardings(params: any) {
	return defHttp.post({ url: `${Api().onboardingQuery}`, params });
}

/**
 * 创建客户入职 [O05]
 * @param params OnboardingInputDto
 * @returns long (入职ID)
 */
export function createOnboarding(params: any) {
	return defHttp.post({ url: `${Api().onboardings}`, params });
}

/**
 * 获取入职详情 [O06]
 * @param id 入职ID
 * @returns OnboardingOutputDto
 */
export function getOnboardingDetail(id: string | number) {
	return defHttp.get({ url: `${Api(id).onboarding}` });
}

/**
 * 更新客户入职 [O07]
 * @param id 入职ID
 * @param params OnboardingInputDto
 * @returns bool
 */
export function updateOnboarding(id: string | number, params: any) {
	return defHttp.put({ url: `${Api(id).onboarding}`, params });
}

/**
 * 删除客户入职 [O08]
 * @param id 入职ID
 * @param confirm 是否确认删除
 * @returns bool
 */
export function deleteOnboarding(id: string | number, confirm: boolean = false) {
	return defHttp.delete({
		url: `${Api(id).onboarding}?confirm=${confirm}`,
	});
}

/**
 * 根据Lead ID获取 [O09]
 * @param leadId Lead ID
 * @returns OnboardingOutputDto
 */
export function getOnboardingByLead(leadId: string) {
	return defHttp.get({ url: `${Api(leadId).onboarding}` });
}

/**
 * 移到下一阶段 [O10]
 * @param id 入职ID
 * @returns bool
 */
export function moveToNextStage(id: string | number) {
	return defHttp.post({ url: `${Api(id).onboardingNextStage}` });
}

/**
 * 移到上一阶段 [O11]
 * @param id 入职ID
 * @returns bool
 */
export function moveToPreviousStage(id: string | number) {
	return defHttp.post({ url: `${Api(id).onboardingPreviousStage}` });
}

/**
 * 移到指定阶段 [O12]
 * @param id 入职ID
 * @param params MoveToStageInputDto
 * @returns bool
 */
export function moveToStage(id: string | number, params: any) {
	return defHttp.post({ url: `${Api(id).onboardingMoveToStage}`, params });
}

/**
 * 完成当前阶段 [O13]
 * @param id 入职ID
 * @returns bool
 */
export function completeCurrentStage(id: string | number, params?: any) {
	return defHttp.post({ url: `${Api(id).onboardingCompleteStage}`, params });
}

/**
 * 暂停入职 [O15]
 * @param id 入职ID
 * @returns bool
 */
export function pauseOnboarding(id: string | number) {
	return defHttp.post({ url: `${Api(id).onboardingPause}` });
}

/**
 * 恢复入职 [O16]
 * @param id 入职ID
 * @returns bool
 */
export function resumeOnboarding(id: string | number) {
	return defHttp.post({ url: `${Api(id).onboardingResume}` });
}

/**
 * 取消入职 [O17]
 * @param id 入职ID
 * @param reason 取消原因
 * @returns bool
 */
export function cancelOnboarding(id: string | number, reason: string) {
	return defHttp.post({
		url: `${Api(id).onboardingCancel}`,
		params: { reason },
	});
}

/**
 * 分配入职 [O18]
 * @param id 入职ID
 * @param params AssignOnboardingInputDto
 * @returns bool
 */
export function assignOnboarding(id: string | number, params: any) {
	return defHttp.post({ url: `${Api(id).onboardingAssign}`, params });
}

/**
 * 更新完成率 [O19]
 * @param id 入职ID
 * @returns bool
 */
export function updateCompletionRate(id: string | number) {
	return defHttp.post({ url: `${Api(id).onboardingUpdateCompletionRate}` });
}

/**
 * 设置优先级 [O20]
 * @param id 入职ID
 * @param priority 优先级
 * @returns bool
 */
export function setOnboardingPriority(id: string | number, priority: string) {
	return defHttp.post({
		url: `${Api(id).onboardingSetPriority}`,
		params: { priority },
	});
}

/**
 * 批量更新状态 [O21]
 * @param params BatchUpdateStatusInputDto
 * @returns bool
 */
export function batchUpdateStatus(params: any) {
	return defHttp.post({ url: `${Api().onboardingBatchUpdateStatus}`, params });
}

// ========================= Lead同步相关接口 =========================

/**
 * 检查创建资格 [LS01]
 * @param lifecycleStageName 生命周期阶段名称
 * @returns bool
 */
export function shouldCreateOnboarding(lifecycleStageName: string) {
	return defHttp.get({
		url: `${Api().leadSyncShouldCreate}`,
		params: { lifecycleStageName },
	});
}

/**
 * 同步Lead到入职 [LS02]
 * @param params LeadSyncRequest
 * @returns long (入职ID)
 */
export function syncLeadToOnboarding(params: any) {
	return defHttp.post({ url: `${Api().leadSync}`, params });
}

/**
 * 同步Lead信息 [LS03]
 * @param params LeadInfoSyncRequest
 * @returns bool
 */
export function syncLeadInfo(params: any) {
	return defHttp.post({ url: `${Api().leadSyncInfo}`, params });
}

/**
 * 批量同步Lead [LS04]
 * @param params BatchSyncRequest
 * @returns BatchSyncResult
 */
export function batchSyncLeads(params: any) {
	return defHttp.post({ url: `${Api().leadBatchSync}`, params });
}

/**
 * 同步状态检查 [LS05]
 * @param leadId Lead ID
 * @returns SyncStatusDto
 */
export function getSyncStatus(leadId: string) {
	return defHttp.get({ url: `${Api(leadId).leadSyncStatus}` });
}

// ========================= 清单任务完成相关接口 =========================

/**
 * 获取所有任务完成记录 [CTC01]
 * @param onboardingId 入职ID（可选）
 * @param stageId 阶段ID（可选）
 * @returns List<ChecklistTaskCompletionDto>
 */
export function getAllTaskCompletions(onboardingId?: string | number, stageId?: string | number) {
	const params: any = {};
	if (onboardingId) {
		params.onboardingId = onboardingId;
	}
	if (stageId) {
		params.stageId = stageId;
	}

	return defHttp.get({
		url: `${Api().checklistTaskCompletions}`,
		params,
	});
}

/**
 * 保存任务完成记录 [CTC02]
 * @param params ChecklistTaskCompletionInputDto
 * @returns bool
 */
export function saveTaskCompletion(params: any) {
	return defHttp.post({ url: `${Api().checklistTaskCompletions}`, params });
}

/**
 * 批量保存任务完成记录 [CTC03]
 * @param params List<ChecklistTaskCompletionInputDto>
 * @returns bool
 */
export function batchSaveTaskCompletions(params: any) {
	return defHttp.post({ url: `${Api().checklistTaskCompletionsBatch}`, params });
}

/**
 * 按入职和清单获取完成记录 [CTC04]
 * @param onboardingId 入职ID
 * @param checklistId 清单ID
 * @returns List<ChecklistTaskCompletionDto>
 */
export function getTaskCompletionsByOnboardingAndChecklist(
	onboardingId: string | number,
	checklistId: string | number
) {
	return defHttp.get({
		url: `${Api(onboardingId).checklistTaskCompletionsByOnboardingAndChecklist}/${checklistId}`,
	});
}

/**
 * 按入职和阶段获取完成记录 [CTC05] - 新增
 * @param onboardingId 入职ID
 * @param stageId 阶段ID
 * @returns List<ChecklistTaskCompletionDto>
 */
export function getTaskCompletionsByOnboardingAndStage(
	onboardingId: string | number,
	stageId: string | number
) {
	return getAllTaskCompletions(onboardingId, stageId);
}

/**
 * 获取完成统计信息 [CTC05]
 * @param onboardingId 入职ID
 * @param checklistId 清单ID
 * @returns object (统计信息)
 */
export function getTaskCompletionStats(
	onboardingId: string | number,
	checklistId: string | number
) {
	return defHttp.get({
		url: `${Api(onboardingId).checklistTaskCompletionsStats}/${checklistId}/stats`,
	});
}

// ========================= 阶段完成日志相关接口 =========================

/**
 * 获取所有阶段完成日志 [SC01]
 * @returns List<StageCompletionLogDto>
 */
export function getAllStageCompletionLogs() {
	return defHttp.get({ url: `${Api().stageCompletionLogs}` });
}

/**
 * 按入职获取完成日志 [SC03]
 * @param onboardingId 入职ID
 * @returns List<StageCompletionLogDto>
 */
export function getStageCompletionLogsByOnboarding(onboardingId: string | number) {
	return defHttp.get({ url: `${Api(onboardingId).stageCompletionLogsByOnboarding}` });
}

/**
 * 获取阶段完成统计 [SC05]
 * @param params 查询参数
 * @returns object (统计信息)
 */
export function getStageCompletionStatistics(params?: any) {
	return defHttp.get({
		url: `${Api().stageCompletionLogsStatistics}`,
		params,
	});
}

/**
 * 批量创建阶段完成日志
 * @param params List<StageCompletionLogInputDto>
 * @returns bool
 */
export function batchCreateStageCompletionLogs(params: any) {
	return defHttp.post({ url: `${Api().stageCompletionLogsBatch}`, params });
}

// ========================= 内部备注相关接口 =========================

/**
 * 获取所有内部备注(分页) [IN01]
 * @param params 分页参数
 * @returns PagedResult<InternalNoteDto>
 */
export function getInternalNotesPaged(params: any) {
	return defHttp.get({
		url: `${Api().internalNotesPaged}`,
		params,
	});
}

/**
 * 创建内部备注 [IN02]
 * @param params InternalNoteInputDto
 * @returns bool
 */
export function createInternalNote(params: any) {
	return defHttp.post({ url: `${Api().internalNotes}`, params });
}

/**
 * 更新内部备注 [IN02-UPDATE]
 * @param id 备注ID
 * @param params InternalNoteInputDto
 * @returns bool
 */
export function updateInternalNote(id: string | number, params: any) {
	return defHttp.put({ url: `${Api().internalNotes}/${id}`, params });
}

/**
 * 删除内部备注 [IN02-DELETE]
 * @param id 备注ID
 * @returns bool
 */
export function deleteInternalNote(id: string | number) {
	return defHttp.delete({ url: `${Api().internalNotes}/${id}` });
}

/**
 * 按入职获取备注 [IN03]
 * @param onboardingId 入职ID
 * @returns List<InternalNoteDto>
 */
export function getInternalNotesByOnboarding(onboardingId: string | number, stageId: string) {
	return defHttp.get({
		url: `${Api(onboardingId).internalNotesByOnboarding}/stage/${stageId}`,
	});
}

/**
 * 获取未解决备注 [IN04]
 * @param onboardingId 入职ID
 * @returns List<InternalNoteDto>
 */
export function getUnresolvedInternalNotes(onboardingId: string | number) {
	return defHttp.get({ url: `${Api(onboardingId).internalNotesUnresolved}` });
}

// ========================= 问卷答案相关接口 =========================

/**
 * 按入职获取问卷答案 [QA01]
 * @param onboardingId 入职ID
 * @returns object (答案数据)
 */
export function getQuestionnaireAnswersByOnboarding(onboardingId: string | number) {
	return defHttp.get({ url: `${Api(onboardingId).questionnaireAnswers}` });
}

/**
 * 保存问卷答案 [QA02]
 * @param onboardingId 入职ID
 * @param stageId 阶段ID
 * @param params QuestionnaireAnswerInputDto
 * @returns bool
 */
export function saveQuestionnaireAnswer(
	onboardingId: string | number,
	stageId: string | number,
	params: any
) {
	return defHttp.post({
		url: `${Api(onboardingId).questionnaireAnswer}/${stageId}/answer`,
		params,
	});
}

/**
 * 保存问卷静态字段 [QA08]
 * @param params 静态字段数据
 * @returns bool
 */
export function saveQuestionnaireStatic(params: any) {
	return defHttp.post({
		url: `${Api().staticQuestionnaireAnswer}`,
		params,
	});
}

/**
 * 批量更新静态字段值
 * @param onboardingId 入职ID
 * @param fieldValues 字段值数组
 * @returns bool
 */
export function batchUpdateStaticFieldValues(
	onboardingId: string | number,
	fieldValues: Array<{
		fieldName: string;
		fieldValueJson: string;
		fieldType: string;
		isRequired: boolean;
		fieldLabel: string;
	}>
) {
	return defHttp.post({
		url: `${Api().staticQuestionnaireAnswer}`,
		params: {
			fieldValues,
			onboardingId: String(onboardingId),
		},
	});
}

/**
 * 获取问卷答案 [QA03]
 * @param onboardingId 入职ID
 * @param stageId 阶段ID
 * @returns object (答案数据)
 */
export function getQuestionnaireAnswer(onboardingId: string | number, stageId: string | number) {
	return defHttp.get({ url: `${Api(onboardingId).questionnaireAnswer}/${stageId}/answer` });
}

/**
 * 提交问卷答案 [QA04]
 * @param onboardingId 入职ID
 * @param stageId 阶段ID
 * @returns bool
 */
export function submitQuestionnaireAnswer(onboardingId: string | number, stageId: string | number) {
	return defHttp.post({
		url: `${Api(onboardingId).questionnaireAnswerSubmit}/${stageId}/submit`,
	});
}

/**
 * 获取答案历史 [QA05]
 * @param onboardingId 入职ID
 * @param stageId 阶段ID
 * @returns object (历史版本)
 */
export function getQuestionnaireAnswerHistory(
	onboardingId: string | number,
	stageId: string | number
) {
	return defHttp.get({
		url: `${Api(onboardingId).questionnaireAnswerHistory}/${stageId}/history`,
	});
}

/**
 * 获取答案统计 [QA06]
 * @param params 查询参数
 * @returns object (统计信息)
 */
export function getQuestionnaireAnswersStatistics(params?: any) {
	return defHttp.get({
		url: `${Api().questionnaireAnswersStatistics}`,
		params,
	});
}

/**
 * 审核答案
 * @param params QuestionnaireAnswerReviewDto
 * @returns bool
 */
export function reviewQuestionnaireAnswers(params: any) {
	return defHttp.post({ url: `${Api().questionnaireAnswerReview}`, params });
}

/**
 * 按状态获取答案
 * @param status 状态
 * @param params 查询参数
 * @returns object (答案列表)
 */
export function getQuestionnaireAnswersByStatus(status: string, params?: any) {
	return defHttp.get({
		url: `${Api(status).questionnaireAnswersByStatus}`,
		params,
	});
}

export function getStageFieldValues(onboardingId: string | number, stageId: string | number) {
	return defHttp.get({ url: `${Api(onboardingId).filedForm}/stage/${stageId}` });
}

/**
 * 获取入职的所有静态字段值（按 onboarding ID）
 * @param onboardingId 入职ID
 * @returns object (静态字段值)
 */
export function getStaticFieldValuesByOnboarding(onboardingId: string | number) {
	return defHttp.get({ url: `${Api(onboardingId).staticFieldValuesByOnboarding}` });
}

export function getCheckList(onboardingId: string | number) {
	return defHttp.get({ url: `${Api(onboardingId).checkList}` });
}

/**
 * 保存检查清单任务完成状态
 * @param params ChecklistTaskCompletionInputDto - 包括 onboardingId, taskId, checklistId, isCompleted, stageId 等
 * @returns bool
 */
export function saveCheckListTask(params: any) {
	return defHttp.post({ url: `${Api().checkListTask}`, params });
}

export function getCheckListIsCompleted(onboardingId: string | number, stageId: string | number) {
	return defHttp.get({
		url: `${Api(onboardingId).checklistTaskCompletions}`,
		params: {
			onboardingId,
			stageId,
		},
	});
}

export function exportOnboarding() {
	return defHttp.get({
		url: `${Api().exportOnboarding}`,
		responseType: 'blob',
	});
}

// ========================= 文件管理相关接口 =========================

/**
 * 上传单个文件 [OF01]
 * @param onboardingId 入职ID
 * @param params 上传参数 {name: string, file: File, filename: string}
 * @param onUploadProgress 上传进度回调函数
 * @returns object (文件信息)
 */
export function uploadOnboardingFile(
	onboardingId: string,
	params: any,
	onUploadProgress?: (progressEvent: any) => void
) {
	return defHttp.uploadFile(
		{
			url: `${Api(onboardingId).onboardingFiles}`,
			onUploadProgress,
		},
		params
	);
}

/**
 * 批量上传文件 [OF02]
 * @param onboardingId 入职ID
 * @param params FormData 包含多个文件
 * @returns object (批量上传结果)
 */
export function batchUploadOnboardingFiles(onboardingId: string | number, params: FormData) {
	return defHttp.post({
		url: `${Api(onboardingId).onboardingFilesBatch}`,
		params,
		headers: {
			'Content-Type': 'multipart/form-data',
		},
	});
}

/**
 * 获取文件列表 [OF03]
 * @param onboardingId 入职ID
 * @returns List<OnboardingFileDto>
 */
export function getOnboardingFiles(onboardingId: string | number) {
	return defHttp.get({ url: `${Api(onboardingId).onboardingFiles}` });
}

/**
 * 删除文件 [OF06]
 * @param onboardingId 入职ID
 * @param fileId 文件ID
 * @returns bool
 */
export function deleteOnboardingFile(onboardingId: string | number, fileId: string | number) {
	return defHttp.delete({ url: `${Api(onboardingId).onboardingFile}/${fileId}` });
}

/**
 * 按阶段获取文件 [OF10]
 * @param onboardingId 入职ID
 * @param stageId 阶段ID
 * @returns List<OnboardingFileDto>
 */
export function getOnboardingFilesByStage(onboardingId: string | number, stageId: string | number) {
	return defHttp.get({ url: `${Api(onboardingId).onboardingFiles}?stageId=${stageId}` });
}

export function previewOnboardingFile(
	onboardingId: string | number,
	fileId: string | number,
	onDownloadProgress?: (progressEvent) => void
) {
	return defHttp.get({
		url: `${Api(onboardingId).perviewOnboardingFile}/${fileId}/preview`,
		responseType: 'blob',
		timeout: 60 * 1000, // 修复：60秒超时
		onDownloadProgress,
	});
}

export function getCheckListIds(params: any) {
	return defHttp.post({ url: `${Api().ckeckListIds}`, params });
}

export function getQuestionIds(params: any) {
	return defHttp.post({ url: `${Api().questionIds}`, params });
}
