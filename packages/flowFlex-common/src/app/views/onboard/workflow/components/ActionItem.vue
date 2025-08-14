<template>
	<div
		class="action-item border border-gray-200 dark:border-gray-700 rounded-lg p-4 transition-all duration-200"
		:class="{
			'border-primary-500': isSelected,
			'action-dragging': isDragging,
		}"
	>
		<div class="flex items-center justify-between mb-3">
			<div class="flex items-center space-x-3">
				<!-- Drag Handle -->
				<el-button
					size="small"
					text
					circle
					:icon="Grid"
					class="cursor-move drag-handle text-gray-400 hover:text-gray-600 dark:text-gray-500 dark:hover:text-gray-300"
					title="Drag to reorder"
				/>
				<el-icon class="text-primary-500" size="20">
					<component :is="getActionIcon(action.type)" />
				</el-icon>
				<div>
					<h4 class="font-medium text-gray-800 dark:text-white">
						{{ action.name || 'Untitled Action' }}
					</h4>
					<p class="text-sm text-gray-500 dark:text-gray-400">
						{{ getActionTypeLabel(action.type) }}
					</p>
				</div>
			</div>
			<div class="flex items-center space-x-2">
				<el-button size="small" @click="onEdit" :type="isSelected ? 'primary' : 'default'">
					{{ isSelected ? 'Editing' : 'Edit' }}
				</el-button>
				<el-button size="small" type="danger" @click="onDelete" :icon="Delete" />
			</div>
		</div>

		<p v-if="action.description" class="text-sm text-gray-600 dark:text-gray-300 mb-2">
			{{ action.description }}
		</p>

		<!-- Order Indicator -->
		<div class="order-indicator">
			<span class="text-xs text-gray-400 dark:text-gray-500">Order: {{ index + 1 }}</span>
		</div>
	</div>
</template>

<script setup lang="ts">
import { Grid, Delete, Operation, Connection, Document } from '@element-plus/icons-vue';

interface ActionItem {
	id: string;
	name: string;
	type: 'python' | 'http';
	description: string;
	config: any;
}

interface Props {
	action: ActionItem;
	index: number;
	isSelected?: boolean;
	isDragging?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	isSelected: false,
	isDragging: false,
});

const emit = defineEmits<{
	edit: [index: number];
	delete: [index: number];
}>();

const actionTypes = [
	{ label: 'Python Script', value: 'python', icon: 'Operation' },
	{ label: 'HTTP API', value: 'http', icon: 'Connection' },
];

const getActionIcon = (type: string) => {
	switch (type) {
		case 'python':
			return Operation;
		case 'http':
			return Connection;
		default:
			return Document;
	}
};

const getActionTypeLabel = (type: string) => {
	const actionType = actionTypes.find((t) => t.value === type);
	return actionType?.label || 'Unknown';
};

const onEdit = () => {
	emit('edit', props.index);
};

const onDelete = () => {
	emit('delete', props.index);
};
</script>

<style scoped lang="scss">
.action-item {
	transition: all 0.2s ease;

	&:hover {
		@apply shadow-md;
	}

	&.action-dragging {
		@apply opacity-75;
	}
}

.drag-handle {
	transition: all 0.2s ease;

	&:hover {
		@apply text-primary-500 dark:text-primary-400;
		transform: scale(1.1);
	}

	&:active {
		transform: scale(0.95);
	}
}

.order-indicator {
	@apply mt-2 pt-2 border-t border-gray-100 dark:border-gray-700;
}

.dark {
	.action-item {
		@apply bg-gray-800;
	}
}
</style>
