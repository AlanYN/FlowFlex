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
						<!-- <el-button
							@click="handleSubmit()"
							type="primary"
							:icon="Document"
							size="small"
							:loading="loading"
							:disabled="
								!canSubmitQuestionnaire ||
								disabled ||
								questionnaireAnswers?.status === 'Submitted'
							"
							class="ml-2"
						>
							Submit
						</el-button> -->
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
					:isSubmitEnabled="canSubmitQuestionnaire"
					:skippedQuestions="skippedQuestionsSet"
					:disabled="disabled || questionnaireAnswers?.status === 'Submitted'"
					:loading="submitting || loading"
					@stage-updated="handleStageUpdated"
					@submit="handleSubmit"
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
import { ElMessage, ElNotification, ElMessageBox } from 'element-plus';
import { ArrowRight } from '@element-plus/icons-vue';
import { OnboardingItem, SectionAnswer } from '#/onboard';

import { saveQuestionnaireAnswer } from '@/apis/ow/onboarding';
import { submitQuestionnaireAnswer } from '@/apis/ow/questionnaire';
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
	loading?: boolean;
}

const props = defineProps<Props>();

// 组件事件
const emit = defineEmits<{
	stageUpdated: [];
	questionSubmitted: [onboardingId: string, stageId: string, questionnaireId: string];
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

// ==================== 问题相关辅助函数 ====================

/**
 * 获取问题的唯一标识符
 * @param question 问题对象
 * @returns 问题ID
 */
const getQuestionId = (question: any): string => {
	return question.id || question.temporaryId || question.questionId;
};

/**
 * 检查答案是否有效（非空且有意义）
 * @param response 用户答案对象
 * @returns 是否有效
 */
const isAnswerValid = (response: any): boolean => {
	if (!response) return false;

	// 处理数组类型答案
	if (Array.isArray(response.answer)) {
		return response.answer.length > 0;
	}

	// 处理非数组类型答案
	return response.answer !== null && response.answer !== undefined && response.answer !== '';
};

/**
 * 创建混合答案数据源
 * 优先使用 dynamicForm 的实时数据，回退到 props 的已保存数据
 * @returns 混合的答案数据
 */
const getMergedAnswers = () => {
	const savedAnswers = props.questionnaireAnswers?.answer || [];
	const currentFormData = dynamicFormRef.value?.formData || {};

	// 创建一个 Map 来存储最终的答案
	const answerMap = new Map();

	// 首先添加已保存的答案
	savedAnswers.forEach((answer: any) => {
		if (answer.questionId) {
			answerMap.set(answer.questionId, answer);
		}
	});

	// 然后用当前表单数据覆盖（包括空值，以正确反映删除操作）
	Object.keys(currentFormData).forEach((questionId) => {
		const currentValue = currentFormData[questionId];
		// 如果当前值为空，则从答案中移除该问题
		if (
			currentValue === undefined ||
			currentValue === '' ||
			(Array.isArray(currentValue) && currentValue.length === 0)
		) {
			answerMap.delete(questionId);
		} else {
			// 如果当前值不为空，则更新答案
			answerMap.set(questionId, {
				questionId,
				answer: currentValue,
				question: '',
				responseText: '',
				type: '',
			});
		}
	});

	return {
		answer: Array.from(answerMap.values()),
	};
};

/**
 * 查找用户对特定问题的答案
 * @param answers 所有答案数据（这个参数现在主要用于兼容性）
 * @param questionId 问题ID
 * @returns 用户答案或null
 */
const findUserAnswer = (answers: any, questionId: string) => {
	// 使用混合数据源
	const mergedAnswers = getMergedAnswers();
	if (!mergedAnswers?.answer) return null;
	return mergedAnswers.answer.find((r: any) => r.questionId === questionId);
};

/**
 * 检查问题是否已被用户回答
 * @param question 问题对象
 * @param answers 所有答案数据
 * @returns 是否已回答
 */
const isQuestionAnswered = (question: any, answers: any): boolean => {
	if (question.type === 'page_break') return true; // 分页符不需要回答

	const questionId = getQuestionId(question);

	// 处理网格类型问题
	if (question.type === 'multiple_choice_grid' || question.type === 'checkbox_grid') {
		// 网格问题需要检查每一行是否都有答案
		if (!question.rows || question.rows.length === 0) return true; // 没有行数据视为完成

		return question.rows.every((row: any) => {
			const gridKey = `${questionId}_${row.id}`;
			const userAnswer = findUserAnswer(answers, gridKey);
			return isAnswerValid(userAnswer);
		});
	}

	if (question.type === 'short_answer_grid') {
		// 短答网格需要检查每一行是否至少有一个单元格有内容
		if (
			!question.rows ||
			question.rows.length === 0 ||
			!question.columns ||
			question.columns.length === 0
		) {
			return true; // 没有行列数据视为完成
		}

		return question.rows.every((row: any) => {
			// 检查该行是否至少有一个单元格有内容
			return question.columns.some((column: any) => {
				const gridKey = `${questionId}_${column.id}_${row.id}`;
				const userAnswer = findUserAnswer(answers, gridKey);
				return isAnswerValid(userAnswer);
			});
		});
	}

	// 处理普通问题
	const userAnswer = findUserAnswer(answers, questionId);
	return isAnswerValid(userAnswer);
};

// ==================== 跳转逻辑相关函数 ====================

/**
 * 检查是否为可跳转的问题
 * @param question 问题对象
 * @returns 是否可跳转
 */
const isJumpableQuestion = (question: any): boolean => {
	return (
		(question.type === 'multiple_choice' || question.type === 'checkboxes') &&
		question.jumpRules &&
		question.jumpRules.length > 0
	);
};

/**
 * 查找匹配的跳转规则
 * @param question 问题对象
 * @param userAnswer 用户答案
 * @returns 匹配的跳转规则或undefined
 */
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

/**
 * 查找目标section在数组中的索引
 * @param sections section数组
 * @param targetSectionId 目标section ID
 * @returns 索引值，未找到返回-1
 */
const findTargetSectionIndex = (sections: any[], targetSectionId: string): number => {
	return sections.findIndex((s) => s.id === targetSectionId || s.temporaryId === targetSectionId);
};

/**
 * 获取section中触发跳转的目标section ID
 * @param section section对象
 * @param answers 所有答案数据
 * @returns 目标section ID或null
 */
const getJumpTargetSectionId = (section: any, answers: any): string | null => {
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

// ==================== 跳过逻辑处理 ====================

/**
 * 跳过类型枚举
 */
enum SkipType {
	SECTION = 'section',
	QUESTION = 'question',
}

/**
 * 跳过规则接口定义（为未来扩展准备）
 */
interface SkipRule {
	type: SkipType;
	sourceId: string; // 触发跳过的问题ID
	targetId: string; // 跳过的目标ID（section ID 或 question ID）
	condition?: any; // 跳过条件（可选，为未来扩展）
}

// 避免未使用警告，这是为未来扩展准备的接口
// eslint-disable-next-line @typescript-eslint/no-unused-vars, vue/no-unused-vars
type SkipRuleType = SkipRule;

/**
 * 获取所有被跳过的问题ID集合
 * @param questionnaireData 问卷数据
 * @param answers 用户答案
 * @returns 被跳过的问题ID集合
 */
const getSkippedQuestions = (questionnaireData: any, answers: any): Set<string> => {
	const skippedQuestions = new Set<string>();

	if (!questionnaireData?.sections || !Array.isArray(questionnaireData.sections)) {
		return skippedQuestions;
	}

	// 过滤掉默认section
	const validSections = questionnaireData.sections.filter((section) => !section.isDefault);

	// 处理section级别的跳过逻辑
	const sectionSkippedQuestions = processSectionSkips(validSections, answers);
	sectionSkippedQuestions.forEach((questionId) => skippedQuestions.add(questionId));

	// 处理问题级别的跳过逻辑（为未来扩展准备）
	const questionSkippedQuestions = processQuestionSkips(validSections, answers);
	questionSkippedQuestions.forEach((questionId) => skippedQuestions.add(questionId));

	return skippedQuestions;
};

/**
 * 处理section级别的跳过逻辑
 * @param validSections 有效的section数组
 * @param answers 用户答案
 * @returns 被跳过的问题ID集合
 */
const processSectionSkips = (validSections: any[], answers: any): Set<string> => {
	const skippedQuestions = new Set<string>();
	const skippedSectionIndexes = new Set<number>();
	const visitedSections = new Set<number>();

	// 遍历所有section，处理跳转逻辑
	for (let sectionIndex = 0; sectionIndex < validSections.length; sectionIndex++) {
		// 跳过已经被标记为跳过的section
		if (skippedSectionIndexes.has(sectionIndex)) {
			continue;
		}

		const section = validSections[sectionIndex];
		visitedSections.add(sectionIndex);

		// 检查section中是否有已回答的跳转问题
		const hasAnsweredJumpQuestions = section.questions?.some((question: any) => {
			if (question.type === 'page_break') return false;
			// 检查是否是有跳转规则的问题且已回答
			if (question.jumpRules && question.jumpRules.length > 0) {
				return isQuestionAnswered(question, answers);
			}
			return false;
		});

		// 如果有已回答的跳转问题，检查是否有跳转逻辑
		if (hasAnsweredJumpQuestions) {
			const jumpTargetSectionId = getJumpTargetSectionId(section, answers);

			if (jumpTargetSectionId) {
				const targetSectionIndex = findTargetSectionIndex(
					validSections,
					jumpTargetSectionId
				);

				if (targetSectionIndex !== -1 && targetSectionIndex > sectionIndex) {
					// 标记中间被跳过的sections，并将其中的问题加入跳过集合
					for (let i = sectionIndex + 1; i < targetSectionIndex; i++) {
						if (!visitedSections.has(i)) {
							skippedSectionIndexes.add(i);
							const skippedSection = validSections[i];

							// 将跳过section中的所有问题加入跳过集合
							if (skippedSection.questions) {
								skippedSection.questions.forEach((question: any) => {
									if (question.type !== 'page_break') {
										const questionId = getQuestionId(question);
										skippedQuestions.add(questionId);
									}
								});
							}
						}
					}

					// 跳转到目标section
					sectionIndex = targetSectionIndex - 1; // -1 是因为for循环会自动+1
				}
			}
		}
	}

	return skippedQuestions;
};

/**
 * 处理问题级别的跳过逻辑（为未来扩展准备）
 * @param validSections 有效的section数组
 * @param answers 用户答案
 * @returns 被跳过的问题ID集合
 */
const processQuestionSkips = (validSections: any[], answers: any): Set<string> => {
	const skippedQuestions = new Set<string>();

	// 遍历所有问题，检查是否有问题级别的跳过规则
	validSections.forEach((section) => {
		if (!section.questions) return;

		section.questions.forEach((question: any) => {
			if (question.type === 'page_break') return;

			// 检查问题是否有跳过规则（为未来扩展准备）
			if (question.skipRules && Array.isArray(question.skipRules)) {
				const questionId = getQuestionId(question);
				const userAnswer = findUserAnswer(answers, questionId);

				if (isAnswerValid(userAnswer)) {
					// 查找匹配的跳过规则
					const matchingSkipRule = question.skipRules.find((rule: any) => {
						// 这里可以根据具体的跳过规则逻辑进行匹配
						// 目前为占位符实现，未来可以扩展
						return rule.condition && evaluateSkipCondition(rule.condition, userAnswer);
					});

					if (matchingSkipRule && matchingSkipRule.type === SkipType.QUESTION) {
						// 将目标问题加入跳过集合
						skippedQuestions.add(matchingSkipRule.targetId);
					}
				}
			}
		});
	});

	return skippedQuestions;
};

/**
 * 评估跳过条件（为未来扩展准备）
 * @param condition 跳过条件
 * @param userAnswer 用户答案
 * @returns 是否满足跳过条件
 */
const evaluateSkipCondition = (condition: any, userAnswer: any): boolean => {
	// 这里是占位符实现，未来可以根据具体需求扩展
	// 例如：condition.value === userAnswer.answer
	return false;
};

// ==================== 基于问题的进度计算 ====================

/**
 * 计算问卷完成状态 - 基于问题级别的进度计算
 */
const completionStats = computed(() => {
	const questionnaireData = JSON.parse(props.questionnaireData.structureJson);

	// 使用混合数据源：实时表单数据 + 已保存数据
	// 由于 getMergedAnswers() 会访问 dynamicFormRef.value?.formData
	// Vue 的响应式系统会自动追踪这个依赖，当 formData 变化时会自动重新计算
	const answers = getMergedAnswers();
	if (!questionnaireData?.sections || !Array.isArray(questionnaireData.sections)) {
		return {
			totalQuestions: 0,
			answeredQuestions: 0,
			skippedQuestions: 0,
			requiredQuestions: 0,
			answeredRequiredQuestions: 0,
			skippedRequiredQuestions: 0,
		};
	}

	// 过滤掉默认section
	const validSections = questionnaireData.sections;

	// 获取所有问题（网格类型问题算作一个问题）
	const allQuestions = validSections.flatMap((section) => {
		if (!section.questions) return [];

		return section.questions.filter((q: any) => q.type !== 'page_break');
	});

	if (allQuestions.length === 0) {
		return {
			totalQuestions: 0,
			answeredQuestions: 0,
			skippedQuestions: 0,
			requiredQuestions: 0,
			answeredRequiredQuestions: 0,
			skippedRequiredQuestions: 0,
		};
	}

	// 获取被跳过的问题集合（使用混合数据源）
	const skippedQuestions = getSkippedQuestions(questionnaireData, answers);

	// 统计各种类型的问题数量
	let totalQuestions = allQuestions.length;
	let answeredQuestions = 0;
	let requiredQuestions = 0;
	let answeredRequiredQuestions = 0;
	let skippedRequiredQuestions = 0;

	allQuestions.forEach((question: any) => {
		const questionId = getQuestionId(question);
		const isSkipped = skippedQuestions.has(questionId);
		const isAnswered = isQuestionAnswered(question, answers);
		const isRequired = question.required;

		// 统计必填问题
		if (isRequired) {
			requiredQuestions++;
			if (isAnswered) {
				answeredRequiredQuestions++;
			} else if (isSkipped) {
				skippedRequiredQuestions++;
			}
		}

		// 统计已回答或跳过的问题（都算作完成）
		if (isAnswered || isSkipped) {
			answeredQuestions++;
		}
	});

	return {
		totalQuestions,
		answeredQuestions,
		skippedQuestions: skippedQuestions.size,
		requiredQuestions,
		answeredRequiredQuestions,
		skippedRequiredQuestions,
		// 计算是否可以提交：所有必填问题都已回答或跳过
		canSubmit: answeredRequiredQuestions + skippedRequiredQuestions === requiredQuestions,
	};
});

/**
 * 计算完成百分比 - 基于问题级别
 */
const completionRate = computed(() => {
	const stats = completionStats.value;
	if (stats.totalQuestions === 0) return 0;
	return Math.round((stats.answeredQuestions / stats.totalQuestions) * 100);
});

/**
 * 生成完成状态描述文本
 */
const completionStatus = computed(() => {
	const stats = completionStats.value;

	if (stats.totalQuestions === 0) {
		return 'No questions to complete';
	}

	let statusText = `${stats.answeredQuestions} of ${stats.totalQuestions} questions completed`;

	// 如果有跳过的问题，添加说明
	if (stats.skippedQuestions > 0) {
		statusText += ` (${stats.skippedQuestions} skipped)`;
	}

	return statusText;
});

/**
 * 检查是否可以提交问卷
 */
const canSubmitQuestionnaire = computed(() => {
	return completionStats.value.canSubmit;
});

/**
 * 获取被跳过的问题集合（供子组件使用）
 */
const skippedQuestionsSet = computed(() => {
	const questionnaireData = JSON.parse(props.questionnaireData.structureJson);
	const answers = getMergedAnswers();
	return getSkippedQuestions(questionnaireData, answers);
});

// 切换展开状态
const toggleExpanded = () => {
	isExpanded.value = !isExpanded.value;
};

// 事件处理函数
const handleStageUpdated = () => {
	emit('stageUpdated');
};

/**
 * 保存问卷答案
 * @param isTip 是否显示提示消息
 * @param isValidate 是否进行验证
 * @returns 保存是否成功
 */
const handleSave = async (isTip: boolean = true, isValidate: boolean = true) => {
	try {
		if (props?.questionnaireAnswers?.status === 'Submitted') return true;
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
		console.error('Error saving questionnaire:', error);
		return false;
	} finally {
		saving.value = false;
	}
};

const submitting = ref(false);
const handleSubmit = async () => {
	try {
		// Calculate unanswered questions count
		const stats = completionStats.value;
		const unansweredCount = stats.totalQuestions - stats.answeredQuestions;

		// If there are unanswered questions, show confirmation dialog
		if (unansweredCount > 0) {
			try {
				await ElMessageBox.confirm(
					`There are ${unansweredCount} unanswered question${
						unansweredCount > 1 ? 's' : ''
					}. Are you sure you want to submit?`,
					'Confirm Submission',
					{
						confirmButtonText: 'Submit',
						cancelButtonText: 'Cancel',
						type: 'warning',
						distinguishCancelAndClose: true,
					}
				);
			} catch (error) {
				// User cancelled or closed the dialog
				if (error === 'cancel') {
					return false;
				}
				throw error;
			}
		}

		submitting.value = true;
		const saveRes = await handleSave(false, true);
		if (!saveRes) {
			return false;
		}
		const res = await submitQuestionnaireAnswer(
			props?.onboardingId || '',
			props.stageId,
			props.questionnaireData.id
		);
		if (res.code == '200') {
			ElMessage.success('Questionnaire submitted successfully');
			emit(
				'questionSubmitted',
				props?.onboardingId || '',
				props.stageId,
				props.questionnaireData.id
			);
		}
	} catch (error) {
		console.error('Error submitting questionnaire:', error);
		return false;
	} finally {
		submitting.value = false;
	}
};

// 暴露给父组件的方法
defineExpose({
	handleSave,
});
</script>

<style scoped lang="scss"></style>
