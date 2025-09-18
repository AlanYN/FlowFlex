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
				<el-tag v-if="type.isNew" size="small" type="success" class="type-new-tag">
					New
				</el-tag>
				<div class="type-content">
					<Icon :icon="type.icon" class="type-icon" />
					<span class="type-name">
						{{ type.name }}
					</span>
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
	width: 100%;
}

.section-title {
	font-size: 1.125rem;
	font-weight: 600;
	color: var(--primary-700);
	margin: 0 0 1rem 0;
}

.question-types-grid {
	display: grid;
	grid-template-columns: 1fr 1fr;
	gap: 0.5rem;
	width: 100%;
	align-items: stretch;
}

.question-type-item {
	position: relative;
	display: flex;
	align-items: center;
	padding: 0.5rem 0.75rem;
	border: 1px solid var(--primary-100);
	cursor: pointer;
	transition: all 0.2s;
	min-height: 2.5rem;
	width: 100%;
	min-width: 0;
	box-sizing: border-box;
	@apply dark:border-black-200 rounded-xl;
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

.type-content {
	display: flex;
	align-items: center;
	flex: 1;
	min-width: 0;
	gap: 0.5rem;
}

.type-icon {
	color: var(--primary-600);
	flex-shrink: 0;
	font-size: 1rem;
}

.type-name {
	font-size: 0.75rem;
	font-weight: 500;
	color: var(--primary-800);
	line-height: 1.3;
	flex: 1;
	min-width: 0;
	word-wrap: break-word;
	overflow-wrap: break-word;
	hyphens: auto;
	@apply dark:text-primary-200;
}

.type-new-tag {
	position: absolute;
	top: -0.25rem;
	right: -0.25rem;
	font-size: 0.5rem;
	height: 1rem;
	line-height: 1;
	padding: 0.125rem 0.25rem;
	z-index: 1;
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

.dark .type-icon {
	color: var(--primary-400);
}

.dark .type-name {
	color: var(--primary-100);
}
</style>
