<template>
	<div class="upcoming-deadlines">
		<div class="upcoming-deadlines__header">
			<h3 class="upcoming-deadlines__title">Upcoming Deadlines</h3>
			<span class="upcoming-deadlines__subtitle">Tasks and milestones due soon</span>
		</div>
		<div class="upcoming-deadlines__content">
			<!-- Loading State -->
			<template v-if="loading">
				<div class="deadline-grid">
					<el-skeleton v-for="i in 3" :key="i" animated class="deadline-skeleton">
						<template #template>
							<el-skeleton-item variant="text" style="width: 80%; height: 16px" />
							<el-skeleton-item
								variant="text"
								style="width: 50%; height: 14px; margin-top: 8px"
							/>
						</template>
					</el-skeleton>
				</div>
			</template>

			<!-- Empty State -->
			<div v-else-if="deadlines.length === 0" class="upcoming-deadlines__empty">
				No upcoming deadlines
			</div>

			<!-- Deadline Cards -->
			<div v-else class="deadline-grid">
				<div
					v-for="deadline in deadlines"
					:key="deadline.id"
					class="deadline-card"
					:class="getUrgencyClass(deadline.urgency)"
				>
					<div class="deadline-card__title">{{ deadline.name }}</div>
					<div class="deadline-card__case">{{ deadline.caseName }}</div>
					<div class="deadline-card__due">{{ deadline.dueDateDisplay }}</div>
					<div class="deadline-card__meta">
						<span class="deadline-card__type">{{ deadline.type }}</span>
						<span
							class="deadline-card__priority"
							:class="getPriorityClass(deadline.priority)"
						>
							{{ deadline.priority }}
						</span>
					</div>
				</div>
			</div>
		</div>
		<div class="upcoming-deadlines__footer">Showing {{ deadlines.length }} deadlines</div>
	</div>
</template>

<script setup lang="ts">
import type { IDeadline, DeadlineUrgency } from '#/dashboard';

interface Props {
	deadlines: IDeadline[];
	loading?: boolean;
}

defineProps<Props>();

function getUrgencyClass(urgency: DeadlineUrgency): string {
	const classMap: Record<DeadlineUrgency, string> = {
		overdue: 'deadline-card--overdue',
		today: 'deadline-card--today',
		tomorrow: 'deadline-card--tomorrow',
		thisWeek: 'deadline-card--week',
		upcoming: 'deadline-card--upcoming',
	};
	return classMap[urgency] || '';
}

function getPriorityClass(priority: string): string {
	const classMap: Record<string, string> = {
		Critical: 'priority--critical',
		High: 'priority--high',
		Medium: 'priority--medium',
		Low: 'priority--low',
	};
	return classMap[priority] || '';
}
</script>

<style scoped lang="scss">
.upcoming-deadlines {
	@apply rounded-xl overflow-hidden flex flex-col;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
	box-shadow: var(--el-box-shadow-light);

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

.deadline-skeleton {
	@apply p-4 rounded-lg;
	background: var(--el-fill-color-light);
}

.deadline-grid {
	@apply grid gap-4;
	grid-template-columns: repeat(3, 1fr);

	@media (max-width: 1023px) {
		grid-template-columns: repeat(2, 1fr);
	}

	@media (max-width: 639px) {
		grid-template-columns: 1fr;
	}
}

.deadline-card {
	@apply p-4 rounded-lg border-l-4;
	background: var(--el-fill-color-blank);
	border: 1px solid var(--el-border-color-lighter);
	border-left-width: 4px;

	&--overdue {
		border-left-color: var(--el-color-danger);
	}

	&--today {
		border-left-color: var(--el-color-warning);
	}

	&--tomorrow {
		border-left-color: var(--el-color-primary);
	}

	&--week {
		border-left-color: var(--el-color-success);
	}

	&--upcoming {
		border-left-color: var(--el-text-color-secondary);
	}

	&__title {
		@apply text-sm font-semibold mb-1;
		color: var(--el-text-color-primary);
	}

	&__case {
		@apply text-xs mb-2;
		color: var(--el-text-color-secondary);
	}

	&__due {
		@apply text-sm font-medium mb-2;
		color: var(--el-color-primary);
	}

	&__meta {
		@apply flex items-center gap-2;
	}

	&__type {
		@apply text-xs px-2 py-0.5 rounded;
		background: var(--el-fill-color);
		color: var(--el-text-color-secondary);
	}

	&__priority {
		@apply text-xs px-2 py-0.5 rounded font-medium;
	}
}

.priority {
	&--critical {
		background: var(--el-color-danger-light-9);
		color: var(--el-color-danger);
	}

	&--high {
		background: var(--el-color-warning-light-9);
		color: var(--el-color-warning-dark-2);
	}

	&--medium {
		background: var(--el-color-primary-light-9);
		color: var(--el-color-primary);
	}

	&--low {
		background: var(--el-fill-color);
		color: var(--el-text-color-secondary);
	}
}
</style>
