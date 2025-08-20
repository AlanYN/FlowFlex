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
/* 外层div */
.layout {
	width: 100%;
	display: flex;
	align-items: center;
	flex-wrap: wrap;
	min-height: 47px;
	box-sizing: border-box;
	border: 1px solid;
	border-radius: 8px;
	font-size: 12px;
	text-align: left;
	padding: 6px 10px;
	word-wrap: break-word;
	overflow: hidden;
	cursor: text;
	@apply border-gray-200 bg-white-100;
}

.miniLayout {
	min-height: 32px;
	padding: 0 5px;

	.input-tag {
		height: 32px;
		line-height: 32px;
		width: 250px;
		font-size: 0.875rem;
		padding: 0 5px;
	}
}

/* 标签 */
.label-box {
	flex-shrink: 0;
	height: 25px;
	margin: 2px 5px;
	display: inline-block;
	padding: 2px;
	border: 1px solid;
	border-radius: 6px;
	@apply border-gray-400 bg-gray-100;
}

.label-title {
	line-height: 18px;
	max-width: 99%;
	position: relative;
	display: inline-block;
	padding-left: 8px;
	color: #495060;
	font-size: 14px;
	opacity: 1;
	overflow: hidden;
	transition: 0.25s linear;
}

.label-close {
	padding: 0 10px 5px 8px;
	display: inline-block;
	opacity: 1;
	filter: none;
	cursor: pointer;
	transform: translateY(-6px);
}

.label-close:after {
	content: 'x';
	-webkit-font-smoothing: antialiased;
	-moz-osx-font-smoothing: grayscale;
	line-height: 27px;
}

/* input */
.input-tag {
	font-size: 12px;
	border: none;
	box-shadow: none;
	outline: none;
	background-color: transparent;
	padding: 0;
	// width: 100%;
	min-width: 150px;
	height: 35px;
	line-height: 35px;
}

.dark {
	.layout {
		border: 0;
		@apply bg-primary-500;
	}

	.label-box {
		@apply bg-transparent;

		.label-title {
			@apply text-gray-50;
		}

		.label-close {
			@apply text-gray-400;
		}
	}
}
</style>
