<template>
	<div class="questions-list">
		<draggable
			v-model="questionsData"
			item-key="id"
			handle=".drag-handle"
			@change="handleQuestionDragEnd"
			ghost-class="ghost-question"
			class="questions-draggable"
			:animation="300"
		>
			<template #item="{ element: item, index }">
				<div class="question-item">
					<div class="question-header">
						<div class="question-left">
							<div class="drag-handle">
								<DragIcon class="drag-icon" />
							</div>
							<div class="question-info">
								<div class="question-text">{{ item.question }}</div>
								<div class="question-meta">
									<el-tag size="small" class="card-tag">
										{{ getQuestionTypeName(item.type) }}
									</el-tag>
									<el-tag v-if="item.required" size="small" type="danger">
										Required
									</el-tag>
								</div>
							</div>
						</div>
						<el-button
							type="danger"
							link
							@click="removeQuestion(index)"
							:icon="Delete"
							class="delete-question-btn"
						/>
					</div>
					<div v-if="item.description" class="question-description">
						{{ item.description }}
					</div>
					<div v-if="item.options && item.options.length > 0" class="question-options">
						<div class="options-label">Options:</div>
						<div class="options-list">
							<el-tag
								v-for="option in item.options"
								:key="option.id"
								size="small"
								class="option-tag"
							>
								{{ option.label }}
							</el-tag>
						</div>
					</div>
				</div>
			</template>
		</draggable>

		<!-- 空状态 -->
		<div v-if="questions.length === 0" class="empty-questions">
			<el-empty description="No questions yet">
				<template #image>
					<el-icon size="48" color="var(--el-color-info)">
						<Document />
					</el-icon>
				</template>
			</el-empty>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { Delete, Document } from '@element-plus/icons-vue';
import draggable from 'vuedraggable';
import DragIcon from '@assets/svg/publicPage/drag.svg';

interface Question {
	id: string;
	type: string;
	question: string;
	description: string;
	required: boolean;
	options: Array<{ id: string; value: string; label: string }>;
}

interface QuestionType {
	id: string;
	name: string;
	icon: string;
	description: string;
	isNew?: boolean;
}

interface Props {
	questions: Question[];
	questionTypes: QuestionType[];
}

const props = defineProps<Props>();

const emits = defineEmits<{
	'remove-question': [index: number];
	'drag-end': [questions: Question[]];
}>();

const questionsData = ref([...props.questions]);

// 监听 props 变化
watch(
	() => props.questions,
	(newQuestions) => {
		questionsData.value = [...newQuestions];
	},
	{ deep: true }
);

const removeQuestion = (index: number) => {
	emits('remove-question', index);
};

const handleQuestionDragEnd = () => {
	emits('drag-end', questionsData.value);
};

const getQuestionTypeName = (type: string) => {
	const questionType = props.questionTypes.find((t) => t.id === type);
	return questionType ? questionType.name : type;
};
</script>

<style scoped lang="scss">
.questions-list {
	margin-bottom: 1.5rem;
}

.questions-draggable {
	display: flex;
	flex-direction: column;
	gap: 0.75rem;
}

.question-item {
	padding: 0.75rem;
	border: 1px solid var(--primary-200);
	border-radius: 0.375rem;
	background-color: white;
	transition: all 0.2s ease;
}

.question-item:hover {
	border-color: var(--primary-300);
	box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
}

.question-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	margin-bottom: 0.5rem;
}

.question-left {
	display: flex;
	align-items: center;
	gap: 0.75rem;
	flex: 1;
	min-width: 0;
}

.drag-handle {
	display: flex;
	align-items: center;
	justify-content: center;
	width: 1.5rem;
	height: 1.5rem;
	cursor: move;
	color: var(--primary-400);
	transition: color 0.2s;
	flex-shrink: 0;
}

.drag-handle:hover {
	color: var(--primary-600);
}

.drag-handle:active {
	cursor: grabbing;
}

.drag-icon {
	width: 1rem;
	height: 1rem;
}

.question-info {
	flex: 1;
	min-width: 0;
}

.question-text {
	font-weight: 500;
	color: var(--primary-800);
	word-break: break-word;
}

.question-meta {
	display: flex;
	gap: 0.5rem;
	flex-wrap: wrap;
}

.card-tag {
	background-color: var(--primary-50);
	color: var(--primary-700);
	border-color: var(--primary-200);
	@apply dark:bg-primary-800 dark:text-primary-200 dark:border-primary-600;
}

.question-description {
	font-size: 0.875rem;
	color: var(--el-text-color-regular);
}

.question-options {
	margin-top: 0.5rem;
}

.options-label {
	font-size: 0.75rem;
	font-weight: 500;
	color: var(--primary-700);
	margin-bottom: 0.25rem;
}

.options-list {
	display: flex;
	flex-wrap: wrap;
	gap: 0.25rem;
}

.option-tag {
	background-color: var(--primary-100);
	color: var(--primary-700);
	border-color: var(--primary-200);
}

.empty-questions {
	text-align: center;
	padding: 2rem;
}

.delete-question-btn {
	color: var(--el-color-danger) !important;
	flex-shrink: 0;
}

.delete-question-btn:hover {
	background-color: var(--el-color-danger-light-9) !important;
}

/* 拖拽时的幽灵样式 */
.ghost-question {
	opacity: 0.6;
	background: var(--primary-50, #f0f7ff);
	border: 1px dashed var(--primary-500, #2468f2);
}

/* 深色模式支持 */
.dark .question-item {
	background-color: var(--primary-800);
	border-color: var(--primary-600);
}

.dark .question-item:hover {
	border-color: var(--primary-500);
}

.dark .question-text {
	color: var(--primary-100);
}

.dark .question-description {
	color: var(--primary-300);
}

.dark .drag-handle {
	color: var(--primary-500);
}

.dark .drag-handle:hover {
	color: var(--primary-400);
}

.dark .ghost-question {
	background: var(--primary-700);
	border: 1px dashed var(--primary-400);
}

.dark .options-label {
	color: var(--primary-200);
}

.dark .option-tag {
	background-color: var(--primary-600);
	color: var(--primary-200);
	border-color: var(--primary-500);
}

.dark .empty-questions {
	color: var(--primary-300);
}
</style>
