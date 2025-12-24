<template>
	<div class="cases-overview">
		<div class="cases-overview__header">
			<h3 class="cases-overview__title">Cases Overview</h3>
			<span class="cases-overview__subtitle">Current case status by stage</span>
		</div>
		<div class="cases-overview__content">
			<!-- Loading State -->
			<template v-if="loading">
				<el-skeleton animated>
					<template #template>
						<div v-for="i in 6" :key="i" class="mb-4">
							<div class="flex justify-between mb-1">
								<el-skeleton-item variant="text" style="width: 40%; height: 14px" />
								<el-skeleton-item
									variant="text"
									style="width: 20px; height: 14px"
								/>
							</div>
							<el-skeleton-item variant="text" style="width: 100%; height: 8px" />
						</div>
					</template>
				</el-skeleton>
			</template>

			<!-- Empty State -->
			<div v-else-if="!data || data.stages.length === 0" class="cases-overview__empty">
				No cases data
			</div>

			<!-- Stage List -->
			<template v-else>
				<div class="stage-list">
					<div v-for="stage in data.stages" :key="stage.stageId" class="stage-item">
						<div class="stage-item__header">
							<span class="stage-item__name">{{ stage.stageName }}</span>
							<span class="stage-item__count">{{ stage.caseCount }}</span>
						</div>
						<div class="stage-item__bar">
							<div
								class="stage-item__fill bg-primary"
								:style="{
									width: `${stage.percentage}%`,
								}"
							></div>
						</div>
					</div>
				</div>
			</template>
		</div>
		<!-- Overall Progress - Fixed at bottom -->
		<div class="cases-overview__footer">
			<template v-if="data && data.stages.length > 0">
				<div class="progress-header">
					<span class="progress-label">Overall Progress</span>
					<span class="progress-value">{{ data.overallProgress }}%</span>
				</div>
				<div class="progress-bar">
					<div
						class="progress-bar__fill"
						:style="{ width: `${data.overallProgress}%` }"
					></div>
				</div>
			</template>
		</div>
	</div>
</template>

<script setup lang="ts">
import type { ICasesOverview } from '#/dashboard';

interface Props {
	data: ICasesOverview | null;
	loading?: boolean;
}

defineProps<Props>();
</script>

<style scoped lang="scss">
.cases-overview {
	@apply rounded-xl overflow-hidden flex flex-col;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
	box-shadow: var(--el-box-shadow-light);
	height: 100%;

	&__header {
		@apply px-4 border-b flex-shrink-0;
		border-color: var(--el-border-color-lighter);
		height: 72px;
		display: flex;
		flex-direction: column;
		justify-content: center;
	}

	&__title {
		@apply text-lg font-semibold m-0;
		color: var(--el-text-color-primary);
	}

	&__subtitle {
		@apply text-sm;
		color: var(--el-text-color-secondary);
	}

	&__content {
		@apply p-4 overflow-y-auto flex-1;
		min-height: 150px;
		max-height: 280px;
	}

	&__empty {
		@apply p-8 text-center;
		color: var(--el-text-color-secondary);
	}

	&__footer {
		@apply px-4 py-3 border-t flex-shrink-0;
		border-color: var(--el-border-color-lighter);
		min-height: 80px;
	}
}

.progress-header {
	@apply flex justify-between items-center mb-2;
}

.progress-label {
	@apply text-sm font-medium;
	color: var(--el-text-color-primary);
}

.progress-value {
	@apply text-lg font-bold;
	color: var(--el-color-primary);
}

.progress-bar {
	@apply h-3 rounded-full overflow-hidden;
	background: var(--el-fill-color);

	&__fill {
		@apply h-full rounded-full transition-all duration-300;
		background: var(--el-color-primary);
	}
}

.stage-list {
	@apply space-y-3;
}

.stage-item {
	&__header {
		@apply flex justify-between items-center mb-1;
	}

	&__name {
		@apply text-sm;
		color: var(--el-text-color-primary);
	}

	&__count {
		@apply text-sm font-medium;
		color: var(--el-text-color-primary);
	}

	&__bar {
		@apply h-2 rounded-full overflow-hidden;
		background: var(--el-fill-color);
	}

	&__fill {
		@apply h-full rounded-full transition-all duration-300;
	}
}
</style>
