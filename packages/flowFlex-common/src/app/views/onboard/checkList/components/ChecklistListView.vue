<template>
	<div class="">
		<el-table
			:data="checklists"
			@selection-change="handleSelectionChange"
			@sort-change="handleSortChange"
			class="w-full rounded-none"
			v-loading="loading"
			:max-height="tableMaxHeight"
			header-cell-class-name="bg-blue-50"
			border
		>
			<template #empty>
				<slot name="empty">
					<el-empty description="No Data" :image-size="50" />
				</slot>
			</template>
			<el-table-column type="selection" fixed="left" width="50" align />
			<el-table-column label="Actions" fixed="left" width="80">
				<template #default="{ row }">
					<el-dropdown trigger="click">
						<el-button
							size="small"
							class="p-1 checklist-action-btn"
							link
							:icon="ArrowDownBold"
						/>

						<template #dropdown>
							<el-dropdown-menu>
								<el-dropdown-item @click="handleCommand('edit', row)">
									<el-icon><Edit /></el-icon>
									Edit
								</el-dropdown-item>
								<el-dropdown-item @click="handleCommand('task', row)">
									<el-icon><Edit /></el-icon>
									View Tasks
								</el-dropdown-item>
								<el-dropdown-item
									command="export"
									:disabled="exportLoading"
									@click="handleCommand('export', row)"
								>
									<el-icon><Download /></el-icon>
									Export to PDF
								</el-dropdown-item>
								<el-dropdown-item @click="handleCommand('duplicate', row)">
									<el-icon><CopyDocument /></el-icon>
									Duplicate
								</el-dropdown-item>
								<el-dropdown-item divided>
									<HistoryButton :id="row.id" :type="WFEMoudels.Checklist" />
								</el-dropdown-item>
								<el-dropdown-item
									divided
									@click="handleCommand('delete', row)"
									class="text-red-500"
								>
									<el-icon><Delete /></el-icon>
									Delete
								</el-dropdown-item>
							</el-dropdown-menu>
						</template>
					</el-dropdown>
				</template>
			</el-table-column>
			<el-table-column prop="name" label="Checklist Name" min-width="220" fixed="left">
				<template #default="{ row }">
					<el-link
						type="primary"
						:underline="false"
						@click="handleCommand('task', row)"
						class="table-cell-link"
					>
						<div class="table-cell-content" :title="row.name">
							{{ row.name }}
						</div>
					</el-link>
				</template>
			</el-table-column>
			<el-table-column prop="description" label="Description" min-width="200">
				<template #default="{ row }">
					<div class="table-cell-content" :title="row.description">
						{{ row.description || 'No description' }}
					</div>
				</template>
			</el-table-column>
			<el-table-column prop="team" label="Team" width="120">
				<template #default="{ row }">
					<div class="table-cell-content" :title="row.team">
						{{ row.team }}
					</div>
				</template>
			</el-table-column>
			<el-table-column prop="assignments" label="Assignments" min-width="300">
				<template #default="{ row }">
					<div class="flex items-center gap-1 max-w-full">
						<!-- 只显示第一个assignment -->
						<el-tag
							class="table-assignment-tag"
							v-if="getDisplayedAssignments(row.assignments).length > 0"
							:key="`${getDisplayedAssignments(row.assignments)[0].workflowId}-${
								getDisplayedAssignments(row.assignments)[0].stageId
							}`"
							:title="`${getWorkflowName(
								getDisplayedAssignments(row.assignments)[0].workflowId
							)} → ${getStageName(
								getDisplayedAssignments(row.assignments)[0].stageId
							)}`"
							type="primary"
						>
							<span class="table-assignment-text">
								{{
									`${getWorkflowName(
										getDisplayedAssignments(row.assignments)[0].workflowId
									)} → ${getStageName(
										getDisplayedAssignments(row.assignments)[0].stageId
									)}`
								}}
							</span>
						</el-tag>
						<!-- 显示剩余数量 -->
						<el-popover
							v-if="
								row.assignments && getUniqueAssignments(row.assignments).length > 1
							"
							placement="top"
							:width="400"
							trigger="click"
						>
							<template #reference>
								<span class="table-assignment-more flex-shrink-0">
									+{{ getUniqueAssignments(row.assignments).length - 1 }}
								</span>
							</template>
							<div class="popover-content">
								<h4 class="popover-title">All Assignments</h4>
								<div class="popover-tags">
									<el-tag
										class="popover-tag"
										v-for="assignment in getUniqueAssignments(
											row.assignments
										).slice(1)"
										:key="`${assignment.workflowId}-${assignment.stageId}`"
										:title="`${getWorkflowName(
											assignment.workflowId
										)} → ${getStageName(assignment.stageId)}`"
										type="primary"
									>
										<span class="popover-tag-text">
											{{
												`${getWorkflowName(
													assignment.workflowId
												)} → ${getStageName(assignment.stageId)}`
											}}
										</span>
									</el-tag>
								</div>
							</div>
						</el-popover>
					</div>
				</template>
			</el-table-column>
			<el-table-column label="Tasks" width="120" align="center">
				<template #default="{ row }">
					<div class="table-cell-content">
						{{ row.totalTasks || 0 }}
					</div>
				</template>
			</el-table-column>
			<el-table-column prop="modifyBy" label="Modified By" width="140">
				<template #default="{ row }">
					<div class="table-cell-content" :title="row.modifyBy">
						{{ row.modifyBy || 'Unknown' }}
					</div>
				</template>
			</el-table-column>
			<el-table-column prop="modifyDate" label="Modified Date" width="250">
				<template #default="{ row }">
					<div
						class="table-cell-content"
						:title="timeZoneConvert(row.modifyDate, false, projectTenMinuteDate)"
					>
						{{ timeZoneConvert(row.modifyDate, false, projectTenMinuteDate) }}
					</div>
				</template>
			</el-table-column>
		</el-table>
	</div>
</template>

<script setup>
import { Edit, CopyDocument, Delete, Download, ArrowDownBold } from '@element-plus/icons-vue';
import { timeZoneConvert } from '@/hooks/time';
import { projectTenMinuteDate, defaultStr, tableMaxHeight } from '@/settings/projectSetting';
import { WFEMoudels } from '@/enums/appEnum';

// Props
const props = defineProps({
	checklists: {
		type: Array,
		default: () => [],
	},
	loading: {
		type: Boolean,
		default: false,
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
const emit = defineEmits([
	'edit-checklist',
	'delete-checklist',
	'export-checklist',
	'duplicate-checklist',
	'view-tasks',
	'new-checklist',
	'selection-change',
	'sort-change',
]);

// Methods
const handleCommand = (command, checklist) => {
	switch (command) {
		case 'task':
			emit('view-tasks', checklist);
			break;
		case 'edit':
			emit('edit-checklist', checklist);
			break;
		case 'export':
			emit('export-checklist', checklist);
			break;
		case 'duplicate':
			emit('duplicate-checklist', checklist);
			break;
		case 'delete':
			emit('delete-checklist', checklist.id);
			break;
	}
};

const handleSelectionChange = (selection) => {
	emit('selection-change', selection);
};

const handleSortChange = (sort) => {
	emit('sort-change', sort);
};

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
	const displayedCount = 1; // 只显示1个
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
/* Checklist custom classes */
.checklist-action-btn {
	color: var(--el-text-color-regular);
}

.checklist-action-btn:hover {
	color: var(--el-color-primary);
}
</style>
