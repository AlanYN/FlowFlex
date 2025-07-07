<template>
	<el-card class="mb-6 rounded-md">
		<template #header>
			<div
				class="bg-gradient-to-r from-blue-500 to-indigo-500 text-white -mx-5 -mt-5 px-5 py-4 rounded-t-lg"
			>
				<h2 class="text-lg font-semibold">{{ currentStageTitle }}</h2>
			</div>
		</template>

		<div class="space-y-6">
			<!-- 阶段描述 -->
			<div class="flex flex-col gap-4">
				<StaticForm ref="staticFormRef" :static-fields="staticFields || []" />
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
				<el-button
					v-if="!isStageCompleted"
					type="primary"
					@click="handleCompleteStage"
					:loading="completing"
				>
					<el-icon class="mr-1"><Check /></el-icon>
					Complete Stage
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
	</el-card>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Document, Check } from '@element-plus/icons-vue';
import { OnboardingItem } from '#/onboard';
import { defaultStr } from '@/settings/projectSetting';

import {
	completeCurrentStage,
	saveQuestionnaireAnswer,
	saveQuestionnaireStatic,
} from '@/apis/ow/onboarding';
import StaticForm from './staticForm.vue';
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
const completing = ref(false);
const staticFormRef = ref();
const dynamicFormRef = ref();

// 计算属性
const currentStageTitle = computed(() => {
	const currentStage = props.workflowStages.find((stage) => stage.stageId === props.stageId);
	return currentStage?.stageName || defaultStr;
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

const handleSave = async () => {
	try {
		saving.value = true;

		// 验证静态表单
		const staticFormValid = await staticFormRef.value?.validateForm();
		const dynamicFormValid = await dynamicFormRef.value?.validateForm();
		if (!staticFormValid || !dynamicFormValid?.isValid || !props?.onboardingId) {
			!dynamicFormValid?.isValid && ElMessage.error(dynamicFormValid?.errors?.join('\n'));
			return;
		}

		const staticFormData = staticFormRef.value?.getFormData();
		const dynamicForm = await dynamicFormRef.value?.transformFormDataForAPI();
		const formSubmit = await Promise.all([
			saveQuestionnaireStatic({
				fieldValues: staticFormData,
				onboardingId: props?.onboardingId,
				stageId: props.stageId,
			}),
			...dynamicForm.map((item) =>
				saveQuestionnaireAnswer(props?.onboardingId || '', props.stageId, {
					...item,
					onboardingId: props?.onboardingId,
				})
			),
		]);
		const res = formSubmit[0] || {
			code: '200',
		};

		const saveResponse = formSubmit[1] || {
			code: '200',
		};
		if (saveResponse.code == '200' && res.code == '200') {
			// 完成阶段
			const response = await completeCurrentStage(props.onboardingId || props.leadId, {
				currentStageId: props.stageId,
			});
			if (response.code === '200') {
				emit('stageUpdated');
			} else {
				ElMessage.error(response.msg || 'Failed to complete stage');
			}
			return true;
		} else {
			ElMessage.error(res.msg || 'Failed to save stage data');
			return false;
		}
	} catch {
		return false;
	} finally {
		saving.value = false;
	}
};

const handleCompleteStage = async () => {
	ElMessageBox.confirm(
		`Are you sure you want to mark this stage as complete? This action will record your name and the current time as the completion signature.`,
		'⚠️ Confirm Stage Completion',
		{
			confirmButtonText: 'Complete Stage',
			cancelButtonText: 'Cancel',
			distinguishCancelAndClose: true,
			showCancelButton: true,
			showConfirmButton: true,
			beforeClose: async (action, instance, done) => {
				if (action === 'confirm') {
					// 显示loading状态
					instance.confirmButtonLoading = true;
					instance.confirmButtonText = 'Deactivating...';

					completing.value = true;
					try {
						const res = await handleSave();
						if (!res) {
							instance.confirmButtonLoading = false;
							instance.confirmButtonText = 'Complete Stage';
							return;
						}
						done();
					} finally {
						completing.value = false;
					}
				} else {
					done();
				}
			},
		}
	);
};

// 设置表单字段值的方法
const setFormFieldValues = (staticFieldsData?: any[] | Record<string, any>) => {
	console.log('设置值');
	if (staticFieldsData && staticFormRef.value) {
		// 直接传递数据给静态表单，让它自己处理格式转换
		staticFormRef.value.setFieldValues(staticFieldsData);
	}
	if (dynamicFormRef.value) {
		dynamicFormRef.value.setFieldValues();
	}
};

// 暴露给父组件的方法
defineExpose({
	setFormFieldValues,
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
