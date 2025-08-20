<template>
	<div class="task-list">
		<div
			class="flex items-center justify-center py-8"
			v-if="!tasksLoaded || (isDragging && draggingChecklistId === props.checklist.id)"
		>
			<el-icon class="animate-spin h-6 w-6 text-primary-500 mr-2">
				<Loading />
			</el-icon>
			{{ !tasksLoaded ? 'Loading tasks...' : 'Updating task order...' }}
		</div>

		<!-- 任务已加载完成时显示 -->
		<div v-if="tasksLoaded">
			<div class="flex items-center justify-between mb-4">
				<h4 class="text-sm font-medium text-gray-900">Tasks</h4>
				<div class="flex items-center space-x-2">
					<el-button
						@click="showAddTaskDialog(props.checklist)"
						type="primary"
						:icon="Plus"
					>
						Add Task
					</el-button>
				</div>
			</div>

			<!-- 添加任务输入框 -->
			<div v-if="addingTaskTo === props.checklist.id" class="flex gap-2 mb-4">
				<el-input
					v-model="newTaskText"
					placeholder="New task..."
					@keyup.enter="addTask(props.checklist.id)"
					class="flex-1"
				/>
				<el-button @click="addTask(props.checklist.id)" type="primary">Add</el-button>
				<el-button @click="cancelAddTask">Cancel</el-button>
			</div>

			<!-- 任务列表 -->
			<div v-if="tasks.length > 0">
				<draggable
					:model-value="tasks"
					@update:model-value="(newTasks) => updateTasks(newTasks)"
					item-key="id"
					handle=".drag-handle"
					:animation="300"
					ghost-class="ghost-task"
					class="tasks-draggable"
					@start="onTaskDragStart(props.checklist.id)"
					@change="onTaskDragChange(props.checklist.id, $event)"
				>
					<template #item="{ element: task }">
						<div
							class="flex items-center gap-3 p-3 transition-all duration-200 border border-transparent rounded-lg task-item"
							:class="{
								'task-disabled':
									isDragging && draggingChecklistId !== props.checklist.id,
								'task-sorting':
									isDragging && draggingChecklistId === props.checklist.id,
							}"
						>
							<!-- 排序图标 -->
							<el-button
								size="small"
								text
								circle
								class="cursor-move drag-handle"
								:class="{
									'drag-disabled':
										isDragging && draggingChecklistId !== props.checklist.id,
									'drag-sorting':
										isDragging && draggingChecklistId === props.checklist.id,
								}"
							>
								<Icon icon="mdi:drag-vertical" class="w-4 h-4" />
							</el-button>

							<!-- 正常显示模式 -->
							<template v-if="!(editingTask && editingTask.id === task.id)">
								<span class="flex-1 text-sm text-gray-900">
									{{ task.name }}
								</span>
								<div class="flex items-center space-x-1">
									<el-button
										@click="editTask(props.checklist.id, task)"
										size="small"
										text
										:icon="Edit"
									/>
									<el-button
										@click="deleteTask(props.checklist.id, task.id)"
										size="small"
										text
										:icon="Delete"
										class="text-red-500"
									/>
								</div>
							</template>

							<!-- 编辑模式 -->
							<template v-else>
								<div class="flex-1 pr-2">
									<el-input
										:model-value="taskFormData.name"
										@update:model-value="
											(val) => updateTaskFormData('name', val)
										"
										placeholder="Task name"
										size="small"
									/>
								</div>
								<div class="flex items-center gap-1">
									<el-button
										@click="saveTaskEdit"
										type="primary"
										size="small"
										:icon="Check"
									/>
									<el-button @click="cancelTaskEdit" size="small" :icon="Close" />
								</div>
							</template>
						</div>
					</template>
				</draggable>
			</div>
			<div v-else class="text-center py-8 text-gray-500">
				<p class="text-sm">
					No tasks added yet. Click the "Add Task" button to add a task.
				</p>
			</div>
		</div>
	</div>
</template>

<script setup>
import { ref } from 'vue';
import { Plus, Edit, Delete, Loading, Check, Close } from '@element-plus/icons-vue';
import { Icon } from '@iconify/vue';
import draggable from 'vuedraggable';
import { ElMessage, ElMessageBox } from 'element-plus';
import {
	getChecklistTasks,
	createChecklistTask,
	updateChecklistTask,
	deleteChecklistTask,
	formatTaskForApi,
} from '@/apis/ow/checklist';
import { useI18n } from '@/hooks/useI18n';

const props = defineProps({
	checklist: {
		type: Object,
		required: true,
	},
});

const emit = defineEmits([
	'task-updated', // 当任务更新时通知父组件刷新checklist
]);

const { t } = useI18n();

// 内部状态管理
const tasks = ref([]);
const tasksLoaded = ref(false);
const editingTask = ref(null);
const taskFormData = ref({
	name: '',
	description: '',
	estimatedMinutes: 0,
	isRequired: false,
});
const addingTaskTo = ref(null);
const newTaskText = ref('');
const isDragging = ref(false);
const draggingChecklistId = ref(null);

// 加载任务数据
const loadTasks = async () => {
	try {
		tasksLoaded.value = false;
		const response = await getChecklistTasks(props.checklist.id);
		const tasksData = response.data || response || [];

		const processedTasks = tasksData
			.map((task) => ({
				...task,
				completed: task.isCompleted || task.completed || false,
				estimatedMinutes: task.estimatedHours ? task.estimatedHours * 60 : 0,
				order: task.orderIndex !== undefined ? task.orderIndex : task.order || 0,
			}))
			.sort((a, b) => (a.order || 0) - (b.order || 0));

		tasks.value = processedTasks;
		tasksLoaded.value = true;
	} catch (error) {
		ElMessage.error(`Failed to load tasks: ${error.message || 'Unknown error'}`);
		tasks.value = [];
		tasksLoaded.value = true;
	}
};

// 显示添加任务对话框
const showAddTaskDialog = (checklist) => {
	addingTaskTo.value = checklist.id;
	newTaskText.value = '';
};

// 取消添加任务
const cancelAddTask = () => {
	addingTaskTo.value = null;
	newTaskText.value = '';
};

// 添加任务
const addTask = async (checklistId) => {
	if (!newTaskText.value.trim()) return;

	try {
		const taskData = formatTaskForApi({
			checklistId: checklistId,
			name: newTaskText.value.trim(),
			description: '',
			isRequired: false,
			order: tasks.value.length,
		});

		await createChecklistTask(taskData);
		ElMessage.success(t('sys.api.operationSuccess'));

		// 重新加载任务数据
		await loadTasks();
		// 通知父组件更新checklist数据
		emit('task-updated', checklistId);
		cancelAddTask();
	} catch (err) {
		ElMessage.error(t('sys.api.operationFailed'));
		cancelAddTask();
	}
};

// 删除任务
const deleteTask = async (checklistId, taskId) => {
	try {
		await ElMessageBox.confirm(
			'Are you sure you want to delete this task? This action cannot be undone.',
			'Confirm Deletion',
			{
				confirmButtonText: 'Delete Task',
				cancelButtonText: 'Cancel',
				type: 'warning',
				customClass: 'custom-confirm-dialog',
				confirmButtonClass: 'el-button--danger',
			}
		);
	} catch {
		return; // 用户取消删除
	}

	try {
		await deleteChecklistTask(taskId, true);
		ElMessage.success('Task deleted successfully');

		// 重新加载任务数据
		await loadTasks();
		// 通知父组件更新checklist数据
		emit('task-updated', checklistId);
	} catch (err) {
		ElMessage.error('Failed to delete task');
		// 重新加载任务数据（即使出错也要刷新）
		await loadTasks();
		// 通知父组件更新checklist数据（即使出错也要刷新）
		emit('task-updated', checklistId);
	}
};

// 编辑任务
const editTask = (checklistId, task) => {
	editingTask.value = task;
	taskFormData.value = {
		name: task.name,
		description: task.description || '',
		estimatedMinutes: task.estimatedMinutes || 0,
		isRequired: task.isRequired || false,
	};
};

// 取消任务编辑
const cancelTaskEdit = () => {
	editingTask.value = null;
	taskFormData.value = {
		name: '',
		description: '',
		estimatedMinutes: 0,
		isRequired: false,
	};
};

// 保存任务编辑
const saveTaskEdit = async () => {
	if (!editingTask.value) return;

	try {
		const taskData = formatTaskForApi({
			...editingTask.value,
			checklistId: props.checklist.id,
			name: taskFormData.value.name,
			description: taskFormData.value.description,
			estimatedMinutes: taskFormData.value.estimatedMinutes,
			isRequired: taskFormData.value.isRequired,
		});

		await updateChecklistTask(editingTask.value.id, taskData);
		ElMessage.success(t('sys.api.operationSuccess'));

		// 重新加载任务数据
		await loadTasks();
		// 通知父组件更新checklist数据
		emit('task-updated', props.checklist.id);
		cancelTaskEdit();
	} catch (err) {
		ElMessage.error(t('sys.api.operationFailed'));
		cancelTaskEdit();
	}
};

// 拖拽开始
const onTaskDragStart = (checklistId) => {
	draggingChecklistId.value = checklistId;
};

// 拖拽改变顺序
const onTaskDragChange = async (checklistId, event) => {
	isDragging.value = true;

	// 创建最小loading时间的Promise
	const minLoadingTime = new Promise((resolve) => setTimeout(resolve, 800));

	try {
		// 获取重新排序后的任务列表
		const reorderedTasks = tasks.value.map((task, index) => ({
			...task,
			order: index,
		}));

		// 更新后端数据 - 为每个任务分配新的顺序号
		const updatePromises = reorderedTasks.map((task, index) => {
			const updatedTask = formatTaskForApi({
				...task,
				checklistId: checklistId,
				order: index,
			});
			return updateChecklistTask(task.id, updatedTask);
		});

		// 等待API调用和最小loading时间
		await Promise.all([Promise.all(updatePromises), minLoadingTime]);
		ElMessage.success('Task order updated successfully');

		// 重新加载任务数据
		await loadTasks();
		// 通知父组件更新checklist数据
		emit('task-updated', checklistId);
	} catch (err) {
		ElMessage.warning('Failed to save new order, but changes are visible locally');
		// 即使出错也要等待最小loading时间并通知更新
		await minLoadingTime;
		// 重新加载任务数据
		await loadTasks();
		emit('task-updated', checklistId);
	} finally {
		// 确保重置拖拽状态
		isDragging.value = false;
		draggingChecklistId.value = null;
	}
};

const updateTasks = (newTasks) => {
	// 更新内部的tasks状态
	tasks.value = newTasks;
};

const updateTaskFormData = (field, value) => {
	taskFormData.value[field] = value;
};

const resetTaskList = () => {
	tasks.value = [];
	tasksLoaded.value = false;
	editingTask.value = null;
	taskFormData.value = {
		name: '',
		description: '',
		estimatedMinutes: 0,
		isRequired: false,
	};
};

defineExpose({
	loadTasks,
	resetTaskList,
});
</script>

<style scoped lang="scss">
.task-list {
	width: 100%;
}

/* 拖拽样式 */
.tasks-draggable {
	display: flex;
	flex-direction: column;
	gap: 4px;
}

.ghost-task {
	opacity: 0.6;
	background: var(--primary-50, #f0f7ff) !important;
	border: 1px dashed var(--primary-500, #2468f2) !important;
	box-shadow: 0 4px 12px rgba(36, 104, 242, 0.15);
	transform: rotate(1deg);
}

.task-item {
	transition: all 0.3s ease;
	background-color: #ffffff;
	border: 1px solid transparent;
	border-radius: 8px;
	box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
	position: relative;
}

.task-item:hover:not(.task-disabled):not(.task-sorting) {
	background-color: #f9fafb !important;
	border-color: #e5e7eb;
	box-shadow: 0 2px 6px rgba(0, 0, 0, 0.15);
	transform: translateY(-1px);
}

/* 拖拽状态样式 */
.task-disabled {
	opacity: 0.6;
	filter: grayscale(0.3);
}

.task-sorting {
	transition: all 0.3s ease;
	transform: scale(0.98);
}

/* 拖拽手柄样式 */
.drag-handle {
	transition: all 0.2s ease;
	cursor: move !important;
}

.drag-handle:hover:not(.drag-disabled):not(.drag-sorting) {
	background-color: #e5e7eb !important;
	color: #374151 !important;
}

.drag-disabled {
	cursor: not-allowed !important;
	opacity: 0.5;
}

.drag-sorting {
	cursor: move !important;
	opacity: 0.8;
	animation: pulse-sorting 1.5s infinite;
}

@keyframes pulse-sorting {
	0%,
	100% {
		opacity: 0.7;
	}
	50% {
		opacity: 0.4;
	}
}

/* 暗色主题 */
html.dark {
	.task-item {
		@apply bg-black-300;
	}

	.task-item:hover:not(.task-disabled):not(.task-sorting) {
		@apply bg-black-200;
	}
}
</style>
