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
								<el-dropdown-item
									v-if="
										hasPermission(row.id, ProjectPermissionEnum.workflow.update)
									"
									@click="$emit('command', 'edit', row)"
								>
									<el-icon><Edit /></el-icon>
									Edit Workflow
								</el-dropdown-item>
								<el-dropdown-item
									v-if="
										!row.isDefault &&
										row.status === 'active' &&
										hasPermission(row.id, ProjectPermissionEnum.workflow.update)
									"
									@click="$emit('command', 'setDefault', row)"
								>
									<el-icon><Star /></el-icon>
									Set as Default
								</el-dropdown-item>
								<el-dropdown-item
									v-if="
										row.status === 'active' &&
										hasPermission(row.id, ProjectPermissionEnum.workflow.update)
									"
									@click="$emit('command', 'deactivate', row)"
								>
									<el-icon><CircleClose /></el-icon>
									Set as Inactive
								</el-dropdown-item>
								<el-dropdown-item
									v-if="
										row.status === 'inactive' &&
										hasPermission(row.id, ProjectPermissionEnum.workflow.update)
									"
									@click="$emit('command', 'activate', row)"
								>
									<el-icon><Check /></el-icon>
									Set as Active
								</el-dropdown-item>
								<el-dropdown-item
									v-if="
										hasPermission(row.id, ProjectPermissionEnum.workflow.create)
									"
									@click="$emit('command', 'duplicate', row)"
								>
									<el-icon><CopyDocument /></el-icon>
									Duplicate
								</el-dropdown-item>
								<el-dropdown-item
									v-if="
										hasPermission(row.id, ProjectPermissionEnum.workflow.read)
									"
									@click="$emit('command', 'workflowChart', row)"
								>
									<el-icon>
										<Connection />
									</el-icon>
									Workflow Chart
								</el-dropdown-item>
								<el-dropdown-item
									v-if="functionPermission(ProjectPermissionEnum.workflow.read)"
									divided
								>
									<HistoryButton :id="row.id" :type="WFEMoudels.Workflow" />
								</el-dropdown-item>
								<el-dropdown-item
									v-if="functionPermission(ProjectPermissionEnum.workflow.read)"
									@click="$emit('command', 'export', row)"
								>
									<el-icon><Download /></el-icon>
									Export Workflow
								</el-dropdown-item>
							</el-dropdown-menu>
						</template>
					</el-dropdown>
				</template>
			</el-table-column>
			<el-table-column prop="name" label="Name" min-width="280" fixed="left">
				<template #default="{ row }">
					<div class="workflow-name-cell">
						<!-- 名称区域（左侧，可收缩） -->
						<div
							class="workflow-name-link table-cell-link"
							:class="
								functionPermission(ProjectPermissionEnum.workflow.read)
									? 'cursor-pointer'
									: 'cursor-not-allowed'
							"
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
							<el-tag v-if="row.status === 'active'" type="success" size="small">
								Active
							</el-tag>
							<el-tag v-else type="danger" size="small">Inactive</el-tag>
						</div>
					</div>
				</template>
			</el-table-column>
			<el-table-column label="Stage Number" width="160" align="center">
				<template #default="{ row }">
					<div class="table-cell-content">
						{{ row.stages?.length || 0 }}
					</div>
				</template>
			</el-table-column>
			<el-table-column prop="createBy" label="Created By" min-width="140">
				<template #default="{ row }">
					<div class="table-cell-content" :title="row.createBy">
						{{ row.createBy || 'Unknown' }}
					</div>
				</template>
			</el-table-column>
			<el-table-column prop="createDate" label="Create Time" width="180">
				<template #default="{ row }">
					<div
						class="table-cell-content"
						:title="timeZoneConvert(row.createDate, false, projectTenMinuteDate)"
					>
						{{ timeZoneConvert(row.createDate, false, projectTenMinuteDate) }}
					</div>
				</template>
			</el-table-column>
			<el-table-column prop="modifyBy" label="Modified By" min-width="140">
				<template #default="{ row }">
					<div class="table-cell-content" :title="row.modifyBy">
						{{ row.modifyBy || 'Unknown' }}
					</div>
				</template>
			</el-table-column>
			<el-table-column prop="modifyDate" label="Modified Time" width="180">
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
import {
	Edit,
	CopyDocument,
	ArrowDownBold,
	Star,
	CircleClose,
	Check,
	Download,
	Connection,
} from '@element-plus/icons-vue';
import { timeZoneConvert } from '@/hooks/time';
import { projectTenMinuteDate, tableMaxHeight } from '@/settings/projectSetting';
import { Workflow } from '#/onboard';
import { WFEMoudels } from '@/enums/appEnum';
import StarIcon from '@assets/svg/workflow/star.svg';
import { ProjectPermissionEnum } from '@/enums/permissionEnum';
import { functionPermission } from '@/hooks';

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

// 检查是否有权限（功能权限 && 数据权限）
const hasPermission = (workflowId: string, functionalPermission: string) => {
	// 从 workflows 列表中查找对应的 workflow
	const workflow = props.workflows.find((w) => w.id === workflowId);
	if (workflow && workflow.permission) {
		return functionPermission(functionalPermission) && workflow.permission.canOperate;
	}
	return functionPermission(functionalPermission);
};

// Methods
const handleSelectionChange = (selection: Workflow[]) => {
	emit('selection-change', selection);
};

const handleSortChange = (sort: any) => {
	emit('sort-change', sort);
};

const handleWorkflowSelect = (workflow: Workflow) => {
	if (!functionPermission(ProjectPermissionEnum.workflow.read)) return;
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

.table-cell-link {
	font-weight: 500;
}

.ai-sparkles {
	font-size: var(--button-2-size); /* 12px - Item Button 2 */
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
