<template>
	<div class="customer-block">
		<div class="flex items-center justify-between">
			<h2 class="text-lg font-semibold">Change Log</h2>
			<el-button
				size="small"
				:icon="RefreshRight"
				:loading="loading"
				@click="loadChangeLogs"
				class="ml-2"
			>
				Refresh
			</el-button>
		</div>
		<el-divider />

		<div class="p-0" v-loading="loading">
			<el-table
				:data="processedChanges"
				max-height="384px"
				class="w-full"
				border
				stripe
				row-key="id"
			>
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
						<span
							class="text-gray-900 dark:text-white-100 truncate"
							:title="row.updatedBy"
						>
							{{ row.updatedBy || defaultStr }}
						</span>
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
			<div v-if="total > 0" class="border-t bg-white dark:bg-black-400 rounded-b-md">
				<CustomerPagination
					:total="total"
					:limit="pageSize"
					:page="currentPage"
					:background="true"
					@pagination="handlePaginationUpdate"
					@update:page="handleCurrentChange"
					@update:limit="handlePageUpdate"
				/>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, watch, onMounted } from 'vue'; // 移除 computed
import { Clock, Document, RefreshRight } from '@element-plus/icons-vue';
import { defaultStr, projectTenMinutesSsecondsDate } from '@/settings/projectSetting';
import { timeZoneConvert } from '@/hooks/time';
import {
	getChangeLogsByOnboarding,
	type ChangeLogItem,
	parseTaskStatusChanges,
	parseStaticFieldChanges,
	getOperationTypeInfo,
} from '@/apis/ow/change-log';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import CustomerPagination from '@/components/global/u-pagination/index.vue';
import { getStageQuestionnairesBatch } from '@/apis/ow/questionnaire'; // 使用批量获取API

// Props
interface Props {
	onboardingId?: string | number;
	stageId?: string | number;
}

const props = defineProps<Props>();
const { t } = useI18n();

// 响应式数据
const loading = ref(false);
const changes = ref<ChangeLogItem[]>([]);
const currentPage = ref(1);
const pageSize = ref(20);
const total = ref(0);
const questionnaireConfigCache = ref<Map<string, any>>(new Map()); // 问卷配置缓存

interface ProcessedChange extends ChangeLogItem {
	type: string;
	typeIcon: string;
	typeColor: string;
	answerChanges?: string[];
	fieldChanges?: Array<{
		fieldName: string;
		beforeValue: string;
		afterValue: string;
	}>;
	taskInfo?: any;
	fileInfo?: any;
}

const processedChanges = ref<ProcessedChange[]>([]); // 修正类型

// 防止重复请求

// 加载数据
const loadChangeLogs = async () => {
	if (!props.onboardingId) return;

	// 防止重复请求
	if (loading.value) return;

	loading.value = true;

	try {
		// 使用真实API调用
		const apiParams = {
			stageId: props.stageId ? String(props.stageId) : undefined,
			pageIndex: currentPage.value,
			pageSize: pageSize.value,
		};
		const response = await getChangeLogsByOnboarding(props.onboardingId, apiParams);

		if (response.code == '200') {
			// 映射API数据到组件期望的格式
			let rawItems = response.data.items || [];

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
				stageId: item.stageId, // 添加 stageId 映射
				onboardingId: item.onboardingId, // 也添加 onboardingId 以备后用
			}));

			// 处理变更数据
			await processChangesData();

			// 更新总数（如果进行了前端过滤）
			total.value = response?.data?.totalCount || 0;
		} else {
			ElMessage.error(response.msg || t('sys.api.operationFailed'));
			changes.value = [];
			processedChanges.value = [];
			total.value = 0;
		}
	} finally {
		loading.value = false;
	}
};

// 处理变更数据
const processChangesData = async () => {
	const processedData = [];
	
	for (const change of changes.value) {
		// 如果API直接返回了处理过的数据，则使用它们
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
				try {
					answerChanges = await parseQuestionnaireAnswerChangesWithConfig(
					change.beforeData,
						change.afterData,
						change // Pass the current change to identify the questionnaire
				);
				} catch (error) {
					console.warn('Enhanced parsing failed, using basic parsing:', error);
					// 回退到基本解析
					answerChanges = ['问卷答案已更新'];
				}
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

		processedData.push({
			...change,
			type: typeInfo.label,
			typeIcon: typeInfo.icon,
			typeColor: typeInfo.color,
			answerChanges,
			fieldChanges,
			taskInfo,
			fileInfo,
		});
	}
	
	processedChanges.value = processedData;
};

// 获取问卷配置（通过阶段ID）
const getQuestionnaireConfigByStageId = async (stageId: string | number): Promise<any> => {
	console.log('🔍 Getting questionnaire config by stageId:', stageId);
	const cacheKey = `stage_${String(stageId)}`;
	
	// 检查缓存
	if (questionnaireConfigCache.value.has(cacheKey)) {
		console.log('📋 Using cached config for stage:', stageId);
		return questionnaireConfigCache.value.get(cacheKey);
	}

	try {
		console.log('🌐 Calling getStageQuestionnairesBatch API with stageId:', stageId);
		// 使用批量API获取阶段对应的问卷
		const response = await getStageQuestionnairesBatch([String(stageId)]);
		console.log('📦 API Response:', response);
		
		if (response.success && response.data && response.data.stageQuestionnaires) {
			const stageData = response.data.stageQuestionnaires[String(stageId)];
			console.log('📄 Stage data:', stageData);
			
			if (stageData && Array.isArray(stageData) && stageData.length > 0) {
				// 获取第一个问卷的配置（一个阶段可能有多个问卷，这里取第一个）
				const questionnaire = stageData[0];
				console.log('📝 First questionnaire:', questionnaire);
				let questionnaireConfig = null;
				
				if (questionnaire.structureJson) {
					try {
						questionnaireConfig = JSON.parse(questionnaire.structureJson);
						console.log('✅ Parsed questionnaire config successfully');
					} catch (error) {
						console.warn('Failed to parse questionnaire structure:', error);
					}
				}
				
				// 缓存配置
				questionnaireConfigCache.value.set(cacheKey, questionnaireConfig);
				return questionnaireConfig;
			}
		}
	} catch (error) {
		console.warn('❌ Failed to fetch questionnaire config by stage ID:', error);
	}
	
	return null;
};

// 增强的答案格式化函数
const formatAnswerWithConfig = (response: any, questionnaireConfig: any): string => {
	if (!response.answer && !response.responseText) {
		return 'No answer';
	}

	const answer = response.answer || response.responseText;
	const type = response.type;
	const questionId = response.questionId;

	// 查找问题配置
	let questionConfig = null;
	if (questionnaireConfig && questionnaireConfig.sections && Array.isArray(questionnaireConfig.sections)) {
		for (const section of questionnaireConfig.sections) {
			if (section.questions && Array.isArray(section.questions)) {
				const question = section.questions.find((q: any) => 
					q.id === questionId || 
					`question-${q.id}` === questionId ||
					q.questionId === questionId
				);
				if (question) {
					questionConfig = question;
					break;
				}
			}
		}
	}

	switch (type) {
		case 'multiple_choice':
			// 处理单选题
			if (questionConfig && questionConfig.options && Array.isArray(questionConfig.options)) {
				const option = questionConfig.options.find((opt: any) => opt.value === answer);
				return option?.label || String(answer);
			}
			return String(answer);

		case 'dropdown':
			// 处理下拉选择
			if (questionConfig && questionConfig.options && Array.isArray(questionConfig.options)) {
				const option = questionConfig.options.find((opt: any) => opt.value === answer);
				return option?.label || String(answer);
			}
			return String(answer);

		case 'checkboxes':
			// 处理多选题
			let answerValues: string[] = [];
			
			if (Array.isArray(answer)) {
				answerValues = answer.map((item) => String(item)).filter(Boolean);
			} else {
				const answerStr = String(answer);
				try {
					const parsed = JSON.parse(answerStr);
					if (Array.isArray(parsed)) {
						answerValues = parsed.map((item) => String(item)).filter(Boolean);
					} else {
						answerValues = answerStr.split(',').map((item) => item.trim()).filter(Boolean);
					}
				} catch {
					answerValues = answerStr.split(',').map((item) => item.trim()).filter(Boolean);
				}
			}

			if (questionConfig && questionConfig.options && Array.isArray(questionConfig.options)) {
				const optionMap = new Map<string, string>();
				questionConfig.options.forEach((option: any) => {
					if (option && option.value !== undefined && option.label !== undefined) {
						optionMap.set(option.value, option.label);
					}
				});
				const labels = answerValues.map(value => optionMap.get(value) || value);
				return labels.join(', ');
			}
			return answerValues.join(', ');

		default:
			// 对于其他类型，使用原有逻辑
			if (type === 'file' || type === 'file_upload') {
				if (Array.isArray(answer)) {
					const fileNames = answer.map((file: any) => {
						if (typeof file === 'object' && file && file.name) {
							return file.name;
						}
						return 'Unknown file';
					});
					return `Files: ${fileNames.join(', ')}`;
				} else if (typeof answer === 'object' && answer && answer.name) {
					return `File: ${answer.name}`;
				} else if (typeof answer === 'string' && answer !== '[object Object]') {
					return `File: ${answer}`;
				}
				return 'File uploaded';
			}
			return String(answer);
	}
};

// 增强的问卷答案变更解析
const parseQuestionnaireAnswerChangesWithConfig = async (beforeData: any, afterData: any, currentChange?: any): Promise<string[]> => {
	if (!afterData) return [];

	try {
		const after = typeof afterData === 'string' ? JSON.parse(afterData) : afterData;
		const changesList: string[] = [];

		// 从当前变更记录中获取 stageId
		let stageId = null;
		
		// 首先尝试从 currentChange 获取
		if (currentChange?.stageId) {
			stageId = String(currentChange.stageId);
			console.log('🎯 Found stageId in currentChange:', stageId);
		} 
		// 然后尝试从 props 获取
		else if (props.stageId) {
			stageId = String(props.stageId);
			console.log('🎯 Using stageId from props:', stageId);
		} 
		else {
			console.log('❌ No stageId found anywhere');
		}

		// 获取问卷配置（通过阶段ID）
		let questionnaireConfig = null;
		if (stageId) {
			console.log('🚀 Calling getQuestionnaireConfigByStageId with:', stageId);
			questionnaireConfig = await getQuestionnaireConfigByStageId(stageId);
			console.log('📋 Got questionnaireConfig:', questionnaireConfig);
		} else {
			console.log('❌ No stageId available, skipping questionnaire config fetch');
		}

		// 处理问卷答案提交的情况（只有 afterData）
		if (!beforeData && after.responses) {
			after.responses.forEach((response: any) => {
				if (response.answer || response.responseText) {
					const formattedAnswer = formatAnswerWithConfig(response, questionnaireConfig);
					changesList.push(`${response.question || response.questionId}: ${formattedAnswer}`);
				}
			});
			return changesList;
		}

		// 处理问卷答案更新的情况（有 beforeData 和 afterData）
		if (beforeData && afterData) {
			const before = typeof beforeData === 'string' ? JSON.parse(beforeData) : beforeData;

			if (before.responses && after.responses) {
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
						const formattedAnswer = formatAnswerWithConfig(afterResp, questionnaireConfig);
						changesList.push(`${afterResp.question || questionId}: ${formattedAnswer}`);
					} else if (
						JSON.stringify(beforeResp.answer) !== JSON.stringify(afterResp.answer)
					) {
						// 修改的答案
						const beforeAnswer = formatAnswerWithConfig(beforeResp, questionnaireConfig);
						const afterAnswer = formatAnswerWithConfig(afterResp, questionnaireConfig);
						changesList.push(
							`${afterResp.question || questionId}: ${beforeAnswer} → ${afterAnswer}`
						);
					}
				});
			}
		}

		return changesList;
	} catch (error) {
		console.error('Error parsing questionnaire answer changes:', error);
		return [];
	}
};

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

const handleCurrentChange = (newPage: number) => {
	currentPage.value = newPage;
};

const handlePaginationUpdate = () => {
	loadChangeLogs();
};

const handlePageUpdate = (newSize: number) => {
	pageSize.value = newSize;
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
	(newValues) => {
		const [newOnboardingId, newStageId] = newValues || [];
		if (newOnboardingId || newStageId) {
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

defineExpose({
	loadChangeLogs,
});
</script>

<style scoped></style>
