<template>
	<div class="config-section">
		<div class="section-header">
			<h3 class="section-title">Sections</h3>
			<el-button type="primary" size="small" @click="addSection" :icon="Plus">
				Add Section
			</el-button>
		</div>
		<el-scrollbar max-height="300px" class="sections-list-container">
			<draggable
				v-model="localSections"
				@end="handleDragEnd"
				item-key="temporaryId"
				handle=".drag-handle"
				class="sections-list"
				ghost-class="ghost-section"
				chosen-class="chosen-section"
				drag-class="drag-section"
				:animation="200"
			>
				<template #item="{ element: section, index }">
					<div
						class="section-item"
						:class="{ active: currentSectionIndex === index }"
						@click="setCurrentSection(index)"
					>
						<div class="drag-handle">
							<Icon icon="mdi:drag-vertical" class="drag-icon" />
						</div>
						<div class="section-info">
							<div class="section-name">{{ index + 1 }}. {{ section.name }}</div>
							<div class="section-count">
								{{ section.questions.length }}
								{{ section.questions.length > 1 ? 'items' : 'item' }}
							</div>
						</div>
						<el-button
							type="primary"
							link
							size="small"
							@click.stop="removeSection(index)"
							:icon="Delete"
							class="delete-btn"
						/>
					</div>
				</template>
			</draggable>
		</el-scrollbar>
	</div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import { Plus, Delete } from '@element-plus/icons-vue';
import draggable from 'vuedraggable';
import { Section } from '#/section';

interface Props {
	sections: Section[];
	currentSectionIndex: number;
}

const props = defineProps<Props>();

const emits = defineEmits<{
	'add-section': [];
	'remove-section': [index: number];
	'set-current-section': [index: number];
	'drag-end': [sections: Section[], dragInfo: { oldIndex: number; newIndex: number }];
}>();

// 本地sections数据，用于拖拽
const localSections = computed({
	get: () => props.sections,
	set: (value: Section[]) => {
		// vuedraggable需要这个setter来更新数据
		// 我们在这里临时存储新的顺序，在handleDragEnd中统一处理
		tempSections.value = value;
	},
});

const tempSections = ref<Section[]>([]);

const addSection = () => {
	emits('add-section');
};

const removeSection = (index: number) => {
	emits('remove-section', index);
};

const setCurrentSection = (index: number) => {
	emits('set-current-section', index);
};

const handleDragEnd = (evt: any) => {
	emits('drag-end', tempSections.value, { oldIndex: evt?.oldIndex, newIndex: evt?.newIndex });
};
</script>

<style scoped lang="scss">
.config-section {
	margin-bottom: 1.5rem;
}

.section-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	margin-bottom: 1rem;
}

.section-title {
	font-size: 1.125rem;
	font-weight: 600;
	color: var(--primary-700);
	margin: 0;
}

.sections-list-container {
	width: 100%;
}

.sections-list {
	display: flex;
	flex-direction: column;
	gap: 0.5rem;
	padding-right: 10px;
}

.section-item {
	display: flex;
	align-items: center;
	padding: 0.75rem;
	border: 1px solid var(--primary-200);
	border-radius: 0.375rem;
	cursor: pointer;
	transition: all 0.2s;
	gap: 0.5rem;
}

.section-item:hover {
	border-color: var(--primary-300);
	background-color: var(--primary-50);
}

.section-item.active {
	border-color: var(--primary-500);
	background-color: var(--primary-50);
}

.section-info {
	flex: 1;
	min-width: 0;
}

.section-name {
	font-size: 0.875rem;
	font-weight: 500;
	color: var(--primary-800);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
	flex: 1;
	min-width: 0;
	line-height: 1.25;
	@apply dark:text-primary-200;
}

.section-count {
	font-size: 0.75rem;
	color: var(--primary-500);
}

.delete-btn:hover {
	background-color: var(--el-color-danger-light-9);
}

/* 深色模式支持 */
.dark .section-title {
	color: var(--primary-200);
}

.dark .section-item {
	border-color: var(--primary-600);
	background-color: var(--primary-800);
}

.dark .section-item:hover {
	border-color: var(--primary-500);
	background-color: var(--primary-700);
}

.dark .section-item.active {
	border-color: var(--primary-400);
	background-color: var(--primary-700);
}

.dark .section-name {
	color: var(--primary-100);
}

.dark .section-count {
	color: var(--primary-300);
}

.dark .sections-list-container {
	background-color: transparent;
}

/* 拖拽相关样式 */
.drag-handle {
	display: flex;
	align-items: center;
	cursor: grab;
	color: var(--primary-400);
	transition: color 0.2s;
}

.drag-handle:hover {
	color: var(--primary-600);
}

.drag-handle:active {
	cursor: grabbing;
}

.drag-icon {
	font-size: 1rem;
}

.ghost-section {
	opacity: 0.5;
	background-color: var(--primary-100);
	border: 2px dashed var(--primary-300);
}

.chosen-section {
	background-color: var(--primary-50);
	border-color: var(--primary-400);
}

.drag-section {
	opacity: 0.8;
	transform: rotate(5deg);
}

/* 深色模式下的拖拽样式 */
.dark .drag-handle {
	color: var(--primary-300);
}

.dark .drag-handle:hover {
	color: var(--primary-200);
}

.dark .ghost-section {
	background-color: var(--primary-700);
	border-color: var(--primary-500);
}

.dark .chosen-section {
	background-color: var(--primary-600);
	border-color: var(--primary-400);
}
</style>
