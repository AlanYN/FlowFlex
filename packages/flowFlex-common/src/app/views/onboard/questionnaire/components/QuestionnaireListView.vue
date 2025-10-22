<template>
	<div class="!p-0 !ml-0">
		<el-table
			:data="questionnaires"
			@selection-change="handleSelectionChange"
			@sort-change="handleSortChange"
			class="w-full rounded-none"
			v-loading="loading"
			:max-height="tableMaxHeight"
			border
		>
			<template #empty>
				<slot name="empty">
					<el-empty description="No Data" :image-size="50" />
				</slot>
			</template>
			<el-table-column type="selection" fixed="left" width="50" align="center" />
			<el-table-column label="Actions" fixed="left" width="80">
				<template #default="{ row }">
					<el-dropdown trigger="click">
						<el-button
							size="small"
							class="p-1 list-action-btn"
							link
							:icon="ArrowDownBold"
						/>

						<template #dropdown>
							<el-dropdown-menu>
								<el-dropdown-item
									@click="$emit('command', 'edit', row)"
									v-if="functionPermission(ProjectPermissionEnum.question.update)"
								>
									<el-icon><Edit /></el-icon>
									Edit
								</el-dropdown-item>
								<el-dropdown-item
									@click="$emit('command', 'preview', row)"
									v-if="functionPermission(ProjectPermissionEnum.question.read)"
								>
									<el-icon><View /></el-icon>
									Preview
								</el-dropdown-item>
								<el-dropdown-item
									@click="$emit('command', 'duplicate', row)"
									v-if="functionPermission(ProjectPermissionEnum.question.create)"
								>
									<el-icon><CopyDocument /></el-icon>
									Duplicate
								</el-dropdown-item>
								<el-dropdown-item
									divided
									v-if="functionPermission(ProjectPermissionEnum.question.read)"
								>
									<HistoryButton :id="row.id" :type="WFEMoudels.Questionnaire" />
								</el-dropdown-item>
								<el-dropdown-item
									@click="$emit('command', 'delete', row)"
									v-if="functionPermission(ProjectPermissionEnum.question.delete)"
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
			<el-table-column
				prop="name"
				label="Questionnaire Name"
				sortable="custom"
				min-width="220"
				fixed="left"
			>
				<template #default="{ row }">
					<el-link
						type="primary"
						:underline="false"
						@click="$emit('command', 'edit', row)"
						class="table-cell-link"
					>
						<div class="table-cell-content" :title="row.name">
							{{ row.name }}
						</div>
					</el-link>
				</template>
			</el-table-column>
			<el-table-column
				prop="description"
				label="Description"
				sortable="custom"
				min-width="200"
			>
				<template #default="{ row }">
					<div class="table-cell-content" :title="row.description">
						{{ row.description }}
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
			<el-table-column label="Sections" sortable="custom" width="120" align="center">
				<template #default="{ row }">
					<div class="table-cell-content">
						{{
							row.structureJson
								? JSON.parse(row.structureJson)?.sections?.filter(
										(section) => !section.isDefault
								  )?.length || 0
								: 0
						}}
					</div>
				</template>
			</el-table-column>
			<el-table-column
				prop="totalQuestions"
				label="Questions"
				sortable="custom"
				width="120"
				align="center"
			>
				<template #default="{ row }">
					<div class="table-cell-content">
						{{ row.totalQuestions }}
					</div>
				</template>
			</el-table-column>
			<el-table-column prop="modifyBy" label="Modified By" sortable="custom" width="140">
				<template #default="{ row }">
					<div class="table-cell-content" :title="row.modifyBy">
						{{ row.modifyBy }}
					</div>
				</template>
			</el-table-column>
			<el-table-column prop="modifyDate" label="Modified Time" sortable="custom" width="250">
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

<script setup lang="ts">
import { defineProps, defineEmits } from 'vue';
import { Edit, CopyDocument, Delete, View, ArrowDownBold } from '@element-plus/icons-vue';
import { timeZoneConvert } from '@/hooks/time';
import { projectTenMinuteDate, defaultStr, tableMaxHeight } from '@/settings/projectSetting';
import { Questionnaire } from '#/onboard';
import { WFEMoudels } from '@/enums/appEnum';
import { functionPermission } from '@/hooks';
import { ProjectPermissionEnum } from '@/enums/permissionEnum';

// Props
const props = defineProps<{
	questionnaires: Questionnaire[];
	loading: boolean;
	workflows: any[];
	allStages: any[];
}>();

// Emits
const emit = defineEmits<{
	command: [command: string, questionnaire: any];
	'selection-change': [selection: Questionnaire[]];
	'sort-change': [sort: any];
}>();

// Methods
const handleSelectionChange = (selection: Questionnaire[]) => {
	emit('selection-change', selection);
};

const handleSortChange = (sort: any) => {
	emit('sort-change', sort);
};

const getWorkflowName = (workflowId: string) => {
	if (!workflowId || workflowId === '0') return defaultStr;
	const workflow = props.workflows.find((w) => w.id === workflowId);
	return workflow?.name || workflowId;
};

const getStageName = (stageId: string) => {
	if (!stageId || stageId === '0') return defaultStr;
	const stage = props.allStages.find((s) => s.id === stageId);
	return stage ? stage.name : stageId;
};

// 获取显示的分配数量（去重）
const getDisplayedAssignments = (assignments: any[]) => {
	const displayedCount = 5; // 显示5个
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
const getUniqueAssignments = (assignments: any[]) => {
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
.list-action-btn {
	color: var(--el-text-color-regular);

	&:hover {
		color: var(--el-color-primary);
	}
}
</style>
