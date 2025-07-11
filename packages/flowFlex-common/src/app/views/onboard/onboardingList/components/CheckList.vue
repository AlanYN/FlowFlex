<template>
	<div class="customer-block" v-if="checklistData && checklistData.length > 0">
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
		<div class="checklist-items">
			<!-- 遍历所有checklist中的任务 -->
			<template v-for="checklist in checklistData || []" :key="checklist.id">
				<!-- 可选：显示每个checklist的分组标题 -->
				<div v-if="checklist.name" class="checklist-group-title">
					{{ checklist.name }}
					<span class="group-task-count">({{ checklist.tasks?.length || 0 }} tasks)</span>
				</div>

				<!-- 该checklist下的所有任务 -->
				<div
					v-for="task in checklist.tasks"
					:key="task.id"
					class="checklist-item-card rounded-md"
					:class="{ completed: task.isCompleted }"
				>
					<!-- 任务复选框 -->
					<div class="item-checkbox rounded-md" @click.stop="toggleTask(task)">
						<el-icon v-if="task.isCompleted" class="check-icon">
							<Check />
						</el-icon>
					</div>

					<!-- 任务内容 -->
					<div class="item-content" @click.stop="toggleTask(task)">
						<h4 v-if="task.name" class="item-title">
							{{ task.name }}
							{{ task.id }}
						</h4>
						<p v-if="task.description" class="item-description">
							{{ task.description }}
						</p>

						<!-- 完成信息 - 只在任务完成时显示 -->
						<div v-if="task.isCompleted" class="completion-info">
							<el-icon class="completion-icon"><Check /></el-icon>
							<span class="completion-text">
								Completed by
								{{ task.assigneeName || task.createBy || defaultStr }} on
								{{ formatDate(task.completedDate) || defaultStr }}
							</span>
						</div>
					</div>

					<!-- 任务状态 -->
					<div class="item-status">
						<el-tag
							v-if="task.isCompleted"
							type="success"
							size="small"
							class="status-tag"
						>
							Complete
						</el-tag>
					</div>
				</div>
			</template>
		</div>
	</div>
</template>

<script setup lang="ts">
import { defaultStr } from '@/settings/projectSetting';
import { Check } from '@element-plus/icons-vue';
import { computed } from 'vue';

// 任务数据结构
interface TaskData {
	id: string;
	checklistId: string;
	name: string;
	description: string;
	taskType: string;
	isCompleted: boolean;
	isRequired: boolean;
	assigneeId: string | null;
	assigneeName: string | null;
	assignedTeam: string | null;
	priority: string;
	order: number;
	estimatedHours: number;
	actualHours: number;
	dueDate: string | null;
	completedDate: string | null;
	completionNotes: string | null;
	dependsOnTaskId: string | null;
	attachmentsJson: string | null;
	status: string;
	isActive: boolean;
	createDate: string;
	createBy: string;
}

// API返回的Checklist数据结构
interface ChecklistData {
	id: string;
	name: string;
	description: string;
	team: string;
	type: string;
	status: string;
	isTemplate: boolean;
	templateId: string | null;
	completionRate: number;
	totalTasks: number;
	completedTasks: number;
	estimatedHours: number;
	isActive: boolean;
	createDate: string;
	createBy: string;
	workflowId: string;
	stageId: string;
	workflowName: string | null;
	stageName: string | null;
	tasks: TaskData[];
}

// Props
interface Props {
	checklistData?: ChecklistData[] | null;
}

const props = defineProps<Props>();

// Events
const emit = defineEmits<{
	taskToggled: [task: TaskData];
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

// 方法
const toggleTask = (task: TaskData) => {
	emit('taskToggled', {
		...task,
		isCompleted: !task.isCompleted,
	});
};

// 格式化日期显示
const formatDate = (dateString: string | null): string => {
	if (!dateString) return '';
	try {
		const date = new Date(dateString);
		// 格式化为类似 "2023-06-05 10:30" 的格式
		return date
			.toLocaleString('en-CA', {
				year: 'numeric',
				month: '2-digit',
				day: '2-digit',
				hour: '2-digit',
				minute: '2-digit',
				hour12: false,
			})
			.replace(',', '');
	} catch {
		return '';
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

/* 每个检查项卡片 - 灰色背景 */
.checklist-item-card {
	display: flex;
	align-items: flex-start;
	gap: 12px;
	padding: 16px;
	background-color: #f9fafb;
	border: 1px solid #e5e7eb;
	transition: all 0.2s ease;
	cursor: pointer;

	&:hover:not(.completed) {
		border-color: #3b82f6;
		background-color: #eff6ff;
		box-shadow: 0 2px 4px rgba(59, 130, 246, 0.1);
	}

	&.completed {
		border: 2px solid #10b981;
		background-color: #ecfdf5;

		&:hover {
			border-color: #10b981;
			background-color: #ecfdf5;
			box-shadow: none;
		}
	}
}

.item-checkbox {
	width: 20px;
	height: 20px;
	border: 2px solid #d1d5db;
	display: flex;
	align-items: center;
	justify-content: center;
	flex-shrink: 0;
	margin-top: 2px;
	transition: all 0.2s ease;
	background-color: white;

	.completed & {
		background-color: #10b981;
		border-color: #10b981;
	}
}

.check-icon {
	color: white;
	font-size: 14px;
	font-weight: bold;
}

.item-content {
	flex: 1;
	min-width: 0;
}

.item-title {
	font-size: 16px;
	font-weight: 600;
	margin: 0 0 4px 0;
	color: #1f2937;
	transition: all 0.2s ease;

	.completed & {
		text-decoration: line-through;
		color: #6b7280;
	}
}

.item-description {
	font-size: 14px;
	margin: 0 0 8px 0;
	color: #6b7280;
	line-height: 1.4;
}

.completion-info {
	display: flex;
	align-items: center;
	gap: 6px;
	font-size: 12px;
	color: #10b981;
}

.completion-icon {
	font-size: 12px;
	color: #10b981;
}

.completion-text {
	font-size: 12px;
}

.item-status {
	flex-shrink: 0;
}

.status-tag {
	background-color: #10b981;
	border-color: #10b981;
	color: white;

	:deep(.el-tag__content) {
		color: white;
	}
}

/* 暗色主题适配 */
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

		&:hover:not(.completed) {
			background-color: var(--black-200);
			border-color: #3b82f6;
			box-shadow: 0 2px 4px rgba(59, 130, 246, 0.2);
		}

		&.completed {
			border-color: #10b981;
			background-color: rgba(16, 185, 129, 0.1);

			&:hover {
				border-color: #10b981;
				background-color: rgba(16, 185, 129, 0.1);
				box-shadow: none;
			}
		}
	}

	.item-title {
		color: var(--white-100);

		.completed & {
			color: var(--gray-400);
		}
	}

	.item-description {
		color: var(--gray-300);
	}

	.item-checkbox {
		border-color: var(--black-200);
		background-color: var(--black-400);

		.completed & {
			background-color: #10b981;
			border-color: #10b981;
		}
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
		padding: 12px;
		gap: 8px;
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
