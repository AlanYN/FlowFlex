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
				<div class="subtitle">Set up section navigation based on the selected answer</div>
			</div>

			<!-- 启用跳转开关 -->
			<el-switch
				v-model="isJumpEnabled"
				size="large"
				active-text="Enable go to section based on answer"
				@change="handleToggleChange"
			/>

			<!-- 跳转规则配置 -->
			<div v-if="isJumpEnabled" class="rules-configuration">
				<div class="config-title">Configure section navigation for each option:</div>

				<div class="rules-list">
					<div v-for="option in questionOptions" :key="option.id" class="rule-item">
						<div v-if="option.isOther" class="option-label">
							<el-tag type="warning">Other</el-tag>
						</div>
						<div v-else class="option-label">{{ option.label }}</div>
						<el-select
							v-model="jumpRules[option.id]"
							placeholder="Continue to next section"
							class="section-selector"
							clearable
							@change="(value) => handleRuleChange(option.id, value)"
						>
							<el-option
								v-for="section in availableSections"
								:key="section.id"
								:label="section.name"
								:value="section.id"
							/>
						</el-select>
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

// 弹窗标题
const dialogTitle = computed(() => {
	return props.question
		? `Configure Go To Section for: ${props.question.question}`
		: 'Configure Go To Section';
});

// 是否启用跳转功能
const isJumpEnabled = ref(false);

// 跳转规则映射 (optionId -> sectionId)
const jumpRules = ref<Record<string, string>>({});

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
	validationErrors.value = [];
};

// 加载现有规则
const loadExistingRules = (question: QuestionWithJumpRules) => {
	if (question.jumpRules && question.jumpRules.length > 0) {
		isJumpEnabled.value = true;
		const rules: Record<string, string> = {};
		question.jumpRules.forEach((rule) => {
			rules[rule.optionId] = rule.targetSectionId;
		});
		jumpRules.value = rules;
	}
};

// 处理开关变化
const handleToggleChange = (value: boolean) => {
	if (!value) {
		jumpRules.value = {};
		validationErrors.value = [];
	}
};

// 处理规则变化
const handleRuleChange = (optionId: string, sectionId: string | null) => {
	if (sectionId) {
		jumpRules.value[optionId] = sectionId;
	} else {
		delete jumpRules.value[optionId];
	}
	validateRules();
};

// 验证规则
const validateRules = () => {
	validationErrors.value = [];

	if (!isJumpEnabled.value) {
		return;
	}

	// 检查是否有无效的小节ID
	Object.values(jumpRules.value).forEach((sectionId) => {
		const section = props.sections.find((s) => s.temporaryId === sectionId);
		if (!section) {
			validationErrors.value.push(`Invalid section selected: ${sectionId}`);
		}
	});

	// 可以添加更多验证逻辑
	// 比如检查循环引用、检查小节是否存在等
};

// 处理保存
const handleSave = () => {
	if (!props.question) return;

	const rules: JumpRule[] = [];

	if (isJumpEnabled.value) {
		Object.entries(jumpRules.value).forEach(([optionId, sectionId]) => {
			const option = questionOptions.value.find((opt) => opt.id === optionId);
			const section = props.sections.find((s) => s.temporaryId === sectionId);

			if (option && section) {
				rules.push({
					id: `${props.question!.temporaryId}`,
					questionId: props.question!.id || props.question!.temporaryId,
					optionId: optionId,
					optionLabel: option.label,
					targetSectionId: sectionId,
					targetSectionName: section.name,
				});
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
			border-radius: 0.375rem;
			background-color: var(--el-bg-color);

			.option-label {
				min-width: 150px;
				font-weight: 500;
				color: var(--el-text-color-primary);
			}

			.section-selector {
				flex: 1;
				min-width: 200px;
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
