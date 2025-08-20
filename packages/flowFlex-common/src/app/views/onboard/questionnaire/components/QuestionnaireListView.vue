<template>
	<div class="customer-block !p-0 !ml-0">
		<el-table
			:data="questionnaires"
			@selection-change="handleSelectionChange"
			@sort-change="handleSortChange"
			class="w-full rounded-none"
			v-loading="loading"
			:max-height="tableMaxHeight"
			header-cell-class-name="bg-blue-50"
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
							class="p-1 text-gray-600 hover:text-blue-600"
							link
							:icon="ArrowDownBold"
						/>

						<template #dropdown>
							<el-dropdown-menu>
								<el-dropdown-item @click="$emit('command', 'edit', row)">
									<el-icon><Edit /></el-icon>
									Edit
								</el-dropdown-item>
								<el-dropdown-item @click="$emit('command', 'preview', row)">
									<el-icon><View /></el-icon>
									Preview
								</el-dropdown-item>
								<el-dropdown-item @click="$emit('command', 'duplicate', row)">
									<el-icon><CopyDocument /></el-icon>
									Duplicate
								</el-dropdown-item>
								<el-dropdown-item
									@click="$emit('command', 'delete', row)"
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
						<div
							class="table-assignment-tag flex-1"
							v-if="getDisplayedAssignments(row.assignments).length > 0"
							:key="`${getDisplayedAssignments(row.assignments)[0].workflowId}-${
								getDisplayedAssignments(row.assignments)[0].stageId
							}`"
							:title="`${getWorkflowName(
								getDisplayedAssignments(row.assignments)[0].workflowId
							)} → ${getStageName(
								getDisplayedAssignments(row.assignments)[0].stageId
							)}`"
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
						</div>
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
									<div
										class="popover-tag"
										v-for="assignment in getUniqueAssignments(
											row.assignments
										).slice(1)"
										:key="`${assignment.workflowId}-${assignment.stageId}`"
										:title="`${getWorkflowName(
											assignment.workflowId
										)} → ${getStageName(assignment.stageId)}`"
									>
										<span class="popover-tag-text">
											{{
												`${getWorkflowName(
													assignment.workflowId
												)} → ${getStageName(assignment.stageId)}`
											}}
										</span>
									</div>
								</div>
							</div>
						</el-popover>
					</div>
				</template>
			</el-table-column>
			<el-table-column label="Sections" sortable="custom" width="120" align="center">
				<template #default="{ row }">
					<div class="table-cell-content">
						{{ JSON.parse(row.structureJson).sections.length }}
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
			<el-table-column prop="modifyDate" label="Modified Date" sortable="custom" width="250">
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
/* 表格视图样式 */
.table-assignment-tag {
	@apply inline-flex items-center justify-center rounded-full border text-xs font-semibold transition-colors bg-primary-50 text-primary-500 border-primary-200 px-3 py-1;
	min-width: 0;
	max-width: 100%;
	flex-shrink: 1;
	background: linear-gradient(to right, rgb(196, 181, 253), rgb(191, 219, 254)) !important;
}

.table-assignment-text {
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
	max-width: 100%;
	text-align: center;
	display: block;
}

.table-assignment-tag:hover {
	@apply bg-primary-100 border-primary-300;
}

.table-assignment-more {
	@apply inline-flex items-center rounded-full border text-xs font-semibold transition-colors bg-primary-50 text-primary-500 border-primary-200 px-2 py-1;
	white-space: nowrap;
	min-width: 40px;
	width: auto;
	justify-content: center;
	flex-shrink: 0;
	background: linear-gradient(to right, rgb(196, 181, 253), rgb(191, 219, 254)) !important;
}

.table-assignment-more:hover {
	@apply bg-primary-100 border-primary-300;
}

/* 表格单元格内容样式 */
.table-cell-content {
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
	max-width: 100%;
	display: block;
}

/* 表格链接样式 */
.table-cell-link {
	display: block;
	width: 100%;
}

:deep(.table-cell-link .el-link__inner) {
	display: block;
	width: 100%;
}

/* 弹出层样式 */
.popover-title {
	font-size: 14px;
	font-weight: 600;
	color: var(--primary-700);
	@apply dark:text-primary-300;
	margin-bottom: 10px;
}

.popover-tags {
	display: flex;
	flex-direction: column;
	gap: 8px;
}

.popover-tag {
	@apply inline-flex items-center justify-center rounded-full border text-xs font-semibold transition-colors bg-primary-50 text-primary-500 border-primary-200 px-3 py-1;
	width: 100%;
	min-width: 0;
	flex-shrink: 0;
	background: linear-gradient(to right, rgb(196, 181, 253), rgb(191, 219, 254)) !important;
}

.popover-tag-text {
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
	max-width: 100%;
	text-align: center;
	display: block;
}

.popover-tag:hover {
	@apply bg-primary-100 border-primary-300;
}

/* 暗色主题样式 */
html.dark {
	/* 表格视图暗色主题样式 */
	.table-assignment-tag {
		@apply bg-black-200 text-white-100 border-black-200;
		background: linear-gradient(to right, #4c1d95, #1e40af) !important;
	}

	.table-assignment-tag:hover {
		@apply bg-black-300 border-black-100;
	}

	.table-assignment-text {
		@apply text-white-100;
	}

	.table-assignment-more {
		@apply bg-black-200 text-white-100 border-black-200;
		background: linear-gradient(to right, #4c1d95, #1e40af) !important;
	}

	.table-assignment-more:hover {
		@apply bg-black-300 border-black-100;
	}

	/* 弹窗暗色主题样式 */
	.popover-tag {
		@apply bg-black-200 text-white-100 border-black-200;
		background: linear-gradient(to right, #4c1d95, #1e40af) !important;
	}

	.popover-tag:hover {
		@apply bg-black-300 border-black-100;
	}

	.popover-tag-text {
		@apply text-white-100;
	}

	/* 表格暗色主题 */
	:deep(.el-table) {
		background-color: var(--black-400) !important;
		border: 1px solid var(--black-200) !important;
	}

	:deep(.el-table .bg-blue-50) {
		background-color: #003c76 !important;
	}

	:deep(.el-table th) {
		background-color: #003c76 !important;
		border-bottom: 1px solid #00509d !important;
		color: #cce8d0 !important;
	}

	:deep(.el-table td) {
		border-bottom: 1px solid var(--black-200) !important;
		background-color: var(--black-400) !important;
		color: var(--white-100) !important;
	}

	:deep(.el-table tbody tr:hover > td) {
		background-color: var(--black-300) !important;
	}

	/* 表格链接暗色主题 */
	:deep(.table-cell-link) {
		color: var(--primary-400) !important;
	}

	:deep(.table-cell-link:hover) {
		color: var(--primary-300) !important;
	}
}
</style>
