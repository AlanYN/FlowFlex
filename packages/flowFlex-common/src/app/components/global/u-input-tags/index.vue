<!-- 组件功能：按压enter键后，生成自定义标签。还可以同时选择固定标签 -->
<template>
	<div class="layout" :class="{ miniLayout: styleType === 'mini' }" @click="inputTagRef.focus()">
		<!-- 自定义标签样式 -->
		<div v-for="(item, index) in tagsArr" :key="index" class="label-box">
			<span class="label-title">{{ item }}</span>
			<i class="label-close" @click="removeTag(index)"></i>
		</div>
		<!-- 输入框 -->
		<input
			v-model="currentVal"
			:placeholder="props.placeholder"
			@keyup.enter="addTags"
			@blur="addTagsOnBlur"
			:disabled="disabled"
			class="input-tag"
			ref="inputTagRef"
			type="text"
		/>
	</div>
</template>

<script setup lang="ts">
import { ref, toRaw, watch, computed } from 'vue';

// 定义标签验证内容
enum VALIDATE {
	REG, // 正则表达式验证
	LIMIT, // 标签数量验证
	REPEAT, // 标签重复验证
}

// 定义类型， widthDefaults不支持外部导入
interface PropsModel {
	modelValue?: string[];
	limit?: number; // 最多能输入几个标签
	styleType?: string; // 输入框样式，视觉中存在两种高度tag
	placeholder?: string; // 提示信息
	disabled?: boolean; // 是否禁用
}

// 接收父组件参数
const props = withDefaults(defineProps<PropsModel>(), {
	// 双向绑定的值
	modelValue: () => [],
	// 设置参数默认值
	placeholder: 'input and press enter to create a tag', // 提示信息
	styleType: 'mini',
});

// 参数定义
let inputTagRef: any = ref(null); // 输入框对象
let currentVal = ref(''); // 输入的标签内容

const emit = defineEmits(['update:modelValue', 'onValidateTag', 'change']);

// 输入的标签数组
let tagsArr = computed({
	get() {
		return props.modelValue;
	},
	set(value) {
		emit('update:modelValue', value);
	},
});

// 数据监听
watch(
	[() => tagsArr.value],
	() => {
		// 监听输入框内值改变
		emit('change', toRaw(tagsArr.value));
	},
	{ deep: true }
);

const changeState = (val: string[]) => {
	tagsArr.value = val;
};

/**
 * 验证数据
 * @param validateName：验证标签内容是否重复
 * @param from: 来源 custom: 手动输入 fixed: 固定标签
 */
const validateFn = (validateName: string): boolean => {
	// 正则验证标签内容
	if (!validateName || tagsArr.value.includes(validateName)) {
		emit('onValidateTag', VALIDATE.REG);
		return false;
	}

	if (props.limit) {
		if (tagsArr.value.length + 1 > props.limit) {
			// 限制标签个数
			emit('onValidateTag', VALIDATE.LIMIT);
			return false;
		}
	}

	for (let i in tagsArr.value) {
		if (tagsArr.value[i] === validateName) {
			// 判断输入标签是否重复
			emit('onValidateTag', VALIDATE.REPEAT);
			return false;
		}
	}

	return true;
};

// 自定义输入标签，添加到输入框内
const addTags = () => {
	let result = validateFn(currentVal.value);
	if (result) {
		let tag = currentVal.value;
		tagsArr.value.push(tag);
		currentVal.value = '';
	}
};

// 失焦时添加标签
const addTagsOnBlur = () => {
	// 只有当输入框有内容时才添加标签
	if (currentVal.value.trim()) {
		addTags();
	}
};
// 删除标签方法
const removeTag = (index: number) => {
	tagsArr.value.splice(index, 1);
};

defineExpose({
	changeState,
});
</script>

<style scoped lang="scss">
/* 外层div - 统一样式 */
.layout {
	width: 100%;
	min-height: 32px;
	border: 1px solid var(--el-border-color, #dcdfe6);
	padding: 4px 11px;
	background-color: var(--el-fill-color-blank, #ffffff);
	transition: all var(--el-transition-duration, 0.2s);
	box-shadow: 0 0 0 1px transparent inset;
	font-size: 14px;
	display: flex;
	align-items: center;
	flex-wrap: wrap;
	gap: 4px;
	cursor: text;
	box-sizing: border-box;
	text-align: left;
	word-wrap: break-word;
	overflow: hidden;
	@apply rounded-xl;
}

.layout:hover {
	border-color: var(--el-border-color-hover, #c0c4cc);
}

.layout:focus-within {
	border-color: var(--primary-500, #409eff);
	box-shadow: 0 0 0 1px var(--primary-500, #409eff) inset !important;
}

.miniLayout {
	min-height: 32px;
	padding: 4px 11px;

	.input-tag {
		height: 24px;
		line-height: 24px;
		min-width: 100px;
		font-size: 14px;
		padding: 0;
	}
}

/* 标签样式 - 统一设计 */
.label-box {
	height: 24px;
	margin: 0;
	background-color: var(--el-fill-color-light, #f5f7fa);
	border: 1px solid var(--el-border-color-lighter, #e4e7ed);
	display: inline-flex;
	align-items: center;
	padding: 0 8px;
	transition: all 0.2s ease;
	flex-shrink: 0;
	@apply rounded-xl;
}

.label-title {
	font-size: 12px;
	padding: 0;
	line-height: 24px;
	color: var(--el-text-color-regular, #606266);
	font-weight: 500;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
	max-width: 120px;
}

.label-close {
	padding: 0;
	margin-left: 6px;
	color: var(--el-text-color-placeholder, #a8abb2);
	cursor: pointer;
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 16px;
	height: 16px;
	border-radius: 50%;
	background: var(--el-fill-color, #f0f2f5);
	transition: all 0.2s ease;
	transform: none;
}

.label-close:hover {
	background: var(--el-fill-color-dark, #e6e8eb);
	color: var(--el-text-color-regular, #606266);
}

.label-close:after {
	content: '×';
	font-size: 12px;
	line-height: 1;
	font-weight: bold;
}

/* input样式 - 统一设计 */
.input-tag {
	min-width: 100px;
	height: 24px;
	line-height: 24px;
	font-size: 14px;
	color: var(--el-text-color-regular, #606266);
	border: none;
	outline: none;
	background: transparent;
	flex: 1;
	padding: 0;
	box-shadow: none;
}

.input-tag::placeholder {
	color: var(--el-text-color-placeholder, #a8abb2);
	font-size: 14px;
}

/* 暗色主题 - 统一设计 */
html.dark {
	.layout {
		background-color: var(--black-200) !important;
		border: 1px solid var(--black-200) !important;
		color: var(--white-100) !important;
	}

	.layout:hover {
		border-color: var(--black-100) !important;
	}

	.layout:focus-within {
		border-color: var(--primary-500) !important;
		box-shadow: 0 0 0 1px var(--primary-500) inset !important;
	}

	.input-tag {
		color: var(--white-100) !important;
		background-color: transparent !important;
	}

	.input-tag::placeholder {
		color: var(--gray-300) !important;
	}

	.label-box {
		background-color: var(--black-300) !important;
		border: 1px solid var(--black-100) !important;
	}

	.label-title {
		color: var(--white-100) !important;
	}

	.label-close {
		background: var(--black-200) !important;
		color: var(--gray-300) !important;
	}

	.label-close:hover {
		background: var(--black-100) !important;
		color: var(--white-100) !important;
	}
}
</style>
