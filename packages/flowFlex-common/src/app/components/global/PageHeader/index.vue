<template>
	<div
		class="relative rounded-xl overflow-hidden mb-5 shadow-sm transition-all duration-300 ease-in-out"
	>
		<!-- 背景层 -->
		<div class="absolute inset-0 z-10">
			<!-- 渐变背景 -->
			<div
				class="absolute inset-0 bg-gradient-to-r from-primary-600 to-indigo-600 opacity-80"
			></div>
		</div>

		<!-- 内容层 -->
		<div class="relative z-20 flex justify-between items-center p-[18px] min-h-[70px]">
			<div class="flex items-center flex-1 max-w-[70%]">
				<!-- 返回按钮 -->
				<el-button
					v-if="showBackButton"
					link
					size="small"
					@click="handleGoBack"
					class="mr-3 !p-1 hover:bg-white/20 rounded-xl transition-colors"
				>
					<el-icon class="text-lg text-white">
						<ArrowLeft />
					</el-icon>
				</el-button>

				<div class="flex-1">
					<h1
						class="text-2xl font-semibold text-white m-0 mb-1 leading-tight tracking-tight"
					>
						<slot name="title">{{ title }}</slot>
					</h1>
					<p
						v-if="description || $slots.description"
						class="text-sm text-white m-0 leading-relaxed opacity-85 font-normal"
					>
						<slot name="description">{{ description }}</slot>
					</p>
				</div>
			</div>
			<div v-if="$slots.actions" class="flex items-center flex-shrink-0">
				<slot name="actions"></slot>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ArrowLeft } from '@element-plus/icons-vue';

interface Props {
	title?: string;
	description?: string;
	showBackButton?: boolean;
}

withDefaults(defineProps<Props>(), {
	title: '',
	description: '',
	showBackButton: false,
});

const emit = defineEmits<{
	'go-back': [];
}>();

const handleGoBack = () => {
	emit('go-back');
};
</script>

<style scoped lang="scss">
/* 标题白色文字效果 */
h1 {
	text-shadow: 0 1px 3px rgba(0, 0, 0, 0.3);
}

/* 为插槽中的按钮提供统一样式类 */
:deep(.page-header-btn) {
	@apply font-medium text-sm px-4 py-2 rounded-xl transition-all duration-300 ease-in-out relative overflow-hidden border-0;
}

:deep(.page-header-btn-primary) {
	@apply text-white shadow-lg;
	background: linear-gradient(135deg, #6366f1, #4f46e5);
	box-shadow:
		0 2px 8px rgba(99, 102, 241, 0.3),
		0 1px 3px rgba(0, 0, 0, 0.2);
}

:deep(.page-header-btn-primary::before) {
	content: '';
	@apply absolute top-0 w-full h-full transition-all duration-500 ease-in-out;
	left: -100%;
	background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.2), transparent);
}

:deep(.page-header-btn-primary:hover) {
	@apply -translate-y-0.5;
	background: linear-gradient(135deg, #4f46e5, #3730a3);
	box-shadow:
		0 4px 12px rgba(99, 102, 241, 0.4),
		0 2px 6px rgba(0, 0, 0, 0.2);
}

:deep(.page-header-btn-primary:hover::before) {
	left: 100%;
}

:deep(.page-header-btn-primary:active) {
	@apply -translate-y-px;
}

:deep(.page-header-btn-secondary) {
	@apply text-white border border-white/30;
	background: rgba(255, 255, 255, 0.1);
}

:deep(.page-header-btn-secondary:hover) {
	@apply -translate-y-px shadow-lg;
	background: rgba(255, 255, 255, 0.2);
	box-shadow: 0 2px 8px rgba(255, 255, 255, 0.3);
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
		@apply text-white;
		background: none !important;
		-webkit-background-clip: unset !important;
		-webkit-text-fill-color: unset !important;
		background-clip: unset !important;
		text-shadow: 0 1px 3px rgba(0, 0, 0, 0.5);
	}

	/* 暗色描述文字 */
	p {
		@apply text-white;
		opacity: 0.8;
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
