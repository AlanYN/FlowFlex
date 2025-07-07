<template>
	<el-card class="shadow-sm rounded-md">
		<template #header>
			<div class="bg-gray-50 dark:bg-black-200 -mx-5 -mt-5 px-5 py-4 rounded-t-lg">
				<div class="flex justify-between items-center">
					<h2 class="text-lg font-medium text-gray-900 dark:text-white-100">
						Change Log
					</h2>
					<el-button size="small" :loading="loading" @click="loadChangeLogs" class="ml-2">
						<el-icon class="mr-1">
							<RefreshRight />
						</el-icon>
						Refresh
					</el-button>
				</div>
			</div>
		</template>

		<div class="p-0" v-loading="loading">
			<el-table :data="processedChanges" class="w-full" stripe row-key="id">
				<el-table-column label="Type" width="140">
					<template #default="{ row }">
						<el-tag
							:type="getTagType(row.type)"
							class="flex items-center w-fit"
							size="small"
						>
							<span class="mr-1 text-xs">{{ row.typeIcon }}</span>
							{{ row.type }}
						</el-tag>
					</template>
				</el-table-column>

				<el-table-column label="Changes" min-width="350">
					<template #default="{ row }">
						<div class="text-sm">
							<!-- 静态字段变更详情 -->
							<div v-if="row.type === 'Field Change' && row.fieldChanges?.length">
								<div class="space-y-2">
									<div
										v-for="(change, index) in row.fieldChanges"
										:key="index"
										class="bg-yellow-50 dark:bg-yellow-900/20 p-3 rounded-md border-l-4 border-yellow-400"
									>
										<div class="text-sm">
											<div
												class="font-semibold text-yellow-800 dark:text-yellow-200 mb-2"
											>
												{{ change.fieldName }}
											</div>
											<div class="space-y-1">
												<!-- 如果有原值，显示前后对比 -->
												<template v-if="change.beforeValue">
													<div class="flex items-center text-xs">
														<span
															class="text-red-600 dark:text-red-400 font-medium mr-2"
														>
															Before:
														</span>
														<span
															class="bg-red-100 dark:bg-red-900/30 px-2 py-1 rounded text-red-800 dark:text-red-200"
														>
															{{ change.beforeValue }}
														</span>
													</div>
													<div class="flex items-center text-xs">
														<span
															class="text-green-600 dark:text-green-400 font-medium mr-2"
														>
															After:
														</span>
														<span
															class="bg-green-100 dark:bg-green-900/30 px-2 py-1 rounded text-green-800 dark:text-green-200"
														>
															{{ change.afterValue || 'N/A' }}
														</span>
													</div>
												</template>
												<!-- 如果没有原值，只显示新设置的值 -->
												<template v-else>
													<div class="flex items-center text-xs">
														<span
															class="text-blue-600 dark:text-blue-400 font-medium mr-2"
														>
															Value Set:
														</span>
														<span
															class="bg-blue-100 dark:bg-blue-900/30 px-2 py-1 rounded text-blue-800 dark:text-blue-200"
														>
															{{ change.afterValue || 'N/A' }}
														</span>
													</div>
												</template>
											</div>
										</div>
									</div>
								</div>
							</div>

							<!-- 问卷答案变更详情 -->
							<div
								v-else-if="
									(row.type === 'Answer Update' ||
										row.type === 'Answer Submit') &&
									row.answerChanges?.length
								"
							>
								<div class="space-y-2">
									<div
										v-for="(change, index) in row.answerChanges"
										:key="index"
										class="bg-blue-50 dark:bg-blue-900/20 p-2 rounded text-xs border-l-4 border-blue-400"
									>
										{{ change }}
									</div>
								</div>
							</div>

							<!-- 文件上传详情 -->
							<div v-else-if="row.type === 'File Upload' && row.fileInfo">
								<div
									class="bg-cyan-50 dark:bg-cyan-900/20 p-2 rounded text-xs border-l-4 border-cyan-400"
								>
									<div class="flex items-center">
										<el-icon class="mr-2 text-cyan-600">
											<Document />
										</el-icon>
										<span class="font-medium">{{ row.fileInfo.fileName }}</span>
										<span
											v-if="row.fileInfo.fileSize"
											class="ml-2 text-gray-500"
										>
											({{ formatFileSize(row.fileInfo.fileSize) }})
										</span>
									</div>
								</div>
							</div>

							<!-- 任务状态变更详情 -->
							<div
								v-else-if="
									(row.type === 'Task Complete' ||
										row.type === 'Task Incomplete') &&
									row.taskInfo
								"
							>
								<div
									class="bg-green-50 dark:bg-green-900/20 p-2 rounded text-xs border-l-4 border-green-400"
								>
									<div class="font-medium">{{ row.taskInfo.taskName }}</div>
									<div class="text-gray-600 mt-1">
										{{ row.taskInfo.statusChange }}
									</div>
								</div>
							</div>

							<!-- 默认显示（简化的标题） -->
							<div v-else class="text-gray-700 dark:text-gray-300">
								{{ getSimplifiedTitle(row) }}
							</div>
						</div>
					</template>
				</el-table-column>

				<el-table-column label="Updated By" width="150">
					<template #default="{ row }">
						<div class="flex items-center space-x-2">
							<span class="text-gray-900 dark:text-white-100">
								{{ row.updatedBy || defaultStr }}
							</span>
						</div>
					</template>
				</el-table-column>

				<el-table-column label="Date & Time" width="200">
					<template #default="{ row }">
						<div class="flex items-center text-gray-600 dark:text-gray-400 text-sm">
							<el-icon class="mr-1 text-xs">
								<Clock />
							</el-icon>
							{{ formatDateTime(row.dateTime) }}
						</div>
					</template>
				</el-table-column>

				<template #empty>
					<div class="py-8 text-gray-500 dark:text-gray-400 text-center">
						<el-icon class="text-4xl mb-2">
							<Document />
						</el-icon>
						<div class="text-lg mb-2">No change records found</div>
						<div class="text-sm">
							{{
								props.stageId
									? 'No changes recorded for this stage yet.'
									: 'No changes recorded for this onboarding yet.'
							}}
						</div>
					</div>
				</template>
			</el-table>

			<!-- 分页 -->
			<div v-if="total > pageSize" class="flex justify-center mt-4 pb-4">
				<el-pagination
					v-model:current-page="currentPage"
					v-model:page-size="pageSize"
					:page-sizes="[10, 20, 50, 100]"
					:total="total"
					layout="total, sizes, prev, pager, next, jumper"
					@size-change="handleSizeChange"
					@current-change="handleCurrentChange"
				/>
			</div>
		</div>
	</el-card>
</template>

<script setup lang="ts">
import { computed, ref, watch, onMounted } from 'vue';
import { Clock, Document, RefreshRight } from '@element-plus/icons-vue';
import { defaultStr, projectTenMinutesSsecondsDate } from '@/settings/projectSetting';
import { timeZoneConvert } from '@/hooks/time';
import {
	getChangeLogsByOnboarding,
	type ChangeLogItem,
	parseQuestionnaireAnswerChanges,
	parseTaskStatusChanges,
	parseStaticFieldChanges,
	getOperationTypeInfo,
} from '@/apis/ow/change-log';

// Props
interface Props {
	onboardingId?: string | number;
	stageId?: string | number;
}

const props = defineProps<Props>();

// 响应式数据
const loading = ref(false);
const changes = ref<ChangeLogItem[]>([]);
const currentPage = ref(1);
const pageSize = ref(20);
const total = ref(0);

// 防止重复请求
const isLoadingRequest = ref(false);

// 临时Mock数据 - 用于开发测试
const getMockData = () => {
	const mockData: ChangeLogItem[] = [
		{
			id: 1,
			type: 'Update',
			typeIcon: '✏️',
			typeColor: 'gray',
			details: 'onboardStage: Application Sent → Application Filled',
			beforeData: { onboardStage: 'Application Sent' },
			afterData: { onboardStage: 'Application Filled' },
			changedFields: ['onboardStage'],
			updatedBy: 'Sarah Johnson',
			dateTime: '05/10/2023 09:45',
			extendedInfo: null,
		},
		{
			id: 2,
			type: 'Completion',
			typeIcon: '✅',
			typeColor: 'blue',
			details: 'Stage Completed: Application Sent',
			beforeData: null,
			afterData: null,
			changedFields: [],
			updatedBy: 'Robert Wilson',
			dateTime: '05/08/2023 14:20',
			extendedInfo: null,
		},
		{
			id: 3,
			type: 'Answer Update',
			typeIcon: '📝',
			typeColor: 'purple',
			details: 'Questionnaire answers updated',
			beforeData: {
				responses: [
					{ questionId: 'question-1750313341060', answer: '11' },
					{ questionId: 'question-1750318638850', answer: '22' },
				],
			},
			afterData: {
				responses: [
					{ questionId: 'question-1750313341060', answer: '111' },
					{ questionId: 'question-1750318638850', answer: '222' },
				],
			},
			changedFields: [],
			updatedBy: 'John Smith',
			dateTime: '05/05/2023 11:30',
			extendedInfo: null,
		},
		{
			id: 4,
			type: 'Task Complete',
			typeIcon: '✅',
			typeColor: 'green',
			details: 'Task Completed: Review credit application',
			beforeData: { TaskName: 'Review credit application', IsCompleted: false },
			afterData: { TaskName: 'Review credit application', IsCompleted: true },
			changedFields: ['IsCompleted'],
			updatedBy: 'Mary Johnson',
			dateTime: '05/03/2023 16:45',
			extendedInfo: null,
		},
		{
			id: 5,
			type: 'File Upload',
			typeIcon: '📎',
			typeColor: 'cyan',
			details: 'Document uploaded',
			beforeData: null,
			afterData: null,
			changedFields: [],
			updatedBy: 'David Chen',
			dateTime: '05/01/2023 10:15',
			extendedInfo: {
				fileName: 'business_license.pdf',
				fileSize: 2048576,
			},
		},
		{
			id: 6,
			type: 'Field Change',
			typeIcon: '🔧',
			typeColor: 'yellow',
			details: 'Priority changed',
			beforeData: { priority: 'Medium' },
			afterData: { priority: 'High' },
			changedFields: ['priority'],
			updatedBy: 'Alice Brown',
			dateTime: '04/28/2023 13:20',
			extendedInfo: null,
		},
	];

	return mockData;
};

// 加载数据
const loadChangeLogs = async () => {
	if (!props.onboardingId) return;

	// 防止重复请求
	if (isLoadingRequest.value) {
		console.log('[ChangeLog] Request already in progress, skipping...');
		return;
	}

	isLoadingRequest.value = true;
	loading.value = true;

	try {
		console.log(
			'[ChangeLog] Loading change logs for onboarding:',
			props.onboardingId,
			'stage:',
			props.stageId,
			'pageIndex:',
			currentPage.value,
			'pageSize:',
			pageSize.value
		);

		// 使用真实API调用
		const apiParams = {
			stageId: props.stageId ? String(props.stageId) : undefined,
			pageIndex: currentPage.value,
			pageSize: pageSize.value,
		};

		console.log('[ChangeLog] API call parameters:', apiParams);

		const response = await getChangeLogsByOnboarding(props.onboardingId, apiParams);

		console.log('[ChangeLog] API response:', {
			code: response.code,
			itemCount: response.data?.items?.length || 0,
			totalCount: response.data?.totalCount || 0,
		});

		// 检查响应是否成功（支持多种响应格式）
		const isSuccess = (response.code === 200 || response.code === '200') && response.data;

		if (isSuccess) {
			// 映射API数据到组件期望的格式
			let rawItems = response.data.items || [];

			// 如果指定了stageId，在前端进行过滤（作为后端过滤的备选方案）
			if (props.stageId) {
				const targetStageId = props.stageId.toString();
				rawItems = rawItems.filter((item: any) => {
					const itemStageId = item.stageId?.toString();
					return itemStageId === targetStageId;
				});
				console.log(
					`[ChangeLog] Filtered ${rawItems.length} items for stageId: ${targetStageId}`
				);
			}

			changes.value = rawItems.map((item: any) => ({
				id: item.id,
				type: item.operationType, // 使用operationType作为type
				details: item.operationDescription || item.operationTitle || '', // 详细描述
				operationTitle: item.operationTitle, // 保留原始标题
				beforeData: item.beforeData,
				afterData: item.afterData,
				changedFields: item.changedFields || [],
				updatedBy: item.operatorName || 'Unknown', // 操作人
				dateTime: item.operationTime || item.createDate || '', // 操作时间
				extendedInfo: item.extendedData, // 扩展信息
			}));

			// 更新总数（如果进行了前端过滤）
			total.value = props.stageId ? changes.value.length : response.data.totalCount || 0;
			console.log('Loaded change logs:', changes.value.length, 'items, total:', total.value);
		} else {
			const errorMsg = response.message || response.msg || 'Unknown error';
			console.warn('Change logs API returned non-200 code:', response.code, errorMsg);
			changes.value = [];
			total.value = 0;
		}
	} catch (error) {
		console.error('Failed to load change logs:', error);

		// 如果API调用失败，可以选择性地回退到Mock数据（仅用于开发调试）
		if (process.env.NODE_ENV === 'development') {
			console.warn('Falling back to mock data for development');
			const mockData = getMockData();
			const startIndex = (currentPage.value - 1) * pageSize.value;
			const endIndex = startIndex + pageSize.value;
			changes.value = mockData.slice(startIndex, endIndex);
			total.value = mockData.length;
		} else {
			changes.value = [];
			total.value = 0;
		}
	} finally {
		loading.value = false;
		isLoadingRequest.value = false;
	}
};

// 处理后的变更数据
const processedChanges = computed(() => {
	return changes.value.map((change) => {
		// 如果API直接返回了处理过的数据，则使用它们
		// 否则使用本地处理逻辑
		const typeInfo =
			change.typeIcon && change.typeColor
				? { label: change.type, icon: change.typeIcon, color: change.typeColor }
				: getOperationTypeInfo(change.type);

		// 解析具体的变更详情
		let answerChanges: string[] = [];
		let fieldChanges: Array<{
			fieldName: string;
			beforeValue: string;
			afterValue: string;
		}> = [];
		let taskInfo: any = null;
		let fileInfo: any = null;

		// 根据类型解析不同的变更信息
		const operationType = change.type;
		switch (operationType) {
			case 'Answer Update':
			case 'QuestionnaireAnswerUpdate':
			case 'QuestionnaireAnswerSubmit':
				answerChanges = parseQuestionnaireAnswerChanges(
					change.beforeData,
					change.afterData
				);
				break;

			case 'Field Change':
			case 'StaticFieldValueChange':
				fieldChanges = parseStaticFieldChanges(
					change.beforeData,
					change.afterData,
					change.changedFields
				);
				break;

			case 'Task Complete':
			case 'Task Incomplete':
			case 'ChecklistTaskComplete':
			case 'ChecklistTaskUncomplete':
				const taskStatusChange = parseTaskStatusChanges(
					change.beforeData,
					change.afterData
				);
				taskInfo = {
					taskName: extractTaskName(change.details),
					statusChange: taskStatusChange,
				};
				break;

			case 'File Upload':
			case 'FileUpload':
				fileInfo = extractFileInfo(change.extendedInfo);
				break;

			case 'Completion':
			case 'Update':
			case 'StageTransition':
			case 'PriorityChange':
				// 这些类型通常只需要显示基本详情
				break;

			default:
				console.log('Unknown change type:', operationType);
				break;
		}

		return {
			...change,
			type: typeInfo.label,
			typeIcon: typeInfo.icon,
			typeColor: typeInfo.color,
			answerChanges,
			fieldChanges,
			taskInfo,
			fileInfo,
		};
	});
});

// 辅助函数
const extractTaskName = (details: string): string => {
	// 从详情字符串中提取任务名称
	const patterns = [
		/Task (?:Complete|Incomplete): (.+)/,
		/Task (?:Completed|Uncompleted): (.+)/,
		/Checklist task '(.+)' has been/,
		/'(.+)' has been (?:completed|marked as incomplete)/,
	];

	for (const pattern of patterns) {
		const match = details.match(pattern);
		if (match) return match[1];
	}

	return details;
};

const extractFileInfo = (extendedInfo: any): { fileName: string; fileSize?: number } | null => {
	if (!extendedInfo) return null;

	try {
		const info = typeof extendedInfo === 'string' ? JSON.parse(extendedInfo) : extendedInfo;
		return {
			fileName: info.FileName || info.fileName || info.filename || 'Unknown file',
			fileSize: info.FileSize || info.fileSize || info.size,
		};
	} catch {
		return null;
	}
};

const formatFileSize = (bytes: number): string => {
	if (bytes === 0) return '0 Bytes';

	const k = 1024;
	const sizes = ['Bytes', 'KB', 'MB', 'GB'];
	const i = Math.floor(Math.log(bytes) / Math.log(k));

	return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
};

// 分页处理
const handleSizeChange = (newSize: number) => {
	pageSize.value = newSize;
	currentPage.value = 1;
	loadChangeLogs();
};

const handleCurrentChange = (newPage: number) => {
	currentPage.value = newPage;
	loadChangeLogs();
};

// 工具函数
const formatDateTime = (dateString: string): string => {
	try {
		if (!dateString) return defaultStr;

		return timeZoneConvert(dateString, false, projectTenMinutesSsecondsDate);
	} catch {
		return dateString || defaultStr;
	}
};

const getTagType = (type: string): string => {
	switch (type) {
		case 'Completion':
		case 'ChecklistTaskComplete':
			return 'success';
		case 'Answer Update':
		case 'QuestionnaireAnswerUpdate':
		case 'Answer Submit':
		case 'QuestionnaireAnswerSubmit':
		case 'FileUpload':
		case 'File Upload':
			return 'info';
		case 'Task Complete':
			return 'success';
		case 'Task Incomplete':
		case 'ChecklistTaskUncomplete':
			return 'warning';
		case 'Field Change':
		case 'StaticFieldValueChange':
			return 'warning';
		default:
			return 'info';
	}
};

const getSimplifiedTitle = (row: any): string => {
	// 从operationTitle中提取简化的标题
	if (row.operationTitle) {
		// 移除冗长的描述，只保留关键信息
		return row.operationTitle
			.replace(/^(Static Field Value Changed: |Questionnaire Answer |)/, '')
			.replace(/ has been (changed|updated|submitted).*$/, '')
			.trim();
	}

	// 如果没有operationTitle，使用details的简化版本
	if (row.details) {
		return row.details
			.replace(/^(Static field |Questionnaire answer )/, '')
			.replace(/ (value )?has been.*$/, '')
			.replace(/ by \w+\..*$/, '')
			.trim();
	}

	return row.type || 'Change';
};

// 监听属性变化并加载数据
watch(
	() => [props.onboardingId, props.stageId],
	(newValues, oldValues) => {
		const [newOnboardingId, newStageId] = newValues || [];
		const [oldOnboardingId, oldStageId] = oldValues || [];

		console.log('[ChangeLog] Props changed:', {
			onboardingId: { old: oldOnboardingId, new: newOnboardingId },
			stageId: { old: oldStageId, new: newStageId },
		});

		if (newOnboardingId) {
			// 重置分页到第一页
			currentPage.value = 1;
			loadChangeLogs();
		}
	},
	{ immediate: true }
);

// 组件挂载时加载数据
onMounted(() => {
	if (props.onboardingId) {
		loadChangeLogs();
	}
});
</script>

<style scoped>
/* 表格样式调整 */
:deep(.el-table) {
	border: none;
}

:deep(.el-table th) {
	background-color: #f8fafc;
	border-bottom: 1px solid #e5e7eb;
	color: #374151;
	font-weight: 500;
}

:deep(.el-table td) {
	border-bottom: 1px solid #f3f4f6;
	padding: 12px 8px;
}

:deep(.el-table tbody tr:hover > td) {
	background-color: #f9fafb;
}

/* 变更详情样式 */
.answer-changes-collapse :deep(.el-collapse-item__header) {
	font-size: 12px;
	padding: 8px 0;
	background-color: transparent;
}

.answer-changes-collapse :deep(.el-collapse-item__content) {
	padding: 8px 0;
}

/* 变更类型颜色 */
.bg-blue-50 {
	background-color: #eff6ff;
}

.bg-yellow-50 {
	background-color: #fefce8;
}

.bg-cyan-50 {
	background-color: #ecfeff;
}

.bg-green-50 {
	background-color: #f0fdf4;
}

/* 暗色主题 */
html.dark :deep(.el-table th) {
	background-color: var(--black-200);
	border-bottom: 1px solid var(--black-100);
	color: var(--white-100);
}

html.dark :deep(.el-table td) {
	border-bottom: 1px solid var(--black-200);
	background-color: var(--black-400);
	color: var(--white-100);
}

html.dark :deep(.el-table tbody tr:hover > td) {
	background-color: var(--black-300);
}

html.dark :deep(.el-table) {
	background-color: var(--black-400);
}

html.dark .bg-blue-50 {
	background-color: rgba(59, 130, 246, 0.1);
}

html.dark .bg-yellow-50 {
	background-color: rgba(251, 191, 36, 0.1);
}

html.dark .bg-cyan-50 {
	background-color: rgba(6, 182, 212, 0.1);
}

html.dark .bg-green-50 {
	background-color: rgba(34, 197, 94, 0.1);
}
</style>
