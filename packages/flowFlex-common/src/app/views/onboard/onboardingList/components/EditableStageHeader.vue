<template>
	<div class="">
		<div
			class="editable-header-card rounded-xl text-white p-2.5"
			style="background: linear-gradient(135deg, #6366f1 0%, #4f46e5 100%)"
		>
			<!-- 显示状态 -->
			<div v-if="!isEditing">
				<!-- 头部标题和编辑按钮 -->
				<div class="flex items-center justify-between">
					<h2 class="text-xl font-semibold">{{ displayTitle }}</h2>
					<el-button
						link
						class="!text-white hover:bg-white/10 p-2 transition-colors"
						@click="handleEdit"
						:icon="Edit"
						:disabled="disabled || !currentStage?.startTime"
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
							class="!text-white hover:bg-white/10 px-3 py-1 transition-colors text-sm"
							@click="handleCancel"
							:disabled="saving"
						>
							Cancel
						</el-button>
						<el-button
							link
							class="!text-white hover:bg-white/10 px-3 py-1 transition-colors text-sm font-medium"
							@click="handleSave"
							:loading="saving"
						>
							Save
						</el-button>
					</div>
				</div>

				<!-- 编辑表单网格 -->
				<div class="grid grid-cols-1 md:grid-cols-3 gap-4 text-sm">
					<!-- Start Date - 只读显示 -->
					<div>
						<div class="text-white/70 mb-2">Start Date</div>
						<el-input
							v-model="displayStartDate"
							class="w-full stage-edit-input"
							disabled
						/>
					</div>
					<!-- Est. Duration - 可编辑 -->
					<div>
						<div class="text-white/70 mb-2">Est. Duration (days)</div>
						<InputNumber
							v-model="editForm.customEstimatedDays as number"
							placeholder="Enter days"
							class="w-full stage-edit-input"
							:disabled="saving"
							:decimalPlaces="2"
							:minNumber="0.01"
							@change="handleEstimatedDaysChange"
						/>
					</div>
					<!-- End Time - 可编辑 -->
					<div>
						<div class="text-white/70 mb-2">End Time</div>
						<el-date-picker
							v-model="editForm.customEndTime"
							type="datetime"
							placeholder="Select end date"
							class="w-full stage-edit-input"
							:disabled="saving"
							:format="projectTenMinutesSsecondsDate"
							:value-format="projectTenMinutesSsecondsDate"
							:disabledDate="disabledEndDate"
							@change="handleEndTimeChange"
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
const emit = defineEmits(['update:stage-data']);

// 响应式数据
const isEditing = ref(false);
const saving = ref(false);

// 编辑表单数据
const editForm = ref({
	customEstimatedDays: null as number | null,
	customEndTime: null as string | null,
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
		return (
			timeZoneConvert(
				props.currentStage?.customEndTime || props.currentStage?.endTime || '',
				false,
				projectTenMinutesSsecondsDate
			) || defaultStr
		);
	} catch (error) {
		console.error('Error calculating ETA:', error);
		return defaultStr;
	}
});

// 初始化编辑表单
const initEditForm = () => {
	if (!props.currentStage) return;
	editForm.value = {
		customEstimatedDays: props.currentStage.estimatedDays || null,
		customEndTime: null, // 可以直接编辑结束时间
	};

	// 如果有开始时间和预估天数，计算默认结束时间
	if (props.currentStage.startTime && editForm.value.customEstimatedDays) {
		try {
			// 直接使用原始的ISO时间字符串创建Date对象
			const startDate = new Date(props.currentStage.startTime);
			// 使用毫秒计算支持小数天数，保持原始时分秒
			const millisecondsToAdd = editForm.value.customEstimatedDays * 24 * 60 * 60 * 1000;
			const endDate = new Date(startDate.getTime() + millisecondsToAdd);
			// 将计算出的结束时间转换为 projectTenMinutesSsecondsDate 格式
			const endTimeFormatted = timeZoneConvert(
				endDate.toString(),
				false,
				projectTenMinutesSsecondsDate
			);
			editForm.value.customEndTime = endTimeFormatted;
		} catch (error) {
			console.error('Error initializing end time:', error);
			editForm.value.customEndTime = null;
		}
	}
};

// 监听当前阶段变化，更新编辑表单
watch(
	() => props.currentStage,
	() => {
		isEditing.value = false;
		initEditForm();
	},
	{ immediate: true }
);

// 预估天数变化时，自动计算结束时间
const handleEstimatedDaysChange = (estimatedDays: number | null) => {
	if (props.currentStage?.startTime && estimatedDays && estimatedDays > 0) {
		try {
			// 直接使用原始的ISO时间字符串创建Date对象
			const startDate = new Date(props.currentStage.startTime);

			// 使用毫秒计算支持小数天数，保持原始时分秒
			const millisecondsToAdd = estimatedDays * 24 * 60 * 60 * 1000;
			const endDate = new Date(startDate.getTime() + millisecondsToAdd);

			// 将计算出的结束时间转换为 projectTenMinutesSsecondsDate 格式
			const endTimeFormatted = timeZoneConvert(
				endDate.toString(),
				false,
				projectTenMinutesSsecondsDate
			);
			editForm.value.customEndTime = endTimeFormatted;
		} catch (error) {
			console.error('Error calculating end time from estimated days:', error);
			editForm.value.customEndTime = null;
		}
	} else if (!estimatedDays) {
		editForm.value.customEndTime = null;
	}
};

// 禁用结束日期选择器中开始日期之前的日期
const disabledEndDate = (time: Date) => {
	if (!props.currentStage?.startTime) {
		return false;
	}

	try {
		// 将开始时间转换为本地时区的格式化字符串
		const startTimeFormatted = timeZoneConvert(
			props.currentStage.startTime,
			false,
			projectTenMinutesSsecondsDate
		);

		const startDate = new Date(startTimeFormatted);
		const startDateOnly = new Date(
			startDate.getFullYear(),
			startDate.getMonth(),
			startDate.getDate()
		);
		const timeOnly = new Date(time.getFullYear(), time.getMonth(), time.getDate());

		// 禁用早于开始日期的所有日期
		return timeOnly < startDateOnly;
	} catch (error) {
		console.error('Error in disabledEndDate:', error);
		return false;
	}
};

// 结束时间变化时，自动计算预估天数
const handleEndTimeChange = (endTime: string | Date | null) => {
	if (props.currentStage?.startTime && endTime) {
		try {
			// 直接使用原始的ISO时间字符串创建Date对象
			const startDate = new Date(props.currentStage.startTime);
			const endDate = new Date(endTime);
			// 验证结束时间不能小于开始时间
			if (endDate < startDate) {
				ElMessage.error('End time cannot be earlier than start time');
				// 重置为之前的有效值或null
				editForm.value.customEndTime = null;
				editForm.value.customEstimatedDays = null;
				return;
			}

			// 计算天数差，支持小数
			const timeDiff = endDate.getTime() - startDate.getTime();
			const daysDiff = timeDiff / (1000 * 60 * 60 * 24);

			// 更新预估天数，保留两位小数
			editForm.value.customEstimatedDays =
				daysDiff > 0 ? Math.round(daysDiff * 100) / 100 : 0.01;
		} catch (error) {
			console.error('Error calculating estimated days from end time:', error);
			editForm.value.customEstimatedDays = null;
		}
	} else if (!endTime) {
		editForm.value.customEstimatedDays = null;
	}
};

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
	if (!editForm.value.customEstimatedDays || editForm.value.customEstimatedDays < 0.01) {
		ElMessage.error('Estimated duration must be at least 0.01 day');
		return;
	}

	if (!editForm.value.customEndTime) {
		ElMessage.error('End time is required');
		return;
	}

	// 验证结束时间不能小于开始时间
	if (props.currentStage.startTime) {
		try {
			// 直接使用原始的ISO时间字符串创建Date对象
			const startDate = new Date(props.currentStage.startTime);
			const endDate = new Date(editForm.value.customEndTime);

			if (endDate < startDate) {
				ElMessage.error('End time cannot be earlier than start time');
				return;
			}
		} catch (error) {
			console.error('Error validating dates:', error);
			ElMessage.error('Invalid date format');
			return;
		}
	}

	saving.value = true;

	try {
		// 准备更新数据，使用 timeZoneConvert 处理时间格式
		// 将格式化的时间字符串转换为Date对象，再转为ISO字符串，最后转换为UTC格式
		const customEndTimeStr = timeZoneConvert(editForm.value.customEndTime, true);
		const updateData = {
			stageId: props.currentStage.stageId,
			customEstimatedDays: editForm.value.customEstimatedDays,
			customEndTime: customEndTimeStr,
		};

		// 发送更新事件给父组件
		emit('update:stage-data', updateData);

		// 退出编辑模式
		isEditing.value = false;
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
