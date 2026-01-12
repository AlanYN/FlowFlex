<template>
	<div class="task-list">
		<div class="flex items-center justify-center py-8" v-if="!tasksLoaded">
			<el-icon class="animate-spin h-6 w-6 text-primary-500 mr-2">
				<Loading />
			</el-icon>
			Loading tasks...
		</div>

		<!-- 任务已加载完成时显示 -->
		<div v-if="tasksLoaded">
			<div class="flex items-center mb-4">
				<h4 class="text-sm font-medium task-list-title">Tasks</h4>
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
					:disabled="!functionPermission(ProjectPermissionEnum.checkList.update)"
				>
					<template #item="{ element: task }">
						<div
							class="flex items-center gap-3 p-3 transition-all duration-200 border border-transparent rounded-xl task-item max-w-full"
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
								<div class="flex items-center justify-between flex-1 min-w-0 gap-2">
									<!-- Task name -->
									<div class="flex-1 min-w-0 pr-3">
										<span
											class="text-sm truncate block task-name-text"
											:title="task?.name || ''"
										>
											{{ task.name }}
										</span>
									</div>

									<!-- Right side items -->
									<div class="flex items-center gap-1 flex-shrink-0">
										<FlowflexUserSelector
											v-if="task.assigneeId"
											v-model="task.assigneeId"
											selection-type="user"
											readonly
										/>
									</div>
									<div
										class="flex items-center gap-1 flex-shrink-0 task-files-icon"
										v-if="task?.filesCount"
									>
										<icon icon="iconoir:attachment" />
										{{ task?.filesCount }}
									</div>
									<div
										class="flex items-center gap-1 flex-shrink-0 task-notes-icon"
										v-if="task?.notesCount"
									>
										<icon icon="mynaui:message" />
										{{ task?.notesCount }}
									</div>
								</div>
								<div class="flex items-center space-x-2">
									<el-button
										@click="editTask(props.checklist.id, task)"
										link
										v-if="
											functionPermission(
												ProjectPermissionEnum.checkList.update
											)
										"
										:icon="Edit"
									/>
									<el-button
										@click="deleteTask(props.checklist.id, task.id)"
										link
										:icon="Delete"
										class="text-red-500"
										v-if="
											functionPermission(
												ProjectPermissionEnum.checkList.delete
											)
										"
									/>
								</div>
							</template>

							<!-- 编辑模式 -->
							<template v-else>
								<div class="flex items-center gap-2 flex-1 pr-2">
									<div class="flex-1 min-w-0">
										<el-input
											:model-value="taskFormData.name"
											@update:model-value="
												(val) => updateTaskFormData('name', val)
											"
											placeholder="Task name"
										/>
									</div>
									<div class="flex-1 flex-shrink-0">
										<flowflex-user-selector
											ref="editTaskAssigneeSelectorRef"
											:model-value="
												taskFormData.assigneeId
													? String(taskFormData.assigneeId)
													: undefined
											"
											@update:model-value="
												(val) =>
													updateTaskFormData(
														'assigneeId',
														val
															? typeof val === 'string'
																? val
																: String(val)
															: null
													)
											"
											placeholder="Select assignee"
											:clearable="true"
											:max-count="1"
										/>
									</div>
								</div>
								<div class="flex items-center gap-1">
									<el-button
										@click="saveTaskEdit"
										type="primary"
										size="small"
										circle
										:icon="Check"
									/>
									<el-button
										@click="cancelTaskEdit"
										size="small"
										circle
										:icon="Close"
									/>
								</div>
							</template>
						</div>
					</template>
				</draggable>
			</div>
			<div v-else class="text-center py-8 task-empty-text">
				<p class="text-sm">
					No tasks added yet. Click the "Add Task" button to add a task.
				</p>
			</div>

			<!-- 添加任务表单 -->
			<div
				v-if="addingTaskTo === props.checklist.id"
				class="task-add-form rounded-xl p-2 mt-4"
			>
				<div class="">
					<div class="flex items-center gap-3">
						<div class="flex-1 min-w-0 flex flex-col gap-2">
							<div class="task-form-label">Task name</div>
							<el-input
								v-model="newTaskText"
								placeholder="Enter task name..."
								@keyup.enter="addTask(props.checklist.id)"
							/>
						</div>
						<div class="flex-1 min-w-0 flex-shrink-0 flex flex-col gap-2">
							<div class="task-form-label">Assignee</div>
							<flowflex-user-selector
								ref="newTaskAssigneeSelectorRef"
								:model-value="newTaskAssignee ? String(newTaskAssignee) : undefined"
								@update:model-value="
									(val) =>
										(newTaskAssignee = val
											? typeof val === 'string'
												? val
												: String(val)
											: null)
								"
								placeholder="Select assignee"
								:max-count="1"
								:clearable="true"
							/>
						</div>
					</div>
					<div class="flex justify-end mt-2">
						<el-button @click="cancelAddTask" :icon="Close" size="small" />
						<el-button
							@click="addTask(props.checklist.id)"
							type="primary"
							:icon="Plus"
							size="small"
							v-if="functionPermission(ProjectPermissionEnum.checkList.create)"
						/>
					</div>
				</div>
			</div>

			<!-- Add Task 按钮 -->
			<div class="mt-4 flex justify-end" v-if="!addingTaskTo">
				<el-button
					@click="showAddTaskDialog(props.checklist)"
					type="primary"
					:icon="Plus"
					circle
					v-if="functionPermission(ProjectPermissionEnum.checkList.create)"
				/>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
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
import FlowflexUserSelector from '@/components/form/flowflexUser/index.vue';
import { functionPermission } from '@/hooks';
import { ProjectPermissionEnum } from '@/enums/permissionEnum';
import type { ChecklistTaskOutputDto } from '@/apis/ow/checklist';

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
type TaskType = ChecklistTaskOutputDto & {
	actionMappingId?: string;
	filesCount?: number;
	notesCount?: number;
	orderIndex?: number;
};

const tasks = ref<TaskType[]>([]);
const tasksLoaded = ref(false);
const editingTask = ref<TaskType | null>(null);
const originalTaskData = ref<TaskType | null>(null); // 保存原始任务数据的副本
const taskFormData = ref({
	name: '',
	description: '',
	estimatedMinutes: 0,
	isRequired: false,
	assigneeId: null as string | number | null,
	assigneeName: '',
});
const addingTaskTo = ref<string | null>(null);
const newTaskText = ref('');
const newTaskAssignee = ref<string | number | null>(null);
const isDragging = ref(false);
const draggingChecklistId = ref<string | null>(null);
const newTaskAssigneeSelectorRef = ref<InstanceType<typeof FlowflexUserSelector> | null>(null);
const editTaskAssigneeSelectorRef = ref<InstanceType<typeof FlowflexUserSelector> | null>(null);

// 加载任务数据
// 加载任务（初始加载，显示loading状态）
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
				orderIndex: task.orderIndex,
			}))
			.sort((a, b) => (a.orderIndex || 0) - (b.orderIndex || 0));

		tasks.value = processedTasks;
		tasksLoaded.value = true;
	} catch (error) {
		ElMessage.error(`Failed to load tasks: ${error.message || 'Unknown error'}`);
		tasks.value = [];
		tasksLoaded.value = true;
	}
};

// 刷新任务（静默刷新，不改变loading状态，避免UI闪烁）
const refreshTasks = async () => {
	try {
		const response = await getChecklistTasks(props.checklist.id);
		const tasksData = response.data || response || [];

		const processedTasks = tasksData
			.map((task) => ({
				...task,
				completed: task.isCompleted || task.completed || false,
				estimatedMinutes: task.estimatedHours ? task.estimatedHours * 60 : 0,
				orderIndex: task.orderIndex,
			}))
			.sort((a, b) => (a.orderIndex || 0) - (b.orderIndex || 0));

		tasks.value = processedTasks;
	} catch (error) {
		console.error('Failed to refresh tasks:', error);
		ElMessage.error(`Failed to refresh tasks: ${error.message || 'Unknown error'}`);
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
	newTaskAssignee.value = null;
};

// 添加任务
const addTask = async (checklistId) => {
	if (!newTaskText.value.trim()) return;

	try {
		// 获取assignee的用户信息（如果选择了的话）
		let assigneeName = null;
		if (newTaskAssignee.value) {
			assigneeName = getAssigneeNameFromSelector(
				newTaskAssigneeSelectorRef,
				newTaskAssignee.value
			);
		}

		const taskData = formatTaskForApi({
			checklistId: checklistId,
			name: newTaskText.value.trim(),
			description: '',
			isRequired: false,
			orderIndex: tasks.value.length,
			assigneeId: newTaskAssignee.value,
			assigneeName: assigneeName,
		});

		await createChecklistTask(taskData);
		ElMessage.success(t('sys.api.operationSuccess'));

		// 静默刷新任务数据，避免UI闪烁
		await refreshTasks();
		// 通知父组件更新checklist数据
		emit('task-updated', checklistId);
		cancelAddTask();
	} catch (err) {
		cancelAddTask();
	}
};

// 删除任务
const deleteTask = async (checklistId: string, taskId: string) => {
	try {
		await ElMessageBox.confirm(
			'Are you sure you want to delete this task? This action cannot be undone.',
			'⚠️ Confirm Deletion',
			{
				confirmButtonText: 'Delete Task',
				cancelButtonText: 'Cancel',
				confirmButtonClass: 'danger-confirm-btn',
				cancelButtonClass: 'cancel-confirm-btn',
				distinguishCancelAndClose: true,
				customClass: 'delete-confirmation-dialog',
				showCancelButton: true,
				showConfirmButton: true,
				beforeClose: async (action, instance, done) => {
					if (action === 'confirm') {
						instance.confirmButtonLoading = true;
						instance.confirmButtonText = 'Deleting...';
						try {
							await deleteChecklistTask(taskId, true);
							ElMessage.success('Task deleted successfully');
							done();
							// 静默刷新任务数据，避免UI闪烁
							await refreshTasks();
							// 通知父组件更新checklist数据
							emit('task-updated', checklistId);
						} catch {
							ElMessage.error('Failed to delete task');
							instance.confirmButtonLoading = false;
							instance.confirmButtonText = 'Delete Task';
						}
					} else {
						done();
					}
				},
			}
		);
	} catch {
		// 用户取消删除
	}
};

// 编辑任务
const editTask = (checklistId, task) => {
	editingTask.value = task;
	// 创建原始任务数据的深拷贝以备恢复使用
	originalTaskData.value = JSON.parse(JSON.stringify(task));
	// 创建编辑表单数据的深拷贝，避免直接修改原始数据
	taskFormData.value = JSON.parse(JSON.stringify(task));
};

// 取消任务编辑
const cancelTaskEdit = () => {
	// 如果有原始数据，恢复到原始状态
	if (originalTaskData.value && editingTask.value) {
		// 在tasks数组中找到对应的任务并恢复数据
		const taskIndex = tasks.value.findIndex((task) => task.id === editingTask.value!.id);
		if (taskIndex !== -1) {
			// 恢复原始数据到tasks数组中
			tasks.value[taskIndex] = JSON.parse(JSON.stringify(originalTaskData.value));
		}
	}

	// 重置编辑状态
	editingTask.value = null;
	originalTaskData.value = null;
	taskFormData.value = {
		name: '',
		description: '',
		estimatedMinutes: 0,
		isRequired: false,
		assigneeId: null,
		assigneeName: '',
	};
};

// 保存任务编辑
const saveTaskEdit = async () => {
	if (!editingTask.value) return;

	try {
		// 获取assignee的用户信息（如果选择了的话）
		let assigneeName = null;
		if (taskFormData.value.assigneeId) {
			assigneeName = getAssigneeNameFromSelector(
				editTaskAssigneeSelectorRef,
				taskFormData.value.assigneeId
			);
		}

		const taskData = formatTaskForApi({
			...editingTask.value,
			checklistId: props.checklist.id,
			name: taskFormData.value.name,
			description: taskFormData.value.description,
			estimatedMinutes: taskFormData.value.estimatedMinutes,
			isRequired: taskFormData.value.isRequired,
			assigneeId: taskFormData.value.assigneeId,
			assigneeName: assigneeName,
		});

		await updateChecklistTask(editingTask.value.id, taskData);
		ElMessage.success(t('sys.api.operationSuccess'));

		// 静默刷新任务数据，避免UI闪烁
		await refreshTasks();
		// 通知父组件更新checklist数据
		emit('task-updated', props.checklist.id);

		// 成功保存后清理编辑状态
		editingTask.value = null;
		originalTaskData.value = null;
		taskFormData.value = {
			name: '',
			description: '',
			estimatedMinutes: 0,
			isRequired: false,
			assigneeId: null,
			assigneeName: '',
		};
	} catch {
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
			orderIndex: index,
		}));

		// 更新后端数据 - 为每个任务分配新的顺序号
		const updatePromises = reorderedTasks.map((task, index) => {
			const updatedTask = formatTaskForApi({
				...task,
				checklistId: checklistId,
				orderIndex: index,
			});
			return updateChecklistTask(task.id, updatedTask);
		});

		// 等待API调用和最小loading时间
		await Promise.all([Promise.all(updatePromises), minLoadingTime]);
		ElMessage.success('Task order updated successfully');

		// 静默刷新任务数据，确保与后端完全一致
		await refreshTasks();
		// 通知父组件更新checklist数据
		emit('task-updated', checklistId);
	} catch (err) {
		console.error('Failed to update task order:', err);
		ElMessage.error('Failed to save new order');
		// API调用失败时，显示loading并重新加载任务数据以恢复正确的顺序
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

// 从 FlowflexUser 组件获取选中用户的姓名
const getAssigneeNameFromSelector = (selectorRef, assigneeId) => {
	if (!selectorRef.value || !assigneeId) return null;

	try {
		const selectedData = selectorRef.value.getSelectedData();
		if (selectedData && selectedData.name) {
			return selectedData.name;
		}
		// 如果无法从组件获取，使用用户ID作为fallback
		return selectorRef.value.getUserNameById(assigneeId) || `User-${assigneeId}`;
	} catch (error) {
		console.warn('Failed to get assignee name from selector:', error);
		return `User-${assigneeId}`;
	}
};

const resetTaskList = () => {
	tasks.value = [];
	tasksLoaded.value = false;
	editingTask.value = null;
	originalTaskData.value = null; // 也重置原始数据
	taskFormData.value = {
		name: '',
		description: '',
		estimatedMinutes: 0,
		isRequired: false,
		assigneeId: null,
		assigneeName: '',
	};
};

defineExpose({
	loadTasks,
	refreshTasks,
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
	background: var(--primary-50) !important;
	border: 1px dashed var(--primary-500) !important;
	box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
	transform: rotate(1deg);
}

.task-item {
	transition: all 0.3s ease;
	background-color: var(--el-color-white);
	border: 1px solid transparent;
	box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
	position: relative;
	cursor: pointer;
	@apply rounded-xl;
}

.task-item:hover:not(.task-disabled):not(.task-sorting) {
	background-color: var(--el-fill-color-lighter) !important;
	border-color: var(--el-border-color-light);
	box-shadow: 0 2px 6px rgba(0, 0, 0, 0.15);
	transform: translateY(-1px);
}

/* 拖拽状态样式 */
.task-disabled {
	opacity: 0.6;
	filter: grayscale(0.3);
	cursor: not-allowed;
}

.task-sorting {
	transition: all 0.3s ease;
	transform: scale(0.98);
	cursor: grabbing;
}

/* 拖拽手柄样式 */
.drag-handle {
	transition: all 0.2s ease;
	cursor: move !important;
}

.drag-handle:hover:not(.drag-disabled):not(.drag-sorting) {
	background-color: var(--el-fill-color-light) !important;
	color: var(--el-text-color-regular) !important;
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

/* TaskList custom classes */
.task-files-icon {
	color: var(--el-color-success);
}

.task-notes-icon {
	color: var(--el-color-warning);
}

.task-list-title {
	color: var(--el-text-color-primary);
}

html.dark .task-list-title {
	color: var(--el-color-white);
}

.task-name-text {
	color: var(--el-text-color-primary);
}

html.dark .task-name-text {
	color: var(--el-color-white);
}

.task-empty-text {
	color: var(--el-text-color-secondary);
}

html.dark .task-empty-text {
	color: var(--el-text-color-placeholder);
}

.task-add-form {
	background-color: var(--el-fill-color-lighter);
	border: 1px solid var(--el-border-color-light);
}

html.dark .task-add-form {
	background-color: var(--el-bg-color-page);
	border-color: var(--el-border-color-darker);
}

.task-form-label {
	color: var(--el-text-color-regular);
}

html.dark .task-form-label {
	color: var(--el-text-color-placeholder);
}

/* 暗色主题 */
html.dark {
	.task-item {
		background-color: var(--el-bg-color-page);
		border-color: var(--el-border-color-darker);
		box-shadow: 0 1px 3px rgba(0, 0, 0, 0.3);
	}

	.task-item:hover:not(.task-disabled):not(.task-sorting) {
		background-color: var(--el-fill-color) !important;
		border-color: var(--el-border-color) !important;
		box-shadow: 0 2px 6px rgba(0, 0, 0, 0.4) !important;
		transform: translateY(-1px);
	}

	.drag-handle:hover:not(.drag-disabled):not(.drag-sorting) {
		background-color: var(--el-fill-color) !important;
		color: var(--el-text-color-secondary) !important;
	}

	.ghost-task {
		background: var(--el-color-primary-dark-2) !important;
		border: 1px dashed var(--el-color-primary) !important;
		box-shadow: 0 4px 12px rgba(0, 0, 0, 0.25);
	}
}
</style>
