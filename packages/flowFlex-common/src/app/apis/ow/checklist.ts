import { defHttp } from '@/apis/axios';
import { useGlobSetting } from '@/settings';

const globSetting = useGlobSetting();

// API路径配置
const Api = (id?: string | number) => {
	return {
		// 清单相关API
		checklists: `${globSetting.apiProName}/ow/checklists/v1`,
		checklist: `${globSetting.apiProName}/ow/checklists/v1/${id}`,
		checklistQuery: `${globSetting.apiProName}/ow/checklists/v1/query`,
		checklistTemplates: `${globSetting.apiProName}/ow/checklists/v1/templates`,
		checklistStatistics: `${globSetting.apiProName}/ow/checklists/v1/statistics`,
		checklistCompletion: `${globSetting.apiProName}/ow/checklists/v1/${id}/completion`,
		checklistExportPdf: `${globSetting.apiProName}/ow/checklists/v1/${id}/export-pdf`,

		// 清单任务相关API
		checklistTasks: `${globSetting.apiProName}/ow/checklist-task/v1`,
		checklistTask: `${globSetting.apiProName}/ow/checklist-task/v1/${id}`,
		checklistTaskList: `${globSetting.apiProName}/ow/checklist-task/v1/list/${id}`,
		checklistTaskComplete: `${globSetting.apiProName}/ow/checklist-task/v1/${id}/complete`,
		checklistTaskOverdue: `${globSetting.apiProName}/ow/checklist-task/v1/overdue`,
		checklistTaskDependencies: `${globSetting.apiProName}/ow/checklist-task/v1/${id}/dependencies`,

		// 工作流和阶段选项API
		workflowOptions: `${globSetting.apiProName}/ow/workflows/v1/all`,
		stageOptions: `${globSetting.apiProName}/ow/stages/v1/all`,
	};
};

// ========================= 清单相关接口 =========================

/**
 * 获取清单列表 [CL01]
 * @returns List<ChecklistOutputDto>
 */
export function getChecklists() {
	return defHttp.get({ url: `${Api().checklists}` });
}

/**
 * 获取模板列表 [CL02]
 * @returns List<ChecklistOutputDto>
 */
export function getChecklistTemplates() {
	return defHttp.get({ url: `${Api().checklistTemplates}` });
}

/**
 * 查询清单(分页) [CL03]
 * @param params ChecklistQueryRequest
 * @returns PagedResult<ChecklistOutputDto>
 */
export function queryChecklists(params: any) {
	return defHttp.post({ url: `${Api().checklistQuery}`, params });
}

/**
 * 获取统计信息 [CL04]
 * @param team 团队名称 (可选)
 * @returns ChecklistStatisticsDto
 */
export function getChecklistStatistics(team?: string) {
	const url = team
		? `${Api().checklistStatistics}?team=${encodeURIComponent(team)}`
		: Api().checklistStatistics;
	return defHttp.get({ url });
}

/**
 * 创建清单 [CL05]
 * @param params ChecklistInputDto
 * @returns long (清单ID)
 */
export function createChecklist(params: any) {
	return defHttp.post({ url: `${Api().checklists}`, params });
}

/**
 * 获取清单详情 [CL06]
 * @param id 清单ID
 * @returns ChecklistOutputDto
 */
export function getChecklistDetail(id: string | number) {
	return defHttp.get({ url: `${Api(id).checklist}` });
}

/**
 * 更新清单 [CL07]
 * @param id 清单ID
 * @param params ChecklistInputDto
 * @returns bool
 */
export function updateChecklist(id: string | number, params: any) {
	return defHttp.put({ url: `${Api(id).checklist}`, params });
}

/**
 * 删除清单
 * @param id 清单ID
 * @param confirm 是否确认删除
 * @returns bool
 */
export function deleteChecklist(id: string | number, confirm: boolean = false) {
	const url = confirm ? `${Api(id).checklist}?confirm=true` : Api(id).checklist;
	return defHttp.delete({ url });
}

/**
 * 获取完成率 [CL08]
 * @param id 清单ID
 * @returns decimal
 */
export function getChecklistCompletion(id: string | number) {
	return defHttp.get({ url: `${Api(id).checklistCompletion}` });
}

/**
 * 导出清单到PDF
 * @param id 清单ID
 * @returns Blob
 */
export function exportChecklistToPdf(id: string | number) {
	return defHttp
		.get({
			url: `${Api(id).checklistExportPdf}`,
			responseType: 'blob',
			headers: {
				Accept: 'application/pdf',
				'Content-Type': 'application/json',
			},
		})
		.then(async (response) => {
			console.log('原始API响应:', response);

			// 检查响应是否存在
			if (!response) {
				throw new Error('API返回空响应');
			}

			// 如果响应已经是Blob
			if (response instanceof Blob) {
				console.log('响应已经是Blob，大小:', response.size);

				// 检查Blob大小是否合理
				if (response.size === 0) {
					throw new Error('返回的PDF文件为空');
				}

				// 如果文件很小，可能是错误信息
				if (response.size < 1024) {
					try {
						const text = await response.text();
						console.log('小文件内容:', text);

						// 检查是否包含错误信息
						if (
							text.includes('error') ||
							text.includes('Error') ||
							text.includes('exception')
						) {
							throw new Error(`服务器返回错误: ${text}`);
						}

						// 检查是否是有效的PDF
						if (!text.startsWith('%PDF')) {
							throw new Error('返回的文件不是有效的PDF格式');
						}

						// 重新创建Blob
						return new Blob([text], { type: 'application/pdf' });
					} catch (textError) {
						console.warn('无法读取响应文本:', textError);
						// 如果无法读取文本，仍然返回原始Blob
						return response;
					}
				}

				return response;
			}

			// 如果响应是ArrayBuffer，转换为Blob
			if (response instanceof ArrayBuffer) {
				console.log('将ArrayBuffer转换为Blob，大小:', response.byteLength);

				if (response.byteLength === 0) {
					throw new Error('返回的PDF文件为空');
				}

				return new Blob([response], { type: 'application/pdf' });
			}

			// 如果响应是其他格式，尝试转换
			console.log('尝试将响应转换为Blob');
			return new Blob([response], { type: 'application/pdf' });
		})
		.catch((error) => {
			console.error('PDF导出API调用失败:', error);

			// 如果是网络错误或API不存在，提供更具体的错误信息
			if (error.response?.status === 404) {
				throw new Error('PDF导出功能暂不可用（API未找到）');
			} else if (error.response?.status === 500) {
				throw new Error('服务器内部错误，请稍后重试');
			} else if (error.response?.status === 400) {
				throw new Error('请求参数错误，请检查清单ID');
			} else if (!navigator.onLine) {
				throw new Error('网络连接失败，请检查网络连接');
			}

			// 如果错误信息已经是我们抛出的，直接传递
			if (error.message && error.message.includes('服务器返回错误')) {
				throw error;
			}

			throw new Error(`PDF导出失败: ${error.message || '未知错误'}`);
		});
}

/**
 * 复制清单
 * @param id 清单ID
 * @param params DuplicateChecklistInputDto
 * @returns long (新清单ID)
 */
export function duplicateChecklist(id: string | number, params: any) {
	return defHttp.post({ url: `${Api(id).checklist}/duplicate`, params });
}

// ========================= 清单任务相关接口 =========================

/**
 * 创建任务 [CT01]
 * @param params ChecklistTaskInputDto
 * @returns long (任务ID)
 */
export function createChecklistTask(params: any) {
	return defHttp.post({ url: `${Api().checklistTasks}`, params });
}

/**
 * 获取任务列表 [CT02]
 * @param checklistId 清单ID
 * @returns List<ChecklistTaskOutputDto>
 */
export function getChecklistTasks(checklistId: string | number) {
	return defHttp.get({ url: `${Api(checklistId).checklistTaskList}` });
}

/**
 * 获取逾期任务 [CT03]
 * @param team 团队名称 (可选)
 * @returns List<OverdueTaskDto>
 */
export function getOverdueTasks(team?: string) {
	const url = team
		? `${Api().checklistTaskOverdue}?team=${encodeURIComponent(team)}`
		: Api().checklistTaskOverdue;
	return defHttp.get({ url });
}

/**
 * 获取依赖关系 [CT04]
 * @param checklistId 清单ID
 * @returns Dictionary<long, List<long>>
 */
export function getTaskDependencies(checklistId: string | number) {
	return defHttp.get({ url: `${Api(checklistId).checklistTaskDependencies}` });
}

/**
 * 获取任务详情 [CT05]
 * @param id 任务ID
 * @returns ChecklistTaskOutputDto
 */
export function getChecklistTaskDetail(id: string | number) {
	return defHttp.get({ url: `${Api(id).checklistTask}` });
}

/**
 * 更新任务
 * @param id 任务ID
 * @param params ChecklistTaskInputDto
 * @returns bool
 */
export function updateChecklistTask(id: string | number, params: any) {
	return defHttp.put({ url: `${Api(id).checklistTask}`, params });
}

/**
 * 删除任务
 * @param id 任务ID
 * @param confirm 是否确认删除
 * @returns bool
 */
export function deleteChecklistTask(id: string | number, confirm: boolean = false) {
	const url = confirm ? `${Api(id).checklistTask}?confirm=true` : Api(id).checklistTask;
	return defHttp.delete({ url });
}

/**
 * 完成任务 [CT06]
 * @param id 任务ID
 * @param params CompleteTaskInputDto
 * @returns bool
 */
export function completeChecklistTask(id: string | number, params: any) {
	return defHttp.post({ url: `${Api(id).checklistTaskComplete}`, params });
}

/**
 * 切换任务完成状态
 * @param id 任务ID
 * @param completed 是否完成
 * @param params 完成参数
 * @returns bool
 */
export function toggleTaskCompletion(id: string | number, completed: boolean, params?: any) {
	if (completed) {
		const completeParams = {
			actualHours: params?.actualHours || 0,
			completionNotes: params?.completionNotes || 'Task completed',
			...params,
		};
		return completeChecklistTask(id, completeParams);
	} else {
		// 如果是取消完成，可能需要调用其他接口或更新状态
		const updateParams = {
			status: 'Pending',
			isCompleted: false,
			completedDate: null,
			...params,
		};
		return updateChecklistTask(id, updateParams);
	}
}

// ========================= 工作流和阶段选项接口 =========================

/**
 * 获取工作流选项
 * @returns List<WorkflowOutputDto>
 */
export function getWorkflowOptions() {
	return defHttp.get({ url: `${Api().workflowOptions}` });
}

/**
 * 获取阶段选项
 * @returns List<StageOutputDto>
 */
export function getStageOptions() {
	return defHttp.get({ url: `${Api().stageOptions}` });
}

// ========================= 类型定义 =========================

export interface ChecklistInputDto {
	name: string;
	description?: string;
	team: string;
	type?: string;
	status?: string;
	isTemplate?: boolean;
	templateId?: number;
	workflowId?: number;
	stageId?: number;
	estimatedHours?: number;
	priority?: string;
	assigneeId?: number;
	assigneeName?: string;
	dueDate?: string;
	isActive?: boolean;
	customFieldsJson?: string;
}

export interface ChecklistOutputDto {
	id: number;
	name: string;
	description?: string;
	team: string;
	type: string;
	status: string;
	isTemplate: boolean;
	templateId?: number;
	workflowId?: number;
	workflowName?: string;
	stageId?: number;
	stageName?: string;
	estimatedHours?: number;
	actualHours?: number;
	completionRate: number;
	priority?: string;
	assigneeId?: number;
	assigneeName?: string;
	dueDate?: string;
	isActive: boolean;
	customFieldsJson?: string;
	tasks?: ChecklistTaskOutputDto[];
	createDate: string;
	createBy: string;
	modifyDate?: string;
	modifyBy?: string;
}

export interface ChecklistTaskInputDto {
	checklistId: number;
	name: string;
	description?: string;
	taskType?: string;
	isRequired?: boolean;
	assigneeId?: number;
	assigneeName?: string;
	assignedTeam?: string;
	priority?: string;
	order?: number;
	estimatedHours?: number;
	dueDate?: string;
	dependsOnTaskIds?: number[];
	customFieldsJson?: string;
}

export interface ChecklistTaskOutputDto {
	id: number;
	checklistId: number;
	name: string;
	description?: string;
	taskType: string;
	status: string;
	isRequired: boolean;
	isCompleted: boolean;
	assigneeId?: number;
	assigneeName?: string;
	assignedTeam?: string;
	priority?: string;
	order: number;
	estimatedHours?: number;
	actualHours?: number;
	estimatedMinutes?: number; // 兼容前端显示
	completed?: boolean; // 兼容前端显示
	dueDate?: string;
	completedDate?: string;
	dependsOnTaskIds?: number[];
	customFieldsJson?: string;
	createDate: string;
	createBy: string;
	modifyDate?: string;
	modifyBy?: string;
}

export interface ChecklistQueryRequest {
	pageIndex?: number;
	pageSize?: number;
	sortField?: string;
	sortDirection?: string;
	name?: string;
	team?: string;
	type?: string;
	status?: string;
	isTemplate?: boolean;
	workflowId?: number;
	stageId?: number;
	assigneeId?: number;
	isActive?: boolean;
}

export interface ChecklistStatisticsDto {
	totalCount: number;
	activeCount: number;
	templateCount: number;
	completedCount: number;
	averageCompletionRate: number;
	teamDistribution: Record<string, number>;
	statusDistribution: Record<string, number>;
	priorityDistribution: Record<string, number>;
}

export interface CompleteTaskInputDto {
	actualHours?: number;
	completionNotes?: string;
	completedDate?: string;
}

export interface DuplicateChecklistInputDto {
	name: string;
	description?: string;
	team?: string;
	copyTasks?: boolean;
	setAsTemplate?: boolean;
}

export interface WorkflowOutputDto {
	id: number;
	name: string;
	description?: string;
	isDefault: boolean;
	status: string;
	isActive: boolean;
	expiryDate?: string;
}

export interface StageOutputDto {
	id: number;
	workflowId: number;
	name: string;
	description?: string;
	sortOrder: number;
	color?: string;
	estimatedDays?: number;
	isRequired: boolean;
}

// ========================= 辅助函数 =========================

/**
 * 格式化清单数据以兼容前端显示
 * @param checklist 清单数据
 * @returns 格式化后的清单数据
 */
export function formatChecklistForDisplay(checklist: ChecklistOutputDto) {
	return {
		...checklist,
		tasks:
			checklist.tasks?.map((task) => ({
				...task,
				completed: task.isCompleted,
				estimatedMinutes: task.estimatedHours ? task.estimatedHours * 60 : 0,
			})) || [],
	};
}

/**
 * 格式化任务数据以兼容API提交
 * @param task 任务数据
 * @returns 格式化后的任务数据
 */
export function formatTaskForApi(task: any): ChecklistTaskInputDto {
	return {
		checklistId: task.checklistId,
		name: task.name,
		description: task.description || '',
		taskType: task.taskType || 'Manual',
		isRequired: task.isRequired || false,
		assigneeId: task.assigneeId,
		assigneeName: task.assigneeName,
		assignedTeam: task.assignedTeam,
		priority: task.priority || 'Medium',
		order: task.order || 0,
		estimatedHours: task.estimatedMinutes
			? Math.round((task.estimatedMinutes / 60) * 100) / 100
			: 0,
		dueDate: task.dueDate,
		dependsOnTaskIds: task.dependsOnTaskIds || [],
		customFieldsJson: task.customFieldsJson,
	};
}

/**
 * 处理API错误响应
 * @param error 错误对象
 * @returns 格式化的错误信息
 */
export function handleApiError(error: any): string {
	if (error?.response?.data?.message) {
		return error.response.data.message;
	}
	if (error?.message) {
		return error.message;
	}
	return 'An unknown error occurred';
}

/**
 * 过滤活跃的workflow（排除Inactive状态且过期的）
 * @param workflows 原始workflow列表
 * @returns 过滤后的活跃workflow列表
 */
export function filterActiveWorkflows(workflows: WorkflowOutputDto[]): WorkflowOutputDto[] {
	return workflows.filter((workflow) => {
		// 只显示Active状态的workflow
		if (workflow.status !== 'Active' || !workflow.isActive) {
			return false;
		}

		// 如果有过期时间，检查是否过期
		if (workflow.expiryDate) {
			const expiryDate = new Date(workflow.expiryDate);
			const now = new Date();
			if (expiryDate < now) {
				return false;
			}
		}

		return true;
	});
}
