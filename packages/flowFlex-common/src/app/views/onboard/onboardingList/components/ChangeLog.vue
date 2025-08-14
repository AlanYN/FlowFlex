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
	const processedData: ProcessedChange[] = [];

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
	const cacheKey = `stage_${String(stageId)}`;

	// 检查缓存
	if (questionnaireConfigCache.value.has(cacheKey)) {
		return questionnaireConfigCache.value.get(cacheKey);
	}

	try {
		// 使用批量API获取阶段对应的问卷
		const response = await getStageQuestionnairesBatch([String(stageId)]);

		if (response.success && response.data && response.data.stageQuestionnaires) {
			const stageData = response.data.stageQuestionnaires[String(stageId)];

			if (stageData && Array.isArray(stageData) && stageData.length > 0) {
				// 获取第一个问卷的配置（一个阶段可能有多个问卷，这里取第一个）
				const questionnaire = stageData[0];
				let questionnaireConfig = null;

				if (questionnaire.structureJson) {
					try {
						questionnaireConfig = JSON.parse(questionnaire.structureJson);
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

// 解析responseText中的Unicode编码
const parseResponseText = (responseText: string): { [key: string]: string } => {
	if (!responseText || responseText.trim() === '{}') {
		return {};
	}

	try {
		// 处理Unicode转义序列
		let decodedText = responseText;
		decodedText = decodedText.replace(/u0022/g, '"');
		decodedText = decodedText.replace(/u0020/g, ' ');
		decodedText = decodedText.replace(/u003A/g, ':');
		decodedText = decodedText.replace(/u002C/g, ',');
		decodedText = decodedText.replace(/u007B/g, '{');
		decodedText = decodedText.replace(/u007D/g, '}');

		const parsed = JSON.parse(decodedText);
		return parsed || {};
	} catch (error) {
		console.warn('Failed to parse responseText:', responseText, error);
		return {};
	}
};

// 从responseText中提取Other选项的自定义值
const extractOtherValues = (
	responseText: string,
	questionId: string
): { [key: string]: string } => {
	const parsed = parseResponseText(responseText);
	const otherValues: { [key: string]: string } = {};

	// 查找包含questionId的键
	Object.keys(parsed).forEach((key) => {
		if (key.includes(questionId)) {
			// 对于网格类型：查找包含"other"的键
			if (key.includes('other')) {
				// 提取column ID，格式如：question-xxx_row-xxx_column-other-xxx
				const parts = key.split('_');
				const columnPart = parts.find((part) => part.startsWith('column-other-'));
				if (columnPart) {
					otherValues[columnPart] = parsed[key];
				}
			}
			// 对于多选题：查找option类型的键
			else if (key.includes('option-') || key.includes('option_')) {
				// 提取option ID，格式如：question-xxx_option-xxx
				const parts = key.split('_');
				let optionPart = parts.find((part) => part.startsWith('option-'));
				if (optionPart) {
					// 同时支持 option- 和 option_ 格式
					const alternativeKey = optionPart.replace('option-', 'option_');
					otherValues[optionPart] = parsed[key];
					otherValues[alternativeKey] = parsed[key];
				}
			}
		}
	});

	return otherValues;
};

// Column ID 到字母标签的映射缓存 (按问题分组)
const questionColumnMaps = new Map<string, Map<string, string>>();

// 为特定问题的 column ID 生成字母标签
const getColumnLabel = (columnId: string, questionId?: string): string => {
	const mapKey = questionId || 'global';

	if (!questionColumnMaps.has(mapKey)) {
		questionColumnMaps.set(mapKey, new Map<string, string>());
	}

	const columnMap = questionColumnMaps.get(mapKey)!;

	if (columnMap.has(columnId)) {
		return columnMap.get(columnId)!;
	}

	// 生成字母标签 (a, b, c, d, ...)
	const label = String.fromCharCode(97 + columnMap.size); // 97 = 'a'
	columnMap.set(columnId, label);

	return label;
};

// 获取多选答案数组
const getCheckboxAnswers = (answer: any): string[] => {
	if (!answer) return [];

	if (Array.isArray(answer)) {
		return answer.filter(Boolean);
	}

	if (typeof answer === 'string') {
		// 检查是否是JSON数组字符串
		if (answer.startsWith('[') && answer.endsWith(']')) {
			try {
				const parsed = JSON.parse(answer);
				if (Array.isArray(parsed)) {
					return parsed.filter(Boolean);
				}
			} catch {
				// 如果解析失败，按逗号分割
			}
		}

		// 按逗号分割字符串
		return answer
			.split(',')
			.map((item) => item.trim())
			.filter(Boolean);
	}

	return [String(answer)];
};

// 获取多选题标签
const getCheckboxLabels = (
	answer: any,
	questionConfig: any,
	responseText?: string,
	questionId?: string
): string[] => {
	if (!answer) {
		return [];
	}

	// 如果没有questionConfig，仍然尝试解析Other选项
	if (!questionConfig?.options) {
		const answerValues = getCheckboxAnswers(answer);

		// 尝试提取Other选项的自定义值
		let otherValues: { [key: string]: string } = {};
		if (responseText && questionId) {
			otherValues = extractOtherValues(responseText, questionId);
		}

		// 处理答案值，替换Other选项
		const processedAnswers: string[] = [];
		let hasOtherWithCustomValue = false;

		// 首先检查是否有Other选项带自定义值
		answerValues.forEach((value) => {
			if (value.startsWith('option_')) {
				const customValue =
					otherValues[value] || otherValues[value.replace('option_', 'option-')];
				if (customValue) {
					hasOtherWithCustomValue = true;
				}
			}
		});

		answerValues.forEach((value) => {
			// 检查是否是option_开头的Other选项
			if (value.startsWith('option_')) {
				const customValue =
					otherValues[value] || otherValues[value.replace('option_', 'option-')];
				if (customValue) {
					processedAnswers.push(`Other: ${customValue}`);
				} else {
					processedAnswers.push(value);
				}
			} else {
				// 检查是否是Other选项的自定义输入值，如果是则跳过
				const isCustomInput = Object.values(otherValues).some(
					(customVal) => customVal.toLowerCase() === value.toLowerCase()
				);

				// 如果有Other自定义值且当前值等于自定义值，则跳过
				if (isCustomInput && hasOtherWithCustomValue) {
					return;
				}

				processedAnswers.push(value);
			}
		});

		return processedAnswers;
	}

	const answerValues = getCheckboxAnswers(answer);

	const optionMap = new Map<string, string>();
	const otherOptionIds = new Set<string>();

	// 建立选项映射并识别Other选项
	questionConfig.options.forEach((option: any) => {
		optionMap.set(option.value, option.label);
		if (
			option.isOther ||
			option.type === 'other' ||
			option.allowCustom ||
			option.hasInput ||
			(option.label &&
				(option.label.toLowerCase().includes('other') ||
					option.label.toLowerCase().includes('enter other') ||
					option.label.toLowerCase().includes('custom') ||
					option.label.toLowerCase().includes('specify')))
		) {
			otherOptionIds.add(option.value);
		}
	});

	// 提取Other选项的自定义值
	let otherValues: { [key: string]: string } = {};
	if (responseText && questionId) {
		otherValues = extractOtherValues(responseText, questionId);
	}

	const labels: string[] = [];
	const processedValues = new Set<string>(); // 跟踪已处理的值

	answerValues.forEach((value) => {
		const optionLabel = optionMap.get(value);

		if (optionLabel) {
			if (otherOptionIds.has(value)) {
				// 这是一个Other选项，查找自定义值
				const customValue =
					otherValues[value] || otherValues[value.replace('option_', 'option-')];
				if (customValue) {
					labels.push(`Other: ${customValue}`);
					// 标记这个自定义值已处理，避免显示原始的输入值
					processedValues.add(customValue.toLowerCase());
				} else {
					labels.push(optionLabel);
				}
			} else {
				// 检查这个值是否是某个Other选项的自定义输入值
				const isCustomInput = Object.values(otherValues).some(
					(customVal) => customVal.toLowerCase() === value.toLowerCase()
				);
				if (!isCustomInput) {
					labels.push(optionLabel);
				}
			}
		} else {
			// 检查是否是Other相关的值
			const isOtherValue = Object.keys(otherValues).some((otherKey) => {
				return (
					otherKey.includes(value) ||
					value.includes(otherKey.replace('option-', '').replace('option_', ''))
				);
			});

			if (isOtherValue) {
				const customValue = Object.entries(otherValues).find(
					([key]) =>
						key.includes(value) ||
						value.includes(key.replace('option-', '').replace('option_', ''))
				)?.[1];
				if (customValue) {
					labels.push(`Other: ${customValue}`);
					processedValues.add(customValue.toLowerCase());
				}
			} else {
				// 检查这个值是否是某个Other选项的自定义输入值，如果是则跳过
				const isCustomInput = Object.values(otherValues).some(
					(customVal) => customVal.toLowerCase() === value.toLowerCase()
				);
				if (!isCustomInput && !processedValues.has(value.toLowerCase())) {
					labels.push(value);
				}
			}
		}
	});

	return labels.filter(Boolean);
};

// 获取网格答案标签
const getGridAnswerLabels = (
	answer: any,
	questionConfig: any,
	responseText?: string,
	questionId?: string
): string[] => {
	if (!answer) return [];

	// 如果没有questionConfig，仍然尝试解析Other选项
	if (!questionConfig?.columns) {
		const answerIds = getCheckboxAnswers(answer);

		// 尝试提取Other选项的自定义值
		let otherValues: { [key: string]: string } = {};
		if (responseText && questionId) {
			otherValues = extractOtherValues(responseText, questionId);
		}

		// 处理答案ID，替换Other选项和提供基本的列映射
		const processedLabels: string[] = [];
		answerIds.forEach((id) => {
			if (id.includes('other') || id.startsWith('column-other-')) {
				const customValue =
					otherValues[id] ||
					Object.entries(otherValues).find(([key]) => key.includes(id))?.[1];
				if (customValue) {
					processedLabels.push(`Other: ${customValue}`);
				} else {
					processedLabels.push(id);
				}
			} else {
				// 为column ID创建简化的标签
				let displayLabel = id;
				if (id.startsWith('column-')) {
					// 为每个唯一的column ID分配字母标签
					displayLabel = getColumnLabel(id, questionId);
				}
				processedLabels.push(displayLabel);
			}
		});

		return processedLabels;
	}

	const answerIds = getCheckboxAnswers(answer);
	const columnMap = new Map<string, string>();
	const otherColumnIds = new Set<string>();

	// 建立列映射并识别Other列
	questionConfig.columns.forEach((column: any) => {
		columnMap.set(column.id, column.label);
		if (
			column.isOther ||
			column.type === 'other' ||
			column.allowCustom ||
			column.hasInput ||
			(column.label &&
				(column.label.toLowerCase().includes('other') ||
					column.label.toLowerCase().includes('enter other') ||
					column.label.toLowerCase().includes('custom') ||
					column.label.toLowerCase().includes('specify')))
		) {
			otherColumnIds.add(column.id);
		}
	});

	// 从responseText中提取Other选项的自定义值
	let otherValues: { [key: string]: string } = {};
	if (responseText && questionId) {
		otherValues = extractOtherValues(responseText, questionId);
	}

	// 将ID转换为对应的label
	const labels: string[] = [];
	answerIds.forEach((id) => {
		const columnLabel = columnMap.get(id);

		if (columnLabel) {
			// 如果这是一个other类型的列，显示自定义值
			if (
				otherColumnIds.has(id) ||
				id.includes('other') ||
				columnLabel.toLowerCase().includes('other')
			) {
				// 查找对应的自定义值，尝试多种格式
				const customValue =
					otherValues[id] ||
					otherValues[id.replace('column-', 'column-other-')] ||
					Object.entries(otherValues).find(([key]) => key.includes(id))?.[1];

				if (customValue) {
					labels.push(`Other: ${customValue}`);
				} else {
					labels.push(columnLabel);
				}
			} else {
				labels.push(columnLabel);
			}
		} else {
			// 没有找到对应的列配置，检查是否是Other相关的值
			const isOtherValue = Object.keys(otherValues).some((otherKey) => {
				return (
					otherKey.includes(id) || (id.includes('other') && otherKey.includes('other'))
				);
			});

			if (isOtherValue) {
				const customValue = Object.entries(otherValues).find(
					([key]) => key.includes(id) || (id.includes('other') && key.includes('other'))
				)?.[1];
				if (customValue) {
					labels.push(`Other: ${customValue}`);
				} else if (id === 'Other' || id.toLowerCase() === 'other') {
					// 如果答案就是"Other"，查找任何other相关的自定义值
					const anyOtherValue = Object.values(otherValues)[0];
					if (anyOtherValue) {
						labels.push(`Other: ${anyOtherValue}`);
					} else {
						labels.push(id);
					}
				}
			} else {
				// 为column ID创建简化的标签
				let displayLabel = id;
				if (id.startsWith('column-')) {
					displayLabel = getColumnLabel(id, questionId);
				}
				labels.push(displayLabel);
			}
		}
	});

	return labels.filter(Boolean);
};

// 检查是否有有效答案
const hasValidAnswer = (answer: string | any): boolean => {
	if (!answer) return false;

	if (typeof answer === 'string') {
		const trimmed = answer.trim();
		// 检查空的JSON对象字符串
		if (trimmed === '{}' || trimmed === '[]') {
			return false;
		}
		return (
			trimmed !== '' &&
			trimmed !== 'No answer provided' &&
			trimmed !== 'No selection made' &&
			trimmed !== 'null' &&
			trimmed !== 'undefined'
		);
	}

	if (Array.isArray(answer)) {
		return answer.length > 0;
	}

	if (typeof answer === 'object' && answer !== null) {
		// 检查是否是空对象
		return Object.keys(answer).length > 0;
	}

	return true;
};

// 增强的答案格式化函数
const formatAnswerWithConfig = (response: any, questionnaireConfig: any): string => {
	if (!response.answer && !response.responseText) {
		return 'No answer';
	}

	const answer = response.answer || response.responseText;

	// 检查是否有有效答案
	if (!hasValidAnswer(answer)) {
		return 'No answer';
	}
	const type = response.type;
	const questionId = response.questionId;

	// 查找问题配置
	let questionConfig: any = null;
	if (
		questionnaireConfig &&
		questionnaireConfig.sections &&
		Array.isArray(questionnaireConfig.sections)
	) {
		for (const section of questionnaireConfig.sections) {
			if (section.questions && Array.isArray(section.questions)) {
				const question = section.questions.find(
					(q: any) =>
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
			if (typeof answer === 'string' && (answer.trim() === '{}' || answer.trim() === '[]')) {
				return 'No answer';
			}
			if (questionConfig && questionConfig.options && Array.isArray(questionConfig.options)) {
				const option = questionConfig.options.find((opt: any) => opt.value === answer);
				return option?.label || String(answer);
			}
			return String(answer);

		case 'dropdown':
			// 处理下拉选择
			if (typeof answer === 'string' && (answer.trim() === '{}' || answer.trim() === '[]')) {
				return 'No answer';
			}
			if (questionConfig && questionConfig.options && Array.isArray(questionConfig.options)) {
				const option = questionConfig.options.find((opt: any) => opt.value === answer);
				return option?.label || String(answer);
			}
			return String(answer);

		case 'checkboxes':
			// 处理多选题，支持Other选项的自定义值
			const checkboxLabels = getCheckboxLabels(
				answer,
				questionConfig,
				response.responseText,
				response.questionId
			);
			return checkboxLabels.join(', ');

		case 'multiple_choice_grid':
		case 'checkbox_grid':
			// 处理网格类型题目，支持Other选项的自定义值
			const gridLabels = getGridAnswerLabels(
				answer,
				questionConfig,
				response.responseText,
				response.questionId
			);
			return gridLabels.join(', ');

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
const parseQuestionnaireAnswerChangesWithConfig = async (
	beforeData: any,
	afterData: any,
	currentChange?: any
): Promise<string[]> => {
	if (!afterData) return [];

	try {
		const after = typeof afterData === 'string' ? JSON.parse(afterData) : afterData;
		const changesList: string[] = [];

		// 从当前变更记录中获取 stageId
		let stageId = '';

		// 首先尝试从 currentChange 获取
		if (currentChange?.stageId) {
			stageId = String(currentChange.stageId);
		}
		// 然后尝试从 props 获取
		else if (props.stageId) {
			stageId = String(props.stageId);
		}

		// 获取问卷配置（通过阶段ID）
		let questionnaireConfig = null;
		if (stageId) {
			questionnaireConfig = await getQuestionnaireConfigByStageId(stageId);
		}

		// 处理问卷答案提交的情况（只有 afterData）
		if (!beforeData && after.responses) {
			after.responses.forEach((response: any) => {
				if (response.answer || response.responseText) {
					// 确保 responseText 被正确传递
					const enhancedResponse = {
						...response,
						responseText: response.responseText || after.responseText,
					};
					const formattedAnswer = formatAnswerWithConfig(
						enhancedResponse,
						questionnaireConfig
					);
					changesList.push(
						`${response.question || response.questionId}: ${formattedAnswer}`
					);
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
						const enhancedAfterResp = {
							...afterResp,
							responseText: afterResp.responseText || after.responseText,
						};
						const formattedAnswer = formatAnswerWithConfig(
							enhancedAfterResp,
							questionnaireConfig
						);
						changesList.push(`${afterResp.question || questionId}: ${formattedAnswer}`);
					} else if (
						JSON.stringify(beforeResp.answer) !== JSON.stringify(afterResp.answer) ||
						beforeResp.responseText !== afterResp.responseText
					) {
						// 修改的答案
						const enhancedBeforeResp = {
							...beforeResp,
							responseText: beforeResp.responseText || before.responseText,
						};
						const enhancedAfterResp = {
							...afterResp,
							responseText: afterResp.responseText || after.responseText,
						};
						const beforeAnswer = formatAnswerWithConfig(
							enhancedBeforeResp,
							questionnaireConfig
						);
						const afterAnswer = formatAnswerWithConfig(
							enhancedAfterResp,
							questionnaireConfig
						);
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
