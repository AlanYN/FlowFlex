<template>
	<div class="">
		<div class="case-header rounded-xl p-2.5">
			<!-- 显示状态 -->
			<div v-if="!isEditing">
				<!-- 头部标题和编辑按钮 -->
				<div class="flex items-center justify-between">
					<h2 class="font-bold text-xl">{{ displayTitle }}</h2>
					<el-button
						link
						type="primary"
						@click="handleEdit"
						:icon="Edit"
						:disabled="disabled || !currentStage?.startTime"
					/>
				</div>
				<div
					v-if="currentStage?.stageDescription"
					class="text-sm text-[var(--el-text-color-secondary)]"
				>
					{{ currentStage?.stageDescription }}
				</div>
				<el-divider class="my-4" />
				<!-- 信息网格 -->
				<div class="grid grid-cols-1 md:grid-cols-3 gap-4 text-sm">
					<div>
						<div class="mb-1">Start Date</div>
						<div class="font-medium">{{ displayStartDate }}</div>
					</div>
					<div>
						<div class="mb-1">Est. Duration</div>
						<div class="font-medium">{{ displayEstimatedDuration }}</div>
					</div>
					<div>
						<div class="mb-1">ETA</div>
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
						<el-button link type="info" @click="handleCancel" :disabled="saving">
							Cancel
						</el-button>
						<el-button link type="primary" @click="handleSave" :loading="saving">
							Save
						</el-button>
					</div>
				</div>

				<!-- 编辑表单网格 -->
				<div class="grid grid-cols-1 md:grid-cols-3 gap-4 text-sm">
					<!-- Start Date - 只读显示 -->
					<div>
						<div class="mb-2">Start Date</div>
						<el-input
							v-model="displayStartDate"
							class="w-full stage-edit-input"
							disabled
						/>
					</div>
					<!-- Est. Duration - 可编辑 -->
					<div>
						<div class="mb-2">Est. Duration (days)</div>
						<InputNumber
							v-model="editForm.customEstimatedDays as number"
							placeholder="Enter days"
							class="w-full stage-edit-input"
							:disabled="saving"
							:isFoloat="false"
							@change="handleEstimatedDaysChange"
						/>
					</div>
					<!-- End Time - 可编辑 -->
					<div>
						<div class="mb-2">End Time</div>
						<el-date-picker
							v-model="editForm.customEndTime as string"
							type="date"
							placeholder="Select end date"
							class="w-full stage-edit-input"
							:disabled="saving"
							:format="projectDate"
							:value-format="projectDate"
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
import { defaultStr, projectTenMinutesSsecondsDate, projectDate } from '@/settings/projectSetting';
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
	return timeZoneConvert(props.currentStage.startTime, false, projectDate);
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
				projectDate
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
/* 响应式设计 */
@media (max-width: 768px) {
	:deep(.grid) {
		grid-template-columns: 1fr;
		gap: 1rem;
	}
}
</style>
