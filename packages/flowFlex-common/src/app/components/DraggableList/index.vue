<template>
	<div class="draggable-list">
		<!-- Empty State -->
		<div v-if="!modelValue || modelValue.length === 0" class="empty-state">
			<slot name="empty">
				<div class="text-center py-12">
					<el-icon class="text-gray-300 dark:text-gray-600 mb-4" size="48">
						<Document />
					</el-icon>
					<h3 class="text-lg font-medium text-gray-500 dark:text-gray-400 mb-2">
						{{ emptyTitle }}
					</h3>
					<p class="text-sm text-gray-400 dark:text-gray-500 mb-4">
						{{ emptyDescription }}
					</p>
				</div>
			</slot>
		</div>

		<!-- Draggable List -->
		<div v-else class="list-container" :class="listClass">
			<draggable
				:model-value="modelValue"
				:item-key="itemKey"
				:handle="dragHandle"
				:animation="animation"
				ghost-class="ghost-item"
				:disabled="disabled || isDragging"
				@start="onDragStart"
				@change="onDragChange"
				@end="onDragEnd"
				@update:model-value="onUpdate"
				:class="draggableClass"
			>
				<template #item="{ element, index }">
					<div
						:class="[
							'draggable-item',
							itemClass,
							{
								'item-dragging': isDragging,
								'item-selected': selectedIndex === index,
							},
						]"
					>
						<slot :item="element" :index="index" :is-dragging="isDragging">
							<div class="default-item">
								{{ element }}
							</div>
						</slot>
					</div>
				</template>
			</draggable>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { Document } from '@element-plus/icons-vue';
import draggable from 'vuedraggable';
import { ElMessage } from 'element-plus';

interface Props {
	modelValue?: any[];
	itemKey?: string;
	dragHandle?: string;
	animation?: number;
	disabled?: boolean;
	listClass?: string;
	draggableClass?: string;
	itemClass?: string;
	emptyTitle?: string;
	emptyDescription?: string;
	selectedIndex?: number;
	showSuccessMessage?: boolean;
	successMessage?: string;
}

const props = withDefaults(defineProps<Props>(), {
	modelValue: () => [],
	itemKey: 'id',
	dragHandle: '.drag-handle',
	animation: 300,
	disabled: false,
	listClass: '',
	draggableClass: 'space-y-4',
	itemClass: '',
	emptyTitle: 'No Items',
	emptyDescription: 'Add items to get started.',
	selectedIndex: -1,
	showSuccessMessage: true,
	successMessage: 'Items reordered successfully',
});

const emit = defineEmits<{
	'update:modelValue': [value: any[]];
	'drag-start': [event: any];
	'drag-end': [event: any];
	'item-moved': [from: number, to: number];
}>();

const isDragging = ref(false);

const onDragStart = (event: any) => {
	isDragging.value = true;
	emit('drag-start', event);
};

const onDragChange = (event: any) => {
	// Handle drag change event - when items are actually reordered
	if (event.moved) {
		const { oldIndex, newIndex } = event.moved;

		if (props.showSuccessMessage) {
			ElMessage.success(props.successMessage);
		}

		emit('item-moved', oldIndex, newIndex);
	}
};

const onDragEnd = (event: any) => {
	isDragging.value = false;
	emit('drag-end', event);
};

const onUpdate = (newValue: any[]) => {
	emit('update:modelValue', newValue);
};

defineExpose({
	isDragging: computed(() => isDragging.value),
});
</script>

<style scoped lang="scss">
.draggable-list {
	@apply w-full;
}

.list-container {
	@apply w-full;
}

.default-item {
	@apply p-4 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-800;
}

.empty-state {
	@apply flex flex-col items-center justify-center min-h-[200px];
}

/* Enhanced ghost styles for better dragging experience */
.ghost-item {
	opacity: 0.6;
	background: var(--primary-50, #f0f7ff);
	border: 1px dashed var(--primary-500, #2468f2);
	transform: scale(1.02);
	transition: all 0.3s ease;
	box-shadow: 0 4px 12px rgba(36, 104, 242, 0.2);
}

.dark {
	.ghost-item {
		background: var(--primary-900, #1e3a8a);
		border-color: var(--primary-400, #60a5fa);
		box-shadow: 0 4px 12px rgba(96, 165, 250, 0.2);
	}
}

/* Dragging state improvements */
.draggable-item {
	transition: all 0.2s ease;

	&.item-dragging {
		opacity: 0.8;
		transform: scale(0.98);
	}

	&.item-selected {
		@apply ring-2 ring-primary-500 ring-opacity-50;
	}
}
</style>
