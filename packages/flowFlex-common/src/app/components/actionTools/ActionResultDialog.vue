<template>
	<el-dialog
		v-model="dialogVisible"
		:title="dialogTitle"
		:width="bigDialogWidth"
		draggable
		:before-close="handleClose"
		:append-to-body="true"
		:destroy-on-close="true"
		:close-on-click-modal="false"
		class="action-result-dialog"
	>
		<div class="action-result-content p-2">
			<!-- Filter Section -->
			<div class="mb-4 flex items-center gap-4">
				<!-- Action Selector (for multiple actions) -->
				<el-select
					v-if="isMultipleActionsMode"
					v-model="selectedActionId"
					placeholder="Select Action"
					class="w-48"
					@change="handleActionChange"
				>
					<el-option
						v-for="actionItem in props.actions"
						:key="actionItem.id"
						:label="
							actionItem.name || actionItem.actionName || `Action ${actionItem.id}`
						"
						:value="actionItem.id"
					/>
				</el-select>
			</div>

			<!-- Loading State -->
			<!-- Results Table -->
			<el-table :data="results" v-loading="loading" class="w-full">
				<!-- Custom Empty State -->
				<template #empty>
					<div class="empty-state-container">
						<div class="empty-state-content">
							<!-- Icon -->
							<div class="empty-state-icon">
								<el-icon class="text-6xl text-gray-300 dark:text-gray-600">
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

							<!-- Action Suggestion -->
							<div
								v-if="isMultipleActionsMode && !selectedActionId"
								class="empty-state-suggestion"
							>
								<el-button
									type="primary"
									size="small"
									@click="handleEmptyStateAction"
									:icon="RefreshRight"
								/>
							</div>
						</div>
					</div>
				</template>
				<el-table-column prop="actionCode" label="Action Code" width="120">
					<template #default="{ row }">
						<span class="font-mono text-sm">{{ row.actionCode }}</span>
					</template>
				</el-table-column>

				<el-table-column prop="actionName" label="Action Name" min-width="200">
					<template #default="{ row }">
						<span class="font-medium">{{ row.actionName }}</span>
					</template>
				</el-table-column>

				<el-table-column prop="actionType" label="Type" width="100">
					<template #default="{ row }">
						<el-tag size="small" class="type-tag">
							{{ row.actionType }}
						</el-tag>
					</template>
				</el-table-column>

				<el-table-column prop="executionStatus" label="Status" width="120">
					<template #default="{ row }">
						<el-tag :class="getStatusClass(row.executionStatus)" size="small">
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

				<el-table-column label="Actions" width="80" fixed="right">
					<template #default="{ row }">
						<el-tooltip
							:content="
								expandedRows.has(row.executionId) ? 'Hide Details' : 'View Details'
							"
							placement="top"
						>
							<el-button
								size="small"
								type="primary"
								link
								@click="toggleDetails(row.executionId)"
								:icon="expandedRows.has(row.executionId) ? ArrowUp : View"
							/>
						</el-tooltip>
					</template>
				</el-table-column>
			</el-table>

			<!-- Expanded Details -->
			<div v-for="row in results" :key="row.executionId">
				<div
					v-if="expandedRows.has(row.executionId)"
					class="mt-3 p-3 bg-gray-50 dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700"
				>
					<!-- 只显示执行内容，不重复表格中的信息 -->
					<div class="space-y-3">
						<!-- Input Details -->
						<div v-if="row.executionInput">
							<div class="text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
								Execution Input
							</div>
							<pre
								class="text-xs bg-white dark:bg-gray-900 p-3 rounded border border-gray-200 dark:border-gray-600 overflow-y-auto font-mono"
								>{{ formatJsonOutput(row.executionInput) }}</pre
							>
						</div>

						<!-- Output Details -->
						<div v-if="row.executionOutput">
							<div class="text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
								Execution Output
							</div>
							<pre
								class="text-xs bg-white dark:bg-gray-900 p-3 rounded border border-gray-200 dark:border-gray-600 overflow-y-auto font-mono"
								>{{ formatJsonOutput(row.executionOutput) }}</pre
							>
						</div>

						<!-- Error Details -->
						<div v-if="row.errorMessage">
							<div class="text-xs font-medium text-red-500 mb-1">Error Message</div>
							<div
								class="text-xs text-red-600 bg-red-50 dark:bg-red-900/20 p-3 rounded border border-red-200 dark:border-red-800 leading-relaxed"
							>
								{{ row.errorMessage }}
							</div>
						</div>

						<!-- Stack Trace -->
						<div v-if="row.errorStackTrace">
							<div class="text-xs font-medium text-red-500 mb-1">Stack Trace</div>
							<pre
								class="text-xs text-red-600 bg-red-50 dark:bg-red-900/20 p-3 rounded border border-red-200 dark:border-red-800 max-h-24 overflow-y-auto font-mono leading-relaxed"
								>{{ row.errorStackTrace }}</pre
							>
						</div>

						<!-- 如果没有任何执行内容，显示提示 -->
						<div
							v-if="
								!row.executionInput &&
								!row.executionOutput &&
								!row.errorMessage &&
								!row.errorStackTrace
							"
							class="text-center py-4"
						>
							<div class="text-gray-400 dark:text-gray-500 text-xs">
								No execution details available
							</div>
						</div>
					</div>
				</div>
			</div>

			<!-- Pagination -->
			<CustomerPagination
				:total="total"
				:limit="pageSize"
				:page="currentPage"
				:background="true"
				@pagination="loadResults"
				@update:page="handleCurrentChange"
				@update:limit="handlePageUpdate"
			/>
		</div>
	</el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { ElMessage } from 'element-plus';
import { ArrowUp, RefreshRight, Document, View } from '@element-plus/icons-vue';
import CustomerPagination from '@/components/global/u-pagination/index.vue';
import { bigDialogWidth, projectTenMinutesSsecondsDate } from '@/settings/projectSetting';
import { getActionResult } from '@/apis/action';
import { timeZoneConvert } from '@/hooks/time';
import type { ActionExecutionResult } from '#/action';

interface ActionInfo {
	id: string;
	name?: string;
	actionName?: string;
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
const results = ref<ActionExecutionResult[]>([]);
const expandedRows = ref(new Set<string>());

// Pagination
const currentPage = ref(1);
const pageSize = ref(20);
const total = ref(0);

// Multiple actions mode
const selectedActionId = ref('');
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
	if (!status) return 'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-100';

	const statusMap = new Map<string, string>([
		['success', 'bg-green-100 text-green-800 dark:bg-green-800 dark:text-green-100'],
		['completed', 'bg-green-100 text-green-800 dark:bg-green-800 dark:text-green-100'],
		['failed', 'bg-red-100 text-red-800 dark:bg-red-800 dark:text-red-100'],
		['error', 'bg-red-100 text-red-800 dark:bg-red-800 dark:text-red-100'],
		['running', 'bg-blue-100 text-blue-800 dark:bg-blue-800 dark:text-blue-100'],
		['executing', 'bg-blue-100 text-blue-800 dark:bg-blue-800 dark:text-blue-100'],
		['pending', 'bg-orange-100 text-orange-800 dark:bg-orange-800 dark:text-orange-100'],
		['waiting', 'bg-orange-100 text-orange-800 dark:bg-orange-800 dark:text-orange-100'],
		['cancelled', 'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-100'],
		['aborted', 'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-100'],
	]);

	return (
		statusMap.get(status.toLowerCase()) ??
		'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-100'
	);
};

const getStatusText = (status: string): string => {
	if (!status) return 'Unknown';

	const statusTextMap = new Map<string, string>([
		['success', 'Success'],
		['completed', 'Completed'],
		['failed', 'Failed'],
		['error', 'Error'],
		['running', 'Running'],
		['executing', 'Executing'],
		['pending', 'Pending'],
		['waiting', 'Waiting'],
		['cancelled', 'Cancelled'],
		['aborted', 'Aborted'],
	]);

	return statusTextMap.get(status.toLowerCase()) ?? status;
};

// Empty state methods
const getEmptyStateTitle = (): string => {
	if (isMultipleActionsMode.value && !selectedActionId.value) {
		return 'Select an Action';
	}
	return 'No Execution Records Found';
};

const getEmptyStateDescription = (): string => {
	if (isMultipleActionsMode.value && !selectedActionId.value) {
		return 'Please select an action from the dropdown above to view its execution records.';
	}

	if (isMultipleActionsMode.value) {
		const selectedAction = props.actions?.find(
			(action) => action.id === selectedActionId.value
		);
		const actionName = selectedAction?.name || selectedAction?.actionName || 'this action';
		return `No execution records found for ${actionName}. This action may not have been executed yet.`;
	}

	return 'This action has not been executed yet, or no execution records match the current criteria.';
};

const handleEmptyStateAction = () => {
	loadResults();
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
	if (!props.onboardingId) {
		console.warn('Missing onboardingId for loading action results');
		return;
	}

	// 根据模式验证参数
	if (isMultipleActionsMode.value) {
		if (!props.actions || props.actions.length === 0) {
			console.warn('Missing actions array for multiple actions mode');
			return;
		}
	} else {
		if (!props.triggerSourceId) {
			console.warn('Missing triggerSourceId for single action mode');
			return;
		}
	}

	loading.value = true;

	try {
		let response;

		const conditions = buildJsonConditions();

		if (isMultipleActionsMode.value) {
			// 多个 actions 模式：使用选中的 action ID 查询
			const targetActionId = selectedActionId.value;
			if (!targetActionId) {
				console.warn('No action selected in multiple actions mode');
				return;
			}
			response = await getActionResult(targetActionId, {
				pageIndex: currentPage.value,
				pageSize: pageSize.value,
				jsonConditions: conditions,
			});
		} else {
			// 单个 action 模式：使用 triggerSourceId 查询
			response = await getActionResult(props.triggerSourceId!, {
				pageIndex: currentPage.value,
				pageSize: pageSize.value,
				jsonConditions: conditions,
			});
		}

		if (response.success) {
			const rawData = response.data?.data || [];

			// Process and enhance the data
			results.value = rawData.map((item: any) => ({
				...item,
				// Format output summary for display
				formattedOutput: getExecutionOutputSummary(item.executionOutput),
			}));
			total.value = response.data?.total || 0;
		} else {
			ElMessage.error(response.msg || 'Failed to load action execution results');
			results.value = [];
			total.value = 0;
		}
	} catch (err) {
		console.error('Load action results error:', err);
		ElMessage.error(
			err instanceof Error ? err.message : 'Failed to load action execution results'
		);
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

const toggleDetails = (executionId: string) => {
	if (expandedRows.value.has(executionId)) {
		// 如果当前行已展开，则关闭它
		expandedRows.value.delete(executionId);
	} else {
		// 关闭所有其他展开的行，只展开当前行
		expandedRows.value.clear();
		expandedRows.value.add(executionId);
	}
};

const handleActionChange = () => {
	// 切换 action 时关闭所有展开的详情
	expandedRows.value.clear();
	currentPage.value = 1;
	loadResults();
};

const handlePageUpdate = (size: number) => {
	// 切换页面大小时关闭所有展开的详情
	expandedRows.value.clear();
	pageSize.value = size;
	currentPage.value = 1;
	loadResults();
};

const handleCurrentChange = (page: number) => {
	// 切换页码时关闭所有展开的详情
	expandedRows.value.clear();
	currentPage.value = page;
	loadResults();
};

// Watch for dialog visibility changes
watch(
	() => props.modelValue,
	(visible) => {
		if (visible && props.onboardingId) {
			// 检查是否有必要的参数
			const hasRequiredParams = isMultipleActionsMode.value
				? props.actions && props.actions.length > 0
				: props.triggerSourceId;

			if (hasRequiredParams) {
				expandedRows.value.clear();
				currentPage.value = 1;
				// 多个 actions 模式时默认选择第一个 action
				if (isMultipleActionsMode.value && props.actions && props.actions.length > 0) {
					selectedActionId.value = props.actions[0].id;
				}
				loadResults();
			}
		}
	}
);
</script>

<style scoped lang="scss">
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
	background-color: #e6f3ff !important;
	border-color: #b3d9ff !important;
	color: #2468f2 !important;
	border-radius: 16px !important;
	padding: 4px 12px !important;
	font-size: 12px !important;
	font-weight: 500 !important;
}

/* Dark mode support */
html.dark {
	.empty-state-title {
		color: var(--white-100);
	}

	.empty-state-description {
		color: var(--gray-300);
	}
}
</style>
