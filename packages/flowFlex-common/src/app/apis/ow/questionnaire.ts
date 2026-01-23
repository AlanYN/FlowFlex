import { defHttp } from '@/apis/axios';
import { useGlobSetting } from '@/settings';

const globSetting = useGlobSetting();

const Api = (id?: string | number) => {
	return {
		// 问卷相关API
		questionnaires: `${globSetting.apiProName}/ow/questionnaires/${globSetting.apiVersion}`,
		questionnaire: `${globSetting.apiProName}/ow/questionnaires/${globSetting.apiVersion}/${id}`,
		questionnaireTemplates: `${globSetting.apiProName}/ow/questionnaires/${globSetting.apiVersion}/templates`,
		questionnaireQuery: `${globSetting.apiProName}/ow/questionnaires/${globSetting.apiVersion}/query`,
		questionnairePreview: `${globSetting.apiProName}/ow/questionnaires/${globSetting.apiVersion}/${id}/preview`,
		questionnaireValidate: `${globSetting.apiProName}/ow/questionnaires/${globSetting.apiVersion}/${id}/validate`,
		questionnaireUpdateStatistics: `${globSetting.apiProName}/ow/questionnaires/${globSetting.apiVersion}/${id}/update-statistics`,
		questionnairePublish: `${globSetting.apiProName}/ow/questionnaires/${globSetting.apiVersion}/${id}/publish`,
		questionnaireDuplicate: `${globSetting.apiProName}/ow/questionnaires/${globSetting.apiVersion}/${id}/duplicate`,
		questionnaireCreateFromTemplate: `${globSetting.apiProName}/ow/questionnaires/${globSetting.apiVersion}/templates/${id}/create`,
		questionnaireArchive: `${globSetting.apiProName}/ow/questionnaires/${globSetting.apiVersion}/${id}/archive`,

		questionFileUpload: `${globSetting.apiProName}/ow/questionnaires/${globSetting.apiVersion}/questions/upload-file`,
	};
};

// ========================= 问卷相关接口 =========================

/**
 * 获取问卷列表 [Q01]
 * @param category 问卷分类 (可选)
 * @returns List<QuestionnaireOutputDto>
 */
export function getQuestionnaireList(category?: string) {
	const params = category ? { category } : undefined;
	return defHttp.get({ url: `${Api().questionnaires}`, params });
}

/**
 * 获取模板列表 [Q02]
 * @returns List<QuestionnaireOutputDto>
 */
export function getQuestionnaireTemplates() {
	return defHttp.get({ url: `${Api().questionnaireTemplates}` });
}

/**
 * 查询问卷(分页) [Q03]
 * @param params QuestionnaireQueryRequest
 * @returns PagedResult<QuestionnaireOutputDto>
 */
export function queryQuestionnaires(params: any) {
	return defHttp.post({ url: `${Api().questionnaireQuery}`, params });
}

/**
 * 创建问卷 [Q04]
 * @param params QuestionnaireInputDto
 * @returns long (问卷ID)
 */
export function createQuestionnaire(params: any) {
	return defHttp.post({ url: `${Api().questionnaires}`, params });
}

/**
 * 获取问卷详情 [Q05]
 * @param id 问卷ID
 * @returns QuestionnaireOutputDto
 */
export function getQuestionnaireDetail(id: string | number) {
	return defHttp.get({ url: `${Api(id).questionnaire}` });
}

/**
 * 预览问卷 [Q06]
 * @param id 问卷ID
 * @returns QuestionnaireOutputDto
 */
export function previewQuestionnaire(id: string | number) {
	return defHttp.get({ url: `${Api(id).questionnairePreview}` });
}

/**
 * 更新问卷 [Q07]
 * @param id 问卷ID
 * @param params QuestionnaireInputDto
 * @returns bool
 */
export function updateQuestionnaire(id: string | number, params: any) {
	return defHttp.put({ url: `${Api(id).questionnaire}`, params });
}

/**
 * 验证问卷结构 [Q08]
 * @param id 问卷ID
 * @returns bool
 */
export function validateQuestionnaire(id: string | number) {
	return defHttp.post({ url: `${Api(id).questionnaireValidate}` });
}

/**
 * 更新统计信息 [Q09]
 * @param id 问卷ID
 * @returns bool
 */
export function updateQuestionnaireStatistics(id: string | number) {
	return defHttp.post({ url: `${Api(id).questionnaireUpdateStatistics}` });
}

/**
 * 发布问卷 [Q10]
 * @param id 问卷ID
 * @returns bool
 */
export function publishQuestionnaire(id: string | number) {
	return defHttp.post({ url: `${Api(id).questionnairePublish}` });
}

/**
 * 复制问卷 [Q11]
 * @param id 问卷ID
 * @param params DuplicateQuestionnaireInputDto
 * @returns long (新问卷ID)
 */
export function duplicateQuestionnaire(id: string | number, params: any) {
	return defHttp.post({ url: `${Api(id).questionnaireDuplicate}`, params });
}

/**
 * 从模板创建问卷 [Q12]
 * @param templateId 模板ID
 * @param params QuestionnaireInputDto
 * @returns long (问卷ID)
 */
export function createQuestionnaireFromTemplate(templateId: string | number, params: any) {
	return defHttp.post({ url: `${Api(templateId).questionnaireCreateFromTemplate}`, params });
}

/**
 * 归档问卷 [Q13]
 * @param id 问卷ID
 * @returns bool
 */
export function archiveQuestionnaire(id: string | number) {
	return defHttp.post({ url: `${Api(id).questionnaireArchive}` });
}

/**
 * 删除问卷 [Q14-Q16]
 * @param id 问卷ID
 * @param confirm 是否确认删除
 * @returns bool 或 CRMResponse
 */
export function deleteQuestionnaire(id: string | number, confirm?: boolean) {
	return defHttp.delete({ url: `${Api(id).questionnaire}?confirm=${!!confirm}` });
}

// ========================= 问卷答案相关接口 =========================

const AnswerApi = (
	onboardingId?: string | number,
	stageId?: string | number,
	answerId?: string | number
) => {
	return {
		// 问卷答案相关API
		questionnaireAnswers: `${globSetting.apiProName}/ow/questionnaire-answers/${globSetting.apiVersion}/${onboardingId}/answers`,
		saveAnswer: `${globSetting.apiProName}/ow/questionnaire-answers/${globSetting.apiVersion}/${onboardingId}/stage/${stageId}/answer`,
		getAnswer: `${globSetting.apiProName}/ow/questionnaire-answers/${globSetting.apiVersion}/${onboardingId}/stage/${stageId}/answer`,
		submitAnswer: `${globSetting.apiProName}/ow/questionnaire-answers/${globSetting.apiVersion}/${onboardingId}/stage/${stageId}`,
		answerHistory: `${globSetting.apiProName}/ow/questionnaire-answers/${globSetting.apiVersion}/${onboardingId}/stage/${stageId}/history`,
		answerStatistics: `${globSetting.apiProName}/ow/questionnaire-answers/${globSetting.apiVersion}/statistics`,
		updateAnswer: `${globSetting.apiProName}/ow/questionnaire-answers/${globSetting.apiVersion}/${answerId}`,
		reviewAnswer: `${globSetting.apiProName}/ow/questionnaire-answers/${globSetting.apiVersion}/review`,
		answersByStatus: `${globSetting.apiProName}/ow/questionnaire-answers/${globSetting.apiVersion}/by-status`,
	};
};

/**
 * 按入职获取问卷答案 [QA01]
 * @param onboardingId 入职ID
 * @returns object (答案列表)
 */
export function getQuestionnaireAnswersByOnboarding(onboardingId: string | number) {
	return defHttp.get({ url: `${AnswerApi(onboardingId).questionnaireAnswers}` });
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
	return defHttp.post({ url: `${AnswerApi(onboardingId, stageId).saveAnswer}`, params });
}

/**
 * 获取问卷答案 [QA03]
 * @param onboardingId 入职ID
 * @param stageId 阶段ID
 * @returns object (答案数据)
 */
export function getQuestionnaireAnswer(onboardingId: string | number, stageId: string | number) {
	return defHttp.get({ url: `${AnswerApi(onboardingId, stageId).getAnswer}` });
}

/**
 * 提交问卷答案 [QA04]
 * @param onboardingId 入职ID
 * @param stageId 阶段ID
 * @returns bool
 */
export function submitQuestionnaireAnswer(
	onboardingId: string | number,
	stageId: string | number,
	questionnaireId: string | number
) {
	return defHttp.post({
		url: `${
			AnswerApi(onboardingId, stageId).submitAnswer
		}/questionnaire/${questionnaireId}/submit`,
	});
}

/**
 * 重置问卷答案 [QA04]
 * @param onboardingId 入职ID
 * @param stageId 阶段ID
 * @returns bool
 */
export function reopenQuestionnaireAnswer(
	onboardingId: string | number,
	stageId: string | number,
	questionnaireId: string | number
) {
	return defHttp.post({
		url: `${
			AnswerApi(onboardingId, stageId).submitAnswer
		}/questionnaire/${questionnaireId}/reopen`,
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
	return defHttp.get({ url: `${AnswerApi(onboardingId, stageId).answerHistory}` });
}

/**
 * 获取答案统计 [QA06]
 * @param stageId 阶段ID (可选)
 * @param days 天数 (可选)
 * @returns object (统计信息)
 */
export function getQuestionnaireAnswerStatistics(stageId?: string | number, days?: number) {
	const params: any = {};
	if (stageId) params.stageId = stageId;
	if (days) params.days = days;
	return defHttp.get({ url: `${AnswerApi().answerStatistics}`, params });
}

/**
 * 更新答案 [QA03]
 * @param answerId 答案ID
 * @param params QuestionnaireAnswerUpdateDto
 * @returns bool
 */
export function updateQuestionnaireAnswer(answerId: string | number, params: any) {
	return defHttp.put({
		url: `${AnswerApi(undefined, undefined, answerId).updateAnswer}`,
		params,
	});
}

/**
 * 删除答案
 * @param answerId 答案ID
 * @returns bool
 */
export function deleteQuestionnaireAnswer(answerId: string | number) {
	return defHttp.delete({ url: `${AnswerApi(undefined, undefined, answerId).updateAnswer}` });
}

/**
 * 审核答案 [QA08]
 * @param params QuestionnaireAnswerReviewDto
 * @returns bool
 */
export function reviewQuestionnaireAnswer(params: any) {
	return defHttp.post({ url: `${AnswerApi().reviewAnswer}`, params });
}

/**
 * 按状态获取答案 [QA10]
 * @param status 状态
 * @param days 天数 (可选)
 * @returns object (答案列表)
 */
export function getQuestionnaireAnswersByStatus(status: string, days?: number) {
	const params: any = {};
	if (days) params.days = days;
	return defHttp.get({ url: `${AnswerApi().answersByStatus}/${status}`, params });
}

// ========================= 批量接口 =========================

/**
 * 批量获取多个Stage的问卷 [BATCH01]
 * @param stageIds Stage ID数组
 * @returns object (按stageId分组的问卷数据)
 */
export function getStageQuestionnairesBatch(stageIds: (string | number)[]) {
	return defHttp.post({
		url: `${globSetting.apiProName}/ow/questionnaires/${globSetting.apiVersion}/batch/by-stages`,
		data: { stageIds },
	});
}

/**
 * 批量获取多个Stage的问卷答案 [BATCH02]
 * @param onboardingId 入职ID
 * @param stageIds Stage ID数组
 * @returns object (按stageId分组的答案数据)
 */
export function getQuestionnaireAnswersBatch(
	onboardingId: string | number,
	stageIds: (string | number)[]
) {
	return defHttp.post({
		url: `${globSetting.apiProName}/ow/questionnaire-answers/${globSetting.apiVersion}/batch/by-stages`,
		params: { onboardingId, stageIds },
	});
}

export function uploadQuestionFile(
	params: any,
	onUploadProgress?: (progressEvent: any) => void,
	signal?: AbortSignal
) {
	return defHttp.uploadFile(
		{
			url: `${Api().questionFileUpload}`,
			onUploadProgress,
			signal,
		},
		params
	);
}
