<template>
	<div class="space-y-8">
		<!-- Receive External Data -->
		<div class="space-y-4">
			<div class="flex justify-between items-center">
				<div>
					<h3 class="text-lg font-semibold text-text-primary m-0">
						Receive External Data
					</h3>
					<p class="text-sm text-text-secondary mt-1">
						Map entity types from BNP to WFE master data
					</p>
				</div>
			</div>

			<el-table
				:data="entityMappings"
				class="w-full"
				empty-text="No entity mappings configured"
			>
				<el-table-column label="BNP Entity" prop="crmEntity" width="200">
					<template #default="{ row }">
						<span class="font-medium text-sm">{{ row.crmEntity }}</span>
					</template>
				</el-table-column>

				<el-table-column label="Map to WFE" prop="wfeEntity" width="200">
					<template #default="{ row }">
						<el-select
							v-model="row.wfeEntity"
							placeholder="Not mapped"
							@change="handleEntityMappingChange"
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

				<el-table-column label="Available Workflows" prop="workflows" min-width="250">
					<template #default="{ row }">
						<el-select
							v-model="row.workflows"
							multiple
							placeholder="Select workflows"
							collapse-tags
							@change="handleEntityMappingChange"
						>
							<el-option
								v-for="workflow in workflowOptions"
								:key="workflow.id"
								:label="workflow.name"
								:value="workflow.id"
							/>
						</el-select>
					</template>
				</el-table-column>
			</el-table>
		</div>

		<!-- Field Mapping -->
		<div class="space-y-4">
			<div class="flex justify-between items-center">
				<div>
					<h3 class="text-lg font-semibold text-text-primary m-0">Field Mapping</h3>
					<p class="text-sm text-text-secondary mt-1">
						Map fields from BNP to WFE dynamic fields
					</p>
				</div>
			</div>

			<el-table
				:data="fieldMappings"
				class="w-full"
				empty-text="No field mappings configured"
			>
				<el-table-column label="BNP Field" prop="crmField" width="180">
					<template #default="{ row }">
						<span class="font-medium text-sm">{{ row.crmField }}</span>
					</template>
				</el-table-column>

				<el-table-column label="WFE Field" prop="wfeField" width="180">
					<template #default="{ row }">
						<el-select
							v-model="row.wfeField"
							placeholder="Select or create field..."
							allow-create
							filterable
							@change="handleFieldMappingChange"
						>
							<el-option
								v-for="field in wfeFieldOptions"
								:key="field.value"
								:label="field.label"
								:value="field.value"
							/>
						</el-select>
					</template>
				</el-table-column>

				<el-table-column label="Type" prop="type" width="120">
					<template #default="{ row }">
						<span class="text-sm text-text-secondary">{{ row.type }}</span>
					</template>
				</el-table-column>

				<el-table-column label="Sync Direction" prop="syncDirection" width="150">
					<template #default="{ row }">
						<el-select
							v-model="row.syncDirection"
							@change="handleSyncDirectionChange(row)"
						>
							<el-option label="View Only" value="view_only" />
							<el-option label="Editable" value="editable" />
						</el-select>
					</template>
				</el-table-column>

				<el-table-column label="Available Workflows" prop="workflows" min-width="200">
					<template #default="{ row }">
						<el-select
							v-model="row.workflows"
							multiple
							placeholder="Select workflows"
							collapse-tags
							@change="handleFieldMappingChange"
						>
							<el-option
								v-for="workflow in workflowOptions"
								:key="workflow.id"
								:label="workflow.name"
								:value="workflow.id"
							/>
						</el-select>
					</template>
				</el-table-column>

				<el-table-column label="Actions" width="100" align="center">
					<template #default="{ $index }">
						<el-button type="danger" text @click="handleDeleteFieldMapping($index)">
							<el-icon><Delete /></el-icon>
						</el-button>
					</template>
				</el-table-column>
			</el-table>
		</div>

		<!-- Attachment Sharing -->
		<div class="space-y-4">
			<div class="flex justify-between items-center">
				<div>
					<h3 class="text-lg font-semibold text-text-primary m-0">Attachment Sharing</h3>
					<p class="text-sm text-text-secondary mt-1">
						Configure which attachments from BNP modules to receive
					</p>
				</div>
			</div>

			<el-table
				:data="attachmentSharing"
				class="w-full"
				empty-text="No attachment sharing configured"
			>
				<el-table-column label="Module" prop="module" width="200">
					<template #default="{ row }">
						<span class="font-medium text-sm">{{ row.module }}</span>
					</template>
				</el-table-column>

				<el-table-column label="Available Workflows" prop="workflows" min-width="300">
					<template #default="{ row }">
						<el-select
							v-model="row.workflows"
							multiple
							placeholder="Select workflows to receive attachments..."
							collapse-tags
							@change="handleAttachmentSharingChange"
						>
							<el-option
								v-for="workflow in workflowOptions"
								:key="workflow.id"
								:label="workflow.name"
								:value="workflow.id"
							/>
						</el-select>
					</template>
				</el-table-column>
			</el-table>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { Delete } from '@element-plus/icons-vue';
import { ElMessage } from 'element-plus';
import type {
	IIntegrationConfig,
	IEntityMapping,
	IFieldMapping,
	IAttachmentSharing,
	IWorkflowOption,
	IWfeEntityOption,
	IFieldOption,
} from '#/integration';

interface Props {
	integration: IIntegrationConfig;
}

interface Emits {
	(e: 'update', data: Partial<IIntegrationConfig>): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

// 本地数据
const entityMappings = ref<IEntityMapping[]>([
	...(props.integration.inboundSettings?.entityMappings || []),
]);

const fieldMappings = ref<IFieldMapping[]>([
	...(props.integration.inboundSettings?.fieldMappings || []),
]);

const attachmentSharing = ref<IAttachmentSharing[]>([
	...(props.integration.inboundSettings?.attachmentSharing || []),
]);

// 模拟选项数据（实际应该从 API 获取）
const wfeEntityOptions = ref<IWfeEntityOption[]>([
	{ value: 'case', label: 'Case' },
	{ value: 'customer', label: 'Customer' },
	{ value: 'lead', label: 'Lead' },
	{ value: 'contact', label: 'Contact' },
]);

const wfeFieldOptions = ref<IFieldOption[]>([
	{ value: 'name', label: 'Name', type: 'text' },
	{ value: 'email', label: 'Email', type: 'email' },
	{ value: 'phone', label: 'Phone', type: 'phone' },
	{ value: 'status', label: 'Status', type: 'select' },
	{ value: 'description', label: 'Description', type: 'textarea' },
]);

const workflowOptions = ref<IWorkflowOption[]>([
	{ id: 'wf-1', name: 'Onboarding Workflow' },
	{ id: 'wf-2', name: 'Customer Service Workflow' },
	{ id: 'wf-3', name: 'Sales Workflow' },
]);

/**
 * 实体映射变更
 */
function handleEntityMappingChange() {
	emitUpdate();
}

/**
 * 删除字段映射
 */
function handleDeleteFieldMapping(index: number) {
	fieldMappings.value.splice(index, 1);
	emitUpdate();
}

/**
 * 字段映射变更
 */
function handleFieldMappingChange() {
	emitUpdate();
}

/**
 * 同步方向变更
 */
function handleSyncDirectionChange(row: IFieldMapping) {
	if (row.syncDirection === 'editable') {
		ElMessage.info(
			'This field will be automatically added to Outbound Settings → Fields to Share'
		);
	}
	emitUpdate();
}

/**
 * 附件共享变更
 */
function handleAttachmentSharingChange() {
	emitUpdate();
}

/**
 * 发送更新事件
 */
function emitUpdate() {
	emit('update', {
		inboundSettings: {
			entityMappings: entityMappings.value,
			fieldMappings: fieldMappings.value,
			attachmentSharing: attachmentSharing.value,
		},
	});
}

// 监听 props 变化
watch(
	() => props.integration.inboundSettings,
	(newSettings) => {
		if (newSettings) {
			entityMappings.value = [...(newSettings.entityMappings || [])];
			fieldMappings.value = [...(newSettings.fieldMappings || [])];
			attachmentSharing.value = [...(newSettings.attachmentSharing || [])];
		}
	},
	{ deep: true }
);
</script>

<style scoped lang="scss">
// Element Plus 表格样式覆盖
:deep(.el-table) {
	background: var(--el-bg-color-overlay);
	border: none;

	th {
		background: var(--el-fill-color);
		color: var(--el-text-color-primary);
		font-weight: 600;
		font-size: 13px;
		border-color: var(--el-border-color-lighter);
	}

	tr {
		background: var(--el-bg-color-overlay);
		border-color: var(--el-border-color-lighter);
	}

	td {
		border-color: var(--el-border-color-lighter);
	}

	.el-select {
		width: 100%;
	}

	.el-input {
		width: 100%;
	}

	.el-input__wrapper {
		background: var(--el-bg-color);
		border-color: var(--el-border-color-lighter);
	}
}
</style>
