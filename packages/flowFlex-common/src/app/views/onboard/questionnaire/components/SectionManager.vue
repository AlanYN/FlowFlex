<template>
	<div class="config-section">
		<div class="section-header">
			<h3 class="section-title">Sections</h3>
			<el-button
				v-if="functionPermission(ProjectPermissionEnum.question.create)"
				type="primary"
				size="small"
				@click="addSection"
				:icon="Plus"
			>
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
						:class="{
							active: currentSectionIndex === index,
							'drag-over': dragOverSectionIndex === index,
						}"
						@click="setCurrentSection(index)"
						:data-section-index="index"
						@drop="handleDropOnSection($event, index)"
						@dragover.prevent="handleDragOver($event, index)"
						@dragenter.prevent="handleDragEnter(index, $event)"
						@dragleave="handleDragLeave(index, $event)"
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
							v-if="functionPermission(ProjectPermissionEnum.question.delete)"
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
import { functionPermission } from '@/hooks';
import { ProjectPermissionEnum } from '@/enums/permissionEnum';

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
	'question-drop': [targetSectionIndex: number];
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
const dragOverSectionIndex = ref<number>(-1);

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

// 处理拖拽进入分区
const handleDragEnter = (sectionIndex: number, evt: DragEvent) => {
	evt.preventDefault();
	// 检查是否是问题拖拽（通过检查拖拽数据或事件类型）
	if (
		evt.dataTransfer?.types?.includes('text/plain') ||
		evt.dataTransfer?.effectAllowed === 'move'
	) {
		dragOverSectionIndex.value = sectionIndex;
	}
};

// 处理拖拽离开分区
const handleDragLeave = (sectionIndex: number, evt: DragEvent) => {
	// 只有当离开的是当前悬停的分区时才清除
	if (dragOverSectionIndex.value === sectionIndex) {
		// 检查是否真的离开了分区（而不是进入子元素）
		const relatedTarget = evt.relatedTarget as HTMLElement;
		const currentTarget = evt.currentTarget as HTMLElement;
		if (!relatedTarget || !currentTarget?.contains(relatedTarget)) {
			dragOverSectionIndex.value = -1;
		}
	}
};

// 处理拖拽悬停在分区上
const handleDragOver = (evt: DragEvent, sectionIndex: number) => {
	evt.preventDefault();
	evt.stopPropagation();
	if (evt.dataTransfer) {
		evt.dataTransfer.dropEffect = 'move';
	}
};

// 处理问题拖拽到分区（使用原生拖拽API）
const handleDropOnSection = (evt: DragEvent, sectionIndex: number) => {
	evt.preventDefault();
	evt.stopPropagation();
	dragOverSectionIndex.value = -1;

	// 通知父组件有问题被拖拽到分区
	// 父组件会通过 drag-start 事件记录的问题数据来处理
	emits('question-drop', sectionIndex);
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
	border: 1px solid var(--el-border-color-light);
	cursor: pointer;
	transition: all 0.2s;
	gap: 0.5rem;
	@apply rounded-xl;
	position: relative;
}

.section-item.drag-over {
	border-color: var(--el-color-primary);
	background-color: var(--el-color-primary-light-9);
}

.section-item:hover {
	border-color: var(--el-color-primary);
	background-color: var(--el-bg-color-light);
}

.section-item.active {
	border-color: var(--el-color-primary);
	background-color: var(--el-bg-color-light);
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

/* 深色模式支持 */
.dark .section-title {
	color: var(--primary-200);
}

.dark .section-item {
	border-color: var(--el-border-color-dark);
	background-color: var(--black);
}

.dark .section-item:hover {
	border-color: var(--el-border-color-dark);
	background-color: var(--el-color-primary);

	.delete-btn {
		color: var(--el-color-white);
	}
}

.dark .section-item.active {
	border-color: var(--el-border-color-dark);
	background-color: var(--el-color-primary);
}

.dark .section-name {
	color: var(--el-color-white);
}

.dark .section-count {
	color: var(--el-color-white);
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
