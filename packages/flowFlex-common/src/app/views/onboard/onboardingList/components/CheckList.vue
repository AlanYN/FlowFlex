<template>
	<div class="wfe-global-block-bg" v-if="checklistData && checklistData.length > 0">
		<div
			class="case-component-header rounded-xl"
			:class="{ expanded: isExpanded }"
			@click="toggleExpanded"
		>
			<div class="flex justify-between">
				<div>
					<div class="flex items-center">
						<el-icon
							class="case-component-expand-icon text-lg mr-2"
							:class="{ rotated: isExpanded }"
						>
							<ArrowRight />
						</el-icon>
						<h3 class="case-component-title">{{ checklistData[0].name }}</h3>
					</div>
					<div class="case-component-subtitle">
						{{ totalCompletedTasks }} of {{ totalTasks }} tasks completed
					</div>
				</div>
				<div class="case-component-info">
					<span class="case-component-percentage">{{ overallCompletionRate }}%</span>
					<span class="case-component-label">Completed</span>
				</div>
			</div>
			<!-- 统一进度条 -->
			<div class="case-component-bar-container">
				<div class="case-component-bar rounded-xl">
					<div
						class="case-component-fill rounded-xl"
						:style="{ width: `${overallCompletionRate}%` }"
					></div>
				</div>
			</div>
		</div>

		<!-- 可折叠检查项列表 -->
		<el-collapse-transition>
			<div v-show="isExpanded">
				<!-- 遍历所有checklist中的任务 -->
				<div class="checklist-items p-4" v-loading="loading">
					<template v-for="checklist in checklistData || []" :key="checklist.id">
						<!-- 可选：显示每个checklist的分组标题 -->

						<!-- 该checklist下的所有任务 -->
						<div
							v-for="task in checklist.tasks"
							:key="`task-${task.id}`"
							class="checklist-item-card rounded-xl"
						>
							<!-- 任务内容 -->
							<div
								class="item-content px-4 py-2"
								:class="{ completed: task.isCompleted }"
							>
								<div class="flex items-center gap-2 mb-1">
									<icon
										icon="material-symbols:check-circle-outline-rounded"
										class="text-xl check-complete-icon"
										v-if="task.isCompleted"
									/>
									<h4 v-if="task.name" class="item-title bolck">
										{{ task.name }}
									</h4>
								</div>

								<p v-if="task.description" class="item-description">
									{{ task.description }}
								</p>
								<div class="flex gap-3 text-sm">
									<div class="flex items-center gap-1 flex-shrink-0">
										<!-- Assignee 缩写 -->
										<icon
											icon="material-symbols:person-2-outline"
											style="color: var(--primary-500)"
										/>
										<span
											class="font-medium text-primary-500"
											:title="task.assigneeName || defaultStr"
										>
											{{
												getAssigneeInitials(task?.assigneeName || '') ||
												defaultStr
											}}
										</span>
									</div>
									<div
										class="flex items-center gap-1 flex-shrink-0 task-files-count"
									>
										<icon icon="iconoir:attachment" />
										{{ task?.filesCount || 0 }}
									</div>
									<div
										class="flex items-center gap-1 flex-shrink-0 task-notes-count"
									>
										<icon icon="mynaui:message" />
										{{ task?.notesCount || 0 }}
									</div>
									<!-- Action Tag -->
									<ActionTag
										v-if="task.actionId && task.actionName"
										:action="{ id: task.actionId, name: task.actionName }"
										:trigger-source-id="task.id"
										trigger-source-type="task"
										:onboarding-id="props.onboardingId"
										type="success"
										size="small"
									/>
								</div>
							</div>

							<!-- 任务操作按钮 -->
							<div class="task-actions">
								<el-button
									class="action-button details-button"
									@click.stop="openTaskDetails(task)"
								>
									Details
								</el-button>
								<el-button
									:type="task.isCompleted ? 'danger' : 'success'"
									class="action-button complete-button"
									@click.stop="toggleTask(task)"
									:disabled="disabled"
								>
									{{ task.isCompleted ? 'Cancel' : 'Done' }}
								</el-button>
							</div>
						</div>
					</template>
				</div>
			</div>
		</el-collapse-transition>

		<!-- 任务详情弹窗 -->
		<TaskDetailsDialog
			v-model:visible="dialogVisible"
			:task="selectedTask"
			:onboarding-id="onboardingId"
			:stage-id="stageId"
			:disabled="disabled"
			@update:task="handleTaskUpdate"
		/>
	</div>
</template>

<script setup lang="ts">
import { ElMessageBox } from 'element-plus';
import { computed, ref, shallowRef, watchEffect } from 'vue';
import { ArrowRight } from '@element-plus/icons-vue';
import { ChecklistData, TaskData } from '#/onboard';
import { useI18n } from '@/hooks/useI18n';
import TaskDetailsDialog from './TaskDetailsDialog.vue';
import ActionTag from '@/components/actionTools/ActionTag.vue';
import { defaultStr } from '@/settings/projectSetting';

const { t } = useI18n();

// Props
interface Props {
	checklistData?: ChecklistData[] | null;
	loading?: boolean;
	onboardingId: string;
	stageId: string;
	disabled?: boolean;
}

const props = defineProps<Props>();

// Events
const emit = defineEmits<{
	taskToggled: [task: TaskData];
	refreshChecklist: [onboardingId: string, stageId: string];
}>();

// 使用缓存的统计数据
const statsCache = ref({ totalTasks: 0, completedTasks: 0, completionRate: 0 });

// 计算统计数据 - 使用watchEffect避免重复计算
watchEffect(() => {
	if (!props.checklistData) {
		statsCache.value = { totalTasks: 0, completedTasks: 0, completionRate: 0 };
		return;
	}

	let totalTasks = 0;
	let completedTasks = 0;

	// 一次遍历计算所有统计数据
	for (const checklist of props.checklistData) {
		if (checklist.tasks) {
			for (const task of checklist.tasks) {
				totalTasks++;
				if (task.isCompleted) {
					completedTasks++;
				}
			}
		}
	}

	const completionRate =
		totalTasks === 0 ? 0 : Math.min(Math.round((completedTasks / totalTasks) * 100), 100);

	statsCache.value = { totalTasks, completedTasks, completionRate };
});

// 计算属性直接从缓存获取
const totalTasks = computed(() => statsCache.value.totalTasks);
const totalCompletedTasks = computed(() => statsCache.value.completedTasks);
const overallCompletionRate = computed(() => statsCache.value.completionRate);

// 任务详情弹窗相关
const dialogVisible = ref(false);
const selectedTask = shallowRef<TaskData | null>(null);

// 折叠状态
const isExpanded = ref(true); // 默认展开

// 切换展开状态
const toggleExpanded = () => {
	isExpanded.value = !isExpanded.value;
};

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
		try {
			await ElMessageBox.confirm(
				'Are you sure you want to complete this task? This action will mark the task as completed.',
				'Confirm Complete Task',
				{
					confirmButtonText: t('sys.app.confirmText'),
					cancelButtonText: t('sys.app.cancelText'),
					type: 'warning',
					showClose: true,
				}
			);
			// 任务未完成时，直接标记为完成
			emit('taskToggled', {
				...task,
				isCompleted: !task.isCompleted,
			});
		} catch (error) {
			// 用户取消操作，不执行任何操作
			console.log('User cancelled the undo operation');
		}
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

// 获取分配人姓名的缩写 - 使用缓存优化性能
const assigneeInitialsCache = new Map<string, string>();

const getAssigneeInitials = (fullName: string) => {
	if (!fullName) return '';

	// 检查缓存
	if (assigneeInitialsCache.has(fullName)) {
		return assigneeInitialsCache.get(fullName)!;
	}

	const names = fullName.trim().split(/\s+/);
	let initials: string;

	if (names.length === 1) {
		// 单个名字，取前两个字符
		initials = names[0].substring(0, 2).toUpperCase();
	} else {
		// 多个名字，取每个名字的首字母
		initials = names
			.map((name) => name.charAt(0).toUpperCase())
			.join('')
			.substring(0, 3); // 最多3个字母
	}

	// 缓存结果
	assigneeInitialsCache.set(fullName, initials);
	return initials;
};
</script>

<style scoped lang="scss">
/* 优化任务列表性能 - 只优化动画 */
.checklist-items {
	transform: translateZ(0);
	backface-visibility: hidden;
	contain: layout;
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
	background-color: var(--el-fill-color-blank);
	/* 优化动画性能 */
	transition:
		transform 0.15s ease,
		box-shadow 0.15s ease,
		border-color 0.15s ease;
	box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
	/* 确保卡片不会被内容撑开 */
	min-width: 0;
	overflow: hidden;
	/* 启用硬件加速 */
	transform: translateZ(0);
	backface-visibility: hidden;
	/* 避免重绘 */
	contain: layout style;
	@apply rounded-xl border;

	&:hover {
		border-color: var(--el-color-primary-light-5);
		box-shadow: 0 4px 8px rgba(0, 0, 0, 0.12);
		transform: translateY(-1px) translateZ(0);
	}
}

.item-content {
	flex: 1;
	min-width: 0;
	/* 确保内容不会撑开父容器 */
	overflow: hidden;
	background-color: var(--el-fill-color-blank);

	&.completed {
		border-left: 6px solid var(--el-color-success);

		&:hover {
			background-color: var(--el-color-success-light-8);
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
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	line-height: 1.5;
	/* 处理长文本，防止撑开容器 */
	overflow: hidden;
	text-overflow: ellipsis;
	display: -webkit-box;
	-webkit-line-clamp: 1;
	line-clamp: 1;
	-webkit-box-orient: vertical;

	.completed & {
		color: var(--el-color-success);
	}
}

/* 新增的简化样式 */
.completed-check {
	font-size: 18px;
	color: var(--el-color-success);
	font-weight: bold;
}

.task-meta {
	display: flex;
	gap: 12px;
	font-size: 12px;
	align-items: center;
}

.assignee-info {
	color: var(--el-color-primary);
	font-weight: 500;
}

.file-count {
	color: var(--el-color-success);
}

.note-count {
	color: var(--el-color-danger);
}

.item-description {
	font-size: 14px;
	margin: 0 0 8px 0;
	color: var(--el-text-color-secondary);
	line-height: 1.4;
	/* 限制描述文本最多显示3行 */
	overflow: hidden;
	display: -webkit-box;
	-webkit-line-clamp: 1;
	line-clamp: 1;
	-webkit-box-orient: vertical;
}

.completion-info {
	display: flex;
	align-items: center;
	gap: 6px;
	font-size: 12px;
	color: var(--el-color-success);
	/* 确保完成信息不会撑开容器 */
	min-width: 0;
	overflow: hidden;
}

.completion-icon {
	font-size: 12px;
	color: var(--el-color-success);
}

.completion-text {
	font-size: 12px;
	/* 防止长邮箱地址撑开容器 */
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.dark {
	.checklist-item-card {
		box-shadow: 0 1px 3px rgba(0, 0, 0, 0.3);

		&:hover {
			border-color: var(--black-100);
			box-shadow: 0 4px 6px rgba(0, 0, 0, 0.3);
			transform: translateY(-1px);
		}

		&.completed {
			background-color: var(--el-color-success-light-9);
			border-color: rgba(16, 185, 129, 0.3);

			&:hover {
				border-color: rgba(16, 185, 129, 0.4);
				box-shadow: 0 4px 6px rgba(16, 185, 129, 0.2);
			}
		}
	}

	.item-title {
		color: var(--el-color-white);

		.completed & {
			color: var(--el-color-success-light-3);
		}
	}

	.item-description {
		color: var(--gray-300);
	}
}

/* 自定义图标和文本颜色类 */
.check-complete-icon {
	color: var(--el-color-success);
}

.task-files-count {
	color: var(--el-color-success);
}

.task-notes-count {
	color: var(--el-color-warning);
}

.details-button {
	background-color: var(--el-fill-color) !important;
	border-color: var(--el-border-color-light) !important;
	color: var(--el-text-color-regular) !important;

	&:hover {
		border-color: var(--el-color-primary-light-5) !important;
		color: var(--el-color-primary) !important;
	}
}
</style>
