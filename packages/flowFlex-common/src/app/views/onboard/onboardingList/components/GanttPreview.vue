<template>
	<el-tooltip
		placement="right"
		:show-after="300"
		:hide-after="100"
		popper-class="gantt-preview-popper"
		:disabled="disabled"
		effect="light"
		@show="fetchPreview"
	>
		<!-- 触发按钮 -->
		<el-button link @click.stop="handleClick">
			<template #icon>
				<Icon icon="mynaui:square-chart-gantt" class="w-5 h-5 info-icon mt-0.5" />
			</template>
		</el-button>

		<!-- Tooltip 内容 -->
		<template #content>
			<div class="gantt-card">
				<!-- 加载中 -->
				<div v-if="loadingPreview" class="gantt-card__loading">
					<el-icon class="is-loading"><Loading /></el-icon>
					<span>Loading...</span>
				</div>

				<template v-else-if="summary">
					<!-- 第一行：Case 名称 + Code + 状态 -->
					<div class="gantt-card__header">
						<div class="gantt-card__title-group">
							<span class="gantt-card__title">{{ summary.caseName }}</span>
							<span class="gantt-card__code">({{ summary.caseCode }})</span>
						</div>
						<span class="gantt-card__badge" :class="`gantt-card__badge--${statusKey}`">
							{{ overallStatus }}
						</span>
					</div>

					<!-- 第二行：Workflow -->
					<div class="gantt-card__workflow">Workflow: {{ summary.workflowName }}</div>

					<!-- 第三行：Progress 文字 + 偏差天数 -->
					<div class="gantt-card__progress-header">
						<span class="gantt-card__progress-text">
							Progress:&nbsp;
							<strong>
								{{ summary.completedStages }} of {{ summary.totalStages }}
							</strong>
							&nbsp;Stages ({{ summary.overallCompletionPercentage }}%)
						</span>
						<span
							v-if="varianceDays !== 0"
							class="gantt-card__variance"
							:class="
								varianceDays > 0
									? 'gantt-card__variance--late'
									: 'gantt-card__variance--early'
							"
						>
							{{ varianceDays > 0 ? '+' : '' }}{{ varianceDays }}d
						</span>
					</div>

					<!-- 分段进度条 -->
					<div class="gantt-card__bars">
						<div
							v-for="stage in stages"
							:key="stage.stageId"
							class="gantt-card__bar"
							:class="`gantt-card__bar--${
								stage.isBlocked ? 'blocked' : stage.ganttStatus.toLowerCase()
							}`"
							:title="`${stage.stageName}: ${
								stage.isBlocked ? 'Blocked' : stage.ganttStatus
							}`"
						></div>
					</div>

					<!-- 第四行：Start / ETA -->
					<div class="gantt-card__dates">
						<span>
							Start:
							<strong>{{ formatDate(summary.plannedStartDate) }}</strong>
						</span>
						<span>
							ETA:
							<strong>{{ etaDate }}</strong>
						</span>
					</div>

					<!-- 第五行：Current Stage -->
					<div v-if="currentStage" class="gantt-card__current">
						Current:&nbsp;
						<strong>
							Stage {{ currentStage.stageOrder }} · {{ currentStage.stageName }}
						</strong>
						<span v-if="currentStage.assignee?.length" class="gantt-card__assignee">
							&nbsp;({{ currentStage.assignee[0]?.name }})
						</span>
					</div>

					<!-- View Full Chart 按钮 -->
					<div class="gantt-card__footer">
						<el-button class="gantt-card__action" @click.stop="handleClick">
							<template #icon>
								<Icon icon="mynaui:maximize-one" class="w-5 h-5 info-icon mt-0.5" />
							</template>
							View Full Chart
						</el-button>
					</div>
				</template>
			</div>
		</template>
	</el-tooltip>
</template>

<script setup lang="ts">
import { ref, computed, watch, onUnmounted } from 'vue';
import { Loading } from '@element-plus/icons-vue';
import dayjs from 'dayjs';
import { timeZoneConvert } from '@/hooks/time';
import { projectDate } from '@/settings/projectSetting';
import { getOnboardingGanttData } from '@/apis/ow/gantt';
import { GanttDataResponse, GanttCaseSummary, GanttStageItem } from '#/gantt';

interface Props {
	onboardingId: string | number;
	disabled?: boolean;
}

const props = defineProps<Props>();
const emit = defineEmits<{ click: [] }>();

const data = ref<GanttDataResponse | null>(null);
const loadingPreview = ref(false);
let hasFetched = false;

const summary = computed<GanttCaseSummary | null>(() => data.value?.summary ?? null);
const stages = computed<GanttStageItem[]>(() => data.value?.stages ?? []);

const currentStage = computed(() => {
	return (
		stages.value.find((s) => s.ganttStatus === 'InProgress' || s.ganttStatus === 'Overdue') ??
		stages.value.find((s) => s.ganttStatus === 'Delayed') ??
		stages.value.find((s) => s.isBlocked) ??
		null
	);
});

const overallStatus = computed(() => {
	if (!summary.value) return '';
	const s = summary.value;
	if (s.overdueStages > 0) return 'Overdue';
	if (s.blockedStages > 0) return 'Blocked';
	if (s.delayedStages > 0) return 'Delayed';
	if (s.completedStages === s.totalStages) return 'Completed';
	if (s.overallCompletionPercentage > 0) return 'In Progress';
	return 'Not Started';
});

const statusKey = computed(() => overallStatus.value.toLowerCase().replace(' ', '-'));

const etaDate = computed(() => {
	if (!summary.value) return '—';
	return formatDate(summary.value.projectedEndDate || summary.value.plannedEndDate);
});

const varianceDays = computed(() => {
	if (!summary.value?.projectedEndDate || !summary.value?.plannedEndDate) return 0;
	return dayjs(summary.value.projectedEndDate).diff(dayjs(summary.value.plannedEndDate), 'day');
});

function formatDate(d: string | null | undefined): string {
	if (!d) return '—';
	return timeZoneConvert(d, false, projectDate);
}

function handleClick() {
	emit('click');
}

async function fetchPreview() {
	if (hasFetched || loadingPreview.value) return;
	loadingPreview.value = true;
	try {
		const res = await getOnboardingGanttData(props.onboardingId);
		data.value = res.data ?? (res as any);
		hasFetched = true;
	} catch {
		// silent
	} finally {
		loadingPreview.value = false;
	}
}

watch(
	() => props.onboardingId,
	() => {
		data.value = null;
		hasFetched = false;
	}
);

// 组件销毁时清除缓存，确保下次挂载时重新请求最新数据
onUnmounted(() => {
	data.value = null;
	hasFetched = false;
});
</script>

<style lang="scss">
/* ===== Popper 容器 ===== */
.gantt-preview-popper.el-tooltip__popper {
	padding: 0 !important;
	border: 1px solid var(--el-border-color-light) !important;
	border-radius: var(--el-border-radius-large, 16px) !important;
	box-shadow: var(--el-box-shadow) !important;
	background-color: var(--el-bg-color-overlay) !important;

	.el-popper__arrow {
		display: none;
	}
}

/* ===== 卡片主体 ===== */
.gantt-card {
	width: 420px;
	padding: 16px;
	background-color: var(--el-bg-color-overlay);
	border-radius: var(--el-border-radius-large, 16px);
	color: var(--el-text-color-regular);

	&__loading {
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 8px;
		padding: 24px 0;
		color: var(--el-text-color-secondary);
		font-size: 13px;
	}

	/* 第一行 */
	&__header {
		display: flex;
		align-items: flex-start;
		justify-content: space-between;
		gap: 8px;
		margin-bottom: 4px;
	}

	&__title-group {
		display: flex;
		align-items: baseline;
		gap: 6px;
		min-width: 0;
		flex: 1;
	}

	&__title {
		font-size: 15px;
		font-weight: 700;
		color: var(--el-text-color-primary);
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
		max-width: 220px;
	}

	&__code {
		font-size: 13px;
		color: var(--el-text-color-secondary);
		white-space: nowrap;
	}

	/* 状态徽章 */
	&__badge {
		flex-shrink: 0;
		display: inline-flex;
		align-items: center;
		padding: 2px 10px;
		border-radius: 999px;
		font-size: 12px;
		font-weight: 500;
		border: 1px solid transparent;

		&--overdue {
			color: var(--el-color-danger);
			background-color: var(--el-color-danger-light-7);
			border-color: var(--el-color-danger);
		}
		&--blocked {
			color: var(--el-text-color-secondary);
			background-color: var(--el-fill-color);
			border-color: var(--el-border-color);
		}
		&--delayed {
			color: var(--el-color-warning);
			background-color: var(--el-color-warning-light-7);
			border-color: var(--el-color-warning);
		}
		&--completed {
			color: var(--el-color-success);
			background-color: var(--el-color-success-light-7);
			border-color: var(--el-color-success);
		}
		&--in-progress {
			color: var(--el-color-primary);
			background-color: var(--el-color-primary-light-9);
			border-color: var(--el-color-primary-light-5);
		}
		&--not-started {
			color: var(--el-text-color-secondary);
			background-color: var(--el-fill-color-light);
			border-color: var(--el-border-color-light);
		}
	}

	/* Workflow 行 */
	&__workflow {
		font-size: 13px;
		color: var(--el-text-color-secondary);
		margin-bottom: 14px;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	/* Progress 行 */
	&__progress-header {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-bottom: 8px;
	}

	&__progress-text {
		font-size: 13px;
		color: var(--el-text-color-secondary);

		strong {
			color: var(--el-text-color-primary);
			font-weight: 600;
		}
	}

	&__variance {
		font-size: 13px;
		font-weight: 600;
		flex-shrink: 0;
		margin-left: 8px;

		&--late {
			color: var(--el-color-warning);
		}
		&--early {
			color: var(--el-color-success);
		}
	}

	/* 分段进度条 */
	&__bars {
		display: flex;
		gap: 4px;
		margin-bottom: 14px;
	}

	&__bar {
		flex: 1;
		height: 8px;
		border-radius: 4px;
		background-color: var(--el-fill-color);

		&--completed {
			background-color: var(--el-color-success);
		}
		&--inprogress {
			background-color: var(--el-color-primary);
		}
		&--overdue {
			background-color: var(--el-color-danger);
		}
		&--delayed {
			background-color: var(--el-color-warning);
		}
		&--blocked {
			background-color: var(--el-text-color-secondary);
		}
		&--notstarted {
			background-color: var(--el-fill-color-lighter);
		}
	}

	/* Start / ETA 行 */
	&__dates {
		display: flex;
		align-items: center;
		justify-content: space-between;
		font-size: 13px;
		color: var(--el-text-color-secondary);
		margin-bottom: 8px;

		strong {
			color: var(--el-text-color-regular);
			font-weight: 500;
		}
	}

	/* Current Stage 行 */
	&__current {
		font-size: 13px;
		color: var(--el-text-color-secondary);
		margin-bottom: 12px;

		strong {
			color: var(--el-text-color-primary);
			font-weight: 600;
		}
	}

	&__assignee {
		color: var(--el-text-color-secondary);
	}

	/* 底部区域：左对齐按钮 */
	&__footer {
		display: flex;
		align-items: center;
		padding-top: 4px;
		border-top: 1px solid var(--el-border-color-lighter);
	}

	/* View Full Chart 按钮：inline 自适应宽度 */
	&__action {
		display: inline-flex;
		align-items: center;
		gap: 5px;
		padding: 4px 12px;
		border: 1px solid var(--el-border-color);
		border-radius: var(--el-border-radius-round, 20px);
		background-color: transparent;
		color: var(--el-text-color-secondary);
		font-size: 12px;
		font-weight: 500;
		cursor: pointer;
		transition:
			background-color 0.15s,
			border-color 0.15s,
			color 0.15s;

		&:hover {
			background-color: var(--el-color-primary-light-9);
			border-color: var(--el-color-primary-light-7);
			color: var(--el-color-primary);
		}

		&:active {
			background-color: var(--el-color-primary-light-8);
		}
	}
}
</style>
