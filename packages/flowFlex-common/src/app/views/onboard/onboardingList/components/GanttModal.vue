<template>
	<el-dialog
		v-model="visible"
		:width="dialogWidth"
		class="gantt-modal"
		:top="'5vh'"
		destroy-on-close
	>
		<template #header>
			<div class="flex items-center justify-between w-full pr-8">
				<div class="flex items-center gap-3">
					<span class="font-semibold text-base">Gantt Chart</span>
				</div>
				<!-- 自定义问号按钮：在弹窗 header 右侧，关闭按钮左边 -->
				<el-tooltip
					v-if="tourReady"
					content="View guided tour"
					placement="bottom"
					:show-after="300"
				>
					<button class="gantt-tour-fab" @click="handleTourFabClick">
						<span class="gantt-tour-fab__icon">?</span>
					</button>
				</el-tooltip>
			</div>
		</template>

		<!-- 加载中占位 -->
		<div v-if="loading" class="flex items-center justify-center py-20">
			<el-icon class="is-loading text-4xl text-primary-500 mr-3"><Loading /></el-icon>
			<span class="text-gray-500">Loading gantt data...</span>
		</div>

		<!-- 错误提示 -->
		<el-result
			v-else-if="error"
			icon="error"
			title="Failed to load gantt data"
			:sub-title="error"
		>
			<template #extra>
				<el-button type="primary" @click="fetchData">Retry</el-button>
			</template>
		</el-result>

		<!-- 甘特图主体 -->
		<div v-else class="gantt-modal-content" :style="{ maxHeight: '75vh', overflowY: 'auto' }">
			<GanttChart
				ref="ganttChartRef"
				:stages="stages"
				:summary="summary"
				:loading="loading"
				:onboarding-id="currentOnboardingId"
				@close="visible = false"
			/>
		</div>

		<template #footer>
			<div class="flex justify-end">
				<el-button @click="visible = false">Close</el-button>
			</div>
		</template>
	</el-dialog>

	<!-- Tour 引导：v-if 与 visible 绑定，弹窗关闭时组件销毁，driver.js 实例随之清除 -->
	<!-- show-fab="false" 禁用内置悬浮按钮，改用弹窗 header 内的自定义按钮触发 -->
	<TourGuide
		v-if="visible && tourReady"
		ref="tourGuideRef"
		:persist-key="tourPersistKey"
		:steps="tourSteps"
		:auto-start="true"
		:show-fab="false"
		:check-seen-remote="checkTourSeenRemote"
		:mark-seen-remote="markTourSeenRemote"
	/>
</template>

<script setup lang="ts">
import { ref, computed, nextTick } from 'vue';
import { Loading } from '@element-plus/icons-vue';
import GanttChart from './GanttChart.vue';
import TourGuide from '@/components/global/TourGuide/index.vue';
import { ganttModalTourSteps } from '@/hooks/useGanttTourSteps';
import { checkTourSeen, markTourSeen } from '@/apis/ow';
import {
	getOnboardingGanttData,
	type GanttStageItem,
	type GanttCaseSummary,
} from '@/apis/ow/gantt';

// ========================= 状态 =========================

const visible = ref(false);
const currentOnboardingId = ref<string | number>('');

const stages = ref<GanttStageItem[]>([]);
const summary = ref<GanttCaseSummary | null>(null);
const loading = ref(false);
const error = ref<string | null>(null);

/** Tour 只在数据加载完成后才挂载，确保锚点元素已渲染 */
const tourReady = ref(false);

const tourGuideRef = ref<InstanceType<typeof TourGuide> | null>(null);
/** GanttChart 实例 ref，用于调用 scrollToToday */
const ganttChartRef = ref<InstanceType<typeof GanttChart> | null>(null);

const dialogWidth = computed(() => {
	if (typeof window !== 'undefined') {
		return Math.min(window.innerWidth * 0.9, 1400) + 'px';
	}
	return '90%';
});

// ========================= Tour 配置 =========================

const tourPersistKey = 'gantt-modal-tour';

/**
 * 动态构建 tour steps：Step 4 注入 beforeHighlight 回调，
 * 先滚动到 Today 线，确保 .g-grid-current-time 在视口内
 */
const tourSteps = computed(() => {
	return ganttModalTourSteps.map((step) => {
		if (step.element === '.g-grid-current-time') {
			return {
				...step,
				beforeHighlight: async () => {
					// 先滚动到 Today 线位置，再让 driver.js 定位
					ganttChartRef.value?.scrollToToday();
					// 给滚动动画留出时间
					await new Promise((resolve) => setTimeout(resolve, 150));
				},
			};
		}
		return step;
	});
});

const checkTourSeenRemote = async (): Promise<boolean> => {
	try {
		const res = await checkTourSeen(tourPersistKey);
		return res?.data === true;
	} catch {
		return false;
	}
};

const markTourSeenRemote = async (): Promise<void> => {
	try {
		await markTourSeen(tourPersistKey);
	} catch {
		// best-effort
	}
};

/** 点击 header 问号按钮：强制重播 tour */
const handleTourFabClick = () => {
	tourGuideRef.value?.replayTour();
};

// ========================= 方法 =========================

async function fetchData() {
	if (!currentOnboardingId.value) return;
	loading.value = true;
	tourReady.value = false;
	error.value = null;
	stages.value = [];
	summary.value = null;
	try {
		const result = await getOnboardingGanttData(currentOnboardingId.value);
		stages.value = result.stages;
		summary.value = result.summary;
		// 等待 Vue 渲染完成后再挂载 Tour，确保 data-tour 锚点已在 DOM 中
		await nextTick();
		tourReady.value = true;
	} catch (e: any) {
		error.value = e?.message || 'Unknown error';
	} finally {
		loading.value = false;
	}
}

/** 外部通过 ref 调用此方法打开弹窗 */
const open = (onboardingId: string | number) => {
	currentOnboardingId.value = onboardingId;
	visible.value = true;
	fetchData();
};

defineExpose({ open });
</script>

<style lang="scss">
.gantt-modal {
	.el-dialog__body {
		padding: 0 20px 0 20px;
	}
	.el-dialog__header {
		padding: 16px 20px 12px;
		border-bottom: 1px solid var(--el-border-color-lighter);
		margin-right: 0;
	}
	.el-dialog__footer {
		padding: 12px 20px;
		border-top: 1px solid var(--el-border-color-lighter);
	}
}

/* 弹窗 header 内的问号按钮 */
.gantt-tour-fab {
	display: flex;
	align-items: center;
	justify-content: center;
	width: 28px;
	height: 28px;
	border-radius: 50%;
	border: none;
	cursor: pointer;
	background: var(--el-color-primary);
	color: #fff;
	box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
	transition:
		transform 0.2s ease,
		background 0.2s ease;
	flex-shrink: 0;

	&:hover {
		transform: scale(1.1);
		background: var(--el-color-primary-dark-2);
	}

	&:active {
		transform: scale(0.95);
	}

	&:focus-visible {
		outline: 2px solid var(--el-color-primary);
		outline-offset: 3px;
	}

	&__icon {
		font-size: 14px;
		font-weight: 700;
		line-height: 1;
		user-select: none;
	}
}
</style>
