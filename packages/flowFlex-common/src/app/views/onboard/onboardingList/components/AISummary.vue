<template>
	<div class="wfe-global-block-bg !rounded-3xl" v-if="showAISummarySection">
		<!-- 统一的头部卡片 -->
		<div
			class="ai-summary-header-card rounded-3xl"
			:class="{ expanded: isExpanded }"
			@click="toggleExpanded"
		>
			<div class="flex items-center justify-between">
				<div class="flex items-center gap-2 min-w-0 flex-1">
					<el-icon
						class="expand-icon text-sm flex-shrink-0"
						:class="{ rotated: isExpanded }"
					>
						<ArrowRight />
					</el-icon>
					<h3 class="ai-summary-title flex-shrink-0">AI Summary</h3>
					<div v-if="loading" class="ai-status-badge generating flex-shrink-0">
						<div class="status-dot"></div>
						<span class="text-xs">Generating...</span>
					</div>
					<div v-else-if="currentAISummary" class="ai-status-badge ready flex-shrink-0">
						<div class="status-dot"></div>
						<span class="text-xs">Ready</span>
					</div>
					<div v-else class="ai-status-badge idle flex-shrink-0">
						<div class="status-dot"></div>
						<span class="text-xs">Idle</span>
					</div>
					<!-- 时间戳信息 -->
					<div
						v-if="currentAISummaryGeneratedAt && currentAISummary"
						class="text-xs text-white/70 truncate"
					>
						Generated: {{ timeZoneConvert(currentAISummaryGeneratedAt) }}
					</div>
				</div>
				<el-button
					:icon="Refresh"
					size="small"
					circle
					:loading="loading"
					@click.stop="handleRefresh"
					title="Regenerate AI Summary"
					class="ai-refresh-btn flex-shrink-0"
				/>
			</div>
		</div>

		<!-- 可折叠AI Summary内容 -->
		<el-collapse-transition>
			<div v-show="isExpanded" class="ai-summary-content p-4">
				<!-- AI Summary内容 -->
				<div v-if="currentAISummary" class="ai-content-display">
					<div class="ai-content-wrapper">
						<p
							class="break-words word-wrap text-sm leading-7 ai-content-text overflow-hidden"
							:class="{ 'ai-streaming': loading }"
						>
							{{ currentAISummary }}
							<span v-if="loading" class="ai-typing-cursor">|</span>
						</p>
					</div>
				</div>

				<!-- 加载状态 -->
				<div v-else-if="loading" class="ai-loading-state">
					<div class="ai-loading-animation">
						<div class="loading-brain">
							<div class="brain-wave"></div>
							<div class="brain-wave"></div>
							<div class="brain-wave"></div>
						</div>
					</div>
					<div class="ai-loading-text">
						<div class="text-sm font-medium ai-loading-title mb-1">
							AI is analyzing your data
						</div>
						<div class="text-xs ai-loading-subtitle">
							{{ loadingText }}
						</div>
					</div>
					<div class="ai-loading-progress">
						<div class="progress-bar"></div>
					</div>
				</div>

				<!-- 空状态 -->
				<div v-else class="ai-empty-state">
					<div class="empty-icon">
						<svg
							width="48"
							height="48"
							viewBox="0 0 24 24"
							fill="none"
							xmlns="http://www.w3.org/2000/svg"
						>
							<path
								d="M12 2L13.09 5.5L16 6L13.09 6.5L12 10L10.91 6.5L8 6L10.91 5.5L12 2Z"
								fill="currentColor"
								opacity="0.3"
							/>
							<path
								d="M18 8L18.82 10.5L21 11L18.82 11.5L18 14L17.18 11.5L15 11L17.18 10.5L18 8Z"
								fill="currentColor"
								opacity="0.3"
							/>
							<path
								d="M6 14L6.82 16.5L9 17L6.82 17.5L6 20L5.18 17.5L3 17L5.18 16.5L6 14Z"
								fill="currentColor"
								opacity="0.3"
							/>
						</svg>
					</div>
					<div class="text-sm ai-empty-title mb-1">No AI insights available</div>
					<div class="text-xs ai-empty-subtitle">
						Click the refresh button to generate intelligent summary
					</div>
				</div>

				<!-- 流式更新指示器 -->
				<div v-if="loading && currentAISummary" class="ai-streaming-indicator">
					<div class="streaming-dots">
						<div class="dot"></div>
						<div class="dot"></div>
						<div class="dot"></div>
					</div>
					<span class="text-xs text-blue-600 dark:text-blue-400 ml-2">
						{{ loadingText }}
					</span>
				</div>
			</div>
		</el-collapse-transition>
	</div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { ArrowRight, Refresh } from '@element-plus/icons-vue';
import { timeZoneConvert } from '@/hooks/time';

// Props
interface Props {
	showAISummarySection?: boolean;
	loading?: boolean;
	loadingText?: string;
	currentAISummary?: string;
	currentAISummaryGeneratedAt?: string;
}

withDefaults(defineProps<Props>(), {
	showAISummarySection: true,
	loading: false,
	loadingText: 'Generating AI summary...',
	currentAISummary: '',
	currentAISummaryGeneratedAt: '',
});

// Events
const emit = defineEmits<{
	refresh: [];
}>();

// 折叠状态
const isExpanded = ref(false); // 默认展开

// 切换展开状态
const toggleExpanded = () => {
	isExpanded.value = !isExpanded.value;
};

// 处理刷新
const handleRefresh = () => {
	emit('refresh');
};
</script>

<style scoped lang="scss">
/* AI内容样式 */
.ai-content-wrapper {
	position: relative;
	width: 100%;
	max-width: 100%;
	overflow-wrap: break-word;
	word-break: break-word;
	@apply rounded-xl;
}

.ai-streaming {
	background: var(--el-fill-color);
	padding: 8px;
	@apply rounded-xl;
}

.ai-typing-cursor {
	color: var(--el-color-primary);
	font-weight: bold;
	animation: typing-blink 1s infinite;
}

/* 加载状态 */
.ai-loading-state {
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	padding: 2rem;
	text-align: center;
}

.ai-loading-animation {
	margin-bottom: 1rem;
}

.loading-brain {
	width: 48px;
	height: 48px;
	position: relative;

	.brain-wave {
		position: absolute;
		width: 100%;
		height: 4px;
		background: var(--el-color-primary);
		animation: brain-wave-animation 1.5s ease-in-out infinite;
		@apply rounded-xl;

		&:nth-child(1) {
			top: 12px;
			animation-delay: 0s;
		}

		&:nth-child(2) {
			top: 22px;
			animation-delay: 0.3s;
		}

		&:nth-child(3) {
			top: 32px;
			animation-delay: 0.6s;
		}
	}
}

.ai-loading-progress {
	width: 100%;
	max-width: 200px;
	height: 3px;
	background: var(--el-fill-color);
	overflow: hidden;
	margin-top: 1rem;
	@apply rounded-xl;

	.progress-bar {
		height: 100%;
		background: var(--el-color-primary);
		animation: progress-flow 2s ease-in-out infinite;
		@apply rounded-xl;
	}
}

/* 空状态 */
.ai-empty-state {
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	padding: 2rem;
	text-align: center;

	.empty-icon {
		margin-bottom: 1rem;
		color: var(--el-text-color-placeholder);
		opacity: 0.7;
		animation: float 4s ease-in-out infinite;
	}
}

/* 流式指示器 */
.ai-streaming-indicator {
	display: flex;
	align-items: center;
	justify-content: center;
	padding: 8px;
	margin-top: 8px;
	background: var(--el-fill-color);
	@apply rounded-xl;
}

.streaming-dots {
	display: flex;
	gap: 4px;

	.dot {
		width: 4px;
		height: 4px;
		background: var(--el-color-primary);
		border-radius: 50%;
		animation: dot-bounce 1.4s ease-in-out infinite both;

		&:nth-child(1) {
			animation-delay: -0.32s;
		}
		&:nth-child(2) {
			animation-delay: -0.16s;
		}
		&:nth-child(3) {
			animation-delay: 0s;
		}
	}
}

/* 优化折叠动画 */
:deep(.el-collapse-transition) {
	transition: height 0.2s ease-out !important;
}

:deep(.el-collapse-transition .el-collapse-item__content) {
	will-change: height;
	transform: translateZ(0); /* 启用硬件加速 */
	backface-visibility: hidden;
}

/* 自定义文本颜色类 */
.ai-content-text {
	color: var(--el-text-color-primary);
}

.ai-loading-title {
	color: var(--el-text-color-regular);
}

.ai-loading-subtitle {
	color: var(--el-text-color-secondary);
}

.ai-empty-title {
	color: var(--el-text-color-secondary);
}

.ai-empty-subtitle {
	color: var(--el-text-color-placeholder);
}

/* 动画定义 */
@keyframes ai-shimmer {
	0% {
		background-position: -200% 0;
	}
	100% {
		background-position: 200% 0;
	}
}

@keyframes typing-blink {
	0%,
	50% {
		opacity: 1;
	}
	51%,
	100% {
		opacity: 0;
	}
}

@keyframes float {
	0%,
	100% {
		transform: translateY(0px);
	}
	50% {
		transform: translateY(-4px);
	}
}

@keyframes brain-wave-animation {
	0%,
	100% {
		transform: scaleX(0.5);
		opacity: 0.5;
	}
	50% {
		transform: scaleX(1);
		opacity: 1;
	}
}

@keyframes progress-flow {
	0% {
		transform: translateX(-100%);
	}
	100% {
		transform: translateX(200%);
	}
}

@keyframes dot-bounce {
	0%,
	80%,
	100% {
		transform: scale(0);
	}
	40% {
		transform: scale(1);
	}
}

/* AI专用动画 */
@keyframes gradient-shift {
	0% {
		background-position: 0% 50%;
	}
	50% {
		background-position: 100% 50%;
	}
	100% {
		background-position: 0% 50%;
	}
}

@keyframes particles-float {
	0%,
	100% {
		background-position:
			0% 0%,
			0% 0%,
			0% 0%,
			0% 50%;
	}
	25% {
		background-position:
			100% 0%,
			0% 100%,
			50% 50%,
			100% 50%;
	}
	50% {
		background-position:
			100% 100%,
			100% 0%,
			100% 100%,
			0% 50%;
	}
	75% {
		background-position:
			0% 100%,
			100% 100%,
			50% 50%,
			100% 50%;
	}
}

/* 文字溢出处理 */
.word-wrap {
	word-wrap: break-word;
	-webkit-hyphens: auto;
	-moz-hyphens: auto;
	hyphens: auto;
}
</style>
