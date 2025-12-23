<template>
	<!-- Entity Type Mapping -->
	<div class="wfe-global-block-bg p-4 mt-8 space-y-4">
		<div>
			<h3 class="text-lg font-semibold text-text-primary m-0">Entity Type Mapping</h3>
			<p class="text-sm text-text-secondary mt-1">
				Configure which external entity types can enter WFE and be used as cases in
				workflows.
			</p>
		</div>

		<!-- 实体映射表格 -->
		<el-table
			:data="localEntityMappings"
			class="w-full"
			:border="true"
			empty-text="No entity types configured"
		>
			<el-table-column label="External Entity Type" prop="externalEntityName" min-width="250">
				<template #default="{ row }">
					<span v-if="isSaved(row)" class="text-sm text-text-primary">
						{{ row.externalEntityName || '-' }}
					</span>
					<el-input
						v-else
						v-model="row.externalEntityName"
						placeholder="Enter external entity type name"
					/>
				</template>
			</el-table-column>

			<el-table-column label="System ID" prop="systemId" width="200" align="center">
				<template #default="{ row }">
					<span class="text-sm text-text-secondary">
						{{ row?.systemId ? ` ${row.systemId}` : 'Auto-generated on save' }}
					</span>
				</template>
			</el-table-column>

			<el-table-column
				label="WFE Master Data"
				prop="wfeEntityType"
				min-width="80"
				align="center"
			>
				<template #default="{ row }">
					<span v-if="isSaved(row)" class="text-sm text-text-primary">
						{{ getWfeEntityLabel(row.wfeEntityType) || '-' }}
					</span>
					<el-select
						v-else
						:disabled="!row.externalEntityName"
						v-model="row.wfeEntityType"
						placeholder="Select master data Type"
					>
						<el-option
							v-for="entity in wfeEntityOptions"
							:key="entity.value"
							:label="entity.label"
							:value="entity.value"
						/>
					</el-select>
				</template>
			</el-table-column>

			<el-table-column label="Workflows" prop="workflowIds" min-width="200" align="center">
				<template #default="{ row }">
					<div v-if="isSaved(row)" class="flex items-center gap-2 flex-wrap">
						<template v-if="getWorkflowList(row.workflowIds).length > 0">
							<el-tag
								v-for="workflow in getDisplayedWorkflows(row.workflowIds)"
								:key="workflow.id"
								type="primary"
								size="small"
							>
								{{ workflow.name }}
							</el-tag>
							<el-tooltip
								v-if="getHiddenWorkflowsCount(row.workflowIds) > 0"
								placement="top"
								raw-content
								effect="light"
							>
								<template #content>
									<div class="flex flex-wrap gap-2">
										<el-tag
											v-for="workflow in getHiddenWorkflows(row.workflowIds)"
											:key="workflow.id"
											type="primary"
											size="small"
										>
											{{ workflow.name }}
										</el-tag>
									</div>
								</template>
								<el-tag type="primary" size="small" class="cursor-pointer">
									+{{ getHiddenWorkflowsCount(row.workflowIds) }}
								</el-tag>
							</el-tooltip>
						</template>
						<span v-else class="text-sm text-text-secondary">-</span>
					</div>
					<el-select
						v-else
						:disabled="!row.externalEntityName"
						v-model="row.workflowIds"
						placeholder="Select workflows"
						multiple
						collapse-tags
						collapse-tags-tooltip
						tag-type="primary"
					>
						<el-option
							v-for="workflow in allWorkflows"
							:key="workflow.id"
							:label="workflow.name"
							:value="workflow.id"
						>
							<div class="flex items-center justify-between">
								<span>{{ workflow.name }}</span>
								<el-tag v-if="!workflow.isActive" type="danger" size="small">
									Inactive
								</el-tag>
								<el-tag v-else type="success" size="small">Active</el-tag>
							</div>
						</el-option>
					</el-select>
				</template>
			</el-table-column>

			<el-table-column
				label=""
				width="50"
				fixed="right"
				v-if="functionPermission(ProjectPermissionEnum.integration.delete)"
			>
				<template #default="{ row, $index }">
					<div class="flex items-center justify-center">
						<el-button
							type="danger"
							link
							:icon="Delete"
							@click="handleDeleteEntityMapping(row, $index)"
						/>
					</div>
				</template>
			</el-table-column>
		</el-table>

		<!-- Add Entity Type 按钮 -->
		<div class="flex items-center justify-between">
			<el-button
				type="default"
				:icon="Plus"
				@click="handleAddEntityMapping"
				class="add-entity-btn"
				v-permission="ProjectPermissionEnum.integration.create"
			>
				Add Entity Type
			</el-button>
			<el-button type="primary" :loading="saving" @click="handleSaveEntityMapping">
				Save
			</el-button>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { Delete, Plus } from '@element-plus/icons-vue';
import { ElMessage, ElTooltip } from 'element-plus';
import { createEntityMapping } from '@/apis/integration';
import type { IEntityMapping, IWfeEntityOption } from '#/integration';
import { functionPermission } from '@/hooks';
import { ProjectPermissionEnum } from '@/enums/permissionEnum';

interface Props {
	integrationId: string | number;
	entityMappings?: IEntityMapping[];
	allWorkflows?: any[];
}

interface Emits {
	(e: 'refresh'): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

// 状态
const saving = ref(false);

// 实体映射数据（本地编辑状态）
const localEntityMappings = ref<IEntityMapping[]>([]);

// 模拟选项数据（实际应该从 API 获取）
const wfeEntityOptions = ref<IWfeEntityOption[]>([{ value: 'case', label: 'Case' }]);

/**
 * 判断是否已保存
 */
function isSaved(row: IEntityMapping): boolean {
	return !!(row?.id || (row as any)?.systemId);
}

/**
 * 获取 WFE Entity 的显示标签
 */
function getWfeEntityLabel(value: string): string {
	const option = wfeEntityOptions.value.find((opt) => opt.value === value);
	return option?.label || value;
}

/**
 * 获取 Workflow 列表（用于显示 el-tag）
 */
function getWorkflowList(workflowIds: string[] | number[]): any[] {
	if (!workflowIds || workflowIds.length === 0) return [];
	if (!props.allWorkflows || props.allWorkflows.length === 0) return [];

	return workflowIds
		.map((workflowId) => {
			return props.allWorkflows?.find((w) => w.id === workflowId);
		})
		.filter(Boolean) as any[];
}

/**
 * 获取要显示的 Workflow 列表（最多显示3个）
 */
function getDisplayedWorkflows(workflowIds: string[] | number[]): any[] {
	const workflows = getWorkflowList(workflowIds);
	return workflows.slice(0, 3);
}

/**
 * 获取隐藏的 Workflow 数量
 */
function getHiddenWorkflowsCount(workflowIds: string[] | number[]): number {
	const workflows = getWorkflowList(workflowIds);
	return Math.max(0, workflows.length - 3);
}

/**
 * 获取隐藏的 Workflow 列表
 */
function getHiddenWorkflows(workflowIds: string[] | number[]): any[] {
	const workflows = getWorkflowList(workflowIds);
	return workflows.slice(3);
}

/**
 * 添加实体映射
 */
function handleAddEntityMapping() {
	localEntityMappings.value.push({
		externalEntityName: '',
		externalEntityType: '',
		wfeEntityType: '',
		workflowIds: [],
		isActive: true,
	});
}

/**
 * 判断是否为空行（未填写必填字段）
 */
function isEmptyRow(mapping: IEntityMapping): boolean {
	return (
		!mapping.externalEntityName ||
		mapping.externalEntityName.trim() === '' ||
		!mapping.wfeEntityType ||
		mapping.wfeEntityType.trim() === ''
	);
}

/**
 * 统一保存所有实体映射
 */
async function handleSaveEntityMapping() {
	// 过滤掉空行数据
	const validMappings = localEntityMappings.value.filter((mapping) => !isEmptyRow(mapping));

	if (validMappings.length === 0) {
		ElMessage.warning('Please add at least one valid entity mapping');
		return;
	}

	// 验证每一行数据
	for (let i = 0; i < validMappings.length; i++) {
		const mapping = validMappings[i];
		if (!mapping.externalEntityName || mapping.externalEntityName.trim() === '') {
			ElMessage.warning(`Row ${i + 1}: Please enter external entity type name`);
			return;
		}
		if (!mapping.wfeEntityType || mapping.wfeEntityType.trim() === '') {
			ElMessage.warning(`Row ${i + 1}: Please select WFE master data`);
			return;
		}
	}

	saving.value = true;

	try {
		// 准备批量保存的数据
		const items = validMappings.map((mapping) => ({
			id: mapping.id, // 如果有id则为更新，否则为新增
			integrationId: props.integrationId,
			externalEntityName: mapping.externalEntityName.trim(),
			externalEntityType: mapping.externalEntityType || mapping.externalEntityName.trim(),
			wfeEntityType: mapping.wfeEntityType,
			workflowIds: mapping.workflowIds || [],
			isActive: mapping.isActive !== false,
		}));

		// 调用批量保存接口
		const res = await createEntityMapping({
			integrationId: props.integrationId as string,
			items,
		});

		if (res.code == '200') {
			ElMessage.success('Entity mappings saved successfully');
			// 通知父组件刷新数据
			emit('refresh');
		} else {
			ElMessage.error(res.msg || 'Failed to save entity mappings');
		}
	} catch (error) {
		console.error('Failed to save entity mappings:', error);
		ElMessage.error('Failed to save entity mappings');
	} finally {
		saving.value = false;
	}
}

/**
 * 删除实体映射（仅从本地列表移除，不立即调用API）
 */
function handleDeleteEntityMapping(_mapping: IEntityMapping, index: number) {
	localEntityMappings.value.splice(index, 1);
}

// 监听 props 变化，同步到本地编辑状态
watch(
	() => props.entityMappings,
	(newMappings) => {
		if (newMappings) {
			localEntityMappings.value = [...newMappings];
		}
	},
	{ immediate: true, deep: true }
);
</script>
