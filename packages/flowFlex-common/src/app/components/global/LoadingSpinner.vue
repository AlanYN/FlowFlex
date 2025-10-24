<template>
	<div class="loading-spinner">
		<div class="dot dot-spin">
			<i></i><i></i><i></i><i></i>
		</div>
		<div v-if="text" class="loading-text">{{ text }}</div>
		<el-progress
			v-if="showProgress && percentage > 0"
			:percentage="percentage"
			:show-text="false"
			class="progress-bar"
		/>
	</div>
</template>

<script setup lang="ts">
interface Props {
	text?: string;
	showProgress?: boolean;
	percentage?: number;
}

withDefaults(defineProps<Props>(), {
	showProgress: false,
	percentage: 0,
});
</script>

<style scoped lang="scss">
.loading-spinner {
	display: flex;
	flex-direction: column;
	align-items: center;
	gap: 20px;
}

.loading-text {
	font-size: 16px;
	font-weight: 500;
	@apply text-gray-800 dark:text-gray-200;
}

.progress-bar {
	width: 200px;
}

.dot {
	display: inline-block;
	position: relative;
	width: 48px;
	height: 48px;
	transform: rotate(45deg);
	animation: ant-rotate 1.2s infinite linear;
}

.dot i {
	display: block;
	position: absolute;
	width: 20px;
	height: 20px;
	transform: scale(0.75);
	animation: ant-spin-move 1s infinite linear alternate;
	border-radius: 100%;
	opacity: 0.3;
}

.dot i:nth-child(1) {
	top: 0;
	left: 0;
	background-color: #e1251b;
}

.dot i:nth-child(2) {
	top: 0;
	right: 0;
	animation-delay: 0.4s;
	background-color: #00833e;
}

.dot i:nth-child(3) {
	right: 0;
	bottom: 0;
	animation-delay: 0.8s;
	background-color: #238dc1;
}

.dot i:nth-child(4) {
	bottom: 0;
	left: 0;
	animation-delay: 1.2s;
	background-color: #f7be00;
}

@keyframes ant-rotate {
	to {
		transform: rotate(405deg);
	}
}

@keyframes ant-spin-move {
	to {
		opacity: 1;
	}
}
</style>

