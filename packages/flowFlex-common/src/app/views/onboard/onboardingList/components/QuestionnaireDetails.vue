<template>
	<div class="customer-block" v-if="hasQuestionnaireData">
		<h2 class="text-lg font-semibold">Questionnaire-{{ questionnaireData.name }}</h2>
		<el-divider />

		<!-- 阶段描述 -->
		<div class="flex flex-col gap-4">
			<!-- 问卷表单 -->
			<DynamicForm
				ref="dynamicFormRef"
				:stageId="stageId"
				:onboardingId="onboardingId || ''"
				:questionnaireData="questionnaireData"
				:isStageCompleted="isStageCompleted"
				:questionnaire-answers="questionnaireAnswers"
				@stage-updated="handleStageUpdated"
			/>
		</div>

		<!-- 操作按钮 -->
		<!-- <div class="flex justify-end space-x-2 pt-6 mt-6 border-t">
			<el-button @click="handleSave" :loading="saving">
				<el-icon class="mr-1"><Document /></el-icon>
				Save
			</el-button>
		</div> -->

		<!-- 阶段历史 -->
		<div v-if="stageHistory.length > 0">
			<h4 class="font-medium text-gray-900 mb-4">Stage History</h4>
			<el-timeline>
				<el-timeline-item
					v-for="item in stageHistory"
					:key="item.id"
					:timestamp="item.timestamp"
					:type="item.type"
				>
					<div>
						<p class="font-medium">{{ item.title }}</p>
						<p class="text-sm text-gray-600 mt-1">{{ item.description }}</p>
						<p class="text-xs text-gray-500 mt-1">by {{ item.user }}</p>
					</div>
				</el-timeline-item>
			</el-timeline>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { ElMessage } from 'element-plus';
import { OnboardingItem } from '#/onboard';

import { saveQuestionnaireAnswer } from '@/apis/ow/onboarding';
import DynamicForm from './dynamicForm.vue';

// 组件属性
interface Props {
	stageId: string;
	onboardingId: string;
	leadData: OnboardingItem | null;
	workflowStages: any[];
	questionnaireData?: any;
	questionnaireAnswers?: {
		lastModifiedAt: string;
		lastModifiedBy: string;
		question: string;
		questionId: string;
		responseText: string;
		type: string;
		answer: string;
		changeHistory: any[];
	}[];
}

const props = defineProps<Props>();

// 组件事件
const emit = defineEmits<{
	stageUpdated: [];
}>();

// 响应式数据
const stageHistory = ref<any[]>([]);
const saving = ref(false);
const dynamicFormRef = ref();

// 计算属性 - 检查是否有问卷数据
const hasQuestionnaireData = computed(() => {
	return props.questionnaireData;
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
			if (!dynamicFormValid?.isValid || !props?.onboardingId) {
				if (!dynamicFormValid?.isValid) {
					ElMessage.error(
						`Please complete all required fields:\n${dynamicFormValid?.errors?.join(
							'\n'
						)}`
					);
				}
				return false;
			}
		}

		const dynamicForm = await dynamicFormRef.value?.transformFormDataForAPI();
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
.stage-details-card {
	:deep(.el-card__header) {
		background-color: #f8fafb;
		border-bottom: 1px solid #e5e7eb;
	}
}

/* 暗色主题 */
html.dark {
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
</style>
