<!-- 组件功能：使用 Element Plus 的 el-input-tag 组件实现标签输入 -->
<template>
	<el-input-tag
		v-model="tagsArr"
		ref="inputTagRef"
		:placeholder="props.placeholder"
		:disabled="disabled"
		:max="props.limit"
		:size="styleType === 'mini' ? 'small' : 'default'"
		tag-type="primary"
		class="u-input-tags"
		@change="handleChange"
		@add-tag="handleAddTag"
		@remove-tag="handleRemoveTag"
	/>
</template>

<script setup lang="ts">
import { ref, toRaw, computed } from 'vue';

// 定义标签验证内容
enum VALIDATE {
	REG, // 正则表达式验证
	LIMIT, // 标签数量验证
	REPEAT, // 标签重复验证
}

// 定义类型
interface PropsModel {
	modelValue?: string[];
	limit?: number; // 最多能输入几个标签
	styleType?: string; // 输入框样式，视觉中存在两种高度tag
	placeholder?: string; // 提示信息
	disabled?: boolean; // 是否禁用
}

// 接收父组件参数
const props = withDefaults(defineProps<PropsModel>(), {
	modelValue: () => [],
	placeholder: 'input and press enter to create a tag',
	styleType: 'mini',
});

// 参数定义
const inputTagRef = ref(); // 输入框对象

const emit = defineEmits(['update:modelValue', 'onValidateTag', 'change']);

// 输入的标签数组（双向绑定）
const tagsArr = computed({
	get() {
		return props.modelValue;
	},
	set(value) {
		emit('update:modelValue', value);
	},
});

// 处理标签变化事件
const handleChange = (value?: string[]) => {
	emit('change', toRaw(value || []));
};

// 处理标签添加事件
const handleAddTag = (value?: string) => {
	// 检查是否为空
	if (!value || !value.trim()) {
		emit('onValidateTag', VALIDATE.REG);
		return;
	}

	// 检查重复（el-input-tag 会自动处理，但仍然发出事件）
	if (tagsArr.value.includes(value)) {
		emit('onValidateTag', VALIDATE.REPEAT);
	}
};

// 处理标签删除事件
const handleRemoveTag = (value?: string) => {
	// 标签删除回调，可在此添加额外逻辑
	if (value) {
		console.log('Tag removed:', value);
	}
};

// 外部调用方法：改变状态
const changeState = (val: string[]) => {
	tagsArr.value = val;
};

defineExpose({
	changeState,
});
</script>
