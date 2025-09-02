<template>
	<div class="customer-block" v-if="hasQuestionnaireData">
		<!-- 统一的头部卡片 -->
		<div
			class="questionnaire-header-card rounded-md"
			:class="{ expanded: isExpanded }"
			@click="toggleExpanded"
		>
			<div class="flex justify-between">
				<div>
					<div class="flex items-center">
						<el-icon class="expand-icon text-lg mr-2" :class="{ rotated: isExpanded }">
							<ArrowRight />
						</el-icon>
						<h3 class="questionnaire-title">{{ questionnaireData.name }}</h3>
					</div>
					<div class="questionnaire-subtitle">
						{{ completionStatus }}
					</div>
				</div>
				<div class="progress-info">
					<span class="progress-percentage">{{ completionRate }}%</span>
					<span class="progress-label">Answer</span>
				</div>
			</div>
			<!-- 进度条 -->
			<div class="progress-bar-container">
				<div class="progress-bar rounded-md">
					<div
						class="progress-fill rounded-md"
						:style="{ width: `${completionRate}%` }"
					></div>
				</div>
			</div>
		</div>

		<!-- 可折叠问卷内容 -->
		<el-collapse-transition>
			<div v-show="isExpanded" class="flex flex-col gap-4 p-4">
				<!-- 问卷表单 -->
				<DynamicForm
					ref="dynamicFormRef"
					:stageId="stageId"
					:onboardingId="onboardingId || ''"
					:questionnaireData="questionnaireData"
					:isStageCompleted="isStageCompleted"
					:questionnaire-answers="questionnaireAnswers"
					:disabled="disabled"
					@stage-updated="handleStageUpdated"
				/>
			</div>
		</el-collapse-transition>

		<!-- 操作按钮 -->
		<!-- <div class="flex justify-end space-x-2 pt-6 mt-6 border-t">
			<el-button @click="handleSave" :loading="saving">
				<el-icon class="mr-1"><Document /></el-icon>
				Save
			</el-button>
		</div> -->
	</div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { ElMessage, ElNotification } from 'element-plus';
import { ArrowRight } from '@element-plus/icons-vue';
import { OnboardingItem, SectionAnswer } from '#/onboard';

import { saveQuestionnaireAnswer } from '@/apis/ow/onboarding';
import DynamicForm from './dynamicForm.vue';

// 组件属性
interface Props {
	stageId: string;
	onboardingId: string;
	leadData: OnboardingItem | null;
	workflowStages: any[];
	questionnaireData?: any;
	questionnaireAnswers?: SectionAnswer;
	disabled?: boolean;
}

const props = defineProps<Props>();

// 组件事件
const emit = defineEmits<{
	stageUpdated: [];
}>();

// 响应式数据
const saving = ref(false);
const dynamicFormRef = ref<InstanceType<typeof DynamicForm>>();
const isExpanded = ref(true); // 默认展开

// 计算属性 - 检查是否有问卷数据
const hasQuestionnaireData = computed(() => {
	return !!props?.questionnaireData;
});

const isStageCompleted = computed(() => {
	if (props.workflowStages.length === 0) {
		return false;
	}

	const sortedStages = [...props.workflowStages].sort((a, b) => a.order - b.order);
	const stageOrder = sortedStages.map((stage) => stage.id);

	const currentStageIndex = stageOrder.indexOf(props?.leadData?.currentStageName || '');
	const thisStageIndex = stageOrder.indexOf(props.stageId);

	if (currentStageIndex === -1 || thisStageIndex === -1) {
		return false;
	}

	return currentStageIndex > thisStageIndex;
});

// 辅助函数：获取问题ID
const getQuestionId = (question: any) => {
	return question.id || question.temporaryId || question.questionId;
};

// 辅助函数：检查答案是否有效
const isAnswerValid = (response: any) => {
	if (!response) return false;

	// 处理数组类型答案
	if (Array.isArray(response.answer)) {
		return response.answer.length > 0;
	}

	// 处理非数组类型答案
	return response.answer !== null && response.answer !== undefined && response.answer !== '';
};

// 辅助函数：查找用户答案
const findUserAnswer = (answers: any, questionId: string) => {
	if (!answers?.answer) return null;
	return answers.answer.find((r: any) => r.questionId === questionId);
};

// 辅助函数：检查是否为可跳转的问题
const isJumpableQuestion = (question: any) => {
	return (
		question.required &&
		(question.type === 'multiple_choice' || question.type === 'checkboxes') &&
		question.jumpRules &&
		question.jumpRules.length > 0
	);
};

// 辅助函数：查找匹配的跳转规则
const findMatchingJumpRule = (question: any, userAnswer: any) => {
	return question.jumpRules.find((rule) => {
		return (
			rule.optionId &&
			question.options.some(
				(option) =>
					(option.id === rule.optionId || option.temporaryId === rule.optionId) &&
					option.value == userAnswer.answer
			)
		);
	});
};

// 辅助函数：查找目标section索引
const findTargetSectionIndex = (sections: any[], targetSectionId: string) => {
	return sections.findIndex((s) => s.id === targetSectionId || s.temporaryId === targetSectionId);
};

// 计算问卷完成状态 - 基于必填问题和跳转逻辑
const completionStats = computed(() => {
	const questionnaireData = JSON.parse(props.questionnaireData.structureJson);
	const answers = props.questionnaireAnswers;

	if (!questionnaireData?.sections || !Array.isArray(questionnaireData.sections)) {
		return { totalRequiredQuestions: 0, answeredRequiredQuestions: 0 };
	}

	let totalRequiredQuestions = 0;
	let answeredRequiredQuestions = 0;

	// 遍历所有section，按照dynamicForm的跳转逻辑处理
	for (let sectionIndex = 0; sectionIndex < questionnaireData.sections.length; sectionIndex++) {
		const section = questionnaireData.sections[sectionIndex];

		if (section.questions && Array.isArray(section.questions)) {
			// 统计当前section的必填问题
			for (const question of section.questions) {
				if (question.type === 'page_break') continue;

				// 只处理必填问题
				if (question.required) {
					totalRequiredQuestions++;

					// 检查是否已回答
					const questionId = getQuestionId(question);
					const userAnswer = findUserAnswer(answers, questionId);

					if (isAnswerValid(userAnswer)) {
						answeredRequiredQuestions++;
					}
				}
			}

			// 检查当前section是否有跳转逻辑（从最后一个问题开始向前查找）
			let jumpTargetSectionId = null;

			for (let i = section.questions.length - 1; i >= 0; i--) {
				const question = section.questions[i];

				// 检查是否是有跳转规则的问题
				if (isJumpableQuestion(question)) {
					const questionId = getQuestionId(question);
					const userAnswer = findUserAnswer(answers, questionId);

					// 检查用户是否已经选择了答案
					if (isAnswerValid(userAnswer)) {
						// 查找匹配的跳转规则
						const matchingRule = findMatchingJumpRule(question, userAnswer);

						// 如果找到匹配的跳转规则，记录目标并跳出循环
						if (matchingRule) {
							jumpTargetSectionId = matchingRule.targetSectionId;
							break;
						}
					}
				}
			}

			// 如果有跳转目标，跳转到目标section（跳过中间sections）
			if (jumpTargetSectionId) {
				const targetSectionIndex = findTargetSectionIndex(
					questionnaireData.sections,
					jumpTargetSectionId
				);

				if (targetSectionIndex !== -1 && targetSectionIndex > sectionIndex) {
					// 直接跳转到目标section，跳过中间的section
					sectionIndex = targetSectionIndex - 1; // -1 是因为for循环会自动+1
				}
			}
		}
	}

	return { totalRequiredQuestions, answeredRequiredQuestions };
});

const completionRate = computed(() => {
	const stats = completionStats.value;
	if (stats.totalRequiredQuestions === 0) return 0;
	return Math.round((stats.answeredRequiredQuestions / stats.totalRequiredQuestions) * 100);
});

const completionStatus = computed(() => {
	const stats = completionStats.value;
	return `${stats.answeredRequiredQuestions} of ${stats.totalRequiredQuestions} required questions answered`;
});

// 切换展开状态
const toggleExpanded = () => {
	isExpanded.value = !isExpanded.value;
};

// 事件处理函数
const handleStageUpdated = () => {
	emit('stageUpdated');
};

const handleSave = async (isTip: boolean = true, isValidate: boolean = true) => {
	try {
		saving.value = true;
		if (isValidate) {
			// 验证动态表单
			const dynamicFormValid = await dynamicFormRef.value?.validateForm();
			if (dynamicFormValid && (!dynamicFormValid?.isValid || !props?.onboardingId)) {
				if (!dynamicFormValid?.isValid) {
					console.log('dynamicFormValid?.errors:', dynamicFormValid?.errors);
					const errorHtml = dynamicFormValid?.errors
						?.map((error) => `<p>${error}</p>`)
						.join('');
					ElNotification({
						title: 'Please complete all required fields',
						dangerouslyUseHTMLString: true,
						message: errorHtml,
						type: 'warning',
					});
				}
				return false;
			}
		}

		const dynamicForm = (await dynamicFormRef.value?.transformFormDataForAPI()) || [];
		if (!Array.isArray(dynamicForm) || dynamicForm.length === 0) {
			return true;
		}
		const res = await Promise.all([
			...dynamicForm.map((item) =>
				saveQuestionnaireAnswer(props?.onboardingId || '', props.stageId, {
					...item,
					onboardingId: props?.onboardingId,
				})
			),
		]);
		if (res[0]?.code == '200') {
			isTip && isValidate && ElMessage.success('Questionnaire saved successfully');
			return true;
		} else {
			isValidate && ElMessage.error(res[0]?.msg || 'Failed to save questionnaire data');
			return false;
		}
	} catch (error) {
		return false;
	} finally {
		saving.value = false;
	}
};

// 暴露给父组件的方法
defineExpose({
	handleSave,
});
</script>

<style scoped lang="scss">
/* 头部卡片样式 - 绿色渐变 */
.questionnaire-header-card {
	background: linear-gradient(135deg, #10b981 0%, #059669 100%);
	padding: 24px;
	color: white;
	box-shadow: 0 4px 12px rgba(16, 185, 129, 0.2);
	display: flex;
	flex-direction: column;
	gap: 16px;
	cursor: pointer;
	transition: all 0.2s ease;

	&:hover {
		box-shadow: 0 6px 16px rgba(16, 185, 129, 0.3);
		transform: translateY(-1px);
	}

	&.expanded {
		border-bottom-left-radius: 0;
		border-bottom-right-radius: 0;
	}
}

.questionnaire-title {
	font-size: 18px;
	font-weight: 600;
	margin: 0 0 4px 0;
	color: white;
}

.questionnaire-subtitle {
	font-size: 14px;
	margin: 0;
	color: rgba(255, 255, 255, 0.9);
	font-weight: 400;
}

.progress-info {
	display: flex;
	flex-direction: column;
	align-items: flex-end;
	text-align: right;
}

.progress-percentage {
	font-size: 24px;
	font-weight: 700;
	line-height: 1;
	color: white;
}

.progress-label {
	font-size: 12px;
	color: rgba(255, 255, 255, 0.9);
	margin-top: 2px;
}

.progress-bar-container {
	width: 100%;
}

.progress-bar {
	width: 100%;
	height: 8px;
	background-color: rgba(255, 255, 255, 0.4);
	overflow: hidden;
}

.progress-fill {
	height: 100%;
	background: linear-gradient(90deg, #34d399 0%, #10b981 100%);
	transition: width 0.3s ease;
}

.expand-icon {
	transition: transform 0.2s ease;
	color: white;

	&.rotated {
		transform: rotate(90deg);
	}
}

/* 优化折叠动画 */
:deep(.el-collapse-transition) {
	transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1) !important;
}

:deep(.el-collapse-transition .el-collapse-item__content) {
	will-change: height;
	transform: translateZ(0); /* 启用硬件加速 */
}

.stage-details-card {
	:deep(.el-card__header) {
		background-color: #f8fafb;
		border-bottom: 1px solid #e5e7eb;
	}
}

/* 暗色主题 */
.dark {
	.questionnaire-header-card {
		background: linear-gradient(135deg, #047857 0%, #065f46 100%);
		box-shadow: 0 4px 12px rgba(5, 150, 105, 0.3);
	}

	.progress-bar {
		background-color: rgba(255, 255, 255, 0.3);
	}

	.progress-fill {
		background: linear-gradient(90deg, #22d3ee 0%, #34d399 100%);
	}

	.stage-details-card {
		:deep(.el-card) {
			background-color: var(--black-400) !important;
			border: 1px solid var(--black-200) !important;
		}

		:deep(.el-card__header) {
			background-color: var(--black-200) !important;
			border-bottom: 1px solid var(--black-100) !important;
			color: var(--white-100) !important;
		}

		:deep(.el-card__body) {
			background-color: var(--black-400) !important;
			color: var(--white-100) !important;
		}
	}

	.text-gray-900,
	.text-gray-700,
	.text-gray-600 {
		color: var(--white-100) !important;
	}

	.text-gray-500 {
		color: #d1d5db !important;
	}

	.text-gray-400 {
		color: #9ca3af !important;
	}
}

/* 响应式设计 */
@media (max-width: 768px) {
	.questionnaire-header-card {
		padding: 16px;
	}

	.progress-info {
		align-items: flex-start;
		text-align: left;
	}
}
</style>
