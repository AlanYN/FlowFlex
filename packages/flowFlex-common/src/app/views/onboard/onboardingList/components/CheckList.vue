<template>
	<div class="checklist-container rounded-md" v-if="checklistData && checklistData.length > 0">
		<!-- 遍历显示所有检查清单 -->
		<div v-for="checklist in checklistData || []" :key="checklist.id" class="checklist-section">
			<!-- 头部卡片 -->
			<div class="checklist-header-card rounded-md">
				<div class="flex justify-between">
					<div>
						<h3 v-if="checklist.name" class="checklist-title">{{ checklist.name }}</h3>
						<div v-if="checklist.description" class="checklist-subtitle">
							{{ checklist.description }}
						</div>
					</div>
					<div class="progress-info">
						<span class="progress-percentage">{{ completionRate }}%</span>
						<span class="progress-label">Complete</span>
					</div>
				</div>
				<!-- 进度条 -->
				<div class="progress-bar-container">
					<div class="progress-bar rounded-md">
						<div
							class="progress-fill rounded-md"
							:style="{ width: `${completionRate}%` }"
						></div>
					</div>
				</div>
			</div>

			<!-- 检查项列表 - 添加拖拽功能 -->
			<div class="checklist-items">
				<draggable
					v-model="localTasks"
					item-key="id"
					handle=".drag-handle"
					:disabled="isDragging"
					@start="onDragStart"
					@end="onDragEnd"
					ghost-class="ghost-task"
					class="tasks-draggable"
					:animation="300"
				>
					<template #item="{ element: task }">
						<div
							class="checklist-item-card rounded-md"
							:class="{ completed: task.isCompleted, dragging: isDragging }"
						>
							<!-- 拖拽手柄 -->
							<div class="drag-handle" :class="{ 'drag-disabled': task.isCompleted }">
								<el-icon class="drag-icon">
									<Rank />
								</el-icon>
							</div>

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
				</draggable>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { ElMessage } from 'element-plus';
import { defaultStr } from '@/settings/projectSetting';
import { Check, Rank } from '@element-plus/icons-vue';
import draggable from 'vuedraggable';
import { batchUpdateTaskOrder } from '@/apis/ow/onboarding';

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
	tasksReordered: [tasks: TaskData[]];
}>();

// 拖拽状态
const isDragging = ref(false);
const originalTaskOrder = ref<TaskData[]>([]);

// 本地任务列表副本
const localTasks = computed({
	get: () => props.checklistData?.[0]?.tasks || [],
	set: (newTasks) => {
		// 这里不直接修改 props，而是通过事件通知父组件
		emit('tasksReordered', newTasks);
	},
});

// 方法
const toggleTask = (task: TaskData) => {
	if (isDragging.value) return; // 拖拽时不允许切换状态

	emit('taskToggled', {
		...task,
		isCompleted: !task.isCompleted,
	});
};

// 拖拽开始
const onDragStart = () => {
	isDragging.value = true;
	// 保存原始顺序，以便在失败时恢复
	originalTaskOrder.value = [...localTasks.value];
};

// 拖拽结束
const onDragEnd = async () => {
	isDragging.value = false;

	if (!localTasks.value.length) return;

	try {
		// 更新任务顺序
		const reorderedTasks = localTasks.value.map((task, index) => ({
			...task,
			order: index + 1,
		}));

		// 准备批量更新的数据
		const updateData = {
			tasks: reorderedTasks.map((task) => ({
				id: task.id,
				order: task.order,
			})),
		};

		// 调用API更新任务顺序
		const response = await batchUpdateTaskOrder(updateData);

		if (response.code === '200') {
			// 发射事件通知父组件
			emit('tasksReordered', reorderedTasks);
			ElMessage.success('Task order updated successfully');
		} else {
			// 恢复原始顺序 - 通过重新设置 localTasks
			localTasks.value = [...originalTaskOrder.value];
			ElMessage.error(response.msg || 'Failed to update task order');
		}
	} catch (error) {
		console.error('Failed to update task order:', error);
		// 恢复原始顺序
		localTasks.value = [...originalTaskOrder.value];
		ElMessage.error('Failed to update task order');
	}
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

const completionRate = computed(() => {
	const completed =
		props.checklistData?.[0]?.tasks?.filter((task) => task.isCompleted).length || 0;
	const total = props.checklistData?.[0]?.tasks?.length || 0;

	if (total === 0) return 0;

	const rate = (completed / total) * 100;
	// 四舍五入到整数，并确保不超过100%
	return Math.min(Math.round(rate), 100);
});
</script>

<style scoped lang="scss">
/* 检查清单容器 */
.checklist-container {
	display: flex;
	flex-direction: column;
	gap: 24px;
	background-color: white;
	padding: 16px;
	box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

/* 每个检查清单部分 */
.checklist-section {
	display: flex;
	flex-direction: column;
	gap: 16px;
}

.checklist-section:not(:last-child) {
	border-bottom: 1px solid #e5e7eb;
	padding-bottom: 24px;
}

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

/* 检查项列表 */
.checklist-items {
	display: flex;
	flex-direction: column;
}

.tasks-draggable {
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

	&:hover:not(.completed):not(.dragging) {
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

	&.dragging {
		cursor: grabbing;
		transform: rotate(2deg);
		box-shadow: 0 8px 16px rgba(0, 0, 0, 0.15);
	}
}

/* 拖拽手柄 */
.drag-handle {
	display: flex;
	align-items: center;
	justify-content: center;
	width: 20px;
	height: 20px;
	cursor: grab;
	color: #9ca3af;
	transition: color 0.2s ease;
	flex-shrink: 0;
	margin-top: 2px;

	&:hover {
		color: #6b7280;
	}

	&:active {
		cursor: grabbing;
	}

	&.drag-disabled {
		cursor: not-allowed;
		opacity: 0.5;
	}
}

.drag-icon {
	font-size: 16px;
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

/* 拖拽时的幽灵样式 */
.ghost-task {
	opacity: 0.6;
	background: #f3f4f6;
	border: 2px dashed #9ca3af;
	transform: rotate(0deg);
}

/* 暗色主题适配 */
.dark {
	.checklist-container {
		background-color: var(--black-400);
		box-shadow: 0 1px 3px rgba(0, 0, 0, 0.3);
	}

	.checklist-section:not(:last-child) {
		border-bottom-color: var(--black-100);
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

	.checklist-item-card {
		background-color: var(--black-300);
		border-color: var(--black-200);

		&:hover:not(.completed):not(.dragging) {
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

	.drag-handle {
		color: var(--gray-400);

		&:hover {
			color: var(--gray-300);
		}
	}

	.ghost-task {
		background: var(--black-200);
		border-color: var(--gray-400);
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

	.drag-handle {
		width: 16px;
		height: 16px;
	}

	.drag-icon {
		font-size: 14px;
	}
}
</style>
