<!-- 组件功能：按压enter键后，生成自定义标签。还可以同时选择固定标签 -->
<template>
	<div
		class="flex flex-wrap shrink-0 rounded-lg px-8 py-6"
		:class="hasBg ? 'bg-gray-50 dark:bg-gray-900' : ''"
	>
		<div
			:class="[`w-1/${props.lineNum}`, index < overNum ? 'pb-8' : '']"
			v-for="(key, index) in Object.keys(props.showInfo)"
			:key="index"
		>
			<div class="text-gray-500 text-base font-normal leading-6">
				{{ props.showInfo[key].label }}
			</div>
			<div class="text-black dark:text-white-100 text-xl font-medium leading-6">
				{{ props.showInfo[key].value || '-' }}
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref } from 'vue';

const props = defineProps({
	showInfo: {
		required: true,
		type: Object,
	},
	lineNum: {
		type: Number,
		default: 3,
	},
	hasBg: {
		type: Boolean,
		default: true,
	},
});
const overNum = ref(Math.floor(Object.keys(props.showInfo).length / props.lineNum) * props.lineNum);
</script>
