<template>
	<div class="flex justify-between w-full">
		<el-input
			class="rounded-none"
			v-model="phoneNumber"
			@blur="handleBlur"
			@input="validatePhoneNumber"
			@change="handleChange"
			clearable
		>
			<template #prepend>
				<el-select
					v-model="phoneCode"
					:placeholder="t('sys.placeholder.selectPlaceholder')"
					class="rounded-lg w-[120px]"
					clearable
					filterable
					:filter-method="filterPhoneCode"
					@click.stop="preventBlur"
					@mousedown.stop="preventBlur"
					@blur="handleBlur"
				>
					<el-option
						v-for="item in phoneCodeOptions"
						:key="item.key"
						:label="item.value.dialingCode"
						:value="item.value.dialingCode"
					>
						<div>{{ item.value.description }}</div>
					</el-option>
				</el-select>
			</template>
		</el-input>
	</div>
</template>

<script setup lang="ts">
import { computed, ref, watch, onMounted } from 'vue';
import { appEnum } from '@/stores/modules/appEnum';
import { useI18n } from 'vue-i18n';

const { t } = useI18n();
const appEnumStore = appEnum();

const filiterrPhoneCode = ref(appEnumStore.getPhoneArea);
const phoneCodeOptions = computed(() => filiterrPhoneCode.value);
// 本地存储选择的区号，不受modelValue影响
const localPhoneCode = ref<string | null | undefined>(null);

interface Props {
	// 合并后的值，格式为：phoneNumberPrefixesId{separator}phone
	modelValue: string | null | undefined;
	// 分隔符，默认为空格
	separator?: string;
}

const props = withDefaults(defineProps<Props>(), {
	separator: ' ',
});

const emit = defineEmits(['update:modelValue', 'blur', 'change']);

// 获取默认的美国区号
const getDefaultUSCode = () => {
	const usOption = phoneCodeOptions.value.find((item) => item?.value?.countryCode === 'US');
	return usOption ? usOption.value.dialingCode : null;
};

// 使用计算属性处理手机号
const phoneNumber = computed({
	get() {
		if (!props.modelValue) return null;
		// 检查是否有分隔符
		if (props.modelValue.includes(props.separator)) {
			const parts = props.modelValue.split(props.separator);
			return parts.length > 1 ? parts[1] : null;
		} else if (props.modelValue.startsWith('+')) {
			return null; // 只有区号，没有手机号
		} else {
			return props.modelValue;
		}
	},
	set(val: string | null | undefined) {
		// 如果输入了手机号且有区号，才更新modelValue
		if (val && localPhoneCode.value) {
			emit('update:modelValue', combineValues(localPhoneCode.value, val));
		} else if (val) {
			// 只有手机号没有区号
			emit('update:modelValue', val);
		} else {
			// 没有手机号
			emit('update:modelValue', null);
		}
	},
});

// 使用计算属性处理区号
const phoneCode = computed({
	get() {
		// 优先使用本地存储的区号
		if (localPhoneCode.value) return localPhoneCode.value;
		// 其次从modelValue中获取
		if (!props.modelValue) return null;
		// 如果值中包含分隔符，说明有可能有区号
		if (props.modelValue.includes(props.separator)) {
			const [codeId] = props.modelValue.split(props.separator);
			return codeId || null;
		}
		// 检查是否以+开头（表示有区号）
		else if (props.modelValue.startsWith('+')) {
			return props.modelValue;
		}
		// 否则没有区号
		return null;
	},
	set(val: string | null | undefined) {
		// 保存到本地变量
		localPhoneCode.value = val;
		// 只有在有手机号的情况下才更新modelValue
		if (phoneNumber.value) {
			emit('update:modelValue', combineValues(val, phoneNumber.value));
		}
	},
});

// 合并区号和手机号，使用配置的分隔符
const combineValues = (code: string | null | undefined, number: string | null | undefined) => {
	if (!code && !number) return null;
	// 只有当手机号存在时，才返回带区号的完整值
	if (code && number) return `${code}${props.separator}${number}`;
	// 如果只有区号而没有手机号，返回null
	if (code && !number) return null;
	if (number) return number; // 只有手机号，直接返回手机号
	return null;
};

// 初始化时，只在modelValue为空时设置默认区号
onMounted(() => {
	// 只有当modelValue整体为空时，才设置默认区号
	if (!props.modelValue) {
		// 没有初始值时，设置默认区号（+1）
		const defaultCode = getDefaultUSCode();
		if (defaultCode) {
			localPhoneCode.value = defaultCode;
			// 但不更新到modelValue，因为没有手机号
		}
	} else {
		// 其他情况在watch中处理，不设置默认区号
	}
});

// 监听modelValue的变化
watch(
	() => props.modelValue,
	(newVal) => {
		if (!newVal) {
			// 当modelValue被清空时，保留区号选择
			// 如果localPhoneCode不存在，设置默认区号
			if (!localPhoneCode.value) {
				const defaultCode = getDefaultUSCode();
				if (defaultCode) {
					localPhoneCode.value = defaultCode;
				}
			}
		} else {
			// 如果值中包含分隔符，说明有可能有区号
			if (newVal.includes(props.separator)) {
				const [code] = newVal.split(props.separator);
				if (code) {
					localPhoneCode.value = code as string;
				} else {
					// 没有区号部分
					localPhoneCode.value = null;
				}
			}
			// 检查是否以+开头（表示有区号）
			else if (newVal.startsWith('+')) {
				localPhoneCode.value = newVal;
			}
			// 否则只有手机号，不设置区号
			else {
				localPhoneCode.value = null;
			}
		}
	},
	{ immediate: true } // 立即执行一次，处理初始值
);

// 防止select触发input的blur事件
const preventBlur = (event: Event) => {
	event.preventDefault();
};

// 过滤区号
const filterPhoneCode = (value: string) => {
	filiterrPhoneCode.value = appEnumStore.getPhoneArea.filter((item) => {
		return (
			item.value.description.toLowerCase().includes(value.toLowerCase()) ||
			item.value.countryCode.toLowerCase().includes(value.toLowerCase())
		);
	});
};

// 验证手机号输入
const validatePhoneNumber = (val: string) => {
	phoneNumber.value = val.replace(/[^0-9\s\-()]/g, '');
};

const handleBlur = () => {
	emit('blur');
};

const handleChange = () => {
	emit('change');
};
</script>

<style lang="scss" scoped>
:deep(.el-input .el-input__wrapper) {
	border-radius: 0px 0.5rem 0.5rem 0px !important;
}
</style>

<style lang="scss" scoped>
:deep(.el-select__wrapper) {
	@apply m-0;
}
</style>
