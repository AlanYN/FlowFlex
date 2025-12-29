<template>
	<div class="department-distribution">
		<div class="department-distribution__header">
			<div class="flex items-center gap-x-2">
				<Icon icon="lucide-briefcase" />
				<h3 class="department-distribution__title">Department Distribution</h3>
			</div>
			<span class="department-distribution__subtitle">Active employees by department</span>
		</div>
		<div class="department-distribution__content">
			<!-- Loading State -->
			<template v-if="loading">
				<el-skeleton animated>
					<template #template>
						<div v-for="i in 6" :key="i" class="mb-3">
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
			<div v-else-if="departments.length === 0" class="department-distribution__empty">
				No department data
			</div>

			<!-- Department List -->
			<div v-else class="department-list">
				<div v-for="dept in departments" :key="dept.name" class="department-item">
					<div class="department-item__header">
						<span class="department-item__name">{{ dept.name }}</span>
						<span class="department-item__count">{{ dept.count }}</span>
					</div>
					<div class="department-item__bar">
						<div
							class="department-item__fill"
							:style="{ width: `${getPercentage(dept.count)}%` }"
						></div>
					</div>
				</div>
			</div>
		</div>
		<div class="department-distribution__footer">
			Showing top {{ departments.length }} departments
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { IDepartmentDistribution } from '#/dashboard';

interface Props {
	departments: IDepartmentDistribution[];
	loading?: boolean;
}

const props = defineProps<Props>();

const maxCount = computed(() => {
	return Math.max(...props.departments.map((d) => d.count), 1);
});

function getPercentage(count: number): number {
	return (count / maxCount.value) * 100;
}
</script>

<style scoped lang="scss">
.department-distribution {
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
		@apply p-4 flex-1;
	}

	&__empty {
		@apply p-8 text-center;
		color: var(--el-text-color-secondary);
	}

	&__footer {
		@apply px-4 py-3 text-center text-sm border-t flex-shrink-0;
		color: var(--el-text-color-secondary);
		border-color: var(--el-border-color-lighter);
		height: 44px;
		display: flex;
		align-items: center;
		justify-content: center;
	}
}

.department-list {
	@apply space-y-3;
}

.department-item {
	&__header {
		@apply flex justify-between items-center mb-1;
	}

	&__name {
		@apply text-sm;
		color: var(--el-text-color-primary);
	}

	&__count {
		@apply text-sm font-medium px-2 py-0.5 rounded;
		background: var(--el-color-primary-light-9);
	}

	&__bar {
		@apply h-2 rounded-full overflow-hidden;
		background: var(--el-fill-color);
	}

	&__fill {
		@apply h-full rounded-full transition-all duration-300;
		background: var(--el-color-primary);
	}
}
</style>
