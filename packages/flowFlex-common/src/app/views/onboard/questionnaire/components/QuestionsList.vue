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
				<div class="question-item flex">
					<template v-if="editingQuestionId === item.id">
						<div class="w-full">
							<QuestionEditor
								:question-types="questionTypes"
								:pressent-question-type="item.type"
								:editing-question="editingQuestion"
								:is-editing="true"
								@update-question="handleUpdateQuestion"
								@cancel-edit="cancelEditQuestion"
							/>
						</div>
					</template>
					<template v-else>
						<div class="drag-handle">
							<DragIcon class="drag-icon" />
						</div>
						<div class="flex-1">
							<div class="question-header">
								<div class="question-left">
									<div class="question-info">
										<div class="flex items-center gap-2 truncate">
											{{ currentSectionIndex + 1 }}.{{ index + 1 }}.
											<el-tag size="small" class="card-tag">
												{{ getQuestionTypeName(item.type) }}
											</el-tag>
											<el-tag v-if="item.required" size="small" type="danger">
												Required
											</el-tag>
										</div>
										<div class="question-meta mt-2">
											<div class="question-text">{{ item.question }}</div>
										</div>
									</div>
								</div>
								<div class="question-actions">
									<el-dropdown
										v-if="item.type === 'multiple_choice'"
										placement="bottom"
									>
										<el-button :icon="More" link />
										<template #dropdown>
											<el-dropdown-menu>
												<el-dropdown-item
													@click="openJumpRuleEditor(index)"
												>
													<div class="flex items-center gap-2">
														<Icon
															icon="mdi:transit-connection-variant"
															class="drag-icon"
														/>
														<span class="text-xs">
															Go to Section Based on Answer
														</span>
													</div>
												</el-dropdown-item>
											</el-dropdown-menu>
										</template>
									</el-dropdown>
									<div class="flex">
										<el-button
											type="primary"
											link
											@click="editQuestion(index)"
											:icon="Edit"
											class="edit-question-btn"
										/>
									</div>
									<el-button
										type="danger"
										link
										@click="removeQuestion(index)"
										:icon="Delete"
										class="delete-question-btn"
									/>
								</div>
							</div>
							<div v-if="item.description" class="question-description mt-2">
								{{ item.description }}
							</div>
							<div
								v-if="item.options && item.options.length > 0"
								class="question-options"
							>
								<div class="options-label">Options:</div>
								<div class="options-list gap-y-2">
									<div
										v-for="(option, optionIndex) in item.options"
										:key="option.id"
										class="option-item"
									>
										<span class="option-number">{{ optionIndex + 1 }}.</span>
										<el-tag v-if="option.isOther" type="warning">Other</el-tag>
										<span v-else class="option-badge">{{ option.label }}</span>
										<span
											v-if="item.type === 'multiple_choice'"
											class="jump-badge"
											:class="getJumpTargetClass(item, option.id)"
										>
											→ {{ getJumpTargetName(item, option.id) }}
										</span>
									</div>
								</div>
							</div>
						</div>
					</template>
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
		<!-- 跳转规则编辑弹窗 -->
		<JumpRuleEditor
			v-model:visible="jumpRuleEditorVisible"
			:question="currentEditingQuestion"
			:sections="sections"
			@save="handleJumpRulesSave"
		/>
	</div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { Delete, Document, Edit, More } from '@element-plus/icons-vue';
import draggable from 'vuedraggable';
import DragIcon from '@assets/svg/publicPage/drag.svg';
import JumpRuleEditor from './JumpRuleEditor.vue';
import QuestionEditor from './QuestionEditor.vue';
import type { Section, JumpRule, QuestionWithJumpRules } from '#/section';
import { QuestionnaireSection } from '#/section';

interface QuestionType {
	id: string;
	name: string;
	icon: string;
	isNew?: boolean;
}

interface Props {
	questions: QuestionnaireSection[];
	questionTypes: QuestionType[];
	sections: Section[];
	currentSectionIndex: number;
}

const props = defineProps<Props>();

const emits = defineEmits<{
	'remove-question': [index: number];
	'drag-end': [questions: QuestionnaireSection[]];
	'update-jump-rules': [questionIndex: number, rules: JumpRule[]];
}>();

// 编辑状态管理 - 使用ID而不是索引
const editingQuestionId = ref<string | null>(null);
const editingQuestion = ref<QuestionnaireSection | null>(null);

// 跳转规则编辑器状态
const jumpRuleEditorVisible = ref(false);
const currentEditingQuestion = ref<QuestionWithJumpRules | null>(null);
const currentEditingIndex = ref(-1);

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

const editQuestion = (index: number) => {
	const question = questionsData.value[index];
	editingQuestionId.value = question.id;
	editingQuestion.value = { ...question };
};

// 处理问题更新
const handleUpdateQuestion = (updatedQuestion: QuestionnaireSection) => {
	const index = questionsData.value.findIndex((q) => q.id === editingQuestionId.value);
	if (index !== -1) {
		questionsData.value[index] = updatedQuestion;
		editingQuestionId.value = null;
		editingQuestion.value = null;
		handleQuestionDragEnd();
	}
};

// 取消编辑
const cancelEditQuestion = () => {
	editingQuestionId.value = null;
	editingQuestion.value = null;
};

const handleQuestionDragEnd = () => {
	emits('drag-end', questionsData.value);
};

const getQuestionTypeName = (type: string) => {
	const questionType = props.questionTypes.find((t) => t.id === type);
	return questionType ? questionType.name : type;
};

// 打开跳转规则编辑器
const openJumpRuleEditor = (index: number) => {
	const question = questionsData.value[index];
	if (question && question.type === 'multiple_choice') {
		currentEditingQuestion.value = question as QuestionWithJumpRules;
		currentEditingIndex.value = index;
		jumpRuleEditorVisible.value = true;
	}
};

// 处理跳转规则保存
const handleJumpRulesSave = (rules: JumpRule[]) => {
	if (currentEditingIndex.value >= 0) {
		emits('update-jump-rules', currentEditingIndex.value, rules);
		currentEditingQuestion.value = null;
		currentEditingIndex.value = -1;
	}
};

// 获取选项的跳转目标名称
const getJumpTargetName = (question: QuestionnaireSection, optionId: string) => {
	if (!question.jumpRules || question.jumpRules.length === 0) {
		return 'Next';
	}

	const jumpRule = question.jumpRules.find((rule) => rule.optionId === optionId);
	if (jumpRule) {
		return jumpRule.targetSectionName;
	}

	return 'Next';
};

// 获取跳转目标的样式类
const getJumpTargetClass = (question: QuestionnaireSection, optionId: string) => {
	if (!question.jumpRules || question.jumpRules.length === 0) {
		return 'jump-default';
	}

	const jumpRule = question.jumpRules.find((rule) => rule.optionId === optionId);
	if (jumpRule) {
		return 'jump-custom';
	}

	return 'jump-default';
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

.question-actions {
	display: flex;
	align-items: center;
	gap: 0.5rem;
	flex-shrink: 0;
}

.edit-question-btn {
	color: var(--primary-600) !important;
	flex-shrink: 0;
}

.edit-question-btn:hover {
	background-color: var(--primary-50) !important;
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
	flex-direction: column;
}

.option-item {
	display: flex;
	align-items: center;
	gap: 0.5rem;
	font-size: 0.75rem;
}

.option-number {
	font-weight: 600;
	color: #1d4ed8;
	min-width: 1rem;
	flex-shrink: 0;
}

.option-badge {
	display: inline-flex;
	align-items: center;
	padding: 0.125rem 0.625rem;
	border-radius: 9999px;
	border: 1px solid #e5e7eb;
	font-size: 0.75rem;
	font-weight: 600;
	color: #374151;
	background-color: #f9fafb;
	transition: all 0.2s;
}

.jump-badge {
	display: inline-flex;
	align-items: center;
	padding: 0.125rem 0.625rem;
	border-radius: 9999px;
	border: 1px solid transparent;
	font-size: 0.75rem;
	font-weight: 600;
	transition: all 0.2s;
}

.jump-badge.jump-default {
	color: #7c3aed;
	background-color: #ede9fe;
}

.jump-badge.jump-custom {
	color: #7c3aed;
	background-color: #ede9fe;
}

.jump-badge:hover {
	opacity: 0.8;
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

.dark .option-number {
	color: #60a5fa;
}

.dark .option-badge {
	color: #e5e7eb;
	background-color: #374151;
	border-color: #4b5563;
}

.dark .jump-badge.jump-default {
	color: #c4b5fd;
	background-color: #4c1d95;
}

.dark .jump-badge.jump-custom {
	color: #c4b5fd;
	background-color: #4c1d95;
}

.dark .empty-questions {
	color: var(--primary-300);
}

.dark .edit-question-btn {
	color: var(--primary-400) !important;
}

.dark .edit-question-btn:hover {
	background-color: var(--primary-700) !important;
}
</style>
