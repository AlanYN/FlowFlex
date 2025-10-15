<template>
	<div class="date-range-picker" @click="clickPicker">
		<div
			class="progress-bar"
			@mouseenter="handleMouseEnter"
			@mouseleave="handleMouseLeave"
			:class="{
				'is-hovered': isHovered,
				empty: !modelValue?.startDate || !modelValue.endDate,
			}"
		>
			<div
				class="progress-fill"
				:style="{ width: progressPercentage + '%' }"
				:class="progressClass"
			></div>
			<div class="progress-content" v-if="!isHovered">
				<div class="date-range-text">{{ displayText }}</div>
			</div>
			<div class="days-count" v-else>{{ daysCount }}</div>
		</div>
		<el-date-picker
			ref="inputRef"
			v-model="valueModel"
			style="width: 0; position: absolute; top: 0; margin-left: 48px; opacity: 0; z-index: -1"
			type="daterange"
			range-separator="to"
			start-placeholder="Start date"
			end-placeholder="End date"
			format="MMM DD"
			:value-format="projectDate"
			class="custom-date-picker"
		>
			<template #focus="{ focus }">
				<button @click="focus">Focus</button>
			</template>
		</el-date-picker>
	</div>
</template>

<script lang="ts" setup>
import { timeZoneConvert } from '@/hooks/time';
import { computed, ref } from 'vue';
import dayjs from 'dayjs';
import { ElDatePicker } from 'element-plus';
import { projectDate } from '@/settings/projectSetting';

const inputRef = ref<InstanceType<typeof ElDatePicker>>();

interface DateRangePickerProps {
	modelValue?: { startDate: string; endDate: string };
	enable?: boolean;
	showSetDate?: boolean;
}

const props = withDefaults(defineProps<DateRangePickerProps>(), {
	modelValue: () => ({ startDate: '', endDate: '' }),
	enable: true,
	showSetDate: false,
});

const emit = defineEmits<{
	'update:modelValue': [value: { startDate: string; endDate: string }];
}>();

// v-model 计算属性
const valueModel = computed({
	get: () => {
		// 将对象格式转换为数组格式给 Element Plus DatePicker 使用
		return props.modelValue && props.modelValue.startDate && props.modelValue.endDate
			? ([
					timeZoneConvert(props.modelValue.startDate),
					timeZoneConvert(props.modelValue.endDate),
			  ] as [string, string])
			: undefined;
	},
	set: (value: [string, string] | undefined) => {
		if (value && value.length === 2) {
			// 将数组格式转换为对象格式
			emit('update:modelValue', {
				startDate: timeZoneConvert(value[0], true),
				endDate: timeZoneConvert(value[1], true),
			});
		}
	},
});

// 鼠标悬停状态
const isHovered = ref(false);

// 计算天数差
const daysCount = computed(() => {
	const startDate = props.modelValue?.startDate;
	const endDate = props.modelValue?.endDate;

	if (!startDate || !endDate || startDate === '' || endDate === '') {
		return props.showSetDate ? 'Set dates' : '-';
	}

	const start = new Date(startDate);
	const end = new Date(endDate);
	const diffTime = Math.abs(end.getTime() - start.getTime());
	const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

	return (diffDays === 0 ? 1 : diffDays + 1) + 'd'; // 包含开始和结束日期
});

// 鼠标事件处理
const handleMouseEnter = () => {
	isHovered.value = true;
};

const handleMouseLeave = () => {
	isHovered.value = false;
};

const clickPicker = () => {
	if (props.enable) {
		inputRef.value?.$el.nextElementSibling.querySelector('input').focus();
	}
};

// 计算进度百分比和样式类
const progressData = computed(() => {
	const startDate = props.modelValue?.startDate;
	const endDate = props.modelValue?.endDate;

	if (!startDate || !endDate || startDate === '' || endDate === '') {
		return { percentage: 0, class: 'empty' };
	}

	const today = new Date();
	today.setHours(0, 0, 0, 0); // 重置时间为当天开始

	const startDateObj = new Date(startDate);
	startDateObj.setHours(0, 0, 0, 0);

	const endDateObj = new Date(endDate);
	endDateObj.setHours(0, 0, 0, 0);

	// 情况1: 起止日期相同（单日事件）

	if (startDateObj.getUTCDate() === endDateObj.getUTCDate()) {
		if (today.getUTCDate() > startDateObj.getUTCDate())
			return { percentage: 0, class: 'completed single-day' };
		else if (today.getUTCDate() < startDateObj.getUTCDate()) {
			return { percentage: 0, class: 'future single-day' };
		} else return { percentage: 0, class: 'completed single-day' };
	}

	// 情况2: 当前日期 < 开始日期（未来事件，全灰色）
	if (today.getTime() < startDateObj.getTime()) {
		return { percentage: 0, class: 'future' };
	}

	// 情况3: 当前日期 > 结束日期（已完成，全蓝色）
	if (today.getTime() > endDateObj.getTime()) {
		return { percentage: 100, class: 'completed' };
	}

	// 情况4: 当前日期在时间段内（进行中，蓝色 + 灰色）
	// 计算总天数（包含开始和结束日期）
	const totalDays =
		Math.floor((endDateObj.getTime() - startDateObj.getTime()) / (1000 * 60 * 60 * 24)) + 1;
	// 计算已经过去的天数（包含当前日期）
	const passedDays =
		Math.floor((today.getTime() - startDateObj.getTime()) / (1000 * 60 * 60 * 24)) + 1;
	// 计算进度百分比
	const percentage = Math.round((passedDays / totalDays) * 100);

	return { percentage: Math.min(Math.max(percentage, 0), 100), class: 'in-progress' };
});

const displayText = computed(() => {
	const startDate = props.modelValue?.startDate;
	const endDate = props.modelValue?.endDate;

	if (!startDate || !endDate || startDate === '' || endDate === '') return '-';

	const startDateObj = dayjs(timeZoneConvert(startDate));
	const endDateObj = dayjs(timeZoneConvert(endDate));
	// 格式化显示文本，类似 "May 5 - Jun 21"

	if (startDateObj == endDateObj) {
		// 同一天
		return `${startDateObj.format('MMM D')}`;
	} else if (startDateObj.month() == endDateObj.month()) {
		// 同一个月
		return `${startDateObj.format('MMM D')} - ${endDateObj.format('D')}`;
	} else {
		return `${startDateObj.format('MMM D')} - ${endDateObj.format('MMM D')}`;
	}
});

const progressPercentage = computed(() => progressData.value.percentage);
const progressClass = computed(() => progressData.value.class);
</script>

<style lang="scss" scoped>
$bg-color: rgb(196, 196, 196);
$bg-progress: var(--el-text-color-primary);
$full-progress-color: var(--el-color-primary);

.date-range-picker {
	// width: 100%;
	width: 120px;
	// height: 48px;
	position: relative;
	cursor: pointer;
}

.custom-date-picker {
	width: 0px;
	top: 0px;
	margin-left: 50px;
	background: transparent;
	position: absolute;
	cursor: pointer;
}

.progress-bar {
	width: 100%;
	height: 100%;
	background-color: var(--el-fill-color);
	overflow: hidden;
	position: relative;
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: 0 12px;
	@apply rounded-xl;
}

.progress-fill {
	position: absolute;
	top: 0;
	left: 0;
	height: 100%;
	transition: width 0.3s ease;
	z-index: 1;
}

.progress-content {
	position: relative;
	z-index: 2;
	display: flex;
	align-items: center;
	justify-content: center;
	width: 100%;
	pointer-events: none;
}

.days-count {
	color: var(--el-color-white);
	width: 100%;
	z-index: 2;
	display: flex;
	pointer-events: none;
	justify-content: center;
	position: relative;
}

.date-range-text {
	color: var(--el-color-white);
	white-space: nowrap;
}

/* 单日事件 - 全蓝色 */
.progress-fill.single-day.future {
	background-color: $full-progress-color !important;
	width: 100%;
}

/* 单日事件 - 全蓝色 */
.progress-fill.single-day.completed {
	background-color: $bg-progress !important;
	width: 100%;
}

/* 已完成事件 - 全蓝色 */
.progress-fill.completed {
	background-color: $full-progress-color !important;
	width: 100%;
}

/* 进行中事件 - 蓝色进度条，背景灰色 */
.progress-fill.in-progress {
	background-color: $full-progress-color !important;
}

/* 当进度为0时的特殊处理 */
.progress-bar:has(.progress-fill.future) {
	background-color: $bg-progress !important;
}

.progress-bar:has(.progress-fill.in-progress) {
	background-color: $bg-progress;
}

.progress-bar:has(.progress-fill.completed) {
	background-color: $full-progress-color !important;
}

.progress-bar.empty {
	background-color: $bg-color !important;
}
</style>
