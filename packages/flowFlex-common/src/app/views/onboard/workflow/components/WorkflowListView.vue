<template>
	<div class="!p-0 !ml-0">
		<el-table
			:data="workflows"
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
			<el-table-column label="Actions" fixed="left" width="80">
				<template #default="{ row }">
					<el-dropdown
						trigger="click"
						@click.stop
						:disabled="isWorkflowActionLoading(row.id)"
					>
						<el-button
							size="small"
							link
							:icon="ArrowDownBold"
							:loading="isWorkflowActionLoading(row.id)"
						/>

						<template #dropdown>
							<el-dropdown-menu>
								<el-dropdown-item @click="$emit('command', 'edit', row)">
									<el-icon><Edit /></el-icon>
									Edit
								</el-dropdown-item>
								<el-dropdown-item
									v-if="!row.isDefault && row.status === 'active'"
									@click="$emit('command', 'setDefault', row)"
								>
									<el-icon><Star /></el-icon>
									Set as Default
								</el-dropdown-item>
								<el-dropdown-item
									v-if="row.status === 'active'"
									@click="$emit('command', 'deactivate', row)"
								>
									<el-icon><CircleClose /></el-icon>
									Set as Inactive
								</el-dropdown-item>
								<el-dropdown-item
									v-if="row.status === 'inactive'"
									@click="$emit('command', 'activate', row)"
								>
									<el-icon><Check /></el-icon>
									Set as Active
								</el-dropdown-item>
								<el-dropdown-item @click="$emit('command', 'duplicate', row)">
									<el-icon><CopyDocument /></el-icon>
									Duplicate
								</el-dropdown-item>
								<el-dropdown-item divided>
									<HistoryButton :id="row.id" :type="WFEMoudels.Workflow" />
								</el-dropdown-item>
								<el-dropdown-item @click="$emit('command', 'export', row)">
									<el-icon><Download /></el-icon>
									Export Workflow
								</el-dropdown-item>
								<el-dropdown-item
									@click="$emit('command', 'delete', row)"
									class="text-red-500"
									divided
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
				label="Name"
				sortable="custom"
				min-width="280"
				fixed="left"
			>
				<template #default="{ row }">
					<div class="workflow-name-cell">
						<!-- 名称区域（左侧，可收缩） -->
						<div
							class="workflow-name-link table-cell-link"
							@click="handleWorkflowSelect(row)"
							:title="row.name"
						>
							{{ row.name }}
						</div>
						<!-- 标签区域（右侧，固定） -->
						<div class="workflow-tags-right">
							<el-tag v-if="row.isDefault" type="warning" size="small">
								<div class="flex items-center gap-1 text-white font-bold">
									<StarIcon class="star-icon" />
									Default
								</div>
							</el-tag>
							<el-tag v-if="row.isAIGenerated" type="primary" size="small">
								<div class="flex items-center gap-1 text-white font-bold">
									<span class="ai-sparkles">✨</span>
									AI
								</div>
							</el-tag>
							<el-tag
								v-if="row.status === 'active'"
								type="success"
								size="small"
								class="rounded-xl"
							>
								Active
							</el-tag>
							<el-tag v-else type="danger" size="small" class="rounded-xl">
								Inactive
							</el-tag>
						</div>
					</div>
				</template>
			</el-table-column>
			<el-table-column label="Stage Number" sortable="custom" width="160" align="center">
				<template #default="{ row }">
					<div class="table-cell-content">
						{{ row.stages?.length || 0 }}
					</div>
				</template>
			</el-table-column>
			<el-table-column prop="createBy" label="Created By" sortable="custom" min-width="140">
				<template #default="{ row }">
					<div class="table-cell-content" :title="row.createBy">
						{{ row.createBy || 'Unknown' }}
					</div>
				</template>
			</el-table-column>
			<el-table-column prop="createDate" label="Create Time" sortable="custom" width="180">
				<template #default="{ row }">
					<div
						class="table-cell-content"
						:title="timeZoneConvert(row.createDate, false, projectTenMinuteDate)"
					>
						{{ timeZoneConvert(row.createDate, false, projectTenMinuteDate) }}
					</div>
				</template>
			</el-table-column>
			<el-table-column prop="modifyBy" label="Updated By" sortable="custom" min-width="140">
				<template #default="{ row }">
					<div class="table-cell-content" :title="row.modifyBy">
						{{ row.modifyBy || 'Unknown' }}
					</div>
				</template>
			</el-table-column>
			<el-table-column prop="modifyDate" label="Update Time" sortable="custom" width="180">
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
import {
	Edit,
	CopyDocument,
	Delete,
	ArrowDownBold,
	Star,
	CircleClose,
	Check,
	Download,
} from '@element-plus/icons-vue';
import { timeZoneConvert } from '@/hooks/time';
import { projectTenMinuteDate, tableMaxHeight } from '@/settings/projectSetting';
import { Workflow } from '#/onboard';
import { WFEMoudels } from '@/enums/appEnum';
import StarIcon from '@assets/svg/workflow/star.svg';

// Props
const props = defineProps<{
	workflows: Workflow[];
	loading: boolean;
	actionLoading?: {
		[workflowId: string]: {
			[action: string]: boolean;
		};
	};
}>();

// Emits
const emit = defineEmits<{
	command: [command: string, workflow: any];
	'selection-change': [selection: Workflow[]];
	'sort-change': [sort: any];
	'select-workflow': [workflowId: string];
}>();

// Methods
const handleSelectionChange = (selection: Workflow[]) => {
	emit('selection-change', selection);
};

const handleSortChange = (sort: any) => {
	emit('sort-change', sort);
};

const handleWorkflowSelect = (workflow: Workflow) => {
	emit('select-workflow', workflow.id);
};

// 检查workflow是否有任何操作正在loading
const isWorkflowActionLoading = (workflowId: string) => {
	if (!props.actionLoading || !props.actionLoading[workflowId]) {
		return false;
	}

	const workflowActions = props.actionLoading[workflowId];
	return Object.values(workflowActions).some((loading) => loading);
};
</script>

<style scoped lang="scss">
/* Workflow name cell layout */
.workflow-name-cell {
	display: flex;
	align-items: center;
	justify-content: space-between;
	width: 100%;
	min-width: 0; /* 允许flex子元素收缩 */
	gap: 8px;
}

.workflow-name-link {
	flex: 1;
	min-width: 0; /* 允许收缩 */
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
	display: block;
	font-weight: 500;
	color: var(--el-color-primary);
	cursor: pointer;
	transition: color 0.2s;
}

.workflow-name-link:hover {
	color: var(--el-color-primary-dark-2);
}

.workflow-tags-right {
	display: flex;
	align-items: center;
	gap: 4px;
	flex-shrink: 0; /* 标签不收缩 */
	margin-left: auto; /* 确保标签靠右 */
}

/* 表格单元格内容样式 */
.table-cell-content {
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.table-cell-link {
	font-weight: 500;
}

.ai-sparkles {
	font-size: 12px;
	animation: sparkle 2s ease-in-out infinite;
	display: inline-block;
}

@keyframes sparkle {
	0%,
	100% {
		transform: scale(1) rotate(0deg);
		opacity: 1;
	}
	25% {
		transform: scale(1.1) rotate(5deg);
		opacity: 0.9;
	}
	50% {
		transform: scale(1.2) rotate(-5deg);
		opacity: 0.8;
	}
	75% {
		transform: scale(1.1) rotate(3deg);
		opacity: 0.9;
	}
}

.star-icon {
	margin-right: 4px;
	width: 12px;
	height: 12px;
}
</style>
