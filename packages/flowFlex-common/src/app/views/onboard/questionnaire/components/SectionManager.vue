<template>
	<div class="config-section">
		<div class="section-header">
			<h3 class="section-title">Sections</h3>
			<el-button type="primary" size="small" @click="addSection" :icon="Plus">
				Add Section
			</el-button>
		</div>
		<el-scrollbar max-height="300px" class="sections-list-container">
			<div class="sections-list">
				<div
					v-for="(section, index) in sections"
					:key="section.id"
					class="section-item"
					:class="{ active: currentSectionIndex === index }"
					@click="setCurrentSection(index)"
				>
					<div class="section-info">
						<div class="section-name">{{ index + 1 }}. {{ section.name }}</div>
						<div class="section-count">{{ section.items.length }} items</div>
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
			</div>
		</el-scrollbar>
	</div>
</template>

<script setup lang="ts">
import { Plus, Delete } from '@element-plus/icons-vue';
import { Section } from '#/section';

interface Props {
	sections: Section[];
	currentSectionIndex: number;
}

defineProps<Props>();

const emits = defineEmits<{
	'add-section': [];
	'remove-section': [index: number];
	'set-current-section': [index: number];
}>();

const addSection = () => {
	emits('add-section');
};

const removeSection = (index: number) => {
	emits('remove-section', index);
};

const setCurrentSection = (index: number) => {
	emits('set-current-section', index);
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
	justify-content: space-between;
	align-items: center;
	padding: 0.75rem;
	border: 1px solid var(--primary-200);
	border-radius: 0.375rem;
	cursor: pointer;
	transition: all 0.2s;
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
</style>
