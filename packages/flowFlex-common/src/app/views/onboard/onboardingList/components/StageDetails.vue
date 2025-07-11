<template>
	<div class="customer-block">
		<h2 class="text-lg font-semibold">Questionnaire</h2>
		<el-divider />

		<!-- 阶段描述 -->
		<div class="flex flex-col gap-4">
			<!-- 问卷表单 -->
			<DynamicForm
				ref="dynamicFormRef"
				:stageId="stageId"
				:leadId="leadId"
				:onboardingId="onboardingId || ''"
				:questionnaireData="questionnaireData"
				:isStageCompleted="isStageCompleted"
				@stage-updated="handleStageUpdated"
			/>
		</div>

		<!-- 操作按钮 -->
		<div class="flex justify-end space-x-2 pt-6 mt-6 border-t">
			<el-button @click="handleSave" :loading="saving">
				<el-icon class="mr-1"><Document /></el-icon>
				Save
			</el-button>
		</div>

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
import { Document } from '@element-plus/icons-vue';
import { OnboardingItem } from '#/onboard';

import { saveQuestionnaireAnswer } from '@/apis/ow/onboarding';
import DynamicForm from './dynamicForm.vue';

// 组件属性
interface Props {
	stageId: string;
	leadId: string;
	onboardingId?: string;
	leadData: OnboardingItem | null;
	workflowStages: any[];
	questionnaireData?: any[];
	staticFields?: any[];
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

const handleSave = async () => {
	try {
		saving.value = true;

		// 验证静态表单
		const dynamicFormValid = await dynamicFormRef.value?.validateForm();
		if (!dynamicFormValid?.isValid || !props?.onboardingId) {
			!dynamicFormValid?.isValid && ElMessage.error(dynamicFormValid?.errors?.join('\n'));
			return;
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
			ElMessage.success('Save success');
			return true;
		} else {
			ElMessage.error(res[0]?.msg || 'Failed to save stage data');
			return false;
		}
	} catch {
		return false;
	} finally {
		saving.value = false;
	}
};

// 设置表单字段值的方法
const setFormFieldValues = () => {
	if (dynamicFormRef.value) {
		dynamicFormRef.value.setFieldValues();
	}
};

// 暴露给父组件的方法
defineExpose({
	setFormFieldValues,
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
