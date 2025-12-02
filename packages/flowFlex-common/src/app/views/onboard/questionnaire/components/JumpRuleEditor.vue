<template>
	<el-dialog
		v-model="dialogVisible"
		:title="dialogTitle"
		:width="dialogWidth"
		draggable
		:close-on-click-modal="false"
		:close-on-press-escape="false"
		@close="handleCancel"
	>
		<div class="jump-rule-editor">
			<div class="editor-header">
				<div class="subtitle">
					{{
						isMultipleChoice
							? 'Set up question navigation based on the selected answer'
							: 'Set up section navigation based on the selected answer'
					}}
				</div>
			</div>

			<!-- 启用跳转开关 -->
			<el-switch
				v-model="isJumpEnabled"
				size="large"
				:active-text="
					isMultipleChoice
						? 'Enable go to question based on answer'
						: 'Enable go to section based on answer'
				"
				@change="(val: string | number | boolean) => handleToggleChange(!!val)"
			/>

			<!-- 跳转规则配置 -->
			<div v-if="isJumpEnabled" class="rules-configuration">
				<div class="config-title">
					{{
						isMultipleChoice
							? 'Configure question navigation for each option:'
							: 'Configure section navigation for each option:'
					}}
				</div>

				<div class="rules-list">
					<div
						v-for="option in questionOptions"
						:key="option.temporaryId"
						class="rule-item"
					>
						<div v-if="option.isOther" class="option-label">
							<el-tag type="warning">Other</el-tag>
						</div>
						<div v-else class="option-label">{{ option.label }}</div>
						<div class="jump-selectors">
							<!-- 选择Section -->
							<el-select
								v-model="selectedSections[option.temporaryId]"
								placeholder="Select section"
								class="flex-1"
								clearable
								@change="(value) => handleSectionChange(option.temporaryId, value)"
							>
								<el-option
									v-for="section in availableSections"
									:key="section.temporaryId"
									:label="section.name"
									:value="section.temporaryId"
								/>
							</el-select>
							<!-- 第二步：选择Question（仅单选题显示） -->
							<el-select
								v-if="isMultipleChoice"
								v-model="jumpRules[option.temporaryId]"
								placeholder="Select question"
								class="flex-1"
								clearable
								:disabled="!selectedSections[option.temporaryId]"
								@change="(value) => handleRuleChange(option.temporaryId, value)"
							>
								<el-option
									v-for="targetQuestion in getAvailableQuestions(
										selectedSections[option.temporaryId]
									)"
									:key="targetQuestion.temporaryId"
									:label="
										getQuestionDisplayLabel(
											targetQuestion,
											selectedSections[option.temporaryId]
										)
									"
									:value="targetQuestion.temporaryId"
								/>
							</el-select>
						</div>
					</div>
				</div>
			</div>

			<!-- 验证错误提示 -->
			<div v-if="validationErrors.length > 0" class="validation-errors">
				<el-alert
					v-for="error in validationErrors"
					:key="error"
					:title="error"
					type="error"
					:closable="false"
					class="error-item"
				/>
			</div>
		</div>

		<template #footer>
			<div class="dialog-footer">
				<el-button @click="handleCancel">Cancel</el-button>
				<el-button type="primary" @click="handleSave" :disabled="!isValid">
					Save Configuration
				</el-button>
			</div>
		</template>
	</el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import type { Section, JumpRule, QuestionWithJumpRules } from '#/section';
import { dialogWidth } from '@/settings/projectSetting';

interface Props {
	visible: boolean;
	question: QuestionWithJumpRules | null;
	sections: Section[];
}

const props = defineProps<Props>();

const emits = defineEmits<{
	'update:visible': [value: boolean];
	save: [rules: JumpRule[]];
}>();

// 弹窗显示状态
const dialogVisible = computed({
	get: () => props.visible,
	set: (value) => emits('update:visible', value),
});

// 判断是否为单选题（只有单选题支持跳转到具体问题）
const isMultipleChoice = computed(() => {
	return props.question?.type === 'multiple_choice';
});

// 弹窗标题
const dialogTitle = computed(() => {
	if (!props.question) return 'Configure Navigation';
	return isMultipleChoice.value
		? `Configure Go To Question for: ${props.question.question}`
		: `Configure Go To Section for: ${props.question.question}`;
});

// 是否启用跳转功能
const isJumpEnabled = ref(false);

// 跳转规则映射 (optionId -> questionId)
const jumpRules = ref<Record<string, string>>({});

// 选中的Section映射 (optionId -> sectionId)
const selectedSections = ref<Record<string, string>>({});

// 验证错误
const validationErrors = ref<string[]>([]);

// 问题选项
const questionOptions = computed(() => {
	return props.question?.options || [];
});

// 可用小节（排除当前问题所在小节）
const availableSections = computed(() => {
	return props.sections.filter((section) => {
		// 可以添加更多过滤逻辑，比如排除当前小节
		return true;
	});
});

// 验证规则是否有效
const isValid = computed(() => {
	return validationErrors.value.length === 0;
});

// 监听问题变化，重置状态
watch(
	() => props.question,
	(newQuestion) => {
		if (newQuestion) {
			resetEditor();
			loadExistingRules(newQuestion);
		}
	},
	{ immediate: true }
);

// 重置编辑器状态
const resetEditor = () => {
	isJumpEnabled.value = false;
	jumpRules.value = {};
	selectedSections.value = {};
	validationErrors.value = [];
};

// 加载现有规则
const loadExistingRules = (question: QuestionWithJumpRules) => {
	if (question.jumpRules && question.jumpRules.length > 0) {
		isJumpEnabled.value = true;
		const rules: Record<string, string> = {};
		const sections: Record<string, string> = {};
		question.jumpRules.forEach((rule) => {
			sections[rule.optionId] = rule.targetSectionId;

			// 只有单选题才加载问题ID
			if (question.type === 'multiple_choice') {
				// 如果规则有targetQuestionId，使用它；否则使用targetSectionId（兼容旧数据）
				if (rule.targetQuestionId) {
					rules[rule.optionId] = rule.targetQuestionId;
				} else {
					// 兼容旧数据：只有sectionId，需要找到该section的第一个问题
					const targetSection = props.sections.find(
						(s) => s.temporaryId === rule.targetSectionId
					);
					if (
						targetSection &&
						targetSection.questions &&
						targetSection.questions.length > 0
					) {
						rules[rule.optionId] = targetSection.questions[0].temporaryId;
					}
				}
			}
		});
		jumpRules.value = rules;
		selectedSections.value = sections;
	}
};

// 处理开关变化
const handleToggleChange = (value: boolean) => {
	if (!value) {
		jumpRules.value = {};
		selectedSections.value = {};
		validationErrors.value = [];
	}
};

// 处理Section选择变化
const handleSectionChange = (optionId: string, sectionId: string | null) => {
	if (sectionId) {
		selectedSections.value[optionId] = sectionId;
		// 清空之前选择的问题
		delete jumpRules.value[optionId];
	} else {
		delete selectedSections.value[optionId];
		delete jumpRules.value[optionId];
	}
	validateRules();
};

// 处理规则变化（问题选择）
const handleRuleChange = (optionId: string, questionId: string | null) => {
	if (questionId) {
		jumpRules.value[optionId] = questionId;
	} else {
		delete jumpRules.value[optionId];
	}
	validateRules();
};

// 获取指定Section下的可用问题列表
const getAvailableQuestions = (sectionId: string | null) => {
	if (!sectionId) return [];
	const section = props.sections.find((s) => s.temporaryId === sectionId);
	if (!section || !section.questions) return [];
	// 排除当前问题本身，避免循环跳转
	return section.questions.filter((q) => q.temporaryId !== props.question?.temporaryId);
};

// 获取问题的显示标签（格式：Section X. Question Y. 问题文本）
const getQuestionDisplayLabel = (targetQuestion: any, sectionId: string) => {
	const section = props.sections.find((s) => s.temporaryId === sectionId);
	if (!section) return targetQuestion.question || '';

	const sectionIndex = props.sections.findIndex((s) => s.temporaryId === sectionId);
	const questionIndex = section.questions.findIndex(
		(q) => q.temporaryId === targetQuestion.temporaryId
	);

	return `${sectionIndex + 1}.${questionIndex + 1}. ${targetQuestion.question || ''}`;
};

// 验证规则
const validateRules = () => {
	validationErrors.value = [];

	if (!isJumpEnabled.value) {
		return;
	}

	// 对于单选题，验证问题ID；对于多选题，只验证section ID
	if (isMultipleChoice.value) {
		// 检查是否有无效的问题ID
		Object.entries(jumpRules.value).forEach(([optionId, questionId]) => {
			const sectionId = selectedSections.value[optionId];
			if (!sectionId) {
				validationErrors.value.push(`Please select a section for option: ${optionId}`);
				return;
			}

			const section = props.sections.find((s) => s.temporaryId === sectionId);
			if (!section) {
				validationErrors.value.push(`Invalid section selected: ${sectionId}`);
				return;
			}

			const question = section.questions.find((q) => q.temporaryId === questionId);
			if (!question) {
				validationErrors.value.push(`Invalid question selected: ${questionId}`);
			}
		});
	} else {
		// 多选题：只验证section ID
		Object.entries(selectedSections.value).forEach(([optionId, sectionId]) => {
			const section = props.sections.find((s) => s.temporaryId === sectionId);
			if (!section) {
				validationErrors.value.push(`Invalid section selected: ${sectionId}`);
			}
		});
	}

	// 可以添加更多验证逻辑
	// 比如检查循环引用、检查问题是否存在等
};

// 处理保存
const handleSave = () => {
	if (!props.question) return;

	const rules: JumpRule[] = [];

	if (isJumpEnabled.value) {
		// 对于多选题，只使用selectedSections；对于单选题，使用jumpRules
		const sourceMap = isMultipleChoice.value ? jumpRules.value : selectedSections.value;

		Object.entries(sourceMap).forEach(([optionId, targetId]) => {
			const option = questionOptions.value.find((opt) => opt.temporaryId === optionId);
			const sectionId = selectedSections.value[optionId];
			const section = props.sections.find((s) => s.temporaryId === sectionId);

			if (option && section) {
				if (isMultipleChoice.value) {
					// 单选题：保存问题ID
					const questionId = targetId;
					const targetQuestion = section.questions.find(
						(q) => q.temporaryId === questionId
					);

					if (targetQuestion) {
						rules.push({
							id: props.question?.id || '',
							questionId: props.question?.temporaryId || props.question!.temporaryId,
							optionId: optionId,
							optionLabel: option.label,
							targetSectionId: sectionId,
							targetSectionName: section.name,
							targetQuestionId: questionId,
							targetQuestionName: targetQuestion.question || '',
						});
					}
				} else {
					// 多选题：只保存section ID，不保存question ID
					rules.push({
						id: props.question?.id || '',
						questionId: props.question?.temporaryId || props.question!.temporaryId,
						optionId: optionId,
						optionLabel: option.label,
						targetSectionId: sectionId,
						targetSectionName: section.name,
						// 不设置 targetQuestionId 和 targetQuestionName
					});
				}
			}
		});
	}

	emits('save', rules);
	dialogVisible.value = false;
};

// 处理取消
const handleCancel = () => {
	dialogVisible.value = false;
};
</script>

<style scoped lang="scss">
.jump-rule-editor {
	.editor-header {
		margin-bottom: 1rem;

		.subtitle {
			color: var(--el-text-color-regular);
			font-size: 0.875rem;
		}
	}

	.rules-configuration {
		.config-title {
			font-weight: 500;
			margin-bottom: 1rem;
			color: var(--el-text-color-primary);
		}

		.rules-list {
			display: flex;
			flex-direction: column;
			gap: 1rem;
		}

		.rule-item {
			display: flex;
			align-items: center;
			gap: 1rem;
			padding: 0.75rem;
			border: 1px solid var(--el-border-color);
			background-color: var(--el-bg-color);
			@apply rounded-xl;

			.option-label {
				min-width: 100px;
				font-weight: 500;
				color: var(--el-text-color-primary);
			}

			.jump-selectors {
				flex: 1;
				display: flex;
				gap: 0.75rem;
				align-items: center;
			}
		}
	}

	.validation-errors {
		margin-top: 1rem;

		.error-item {
			margin-bottom: 0.5rem;
		}
	}
}

.dialog-footer {
	display: flex;
	justify-content: flex-end;
	gap: 0.75rem;
}

// 深色模式支持
.dark {
	.rule-item {
		background-color: var(--el-bg-color-page);
		border-color: var(--el-border-color-darker);
	}
}
</style>
