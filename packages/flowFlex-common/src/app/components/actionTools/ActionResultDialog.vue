<template>
	<el-dialog
		v-model="dialogVisible"
		:title="dialogTitle"
		:width="moreDialogWidth"
		draggable
		:before-close="handleClose"
		:append-to-body="true"
		:destroy-on-close="true"
		:close-on-click-modal="false"
		class="action-result-dialog"
	>
		<div class="action-result-content p-2">
			<!-- Loading State -->
			<!-- Results Table -->
			<el-table
				:data="results"
				v-loading="loading"
				class="w-full"
				max-height="500"
				border
				:expand-row-keys="Array.from(expandedRows)"
				row-key="executionId"
			>
				<template #empty>
					<div class="empty-state-container">
						<div class="empty-state-content">
							<!-- Icon -->
							<div class="empty-state-icon">
								<el-icon class="text-6xl empty-icon-color">
									<Document />
								</el-icon>
							</div>

							<!-- Title -->
							<h3 class="empty-state-title">
								{{ getEmptyStateTitle() }}
							</h3>

							<!-- Description -->
							<p class="empty-state-description">
								{{ getEmptyStateDescription() }}
							</p>
						</div>
					</div>
				</template>
				<!-- Expand Column -->
				<el-table-column type="expand" width="40" fixed="left">
					<template #default="{ row }">
						<div>
							<div v-if="row.executionInput || row.executionOutput" class="flex">
								<!-- Input Details -->
								<div v-if="row.executionInput" class="flex-1 shrink-0 min-w-0 pr-4">
									<div class="p-0">
										<!-- 智能渲染输入数据 -->
										<template v-if="row.inputType">
											<FileResultRenderer
												v-if="row.inputType.type === 'file'"
												:output-type="row.inputType"
												@preview="handleFilePreview"
												@download="handleFileDownload"
											/>
											<JsonResultRenderer
												v-else-if="row.inputType.type === 'json'"
												:output-type="row.inputType"
												:max-height="'300px'"
												@copy="handleJsonCopy"
												@format="handleJsonFormat"
											>
												<template #label>
													<div class="flex items-center space-x-2">
														<div
															class="w-2 h-2 section-indicator rounded-full"
														></div>
														<h4
															class="text-sm font-semibold section-title m-0"
														>
															Input
														</h4>
													</div>
												</template>
											</JsonResultRenderer>
											<div v-else class="text-renderer">
												<el-scrollbar max-height="300px">
													<pre
														class="font-mono text-xs leading-relaxed code-text-color p-4 m-0 whitespace-pre-wrap break-words"
														>{{ row.inputType.data }}</pre
													>
												</el-scrollbar>
											</div>
										</template>
										<!-- 降级方案：使用原有格式化 -->
										<template v-else>
											<el-scrollbar max-height="300px">
												<pre
													class="font-mono text-xs leading-relaxed code-text-color p-4 m-0 whitespace-pre-wrap break-words"
													>{{ formatJsonOutput(row.executionInput) }}</pre
												>
											</el-scrollbar>
										</template>
									</div>
								</div>
								<div
									class="w-px border-l border-dashed divider-border-color my-4"
								></div>
								<div
									v-if="row.executionOutput"
									class="flex-1 shrink-0 min-w-0 pr-4"
								>
									<div class="p-0">
										<!-- 智能渲染输出数据 -->
										<template v-if="row.outputType">
											<FileResultRenderer
												v-if="row.outputType.type === 'file'"
												:output-type="row.outputType"
												@preview="handleFilePreview"
												@download="handleFileDownload"
											>
												<template #label>
													<div class="flex items-center space-x-2">
														<div
															class="w-2 h-2 bg-green-500 rounded-full"
														></div>
														<h4
															class="text-sm font-semibold section-title m-0"
														>
															Output
														</h4>
													</div>
												</template>
											</FileResultRenderer>
											<JsonResultRenderer
												v-else-if="row.outputType.type === 'json'"
												:output-type="row.outputType"
												:max-height="'300px'"
												@copy="handleJsonCopy"
												@format="handleJsonFormat"
											>
												<template #label>
													<div class="flex items-center space-x-2">
														<div
															class="w-2 h-2 bg-green-500 rounded-full"
														></div>
														<h4
															class="text-sm font-semibold section-title m-0"
														>
															Output
														</h4>
													</div>
												</template>
											</JsonResultRenderer>
											<div v-else class="text-renderer">
												<el-scrollbar max-height="300px">
													<pre
														class="font-mono text-xs leading-relaxed code-text-color p-4 m-0 whitespace-pre-wrap break-words"
														>{{ row.outputType.data }}</pre
													>
												</el-scrollbar>
											</div>
										</template>
										<!-- 降级方案：使用原有格式化 -->
										<template v-else>
											<el-scrollbar max-height="300px">
												<pre
													class="font-mono text-xs leading-relaxed code-text-color border-0 rounded-none p-4 m-0 whitespace-pre-wrap break-words"
													>{{
														formatJsonOutput(row.executionOutput)
													}}</pre
												>
											</el-scrollbar>
										</template>
									</div>
								</div>
							</div>

							<!-- Error Details -->
							<div v-if="row.errorMessage" class="p-2">
								<div
									class="flex items-center gap-x-2 text-sm text-red-700 dark:text-red-300 bg-red-50 dark:bg-red-900/20 p-3 rounded-xl border border-red-200 dark:border-red-800"
								>
									<h4 class="text-sm font-medium text-red-600 dark:text-red-400">
										Error Message:
									</h4>
									{{ row.errorMessage }}
								</div>
							</div>

							<!-- Stack Trace -->
							<div v-if="row.errorStackTrace" class="execution-detail-section">
								<h4 class="text-sm font-medium text-red-600 dark:text-red-400 mb-2">
									Stack Trace
								</h4>
								<el-scrollbar max-height="128px">
									<pre
										class="execution-code-block text-red-700 dark:text-red-300 bg-red-50 dark:bg-red-900/20 border-red-200 dark:border-red-800"
										>{{ row.errorStackTrace }}</pre
									>
								</el-scrollbar>
							</div>

							<!-- Empty State -->
							<div
								v-if="
									!row.executionInput &&
									!row.executionOutput &&
									!row.errorMessage &&
									!row.errorStackTrace
								"
								class="text-center py-8"
							>
								<div class="empty-text-color text-sm">
									No execution details available
								</div>
							</div>
						</div>
					</template>
				</el-table-column>
				<el-table-column prop="actionCode" label="Action Code" width="120" fixed="left">
					<template #default="{ row }">
						<span class="font-mono text-sm">{{ row.actionCode }}</span>
					</template>
				</el-table-column>

				<el-table-column prop="actionName" label="Action Name" min-width="200" fixed="left">
					<template #default="{ row }">
						<span class="font-medium">{{ row.actionName }}</span>
					</template>
				</el-table-column>

				<el-table-column prop="actionType" label="Type" width="100">
					<template #default="{ row }">
						<el-tag class="type-tag">
							{{ row.actionType }}
						</el-tag>
					</template>
				</el-table-column>

				<el-table-column prop="executionStatus" label="Status" width="120">
					<template #default="{ row }">
						<el-tag
							:class="getStatusClass(row.executionStatus)"
							:type="undefined"
							class="status-tag"
						>
							{{ getStatusText(row.executionStatus) }}
						</el-tag>
					</template>
				</el-table-column>

				<el-table-column prop="startedAt" label="Started At" width="160">
					<template #default="{ row }">
						{{ timeZoneConvert(row.startedAt, false, projectTenMinutesSsecondsDate) }}
					</template>
				</el-table-column>

				<el-table-column prop="completedAt" label="Completed At" width="160">
					<template #default="{ row }">
						{{ timeZoneConvert(row.completedAt, false, projectTenMinutesSsecondsDate) }}
					</template>
				</el-table-column>

				<el-table-column prop="duration" label="Duration" width="100">
					<template #default="{ row }">
						{{ calculateDuration(row.startedAt, row.completedAt) }}
					</template>
				</el-table-column>
			</el-table>

			<!-- Pagination -->
			<CustomerPagination
				:total="total"
				:limit="pageSize"
				:page="currentPage"
				:background="true"
				@pagination="handlePagination"
				@update:page="handleCurrentChange"
				@update:limit="handlePageUpdate"
			/>
		</div>
	</el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { ElMessage } from 'element-plus';
import { Document } from '@element-plus/icons-vue';
import CustomerPagination from '@/components/global/u-pagination/index.vue';
import { projectTenMinutesSsecondsDate, moreDialogWidth } from '@/settings/projectSetting';
import { getActionResult } from '@/apis/action';
import { timeZoneConvert } from '@/hooks/time';
import type { ActionExecutionResult } from '#/action';
// 新增导入
import { detectOutputType, type OutputType } from '@/utils/output-type-detector';
import FileResultRenderer from './FileResultRenderer.vue';
import JsonResultRenderer from './JsonResultRenderer.vue';

interface ActionInfo {
	id: string;
	name?: string;
	actionName?: string;
}

// 扩展ActionExecutionResult接口
interface EnhancedActionExecutionResult extends ActionExecutionResult {
	formattedOutput?: string;
	outputType?: OutputType;
	inputType?: OutputType;
}

interface Props {
	modelValue: boolean;
	// 单个 action 模式的参数
	action?: ActionInfo;
	triggerSourceId?: string;
	// 多个 actions 模式的参数
	actions?: ActionInfo[];
	actionName: string;
	onboardingId: string;
}

const props = defineProps<Props>();

const emits = defineEmits<{
	'update:modelValue': [value: boolean];
}>();

// Dialog state
const dialogVisible = computed({
	get: () => props.modelValue,
	set: (value) => emits('update:modelValue', value),
});

const dialogTitle = computed(() => {
	return `Action Execution Records - ${props.actionName}`;
});

// Data state
const loading = ref(false);
const results = ref<EnhancedActionExecutionResult[]>([]);
const expandedRows = ref(new Set<string>());

// Pagination
const currentPage = ref(1);
const pageSize = ref(20);
const total = ref(0);

// Multiple actions mode (for display purposes only)
const isMultipleActionsMode = computed(() => {
	return props.actions && props.actions.length > 0;
});

// Utility functions
const calculateDuration = (startedAt: string, completedAt: string): string => {
	if (!startedAt || !completedAt) return 'N/A';

	try {
		const startTime = new Date(startedAt).getTime();
		const endTime = new Date(completedAt).getTime();
		const durationMs = endTime - startTime;

		if (durationMs < 0) return 'N/A';

		return formatDuration(durationMs);
	} catch (error) {
		console.error('Duration calculation error:', error);
		return 'N/A';
	}
};

const formatDuration = (durationMs: number): string => {
	if (!durationMs) return '';
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

const getExecutionOutputSummary = (executionOutput: any): string => {
	if (!executionOutput) return '';

	try {
		const output =
			typeof executionOutput === 'string' ? JSON.parse(executionOutput) : executionOutput;
		const summaryParts: string[] = [];

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
		const str = String(executionOutput);
		return str.length > 100 ? str.substring(0, 100) + '...' : str;
	}
};

const formatJsonOutput = (data: any): string => {
	try {
		const parsed = typeof data === 'string' ? JSON.parse(data) : data;
		return JSON.stringify(parsed, null, 2);
	} catch (error) {
		return String(data);
	}
};

const getStatusClass = (status: string): string => {
	if (!status) return 'status-tag status-default';

	const statusMap = new Map<string, string>([
		['completed', 'status-tag status-success'],
		['success', 'status-tag status-success'],
		['failed', 'status-tag status-danger'],
		['error', 'status-tag status-danger'],
		['pending', 'status-tag status-warning'],
		['running', 'status-tag status-warning'],
		['executing', 'status-tag status-warning'],
		['waiting', 'status-tag status-warning'],
	]);

	return statusMap.get(status.toLowerCase()) ?? 'status-tag status-default';
};

const getStatusText = (status: string): string => {
	if (!status) return 'Unknown';

	const statusTextMap = new Map<string, string>([
		['completed', 'Completed'],
		['success', 'Success'],
		['failed', 'Failed'],
		['error', 'Error'],
		['pending', 'Pending'],
		['running', 'Running'],
		['executing', 'Executing'],
		['waiting', 'Waiting'],
	]);

	return statusTextMap.get(status.toLowerCase()) ?? status;
};

// Empty state methods
const getEmptyStateTitle = (): string => {
	return 'No Execution Records Found';
};

const getEmptyStateDescription = (): string => {
	if (isMultipleActionsMode.value) {
		return 'No execution records found for these actions. They may not have been executed yet.';
	}
	return 'This action has not been executed yet, or no execution records match the current criteria.';
};

// Build JSON conditions for API
const buildJsonConditions = () => {
	const conditions: any[] = [];

	// Always add onboardingId condition
	if (props.onboardingId) {
		conditions.push({
			jsonPath: 'OnboardingId',
			operator: '=',
			value: props.onboardingId,
		});
	}

	return conditions;
};

// Load action execution results
const loadResults = async () => {
	// 验证必要参数
	if (!props.onboardingId || !props.triggerSourceId) {
		console.warn('Missing required parameters for loading action results');
		return;
	}

	loading.value = true;

	try {
		const conditions = buildJsonConditions();

		// 统一使用 triggerSourceId 查询
		const response = await getActionResult(props.triggerSourceId, {
			pageIndex: currentPage.value,
			pageSize: pageSize.value,
			jsonConditions: conditions,
		});

		if (response.success) {
			const rawData =
				response.data?.data?.filter((item) => {
					return item.actionDefinitionId === props.action?.id;
				}) || [];

			// Process and enhance the data with type detection
			results.value = rawData.map((item: any) => {
				const enhanced: EnhancedActionExecutionResult = {
					...item,
					// Format output summary for display
					formattedOutput: getExecutionOutputSummary(item.executionOutput),
				};

				// Detect output type if executionOutput exists
				if (item.executionOutput) {
					try {
						enhanced.outputType = detectOutputType(item.executionOutput);
					} catch (error) {
						console.warn('Failed to detect output type:', error);
						// Fallback to text type
						enhanced.outputType = {
							type: 'text',
							data: item.executionOutput,
						};
					}
				}

				// Detect input type if executionInput exists
				if (item.executionInput) {
					try {
						enhanced.inputType = detectOutputType(item.executionInput);
					} catch (error) {
						console.warn('Failed to detect input type:', error);
						// Fallback to text type
						enhanced.inputType = {
							type: 'text',
							data: item.executionInput,
						};
					}
				}

				return enhanced;
			});
			total.value = response.data?.total || 0;
		} else {
			ElMessage.error(response.msg || 'Failed to load action execution results');
			results.value = [];
			total.value = 0;
		}
	} catch (err) {
		console.error('Load action results error:', err);
		results.value = [];
		total.value = 0;
	} finally {
		loading.value = false;
	}
};

// Event handlers
const handleClose = () => {
	dialogVisible.value = false;
	expandedRows.value.clear();
};

const handlePageUpdate = (size: number) => {
	// 切换页面大小时关闭所有展开的详情
	expandedRows.value.clear();
	pageSize.value = size;
	currentPage.value = 1;
};

const handleCurrentChange = (page: number) => {
	// 切换页码时关闭所有展开的详情
	expandedRows.value.clear();
	currentPage.value = page;
};

const handlePagination = () => {
	// 统一的分页处理函数，只负责调用 API
	loadResults();
};

// 新增事件处理函数
const handleFilePreview = (outputType: OutputType) => {
	console.log('File preview requested:', outputType);
	// 文件预览逻辑已在FileResultRenderer组件内部处理
};

const handleFileDownload = (outputType: OutputType) => {
	console.log('File download requested:', outputType);
	// 文件下载逻辑已在FileResultRenderer组件内部处理
};

const handleJsonCopy = (outputType: OutputType) => {
	console.log('JSON copy requested:', outputType);
	// JSON复制逻辑已在JsonResultRenderer组件内部处理
};

const handleJsonFormat = (outputType: OutputType) => {
	console.log('JSON format requested:', outputType);
	// JSON格式化逻辑已在JsonResultRenderer组件内部处理
};

// Watch for dialog visibility changes
watch(
	() => props.modelValue,
	(visible) => {
		if (visible && props.onboardingId && props.triggerSourceId) {
			expandedRows.value.clear();
			currentPage.value = 1;
			loadResults();
		}
	}
);
</script>

<style scoped lang="scss">
/* 新增CSS类定义 */
.empty-icon-color {
	color: var(--el-text-color-placeholder);
}

html.dark .empty-icon-color {
	color: var(--el-border-color);
}

.section-indicator {
	background-color: var(--el-color-primary);
}

.section-title {
	color: var(--el-text-color-regular);
}

.code-text-color {
	color: var(--el-text-color-primary);
}

.divider-border-color {
	border-color: var(--el-border-color-light);
}

html.dark .divider-border-color {
	border-color: var(--el-border-color);
}

.empty-text-color {
	color: var(--el-text-color-secondary);
}

/* 状态标签样式 */
.status-tag {
	@apply inline-flex items-center px-2 py-1 rounded text-xs font-medium;
}

.status-success {
	background-color: var(--el-color-success-light-9) !important;
	color: var(--el-color-success-dark-2) !important;
}

html.dark .status-success {
	background-color: var(--el-color-success-dark-2) !important;
	color: var(--el-color-success-light-9) !important;
}

.status-danger {
	background-color: var(--el-color-danger-light-9) !important;
	color: var(--el-color-danger-dark-2) !important;
}

html.dark .status-danger {
	background-color: var(--el-color-danger-dark-2) !important;
	color: var(--el-color-danger-light-9) !important;
}

.status-warning {
	background-color: var(--el-color-warning-light-9) !important;
	color: var(--el-color-warning-dark-2) !important;
}

html.dark .status-warning {
	background-color: var(--el-color-warning-dark-2) !important;
	color: var(--el-color-warning-light-9) !important;
}

.status-default {
	background-color: var(--el-fill-color-light) !important;
	color: var(--el-text-color-regular) !important;
}

html.dark .status-default {
	background-color: var(--el-fill-color) !important;
	color: var(--el-text-color-primary) !important;
}

.action-result-dialog {
	:deep(.el-dialog__body) {
		padding: 20px;
	}
}

.action-result-content {
	max-height: 70vh;
	overflow-y: auto;
}

/* Empty State Styles */
.empty-state-container {
	display: flex;
	align-items: center;
	justify-content: center;
	min-height: 300px;
	padding: 40px 20px;
}

.empty-state-content {
	text-align: center;
	max-width: 400px;
}

.empty-state-icon {
	margin-bottom: 24px;
	display: flex;
	justify-content: center;
}

.empty-state-title {
	font-size: 18px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	margin: 0 0 12px 0;
	line-height: 1.4;
}

.empty-state-description {
	font-size: 14px;
	color: var(--el-text-color-regular);
	margin: 0 0 24px 0;
	line-height: 1.6;
}

.empty-state-suggestion {
	display: flex;
	justify-content: center;
}

/* Type tag styles */
.type-tag {
	background-color: var(--el-color-primary-light-9) !important;
	border-color: var(--el-color-primary-light-7) !important;
	color: var(--el-color-primary) !important;
	padding: 4px 12px !important;
	font-size: 12px !important;
	font-weight: 500 !important;
}

/* Status tag styles - 覆盖 el-tag 默认样式 */
.status-tag {
	border: none !important;
	padding: 4px 8px !important;
	font-size: 12px !important;
	font-weight: 500 !important;
}

.execution-code-block {
	@apply rounded-xl;
	font-size: 12px;
	line-height: 1.5;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-light);
	padding: 12px;
	margin: 0;
	overflow-x: auto;
	max-height: 200px;
	overflow-y: auto;
	white-space: pre-wrap;
	word-wrap: break-word;
}

/* Dark mode support */
html.dark {
	.empty-state-title {
		color: var(--white-100);
	}

	.empty-state-description {
		color: var(--gray-300);
	}

	.execution-code-block {
		background: var(--el-bg-color-page);
		border-color: var(--el-border-color);
		color: var(--el-text-color-primary);
	}
}

/* 新增渲染组件样式 */
.text-renderer {
	@apply rounded-xl;
	background: var(--el-bg-color);
}

html.dark .text-renderer {
	background: var(--el-bg-color-page);
}

.text-renderer pre {
	@apply bg-transparent border-0 rounded-none;
}

/* 确保新组件与现有样式一致 */
:deep(.file-result-renderer),
:deep(.json-result-renderer) {
	@apply w-full;
}

/* 调整新组件在展开面板中的间距 */
:deep(.file-result-renderer .file-info-card),
:deep(.json-result-renderer .json-toolbar) {
	@apply mb-0;
}
</style>
