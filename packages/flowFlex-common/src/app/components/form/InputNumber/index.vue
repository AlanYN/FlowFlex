<template>
	<el-input
		ref="inputRef"
		v-model="displayValue"
		:type="fieldProperty.type"
		:clearable="fieldProperty.clearable"
		:placeholder="fieldProperty.placeholder"
		:maxlength="fieldProperty.maxlength"
		:readonly="fieldProperty.readonly"
		@blur="blur"
		@change="change"
		@input="handleInput"
		:disabled="fieldProperty.disabled"
	>
		<!-- 如果是财务格式，添加 $ 前缀 -->
		<template v-if="showFinancialPrefix" #prefix>$</template>
	</el-input>
</template>
<script lang="ts">
import { computed, ref } from 'vue';
import { toFloatNumber, toIntegerNumber } from '@/utils/formField';
import { formatToFinancial } from '@/utils';
import { ElInput } from 'element-plus';

export default {
	name: 'InputNumber',
	props: {
		modelValue: [String, Number], // 组件绑定值
		content: [String, Number], // 组件绑定值
		property: {
			type: Object,
			default() {
				return {};
			},
		},
		isFoloat: {
			type: Boolean,
			default: true, // 输入框限制是不是浮点行 true表示输入的是浮点
		},
		minusNumber: {
			type: Boolean,
			default: false, // 是否可以输入负数
		},
		minNumber: {
			type: Number,
			default: 0, // 最小值
		},
		isFinancial: {
			type: Boolean,
			default: false, // 是否使用财务格式
		},
		decimalPlaces: {
			type: Number,
			default: 5, // 小数位数，默认2位
		},
	},
	setup(props, { emit, expose }) {
		const fieldProperty = computed(() => {
			return {
				placeholder: '', // 提示语
				maxlength: -1, // 可输入最大长度
				readonly: false, // 是否只读
				disabled: false, // 是否可输入
				clearable: false, // 是否可清空输入框
				type: 'text', // 输入框类型 input | textarea | password | text
				...props.property,
			};
		});

		// 是否正在编辑状态
		const isEditing = ref(false);

		// 是否显示财务前缀
		const showFinancialPrefix = computed(() => {
			// 只要是财务格式就显示
			return props.isFinancial;
		});

		// 显示值
		const displayValue = computed({
			get() {
				// 如果正在编辑，直接返回原始值
				if (isEditing.value) {
					return props.modelValue;
				}

				// 如果是财务格式，且有值，则格式化
				return props.isFinancial && props.modelValue
					? formatToFinancial(props.modelValue)
					: props.modelValue;
			},
			set(val) {
				// 设置值时，移除格式化的逗号和货币符号
				const cleanValue = val ? val.toString().replace(/[,$]/g, '') : val;
				emit('update:modelValue', cleanValue);
			},
		});

		const change = (val: string) => {
			handleInput(val);
		};

		const handleInput = (value: string) => {
			// 进入编辑状态
			isEditing.value = true;

			// 移除格式化的字符
			const cleanValue = value.replace(/[,$]/g, '');

			// 处理数字输入
			const processedValue = props.isFoloat
				? toFloatNumber(cleanValue, props.minNumber, props.minusNumber, props.decimalPlaces)
				: toIntegerNumber(cleanValue, props.minNumber, props.minusNumber);

			emit('update:modelValue', processedValue);
		};

		const blur = () => {
			// 退出编辑状态
			isEditing.value = false;

			if (props.minusNumber && !/\d/.test(displayValue.value as string)) {
				displayValue.value = '';
			}

			// 如果是财务格式，在失焦时格式化
			if (props.isFinancial && displayValue.value) {
				const cleanValue = displayValue.value.toString().replace(/[,$]/g, '');
				emit('update:modelValue', cleanValue);
			}

			emit('fieldBlur', displayValue.value); // 触发
		};

		const inputRef = ref<InstanceType<typeof ElInput> | null>(null);
		expose({
			focus: () => {
				inputRef.value!.focus();
			},
			select: () => {
				inputRef.value!.select();
			},
			clear: () => {
				inputRef.value!.clear();
			},
		});

		return {
			displayValue,
			change,
			blur,
			fieldProperty,
			handleInput,
			inputRef,
			showFinancialPrefix,
		};
	},
};
</script>
<style lang="less" scoped>
// 调整前缀样式，使其更加美观
:deep(.el-input__prefix) {
	display: flex;
	align-items: center;
	padding-right: 4px;
	color: var(--el-text-color-secondary);
}
</style>
