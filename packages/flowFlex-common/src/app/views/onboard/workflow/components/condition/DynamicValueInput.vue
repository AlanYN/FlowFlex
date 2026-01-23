<template>
	<div class="dynamic-value-input">
		<!-- 下拉选择类型 -->
		<el-select
			v-if="inputType === 'select'"
			:model-value="modelValue"
			placeholder="Select value"
			:multiple="!!constraints?.allowMultiple"
			@update:model-value="handleChange"
			clearable
			tag-type="primary"
		>
			<el-option
				v-for="opt in options"
				:key="opt.value"
				:label="opt.label"
				:value="opt.value"
			/>
		</el-select>

		<!-- 数字输入类型 -->
		<InputNumber
			v-else-if="inputType === 'number'"
			:model-value="modelValue"
			:is-foloat="numberConstraints.isFloat"
			:minus-number="numberConstraints.allowNegative"
			:is-financial="numberConstraints.isFinancial"
			:decimal-places="numberConstraints.decimalPlaces"
			:property="{ placeholder: placeholder || 'Enter number' }"
			@update:model-value="handleChange"
		/>

		<!-- 日期选择类型 -->
		<el-date-picker
			v-else-if="inputType === 'date'"
			:model-value="modelValue"
			:type="dateConstraints.dateType"
			:placeholder="placeholder || 'Select date'"
			:format="dateConstraints.dateFormat"
			:value-format="dateConstraints.dateFormat"
			class="w-full"
			@update:model-value="handleChange"
		/>

		<!-- 人员选择类型 -->
		<FlowflexUserSelector
			v-else-if="inputType === 'people'"
			:model-value="modelValue"
			selection-type="user"
			:placeholder="placeholder || 'Select user'"
			@update:model-value="handleUserChange"
		/>

		<!-- 电话输入类型 -->
		<MergedArea
			v-else-if="inputType === 'phone'"
			:model-value="modelValue"
			@update:model-value="handleChange"
		/>

		<!-- 默认文本输入 -->
		<el-input
			v-else
			:model-value="modelValue"
			:placeholder="placeholder || 'Enter value'"
			:maxlength="textConstraints.maxLength"
			:show-word-limit="textConstraints.showWordLimit"
			@update:model-value="handleChange"
		/>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { DynamicFieldConstraints, ValueOption } from '#/condition';
import FlowflexUserSelector from '@/components/form/flowflexUser/index.vue';
import InputNumber from '@/components/form/InputNumber/index.vue';
import MergedArea from '@/components/form/inputPhone/mergedArea.vue';

// 值输入类型
type ValueInputType = 'text' | 'number' | 'select' | 'date' | 'people' | 'phone';

// Props
const props = defineProps<{
	modelValue: any;
	inputType: ValueInputType;
	options?: ValueOption[];
	constraints?: DynamicFieldConstraints;
	placeholder?: string;
}>();

// Emits
const emit = defineEmits<{
	(e: 'update:modelValue', value: any): void;
}>();

// 计算属性：数字类型约束
const numberConstraints = computed(() => ({
	isFloat: props.constraints?.isFloat ?? true,
	allowNegative: props.constraints?.allowNegative ?? false,
	isFinancial: props.constraints?.isFinancial ?? false,
	decimalPlaces: props.constraints?.decimalPlaces ?? 2,
}));

// 计算属性：日期类型约束
const dateConstraints = computed(() => ({
	dateType: props.constraints?.dateType || 'date',
	dateFormat: props.constraints?.dateFormat || 'YYYY-MM-DD',
}));

// 计算属性：文本类型约束
const textConstraints = computed(() => ({
	maxLength: props.constraints?.maxLength,
	showWordLimit: !!props.constraints?.maxLength,
}));

// 处理值变化
const handleChange = (value: string | number | null | undefined) => {
	emit('update:modelValue', value != null ? value : '');
};

// 处理用户选择变化（FlowflexUserSelector 返回 string | string[] | undefined）
const handleUserChange = (value: string | string[] | undefined) => {
	emit('update:modelValue', value || '');
};
</script>

<style lang="scss" scoped>
.dynamic-value-input {
	width: 100%;
}
</style>
