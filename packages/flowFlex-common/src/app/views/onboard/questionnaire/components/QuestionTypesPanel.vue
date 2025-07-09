<template>
	<div class="config-section">
		<h3 class="section-title">Question Types</h3>
		<div class="question-types-grid">
			<div
				v-for="type in questionTypes"
				:key="type.id"
				class="question-type-item"
				@click="selectQuestionType(type.id)"
				:class="{ active: selectedType === type.id }"
			>
				<el-icon class="type-icon">
					<component :is="type.icon" />
				</el-icon>
				<div class="type-info">
					<div class="type-content">
						<span class="type-name">{{ type.name }}</span>
						<el-tag v-if="type.isNew" size="small" type="success" class="type-new-tag">
							New
						</el-tag>
					</div>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
interface QuestionType {
	id: string;
	name: string;
	icon: string;
	description: string;
	isNew?: boolean;
}

interface Props {
	selectedType: string;
	questionTypes: QuestionType[];
}

defineProps<Props>();

const emits = defineEmits<{
	'select-type': [typeId: string];
}>();

const selectQuestionType = (typeId: string) => {
	emits('select-type', typeId);
};
</script>

<style scoped lang="scss">
.config-section {
	margin-bottom: 1.5rem;
}

.section-title {
	font-size: 1.125rem;
	font-weight: 600;
	color: var(--primary-700);
	margin: 0 0 1rem 0;
}

.question-types-grid {
	display: grid;
	grid-template-columns: repeat(2, 1fr);
	gap: 0.5rem;
}

.question-type-item {
	display: flex;
	align-items: center;
	padding: 0.75rem;
	border: 1px solid var(--primary-100);
	border-radius: 0.375rem;
	cursor: pointer;
	transition: all 0.2s;
	min-height: 3rem;
	width: 100%;
	box-sizing: border-box;
	@apply dark:border-black-200;
}

.question-type-item:hover {
	background-color: var(--primary-50);
	border-color: var(--primary-300);
	@apply dark:bg-primary-800 dark:border-primary-500;
}

.question-type-item.active {
	background-color: var(--primary-100);
	border-color: var(--primary-500);
	@apply dark:bg-primary-700 dark:border-primary-400;
}

.type-icon {
	color: var(--primary-600);
	margin-right: 0.5rem;
}

.type-info {
	flex: 1;
	min-width: 0;
}

.type-content {
	display: flex;
	align-items: center;
	gap: 0.375rem;
	min-height: 1.5rem;
}

.type-name {
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

.type-new-tag {
	flex-shrink: 0;
	font-size: 0.625rem;
	height: 1.125rem;
	line-height: 1;
	padding: 0.125rem 0.25rem;
}

/* 深色模式支持 */
.dark .section-title {
	color: var(--primary-200);
}

.dark .question-type-item {
	border-color: var(--primary-600);
	background-color: var(--primary-800);
}

.dark .question-type-item:hover {
	background-color: var(--primary-700);
	border-color: var(--primary-500);
}

.dark .question-type-item.active {
	background-color: var(--primary-600);
	border-color: var(--primary-400);
}

.dark .type-name {
	color: var(--primary-100);
}
</style>
