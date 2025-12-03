<template>
	<div class="space-y-8">
		<!-- Fields to Share -->
		<div class="space-y-4">
			<div>
				<h3 class="text-lg font-semibold text-text-primary m-0">Fields to Share</h3>
				<p class="text-sm text-text-secondary mt-1">
					Fields shared with IAM System through outbound actions (read-only view).
				</p>
			</div>

			<!-- Search Filter -->
			<el-input
				v-model="fieldSearch"
				placeholder="Filter by field name..."
				clearable
				class="w-full"
			>
				<template #prefix>
					<el-icon><Search /></el-icon>
				</template>
			</el-input>

			<el-table
				:data="filteredFieldMappings"
				class="w-full"
				empty-text="No fields configured"
				:border="true"
			>
				<el-table-column label="Action ID" prop="actionCode" width="150">
					<template #default="{ row }">
						<span class="font-medium text-sm action-id">{{ row.actionCode }}</span>
					</template>
				</el-table-column>

				<el-table-column label="Action Name" prop="actionName" min-width="200">
					<template #default="{ row }">
						<span class="text-sm">{{ row.actionName }}</span>
					</template>
				</el-table-column>

				<el-table-column label="Field (Display Name)" prop="wfeFieldName" min-width="220">
					<template #default="{ row }">
						<span class="text-sm">{{ row.wfeFieldName }}</span>
					</template>
				</el-table-column>

				<el-table-column label="Field (API Name)" prop="externalFieldName" min-width="220">
					<template #default="{ row }">
						<span class="font-medium text-sm">{{ row.externalFieldName }}</span>
					</template>
				</el-table-column>
			</el-table>
		</div>

		<!-- Attachments to Share -->
		<div class="space-y-4">
			<div>
				<h3 class="text-lg font-semibold text-text-primary m-0">Attachments to Share</h3>
				<p class="text-sm text-text-secondary mt-1">
					Configure which workflow attachments can be shared with IAM System.
				</p>
			</div>

			<el-table
				v-loading="isLoading"
				:data="attachmentSharing"
				class="w-full"
				empty-text="No attachment sharing configured"
				:border="true"
			>
				<el-table-column label="Workflow" min-width="250">
					<template #default="{ row }">
						<el-select
							v-model="row.workflowId"
							placeholder="Select workflow..."
							@change="(val) => handleWorkflowChange(row, val)"
						>
							<el-option
								v-for="workflow in getAvailableWorkflows(row)"
								:key="workflow.id"
								:label="workflow.name"
								:value="String(workflow.id)"
							>
								<div class="flex items-center justify-between">
									<span>{{ workflow.name }}</span>
									<el-tag v-if="!workflow.isActive" type="danger">
										Inactive
									</el-tag>
									<el-tag v-else type="success">Active</el-tag>
								</div>
							</el-option>
						</el-select>
					</template>
				</el-table-column>

				<el-table-column label="Stage" min-width="250" align="center">
					<template #default="{ row }">
						<div
							v-if="!!row?.id"
							class="flex items-center gap-2 flex-wrap justify-center"
						>
							<template v-if="getStageList(row).length > 0">
								<el-tag
									v-for="stage in getDisplayedStages(row)"
									:key="stage.id"
									type="primary"
									size="small"
								>
									{{ stage.name }}
								</el-tag>
								<el-tooltip
									v-if="getHiddenStagesCount(row) > 0"
									placement="top"
									raw-content
									effect="light"
								>
									<template #content>
										<div class="flex flex-wrap gap-2">
											<el-tag
												v-for="stage in getHiddenStages(row)"
												:key="stage.id"
												type="primary"
												size="small"
											>
												{{ stage.name }}
											</el-tag>
										</div>
									</template>
									<el-tag type="primary" size="small" class="cursor-pointer">
										+{{ getHiddenStagesCount(row) }}
									</el-tag>
								</el-tooltip>
							</template>
							<span v-else class="text-sm text-text-secondary">-</span>
						</div>
						<el-select
							v-else
							v-model="row.stageId"
							placeholder="Select stage..."
							:disabled="!row.workflowId"
							:loading="stagesLoadingMap[row.workflowId] || false"
							@change="handleAttachmentSharingChange"
							@focus="() => loadStagesForWorkflow(row.workflowId)"
							multiple
							filterable
							collapse-tags
							collapse-tags-tooltip
							tag-type="primary"
						>
							<el-option
								v-for="stage in stagesCache[row.workflowId] || []"
								:key="stage.id"
								:label="stage.name"
								:value="String(stage.id)"
							/>
						</el-select>
					</template>
				</el-table-column>

				<el-table-column label="Actions" width="180" align="center">
					<template #default="{ $index }">
						<div class="flex items-center justify-center gap-1">
							<el-button
								type="danger"
								link
								@click="handleDeleteItem($index)"
								:icon="Delete"
							/>
						</div>
					</template>
				</el-table-column>
			</el-table>

			<div class="flex justify-between items-center">
				<el-button type="primary" @click="handleAddItem">
					<el-icon><Plus /></el-icon>
					Add Workflow & Stage
				</el-button>
				<el-button type="primary" :loading="isSaving" @click="saveAttachmentSharing">
					Save
				</el-button>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import { Search, Delete, Plus } from '@element-plus/icons-vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import {
	createOutboundSettingsAttachment,
	getOutboundSettingsAttachment,
} from '@/apis/integration';
import { getStagesByWorkflow } from '@/apis/ow';
import type { FieldMapping, OutboundAttachmentItem1 } from '#/integration';

interface Props {
	integrationId: string | number;
	outboundSettings?: {
		masterData?: string[];
		fields?: string[];
		attachmentWorkflows?: string[];
	};
	workflows?: any[];
	outboundFieldMappings?: FieldMapping[];
}

const props = defineProps<Props>();

// 搜索过滤
const fieldSearch = ref('');

const isLoading = ref(false);
const isSaving = ref(false);

// 附件共享扩展类型
interface IAttachmentSharingExtended extends OutboundAttachmentItem1 {
	isEditing?: boolean;
}

// 附件共享数据
const attachmentSharing = ref<IAttachmentSharingExtended[]>([]);

// Stages 数据缓存
const stagesCache = ref<Record<string, Array<{ id: string; name: string }>>>({});
// Stages 加载状态（按 workflowId）
const stagesLoadingMap = ref<Record<string, boolean>>({});

/**
 * 过滤后的字段映射列表
 */
const filteredFieldMappings = computed(() => {
	if (!fieldSearch.value) {
		return props.outboundFieldMappings || [];
	}

	const search = fieldSearch.value.toLowerCase();
	return props.outboundFieldMappings?.filter(
		(item) => item.wfeFieldName?.toLowerCase().includes(search)
	);
});

/**
 * 加载指定 workflow 的 stages 数据
 */
const loadStagesForWorkflow = async (workflowId: string) => {
	if (!workflowId || workflowId === '0' || stagesCache.value[workflowId]) {
		return stagesCache.value[workflowId] || [];
	}

	try {
		stagesLoadingMap.value[workflowId] = true;
		const response = await getStagesByWorkflow(workflowId);
		if (response.code === '200') {
			const stages = response.data || [];
			stagesCache.value[workflowId] = stages;
			return stages;
		} else {
			stagesCache.value[workflowId] = [];
			return [];
		}
	} catch (error) {
		console.error('Failed to load stages for workflow:', error);
		stagesCache.value[workflowId] = [];
		return [];
	} finally {
		stagesLoadingMap.value[workflowId] = false;
	}
};

/**
 * 获取当前行可用的 workflows（排除已被其他行选择的）
 */
function getAvailableWorkflows(currentRow: IAttachmentSharingExtended) {
	if (!props.workflows || props.workflows.length === 0) {
		return [];
	}

	// 获取所有已被其他行选择的 workflowId（排除当前行）
	const selectedWorkflowIds = new Set<string>();
	attachmentSharing.value.forEach((row) => {
		if (row !== currentRow && row.workflowId && row.workflowId !== '0') {
			selectedWorkflowIds.add(String(row.workflowId));
		}
	});

	// 过滤掉已被选择的 workflow，但保留当前行已选择的
	return props.workflows.filter((workflow) => {
		const workflowIdStr = String(workflow.id);
		// 如果当前行已选择此 workflow，则保留
		if (currentRow.workflowId === workflowIdStr) {
			return true;
		}
		// 否则，如果未被其他行选择，则保留
		return !selectedWorkflowIds.has(workflowIdStr);
	});
}

/**
 * 处理 workflow 变化
 */
const handleWorkflowChange = async (row: IAttachmentSharingExtended, workflowId: string) => {
	row.stageId = [];
	if (workflowId && workflowId !== '0') {
		await loadStagesForWorkflow(workflowId);
	}
	handleAttachmentSharingChange();
};

/**
 * 附件共享变更
 */
function handleAttachmentSharingChange() {
	// 数据变更处理（不自动保存）
}

/**
 * 加载附件工作流配置
 */
async function loadAttachmentWorkflows() {
	if (!props.integrationId || props.integrationId === 'new') {
		attachmentSharing.value = [];
		return;
	}

	isLoading.value = true;
	try {
		const response = await getOutboundSettingsAttachment(props.integrationId);
		if (response.success && response.data) {
			const items = response.data.items || [];
			attachmentSharing.value = items.map((item) => ({
				...item,
				workflowId: String(item.workflowId),
				stageId: Array.isArray(item.stageId) ? item.stageId : [item.stageId],
				isEditing: false,
			}));

			// 加载所有有 workflowId 的 stages（用于回显）
			const workflowIds = new Set<string>();
			items.forEach((item) => {
				if (item.workflowId) {
					workflowIds.add(String(item.workflowId));
				}
			});

			// 并行加载所有需要的 stages
			await Promise.all(
				Array.from(workflowIds).map((workflowId) => loadStagesForWorkflow(workflowId))
			);
		} else {
			attachmentSharing.value = [];
		}
	} catch (error) {
		console.error('Failed to load attachment workflows:', error);
		attachmentSharing.value = [];
	} finally {
		isLoading.value = false;
	}
}

/**
 * 保存附件工作流配置
 */
async function saveAttachmentSharing() {
	if (!props.integrationId || props.integrationId === 'new') {
		return;
	}

	// 过滤掉没有 workflowId 或 stageId 的项
	const validItems = attachmentSharing.value.filter(
		(item) =>
			item.workflowId && item.stageId && item.workflowId !== '0' && item.stageId.length > 0
	);

	if (validItems.length === 0) {
		// 如果没有有效项，清空配置
		try {
			await createOutboundSettingsAttachment(String(props.integrationId), []);
		} catch (error) {
			console.error('Failed to save attachment sharing:', error);
		}
		return;
	}

	isSaving.value = true;
	try {
		const items = validItems.map((item) => ({
			id: item.id || '',
			workflowId: item.workflowId,
			stageId: item.stageId,
		}));

		const response = await createOutboundSettingsAttachment(String(props.integrationId), items);
		if (response.success) {
			// 重新加载数据以获取最新的 id
			await loadAttachmentWorkflows();
		} else {
			ElMessage.error(response.msg || 'Failed to save attachment sharing');
		}
	} finally {
		isSaving.value = false;
	}
}

/**
 * 添加新项
 */
function handleAddItem() {
	attachmentSharing.value.push({
		id: '',
		workflowId: '',
		stageId: [],
		isEditing: true,
	});
}

/**
 * 删除项
 */
function handleDeleteItem(index: number) {
	const row = attachmentSharing.value[index];
	if (!row) return;

	ElMessageBox.confirm(
		`Are you sure you want to delete this workflow & stage configuration?`,
		'Confirm Delete',
		{
			confirmButtonText: 'Delete',
			cancelButtonText: 'Cancel',
			type: 'warning',
		}
	)
		.then(() => {
			attachmentSharing.value.splice(index, 1);
		})
		.catch(() => {
			// 取消删除
		});
}

/**
 * 获取 Stage 列表（用于显示 el-tag）
 */
function getStageList(row: IAttachmentSharingExtended): Array<{ id: string; name: string }> {
	if (!row.stageId || !row.workflowId) return [];
	const stageIds = Array.isArray(row.stageId) ? row.stageId : [row.stageId];
	const stages = stagesCache.value[row.workflowId] || [];
	return stageIds
		.map((id) => {
			return stages.find((s) => String(s.id) === String(id));
		})
		.filter(Boolean) as Array<{ id: string; name: string }>;
}

/**
 * 获取要显示的 Stage 列表（最多显示3个）
 */
function getDisplayedStages(row: IAttachmentSharingExtended): Array<{ id: string; name: string }> {
	const stages = getStageList(row);
	return stages.slice(0, 3);
}

/**
 * 获取隐藏的 Stage 数量
 */
function getHiddenStagesCount(row: IAttachmentSharingExtended): number {
	const stages = getStageList(row);
	return Math.max(0, stages.length - 3);
}

/**
 * 获取隐藏的 Stage 列表
 */
function getHiddenStages(row: IAttachmentSharingExtended): Array<{ id: string; name: string }> {
	const stages = getStageList(row);
	return stages.slice(3);
}

// 监听 integrationId 变化（包括初始化）
watch(
	() => props.integrationId,
	(newId) => {
		if (newId && newId !== 'new') {
			loadAttachmentWorkflows();
		} else {
			attachmentSharing.value = [];
		}
	},
	{ immediate: true }
);
</script>
