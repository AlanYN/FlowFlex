<template>
	<div class="customer-block" v-if="showAISummarySection">
		<!-- 统一的头部卡片 -->
		<div
			class="ai-summary-header-card rounded-md"
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
							class="break-words word-wrap text-sm leading-7 text-gray-800 dark:text-gray-100 overflow-hidden"
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
						<div class="text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
							AI is analyzing your data
						</div>
						<div class="text-xs text-gray-500 dark:text-gray-400">
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
					<div class="text-sm text-gray-500 dark:text-gray-400 mb-1">
						No AI insights available
					</div>
					<div class="text-xs text-gray-400 dark:text-gray-500">
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
const isExpanded = ref(true); // 默认展开

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
/* 头部卡片样式 - 炫酷AI效果 */
.ai-summary-header-card {
	position: relative;
	padding: 12px 16px;
	color: white;
	cursor: pointer;
	transition: all 0.3s ease;
	overflow: hidden;

	/* 主要渐变背景 */
	background: linear-gradient(
		135deg,
		#1e3a8a 0%,
		#3730a3 25%,
		#1e40af 50%,
		#0f172a 75%,
		#1e3a8a 100%
	);
	background-size: 300% 300%;
	animation: gradient-shift 6s ease infinite;

	/* 内容背景遮罩 */
	&::after {
		content: '';
		position: absolute;
		top: 3px;
		left: 3px;
		right: 3px;
		bottom: 3px;
		background: linear-gradient(
			135deg,
			#1e3a8a 0%,
			#3730a3 25%,
			#1e40af 50%,
			#0f172a 75%,
			#1e3a8a 100%
		);
		background-size: 300% 300%;
		animation: gradient-shift 6s ease infinite;
		border-radius: 5px;
		z-index: -1;
	}

	/* AI粒子效果 */
	&::after {
		background-image: radial-gradient(
				circle at 20% 20%,
				rgba(59, 130, 246, 0.3) 1px,
				transparent 1px
			),
			radial-gradient(circle at 80% 80%, rgba(139, 92, 246, 0.3) 1px, transparent 1px),
			radial-gradient(circle at 40% 40%, rgba(16, 185, 129, 0.3) 1px, transparent 1px),
			linear-gradient(135deg, #1e3a8a 0%, #3730a3 25%, #1e40af 50%, #0f172a 75%, #1e3a8a 100%);
		background-size:
			50px 50px,
			30px 30px,
			40px 40px,
			300% 300%;
		animation:
			gradient-shift 6s ease infinite,
			particles-float 8s ease-in-out infinite;
	}

	&:hover {
		transform: translateY(-2px);
	}

	&.expanded {
		border-bottom-left-radius: 0;
		border-bottom-right-radius: 0;

		&::before {
			border-bottom-left-radius: 0;
			border-bottom-right-radius: 0;
		}

		&::after {
			border-bottom-left-radius: 0;
			border-bottom-right-radius: 0;
		}
	}

	/* 确保内容在最上层 */
	> * {
		position: relative;
		z-index: 1;
	}
}

.ai-summary-title {
	font-size: 14px;
	font-weight: 600;
	margin: 0;
	color: white;
}

.ai-summary-subtitle {
	font-size: 12px;
	margin: 0;
	color: rgba(255, 255, 255, 0.9);
	font-weight: 400;
}

.ai-actions {
	display: flex;
	align-items: center;
}

.expand-icon {
	transition: transform 0.2s ease;
	color: white;

	&.rotated {
		transform: rotate(90deg);
	}
}

/* 状态徽章 - AI主题 */
.ai-status-badge {
	display: flex;
	align-items: center;
	gap: 4px;
	padding: 3px 8px;
	border-radius: 12px;
	font-weight: 500;
	font-size: 10px;
	text-transform: uppercase;
	letter-spacing: 0.3px;
	background: rgba(255, 255, 255, 0.15);
	backdrop-filter: blur(10px);
	border: 1px solid rgba(255, 255, 255, 0.2);
	box-shadow: 0 1px 4px rgba(0, 0, 0, 0.1);

	.status-dot {
		width: 6px;
		height: 6px;
		border-radius: 50%;
		margin-right: 2px;
		box-shadow: 0 0 6px currentColor;
	}

	&.generating {
		background: linear-gradient(135deg, rgba(245, 158, 11, 0.2), rgba(251, 191, 36, 0.3));
		border-color: rgba(245, 158, 11, 0.4);
		color: #fbbf24;

		.status-dot {
			background: #fbbf24;
			animation:
				status-pulse 1s ease-in-out infinite,
				glow-pulse 2s ease-in-out infinite;
		}
	}

	&.ready {
		background: linear-gradient(135deg, rgba(16, 185, 129, 0.2), rgba(52, 211, 153, 0.3));
		border-color: rgba(16, 185, 129, 0.4);
		color: #10b981;

		.status-dot {
			background: #10b981;
			animation: status-pulse 2s ease-in-out infinite;
		}
	}

	&.idle {
		background: linear-gradient(135deg, rgba(156, 163, 175, 0.2), rgba(209, 213, 219, 0.3));
		border-color: rgba(156, 163, 175, 0.4);
		color: #9ca3af;

		.status-dot {
			background: #9ca3af;
			animation: status-pulse 3s ease-in-out infinite;
		}
	}
}

/* 刷新按钮 - AI主题 */
.ai-refresh-btn {
	background: linear-gradient(135deg, rgba(255, 255, 255, 0.2), rgba(255, 255, 255, 0.1));
	border: 1px solid rgba(255, 255, 255, 0.3);
	color: white;
	transition: all 0.3s ease;
	backdrop-filter: blur(10px);
	box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);

	&:hover {
		background: linear-gradient(135deg, rgba(255, 255, 255, 0.3), rgba(255, 255, 255, 0.2));
		border-color: rgba(255, 255, 255, 0.5);
		transform: translateY(-1px);
		box-shadow:
			0 4px 12px rgba(0, 0, 0, 0.2),
			0 0 15px rgba(255, 255, 255, 0.25);
	}

	&:active {
		transform: translateY(0) scale(0.98);
	}
}

/* AI内容样式 */
.ai-content-wrapper {
	position: relative;
	border-radius: 8px;
	width: 100%;
	max-width: 100%;
	overflow-wrap: break-word;
	word-break: break-word;
}

.ai-streaming {
	background: linear-gradient(
		90deg,
		transparent 0%,
		rgba(59, 130, 246, 0.08) 50%,
		transparent 100%
	);
	background-size: 200% 100%;
	animation: ai-shimmer 2s infinite;
	border-radius: 6px;
	padding: 8px;
}

.ai-typing-cursor {
	color: #3b82f6;
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
		background: linear-gradient(90deg, #3b82f6 0%, #8b5cf6 50%, #6366f1 100%);
		border-radius: 2px;
		animation: brain-wave-animation 1.5s ease-in-out infinite;

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
	background: rgba(59, 130, 246, 0.1);
	border-radius: 2px;
	overflow: hidden;
	margin-top: 1rem;

	.progress-bar {
		height: 100%;
		background: linear-gradient(90deg, #3b82f6 0%, #8b5cf6 100%);
		border-radius: 2px;
		animation: progress-flow 2s ease-in-out infinite;
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
		color: #9ca3af;
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
	background: rgba(59, 130, 246, 0.05);
	border-radius: 6px;
}

.streaming-dots {
	display: flex;
	gap: 4px;

	.dot {
		width: 4px;
		height: 4px;
		background: #3b82f6;
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

/* 暗色主题样式 */
.dark {
	.ai-summary-header-card {
		/* 暗色主题下增强的渐变背景 */
		background: linear-gradient(
			135deg,
			#0c1427 0%,
			#1e3a8a 20%,
			#3730a3 40%,
			#1e40af 60%,
			#0f172a 80%,
			#0c1427 100%
		);
		background-size: 300% 300%;
		animation: gradient-shift 6s ease infinite;

		&::after {
			background: linear-gradient(
				135deg,
				#0c1427 0%,
				#1e3a8a 20%,
				#3730a3 40%,
				#1e40af 60%,
				#0f172a 80%,
				#0c1427 100%
			);

			/* 增强粒子效果可见性 */
			background-image: radial-gradient(
					circle at 20% 20%,
					rgba(96, 165, 250, 0.6) 1.5px,
					transparent 1.5px
				),
				radial-gradient(
					circle at 80% 80%,
					rgba(167, 139, 250, 0.6) 1.5px,
					transparent 1.5px
				),
				radial-gradient(circle at 40% 40%, rgba(52, 211, 153, 0.6) 1.5px, transparent 1.5px),
				radial-gradient(circle at 60% 20%, rgba(251, 191, 36, 0.5) 1px, transparent 1px),
				radial-gradient(circle at 30% 70%, rgba(248, 113, 113, 0.5) 1px, transparent 1px),
				linear-gradient(
					135deg,
					#0c1427 0%,
					#1e3a8a 20%,
					#3730a3 40%,
					#1e40af 60%,
					#0f172a 80%,
					#0c1427 100%
				);
			background-size:
				60px 60px,
				40px 40px,
				50px 50px,
				35px 35px,
				45px 45px,
				300% 300%;
			animation:
				gradient-shift 6s ease infinite,
				particles-float 10s ease-in-out infinite;
		}
	}
}

/* 动画定义 */
@keyframes status-pulse {
	0%,
	100% {
		opacity: 1;
		transform: scale(1);
	}
	50% {
		opacity: 0.7;
		transform: scale(1.1);
	}
}

@keyframes glow-pulse {
	0%,
	100% {
		box-shadow: 0 0 8px currentColor;
	}
	50% {
		box-shadow:
			0 0 16px currentColor,
			0 0 24px currentColor;
	}
}

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

/* 响应式设计 */
@media (max-width: 768px) {
	.ai-summary-header-card {
		padding: 8px 12px;
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
