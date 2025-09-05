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
				<Icon :icon="type.icon" class="type-icon" />
				<div class="type-info">
					<div class="type-content">
						<span class="type-name" :class="getTextSizeClass(type.name)">
							{{ type.name }}
						</span>
					</div>
					<el-tag v-if="type.isNew" size="small" type="success" class="type-new-tag">
						New
					</el-tag>
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

// 根据文字长度动态调整文字大小
const getTextSizeClass = (text: string) => {
	const length = text.length;
	if (length > 20) return 'text-extra-small';
	if (length > 15) return 'text-small';
	if (length > 10) return 'text-medium';
	return 'text-normal';
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
	display: flex;
	align-items: center;
	padding: 0.5rem 0.75rem;
	border: 1px solid var(--primary-100);
	border-radius: 0.375rem;
	cursor: pointer;
	transition: all 0.2s;
	min-height: 2.5rem;
	width: 100%;
	min-width: 0;
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
	flex-shrink: 0;
}

.type-info {
	flex: 1;
	min-width: 0;
	display: flex;
	justify-content: space-between;
	align-items: center;
	gap: 0.5rem;
}

.type-content {
	flex: 1;
	min-width: 0;
	overflow: hidden;
}

.type-name {
	font-size: 0.75rem;
	font-weight: 500;
	color: var(--primary-800);
	line-height: 1.3;
	white-space: nowrap;
	display: block;
	transform-origin: left center;
	transition: transform 0.2s ease;
	@apply dark:text-primary-200;

	/* 动态文字大小类 */
	&.text-normal {
		transform: scale(1);
	}

	&.text-medium {
		transform: scale(0.9);
	}

	&.text-small {
		transform: scale(0.8);
	}

	&.text-extra-small {
		transform: scale(0.65);
	}
}

.type-new-tag {
	flex-shrink: 0;
	font-size: 0.5rem;
	height: 1rem;
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
