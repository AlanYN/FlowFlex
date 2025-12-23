<template>
	<div class="flex w-full gap-x-2">
		<el-select
			v-if="props.showMonthOptions"
			v-model="unitDiscountModel.invoiceMonth"
			:disabled="disabled"
			:placeholder="`${t('sys.placeholder.selectPlaceholder')} month`"
			class="rounded-xl w-[150px]"
			clearable
			filterable
			@change="handleMonthChange"
		>
			<el-option
				v-for="item in monthOptions"
				:key="item.value"
				:label="item.label"
				:value="item.value"
			/>
		</el-select>
		<el-select
			v-model="unitDiscountModel.invoiceDate"
			:disabled="disabled"
			:placeholder="`${t('sys.placeholder.selectPlaceholder')} date`"
			class="rounded-xl w-[150px]"
			clearable
			filterable
		>
			<el-option
				v-for="item in dateOptions"
				:key="item.value"
				:label="item.label"
				:value="item.value"
			/>
		</el-select>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { getYearMonthDays, convertToSelectOptions } from '@/utils/dateUtils';

const { t } = useI18n();

// 获取年份数据
const yearData = getYearMonthDays();
const { monthOptions, getDaysForMonth } = convertToSelectOptions(yearData);

interface Props {
	modelValue: {
		invoiceMonth: string;
		invoiceDate: string;
		[key: string]: any;
	};
	showMonthOptions?: boolean;
	disabled?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	showMonthOptions: true,
	disabled: false,
});

const emit = defineEmits(['update:modelValue']);

const unitDiscountModel = computed({
	get() {
		return props.modelValue;
	},
	set(val) {
		emit('update:modelValue', val);
	},
});

// 使用计算属性替代 ref
const dateOptions = computed(() => {
	// 如果不显示月份选项，默认显示1-31的日期
	if (!props.showMonthOptions) {
		return Array.from({ length: 31 }, (_, index) => ({
			label: `${index + 1}`,
			value: index + 1,
		}));
	}

	// 如果显示月份选项，且已选择月份，则返回对应月份的日期
	if (unitDiscountModel.value.invoiceMonth) {
		return getDaysForMonth(Number(unitDiscountModel.value.invoiceMonth));
	}

	// 默认返回空数组
	return [];
});

// 月份变化时重置日期选择
const handleMonthChange = () => {
	unitDiscountModel.value.invoiceDate = '';
};
</script>
