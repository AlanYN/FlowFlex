<template>
	<div class="recent-achievements">
		<div class="recent-achievements__header">
			<h3 class="recent-achievements__title">Recent Achievements</h3>
			<span class="recent-achievements__subtitle">Latest case milestones completed</span>
		</div>
		<div class="recent-achievements__content">
			<!-- Loading State -->
			<template v-if="loading">
				<div class="achievement-list">
					<el-skeleton v-for="i in 3" :key="i" animated class="achievement-skeleton">
						<template #template>
							<el-skeleton-item variant="text" style="width: 70%; height: 16px" />
							<el-skeleton-item
								variant="text"
								style="width: 90%; height: 14px; margin-top: 8px"
							/>
							<div class="flex gap-2 mt-2">
								<el-skeleton-item
									variant="circle"
									style="width: 24px; height: 24px"
								/>
								<el-skeleton-item
									variant="circle"
									style="width: 24px; height: 24px"
								/>
							</div>
						</template>
					</el-skeleton>
				</div>
			</template>

			<!-- Empty State -->
			<div v-else-if="achievements.length === 0" class="recent-achievements__empty">
				No achievements yet
			</div>

			<!-- Achievement List -->
			<div v-else class="achievement-list">
				<div
					v-for="achievement in achievements"
					:key="achievement.id"
					class="achievement-item"
				>
					<div class="achievement-item__header">
						<h4 class="achievement-item__title">{{ achievement.title }}</h4>
						<span class="achievement-item__date">
							{{ achievement.completionDateDisplay }}
						</span>
					</div>
					<p class="achievement-item__description">{{ achievement.description }}</p>
					<div class="achievement-item__footer">
						<span class="achievement-item__label">Team:</span>
						<div class="achievement-item__teams">
							<span v-for="team in achievement.teams" :key="team" class="team-badge">
								{{ getTeamInitials(team) }}
							</span>
						</div>
					</div>
				</div>
			</div>
		</div>
		<div class="recent-achievements__footer">
			Showing {{ achievements.length }} achievements
		</div>
	</div>
</template>

<script setup lang="ts">
import type { IAchievement } from '#/dashboard';

interface Props {
	achievements: IAchievement[];
	loading?: boolean;
}

defineProps<Props>();

function getTeamInitials(team: string): string {
	return team
		.split(' ')
		.map((word) => word.charAt(0).toUpperCase())
		.join('')
		.substring(0, 2);
}
</script>

<style scoped lang="scss">
.recent-achievements {
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
		max-height: 320px;
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

.achievement-skeleton {
	@apply p-4 mb-3 rounded-lg;
	background: var(--el-fill-color-light);
}

.achievement-list {
	@apply space-y-4;
}

.achievement-item {
	@apply p-4 rounded-lg border;
	border-color: var(--el-border-color-lighter);
	background: var(--el-fill-color-blank);

	&__header {
		@apply flex justify-between items-start mb-2;
	}

	&__title {
		@apply text-sm font-semibold m-0;
		color: var(--el-text-color-primary);
	}

	&__date {
		@apply text-xs flex-shrink-0;
		color: var(--el-text-color-secondary);
	}

	&__description {
		@apply text-sm m-0 mb-3;
		color: var(--el-text-color-secondary);
	}

	&__footer {
		@apply flex items-center gap-2;
	}

	&__label {
		@apply text-xs;
		color: var(--el-text-color-secondary);
	}

	&__teams {
		@apply flex gap-1;
	}
}

.team-badge {
	@apply w-6 h-6 rounded-full flex items-center justify-center text-xs font-medium;
	background: var(--el-color-primary-light-7);
	color: var(--el-color-primary);
}
</style>
