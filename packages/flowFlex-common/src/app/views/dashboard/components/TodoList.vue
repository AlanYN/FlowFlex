<template>
	<div class="todo-list">
		<div class="todo-list__header">
			<div class="todo-list__title">
				<Icon icon="lucide-list-todo" />
				<div>To-Do List</div>
			</div>
			<span class="todo-list__subtitle">Tasks that need your attention</span>
		</div>
		<div class="todo-list__content">
			<!-- Loading State -->
			<template v-if="loading">
				<div class="checklist-items">
					<el-skeleton v-for="i in 5" :key="i" animated class="task-skeleton">
						<template #template>
							<div class="flex items-center gap-3">
								<el-skeleton-item variant="text" style="width: 70%; height: 16px" />
							</div>
							<div class="flex gap-2 mt-2">
								<el-skeleton-item
									variant="text"
									style="width: 60px; height: 14px"
								/>
								<el-skeleton-item
									variant="text"
									style="width: 40px; height: 14px"
								/>
								<el-skeleton-item
									variant="text"
									style="width: 40px; height: 14px"
								/>
							</div>
						</template>
					</el-skeleton>
				</div>
			</template>

			<!-- Empty State -->
			<div v-else-if="items.length === 0" class="todo-list__empty">No tasks</div>

			<!-- Task List -->
			<div v-else class="checklist-items">
				<div v-for="task in items" :key="task.id" class="checklist-item-card rounded-xl">
					<!-- 任务内容 -->
					<div
						class="item-content px-4 py-2"
						:class="{ completed: task.status === 'Completed' }"
					>
						<div class="flex items-center gap-2 mb-1">
							<icon
								v-if="task.status === 'Completed'"
								icon="material-symbols:check-circle-outline-rounded"
								class="text-xl check-complete-icon"
							/>
							<el-link @click="clickTask(task.onboardingId)">{{ task.name }}</el-link>
						</div>
						<p v-if="task.description" class="item-description">
							{{ task.description }}
						</p>
						<div class="flex gap-3 text-sm">
							<div class="flex items-center gap-1 flex-shrink-0">
								<icon
									icon="material-symbols:person-2-outline"
									style="color: var(--primary-500)"
								/>
								<span
									class="font-medium text-primary-500"
									:title="task.assigneeName || defaultStr"
								>
									{{ getAssigneeInitials(task.assigneeName || '') || defaultStr }}
								</span>
							</div>

							<el-tag v-if="task.priority" :type="getPriorityTagType(task.priority)">
								{{ task.priority }}
							</el-tag>
						</div>
					</div>

					<!-- 任务操作按钮 -->
					<div class="task-actions">
						<el-button
							:type="task.status === 'Completed' ? 'danger' : 'success'"
							class="action-button complete-button"
							:loading="taskLoading === task.id"
							@click.stop="toggleTask(task)"
						>
							{{ task.status === 'Completed' ? 'Cancel' : 'Done' }}
						</el-button>
					</div>
				</div>
			</div>
		</div>
		<div class="todo-list__footer">Showing {{ items.length }} tasks</div>
	</div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import type { IDashboardTask } from '#/dashboard';
import { Icon } from '@iconify/vue';
import { defaultStr } from '@/settings/projectSetting';
import { useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { saveCheckListTask } from '@/apis/ow/onboarding';
import { useI18n } from '@/hooks/useI18n';

const { t } = useI18n();
const router = useRouter();

interface Props {
	items: IDashboardTask[];
	loading?: boolean;
}

defineProps<Props>();

const emit = defineEmits<{
	refresh: [];
}>();

const taskLoading = ref<number | null>(null);

async function toggleTask(task: IDashboardTask) {
	const isCompleted = task.status === 'Completed';
	const confirmTitle = isCompleted ? 'Confirm Undo Task' : 'Confirm Complete Task';
	const confirmMessage = isCompleted
		? 'Are you sure you want to undo this completed task? This action will mark the task as incomplete.'
		: 'Are you sure you want to complete this task? This action will mark the task as completed.';

	try {
		await ElMessageBox.confirm(confirmMessage, confirmTitle, {
			confirmButtonText: t('sys.app.confirmText'),
			cancelButtonText: t('sys.app.cancelText'),
			type: 'warning',
			showClose: true,
		});

		// 用户确认后执行操作
		taskLoading.value = task.id;
		const res = await saveCheckListTask({
			checklistId: task.checklistId,
			isCompleted: !isCompleted,
			taskId: task.id,
			onboardingId: task.onboardingId,
			stageId: task.stageId,
		});

		if (res.code === '200') {
			ElMessage.success(t('sys.api.operationSuccess'));
			// 通知父组件刷新数据
			emit('refresh');
		} else {
			ElMessage.error(res.msg || t('sys.api.operationFailed'));
		}
	} catch (error) {
		// 用户取消操作
		console.log('User cancelled the operation');
	} finally {
		taskLoading.value = null;
	}
}

const getPriorityTagType = (priority: string) => {
	switch (priority.toLowerCase()) {
		case 'high':
		case 'critical':
			return 'danger';
		case 'medium':
			return 'warning';
		case 'low':
			return 'success';
		default:
			return 'info';
	}
};

const assigneeInitialsCache = new Map<string, string>();

function getAssigneeInitials(fullName: string): string {
	if (!fullName) return '';
	if (assigneeInitialsCache.has(fullName)) {
		return assigneeInitialsCache.get(fullName)!;
	}
	const names = fullName.trim().split(/\s+/);
	let initials: string;
	if (names.length === 1) {
		initials = names[0].substring(0, 2).toUpperCase();
	} else {
		initials = names
			.map((name) => name.charAt(0).toUpperCase())
			.join('')
			.substring(0, 3);
	}
	assigneeInitialsCache.set(fullName, initials);
	return initials;
}

const clickTask = (id: string) => {
	router.push(`/onboard/onboardDetail?onboardingId=${id}`);
};
</script>

<style scoped lang="scss">
.todo-list {
	@apply rounded-xl overflow-hidden flex flex-col;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
	box-shadow: var(--el-box-shadow-light);
	height: 100%;

	&__header {
		@apply px-4 border-b flex-shrink-0;
		border-color: var(--el-border-color-lighter);
		height: 72px;
		display: flex;
		flex-direction: column;
		justify-content: center;
	}

	&__title {
		@apply text-lg font-semibold m-0 flex items-center space-x-2;
		color: var(--el-text-color-primary);
	}

	&__subtitle {
		@apply text-sm;
		color: var(--el-text-color-secondary);
	}

	&__content {
		@apply overflow-y-auto p-4 flex-1;
		min-height: 200px;
		max-height: 320px;
	}

	&__empty {
		@apply p-8 text-center;
		color: var(--el-text-color-secondary);
	}

	&__footer {
		@apply px-4 py-3 text-center text-sm border-t flex-shrink-0;
		color: var(--el-text-color-secondary);
		border-color: var(--el-border-color-lighter);
		height: 44px;
		display: flex;
		align-items: center;
		justify-content: center;
	}
}

.task-skeleton {
	@apply p-3 mb-2 rounded-lg;
	background: var(--el-fill-color-light);
}

.checklist-items {
	display: flex;
	flex-direction: column;
	gap: 8px;
}

.checklist-item-card {
	display: flex;
	align-items: stretch;
	justify-content: space-between;
	background-color: var(--el-fill-color-blank);
	transition:
		transform 0.15s ease,
		box-shadow 0.15s ease,
		border-color 0.15s ease;
	box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
	min-width: 0;
	overflow: hidden;
	@apply border;

	&:hover {
		border-color: var(--el-color-primary-light-5);
		box-shadow: 0 4px 8px rgba(0, 0, 0, 0.12);
		transform: translateY(-1px);
	}
}

.item-content {
	flex: 1;
	min-width: 0;
	overflow: hidden;
	background-color: var(--el-fill-color-blank);

	&.completed {
		border-left: 6px solid var(--el-color-success);
	}
}

.item-description {
	font-size: 14px;
	margin: 0 0 8px 0;
	color: var(--el-text-color-secondary);
	line-height: 1.4;
	overflow: hidden;
	display: -webkit-box;
	-webkit-line-clamp: 1;
	line-clamp: 1;
	-webkit-box-orient: vertical;
}

.item-due {
	color: var(--el-text-color-secondary);
}

.task-actions {
	display: flex;
	flex-direction: row;
	align-items: stretch;
	align-self: stretch;

	:deep(.el-button) {
		width: 80px;
		height: 100% !important;
		display: flex !important;
		align-items: center !important;
		justify-content: center !important;
		border-radius: inherit !important;
	}
}

.action-button {
	flex: 1;
	border-radius: 0 !important;
	margin: 0 !important;

	&.complete-button {
		border-top-right-radius: 8px !important;
		border-bottom-right-radius: 8px !important;
	}
}

.check-complete-icon {
	color: var(--el-color-success);
}
</style>
