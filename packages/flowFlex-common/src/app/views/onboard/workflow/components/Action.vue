<template>
	<div class="action-container">
		<!-- Action Header -->
		<div class="action-header">
			<div class="flex items-center justify-between mb-4">
				<h3 class="text-lg font-semibold text-gray-800 dark:text-white">
					Configure Actions
				</h3>
				<el-button type="primary" :icon="Plus" @click="addAction" link>
					Add Action
				</el-button>
			</div>
			<p class="text-sm text-gray-600 dark:text-gray-300 mb-6">
				Set up automated actions that will be triggered when this stage is completed.
			</p>
		</div>

		<!-- Draggable Actions List -->
		<div v-loading="actionListLoading" class="actions-container">
			<div v-if="actions.length === 0" class="empty-state">
				<div class="text-center py-12">
					<el-icon class="text-gray-300 dark:text-gray-600 mb-4" size="48">
						<Document />
					</el-icon>
					<h3 class="text-lg font-medium text-gray-500 dark:text-gray-400 mb-2">
						No Actions Configured
					</h3>
					<p class="text-sm text-gray-400 dark:text-gray-500 mb-4">
						Add actions to automate workflows when this stage is completed.
					</p>
					<el-button type="primary" @click="addAction" :icon="Plus">
						Add Your First Action
					</el-button>
				</div>
			</div>

			<draggable
				v-else
				v-model="actions"
				item-key="id"
				handle=".drag-handle"
				ghost-class="ghost-action"
				@end="onDragEnd"
				class="flex flex-col gap-2"
				:animation="300"
			>
				<template #item="{ element: action, index }">
					<ActionItem
						:action="action"
						:index="index"
						:is-selected="selectedActionIndex === index"
						@edit="editAction"
						@delete="removeAction"
					/>
				</template>
			</draggable>
		</div>

		<!-- Action Configuration Dialog -->
		<ActionConfigDialog
			v-model="showActionDialog"
			:action="currentActionForEdit"
			:is-editing="editingIndex !== -1"
			:stage-id="stageId"
			:workflow-id="workflowId"
			@save="onActionSave"
			@cancel="onActionCancel"
		/>
	</div>
</template>

<script setup lang="ts">
import { ref, nextTick } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Plus, Document } from '@element-plus/icons-vue';
import draggable from 'vuedraggable';
import ActionItem from './ActionItem.vue';
import ActionConfigDialog from './ActionConfigDialog.vue';
import { getActionDefinitions, deleteAction } from '@/apis/action';
import { useI18n } from 'vue-i18n';
import { ActionListItem } from '#/action';

const { t } = useI18n();

// Props
const props = defineProps<{
	stageId?: string;
	workflowId?: string;
}>();

// State
const selectedActionIndex = ref(-1);
const editingIndex = ref(-1);
const showActionDialog = ref(false);
const currentActionForEdit = ref<any>(null);

const actions = ref<ActionListItem[]>([]);
const actionListLoading = ref(false);
const getActionList = async () => {
	if (!props.stageId || !props.workflowId) return;
	try {
		actionListLoading.value = true;
		const res = await getActionDefinitions({
			pageSize: 100,
			pageIndex: 1,
		});
		if (res.code === '200') {
			actions.value = res.data.data.map((item) => ({
				...item,
				actionConfig: JSON.parse(item.actionConfig),
			}));
		}
	} finally {
		actionListLoading.value = false;
	}
};
// Action management methods
const addAction = () => {
	currentActionForEdit.value = null;
	editingIndex.value = -1;
	selectedActionIndex.value = -1;
	showActionDialog.value = true;
};

const editAction = (index: number) => {
	const action = actions.value[index];
	// Convert ActionListItem to ActionItem by adding required type field
	currentActionForEdit.value = {
		...action,
		type: action.actionType === 1 ? 'python' : 'http',
	};
	editingIndex.value = index;
	selectedActionIndex.value = index;
	nextTick(() => {
		showActionDialog.value = true;
	});
};

const removeAction = async (index: number) => {
	try {
		ElMessageBox.confirm(
			'This will permanently delete the action. Continue?',
			'Delete Action',
			{
				confirmButtonText: 'Delete',
				cancelButtonText: 'Cancel',
				type: 'warning',
				beforeClose: async (action, instance, done) => {
					if (action === 'confirm') {
						// 显示loading状态
						instance.confirmButtonLoading = true;
						instance.confirmButtonText = 'Activating...';

						try {
							// 调用激活工作流API
							const res = await deleteAction(actions.value[index].id);

							if (res.code === '200') {
								ElMessage.success(t('sys.api.operationSuccess'));
								// 更新本地状态
								getActionList();
								done(); // 关闭对话框
							} else {
								ElMessage.error(res.msg || t('sys.api.operationFailed'));
								// 恢复按钮状态
								instance.confirmButtonLoading = false;
								instance.confirmButtonText = 'Delete';
							}
						} catch (error) {
							ElMessage.error(t('sys.api.operationFailed'));
							// 恢复按钮状态
							instance.confirmButtonLoading = false;
							instance.confirmButtonText = 'Delete';
						}
					} else {
						done(); // 取消或关闭时直接关闭对话框
					}
				},
			}
		);
	} catch {
		// User cancelled
	}
};

// Dialog event handlers
const onActionSave = (action: ActionListItem) => {
	const updatedActions = [...actions.value];

	if (editingIndex.value !== -1) {
		// Update existing action
		updatedActions[editingIndex.value] = { ...action };
		ElMessage.success('Action updated successfully');
	} else {
		// Add new action
		updatedActions.push({ ...action });
		ElMessage.success('Action added successfully');
	}

	actions.value = updatedActions;
	selectedActionIndex.value = -1;
	resetEditingState();
};

const onActionCancel = () => {
	selectedActionIndex.value = -1;
	resetEditingState();
};

const onDragEnd = () => {
	// Reset selection after drag
	selectedActionIndex.value = -1;
	ElMessage.success('Action order updated successfully');
};

const resetEditingState = () => {
	editingIndex.value = -1;
	currentActionForEdit.value = null;
};

defineExpose({
	getActionList,
});
</script>

<style scoped lang="scss">
.action-container {
	@apply min-h-0 flex flex-col;
}

.action-header {
	@apply flex-shrink-0;
}

.actions-container {
	@apply flex-1 min-h-0;
}

.empty-state {
	@apply flex-1 flex items-center justify-center;
}

// Custom ghost class for actions
.ghost-action {
	opacity: 0.6;
	background: var(--primary-50, #f0f7ff);
	border: 1px dashed var(--primary-500, #2468f2);
}
</style>
