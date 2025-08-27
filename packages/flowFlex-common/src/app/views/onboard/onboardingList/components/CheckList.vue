<template>
	<div class="customer-block" v-if="checklistData && checklistData.length > 0">
		<h2 class="text-lg font-semibold">Checklist - {{ checklistData[0].name }}</h2>
		<el-divider />
		<!-- 统一的进度头部卡片 -->
		<div class="checklist-header-card rounded-md">
			<div class="flex justify-between">
				<div>
					<h3 class="checklist-title">Overall Progress</h3>
					<div class="checklist-subtitle">
						{{ totalCompletedTasks }} of {{ totalTasks }} tasks completed
					</div>
				</div>
				<div class="progress-info">
					<span class="progress-percentage">{{ overallCompletionRate }}%</span>
					<span class="progress-label">Complete</span>
				</div>
			</div>
			<!-- 统一进度条 -->
			<div class="progress-bar-container">
				<div class="progress-bar rounded-md">
					<div
						class="progress-fill rounded-md"
						:style="{ width: `${overallCompletionRate}%` }"
					></div>
				</div>
			</div>
		</div>

		<!-- 所有检查项列表 -->
		<div class="checklist-items py-4" v-loading="loading">
			<!-- 遍历所有checklist中的任务 -->
			<template v-for="checklist in checklistData || []" :key="checklist.id">
				<!-- 可选：显示每个checklist的分组标题 -->

				<!-- 该checklist下的所有任务 -->
				<div
					v-for="task in checklist.tasks"
					:key="task.id"
					class="checklist-item-card rounded-md"
				>
					<!-- 任务内容 -->
					<div class="item-content px-4 py-2" :class="{ completed: task.isCompleted }">
						<div class="flex items-center gap-2 mb-1">
							<icon
								icon="material-symbols:check-circle-outline-rounded"
								style="color: #10b981"
								class="text-xl"
								v-if="task.isCompleted"
							/>
							<h4 v-if="task.name" class="item-title bolck">
								{{ task.name }}
							</h4>
						</div>

						<p v-if="task.description" class="item-description">
							{{ task.description }}
						</p>
						<div class="flex gap-3">
							<div class="flex items-center gap-1 flex-shrink-0">
								<!-- Assignee 缩写 -->
								<icon
									icon="material-symbols:person-2-outline"
									style="color: var(--primary-500)"
								/>
								<span
									class="text-xs font-medium text-primary-500"
									:title="task.assigneeName || defaultStr"
								>
									{{ getAssigneeInitials(task.assigneeName) || defaultStr }}
								</span>
							</div>
							<div
								class="flex items-center gap-1 flex-shrink-0"
								style="color: #47b064"
							>
								<icon icon="iconoir:attachment" />
								{{ task?.filesCount || 0 }}
							</div>
							<div
								class="flex items-center gap-1 flex-shrink-0"
								style="color: #ed6f2d"
							>
								<icon icon="mynaui:message" />
								{{ task?.notesCount || 0 }}
							</div>
						</div>
					</div>

					<!-- 任务操作按钮 -->
					<div class="task-actions">
						<el-button
							class="action-button details-button"
							@click.stop="openTaskDetails(task)"
							color="#e6f1fa"
						>
							Details
						</el-button>
						<el-button
							:type="task.isCompleted ? 'danger' : 'success'"
							class="action-button complete-button"
							@click.stop="toggleTask(task)"
						>
							{{ task.isCompleted ? 'Cancel' : 'Done' }}
						</el-button>
					</div>
				</div>
			</template>
		</div>

		<!-- 任务详情弹窗 -->
		<TaskDetailsDialog
			v-model:visible="dialogVisible"
			:task="selectedTask"
			:onboarding-id="onboardingId"
			:stage-id="stageId"
			@update:task="handleTaskUpdate"
		/>
	</div>
</template>

<script setup lang="ts">
import { ElMessageBox } from 'element-plus';
import { computed, ref } from 'vue';
import { ChecklistData, TaskData } from '#/onboard';
import { useI18n } from '@/hooks/useI18n';
import TaskDetailsDialog from './TaskDetailsDialog.vue';
import { defaultStr } from '@/settings/projectSetting';

const { t } = useI18n();

// Props
interface Props {
	checklistData?: ChecklistData[] | null;
	loading?: boolean;
	onboardingId: string;
	stageId: string;
}

const props = defineProps<Props>();

// Events
const emit = defineEmits<{
	taskToggled: [task: TaskData];
	refreshChecklist: [onboardingId: string, stageId: string];
}>();

// 计算总任务数
const totalTasks = computed(() => {
	if (!props.checklistData) return 0;
	return props.checklistData.reduce((total, checklist) => {
		return total + (checklist.tasks?.length || 0);
	}, 0);
});

// 计算总完成任务数
const totalCompletedTasks = computed(() => {
	if (!props.checklistData) return 0;
	return props.checklistData.reduce((total, checklist) => {
		const completedCount = checklist.tasks?.filter((task) => task.isCompleted).length || 0;
		return total + completedCount;
	}, 0);
});

// 计算总体完成率
const overallCompletionRate = computed(() => {
	if (totalTasks.value === 0) return 0;
	const rate = (totalCompletedTasks.value / totalTasks.value) * 100;
	return Math.min(Math.round(rate), 100);
});

// 任务详情弹窗相关
const dialogVisible = ref(false);
const selectedTask = ref<TaskData | null>(null);

// 方法
const toggleTask = async (task: TaskData) => {
	if (task.isCompleted) {
		// 当任务已完成时，需要确认是否撤销
		try {
			await ElMessageBox.confirm(
				'Are you sure you want to undo this completed task? This action will mark the task as incomplete.',
				'Confirm Undo Task',
				{
					confirmButtonText: t('sys.app.confirmText'),
					cancelButtonText: t('sys.app.cancelText'),
					type: 'warning',
					showClose: true,
				}
			);

			// 用户确认后执行撤销操作
			emit('taskToggled', {
				...task,
				isCompleted: !task.isCompleted,
			});
		} catch (error) {
			// 用户取消操作，不执行任何操作
			console.log('User cancelled the undo operation');
		}
	} else {
		// 任务未完成时，直接标记为完成
		emit('taskToggled', {
			...task,
			isCompleted: !task.isCompleted,
		});
	}
};

// 打开任务详情弹窗
const openTaskDetails = (task: TaskData) => {
	selectedTask.value = task;
	dialogVisible.value = true;
};

// 处理任务详情更新
const handleTaskUpdate = () => {
	emit('refreshChecklist', props.onboardingId, props.stageId);
	dialogVisible.value = false;
};

// 获取分配人姓名的缩写
const getAssigneeInitials = (fullName) => {
	if (!fullName) return '';

	const names = fullName.trim().split(/\s+/);
	if (names.length === 1) {
		// 单个名字，取前两个字符
		return names[0].substring(0, 2).toUpperCase();
	} else {
		// 多个名字，取每个名字的首字母
		return names
			.map((name) => name.charAt(0).toUpperCase())
			.join('')
			.substring(0, 3); // 最多3个字母
	}
};
</script>

<style scoped lang="scss">
/* 头部卡片样式 - 橙色渐变 */
.checklist-header-card {
	background: linear-gradient(135deg, #ff6b35 0%, #f7931e 100%);
	padding: 24px;
	color: white;
	box-shadow: 0 4px 12px rgba(255, 107, 53, 0.2);
	display: flex;
	flex-direction: column;
	gap: 16px;
}

.checklist-title {
	font-size: 18px;
	font-weight: 600;
	margin: 0 0 4px 0;
	color: white;
}

.checklist-subtitle {
	font-size: 14px;
	margin: 0;
	color: rgba(255, 255, 255, 0.9);
	font-weight: 400;
}

.progress-info {
	display: flex;
	flex-direction: column;
	align-items: flex-end;
	text-align: right;
}

.progress-percentage {
	font-size: 24px;
	font-weight: 700;
	line-height: 1;
	color: white;
}

.progress-label {
	font-size: 12px;
	color: rgba(255, 255, 255, 0.9);
	margin-top: 2px;
}

.progress-bar-container {
	width: 100%;
}

.progress-bar {
	width: 100%;
	height: 8px;
	background-color: rgba(255, 255, 255, 0.3);
	overflow: hidden;
}

.progress-fill {
	height: 100%;
	background-color: #10b981;
	transition: width 0.3s ease;
}

/* 检查项分组标题 */
.checklist-group-title {
	font-size: 16px;
	font-weight: 600;
	color: #374151;
	margin: 16px 0 8px 0;
	padding-bottom: 8px;
	border-bottom: 2px solid #e5e7eb;
	display: flex;
	align-items: center;
	gap: 8px;
}

.group-task-count {
	font-size: 14px;
	font-weight: 400;
	color: #6b7280;
}

/* 检查项列表 */
.checklist-items {
	display: flex;
	flex-direction: column;
	gap: 8px;
}

/* 每个检查项卡片 - 现代化布局 */
.checklist-item-card {
	display: flex;
	align-items: stretch;
	justify-content: space-between;
	background-color: #ffffff;
	border: 1px solid #e5e7eb;
	border-radius: 8px;
	transition: all 0.2s ease;
	box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
	/* 确保卡片不会被内容撑开 */
	min-width: 0;
	overflow: hidden;

	&:hover {
		border-color: #d1d5db;
		box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
		transform: translateY(-1px);
	}
}

.item-content {
	flex: 1;
	min-width: 0;
	/* 确保内容不会撑开父容器 */
	overflow: hidden;

	&.completed {
		background-color: #f0fdf4;
		border-color: #bbf7d0;

		&:hover {
			border-color: #bbf7d0;
			box-shadow: 0 4px 6px rgba(34, 197, 94, 0.1);
		}
	}
}

/* 重置 Element Plus 按钮默认样式并撑满高度 */
.action-button {
	flex: 1;
	border-radius: 0 !important;
	margin: 0 !important;

	&.complete-button {
		border-top-right-radius: 0 !important;
		border-bottom-right-radius: 0 !important;
	}

	&.details-button {
		border-top-right-radius: 8px !important;
		border-bottom-right-radius: 8px !important;
		border-left: none !important;
	}
}

/* 深度选择器确保按钮内部元素也撑满 */
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

.item-title {
	font-size: 16px;
	font-weight: 600;
	color: #1f2937;
	line-height: 1.5;
	/* 处理长文本，防止撑开容器 */
	overflow: hidden;
	text-overflow: ellipsis;
	display: -webkit-box;
	-webkit-line-clamp: 2;
	line-clamp: 2;
	-webkit-box-orient: vertical;

	.completed & {
		color: #10b981;
	}
}

.item-description {
	font-size: 14px;
	margin: 0 0 8px 0;
	color: #6b7280;
	line-height: 1.4;
	/* 限制描述文本最多显示3行 */
	overflow: hidden;
	display: -webkit-box;
	-webkit-line-clamp: 3;
	line-clamp: 3;
	-webkit-box-orient: vertical;
}

.completion-info {
	display: flex;
	align-items: center;
	gap: 6px;
	font-size: 12px;
	color: #10b981;
	/* 确保完成信息不会撑开容器 */
	min-width: 0;
	overflow: hidden;
}

.completion-icon {
	font-size: 12px;
	color: #10b981;
}

.completion-text {
	font-size: 12px;
	/* 防止长邮箱地址撑开容器 */
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.dark {
	.checklist-container {
		background-color: var(--black-400);
		box-shadow: 0 1px 3px rgba(0, 0, 0, 0.3);
	}

	.checklist-header-card {
		background: linear-gradient(135deg, #e53e3e 0%, #dd6b20 100%);
		box-shadow: 0 4px 12px rgba(229, 62, 62, 0.3);
	}

	.progress-bar {
		background-color: rgba(255, 255, 255, 0.2);
	}

	.progress-fill {
		background-color: #34d399;
	}

	.checklist-group-title {
		color: var(--white-100);
		border-bottom-color: var(--black-100);
	}

	.group-task-count {
		color: var(--gray-400);
	}

	.checklist-item-card {
		background-color: var(--black-300);
		border-color: var(--black-200);
		box-shadow: 0 1px 3px rgba(0, 0, 0, 0.3);

		&:hover {
			border-color: var(--black-100);
			box-shadow: 0 4px 6px rgba(0, 0, 0, 0.3);
			transform: translateY(-1px);
		}

		&.completed {
			background-color: rgba(16, 185, 129, 0.1);
			border-color: rgba(16, 185, 129, 0.3);

			&:hover {
				border-color: rgba(16, 185, 129, 0.4);
				box-shadow: 0 4px 6px rgba(16, 185, 129, 0.2);
			}
		}
	}

	.item-title {
		color: var(--white-100);

		.completed & {
			color: #34d399;
		}
	}

	.item-description {
		color: var(--gray-300);
	}
}

/* 响应式设计 */
@media (max-width: 768px) {
	.checklist-header-card {
		padding: 16px;
	}

	.progress-info {
		align-items: flex-start;
		text-align: left;
	}

	.checklist-item-card {
		padding: 16px;
		flex-direction: column;
		gap: 12px;
	}

	.item-content {
		margin-right: 0;
		margin-bottom: 12px;
	}

	.item-actions {
		width: 100%;
		justify-content: flex-end;
	}

	.item-title {
		font-size: 15px;
	}

	.item-description {
		font-size: 13px;
	}

	.checklist-group-title {
		font-size: 15px;
	}
}
</style>
