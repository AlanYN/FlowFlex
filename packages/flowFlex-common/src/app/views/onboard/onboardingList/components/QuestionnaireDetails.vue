<template>
	<div class="wfe-global-block-bg" v-if="hasQuestionnaireData">
		<!-- 统一的头部卡片 -->
		<div
			class="case-component-header rounded-xl"
			:class="{ expanded: isExpanded }"
			@click="toggleExpanded"
		>
			<div class="flex justify-between">
				<div>
					<div class="flex items-center">
						<el-icon
							class="case-component-expand-icon text-lg mr-2"
							:class="{ rotated: isExpanded }"
						>
							<ArrowRight />
						</el-icon>
						<h3 class="case-component-title">{{ questionnaireData.name }}</h3>
					</div>
					<div class="case-component-subtitle">
						{{ completionStatus }}
					</div>
				</div>
				<div class="case-component-info">
					<span class="case-component-percentage">{{ completionRate }}%</span>
					<span class="case-component-label">Answered</span>
				</div>
			</div>
			<!-- 进度条 -->
			<div class="case-component-bar-container">
				<div class="case-component-bar rounded-xl">
					<div
						class="case-component-fill rounded-xl"
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

// 辅助函数：检查section是否完成
const isSectionCompleted = (section: any, answers: any) => {
	if (!section.questions || !Array.isArray(section.questions)) {
		return true; // 没有问题的section视为完成
	}

	// 检查section内所有必填题是否都已填答
	for (const question of section.questions) {
		if (question.type === 'page_break') continue;

		// 只检查必填问题
		if (question.required) {
			const questionId = getQuestionId(question);
			const userAnswer = findUserAnswer(answers, questionId);

			// 如果有任何必填题未回答，则section未完成
			if (!isAnswerValid(userAnswer)) {
				return false;
			}
		}
	}

	return true; // 所有必填题都已回答
};

// 辅助函数：获取需要跳转的目标section ID
const getJumpTargetSectionId = (section: any, answers: any) => {
	if (!section.questions || !Array.isArray(section.questions)) {
		return null;
	}

	// 从最后一个问题开始向前查找跳转规则
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

				// 如果找到匹配的跳转规则，返回目标section ID
				if (matchingRule) {
					return matchingRule.targetSectionId;
				}
			}
		}
	}

	return null;
};

// 计算问卷完成状态 - 基于section级别的进度计算
const completionStats = computed(() => {
	const questionnaireData = JSON.parse(props.questionnaireData.structureJson);
	const answers = props.questionnaireAnswers;

	if (!questionnaireData?.sections || !Array.isArray(questionnaireData.sections)) {
		return { totalSections: 0, completedSections: 0, skippedSections: 0 };
	}

	// 过滤掉默认section（如果存在isDefault字段）
	const validSections = questionnaireData.sections.filter((section) => !section.isDefault);
	const totalSections = validSections.length;
	if (totalSections === 0) {
		return { totalSections: 0, completedSections: 0, skippedSections: 0 };
	}

	let completedSections = 0;
	let skippedSections = 0;
	const visitedSections = new Set<number>();
	const skippedSectionIndexes = new Set<number>();

	// 遍历所有section，处理跳转逻辑
	for (let sectionIndex = 0; sectionIndex < validSections.length; sectionIndex++) {
		// 跳过已经被标记为跳过的section
		if (skippedSectionIndexes.has(sectionIndex)) {
			continue;
		}

		const section = validSections[sectionIndex];
		visitedSections.add(sectionIndex);

		// 检查当前section是否完成
		if (isSectionCompleted(section, answers)) {
			completedSections++;

			// 检查是否有跳转逻辑
			const jumpTargetSectionId = getJumpTargetSectionId(section, answers);

			if (jumpTargetSectionId) {
				const targetSectionIndex = findTargetSectionIndex(
					validSections,
					jumpTargetSectionId
				);

				if (targetSectionIndex !== -1 && targetSectionIndex > sectionIndex) {
					// 标记中间被跳过的sections
					for (let i = sectionIndex + 1; i < targetSectionIndex; i++) {
						if (!visitedSections.has(i)) {
							skippedSectionIndexes.add(i);
							skippedSections++;
						}
					}

					// 跳转到目标section
					sectionIndex = targetSectionIndex - 1; // -1 是因为for循环会自动+1
				}
			}
		}
	}

	return {
		totalSections,
		completedSections: completedSections + skippedSections, // 跳过的section计为完成
		actualCompletedSections: completedSections,
		skippedSections,
	};
});

const completionRate = computed(() => {
	const stats = completionStats.value;
	if (stats.totalSections === 0) return 0;
	return Math.round((stats.completedSections / stats.totalSections) * 100);
});

const completionStatus = computed(() => {
	const stats = completionStats.value;

	if (stats.totalSections === 0) {
		return 'No sections to complete';
	}

	let statusText = `${stats.completedSections} of ${stats.totalSections} sections completed`;

	// 如果有跳过的section，添加说明
	if (stats.skippedSections > 0) {
		statusText += ` (${stats.skippedSections} skipped)`;
	}

	return statusText;
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

<style scoped lang="scss"></style>
