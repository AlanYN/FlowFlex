import { defHttp } from '@/apis/axios';
import { useGlobSetting } from '@/settings';

const globSetting = useGlobSetting();

const Api = (id?: string | number) => {
	return {
		// Change Log 相关API
		changeLogs: `${globSetting.apiProName}/ow/change-logs/${globSetting.apiVersion}`,
		changeLogById: `${globSetting.apiProName}/ow/change-logs/${globSetting.apiVersion}/${id}`,
		changeLogByBusiness: `${globSetting.apiProName}/ow/change-logs/${globSetting.apiVersion}/business`,
		changeLogByOnboarding: `${globSetting.apiProName}/ow/change-logs/${globSetting.apiVersion}/onboarding/${id}`,
	};
};

// ========================= Change Log 查询参数接口 =========================

export interface ChangeLogQueryParams {
	onboardingId?: number; // 入职ID
	stageId?: number | string; // 阶段ID - 支持字符串以避免大整数精度丢失
	operationType?: string; // 操作类型
	startDate?: string; // 开始时间
	endDate?: string; // 结束时间
	pageIndex?: number; // 页码
	pageSize?: number; // 页大小
	includeActionExecutions?: boolean; // 是否包含 Action 执行记录
}

// ========================= Change Log 数据接口 =========================

export interface ChangeLogItem {
	id: number | string;
	type: string; // 操作类型标签
	typeIcon?: string; // 类型图标（可选，如果后端没有提供则前端生成）
	typeColor?: string; // 类型颜色（可选，如果后端没有提供则前端生成）
	details: string; // 详细描述
	operationTitle?: string; // 操作标题
	operationDescription?: string; // 操作详细描述
	beforeData?: any; // 变更前数据
	afterData?: any; // 变更后数据
	changedFields?: string[]; // 变更字段
	updatedBy: string; // 操作人
	dateTime: string; // 操作时间
	extendedInfo?: any; // 扩展信息
}

export interface ChangeLogResponse {
	code: number | string; // API可能返回数字或字符串
	message?: string;
	msg?: string; // 有些API使用msg字段
	success?: boolean;
	data: {
		items: ChangeLogItem[];
		totalCount: number;
		pageIndex: number;
		pageSize: number;
	};
}

// ========================= Change Log API 接口 =========================

/**
 * 获取Change Log列表 [CL01]
 * @param params 查询参数
 * @returns PagedResult<ChangeLogItem>
 */
export function getChangeLogs(params?: ChangeLogQueryParams) {
	return defHttp.get<ChangeLogResponse>({
		url: Api().changeLogs,
		params,
	});
}

/**
 * 获取特定业务对象的Change Log [CL02]
 * @param businessModule 业务模块 (QuestionnaireAnswer, ChecklistTask, etc.)
 * @param businessId 业务ID
 * @param params 查询参数
 * @returns List<ChangeLogItem>
 */
export function getChangeLogsByBusiness(
	businessModule: string,
	businessId: string | number,
	params?: ChangeLogQueryParams
) {
	return defHttp.get<ChangeLogResponse>({
		url: `${Api().changeLogByBusiness}/${businessModule}/${businessId}`,
		params,
	});
}

/**
 * 获取入职流程的完整Change Log [CL03]
 * @param onboardingId 入职ID
 * @param params 查询参数
 * @returns List<ChangeLogItem>
 */
export function getChangeLogsByOnboarding(
	onboardingId: string | number,
	params?: ChangeLogQueryParams
) {
	const url = Api(onboardingId).changeLogByOnboarding;
	console.log('[API] getChangeLogsByOnboarding:', { url, params });

	return defHttp.get<ChangeLogResponse>({
		url,
		params,
	});
}

/**
 * 获取单个Change Log详情 [CL04]
 * @param id Change Log ID
 * @returns ChangeLogItem
 */
export function getChangeLogDetail(id: string | number) {
	return defHttp.get<{ code: number; message: string; data: ChangeLogItem }>({
		url: Api(id).changeLogById,
	});
}

// ========================= 数据解析工具函数 =========================

/**
 * 解析问卷答案变更详情
 */
export function parseQuestionnaireAnswerChanges(beforeData: any, afterData: any): string[] {
	// 如果没有 afterData，返回空数组
	if (!afterData) return [];

	try {
		const after = typeof afterData === 'string' ? JSON.parse(afterData) : afterData;
		const changes: string[] = [];

		// 处理问卷答案提交的情况（只有 afterData）
		if (!beforeData && after.responses) {
			// 这是新提交的问卷答案
			after.responses.forEach((response: any) => {
				if (response.answer || response.responseText) {
					const formattedAnswer = formatAnswerForDisplay(response);
					changes.push(`${response.question || response.questionId}: ${formattedAnswer}`);
				}
			});
			return changes;
		}

		// 处理问卷答案更新的情况（有 beforeData 和 afterData）
		if (beforeData && afterData) {
			const before = typeof beforeData === 'string' ? JSON.parse(beforeData) : beforeData;

			// 对比问卷答案变化
			if (before.responses && after.responses) {
				// 创建一个映射来比较相同questionId的答案
				const beforeMap = new Map();
				const afterMap = new Map();

				before.responses.forEach((resp: any) => {
					beforeMap.set(resp.questionId, resp);
				});

				after.responses.forEach((resp: any) => {
					afterMap.set(resp.questionId, resp);
				});

				// 比较变化
				afterMap.forEach((afterResp: any, questionId: string) => {
					const beforeResp = beforeMap.get(questionId);

					if (!beforeResp) {
						// 新增的答案
						const formattedAnswer = formatAnswerForDisplay(afterResp);
						changes.push(`${afterResp.question || questionId}: ${formattedAnswer}`);
					} else if (
						JSON.stringify(beforeResp.answer) !== JSON.stringify(afterResp.answer)
					) {
						// 修改的答案
						const beforeAnswer = formatAnswerForDisplay(beforeResp);
						const afterAnswer = formatAnswerForDisplay(afterResp);
						changes.push(
							`${afterResp.question || questionId}: ${beforeAnswer} → ${afterAnswer}`
						);
					}
				});
			}
		}

		return changes;
	} catch (error) {
		console.error('Error parsing questionnaire answer changes:', error);
		return [];
	}
}

/**
 * 格式化答案用于显示
 */
function formatAnswerForDisplay(response: any): string {
	if (!response.answer && !response.responseText) {
		return 'No answer';
	}

	const answer = response.answer || response.responseText;
	const type = response.type;

	switch (type) {
		case 'multiple_choice':
			// 处理单选题 - 尝试从问题配置中获取对应的 label
			return (
				getChoiceLabel(answer, response.questionConfig || response.config) || String(answer)
			);

		case 'dropdown':
			// 处理下拉选择 - 尝试从问题配置中获取对应的 label
			return (
				getChoiceLabel(answer, response.questionConfig || response.config) || String(answer)
			);

		case 'checkboxes':
			// 处理复选框 - 获取多个选项的 labels
			return getCheckboxLabels(answer, response.questionConfig || response.config);

		case 'file':
		case 'file_upload':
			// 处理文件上传
			if (Array.isArray(answer)) {
				const fileNames = answer.map((file: any) => {
					if (typeof file === 'object' && file.name) {
						return file.name;
					}
					return 'Unknown file';
				});
				return `Files: ${fileNames.join(', ')}`;
			} else if (typeof answer === 'object' && answer.name) {
				return `File: ${answer.name}`;
			} else if (typeof answer === 'string' && answer !== '[object Object]') {
				return `File: ${answer}`;
			}
			return 'File uploaded';

		case 'checkbox_grid':
		case 'multiple_choice_grid':
			// 处理网格类型
			if (Array.isArray(answer)) {
				return `Grid: ${answer.join(', ')}`;
			}
			return `Grid: ${answer}`;

		case 'rating':
			return `${answer}/5`;

		case 'linear_scale':
			return `${answer}/10`;

		default:
			return String(answer);
	}
}

/**
 * 获取单选或下拉选择的 label
 */
function getChoiceLabel(answer: string, questionConfig: any): string | null {
	if (!answer || !questionConfig?.options) {
		return null;
	}

	// 查找匹配的选项
	const option = questionConfig.options.find((opt: any) => opt.value === answer);
	return option?.label || null;
}

/**
 * 获取多选题的 labels
 */
function getCheckboxLabels(answer: any, questionConfig: any): string {
	if (!answer) {
		return 'No answer';
	}

	// 首先处理答案格式
	let answerValues: string[] = [];

	if (Array.isArray(answer)) {
		answerValues = answer.map((item) => String(item)).filter(Boolean);
	} else {
		const answerStr = String(answer);
		try {
			// 尝试解析 JSON 数组
			const parsed = JSON.parse(answerStr);
			if (Array.isArray(parsed)) {
				answerValues = parsed.map((item) => String(item)).filter(Boolean);
			} else {
				// 如果不是数组，按逗号分割
				answerValues = answerStr
					.split(',')
					.map((item) => item.trim())
					.filter(Boolean);
			}
		} catch {
			// 解析失败，按逗号分割
			answerValues = answerStr
				.split(',')
				.map((item) => item.trim())
				.filter(Boolean);
		}
	}

	// 如果没有选项配置，直接返回值
	if (!questionConfig?.options) {
		return answerValues.join(', ');
	}

	// 创建值到标签的映射
	const optionMap = new Map<string, string>();
	questionConfig.options.forEach((option: any) => {
		optionMap.set(option.value, option.label);
	});

	// 将值转换为标签
	const labels = answerValues.map((value) => optionMap.get(value) || value);
	return labels.join(', ');
}

/**
 * 解析任务状态变更详情
 */
export function parseTaskStatusChanges(beforeData: any, afterData: any): string {
	if (!beforeData || !afterData) return '';

	try {
		const before = typeof beforeData === 'string' ? JSON.parse(beforeData) : beforeData;
		const after = typeof afterData === 'string' ? JSON.parse(afterData) : afterData;

		const taskName = before.TaskName || after.TaskName || 'Task';
		const statusFrom = before.IsCompleted ? 'Completed' : 'Incomplete';
		const statusTo = after.IsCompleted ? 'Completed' : 'Incomplete';

		return `${taskName}: ${statusFrom} → ${statusTo}`;
	} catch (error) {
		console.error('Error parsing task status changes:', error);
		return '';
	}
}

/**
 * 解析静态字段变更详情
 */
export function parseStaticFieldChanges(
	beforeData: any,
	afterData: any,
	changedFields?: string[]
): Array<{
	fieldName: string;
	beforeValue: string;
	afterValue: string;
}> {
	// 如果没有 afterData，返回空数组
	if (!afterData) return [];

	try {
		const after = typeof afterData === 'string' ? JSON.parse(afterData) : afterData;
		const changes: Array<{
			fieldName: string;
			beforeValue: string;
			afterValue: string;
		}> = [];

		// 处理静态字段新增/设置的情况（只有 afterData）
		if (!beforeData && after.fieldName && after.value !== undefined) {
			// 这是新设置的静态字段值
			let afterValue = after.value;

			// 如果值是被双重JSON编码的字符串，进行解析
			try {
				if (
					typeof afterValue === 'string' &&
					afterValue.startsWith('"') &&
					afterValue.endsWith('"')
				) {
					afterValue = JSON.parse(afterValue);
				}
			} catch {
				// 如果解析失败，保持原值
			}

			changes.push({
				fieldName: after.fieldName,
				beforeValue: '', // 新设置时没有原值
				afterValue: String(afterValue || ''),
			});
			return changes;
		}

		// 处理静态字段更新的情况（有 beforeData 和 afterData）
		if (beforeData && afterData) {
			const before = typeof beforeData === 'string' ? JSON.parse(beforeData) : beforeData;

			// 处理特殊的 API 数据格式 (如您提供的示例)
			if (
				before.fieldName &&
				after.fieldName &&
				before.value !== undefined &&
				after.value !== undefined
			) {
				// 处理格式: {"value": "\"3333.00\"", "fieldName": "REQUESTEDCREDITLIMIT"}
				let beforeValue = before.value;
				let afterValue = after.value;

				// 如果值是被双重JSON编码的字符串，进行解析
				try {
					if (
						typeof beforeValue === 'string' &&
						beforeValue.startsWith('"') &&
						beforeValue.endsWith('"')
					) {
						beforeValue = JSON.parse(beforeValue);
					}
					if (
						typeof afterValue === 'string' &&
						afterValue.startsWith('"') &&
						afterValue.endsWith('"')
					) {
						afterValue = JSON.parse(afterValue);
					}
				} catch {
					// 如果解析失败，保持原值
				}

				changes.push({
					fieldName: before.fieldName || after.fieldName,
					beforeValue: String(beforeValue || ''),
					afterValue: String(afterValue || ''),
				});
			} else {
				// 处理常规的对象格式变更
				const fieldsToCompare = changedFields || Object.keys({ ...before, ...after });

				fieldsToCompare.forEach((field) => {
					if (before[field] !== after[field]) {
						changes.push({
							fieldName: field,
							beforeValue: String(before[field] || ''),
							afterValue: String(after[field] || ''),
						});
					}
				});
			}
		}

		return changes;
	} catch (error) {
		console.error('Error parsing static field changes:', error);
		return [];
	}
}

/**
 * 获取操作类型显示信息
 */
export function getOperationTypeInfo(operationType: string) {
	const typeMap = {
		Update: { label: 'Update', icon: '✏️', color: 'gray' },
		Completion: { label: 'Completion', icon: '✅', color: 'blue' },
		ChecklistTaskComplete: { label: 'Task Complete', icon: '✅', color: 'green' },
		ChecklistTaskUncomplete: { label: 'Task Incomplete', icon: '❌', color: 'orange' },
		QuestionnaireAnswerUpdate: { label: 'Answer Update', icon: '📝', color: 'purple' },
		QuestionnaireAnswerSubmit: { label: 'Answer Submit', icon: '📋', color: 'blue' },
		FileUpload: { label: 'File Upload', icon: '📎', color: 'cyan' },
		StaticFieldValueChange: { label: 'Field Change', icon: '🔧', color: 'yellow' },
		StageTransition: { label: 'Stage Move', icon: '🔄', color: 'blue' },
		PriorityChange: { label: 'Priority', icon: '⚡', color: 'red' },
		// Action Execution 相关类型
		ActionExecutionSuccess: { label: 'Action Success', icon: '🎯', color: 'green' },
		ActionExecutionFailed: { label: 'Action Failed', icon: '❌', color: 'red' },
		ActionExecutionRunning: { label: 'Action Running', icon: '⏳', color: 'blue' },
		ActionExecutionPending: { label: 'Action Pending', icon: '⏱️', color: 'orange' },
		ActionExecutionCancelled: { label: 'Action Cancelled', icon: '🚫', color: 'gray' },
		ActionExecution: { label: 'Action Execution', icon: '🎯', color: 'blue' },
	};

	return typeMap[operationType] || { label: operationType, icon: '📋', color: 'gray' };
}
