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
			<div class="flex items-center mb-4">
				<h4 class="text-sm font-medium text-gray-900">Tasks</h4>
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
							class="flex items-center gap-3 p-3 transition-all duration-200 border border-transparent rounded-lg task-item max-w-full"
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
										<span class="text-sm text-gray-900 truncate block">
											{{ task.name }}
										</span>
									</div>

									<!-- Right side items -->
									<div
										class="flex items-center gap-1 flex-shrink-0"
										v-if="task.assigneeName"
									>
										<!-- Assignee 缩写 -->
										<icon
											icon="material-symbols:person-2-outline"
											style="color: var(--primary-500)"
										/>
										<span
											class="text-xs font-medium text-primary-500"
											:title="task.assigneeName"
										>
											{{ getAssigneeInitials(task.assigneeName) }}
										</span>

										<!-- Action 绑定状态图标 -->
										<el-tag v-if="task.actionId" type="success" size="small">
											{{ task.actionName }}
										</el-tag>
									</div>
									<div
										class="flex items-center gap-1 flex-shrink-0"
										style="color: #47b064"
										v-if="task?.filesCount"
									>
										<icon icon="iconoir:attachment" />
										{{ task?.filesCount }}
									</div>
									<div
										class="flex items-center gap-1 flex-shrink-0"
										style="color: #ed6f2d"
										v-if="task?.notesCount"
									>
										<icon icon="mynaui:message" />
										{{ task?.notesCount }}
									</div>
								</div>
								<div class="flex items-center space-x-1">
									<el-dropdown placement="bottom">
										<el-button :icon="MoreFilled" link size="small" />
										<template #dropdown>
											<el-dropdown-menu>
												<!-- 如果已绑定 action，显示编辑和删除选项 -->
												<template v-if="true">
													<el-dropdown-item
														@click="openActionEditor(task)"
													>
														<div class="flex items-center gap-2">
															<Icon
																icon="tabler:edit"
																class="w-4 h-4"
															/>
															<span class="text-xs">Edit Action</span>
														</div>
													</el-dropdown-item>
													<el-dropdown-item
														v-if="task.actionId"
														@click="removeActionBinding(task)"
													>
														<div class="flex items-center gap-2">
															<Icon
																icon="tabler:unlink"
																class="w-4 h-4 text-red-500"
															/>
															<span class="text-xs text-red-500">
																Remove Action
															</span>
														</div>
													</el-dropdown-item>
												</template>
											</el-dropdown-menu>
										</template>
									</el-dropdown>
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
								<div class="flex items-center gap-2 flex-1 pr-2">
									<div class="flex-1 min-w-0">
										<el-input
											:model-value="taskFormData.name"
											@update:model-value="
												(val) => updateTaskFormData('name', val)
											"
											placeholder="Task name"
											size="small"
										/>
									</div>
									<div class="flex-1 flex-shrink-0">
										<flowflex-user-selector
											ref="editTaskAssigneeSelectorRef"
											:model-value="taskFormData.assigneeId"
											@update:model-value="
												(val) => updateTaskFormData('assigneeId', val)
											"
											placeholder="Select assignee"
											:clearable="true"
											size="small"
										/>
									</div>
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

			<!-- 添加任务表单 -->
			<div
				v-if="addingTaskTo === props.checklist.id"
				class="border border-gray-200 rounded-lg p-2 mt-4 bg-gray-50"
			>
				<div class="">
					<div class="flex items-center gap-3">
						<div class="flex-1 min-w-0 flex flex-col gap-2">
							<div class="">task name</div>
							<el-input
								v-model="newTaskText"
								placeholder="Enter task name..."
								@keyup.enter="addTask(props.checklist.id)"
							/>
						</div>
						<div class="flex-1 min-w-0 flex-shrink-0 flex flex-col gap-2">
							<div class="">Assignee</div>
							<flowflex-user-selector
								ref="newTaskAssigneeSelectorRef"
								v-model="newTaskAssignee"
								placeholder="Select assignee"
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
						/>
					</div>
				</div>
			</div>

			<!-- Add Task 按钮 -->
			<div class="mt-4 flex justify-end">
				<el-button
					@click="showAddTaskDialog(props.checklist)"
					type="primary"
					:icon="Plus"
					size="default"
				/>
			</div>
		</div>

		<ActionConfigDialog
			ref="actionConfigDialogRef"
			v-model="actionEditorVisible"
			:action="actionInfo"
			:is-editing="!!actionInfo"
			:triggerSourceId="currentActionTask?.id"
			:loading="editActionLoading"
			:triggerType="TriggerTypeEnum.Task"
			@save-success="onActionSave"
			@cancel="onActionCancel"
		/>
	</div>
</template>

<script setup>
import { ref } from 'vue';
import { Plus, Edit, Delete, Loading, Check, Close, MoreFilled } from '@element-plus/icons-vue';
import { Icon } from '@iconify/vue';
import draggable from 'vuedraggable';
import { ElMessage, ElMessageBox } from 'element-plus';
import { getActionDetail, deleteMappingAction } from '@/apis/action';
import {
	getChecklistTasks,
	createChecklistTask,
	updateChecklistTask,
	deleteChecklistTask,
	formatTaskForApi,
} from '@/apis/ow/checklist';
import { useI18n } from '@/hooks/useI18n';
import ActionConfigDialog from '@/components/actionTools/ActionConfigDialog.vue';
import FlowflexUserSelector from '@/components/form/flowflexUser/index.vue';
import { TriggerTypeEnum } from '@/enums/appEnum';

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
	assigneeId: null,
	assigneeName: '',
});
const addingTaskTo = ref(null);
const newTaskText = ref('');
const newTaskAssignee = ref(null);
const isDragging = ref(false);
const draggingChecklistId = ref(null);
const newTaskAssigneeSelectorRef = ref(null);
const editTaskAssigneeSelectorRef = ref(null);

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
			order: tasks.value.length,
			assigneeId: newTaskAssignee.value,
			assigneeName: assigneeName,
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
	taskFormData.value = task;
};

// 取消任务编辑
const cancelTaskEdit = () => {
	editingTask.value = null;
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

		// 重新加载任务数据
		await loadTasks();
		// 通知父组件更新checklist数据
		// emit('task-updated', props.checklist.id);
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
	taskFormData.value = {
		name: '',
		description: '',
		estimatedMinutes: 0,
		isRequired: false,
		assigneeId: null,
		assigneeName: '',
	};
};

// Action 相关状态
const actionEditorVisible = ref(false);
const actionInfo = ref(null);
const editActionLoading = ref(false);
const actionConfigDialogRef = ref(null);
const currentActionTask = ref(null); // 当前正在操作 action 的任务

// 打开 Action 编辑器
const openActionEditor = async (task) => {
	currentActionTask.value = task; // 使用新的变量存储当前操作的任务
	actionEditorVisible.value = true;
	console.log(task);
	// 如果任务已经绑定了 action，获取 action 详情
	if (task.actionId) {
		try {
			editActionLoading.value = true;
			const actionDetailRes = await getActionDetail(task.actionId);
			if (actionDetailRes.code === '200' && actionDetailRes?.data) {
				actionInfo.value = {
					...actionDetailRes?.data,
					actionConfig: JSON.parse(actionDetailRes?.data?.actionConfig || '{}'),
					type: actionDetailRes?.data?.actionType === 1 ? 'python' : 'http',
				};
			}
		} catch (error) {
			console.error('Failed to load action details:', error);
			ElMessage.warning('Failed to load action details');
		} finally {
			editActionLoading.value = false;
		}
	} else {
		actionInfo.value = null;
	}
};

// Action 保存成功回调
const onActionSave = async (actionResult) => {
	if (actionResult.id && currentActionTask.value) {
		try {
			const updateResponse = await updateChecklistTask(currentActionTask.value.id, {
				...currentActionTask.value,
				checklistId: props.checklist.id,
				actionId: actionResult.id,
				actionName: actionResult.name,
				actionMappingId: actionResult?.actionMappingId,
			});
			if (updateResponse.code === '200') {
				ElMessage.success(t('sys.api.operationSuccess'));
				await loadTasks();
			} else {
				ElMessage.error(t('sys.api.operationFailed'));
			}
		} catch (error) {
			console.error('Failed to bind action to task:', error);
			ElMessage.error('Failed to bind action to task');
		}
	}
	onActionCancel();
};

// 取消 Action 编辑
const onActionCancel = () => {
	actionEditorVisible.value = false;
	actionInfo.value = null;
	currentActionTask.value = null; // 使用新变量清理状态
};

// 删除 Action 绑定
const removeActionBinding = async (task) => {
	try {
		await ElMessageBox.confirm(
			'This will remove the action binding from this task. The action itself will not be deleted. Continue?',
			'Remove Action Binding',
			{
				confirmButtonText: 'Remove',
				cancelButtonText: 'Cancel',
				type: 'warning',
				beforeClose: async (action, instance, done) => {
					if (action === 'confirm') {
						instance.confirmButtonLoading = true;
						instance.confirmButtonText = 'Removing...';

						try {
							// 如果有 actionMappingId，删除映射关系
							if (task.actionMappingId) {
								const deleteActionRes = await deleteMappingAction(
									task.actionMappingId
								);
								if (deleteActionRes.code !== '200') {
									throw new Error(
										deleteActionRes.msg || 'Failed to remove action mapping'
									);
								}
							}

							// 更新 task，移除 action 绑定
							const unbindTaskData = formatTaskForApi({
								...task,
								checklistId: props.checklist.id,
								actionId: null,
								actionName: null,
								actionMappingId: null,
							});

							const unbindResponse = await updateChecklistTask(
								task.id,
								unbindTaskData
							);
							if (unbindResponse.code === '200') {
								ElMessage.success('Action binding removed successfully');
								await loadTasks();
								emit('task-updated', props.checklist.id);
								done();
							} else {
								throw new Error('Failed to update task');
							}
						} catch (error) {
							console.error('Failed to remove action binding:', error);
							ElMessage.error('Failed to remove action binding');
							instance.confirmButtonLoading = false;
							instance.confirmButtonText = 'Remove';
						}
					} else {
						done();
					}
				},
			}
		);
	} catch {
		// User cancelled
	}
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
	cursor: pointer;
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
