<template>
	<transition name="fold-transition">
		<div
			v-show="isFold || !isFold"
			:class="isFold ? 'column-content-row' : 'column-content-col'"
		>
			<div :class="isFold ? 'column-header-row' : 'column-header-col'">
				<div v-show="!isFold">{{ total }}</div>
				<div class="truncate" :title="title">{{ title }}</div>
				<div class="flex gap-4">
					<div v-show="isFold">{{ total }}</div>
					<el-button
						class="w-[26px] h-[25px]"
						:icon="isFold ? ArrowLeft : ArrowRight"
						@click="changeIsFold"
					/>
				</div>
			</div>
			<div class="h-full" v-show="isFold">
				<slot name="row"></slot>
			</div>
		</div>
	</transition>
</template>

<script lang="ts" setup>
import { ref } from 'vue';
import { ArrowLeft, ArrowRight } from '@element-plus/icons-vue';

interface Props {
	title: string;
	total: number;
}

const isFold = ref(true);
const changeIsFold = () => {
	isFold.value = !isFold.value;
};

defineProps<Props>();
</script>

<style lang="scss" scoped>
.column-content-row {
	width: 300px;
	margin-top: 60px;
	overflow-y: auto;
	height: calc(100vh - 440px);
}

.column-content-col {
	width: 50px;
	margin-top: 60px;
	overflow-y: auto;
	height: calc(100vh - 440px);
}

.column-header-row {
	padding: 10px 15px;

	margin-bottom: 10px;
	border-bottom: 1px solid var(--black-500);
	position: absolute;
	top: 0;
	left: 0;
	right: 0;

	@apply flex justify-between font-bold bg-white dark:bg-black-200;
}

.column-header-col {
	padding: 10px 15px;
	position: absolute;
	top: 0;
	left: 0;
	right: 0;
	writing-mode: vertical-rl;
	/* 设置为竖直排列，从右到左 */
	height: 100%;

	@apply font-bold flex flex-row-reverse items-center justify-end gap-[10px] bg-white dark:bg-black-200;
}

/* 添加过渡动画样式 */
.fold-transition-enter-active,
.fold-transition-leave-active {
	transition: all 0.3s ease;
}
.fold-transition-enter-from,
.fold-transition-leave-to {
	opacity: 0;
	transform: translateX(50px);
}
</style>
