<template>
	<div class="action-container">
		<!-- Action Header -->
		<div class="action-header">
			<div class="flex items-center justify-between mb-4">
				<h3 class="text-lg font-semibold text-gray-800 dark:text-white">
					Configure Actions
				</h3>
				<el-button type="primary" :icon="Plus" @click="addAction" size="small">
					Add Action
				</el-button>
			</div>
			<p class="text-sm text-gray-600 dark:text-gray-300 mb-6">
				Set up automated actions that will be triggered when this stage is completed.
			</p>
		</div>

		<!-- Draggable Actions List -->
		<DraggableList
			v-model="actions"
			item-key="id"
			drag-handle=".drag-handle"
			ghost-class="ghost-action"
			:selected-index="selectedActionIndex"
			empty-title="No Actions Configured"
			empty-description="Add actions to automate workflows when this stage is completed."
			success-message="Action order updated successfully"
			@item-moved="onActionMoved"
			@drag-end="onDragEnd"
		>
			<template #default="{ item: action, index, isDragging }">
				<ActionItem
					:action="action"
					:index="index"
					:is-selected="selectedActionIndex === index"
					:is-dragging="isDragging"
					@edit="editAction"
					@delete="removeAction"
				/>
			</template>
			<template #empty>
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
			</template>
		</DraggableList>

		<!-- Action Configuration Dialog -->
		<ActionConfigDialog
			v-model="showActionDialog"
			:action="currentActionForEdit"
			:is-editing="editingIndex !== -1"
			:stage-id="stageId"
			@save="onActionSave"
			@cancel="onActionCancel"
		/>
	</div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Plus, Document } from '@element-plus/icons-vue';
import DraggableList from '@/components/DraggableList/index.vue';
import ActionItem from './ActionItem.vue';
import ActionConfigDialog from './ActionConfigDialog.vue';

// Types
interface ActionConfig {
	sourceCode?: string;
	url?: string;
	method?: string;
	headers?: string;
	timeout?: number;
	[key: string]: any;
}

interface ActionItemType {
	id: string;
	name: string;
	type: 'python' | 'http';
	description: string;
	config: ActionConfig;
}

// Props
const props = defineProps<{
	stageId?: string;
	modelValue?: ActionItemType[];
}>();

// Emits
const emit = defineEmits<{
	'update:modelValue': [value: ActionItemType[]];
}>();

// State
const selectedActionIndex = ref(-1);
const editingIndex = ref(-1);
const showActionDialog = ref(false);
const currentActionForEdit = ref<ActionItemType | null>(null);

// Computed two-way binding for actions
const actions = computed({
	get: () => props.modelValue || [],
	set: (newValue: ActionItemType[]) => {
		emit('update:modelValue', [...newValue]);
	},
});

// Action management methods
const addAction = () => {
	currentActionForEdit.value = null;
	editingIndex.value = -1;
	selectedActionIndex.value = -1;
	showActionDialog.value = true;
};

const editAction = (index: number) => {
	const action = actions.value[index];
	currentActionForEdit.value = { ...action };
	editingIndex.value = index;
	selectedActionIndex.value = index;
	showActionDialog.value = true;
};

const removeAction = async (index: number) => {
	try {
		await ElMessageBox.confirm(
			'This will permanently delete the action. Continue?',
			'Delete Action',
			{
				confirmButtonText: 'Delete',
				cancelButtonText: 'Cancel',
				type: 'warning',
			}
		);

		const updatedActions = [...actions.value];
		updatedActions.splice(index, 1);
		actions.value = updatedActions;

		if (selectedActionIndex.value === index) {
			selectedActionIndex.value = -1;
		} else if (selectedActionIndex.value > index) {
			selectedActionIndex.value--;
		}

		ElMessage.success('Action deleted successfully');
	} catch {
		// User cancelled
	}
};

// Dialog event handlers
const onActionSave = (action: ActionItemType) => {
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

const onActionMoved = (fromIndex: number, toIndex: number) => {
	// The DraggableList component already handles the array reordering through v-model
	// We just need to adjust the selectedActionIndex if necessary
	if (selectedActionIndex.value === fromIndex) {
		selectedActionIndex.value = toIndex;
	} else if (selectedActionIndex.value > fromIndex && selectedActionIndex.value <= toIndex) {
		selectedActionIndex.value--;
	} else if (selectedActionIndex.value < fromIndex && selectedActionIndex.value >= toIndex) {
		selectedActionIndex.value++;
	}
};

const onDragEnd = () => {
	// Ensure the parent component is notified of the final state
	// The computed setter will automatically emit the update:modelValue event
	// This is just to ensure any additional logic can be performed
	console.log('Action drag ended, current actions:', actions.value);
};

const resetEditingState = () => {
	editingIndex.value = -1;
	currentActionForEdit.value = null;
};
</script>

<style scoped lang="scss">
.action-container {
	@apply min-h-0 flex flex-col;
}

.action-header {
	@apply flex-shrink-0;
}

// Custom ghost class for actions
:global(.ghost-action) {
	@apply opacity-50 bg-primary-50 dark:bg-primary-900 border-primary-300 dark:border-primary-600;
	transform: rotate(5deg);
}

.dark {
	:global(.ghost-action) {
		@apply bg-primary-900 border-primary-600;
	}
}
</style>
