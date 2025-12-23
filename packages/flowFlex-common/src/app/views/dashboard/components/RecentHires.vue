<template>
	<div class="recent-hires">
		<div class="recent-hires__header">
			<div class="flex items-center gap-x-2">
				<Icon icon="lucide-users" class="text-primary" />
				<h3 class="recent-hires__title">Recent Hires</h3>
			</div>
			<span class="recent-hires__subtitle">New employees (last 90 days)</span>
		</div>
		<div class="recent-hires__content">
			<!-- Loading State -->
			<template v-if="loading">
				<el-skeleton animated>
					<template #template>
						<div v-for="i in 3" :key="i" class="flex items-center gap-3 mb-3">
							<el-skeleton-item variant="circle" style="width: 40px; height: 40px" />
							<div class="flex-1">
								<el-skeleton-item variant="text" style="width: 60%; height: 14px" />
								<el-skeleton-item
									variant="text"
									style="width: 40%; height: 12px; margin-top: 4px"
								/>
							</div>
						</div>
					</template>
				</el-skeleton>
			</template>

			<!-- Empty State -->
			<div v-else-if="recentHires.length === 0" class="recent-hires__empty">
				<el-icon class="recent-hires__empty-icon"><UserFilled /></el-icon>
				<span>{{ recentHires.length }} new hires</span>
			</div>

			<!-- Hire List -->
			<div v-else class="hire-list">
				<div v-for="hire in recentHires" :key="hire.id" class="hire-item">
					<div class="hire-item__avatar">
						{{ getInitials(hire.name) }}
					</div>
					<div class="hire-item__info">
						<span class="hire-item__name">{{ hire.name }}</span>
						<span class="hire-item__department">{{ hire.department }}</span>
					</div>
					<span class="hire-item__date">{{ formatDate(hire.hireDate) }}</span>
				</div>
			</div>
		</div>
		<div class="recent-hires__footer">
			<el-button type="primary" class="w-full">Add Employee</el-button>
		</div>
	</div>
</template>

<script setup lang="ts">
import { UserFilled } from '@element-plus/icons-vue';
import type { IRecentHire } from '#/dashboard';
import dayjs from 'dayjs';

interface Props {
	recentHires: IRecentHire[];
	loading?: boolean;
}

defineProps<Props>();

function getInitials(name: string): string {
	return name
		.split(' ')
		.map((n) => n.charAt(0).toUpperCase())
		.join('')
		.substring(0, 2);
}

function formatDate(date: string): string {
	return dayjs(date).format('MMM D');
}
</script>

<style scoped lang="scss">
.recent-hires {
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
		@apply flex flex-col items-center justify-center h-full py-8;
		color: var(--el-text-color-secondary);
	}

	&__empty-icon {
		@apply text-4xl mb-2;
		color: var(--el-text-color-placeholder);
	}

	&__footer {
		@apply px-4 py-2 border-t flex-shrink-0;
		border-color: var(--el-border-color-lighter);
		height: 44px;
		display: flex;
		align-items: center;
		justify-content: center;
	}
}

.hire-list {
	@apply space-y-3;
}

.hire-item {
	@apply flex items-center gap-3;

	&__avatar {
		@apply w-10 h-10 rounded-full flex items-center justify-center text-sm font-medium flex-shrink-0;
		background: var(--el-color-primary-light-7);
		color: var(--el-color-primary);
	}

	&__info {
		@apply flex-1 min-w-0;
	}

	&__name {
		@apply block text-sm font-medium truncate;
		color: var(--el-text-color-primary);
	}

	&__department {
		@apply block text-xs;
		color: var(--el-text-color-secondary);
	}

	&__date {
		@apply text-xs flex-shrink-0;
		color: var(--el-text-color-secondary);
	}
}
</style>
