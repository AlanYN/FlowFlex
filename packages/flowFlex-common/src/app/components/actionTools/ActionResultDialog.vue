<template>
	<el-dialog
		v-model="dialogVisible"
		:title="dialogTitle"
		width="80%"
		:before-close="handleClose"
		:append-to-body="true"
		:destroy-on-close="true"
		:close-on-click-modal="false"
		class="action-result-dialog"
	>
		<div class="action-result-content">
			<!-- Filter Section -->
			<div class="mb-4 flex items-center gap-4">
				<el-select
					v-model="localFilters.status"
					placeholder="Status Filter"
					clearable
					class="w-40"
					@change="handleFilterChange"
				>
					<el-option label="Success" value="success" />
					<el-option label="Failed" value="failed" />
					<el-option label="Running" value="running" />
					<el-option label="Pending" value="pending" />
				</el-select>

				<el-button @click="resetFilters" :icon="RefreshRight">Reset</el-button>
			</div>

			<!-- Loading State -->
			<div v-if="loading" class="flex justify-center items-center py-8">
				<el-icon class="is-loading text-2xl text-primary mr-2">
					<Loading />
				</el-icon>
				<span>Loading execution records...</span>
			</div>

			<!-- Results Table -->
			<el-table
				v-else
				:data="results"
				v-loading="loading"
				class="w-full"
				:empty-text="'No execution records'"
			>
				<el-table-column prop="executionId" label="Execution ID" width="200">
					<template #default="{ row }">
						<span class="font-mono text-sm">{{ row.executionId }}</span>
					</template>
				</el-table-column>

				<el-table-column prop="status" label="Status" width="120">
					<template #default="{ row }">
						<el-tag :class="getStatusClass(row.status)" size="small">
							{{ getStatusText(row.status) }}
						</el-tag>
					</template>
				</el-table-column>

				<el-table-column prop="startedAt" label="Started At" width="180">
					<template #default="{ row }">
						{{ formatDateTime(row.startedAt) }}
					</template>
				</el-table-column>

				<el-table-column prop="duration" label="Duration" width="100">
					<template #default="{ row }">
						{{ formatDuration(row.duration || 0) }}
					</template>
				</el-table-column>

				<el-table-column prop="executionOutput" label="Output Summary" min-width="200">
					<template #default="{ row }">
						<div
							class="max-w-xs truncate"
							:title="getExecutionOutputSummary(row.executionOutput)"
						>
							{{ getExecutionOutputSummary(row.executionOutput) }}
						</div>
					</template>
				</el-table-column>

				<el-table-column label="Actions" width="120" fixed="right">
					<template #default="{ row }">
						<el-button
							size="small"
							@click="toggleDetails(row.executionId)"
							:icon="expandedRows.has(row.executionId) ? ArrowUp : ArrowDown"
						>
							{{
								expandedRows.has(row.executionId) ? 'Hide Details' : 'View Details'
							}}
						</el-button>
					</template>
				</el-table-column>
			</el-table>

			<!-- Expanded Details -->
			<div v-for="row in results" :key="row.executionId">
				<div
					v-if="expandedRows.has(row.executionId)"
					class="mt-4 p-4 bg-gray-50 dark:bg-gray-800 rounded-lg border"
				>
					<h4 class="font-semibold mb-3">Execution Details</h4>
					<div class="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
						<div>
							<label class="block text-sm font-medium mb-1">Action Name:</label>
							<p class="text-sm">{{ row.actionName }}</p>
						</div>
						<div>
							<label class="block text-sm font-medium mb-1">Action Type:</label>
							<p class="text-sm">{{ row.actionType }}</p>
						</div>
						<div>
							<label class="block text-sm font-medium mb-1">Trigger Source:</label>
							<p class="text-sm">{{ row.triggerSource }}</p>
						</div>
						<div>
							<label class="block text-sm font-medium mb-1">Completed At:</label>
							<p class="text-sm">{{ formatDateTime(row.completedAt || '') }}</p>
						</div>
					</div>

					<!-- Input Details -->
					<div v-if="row.executionInput" class="mb-4">
						<label class="block text-sm font-medium mb-2">Input:</label>
						<el-collapse>
							<el-collapse-item title="View Input Details" name="input">
								<pre
									class="text-xs bg-white dark:bg-gray-900 p-3 rounded border overflow-auto max-h-60"
									>{{ formatJsonOutput(row.executionInput) }}</pre
								>
							</el-collapse-item>
						</el-collapse>
					</div>

					<!-- Output Details -->
					<div v-if="row.executionOutput" class="mb-4">
						<label class="block text-sm font-medium mb-2">Output:</label>
						<el-collapse>
							<el-collapse-item title="View Output Details" name="output">
								<pre
									class="text-xs bg-white dark:bg-gray-900 p-3 rounded border overflow-auto max-h-60"
									>{{ formatJsonOutput(row.executionOutput) }}</pre
								>
							</el-collapse-item>
						</el-collapse>
					</div>

					<!-- Error Details -->
					<div v-if="row.errorMessage" class="mb-4">
						<label class="block text-sm font-medium mb-2 text-red-600">
							Error Message:
						</label>
						<div
							class="text-sm text-red-600 bg-red-50 dark:bg-red-900/20 p-3 rounded border"
						>
							{{ row.errorMessage }}
						</div>
					</div>

					<div v-if="row.errorStackTrace" class="mb-4">
						<label class="block text-sm font-medium mb-2 text-red-600">
							Stack Trace:
						</label>
						<el-collapse>
							<el-collapse-item title="View Stack Trace" name="stackTrace">
								<pre
									class="text-xs text-red-600 bg-red-50 dark:bg-red-900/20 p-3 rounded border overflow-auto max-h-40"
									>{{ row.errorStackTrace }}</pre
								>
							</el-collapse-item>
						</el-collapse>
					</div>
				</div>
			</div>

			<!-- Pagination -->
			<div class="mt-6 flex justify-center">
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
	</el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { ElMessage } from 'element-plus';
import { Loading, ArrowUp, ArrowDown, RefreshRight } from '@element-plus/icons-vue';
import { getActionResult } from '@/apis/action';
import { timeZoneConvert } from '@/hooks/time';
import type { ActionExecutionResult } from '#/action';

interface Props {
	modelValue: boolean;
	triggerSourceId: string;
	triggerSourceType: string;
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

// Filters
const localFilters = ref({
	status: '',
});

// Utility functions
const extractTriggerSource = (triggerContext: string): string => {
	try {
		const context = JSON.parse(triggerContext);
		if (context.TaskName) return `Task: ${context.TaskName}`;
		if (context.StageName) return `Stage: ${context.StageName}`;
		if (context.QuestionText) return `Question: ${context.QuestionText}`;
		if (context.OptionText) return `Option: ${context.OptionText}`;
		return 'Unknown';
	} catch (error) {
		return 'Unknown';
	}
};

const formatDateTime = (dateString: string): string => {
	if (!dateString) return '';
	try {
		return timeZoneConvert(dateString, false, 'MM/DD/YYYY HH:mm:ss');
	} catch (error) {
		console.error('Date format error:', error);
		return dateString;
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

	// Status filter
	if (localFilters.value.status) {
		conditions.push({
			jsonPath: '$.status',
			operator: 'eq',
			value: localFilters.value.status,
		});
	}

	return conditions;
};

// Load action execution results
const loadResults = async () => {
	if (!props.triggerSourceId || !props.onboardingId) {
		console.warn('Missing triggerSourceId or onboardingId for loading action results');
		return;
	}

	loading.value = true;

	try {
		const conditions = buildJsonConditions();
		const response = await getActionResult(props.triggerSourceId, {
			pageIndex: currentPage.value, // API uses 0-based index
			pageSize: pageSize.value,
			jsonConditions: conditions,
		});

		if (response.success) {
			const rawData = response.data?.data || [];
			// Process and enhance the data
			results.value = rawData.map((item: any) => ({
				...item,
				// Map executionStatus to status for compatibility
				status: item.executionStatus,
				// Calculate duration from startedAt and completedAt
				duration:
					item.startedAt && item.completedAt
						? new Date(item.completedAt).getTime() - new Date(item.startedAt).getTime()
						: 0,
				// Extract trigger source from triggerContext if needed
				triggerSource: item.triggerContext ? extractTriggerSource(item.triggerContext) : '',
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
		expandedRows.value.delete(executionId);
	} else {
		expandedRows.value.add(executionId);
	}
};

const handleFilterChange = () => {
	currentPage.value = 1;
	loadResults();
};

const resetFilters = () => {
	localFilters.value.status = '';
	currentPage.value = 1;
	loadResults();
};

const handleSizeChange = (size: number) => {
	pageSize.value = size;
	currentPage.value = 1;
	loadResults();
};

const handleCurrentChange = (page: number) => {
	currentPage.value = page;
	loadResults();
};

// Watch for dialog visibility changes
watch(
	() => props.modelValue,
	(visible) => {
		if (visible && props.triggerSourceId && props.onboardingId) {
			expandedRows.value.clear();
			currentPage.value = 1;
			loadResults();
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

pre {
	white-space: pre-wrap;
	word-wrap: break-word;
}
</style>
