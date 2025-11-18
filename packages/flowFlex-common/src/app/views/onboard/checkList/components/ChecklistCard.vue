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
									<el-dropdown-item
										command="edit"
										v-if="
											functionPermission(
												ProjectPermissionEnum.checkList.update
											)
										"
									>
										<el-icon class="mr-2"><Edit /></el-icon>
										Edit
									</el-dropdown-item>
									<el-dropdown-item
										command="task"
										v-if="
											functionPermission(ProjectPermissionEnum.checkList.read)
										"
									>
										<el-icon class="mr-2"><Edit /></el-icon>
										View Tasks
									</el-dropdown-item>
									<el-dropdown-item
										command="export"
										:disabled="exportLoading"
										v-if="
											functionPermission(ProjectPermissionEnum.checkList.read)
										"
									>
										<el-icon class="mr-2"><Download /></el-icon>
										{{ exportLoading ? 'Exporting...' : 'Export to PDF' }}
									</el-dropdown-item>
									<el-dropdown-item
										command="duplicate"
										v-if="
											functionPermission(
												ProjectPermissionEnum.checkList.create
											)
										"
										:disabled="duplicateLoading"
									>
										<el-icon class="mr-2"><CopyDocument /></el-icon>
										{{ duplicateLoading ? 'Duplicating...' : 'Duplicate' }}
									</el-dropdown-item>
									<el-divider class="my-0" />
									<el-dropdown-item
										v-if="
											functionPermission(ProjectPermissionEnum.checkList.read)
										"
									>
										<HistoryButton
											:id="checklist.id"
											:type="WFEMoudels.Checklist"
										/>
									</el-dropdown-item>
									<el-divider class="my-0" />
									<el-dropdown-item
										command="delete"
										class="text-red-500 hover:!bg-red-500 hover:!text-white"
										:disabled="deleteLoading"
										v-if="
											functionPermission(
												ProjectPermissionEnum.checkList.delete
											)
										"
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
				<AssignmentsDisplay
					:assignments="checklist.assignments"
					:workflows="workflows"
					:all-stages="allStages"
					container-height="60px"
					:display-count="2"
				/>

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
import { projectTenMinuteDate } from '@/settings/projectSetting';
import { WFEMoudels } from '@/enums/appEnum';
import { functionPermission } from '@/hooks';
import { ProjectPermissionEnum } from '@/enums/permissionEnum';
import AssignmentsDisplay from '@/components/common/AssignmentsDisplay.vue';

// Props
defineProps({
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
</script>
