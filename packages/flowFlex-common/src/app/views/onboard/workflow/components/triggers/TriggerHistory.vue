<template>
	<!-- FAB — only visible when panel is closed -->
	<div
		class="trigger-history-fab"
		title="Trigger History"
		:class="{ 'fab-hidden': isExpanded }"
		@click="open"
	>
		<el-icon class="fab-icon"><Connection /></el-icon>
	</div>

	<!-- Slide panel -->
	<Teleport to="body">
		<Transition name="trigger-history-panel">
			<div v-if="isExpanded" class="trigger-history-wrapper">
				<div class="trigger-history-overlay" @click="close"></div>
				<div class="trigger-history-panel">
					<!-- Header -->
					<div class="trigger-history-panel-header">
						<div class="flex items-center gap-2">
							<el-icon class="text-xl"><Connection /></el-icon>
							<h3 class="text-base font-semibold">Trigger History</h3>
						</div>
						<div class="flex items-center gap-2">
							<!-- Workflow switcher -->
							<el-select
								v-model="selectedWorkflowId"
								:loading="workflowsLoading"
								placeholder="Select workflow"
								class="w-48"
								filterable
								@change="onWorkflowChange"
							>
								<el-option
									v-for="wf in allWorkflows"
									:key="wf.id"
									:value="wf.id"
									:label="
										wf.id === props.workflowId
											? `${wf.name} (Current)`
											: wf.name
									"
								/>
							</el-select>
							<el-divider direction="vertical" style="height: 20px" />
							<el-select
								v-model="statusFilter"
								clearable
								placeholder="All statuses"
								class="w-40"
								@change="onFilterChange"
							>
								<el-option label="Triggered" value="Triggered" />
								<el-option label="Skipped" value="Skipped" />
								<el-option label="Failed" value="Failed" />
							</el-select>
							<div>
								<el-button
									:icon="RefreshRight"
									type="primary"
									:loading="loading"
									circle
									@click="load"
								/>
							</div>
							<el-button :icon="Close" circle @click="close" />
						</div>
					</div>

					<!-- Content — flex:1, scrolls vertically -->
					<el-table
						:data="logs"
						class="w-full"
						border
						stripe
						row-key="id"
						:tooltip-options="{ placement: 'top' }"
						v-loading="loading"
					>
						<el-table-column label="Date & Time" width="175" show-overflow-tooltip>
							<template #default="{ row }">
								<div
									class="flex items-center gap-1 text-gray-600 dark:text-gray-400"
								>
									<el-icon><Clock /></el-icon>
									{{ formatDateTime(row.createDate) }}
								</div>
							</template>
						</el-table-column>

						<el-table-column label="Status" width="120">
							<template #default="{ row }">
								<el-tag :type="tagType(row.status)" class="font-semibold">
									{{ row.status }}
								</el-tag>
							</template>
						</el-table-column>

						<el-table-column label="Source Case" width="220" show-overflow-tooltip>
							<template #default="{ row }">
								<el-link type="primary" @click="goToCase(row.sourceOnboardingId)">
									<template v-if="row.sourceCaseCode">
										{{ row.sourceCaseCode }} ·
									</template>
									{{ row.sourceCaseName || row.sourceOnboardingId }}
								</el-link>
							</template>
						</el-table-column>

						<el-table-column label="Target Case" width="220" show-overflow-tooltip>
							<template #default="{ row }">
								<el-link
									v-if="row.targetOnboardingId"
									type="success"
									@click="goToCase(row.targetOnboardingId)"
								>
									<template v-if="row.targetCaseCode">
										{{ row.targetCaseCode }} ·
									</template>
									{{ row.targetCaseName || row.targetOnboardingId }}
								</el-link>
								<span v-else class="text-gray-400">—</span>
							</template>
						</el-table-column>

						<el-table-column label="Trigger Type" width="155" show-overflow-tooltip>
							<template #default="{ row }">
								<span class="text-gray-600 dark:text-gray-400">
									{{ row.completionType }}
								</span>
							</template>
						</el-table-column>

						<!-- Reason: use custom el-tooltip slot to control max-width -->
						<el-table-column label="Reason / Error" min-width="260">
							<template #default="{ row }">
								<el-tooltip v-if="row.reason" placement="top" :show-after="300">
									<template #content>
										<div
											style="
												max-width: 400px;
												white-space: pre-wrap;
												word-break: break-word;
												line-height: 1.5;
											"
										>
											{{ row.reason }}
										</div>
									</template>
									<span
										class="text-gray-600 dark:text-gray-400 cursor-default truncate block"
									>
										{{ row.reason }}
									</span>
								</el-tooltip>
								<span v-else class="text-gray-400">—</span>
							</template>
						</el-table-column>

						<el-table-column
							label=""
							width="64"
							align="center"
							fixed="right"
							class-name="no-overflow-cell"
						>
							<template #default="{ row }">
								<el-tooltip
									v-if="row.status === 'Failed' || row.status === 'Skipped'"
									content="Retry — re-evaluate and trigger again with the latest source Case data"
									placement="top"
								>
									<el-button
										type="warning"
										:icon="RefreshRight"
										circle
										plain
										:loading="retryingId === row.id"
										@click="handleRetry(row)"
									/>
								</el-tooltip>
							</template>
						</el-table-column>

						<template #empty>
							<div class="py-10 text-center text-gray-500">
								<el-icon class="text-4xl mb-2"><Connection /></el-icon>
								<p>No trigger history yet.</p>
							</div>
						</template>
					</el-table>

					<!-- Pagination — outside content area, always visible at bottom -->
					<div class="trigger-history-panel-footer">
						<CustomerPagination
							:total="total"
							:limit="pageSize"
							:page="currentPage"
							:background="true"
							@pagination="load"
							@update:page="handlePageChange"
						/>
					</div>
				</div>
			</div>
		</Transition>
	</Teleport>
</template>

<script setup lang="ts">
import { ref, watch, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { Connection, RefreshRight, Close, Clock } from '@element-plus/icons-vue';
import { ElMessage } from 'element-plus';
import {
	getTriggerLogsByWorkflow,
	getTriggerGraphAllWorkflows,
	retryTrigger,
} from '@/apis/ow/triggers';
import CustomerPagination from '@/components/global/u-pagination/index.vue';

const props = defineProps<{ workflowId: string }>();
const router = useRouter();

const isExpanded = ref(false);
const loading = ref(false);
const logs = ref<any[]>([]);
const total = ref(0);
const currentPage = ref(1);
const pageSize = 20;
const statusFilter = ref<string | undefined>(undefined);
const retryingId = ref<string | null>(null);

// ── Workflow switcher ──────────────────────────────────────────────────────
interface WorkflowOption {
	id: string;
	name: string;
}
const allWorkflows = ref<WorkflowOption[]>([]);
const selectedWorkflowId = ref<string>('');
const workflowsLoading = ref(false);

const loadWorkflows = async () => {
	if (allWorkflows.value.length > 0) return;
	workflowsLoading.value = true;
	try {
		const res = await getTriggerGraphAllWorkflows();
		allWorkflows.value = (res?.data ?? []).map((w: any) => ({
			id: String(w.id),
			name: w.name || w.id,
		}));
	} finally {
		workflowsLoading.value = false;
	}
};

const onWorkflowChange = (val: string) => {
	selectedWorkflowId.value = val;
	currentPage.value = 1;
	logs.value = [];
	total.value = 0;
	load();
};
// ─────────────────────────────────────────────────────────────────────────────

const open = () => {
	isExpanded.value = true;
	loadWorkflows();
	if (logs.value.length === 0) load();
};
const close = () => {
	isExpanded.value = false;
};

const tagType = (status: string) => {
	switch (status) {
		case 'Triggered':
			return 'success';
		case 'Skipped':
			return 'warning';
		case 'Failed':
			return 'danger';
		default:
			return 'info';
	}
};

const formatDateTime = (dateStr: string): string => {
	if (!dateStr) return '—';
	try {
		return new Date(dateStr).toLocaleString('zh-CN', {
			year: 'numeric',
			month: '2-digit',
			day: '2-digit',
			hour: '2-digit',
			minute: '2-digit',
			second: '2-digit',
		});
	} catch {
		return dateStr;
	}
};

const goToCase = (id: string) => {
	if (!id) return;
	const url = router.resolve({
		path: '/onboard/onboardDetail',
		query: { onboardingId: id },
	}).href;
	window.open(url, '_blank');
};

const load = async () => {
	const targetId = selectedWorkflowId.value || props.workflowId;
	if (!targetId) return;
	loading.value = true;
	try {
		const res = await getTriggerLogsByWorkflow(targetId, {
			pageIndex: currentPage.value,
			pageSize,
			status: statusFilter.value || undefined,
		});
		if (res?.data) {
			logs.value = res.data.items ?? [];
			total.value = res.data.total ?? 0;
		}
	} catch {
		logs.value = [];
		total.value = 0;
	} finally {
		loading.value = false;
	}
};

const handlePageChange = (page: number) => {
	currentPage.value = page;
	load();
};

const handleRetry = async (row: any) => {
	retryingId.value = row.id;
	try {
		await retryTrigger(row.sourceOnboardingId, row.sourceWorkflowId);
		ElMessage.success('Retry triggered. A new log entry will appear shortly.');
		setTimeout(() => load(), 1500);
	} catch {
		ElMessage.error('Retry failed. Please try again.');
	} finally {
		retryingId.value = null;
	}
};

const onFilterChange = () => {
	currentPage.value = 1;
	load();
};

watch(
	() => props.workflowId,
	(val) => {
		selectedWorkflowId.value = val;
		total.value = 0;
		logs.value = [];
	}
);

onMounted(() => {
	selectedWorkflowId.value = props.workflowId;
});
</script>

<style scoped lang="scss">
/* ── FAB ─────────────────────────────────────────────────────── */
.trigger-history-fab {
	position: fixed;
	right: 24px;
	bottom: 68px;
	width: 36px;
	height: 36px;
	border-radius: 50%;
	background: var(--el-color-warning);
	color: white;
	display: flex;
	align-items: center;
	justify-content: center;
	cursor: pointer;
	box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
	transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
	z-index: 2001;

	&:hover {
		transform: scale(1.1);
		box-shadow: 0 6px 20px rgba(0, 0, 0, 0.25);
	}

	&.fab-hidden {
		transform: scale(0);
		opacity: 0;
		pointer-events: none;
	}

	.fab-icon {
		font-size: 18px;
	}
}

/* ── Wrapper & overlay ───────────────────────────────────────── */
.trigger-history-wrapper {
	position: fixed;
	inset: 0;
	z-index: 2000;
}

.trigger-history-overlay {
	position: absolute;
	inset: 0;
	background-color: rgba(var(--black-400-rgb, 0, 0, 0), 0.6);
	backdrop-filter: blur(1px);
	-webkit-backdrop-filter: blur(1px);
}

/* ── Panel — fixed size identical to ChangeLog ───────────────── */
.trigger-history-panel {
	position: absolute;
	right: 24px;
	bottom: 90px;
	width: 1100px;
	height: 700px;
	max-width: calc(100vw - 48px);
	max-height: calc(100vh - 120px);
	background: var(--el-bg-color);
	border-radius: 12px;
	box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
	display: flex;
	flex-direction: column;
	overflow: hidden;
}

.trigger-history-panel-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: 16px 20px;
	border-bottom: 1px solid var(--el-border-color-lighter);
	background: var(--el-fill-color-light);
	flex-shrink: 0;
}

/* Pagination — always visible at bottom */
.trigger-history-panel-footer {
	flex-shrink: 0;
	padding: 12px 20px;
	border-top: 1px solid var(--el-border-color-lighter);
	background: var(--el-fill-color-light);
}

/* ── Transition — same as ChangeLog ─────────────────────────── */
.trigger-history-panel-enter-active {
	transition: opacity 0.3s ease;

	.trigger-history-overlay {
		transition: opacity 0.3s ease;
	}
	.trigger-history-panel {
		transition:
			transform 0.35s cubic-bezier(0.4, 0, 0.2, 1),
			opacity 0.3s ease;
	}
}

.trigger-history-panel-leave-active {
	transition: opacity 0.25s ease;

	.trigger-history-overlay {
		transition: opacity 0.25s ease;
	}
	.trigger-history-panel {
		transition:
			transform 0.25s cubic-bezier(0.4, 0, 0.2, 1),
			opacity 0.2s ease;
	}
}

.trigger-history-panel-enter-from,
.trigger-history-panel-leave-to {
	.trigger-history-overlay {
		opacity: 0;
	}
	.trigger-history-panel {
		transform: translateX(calc(100% + 24px)) translateY(calc(100% + 90px));
		opacity: 0;
	}
}

html.dark {
	.trigger-history-fab {
		box-shadow: 0 4px 12px rgba(0, 0, 0, 0.4);
	}
	.trigger-history-panel {
		box-shadow: 0 20px 60px rgba(0, 0, 0, 0.5);
	}
}

@media (max-width: 992px) {
	.trigger-history-panel {
		width: calc(100vw - 48px);
		height: calc(100vh - 120px);
	}
}

@media (max-width: 768px) {
	.trigger-history-fab {
		right: 16px;
		bottom: 60px;
	}
	.trigger-history-panel {
		right: 12px;
		bottom: 76px;
	}
}
</style>
