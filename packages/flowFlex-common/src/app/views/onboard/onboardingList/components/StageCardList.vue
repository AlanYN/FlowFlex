<template>
	<div class="space-y-4">
		<!-- 骨架屏 -->
		<template v-if="loading">
			<el-skeleton
				v-for="i in 3"
				:key="i"
				animated
				:loading="loading"
				class="stage-card-skeleton"
			>
				<template #template>
					<el-card class="stage-card shadow-sm rounded-md">
						<template #header>
							<div class="flex justify-between items-center py-1">
								<div class="flex items-center">
									<el-skeleton-item
										variant="circle"
										style="width: 16px; height: 16px; margin-right: 8px"
									/>
									<el-skeleton-item variant="text" style="width: 120px" />
								</div>
								<el-skeleton-item
									variant="button"
									style="width: 60px; height: 24px"
								/>
							</div>
						</template>
						<div class="stage-content p-3">
							<div class="flex flex-wrap gap-2">
								<el-skeleton-item
									v-for="j in 4"
									:key="j"
									variant="button"
									style="width: 140px; height: 32px; margin: 2px"
								/>
							</div>
						</div>
						<template #footer>
							<div class="bg-gray-50 py-2 px-3 border-t -mx-6 -mb-6">
								<div class="flex justify-between items-center text-xs">
									<el-skeleton-item variant="text" style="width: 50px" />
									<el-skeleton-item variant="text" style="width: 60px" />
									<el-skeleton-item variant="text" style="width: 40px" />
									<el-skeleton-item variant="text" style="width: 60px" />
								</div>
							</div>
						</template>
					</el-card>
				</template>
			</el-skeleton>
		</template>

		<!-- 实际内容 -->
		<template v-else>
			<el-card
				v-for="stage in activeStages"
				:key="stage"
				class="stage-card shadow-sm rounded-md"
			>
				<template #header>
					<div class="flex justify-between items-center cursor-pointer py-1">
						<div class="flex items-center">
							<el-icon
								class="mr-2 transition-transform text-gray-600"
								:class="{ 'rotate-90': activeStages.includes(stage) }"
							>
								<ArrowRight />
							</el-icon>
							<span class="text-base font-medium text-gray-900">
								{{ stage }}
							</span>
						</div>
						<el-tag type="info" class="flex items-center">
							<el-icon class="mr-1"><User /></el-icon>
							{{ groupedLeads[stage]?.length || 0 }}
						</el-tag>
					</div>
				</template>

				<div v-if="activeStages.includes(stage)" class="stage-content p-3">
					<div v-if="groupedLeads[stage]?.length" class="flex flex-wrap gap-2">
						<el-tooltip
							v-for="lead in groupedLeads[stage]"
							:key="lead.id"
							placement="top"
							:show-after="500"
							effect="light"
						>
							<template #content>
								<div class="p-3 max-w-xs">
									<div class="font-medium mb-1 text-gray-900">
										{{ lead.leadName }}
									</div>
									<div class="text-xs text-gray-500 mb-1">
										{{ lead.leadId }}
									</div>
									<div class="grid grid-cols-1 gap-2 text-xs mb-2 text-gray-600">
										<div class="flex items-center">
											<el-icon class="mr-1 text-gray-500">
												<Calendar />
											</el-icon>
											<span>
												Start:
												{{
													timeZoneConvert(
														lead.currentStageStartTime,
														false,
														projectTenMinutesSsecondsDate
													)
												}}
											</span>
										</div>
										<div class="flex items-center">
											<el-icon class="mr-1 text-gray-500">
												<Calendar />
											</el-icon>
											<span>
												End:
												{{
													timeZoneConvert(
														lead?.currentStageEndTime,
														false,
														projectTenMinutesSsecondsDate
													)
												}}
											</span>
										</div>
									</div>
									<div class="flex items-center justify-between text-xs">
										<el-tag
											:type="getPriorityTagType(lead.priority)"
											size="small"
											class="text-white"
										>
											{{ lead.priority }}
										</el-tag>
										<span
											v-if="isOverdue(lead.estimatedCompletionDate)"
											class="text-red-500 flex items-center"
										>
											<el-icon class="mr-1"><Warning /></el-icon>
											Overdue
										</span>
									</div>
								</div>
							</template>
							<el-button
								size="small"
								class="pipeline-lead-button rounded-md"
								:class="[
									getPriorityBorderClass(lead.priority),
									isOverdue(lead.estimatedCompletionDate) ? 'border-red-500' : '',
								]"
								@click="handleEdit(lead.id)"
							>
								<span class="truncate max-w-[180px]">
									{{ lead.leadName }}
								</span>
								<div class="flex items-center ml-1">
									<el-icon
										v-if="isOverdue(lead.estimatedCompletionDate)"
										class="text-red-500 mr-1"
									>
										<Warning />
									</el-icon>
								</div>
							</el-button>
						</el-tooltip>
					</div>
					<div v-else class="text-center py-4 text-gray-500 text-sm">
						No customers in this stage
					</div>
				</div>

				<template #footer>
					<div class="bg-gray-50 py-2 px-3 border-t -mx-6 -mb-6">
						<div class="flex justify-between items-center text-xs text-gray-500">
							<span>High: {{ getStageCountByPriority(stage, 'High') }}</span>
							<span>Medium: {{ getStageCountByPriority(stage, 'Medium') }}</span>
							<span>Low: {{ getStageCountByPriority(stage, 'Low') }}</span>
							<span>Overdue: {{ getStageOverdueCount(stage) }}</span>
						</div>
					</div>
				</template>
			</el-card>
		</template>

		<div
			v-if="!loading && activeStages.length === 0"
			class="text-center py-10 bg-gray-50 rounded-lg"
		>
			<p class="text-gray-500">
				No stages selected. Please select at least one stage to view.
			</p>
		</div>
	</div>
</template>

<script setup lang="ts">
import { PropType } from 'vue';
import { ArrowRight, User, Calendar, Warning } from '@element-plus/icons-vue';
import { OnboardingItem } from '#/onboard';
import { projectTenMinutesSsecondsDate } from '@/settings/projectSetting';
import { timeZoneConvert } from '@/hooks/time';

defineProps({
	loading: {
		type: Boolean,
		default: false,
	},
	activeStages: {
		type: Array as PropType<string[]>,
		required: true,
	},
	groupedLeads: {
		type: Object as PropType<Record<string, OnboardingItem[]>>,
		required: true,
	},
	formatDate: {
		type: Function,
		required: true,
	},
	isOverdue: {
		type: Function,
		required: true,
	},
	getPriorityTagType: {
		type: Function,
		required: true,
	},
	getPriorityBorderClass: {
		type: Function,
		required: true,
	},
	getStageCountByPriority: {
		type: Function,
		required: true,
	},
	getStageOverdueCount: {
		type: Function,
		required: true,
	},
	handleEdit: {
		type: Function,
		required: true,
	},
});
</script>

<style scoped lang="scss">
/* 管道视图样式 */
.stage-card {
	border: 1px solid #e5e7eb;
	overflow: hidden;
}

.stage-card :deep(.el-card__header) {
	background-color: var(--primary-10);
	border-bottom: 1px solid #e5e7eb;
	padding: 12px 20px;
}

.stage-card :deep(.el-card__body) {
	padding: 0;
}

.stage-content {
	padding: 16px;
}

:deep(.pipeline-lead-button) {
	border: 1px solid #d1d5db;
	border-left-width: 4px;
	background: white;
	color: #374151;
	font-size: 14px;
	padding: 8px 12px;
	margin: 2px;
	transition: all 0.2s;
	height: 32px;
	text-align: left;
	justify-content: flex-start;
	font-weight: normal;
}

:deep(.pipeline-lead-button:hover) {
	background-color: #f9fafb;
}

:deep(.pipeline-lead-button.border-red-500) {
	border-color: #dc2626 !important;
	border-left-color: #dc2626 !important;
}

:deep(.pipeline-lead-button.border-yellow-500) {
	border-color: #d97706 !important;
	border-left-color: #d97706 !important;
}

:deep(.pipeline-lead-button.border-green-500) {
	border-color: #059669 !important;
	border-left-color: #059669 !important;
}

:deep(.pipeline-lead-button.border-gray-500) {
	border-color: #6b7280 !important;
	border-left-color: #6b7280 !important;
}

/* 逾期时的红色边框覆盖优先级颜色 */
:deep(
		.pipeline-lead-button.border-red-500:not(.border-yellow-500):not(.border-green-500):not(
				.border-gray-500
			)
	) {
	border-color: #dc2626 !important;
	border-left-color: #dc2626 !important;
}

/* 骨架屏样式 */
.stage-card-skeleton {
	.stage-card {
		border: 1px solid #e5e7eb;
		overflow: hidden;
	}

	.stage-card :deep(.el-card__header) {
		background-color: var(--primary-10);
		border-bottom: 1px solid #e5e7eb;
		padding: 12px 20px;
	}

	.stage-card :deep(.el-card__body) {
		padding: 0;
	}

	.stage-content {
		padding: 16px;
	}
}

/* 旋转动画 */
.rotate-90 {
	transform: rotate(90deg);
}

/* 暗色主题样式 */
html.dark {
	/* 管道视图暗色主题 */
	.stage-card {
		border: 1px solid #4a5568 !important;
		background-color: var(--black-400) !important;
	}

	.stage-card :deep(.el-card__header) {
		background-color: #003c76 !important;
		border-bottom: 1px solid #00509d !important;
		color: #cce8d0 !important;
	}

	.stage-card :deep(.el-card__body) {
		background-color: var(--black-400) !important;
	}

	.stage-content {
		background-color: var(--black-400) !important;
	}

	/* 客户按钮暗色主题 */
	:deep(.pipeline-lead-button) {
		background-color: var(--black-200) !important;
		border: 1px solid var(--black-200) !important;
		border-left-width: 4px !important;
		color: var(--white-100) !important;
	}

	:deep(.pipeline-lead-button:hover) {
		background-color: var(--black-300) !important;
	}

	/* 卡片底部统计区域暗色主题 */
	.stage-card :deep(.el-card__footer) {
		background-color: var(--black-200) !important;
		border-top: 1px solid #4a5568 !important;
		color: #9ca3af !important;
	}
}
</style>
