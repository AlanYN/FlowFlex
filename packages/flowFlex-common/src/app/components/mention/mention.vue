<template>
	<el-mention
		ref="selectRef"
		v-model="formattedValue"
		:options="assignOptions"
		:loading="optionsLoading"
		placeholder="Please input"
		@search="remoteMethod"
		@select="handleSelect"
		@blur="handleBlur"
		type="textarea"
		whole
		show-arrow
		:maxlength="textraTwoHundredLength"
		show-word-limit
		:rows="inputTextraAutosize.minRows"
		:autoSize="inputTextraAutosize"
	>
		<template #loading>
			<svg class="circular" viewBox="0 0 50 50">
				<circle class="path" cx="25" cy="25" r="20" fill="none" />
			</svg>
		</template>
	</el-mention>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import { ElMention } from 'element-plus';
import { assginToOprions } from '@/hooks/searchAssginTo';
import { textraTwoHundredLength, inputTextraAutosize } from '@/settings/projectSetting';

const { assignOptions, optionsLoading, remoteMethod } = assginToOprions();

const props = defineProps<{
	modelValue: string;
}>();

const emit = defineEmits(['update:modelValue', 'blur']);

const selectRef = ref<InstanceType<typeof ElMention>>();
// 使用计算属性处理双向绑定
const formattedValue = computed({
	// 从存储格式[~用户名]转换为显示格式@用户名
	get() {
		if (!props.modelValue) return '';
		return props.modelValue.replace(/\[~(\S+?)\]/g, '@$1');
	},
	// 从显示格式@用户名转换为存储格式[~用户名]
	set(val: string) {
		if (!val) {
			emit('update:modelValue', '');
			return;
		}
		const storageValue = val.replace(/@(\S+)/g, '[~$1]');
		emit('update:modelValue', storageValue);
	},
});

// 处理用户选择
const handleSelect = (option: any) => {
	console.log('Selected option:', option);

	// const currentText = formattedValue.value;
	// const lastAtIndex = currentText.lastIndexOf('@');

	// if (lastAtIndex !== -1) {
	// 	// 构建新的显示文本
	// 	const prefix = currentText.substring(0, lastAtIndex);
	// 	// 自动在用户名后添加空格，便于继续输入
	// 	const newDisplayText = prefix + `@${option.value} `;

	// 	// 使用计算属性的setter会自动处理格式转换
	// 	formattedValue.value = newDisplayText;
	// }
};

const handleBlur = () => {
	emit('blur');
};

defineExpose({
	focus: () => {
		// 使用input实例调用focus方法
		selectRef.value?.input?.focus();
	},
});
</script>
