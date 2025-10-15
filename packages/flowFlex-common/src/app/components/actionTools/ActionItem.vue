<template>
	<div
		class="action-item wfe-global-block-bg p-4 transition-all duration-200"
		:class="{
			'action-dragging': isDragging,
		}"
	>
		<div class="flex items-center justify-between mb-2">
			<div class="flex items-center space-x-3">
				<!-- Drag Handle -->
				<el-button
					circle
					class="cursor-move drag-handle text-gray-400 hover:text-gray-600 dark:text-gray-500 dark:hover:text-gray-300"
					title="Drag to reorder"
					type="primary"
				>
					{{ index + 1 }}
				</el-button>
				<el-icon class="text-primary-500" size="20">
					<component :is="getActionIcon(action?.actionType)" />
				</el-icon>
				<div>
					<h4 class="font-medium text-gray-800 dark:text-white">
						{{ action?.actionName || defaultStr }}
					</h4>
					<p class="text-sm text-gray-500 dark:text-gray-400">
						{{ action?.actionType || defaultStr }}
					</p>
				</div>
			</div>
			<div class="flex items-center space-x-2">
				<el-button @click="onEdit" :type="isSelected ? 'primary' : 'default'" link>
					{{ isSelected ? 'Editing' : 'Edit' }}
				</el-button>
				<el-button link type="danger" @click="onDelete" :icon="Delete" />
			</div>
		</div>

		<p v-if="action.description" class="text-sm text-gray-600 dark:text-gray-300">
			{{ action.description }}
		</p>
	</div>
</template>

<script setup lang="ts">
import { Delete, Operation, Connection, Document } from '@element-plus/icons-vue';
import { ActionListItem } from '#/action';
import { defaultStr } from '@/settings/projectSetting';

interface Props {
	action: ActionListItem;
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

const getActionIcon = (type: string | number) => {
	switch (type) {
		case 'Python':
			return Operation;
		case 'HttpApi':
			return Connection;
		default:
			return Document;
	}
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
</style>
