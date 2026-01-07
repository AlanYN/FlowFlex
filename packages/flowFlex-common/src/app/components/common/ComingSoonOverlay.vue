<template>
	<div class="coming-soon-overlay" :class="{ 'is-absolute': absolute }">
		<div class="overlay-backdrop"></div>
		<div class="overlay-content">
			<el-icon class="overlay-icon" :size="iconSize">
				<Clock />
			</el-icon>
			<h3 class="overlay-title">{{ title }}</h3>
			<p v-if="description" class="overlay-description">{{ description }}</p>
			<slot name="extra"></slot>
		</div>
	</div>
</template>

<script setup lang="ts">
import { Clock } from '@element-plus/icons-vue';

interface Props {
	title?: string;
	description?: string;
	iconSize?: number;
	absolute?: boolean; // true: 绝对定位覆盖父容器, false: 固定定位覆盖整个视口
}

withDefaults(defineProps<Props>(), {
	title: 'Coming Soon',
	description: 'This feature is under development and will be available soon.',
	iconSize: 48,
	absolute: true,
});
</script>

<style scoped lang="scss">
.coming-soon-overlay {
	position: fixed;
	top: 0;
	left: 0;
	right: 0;
	bottom: 0;
	z-index: 1000;
	display: flex;
	align-items: center;
	justify-content: center;

	&.is-absolute {
		position: absolute;
	}

	.overlay-backdrop {
		position: absolute;
		top: 0;
		left: 0;
		right: 0;
		bottom: 0;
		background-color: rgba(255, 255, 255, 0.85);
		backdrop-filter: blur(4px);

		:root.dark & {
			background-color: rgba(18, 18, 23, 0.92); // --black-500 的深色模式值
		}
	}

	.overlay-content {
		position: relative;
		z-index: 1;
		text-align: center;
		padding: 2rem;
		max-width: 400px;
	}

	.overlay-icon {
		color: var(--el-color-primary);
		margin-bottom: 1rem;
		animation: pulse 2s ease-in-out infinite;
	}

	.overlay-title {
		font-size: 1.5rem;
		font-weight: 600;
		color: var(--el-text-color-primary);
		margin: 0 0 0.5rem;
	}

	.overlay-description {
		font-size: 0.875rem;
		color: var(--el-text-color-secondary);
		margin: 0;
		line-height: 1.5;
	}
}

@keyframes pulse {
	0%,
	100% {
		opacity: 1;
		transform: scale(1);
	}
	50% {
		opacity: 0.7;
		transform: scale(1.05);
	}
}
</style>
