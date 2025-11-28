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
			:data="entityMappings"
			class="w-full"
			:border="true"
			empty-text="No entity types configured"
		>
			<el-table-column label="External Entity Type" prop="externalEntityName" min-width="250">
				<template #default="{ row }">
					<el-input
						v-model="row.externalEntityName"
						placeholder="Enter external entity type name"
						:disabled="!!row?.systemId"
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
				min-width="200"
				align="center"
			>
				<template #default="{ row }">
					<el-select
						:disabled="!row.externalEntityName || !!row?.systemId"
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
					<el-select
						:disabled="!row.externalEntityName || !!row?.systemId"
						v-model="row.workflowIds"
						placeholder="Select workflows"
						multiple
						collapse-tags
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

			<el-table-column label="" width="50" fixed="right">
				<template #default="{ row, $index }">
					<div class="flex items-center justify-center">
						<el-button
							v-if="!!row?.systemId || !!row?.id"
							type="danger"
							link
							:icon="Delete"
							:disabled="!row?.id"
							@click="handleDeleteEntityMapping(row, $index)"
						/>
						<el-button
							v-else
							type="primary"
							:disabled="!row.externalEntityName || !row.wfeEntityType"
							:loading="savingMappingId === (row.id || `temp-${$index}`)"
							@click="handleSaveEntityMapping(row, $index)"
							link
							:icon="SaveChangeIcon"
						/>
					</div>
				</template>
			</el-table-column>
		</el-table>

		<!-- Add Entity Type 按钮 -->
		<div class="flex items-center">
			<el-button
				type="default"
				:icon="Plus"
				@click="handleAddEntityMapping"
				class="add-entity-btn"
			>
				Add Entity Type
			</el-button>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { Delete, Plus } from '@element-plus/icons-vue';
import { ElMessage } from 'element-plus';
import { createEntityMapping, updateEntityMapping, deleteEntityMapping } from '@/apis/integration';
import type { IEntityMapping, IWfeEntityOption } from '#/integration';
import SaveChangeIcon from '@assets/svg/publicPage/saveChange.svg';

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
const savingMappingId = ref<string | number | null>(null);

// 实体映射数据
const entityMappings = ref<IEntityMapping[]>(props.entityMappings || []);

// 模拟选项数据（实际应该从 API 获取）
const wfeEntityOptions = ref<IWfeEntityOption[]>([{ value: 'case', label: 'Case' }]);

/**
 * 添加实体映射
 */
function handleAddEntityMapping() {
	entityMappings.value.push({
		externalEntityName: '',
		externalEntityType: '',
		wfeEntityType: '',
		workflowIds: [],
		isActive: true,
	});
}

/**
 * 保存实体映射
 */
async function handleSaveEntityMapping(mapping: IEntityMapping, index: number) {
	if (!mapping.externalEntityName || mapping.externalEntityName.trim() === '') {
		ElMessage.warning('Please enter external entity type name');
		return;
	}

	if (!mapping.wfeEntityType || mapping.wfeEntityType.trim() === '') {
		ElMessage.warning('Please select WFE master data');
		return;
	}

	savingMappingId.value = mapping.id || `temp-${index}`;

	try {
		const mappingData = {
			integrationId: props.integrationId,
			externalEntityName: mapping.externalEntityName.trim(),
			externalEntityType: mapping.externalEntityType || mapping.externalEntityName.trim(),
			wfeEntityType: mapping.wfeEntityType,
			workflowIds: mapping.workflowIds || [],
			isActive: mapping.isActive !== false,
		};

		let res;
		if (mapping.id) {
			// 更新现有映射
			res = await updateEntityMapping(mapping.id, mappingData);
		} else {
			// 创建新映射
			res = await createEntityMapping(mappingData);
		}

		if (res.success) {
			ElMessage.success(
				mapping.id
					? 'Entity mapping updated successfully'
					: 'Entity mapping created successfully'
			);
			// 如果是新创建的，更新本地数据中的 id
			if (!mapping.id && res.data) {
				entityMappings.value[index].id = res.data;
			}
			// 通知父组件刷新数据
			emit('refresh');
		} else {
			ElMessage.error(res.msg || 'Failed to save entity mapping');
		}
	} catch (error) {
		console.error('Failed to save entity mapping:', error);
		ElMessage.error('Failed to save entity mapping');
	} finally {
		savingMappingId.value = null;
	}
}

/**
 * 删除实体映射
 */
async function handleDeleteEntityMapping(mapping: IEntityMapping, index: number) {
	if (!mapping.id) {
		// 如果是未保存的新记录，直接删除
		entityMappings.value.splice(index, 1);
		return;
	}

	try {
		const res = await deleteEntityMapping(mapping.id);
		if (res.success) {
			ElMessage.success('Entity mapping deleted successfully');
			entityMappings.value.splice(index, 1);
			// 通知父组件刷新数据
			emit('refresh');
		} else {
			ElMessage.error(res.msg || 'Failed to delete entity mapping');
		}
	} catch (error) {
		console.error('Failed to delete entity mapping:', error);
		ElMessage.error('Failed to delete entity mapping');
	}
}

// 监听 props 变化
watch(
	() => props.entityMappings,
	(newMappings) => {
		if (newMappings) {
			entityMappings.value = [...newMappings];
		}
	},
	{ immediate: true, deep: true }
);
</script>
