<template>
	<div class="customer-block">
		<!-- 统一的头部卡片 -->
		<div
			class="change-log-header-card rounded-md"
			:class="{ expanded: isExpanded }"
			@click="toggleExpanded"
		>
			<div class="flex justify-between">
				<div>
					<div class="flex items-center">
						<el-icon class="expand-icon text-lg mr-2" :class="{ rotated: isExpanded }">
							<ArrowRight />
						</el-icon>
						<h3 class="change-log-title">Change Log</h3>
					</div>
				</div>
				<div class="change-log-actions">
					<el-button
						v-if="isExpanded"
						size="small"
						:icon="RefreshRight"
						:loading="loading"
						@click.stop="loadChangeLogs"
						class="refresh-button"
					>
						Refresh
					</el-button>
				</div>
			</div>
		</div>

		<!-- 可折叠内容 -->
		<el-collapse-transition>
			<div v-show="isExpanded" class="">
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
									<div
										v-if="
											row.type === 'Field Change' && row.fieldChanges?.length
										"
									>
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
											row.type === 'Answer Update' ||
											row.type === 'Answer Submit' ||
											row.type === 'QuestionnaireAnswerUpdate' ||
											row.type === 'QuestionnaireAnswerSubmit'
										"
									>
										<!-- 如果有具体变更，显示变更详情 -->
										<div v-if="row.answerChanges?.length" class="space-y-2">
											<div
												v-for="(change, index) in row.answerChanges"
												:key="index"
												class="bg-blue-50 dark:bg-blue-900/20 p-2 rounded text-xs border-l-4 border-blue-400"
											>
												{{ change }}
											</div>
										</div>
										<!-- 如果没有具体变更，显示"没有变化" -->
										<div
											v-else
											class="bg-gray-50 dark:bg-gray-900/20 p-2 rounded text-xs border-l-4 border-gray-400"
										>
											<span class="text-gray-600 dark:text-gray-400 italic">
												No changes
											</span>
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
												<span class="font-medium">
													{{ row.fileInfo.fileName }}
												</span>
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

									<!-- Action Execution 详情 -->
									<div
										v-else-if="
											(row.type === 'Action Success' ||
												row.type === 'Action Failed' ||
												row.type === 'Action Running' ||
												row.type === 'Action Pending' ||
												row.type === 'Action Cancelled' ||
												row.type === 'ActionExecutionSuccess' ||
												row.type === 'ActionExecutionFailed' ||
												row.type === 'ActionExecutionRunning' ||
												row.type === 'ActionExecutionPending' ||
												row.type === 'ActionExecutionCancelled' ||
												row.type === 'Stage Action' ||
												row.type === 'Task Action' ||
												row.type === 'Question Action') &&
											row.actionInfo
										"
									>
										<div
											:class="getActionExecutionBgClass(row.type)"
											class="p-3 rounded-md border-l-4"
										>
											<div class="text-sm">
												<div
													class="font-semibold mb-2 flex items-center justify-between"
												>
													<span class="text-gray-800 dark:text-gray-200">
														{{ row.actionInfo.actionName }}
													</span>
													<span
														:class="
															getActionExecutionStatusClass(row.type)
														"
														class="px-2 py-1 rounded text-xs font-medium"
													>
														{{ getActionExecutionStatusText(row.type) }}
													</span>
												</div>

												<!-- 显示 operationTitle -->
												<div v-if="row.operationTitle" class="mb-2">
													<div
														class="text-gray-800 dark:text-gray-200 text-sm font-medium"
													>
														{{ row.operationTitle }}
													</div>
												</div>

												<!-- 显示 operationDescription -->
												<div v-if="row.operationDescription" class="mb-3">
													<div
														class="text-gray-700 dark:text-gray-300 text-sm leading-relaxed whitespace-pre-line"
													>
														{{ row.operationDescription }}
													</div>
												</div>

												<div class="space-y-1 text-xs">
													<!-- 显示 Action 来源 -->
													<div
														v-if="getActionSource(row.type)"
														class="flex items-center"
													>
														<span class="text-gray-500 mr-2">
															Source:
														</span>
														<span
															:class="getActionSourceClass(row.type)"
															class="px-2 py-1 rounded text-xs font-medium"
														>
															{{ getActionSource(row.type) }}
														</span>
													</div>
													<div class="flex items-center">
														<span class="text-gray-500 mr-2">
															Type:
														</span>
														<span
															class="text-gray-700 dark:text-gray-300"
														>
															{{ row.actionInfo.actionType }}
														</span>
													</div>
													<!-- 执行状态 -->
													<div
														v-if="row.actionInfo.executionStatus"
														class="flex items-center"
													>
														<span class="text-gray-500 mr-2">
															Status:
														</span>
														<span
															:class="
																getExecutionStatusClass(
																	row.actionInfo.executionStatus
																)
															"
															class="px-2 py-1 rounded text-xs font-medium"
														>
															{{ row.actionInfo.executionStatus }}
														</span>
													</div>

													<!-- 执行时间范围 -->
													<div
														v-if="row.actionInfo.startedAt"
														class="flex items-center"
													>
														<span class="text-gray-500 mr-2">
															Started:
														</span>
														<span
															class="text-gray-700 dark:text-gray-300 text-xs"
														>
															{{
																formatDateTime(
																	row.actionInfo.startedAt
																)
															}}
														</span>
													</div>
													<div
														v-if="row.actionInfo.completedAt"
														class="flex items-center"
													>
														<span class="text-gray-500 mr-2">
															Completed:
														</span>
														<span
															class="text-gray-700 dark:text-gray-300 text-xs"
														>
															{{
																formatDateTime(
																	row.actionInfo.completedAt
																)
															}}
														</span>
													</div>
													<div
														v-if="row.actionInfo.duration"
														class="flex items-center"
													>
														<span class="text-gray-500 mr-2">
															Duration:
														</span>
														<span
															class="text-gray-700 dark:text-gray-300"
														>
															{{
																formatDuration(
																	row.actionInfo.duration
																)
															}}
														</span>
													</div>
													<div
														v-if="row.actionInfo.executionId"
														class="flex items-center"
													>
														<span class="text-gray-500 mr-2">
															Execution ID:
														</span>
														<span
															class="text-gray-700 dark:text-gray-300 font-mono text-xs"
														>
															{{ row.actionInfo.executionId }}
														</span>
													</div>
													<!-- 显示执行输出摘要 -->
													<div
														v-if="
															getExecutionOutputSummary(
																row.actionInfo.executionOutput
															)
														"
														class="mt-2"
													>
														<div class="text-gray-500 text-xs mb-1">
															Output:
														</div>
														<div
															class="bg-gray-100 dark:bg-gray-800 p-2 rounded text-xs"
														>
															{{
																getExecutionOutputSummary(
																	row.actionInfo.executionOutput
																)
															}}
														</div>
													</div>

													<!-- 显示执行输入摘要 -->
													<div
														v-if="
															getExecutionInputSummary(
																row.actionInfo.executionInput
															)
														"
														class="mt-2"
													>
														<div class="text-gray-500 text-xs mb-1">
															Input:
														</div>
														<div
															class="bg-blue-50 dark:bg-blue-900/20 p-2 rounded text-xs"
														>
															{{
																getExecutionInputSummary(
																	row.actionInfo.executionInput
																)
															}}
														</div>
													</div>

													<!-- 错误信息显示 -->
													<div
														v-if="row.actionInfo.errorMessage"
														class="mt-2"
													>
														<div
															class="text-red-600 dark:text-red-400 text-xs"
														>
															<span class="font-medium">Error:</span>
															{{ row.actionInfo.errorMessage }}
														</div>
													</div>

													<!-- 错误堆栈跟踪（只在有错误时显示） -->
													<div
														v-if="row.actionInfo.errorStackTrace"
														class="mt-1"
													>
														<details class="text-red-500 text-xs">
															<summary
																class="cursor-pointer hover:text-red-700"
															>
																Stack Trace
															</summary>
															<pre
																class="mt-1 whitespace-pre-wrap bg-red-50 dark:bg-red-900/20 p-2 rounded text-xs overflow-x-auto"
																>{{
																	row.actionInfo.errorStackTrace
																}}</pre
															>
														</details>
													</div>
												</div>
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
									v-if="row.updatedBy && row.updatedBy.trim() !== ''"
									class="text-gray-900 dark:text-white-100 truncate"
									:title="row.updatedBy"
								>
									{{ row.updatedBy }}
								</span>
								<span
									v-else
									class="text-gray-400 dark:text-gray-500 text-sm italic"
								>
									<!-- 系统操作不显示操作者 -->
								</span>
							</template>
						</el-table-column>

						<el-table-column label="Date & Time" width="200">
							<template #default="{ row }">
								<div
									class="flex items-center text-gray-600 dark:text-gray-400 text-sm"
								>
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
								<div v-if="!props.stageId" class="text-lg mb-2">
									Please select a stage
								</div>
								<div v-else class="text-lg mb-2">No change records found</div>
								<div class="text-sm">
									{{
										!props.stageId
											? 'Change logs require a stage selection. Please select a stage to view its change history.'
											: 'No changes recorded for this stage yet.'
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
		</el-collapse-transition>
	</div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'; // 移除 computed
import { Clock, Document, RefreshRight, ArrowRight } from '@element-plus/icons-vue';
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
const isExpanded = ref(false); // 折叠状态

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
	actionInfo?: any;
}

const processedChanges = ref<ProcessedChange[]>([]); // 修正类型

// 防止重复请求

// 加载数据
const loadChangeLogs = async () => {
	if (!props.onboardingId) return;

	// stageId 现在是必填参数，如果没有则不加载数据
	if (!props.stageId) {
		console.warn('⚠️ Change logs loading skipped: stageId is required parameter');
		changes.value = [];
		processedChanges.value = [];
		total.value = 0;
		return;
	}

	// 防止重复请求
	if (loading.value) return;

	loading.value = true;

	try {
		// stageId 是必填参数，确保API调用格式统一
		const apiParams = {
			stageId: String(props.stageId), // 必填参数
			pageIndex: currentPage.value,
			pageSize: pageSize.value,
		};
		const response = await getChangeLogsByOnboarding(props.onboardingId, apiParams);

		if (response.code == '200') {
			// 映射API数据到组件期望的格式
			let rawItems = response.data.items || [];

			changes.value = rawItems.map((item: any) => {
				return {
					id: item.id,
					type: item.operationType, // 使用operationType作为type
					details: item.operationDescription || item.operationTitle || '', // 详细描述
					operationTitle: item.operationTitle, // 保留原始标题
					operationDescription: item.operationDescription, // 保留原始描述
					beforeData: item.beforeData,
					afterData: item.afterData,
					changedFields: item.changedFields || [],
					updatedBy: item.operatorName || '', // 操作人，空字符串表示系统操作
					dateTime: item.operationTime || item.createDate || '', // 操作时间
					extendedInfo: item.extendedData, // 扩展信息
					stageId: item.stageId, // 添加 stageId 映射
					onboardingId: item.onboardingId, // 也添加 onboardingId 以备后用
				};
			});

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

// 处理变更数据 - 优化版
const processChangesData = async () => {
	const processedData: ProcessedChange[] = [];

	// 创建类型处理器映射，减少switch语句复杂度
	const typeHandlers = {
		questionnaire: ['Answer Update', 'QuestionnaireAnswerUpdate', 'QuestionnaireAnswerSubmit'],
		field: ['Field Change', 'StaticFieldValueChange'],
		task: [
			'Task Complete',
			'Task Incomplete',
			'ChecklistTaskComplete',
			'ChecklistTaskUncomplete',
		],
		file: ['File Upload', 'FileUpload'],
		action: [
			'Action Success',
			'Action Failed',
			'Action Running',
			'Action Pending',
			'Action Cancelled',
			'ActionExecutionSuccess',
			'ActionExecutionFailed',
			'ActionExecutionRunning',
			'ActionExecutionPending',
			'ActionExecutionCancelled',
			'StageActionExecution',
			'TaskActionExecution',
			'QuestionActionExecution',
		],
		basic: ['Completion', 'Update', 'StageTransition', 'PriorityChange'],
	};

	// 反向映射：从操作类型到处理器类型
	const typeToHandler = new Map<string, string>();
	Object.entries(typeHandlers).forEach(([handler, types]) => {
		types.forEach((type) => typeToHandler.set(type, handler));
	});

	for (const change of changes.value) {
		const typeInfo =
			change.typeIcon && change.typeColor
				? { label: change.type, icon: change.typeIcon, color: change.typeColor }
				: getOperationTypeInfo(change.type);

		// 根据操作类型确定处理器
		const handlerType = typeToHandler.get(change.type) || 'basic';
		let specificData: any = {};

		// 根据处理器类型解析数据
		switch (handlerType) {
			case 'questionnaire':
				specificData.answerChanges = await parseQuestionnaireAnswerChanges(
					change.beforeData,
					change.afterData,
					change
				);
				break;

			case 'field':
				specificData.fieldChanges = parseStaticFieldChanges(
					change.beforeData,
					change.afterData,
					change.changedFields
				);
				break;

			case 'task':
				specificData.taskInfo = {
					taskName: extractTaskName(change.details),
					statusChange: parseTaskStatusChanges(change.beforeData, change.afterData),
				};
				break;

			case 'file':
				specificData.fileInfo = extractFileInfo(change.extendedInfo);
				break;

			case 'action':
				specificData.actionInfo = extractActionInfo(change);
				break;

			default:
				// 基本类型不需要特殊处理
				break;
		}

		processedData.push({
			...change,
			type: typeInfo.label,
			typeIcon: typeInfo.icon,
			typeColor: typeInfo.color,
			answerChanges: [],
			fieldChanges: [],
			taskInfo: null,
			fileInfo: null,
			actionInfo: null,
			...specificData,
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

// 获取多选题标签 - 优化版
const getCheckboxLabels = (
	answer: any,
	questionConfig: any,
	responseText?: string,
	questionId?: string
): string[] => {
	if (!answer) return [];

	const answerValues = getCheckboxAnswers(answer);
	if (answerValues.length === 0) return [];

	// 提取Other选项的自定义值
	const otherValues =
		responseText && questionId ? extractOtherValues(responseText, questionId) : {};

	// 如果没有questionConfig，使用简化处理
	if (!questionConfig?.options) {
		return answerValues
			.map((value) => {
				if (value.startsWith('option_')) {
					const customValue =
						otherValues[value] || otherValues[value.replace('option_', 'option-')];
					return customValue ? `Other: ${customValue}` : value;
				}
				return value;
			})
			.filter(Boolean);
	}

	// 建立选项映射
	const optionMap = new Map();
	const otherOptionIds = new Set();
	const isOtherOption = (option: any) =>
		option.isOther ||
		option.type === 'other' ||
		option.allowCustom ||
		option.hasInput ||
		option.label?.toLowerCase().match(/other|custom|specify|enter other/);

	questionConfig.options.forEach((option: any) => {
		optionMap.set(option.value, option.label);
		if (isOtherOption(option)) {
			otherOptionIds.add(option.value);
		}
	});

	const processedValues = new Set();
	const labels = answerValues
		.map((value) => {
			const optionLabel = optionMap.get(value);

			if (optionLabel) {
				if (otherOptionIds.has(value)) {
					// Other选项处理
					const customValue =
						otherValues[value] || otherValues[value.replace('option_', 'option-')];
					if (customValue) {
						processedValues.add(customValue.toLowerCase());
						return `Other: ${customValue}`;
					}
					return optionLabel;
				}
				// 检查是否为自定义输入值
				const isCustomInput = Object.values(otherValues).some(
					(customVal) => customVal.toLowerCase() === value.toLowerCase()
				);
				return isCustomInput ? null : optionLabel;
			}

			// 处理Other相关值或未映射值
			const relatedOtherValue = Object.entries(otherValues).find(
				([key]) => key.includes(value) || value.includes(key.replace(/^option[-_]/, ''))
			);

			if (relatedOtherValue) {
				processedValues.add(relatedOtherValue[1].toLowerCase());
				return `Other: ${relatedOtherValue[1]}`;
			}

			// 避免显示重复的自定义输入值
			const isCustomInput = Object.values(otherValues).some(
				(customVal) => customVal.toLowerCase() === value.toLowerCase()
			);
			return !isCustomInput && !processedValues.has(value.toLowerCase()) ? value : null;
		})
		.filter(Boolean);

	return labels;
};

// 获取网格答案标签
const getGridAnswerLabels = (
	answer: any,
	questionConfig: any,
	responseText?: string,
	questionId?: string
): string[] => {
	if (!answer) return [];

	// 如果没有 questionConfig，仍然尝试解析 Other 选项
	if (!questionConfig?.columns) {
		const answerIds = getCheckboxAnswers(answer);

		// 提取 Other 自定义值
		let otherValues: { [key: string]: string } = {};
		if (responseText && questionId) {
			otherValues = extractOtherValues(responseText, questionId);
		}

		const processedLabels: string[] = [];
		answerIds.forEach((rawId) => {
			const id = String(rawId);
			const idLower = id.toLowerCase();
			if (
				idLower.includes('other') ||
				idLower === 'other' ||
				idLower.startsWith('column-other-')
			) {
				const customValue =
					otherValues[id] ||
					Object.entries(otherValues).find(([key]) =>
						key.toLowerCase().includes(idLower)
					)?.[1] ||
					Object.values(otherValues)[0];
				processedLabels.push(customValue ? `Other: ${customValue}` : 'Other');
			} else {
				// 为 column ID 创建简化的标签
				let displayLabel = id;
				if (id.startsWith('column-')) {
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

	// 建立列映射并识别 Other 列
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

	// 从 responseText 中提取 Other 自定义值
	let otherValues: { [key: string]: string } = {};
	if (responseText && questionId) {
		otherValues = extractOtherValues(responseText, questionId);
	}

	// 将 ID 转换为对应的 label
	const labels: string[] = [];
	answerIds.forEach((rawId) => {
		const id = String(rawId);
		const idLower = id.toLowerCase();
		const columnLabel = columnMap.get(id);

		if (columnLabel) {
			// Other 列显示自定义值
			if (
				otherColumnIds.has(id) ||
				idLower.includes('other') ||
				columnLabel.toLowerCase().includes('other')
			) {
				const customValue =
					otherValues[id] ||
					otherValues[id.replace('column-', 'column-other-')] ||
					Object.entries(otherValues).find(([key]) =>
						key.toLowerCase().includes(idLower)
					)?.[1] ||
					Object.values(otherValues)[0];

				labels.push(customValue ? `Other: ${customValue}` : columnLabel);
			} else {
				labels.push(columnLabel);
			}
		} else {
			// 未配置映射，尝试与 otherValues 关联
			const hasRelatedOther = Object.keys(otherValues).some((key) =>
				key.toLowerCase().includes(idLower)
			);
			if (hasRelatedOther || idLower.includes('other')) {
				const customValue =
					Object.entries(otherValues).find(([key]) =>
						key.toLowerCase().includes(idLower)
					)?.[1] || Object.values(otherValues)[0];
				labels.push(customValue ? `Other: ${customValue}` : 'Other');
			} else {
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

// 解析短答网格单行（或多行）答案为 “行:值” 列表字符串
const getShortAnswerGridRowSummary = (response: any, questionConfig: any): string => {
	const parsed = parseResponseText(response?.responseText || '');
	if (!parsed || Object.keys(parsed).length === 0) {
		return 'No answer';
	}

	// 建立 rowId -> label 的映射
	const rowIdToLabel = new Map<string, string>();
	if (questionConfig?.rows && Array.isArray(questionConfig.rows)) {
		questionConfig.rows.forEach((r: any) => rowIdToLabel.set(r.id, r.label));
	}

	const rows: Array<{ label: string; value: string }> = [];
	Object.entries(parsed).forEach(([key, val]) => {
		const parts = key.split('_');
		const rowPart = parts.find((p) => p.startsWith('row-')) || '';
		const label = rowIdToLabel.get(rowPart) || rowPart.replace('row-', '');
		const value = String(val ?? '').trim();
		if (label && value) {
			rows.push({ label, value });
		}
	});

	// 兜底：如果解析不到 rowPart，尝试从 question 文本中取行号
	if (rows.length === 0) {
		const firstVal = Object.values(parsed)[0];
		let rowLabel = '';
		if (typeof response?.question === 'string') {
			const match = response.question.match(/-\s*(\d+)\s*$/);
			if (match) rowLabel = match[1];
		}
		if (rowLabel && firstVal) {
			return `${rowLabel}:${String(firstVal)}`;
		}
	}

	return rows.map((r) => `${r.label}:${r.value}`).join(' ');
};

// 检查是否有有效答案 - 优化版
const hasValidAnswer = (answer: string | any): boolean => {
	if (!answer) return false;

	if (typeof answer === 'string') {
		const trimmed = answer.trim();
		// 使用正则表达式简化检查
		return (
			trimmed !== '' &&
			!/^({}|\[\]|null|undefined|No answer provided|No selection made)$/.test(trimmed)
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

		case 'short_answer_grid':
			return getShortAnswerGridRowSummary(response, questionConfig);

		case 'date':
			return formatAnswerDate(answer, 'date');
		case 'time':
			return formatAnswerDate(answer, 'time');

		default:
			// 对于其他类型，使用原有逻辑
			if (type === 'file' || type === 'file_upload') {
				if (Array.isArray(answer)) {
					const fileNames = answer.map((file: any) => {
						if (typeof file === 'object' && file && file.name) {
							return file.name;
						}
						return 'file';
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
// 简化的问卷答案变更解析
const parseQuestionnaireAnswerChanges = async (
	beforeData: any,
	afterData: any,
	currentChange?: any
): Promise<string[]> => {
	if (!afterData) return [];

	try {
		const after = typeof afterData === 'string' ? JSON.parse(afterData) : afterData;
		const changesList: string[] = [];

		// 获取问卷配置
		const stageId = currentChange?.stageId || props.stageId;
		const questionnaireConfig = stageId ? await getQuestionnaireConfigByStageId(stageId) : null;

		// 处理答案提交（只有 afterData）
		if (!beforeData && after.responses) {
			after.responses.forEach((response: any) => {
				if (response.answer || response.responseText) {
					const formattedAnswer = formatAnswerWithConfig(response, questionnaireConfig);
					const questionTitle = response.question || response.questionId;
					changesList.push(`${questionTitle}: ${formattedAnswer}`);
				}
			});
			return changesList;
		}

		// 处理答案更新（有 beforeData 和 afterData）
		if (beforeData && afterData) {
			const before: any =
				typeof beforeData === 'string' ? JSON.parse(beforeData) : beforeData;

			if (before.responses && after.responses) {
				const beforeMap = new Map(before.responses.map((r: any) => [r.questionId, r]));

				after.responses.forEach((afterResp: any) => {
					const beforeResp: any = beforeMap.get(afterResp.questionId);
					const questionTitle = afterResp.question || afterResp.questionId;

					if (!beforeResp) {
						// 新增答案
						const formattedAnswer = formatAnswerWithConfig(
							afterResp,
							questionnaireConfig
						);
						changesList.push(`${questionTitle}: ${formattedAnswer}`);
					} else if (
						JSON.stringify(beforeResp?.answer) !== JSON.stringify(afterResp?.answer)
					) {
						// 修改答案
						const beforeAnswer = formatAnswerWithConfig(
							beforeResp,
							questionnaireConfig
						);
						const afterAnswer = formatAnswerWithConfig(afterResp, questionnaireConfig);
						changesList.push(`${questionTitle}: ${beforeAnswer} → ${afterAnswer}`);
					}
				});
			}
		}

		return changesList;
	} catch (error) {
		console.error('Error parsing questionnaire answer changes:', error);
		return ['Questionnaire updated'];
	}
};

// 工具函数集合 - 简化和整合版

// 任务名称提取
const extractTaskName = (details: string): string => {
	const patterns = [
		/Task (?:Complete|Incomplete|Completed|Uncompleted): (.+)/,
		/Checklist task '(.+)' has been/,
		/'(.+)' has been (?:completed|marked as incomplete)/,
	];
	return patterns.find((p) => p.test(details))?.exec(details)?.[1] || details;
};

// 文件信息提取
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

// 文件大小格式化
const formatFileSize = (bytes: number): string => {
	if (!bytes) return '0 Bytes';
	const k = 1024;
	const sizes = ['Bytes', 'KB', 'MB', 'GB'];
	const i = Math.floor(Math.log(bytes) / Math.log(k));
	return `${parseFloat((bytes / Math.pow(k, i)).toFixed(2))} ${sizes[i]}`;
};

// 日期时间格式化工具集合
const formatAnswerDate = (dateStr: any, questionType?: string): string => {
	if (!dateStr) return '';
	try {
		const date = new Date(String(dateStr));
		if (isNaN(date.getTime())) return String(dateStr);

		if (questionType === 'time') {
			return [date.getHours(), date.getMinutes(), date.getSeconds()]
				.map((n) => String(n).padStart(2, '0'))
				.join(':');
		}

		const month = String(date.getMonth() + 1).padStart(2, '0');
		const day = String(date.getDate()).padStart(2, '0');
		return `${month}/${day}/${date.getFullYear()}`;
	} catch {
		return String(dateStr);
	}
};

const formatDateTime = (dateString: string): string => {
	try {
		return dateString
			? timeZoneConvert(dateString, false, projectTenMinutesSsecondsDate)
			: defaultStr;
	} catch {
		return dateString || defaultStr;
	}
};

// 简化的分页处理函数
const handleCurrentChange = (newPage: number) => {
	currentPage.value = newPage;
};
const handlePaginationUpdate = () => {
	loadChangeLogs();
};
const handlePageUpdate = (newSize: number) => {
	pageSize.value = newSize;
};

// 优化的标签类型获取 - 使用Map减少switch复杂度
const getTagType = (type: string): string => {
	const tagTypeMap = new Map<string, string>([
		// Success types
		...[
			'Completion',
			'ChecklistTaskComplete',
			'Task Complete',
			'Action Success',
			'ActionExecutionSuccess',
			'Task Action',
			'TaskActionExecution',
		].map((t): [string, string] => [t, 'success']),
		// Warning types
		...[
			'Task Incomplete',
			'ChecklistTaskUncomplete',
			'Field Change',
			'StaticFieldValueChange',
			'Action Cancelled',
			'ActionExecutionCancelled',
			'Question Action',
			'QuestionActionExecution',
		].map((t): [string, string] => [t, 'warning']),
		// Danger types
		...['Action Failed', 'ActionExecutionFailed'].map((t): [string, string] => [t, 'danger']),
		// Info types (default)
		...[
			'Answer Update',
			'QuestionnaireAnswerUpdate',
			'Answer Submit',
			'QuestionnaireAnswerSubmit',
			'FileUpload',
			'File Upload',
			'Action Running',
			'Action Pending',
			'ActionExecutionRunning',
			'ActionExecutionPending',
			'Stage Action',
			'StageActionExecution',
		].map((t): [string, string] => [t, 'info']),
	]);

	return tagTypeMap.get(type) ?? 'info';
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

// ActionExecution 相关辅助函数
// 简化的Action信息提取
const extractActionInfo = (change: any): any => {
	try {
		const extendedInfo = parseExtendedData(change.extendedInfo || change.extendedData);

		return {
			actionName: change.operationTitle?.replace(/^Action\s+\w+:\s*/, '') || 'Action',
			...extendedInfo,
		};
	} catch (error) {
		console.warn('Failed to extract action info:', error);
		return {
			actionName: 'Action',
			actionType: '',
			executionId: '',
			duration: null,
			executionStatus: '',
			errorMessage: '',
		};
	}
};

// 解析 extendedData 获取详细的 action execution 信息
const parseExtendedData = (extendedData: string): any => {
	if (!extendedData) return {};

	try {
		return typeof extendedData === 'string' ? JSON.parse(extendedData) : extendedData;
	} catch (error) {
		console.warn('Failed to parse extended data:', error);
		return {};
	}
};

// 删除了冗余的辅助函数，简化代码结构

const formatDuration = (durationMs: number): string => {
	if (durationMs < 1000) {
		return `${durationMs}ms`;
	} else if (durationMs < 60000) {
		return `${(durationMs / 1000).toFixed(1)}s`;
	} else {
		const minutes = Math.floor(durationMs / 60000);
		const seconds = Math.floor((durationMs % 60000) / 1000);
		return `${minutes}m ${seconds}s`;
	}
};

// 简化的样式映射 - 直接映射类型到样式类
const actionBgClassMap = new Map<string, string>([
	['Action Success', 'bg-green-50 dark:bg-green-900/20 border-green-400'],
	['ActionExecutionSuccess', 'bg-green-50 dark:bg-green-900/20 border-green-400'],
	['Action Failed', 'bg-red-50 dark:bg-red-900/20 border-red-400'],
	['ActionExecutionFailed', 'bg-red-50 dark:bg-red-900/20 border-red-400'],
	['Action Running', 'bg-blue-50 dark:bg-blue-900/20 border-blue-400'],
	['ActionExecutionRunning', 'bg-blue-50 dark:bg-blue-900/20 border-blue-400'],
	['Action Pending', 'bg-orange-50 dark:bg-orange-900/20 border-orange-400'],
	['ActionExecutionPending', 'bg-orange-50 dark:bg-orange-900/20 border-orange-400'],
	['Action Cancelled', 'bg-gray-50 dark:bg-gray-900/20 border-gray-400'],
	['ActionExecutionCancelled', 'bg-gray-50 dark:bg-gray-900/20 border-gray-400'],
	['StageActionExecution', 'bg-blue-50 dark:bg-blue-900/20 border-blue-400'],
	['TaskActionExecution', 'bg-green-50 dark:bg-green-900/20 border-green-400'],
	['QuestionActionExecution', 'bg-purple-50 dark:bg-purple-900/20 border-purple-400'],
]);

const actionStatusClassMap = new Map<string, string>([
	['Action Success', 'bg-green-100 text-green-800 dark:bg-green-800 dark:text-green-100'],
	['ActionExecutionSuccess', 'bg-green-100 text-green-800 dark:bg-green-800 dark:text-green-100'],
	['Action Failed', 'bg-red-100 text-red-800 dark:bg-red-800 dark:text-red-100'],
	['ActionExecutionFailed', 'bg-red-100 text-red-800 dark:bg-red-800 dark:text-red-100'],
	['Action Running', 'bg-blue-100 text-blue-800 dark:bg-blue-800 dark:text-blue-100'],
	['ActionExecutionRunning', 'bg-blue-100 text-blue-800 dark:bg-blue-800 dark:text-blue-100'],
	['Action Pending', 'bg-orange-100 text-orange-800 dark:bg-orange-800 dark:text-orange-100'],
	[
		'ActionExecutionPending',
		'bg-orange-100 text-orange-800 dark:bg-orange-800 dark:text-orange-100',
	],
	['Action Cancelled', 'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-100'],
	['ActionExecutionCancelled', 'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-100'],
	['StageActionExecution', 'bg-blue-100 text-blue-800 dark:bg-blue-800 dark:text-blue-100'],
	['TaskActionExecution', 'bg-green-100 text-green-800 dark:bg-green-800 dark:text-green-100'],
	[
		'QuestionActionExecution',
		'bg-purple-100 text-purple-800 dark:bg-purple-800 dark:text-purple-100',
	],
]);

const getActionExecutionBgClass = (type: string): string => {
	return actionBgClassMap.get(type) ?? 'bg-gray-50 dark:bg-gray-900/20 border-gray-400';
};

const getActionExecutionStatusClass = (type: string): string => {
	return (
		actionStatusClassMap.get(type) ??
		'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-100'
	);
};

const actionStatusTextMap = new Map<string, string>([
	['Action Success', 'Success'],
	['ActionExecutionSuccess', 'Success'],
	['Action Failed', 'Failed'],
	['ActionExecutionFailed', 'Failed'],
	['Action Running', 'Running'],
	['ActionExecutionRunning', 'Running'],
	['Action Pending', 'Pending'],
	['ActionExecutionPending', 'Pending'],
	['Action Cancelled', 'Cancelled'],
	['ActionExecutionCancelled', 'Cancelled'],
	['StageActionExecution', 'Stage Action'],
	['TaskActionExecution', 'Task Action'],
	['QuestionActionExecution', 'Question Action'],
]);

const getActionExecutionStatusText = (type: string): string => {
	return actionStatusTextMap.get(type) ?? '';
};

// 获取 Action 执行来源
const getActionSource = (type: string): string => {
	switch (type) {
		case 'StageActionExecution':
			return 'Stage';
		case 'TaskActionExecution':
			return 'Task';
		case 'QuestionActionExecution':
			return 'Question';
		default:
			return '';
	}
};

// 获取 Action 来源的样式类
const getActionSourceClass = (type: string): string => {
	switch (type) {
		case 'StageActionExecution':
			return 'bg-blue-100 text-blue-800 dark:bg-blue-800 dark:text-blue-100';
		case 'TaskActionExecution':
			return 'bg-green-100 text-green-800 dark:bg-green-800 dark:text-green-100';
		case 'QuestionActionExecution':
			return 'bg-purple-100 text-purple-800 dark:bg-purple-800 dark:text-purple-100';
		default:
			return 'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-100';
	}
};

// 获取执行状态的样式类
const executionStatusClassMap = new Map<string, string>([
	['success', 'bg-green-100 text-green-800 dark:bg-green-800 dark:text-green-100'],
	['completed', 'bg-green-100 text-green-800 dark:bg-green-800 dark:text-green-100'],
	['finished', 'bg-green-100 text-green-800 dark:bg-green-800 dark:text-green-100'],
	['failed', 'bg-red-100 text-red-800 dark:bg-red-800 dark:text-red-100'],
	['error', 'bg-red-100 text-red-800 dark:bg-red-800 dark:text-red-100'],
	['exception', 'bg-red-100 text-red-800 dark:bg-red-800 dark:text-red-100'],
	['running', 'bg-blue-100 text-blue-800 dark:bg-blue-800 dark:text-blue-100'],
	['executing', 'bg-blue-100 text-blue-800 dark:bg-blue-800 dark:text-blue-100'],
	['in_progress', 'bg-blue-100 text-blue-800 dark:bg-blue-800 dark:text-blue-100'],
	['pending', 'bg-orange-100 text-orange-800 dark:bg-orange-800 dark:text-orange-100'],
	['waiting', 'bg-orange-100 text-orange-800 dark:bg-orange-800 dark:text-orange-100'],
	['queued', 'bg-orange-100 text-orange-800 dark:bg-orange-800 dark:text-orange-100'],
	['cancelled', 'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-100'],
	['aborted', 'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-100'],
	['terminated', 'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-100'],
]);

const getExecutionStatusClass = (status: string): string => {
	if (!status) return 'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-100';
	return (
		executionStatusClassMap.get(status.toLowerCase()) ??
		'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-100'
	);
};

// 获取执行输出摘要
const getExecutionOutputSummary = (executionOutput: any): string => {
	if (!executionOutput) return '';

	try {
		const output =
			typeof executionOutput === 'string' ? JSON.parse(executionOutput) : executionOutput;
		const summaryParts: string[] = [];

		// 提取关键信息
		if (output.success !== undefined) {
			summaryParts.push(`Success: ${output.success}`);
		}

		if (output.status) {
			summaryParts.push(`Status: ${output.status}`);
		}

		if (output.message) {
			const message =
				output.message.length > 100
					? output.message.substring(0, 100) + '...'
					: output.message;
			summaryParts.push(`Message: ${message}`);
		}

		if (output.result !== undefined) {
			const result =
				typeof output.result === 'object'
					? JSON.stringify(output.result).substring(0, 100) + '...'
					: String(output.result);
			summaryParts.push(`Result: ${result}`);
		}

		return summaryParts.join(' | ');
	} catch (error) {
		// 如果解析失败，返回原始字符串的前100个字符
		const str = String(executionOutput);
		return str.length > 100 ? str.substring(0, 100) + '...' : str;
	}
};

// 获取执行输入摘要
const getExecutionInputSummary = (executionInput: any): string => {
	if (!executionInput) return '';

	try {
		const input =
			typeof executionInput === 'string' ? JSON.parse(executionInput) : executionInput;
		const summaryParts: string[] = [];

		// 提取关键输入参数
		Object.keys(input).forEach((key) => {
			if (summaryParts.length < 3) {
				// 最多显示3个参数
				const value = input[key];
				const valueStr =
					typeof value === 'object'
						? JSON.stringify(value).substring(0, 50) + '...'
						: String(value);
				summaryParts.push(`${key}: ${valueStr}`);
			}
		});

		return summaryParts.join(' | ');
	} catch (error) {
		// 如果解析失败，返回原始字符串的前100个字符
		const str = String(executionInput);
		return str.length > 100 ? str.substring(0, 100) + '...' : str;
	}
};

// 监听属性变化，只在展开状态下才重新加载数据
watch(
	() => [props.onboardingId, props.stageId],
	(newValues) => {
		const [newOnboardingId, newStageId] = newValues || [];
		if (newOnboardingId && newStageId) {
			// 重置分页到第一页
			currentPage.value = 1;
			// 清空现有数据
			changes.value = [];
			processedChanges.value = [];
			total.value = 0;
			// 只有在展开状态下才自动加载
			if (isExpanded.value) {
				loadChangeLogs();
			}
		}
	},
	{ immediate: false }
);

// 切换展开状态
const toggleExpanded = () => {
	isExpanded.value = !isExpanded.value;
	// 如果是第一次展开且没有数据，自动加载
	if (isExpanded.value && processedChanges.value.length === 0) {
		loadChangeLogs();
	}
};

defineExpose({
	loadChangeLogs,
});
</script>

<style scoped lang="scss">
/* 头部卡片样式 - 灰蓝色渐变 */
.change-log-header-card {
	background: linear-gradient(135deg, #64748b 0%, #475569 100%);
	padding: 12px 16px;
	color: white;
	box-shadow: 0 4px 12px rgba(100, 116, 139, 0.2);
	display: flex;
	flex-direction: column;
	gap: 16px;
	cursor: pointer;
	transition: all 0.2s ease;

	&:hover {
		box-shadow: 0 6px 16px rgba(100, 116, 139, 0.3);
		transform: translateY(-1px);
	}

	&.expanded {
		border-bottom-left-radius: 0;
		border-bottom-right-radius: 0;
		background: linear-gradient(135deg, #475569 0%, #334155 100%);
	}
}

.change-log-title {
	font-size: 14px;
	font-weight: 600;
	margin: 0;
	color: white;
}

.change-log-subtitle {
	font-size: 14px;
	margin: 4px 0 0 0;
	color: rgba(255, 255, 255, 0.9);
	font-weight: 400;
}

.change-log-actions {
	display: flex;
	align-items: center;
}

.refresh-button {
	background-color: rgba(255, 255, 255, 0.2);
	border-color: rgba(255, 255, 255, 0.3);
	color: white;

	&:hover {
		background-color: rgba(255, 255, 255, 0.3);
		border-color: rgba(255, 255, 255, 0.4);
	}

	&:disabled {
		background-color: rgba(255, 255, 255, 0.1);
		border-color: rgba(255, 255, 255, 0.2);
		color: rgba(255, 255, 255, 0.6);
	}
}

.expand-icon {
	transition: transform 0.2s ease;
	color: white;

	&.rotated {
		transform: rotate(90deg);
	}
}

/* 优化折叠动画 */
:deep(.el-collapse-transition) {
	transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1) !important;
}

:deep(.el-collapse-transition .el-collapse-item__content) {
	will-change: height;
	transform: translateZ(0); /* 启用硬件加速 */
}

// 暗色主题支持
.dark {
	.change-log-header-card {
		background: linear-gradient(135deg, #475569 0%, #334155 100%);
		box-shadow: 0 4px 12px rgba(71, 85, 105, 0.3);

		&:hover {
			box-shadow: 0 6px 16px rgba(71, 85, 105, 0.4);
		}

		&.expanded {
			background: linear-gradient(135deg, #334155 0%, #1e293b 100%);
		}
	}
}

/* 响应式设计 */
@media (max-width: 768px) {
	.change-log-header-card {
		padding: 12px 16px;

		.change-log-actions {
			margin-top: 8px;
		}
	}

	.change-log-header-card .flex {
		flex-direction: column;
		align-items: flex-start;
	}
}
</style>
