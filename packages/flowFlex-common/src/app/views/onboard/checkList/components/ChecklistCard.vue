<template>
	<el-card class="checklist-card overflow-hidden transition-all">
		<!-- 卡片头部 -->
		<template #header>
			<div class="card-header -m-5 p-4">
				<div class="flex items-center justify-between w-full">
					<div class="flex items-center space-x-3 flex-1 min-w-0">
						<div
							class="card-icon rounded-full flex-shrink-0 flex items-center justify-center"
						>
							<Icon icon="material-symbols:checklist-rounded" class="w-6 h-6" />
						</div>
						<h3
							class="card-title text-xl font-semibold leading-tight tracking-tight truncate"
							:title="checklist.name"
						>
							{{ checklist.name }}
						</h3>
					</div>
					<div class="flex items-center space-x-2">
						<el-dropdown
							trigger="click"
							@command="(cmd) => $emit('command', cmd, checklist)"
							class="flex-shrink-0"
						>
							<el-button text class="card-more-btn" link>
								<el-icon class="h-4 w-4"><MoreFilled /></el-icon>
							</el-button>
							<template #dropdown>
								<el-dropdown-menu>
									<el-dropdown-item command="edit">
										<el-icon class="mr-2"><Edit /></el-icon>
										Edit
									</el-dropdown-item>
									<el-dropdown-item command="task">
										<el-icon class="mr-2"><Edit /></el-icon>
										View Tasks
									</el-dropdown-item>
									<el-dropdown-item command="export" :disabled="exportLoading">
										<el-icon class="mr-2"><Download /></el-icon>
										{{ exportLoading ? 'Exporting...' : 'Export to PDF' }}
									</el-dropdown-item>
									<el-dropdown-item
										command="duplicate"
										:disabled="duplicateLoading"
									>
										<el-icon class="mr-2"><CopyDocument /></el-icon>
										{{ duplicateLoading ? 'Duplicating...' : 'Duplicate' }}
									</el-dropdown-item>
									<el-dropdown-item divided>
										<HistoryButton
											:id="checklist.id"
											:type="WFEMoudels.Checklist"
										/>
									</el-dropdown-item>
									<el-dropdown-item
										divided
										command="delete"
										class="text-red-500"
										:disabled="deleteLoading"
									>
										<el-icon class="mr-2"><Delete /></el-icon>
										{{ deleteLoading ? 'Deleting...' : 'Delete' }}
									</el-dropdown-item>
								</el-dropdown-menu>
							</template>
						</el-dropdown>
					</div>
				</div>
				<p class="text-white text-sm mt-1.5 truncate h-6">
					{{ checklist.description }}
				</p>
			</div>
		</template>

		<!-- 卡片内容 -->
		<div class="">
			<div class="space-y-3">
				<!-- Assignments区域 -->
				<div class="space-y-2">
					<div class="flex items-center text-sm">
						<span class="card-label">Assignments:</span>
					</div>
					<div class="assignments-container" style="height: 60px; overflow: hidden">
						<template
							v-for="(assignment, index) in getDisplayedAssignments(
								checklist.assignments
							)"
							:key="`${assignment.workflowId}-${assignment.stageId}`"
						>
							<!-- 第一个assignment独占一行 -->
							<div v-if="index === 0" class="flex gap-2 mb-2">
								<span
									class="card-link card-link-full"
									:title="`${getWorkflowName(
										assignment.workflowId
									)} → ${getStageName(assignment.stageId)}`"
								>
									<span
										class="w-full text-center overflow-hidden text-ellipsis whitespace-nowrap block"
									>
										{{
											`${getWorkflowName(
												assignment.workflowId
											)} → ${getStageName(assignment.stageId)}`
										}}
									</span>
								</span>
							</div>
							<!-- 第二个assignment，根据是否有剩余内容决定是否与+几按钮共享一行 -->
							<div v-if="index === 1" class="flex gap-2 items-center">
								<span
									:class="{
										'card-link': true,
										'card-link-full':
											getUniqueAssignments(checklist.assignments).length <= 2,
										'card-link-shared':
											getUniqueAssignments(checklist.assignments).length > 2,
									}"
									:title="`${getWorkflowName(
										assignment.workflowId
									)} → ${getStageName(assignment.stageId)}`"
								>
									<span
										class="w-full text-center overflow-hidden text-ellipsis whitespace-nowrap block"
									>
										{{
											`${getWorkflowName(
												assignment.workflowId
											)} → ${getStageName(assignment.stageId)}`
										}}
									</span>
								</span>
								<!-- 显示剩余数量的按钮 -->
								<el-popover
									v-if="
										checklist.assignments &&
										getUniqueAssignments(checklist.assignments).length > 2
									"
									placement="top"
									:width="400"
									trigger="click"
								>
									<template #reference>
										<span class="card-link-more">
											+{{
												getUniqueAssignments(checklist.assignments).length -
												2
											}}
										</span>
									</template>
									<div class="popover-content">
										<h4 class="popover-title">More Assignments</h4>
										<div class="popover-tags">
											<span
												class="popover-tag"
												v-for="moreAssignment in getUniqueAssignments(
													checklist.assignments
												).slice(2)"
												:key="`${moreAssignment.workflowId}-${moreAssignment.stageId}`"
												:title="`${getWorkflowName(
													moreAssignment.workflowId
												)} → ${getStageName(moreAssignment.stageId)}`"
											>
												<span class="popover-tag-text">
													{{
														`${getWorkflowName(
															moreAssignment.workflowId
														)} → ${getStageName(
															moreAssignment.stageId
														)}`
													}}
												</span>
											</span>
										</div>
									</div>
								</el-popover>
							</div>
						</template>
					</div>
				</div>

				<div class="flex items-center justify-between text-sm">
					<el-tooltip class="flex-1" content="team">
						<div class="flex flex-1 items-center gap-2">
							<Icon
								icon="fluent-mdl2:team-favorite"
								class="text-primary-500 w-5 h-5"
							/>
							<span class="font-medium">
								{{ checklist.teamName || checklist.team }}
							</span>
						</div>
					</el-tooltip>
					<el-tooltip class="flex-1" content="total number of tasks">
						<div class="flex flex-1 items-center gap-2">
							<Icon
								icon="material-symbols-light:insert-page-break"
								class="text-primary-500 w-5 h-5"
							/>
							<span class="font-medium">
								{{ checklist.totalTasks || 0 }}
							</span>
						</div>
					</el-tooltip>
				</div>
				<div class="flex items-center justify-between text-sm">
					<el-tooltip class="flex-1" content="last modify by">
						<div class="flex flex-1 items-center gap-2">
							<Icon icon="ic:baseline-person-3" class="text-primary-500 w-5 h-5" />
							<span class="font-medium">
								{{ checklist.modifyBy }}
							</span>
						</div>
					</el-tooltip>
					<el-tooltip class="flex-1" content="last modify date">
						<div class="flex flex-1 items-center gap-2">
							<Icon
								icon="ic:baseline-calendar-month"
								class="text-primary-500 w-5 h-5"
							/>
							<span class="font-medium">
								{{
									timeZoneConvert(
										checklist.modifyDate,
										false,
										projectTenMinuteDate
									)
								}}
							</span>
						</div>
					</el-tooltip>
				</div>
			</div>
		</div>
	</el-card>
</template>

<script setup>
import { Edit, CopyDocument, Delete, MoreFilled, Download } from '@element-plus/icons-vue';
import { Icon } from '@iconify/vue';
import { timeZoneConvert } from '@/hooks/time';
import { projectTenMinuteDate, defaultStr } from '@/settings/projectSetting';
import { WFEMoudels } from '@/enums/appEnum';

// Props
const props = defineProps({
	checklist: {
		type: Object,
		required: true,
	},
	workflows: {
		type: Array,
		default: () => [],
	},
	allStages: {
		type: Array,
		default: () => [],
	},
	deleteLoading: {
		type: Boolean,
		default: false,
	},
	exportLoading: {
		type: Boolean,
		default: false,
	},
	duplicateLoading: {
		type: Boolean,
		default: false,
	},
});

// Emits
defineEmits(['command']);

const getWorkflowName = (workflowId) => {
	if (!workflowId || workflowId === '0') return defaultStr;
	const workflow = props.workflows.find((w) => w.id === workflowId);
	return workflow?.name || workflowId;
};

const getStageName = (stageId) => {
	if (!stageId || stageId === '0') return defaultStr;
	const stage = props.allStages.find((s) => s.id === stageId);
	return stage ? stage.name : stageId;
};

// 获取显示的分配数量（去重）
const getDisplayedAssignments = (assignments) => {
	const displayedCount = 2; // 显示2个
	if (!assignments || assignments.length === 0) {
		return [];
	}

	// 根据workflowId+stageId组合进行去重
	const uniqueAssignments = assignments.filter((assignment, index, self) => {
		return (
			index ===
			self.findIndex(
				(a) => a.workflowId === assignment.workflowId && a.stageId === assignment.stageId
			)
		);
	});

	// 返回前N个去重后的数据
	return uniqueAssignments.slice(0, displayedCount);
};

// 获取去重后的所有数据
const getUniqueAssignments = (assignments) => {
	if (!assignments || assignments.length === 0) {
		return [];
	}

	return assignments.filter((assignment, index, self) => {
		return (
			index ===
			self.findIndex(
				(a) => a.workflowId === assignment.workflowId && a.stageId === assignment.stageId
			)
		);
	});
};
</script>

<style scoped lang="scss">
/* Assignments 相关样式已整合到 @styles/components/assignments.scss */

.expand-btn {
	@apply text-primary-600 border-primary-200;
}

.expand-btn:hover {
	@apply bg-primary-50 border-primary-300;
}
</style>
