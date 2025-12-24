<template>
	<div class="employee-stats">
		<div class="employee-stats__header">
			<div class="flex items-center gap-x-2">
				<Icon icon="lucide-user-cog" class="text-primary" />
				<h3 class="employee-stats__title">Employee Overview</h3>
			</div>
			<span class="employee-stats__subtitle">Current workforce statistics</span>
		</div>
		<div class="employee-stats__content">
			<!-- Loading State -->
			<template v-if="loading">
				<el-skeleton animated>
					<template #template>
						<div class="space-y-3">
							<el-skeleton-item
								variant="rect"
								style="width: 100%; height: 80px; border-radius: 8px"
							/>
							<div class="grid grid-cols-2 gap-3">
								<el-skeleton-item
									variant="rect"
									style="width: 100%; height: 70px; border-radius: 8px"
								/>
								<el-skeleton-item
									variant="rect"
									style="width: 100%; height: 70px; border-radius: 8px"
								/>
							</div>
							<el-skeleton-item
								variant="rect"
								style="width: 100%; height: 70px; border-radius: 8px"
							/>
							<el-skeleton-item
								variant="rect"
								style="width: 100%; height: 70px; border-radius: 8px"
							/>
						</div>
					</template>
				</el-skeleton>
			</template>

			<!-- Stats Content -->
			<template v-else>
				<!-- Total Employees -->
				<div class="stat-card stat-card--total">
					<div class="stat-card__icon-wrapper stat-card__icon-wrapper--purple">
						<Icon icon="lucide-users" class="w-6 h-6" />
					</div>
					<div class="stat-card__info">
						<span class="stat-card__label">Total Employees</span>
						<span class="stat-card__value">{{ stats.total }}</span>
					</div>
				</div>

				<!-- Active & On Leave -->
				<div class="stat-grid">
					<div class="stat-card stat-card--active">
						<div class="flex items-center gap-x-2">
							<Icon icon="lucide-circle-check-big" class="text-primary" />
							<span class="stat-card__label">Active</span>
						</div>
						<span class="stat-card__value">{{ stats.active }}</span>
					</div>
					<div class="stat-card stat-card--leave">
						<div class="flex items-center gap-x-2">
							<Icon icon="lucide-clock" class="text-primary" />
							<span class="stat-card__label">On Leave</span>
						</div>
						<span class="stat-card__value">{{ stats.onLeave }}</span>
					</div>
				</div>

				<!-- Avg. Salary -->
				<div class="stat-card stat-card--salary">
					<div class="stat-card__main">
						<div class="flex items-center gap-x-2">
							<Icon class="text-primary" icon="lucide-dollar-sign" />

							<span class="stat-card__label">Avg. Salary</span>
						</div>
						<span class="stat-card__value">${{ formatNumber(stats.avgSalary) }}</span>
					</div>
					<Icon
						:icon="stats.avgSalary > 0 ? 'lucide-trending-up' : 'lucide-trending-down'"
					/>
				</div>

				<!-- Departments -->
				<div class="stat-card stat-card--departments">
					<div class="flex items-center gap-x-2">
						<Icon icon="lucide-briefcase" class="text-primary" />
						<span class="stat-card__label">Departments</span>
					</div>
					<span class="stat-card__value">{{ stats.departmentCount }}</span>
				</div>
			</template>
		</div>
	</div>
</template>

<script setup lang="ts">
import type { IEmployeeStats } from '#/dashboard';

interface Props {
	stats: IEmployeeStats;
	loading?: boolean;
}

defineProps<Props>();

function formatNumber(num: number): string {
	return num.toLocaleString();
}
</script>

<style scoped lang="scss">
.employee-stats {
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
		@apply p-4 space-y-3 flex-1;
	}
}

.stat-grid {
	@apply grid grid-cols-2 gap-3;
}

.stat-card {
	@apply p-4 rounded-lg;
	background: var(--el-fill-color-light);

	&--total {
		@apply flex items-center gap-4;
	}

	&--salary {
		@apply flex items-center justify-between;
	}

	&__icon-wrapper {
		@apply w-12 h-12 rounded-full flex items-center justify-center;

		&--purple {
			background: var(--el-color-primary);
		}
	}

	&__info {
		@apply flex flex-col;
	}

	&__label {
		@apply text-sm;
		color: var(--el-text-color-secondary);
	}

	&__value {
		@apply text-2xl font-bold;
		color: var(--el-text-color-primary);
	}

	&__main {
		@apply flex flex-col;
	}
}
</style>
