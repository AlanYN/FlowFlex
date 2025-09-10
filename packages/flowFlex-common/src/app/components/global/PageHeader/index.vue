<template>
	<div
		class="relative rounded-lg overflow-hidden mb-5 shadow-sm transition-all duration-300 ease-in-out"
	>
		<!-- 背景层 -->
		<div class="absolute inset-0 z-10">
			<!-- 渐变背景 -->
			<div
				class="absolute inset-0 bg-gradient-to-br from-primary-50 via-primary-100 to-primary-200 opacity-80"
			></div>
			<!-- 动态图案 -->
			<div class="page-header-pattern absolute inset-0"></div>
		</div>

		<!-- 内容层 -->
		<div class="relative z-20 flex justify-between items-center p-[18px] min-h-[70px]">
			<div class="flex-1 max-w-[70%]">
				<h1
					class="text-2xl font-semibold text-primary-700 m-0 mb-1 leading-tight tracking-tight"
				>
					<slot name="title">{{ title }}</slot>
				</h1>
				<p
					v-if="description || $slots.description"
					class="text-sm text-primary-600 m-0 leading-relaxed opacity-85 font-normal"
				>
					<slot name="description">{{ description }}</slot>
				</p>
			</div>
			<div v-if="$slots.actions" class="flex items-center gap-3 flex-shrink-0">
				<slot name="actions"></slot>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
interface Props {
	title?: string;
	description?: string;
}

withDefaults(defineProps<Props>(), {
	title: '',
	description: '',
});
</script>

<style scoped lang="scss">
/* 动态背景图案动画 */
.page-header-pattern {
	background-image: radial-gradient(
			circle at 25% 40%,
			rgba(255, 255, 255, 0.08) 0%,
			transparent 40%
		),
		radial-gradient(circle at 75% 30%, rgba(255, 255, 255, 0.04) 0%, transparent 40%);
	background-size:
		200px 200px,
		150px 150px;
	background-position:
		0 0,
		80px 0;
	animation: float 30s ease-in-out infinite;
}

@keyframes float {
	0%,
	100% {
		transform: translate(0, 0) rotate(0deg);
	}

	33% {
		transform: translate(10px, -10px) rotate(1deg);
	}

	66% {
		transform: translate(-5px, 5px) rotate(-1deg);
	}
}

/* 标题渐变文字效果 */
h1 {
	text-shadow: 0 1px 2px rgba(0, 0, 0, 0.05);
	background: linear-gradient(135deg, var(--primary-600), var(--primary-800));
	-webkit-background-clip: text;
	-webkit-text-fill-color: transparent;
	background-clip: text;
}

/* 为插槽中的按钮提供统一样式类 */
:deep(.page-header-btn) {
	@apply font-medium text-sm px-4 py-2 rounded-md transition-all duration-300 ease-in-out relative overflow-hidden border-0;
}

:deep(.page-header-btn-primary) {
	@apply text-white shadow-lg;
	background: linear-gradient(135deg, var(--primary-500), var(--primary-600));
	box-shadow:
		0 2px 8px rgba(var(--primary-500-rgb), 0.2),
		0 1px 3px rgba(0, 0, 0, 0.1);
}

:deep(.page-header-btn-primary::before) {
	content: '';
	@apply absolute top-0 w-full h-full transition-all duration-500 ease-in-out;
	left: -100%;
	background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.2), transparent);
}

:deep(.page-header-btn-primary:hover) {
	@apply -translate-y-0.5;
	background: linear-gradient(135deg, var(--primary-600), var(--primary-700));
	box-shadow:
		0 4px 12px rgba(var(--primary-500-rgb), 0.3),
		0 2px 6px rgba(0, 0, 0, 0.1);
}

:deep(.page-header-btn-primary:hover::before) {
	left: 100%;
}

:deep(.page-header-btn-primary:active) {
	@apply -translate-y-px;
}

:deep(.page-header-btn-secondary) {
	@apply text-primary-600 border border-primary-200;
	background: rgba(var(--primary-500-rgb), 0.1);
}

:deep(.page-header-btn-secondary:hover) {
	@apply -translate-y-px shadow-lg;
	background: rgba(var(--primary-500-rgb), 0.15);
	box-shadow: 0 2px 8px rgba(var(--primary-500-rgb), 0.2);
}

:deep(.page-header-btn .mr-2) {
	@apply mr-1.5;
}

/* 响应式设计 - 移动端调整 */
@media (max-width: 768px) {
	.page-header-content {
		@apply flex-col items-start gap-3 px-5 py-4 min-h-0;
	}

	.page-header-info {
		@apply max-w-full;
	}

	h1 {
		@apply text-xl;
	}

	p {
		@apply text-xs;
	}

	.page-header-actions {
		@apply w-full justify-start;
	}
}

/* 暗色主题支持 */
html.dark {
	/* 暗色渐变背景 */
	.bg-gradient-to-br {
		background: linear-gradient(
			135deg,
			var(--black-100) 0%,
			var(--black-200) 50%,
			var(--primary-800) 100%
		);
	}

	/* 暗色标题样式 */
	h1 {
		@apply text-white-100;
		background: linear-gradient(135deg, var(--primary-400), var(--primary-300));
		-webkit-background-clip: text;
		-webkit-text-fill-color: transparent;
		background-clip: text;
	}

	/* 暗色描述文字 */
	p {
		@apply text-gray-300;
	}

	/* 暗色模式下隐藏背景图案 */
	.page-header-pattern {
		@apply hidden;
	}

	/* 暗色阴影 - 增强边界效果 */
	.shadow-sm {
		box-shadow:
			0 2px 8px 0 rgba(0, 0, 0, 0.4),
			0 1px 3px 0 rgba(0, 0, 0, 0.3),
			inset 0 1px 0 rgba(255, 255, 255, 0.1);
	}
}

/* 高对比度模式支持 */
@media (prefers-contrast: high) {
	.bg-gradient-to-br {
		@apply bg-primary-100;
		background-image: none;
	}

	h1 {
		@apply text-primary-900;
		background: none !important;
		-webkit-text-fill-color: initial;
	}

	.page-header-pattern {
		@apply hidden;
	}
}

/* 减少动画模式支持 */
@media (prefers-reduced-motion: reduce) {
	.page-header-pattern {
		@apply animate-none;
	}

	:deep(.page-header-btn-primary::before) {
		@apply transition-none;
	}

	:deep(.page-header-btn) {
		@apply transition-none;
	}
}
</style>
