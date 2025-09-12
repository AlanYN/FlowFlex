<template>
	<div class="customer-block">
		<div
			class="editable-header-card rounded-md text-white px-6 py-5"
			style="background: linear-gradient(135deg, #6366f1 0%, #4f46e5 100%)"
		>
			<!-- 显示状态 -->
			<div v-if="!isEditing">
				<!-- 头部标题和编辑按钮 -->
				<div class="flex items-center justify-between">
					<h2 class="text-xl font-semibold">{{ displayTitle }}</h2>
					<el-button
						link
						class="!text-white hover:bg-white/10 p-2 rounded-md transition-colors"
						@click="handleEdit"
						:icon="Edit"
						:disabled="disabled"
					/>
				</div>
				<el-divider class="my-4" />
				<!-- 信息网格 -->
				<div class="grid grid-cols-1 md:grid-cols-3 gap-4 text-sm">
					<div>
						<div class="text-white/70 mb-1">Start Date</div>
						<div class="font-medium">{{ displayStartDate }}</div>
					</div>
					<div>
						<div class="text-white/70 mb-1">Est. Duration</div>
						<div class="font-medium">{{ displayEstimatedDuration }}</div>
					</div>
					<div>
						<div class="text-white/70 mb-1">ETA</div>
						<div class="font-medium">{{ displayETA }}</div>
					</div>
				</div>
			</div>

			<!-- 编辑状态 -->
			<div v-else>
				<!-- 编辑头部标题和操作按钮 -->
				<div class="flex items-center justify-between mb-4">
					<h2 class="text-xl font-semibold">Edit Stage Information</h2>
					<div class="flex items-center gap-2">
						<el-button
							link
							class="!text-white hover:bg-white/10 px-3 py-1 rounded-md transition-colors text-sm"
							@click="handleCancel"
							:disabled="saving"
						>
							Cancel
						</el-button>
						<el-button
							link
							class="!text-white hover:bg-white/10 px-3 py-1 rounded-md transition-colors text-sm font-medium"
							@click="handleSave"
							:loading="saving"
						>
							Save
						</el-button>
					</div>
				</div>

				<!-- 编辑表单网格 -->
				<div class="grid grid-cols-1 md:grid-cols-3 gap-4 text-sm">
					<div>
						<div class="text-white/70 mb-2">Start Date</div>
						<el-date-picker
							v-model="editForm.startDate"
							type="date"
							placeholder="Select start date"
							class="w-full stage-edit-input"
							:disabled="saving"
						/>
					</div>
					<div>
						<div class="text-white/70 mb-2">Est. Duration (days)</div>
						<InputNumber
							v-model="editForm.estimatedDays as number"
							placeholder="Enter days"
							class="w-full stage-edit-input"
							:disabled="saving"
						/>
					</div>
					<div>
						<div class="text-white/70 mb-2">ETA</div>
						<el-date-picker
							v-model="editForm.endDate"
							type="date"
							placeholder="Select end date"
							class="w-full stage-edit-input"
							:disabled="saving"
						/>
					</div>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { ElMessage } from 'element-plus';
import { Edit } from '@element-plus/icons-vue';
import { timeZoneConvert } from '@/hooks/time';
import { defaultStr, projectTenMinutesSsecondsDate } from '@/settings/projectSetting';
import InputNumber from '@/components/form/InputNumber/index.vue';
import type { Stage } from '#/onboard';

// Props 定义
interface Props {
	currentStage?: Stage | null;
	disabled?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	currentStage: null,
	disabled: false,
});

// Emits 定义
const emit = defineEmits<{
	'update:stage-data': [
		data: {
			stageId: string;
			startDate?: string;
			estimatedDays?: number;
			endDate?: string;
		},
	];
}>();

// 响应式数据
const isEditing = ref(false);
const saving = ref(false);

// 编辑表单数据
const editForm = ref({
	startDate: null as Date | null,
	estimatedDays: null as number | null,
	endDate: null as Date | null,
});

// 计算属性 - 显示标题
const displayTitle = computed(() => {
	return props.currentStage?.stageName || defaultStr;
});

// 计算属性 - 显示开始日期
const displayStartDate = computed(() => {
	if (!props.currentStage?.startTime) return defaultStr;
	return timeZoneConvert(props.currentStage.startTime, false, projectTenMinutesSsecondsDate);
});

// 计算属性 - 显示预估时长
const displayEstimatedDuration = computed(() => {
	if (!props.currentStage?.estimatedDays) return defaultStr;
	const days = props.currentStage.estimatedDays;
	if (days === 1) return '1 day';
	if (days < 30) return `${days} days`;
	if (days < 365) {
		const months = Math.round(days / 30);
		return months === 1 ? '1 month' : `${months} months`;
	}
	const years = Math.round(days / 365);
	return years === 1 ? '1 year' : `${years} years`;
});

// 计算属性 - 显示ETA
const displayETA = computed(() => {
	if (!props.currentStage?.startTime || !props.currentStage?.estimatedDays) {
		return defaultStr;
	}

	try {
		const startDate = new Date(props.currentStage.startTime);
		const etaDate = new Date(startDate);
		etaDate.setDate(startDate.getDate() + props.currentStage.estimatedDays);
		return timeZoneConvert(etaDate.toISOString(), false, projectTenMinutesSsecondsDate);
	} catch (error) {
		console.error('Error calculating ETA:', error);
		return defaultStr;
	}
});

// 初始化编辑表单
const initEditForm = () => {
	if (!props.currentStage) return;

	editForm.value = {
		startDate: props.currentStage.startTime ? new Date(props.currentStage.startTime) : null,
		estimatedDays: props.currentStage.estimatedDays || null,
		endDate: null, // ETA 是计算得出的，不直接编辑
	};

	// 如果有开始时间和预估天数，计算结束时间
	if (editForm.value.startDate && editForm.value.estimatedDays) {
		const endDate = new Date(editForm.value.startDate);
		endDate.setDate(endDate.getDate() + editForm.value.estimatedDays);
		editForm.value.endDate = endDate;
	}
};

// 监听当前阶段变化，更新编辑表单
watch(
	() => props.currentStage,
	() => {
		if (!isEditing.value) {
			initEditForm();
		}
	},
	{ immediate: true }
);

// 监听编辑表单中开始时间和预估天数的变化，自动计算结束时间
watch(
	[() => editForm.value.startDate, () => editForm.value.estimatedDays],
	([startDate, estimatedDays]) => {
		if (startDate && estimatedDays && estimatedDays > 0) {
			const endDate = new Date(startDate);
			endDate.setDate(endDate.getDate() + estimatedDays);
			editForm.value.endDate = endDate;
		} else {
			editForm.value.endDate = null;
		}
	}
);

// 事件处理函数
const handleEdit = () => {
	if (props.disabled) return;
	initEditForm();
	isEditing.value = true;
};

const handleCancel = () => {
	isEditing.value = false;
	initEditForm(); // 重置表单数据
};

const handleSave = async () => {
	if (!props.currentStage?.stageId) {
		ElMessage.error('Invalid stage information');
		return;
	}

	// 表单验证
	if (!editForm.value.startDate) {
		ElMessage.error('Start date is required');
		return;
	}

	if (!editForm.value.estimatedDays || editForm.value.estimatedDays < 1) {
		ElMessage.error('Estimated duration must be at least 1 day');
		return;
	}

	saving.value = true;

	try {
		// 准备更新数据
		const updateData = {
			stageId: props.currentStage.stageId,
			startDate: editForm.value.startDate.toISOString(),
			estimatedDays: editForm.value.estimatedDays,
			endDate: editForm.value.endDate?.toISOString(),
		};

		// 发送更新事件给父组件
		emit('update:stage-data', updateData);

		// 退出编辑模式
		isEditing.value = false;

		ElMessage.success('Stage information updated successfully');
	} catch (error) {
		console.error('Error saving stage data:', error);
		ElMessage.error('Failed to save stage information');
	} finally {
		saving.value = false;
	}
};
</script>

<style scoped lang="scss">
/* 统一的头部卡片样式 */
.editable-header-card {
	box-shadow: 0 4px 12px rgba(99, 102, 241, 0.2);
	display: flex;
	flex-direction: column;
	gap: 16px;
	transition: all 0.2s ease;

	&:hover {
		box-shadow: 0 6px 16px rgba(99, 102, 241, 0.3);
		transform: translateY(-1px);
	}
}

.customer-block {
	margin-bottom: 16px;

	&:last-child {
		margin-bottom: 0;
	}
}

/* 编辑输入框样式 */
:deep(.stage-edit-input) {
	.el-input__wrapper {
		background-color: rgba(255, 255, 255, 0.1);
		border-color: rgba(255, 255, 255, 0.2);
		color: white;

		&:hover {
			border-color: rgba(255, 255, 255, 0.3);
		}

		&.is-focus {
			border-color: rgba(255, 255, 255, 0.5);
			box-shadow: 0 0 0 1px rgba(255, 255, 255, 0.2) inset;
		}
	}

	.el-input__inner {
		color: white;

		&::placeholder {
			color: rgba(255, 255, 255, 0.6);
		}
	}

	.el-input-number__decrease,
	.el-input-number__increase {
		background-color: rgba(255, 255, 255, 0.1);
		border-color: rgba(255, 255, 255, 0.2);
		color: white;

		&:hover {
			background-color: rgba(255, 255, 255, 0.2);
			color: white;
		}
	}
}

/* 日期选择器样式 */
:deep(.el-date-editor) {
	.el-input__wrapper {
		background-color: rgba(255, 255, 255, 0.1);
		border-color: rgba(255, 255, 255, 0.2);

		&:hover {
			border-color: rgba(255, 255, 255, 0.3);
		}

		&.is-focus {
			border-color: rgba(255, 255, 255, 0.5);
		}
	}

	.el-input__inner {
		color: white;

		&::placeholder {
			color: rgba(255, 255, 255, 0.6);
		}
	}

	.el-input__prefix-inner,
	.el-input__suffix-inner {
		color: rgba(255, 255, 255, 0.8);
	}
}

/* 暗色主题适配 */
html.dark {
	.editable-header-card {
		background: linear-gradient(135deg, #4338ca 0%, #3730a3 100%) !important;
		box-shadow: 0 4px 12px rgba(67, 56, 202, 0.3);
	}

	.editable-header-card:hover {
		box-shadow: 0 6px 16px rgba(67, 56, 202, 0.4);
	}
	:deep(.stage-edit-input) {
		.el-input__wrapper {
			background-color: rgba(0, 0, 0, 0.2);
			border-color: rgba(255, 255, 255, 0.1);
		}
	}

	:deep(.el-date-editor) {
		.el-input__wrapper {
			background-color: rgba(0, 0, 0, 0.2);
			border-color: rgba(255, 255, 255, 0.1);
		}
	}
}

/* 响应式设计 */
@media (max-width: 768px) {
	:deep(.grid) {
		grid-template-columns: 1fr;
		gap: 1rem;
	}

	.flex.items-center.justify-between {
		flex-direction: column;
		align-items: flex-start;
		gap: 0.75rem;
	}

	.flex.items-center.gap-2 {
		align-self: flex-end;
	}
}
</style>
