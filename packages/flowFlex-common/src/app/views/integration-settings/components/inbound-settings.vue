<template>
	<div class="space-y-8">
		<!-- Field Mapping -->
		<div class="space-y-4">
			<div class="flex justify-between items-center">
				<div>
					<h3 class="text-lg font-semibold text-text-primary m-0">Field Mapping</h3>
					<p class="text-sm text-text-secondary mt-1">
						Fields brought in by inbound actions (read-only view).
					</p>
				</div>
			</div>

			<!-- Search Filters -->
			<div class="flex gap-4">
				<el-input
					v-model="externalFieldSearch"
					placeholder="Filter by external field name..."
					clearable
					class="flex-1"
				>
					<template #prefix>
						<el-icon><Search /></el-icon>
					</template>
				</el-input>
				<el-input
					v-model="wfeFieldSearch"
					placeholder="Filter by WFE field name..."
					clearable
					class="flex-1"
				>
					<template #prefix>
						<el-icon><Search /></el-icon>
					</template>
				</el-input>
			</div>

			<el-table
				:data="filteredFieldMappings"
				class="w-full"
				empty-text="No field mappings configured"
				:border="true"
			>
				<el-table-column label="Action ID" prop="actionId" width="150">
					<template #default="{ row }">
						<span class="font-medium text-sm action-id">{{ row.actionId }}</span>
					</template>
				</el-table-column>

				<el-table-column label="Action Name" prop="actionName" min-width="200">
					<template #default="{ row }">
						<span class="text-sm">{{ row.actionName }}</span>
					</template>
				</el-table-column>

				<el-table-column
					label="External Field (API Name)"
					prop="externalFieldName"
					min-width="220"
				>
					<template #default="{ row }">
						<span class="font-medium text-sm">{{ row.externalFieldName }}</span>
					</template>
				</el-table-column>

				<el-table-column
					label="WFE Field (Display Name)"
					prop="wfeFieldDisplayName"
					min-width="220"
				>
					<template #default="{ row }">
						<span class="text-sm">{{ row.wfeFieldDisplayName || '-' }}</span>
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
						Configure which attachments to receive from external system modules.
					</p>
				</div>
			</div>

			<el-table
				:data="attachmentSharing"
				class="w-full"
				empty-text="No attachment sharing configured"
				:border="true"
			>
				<el-table-column label="External Module" min-width="200">
					<template #default="{ row }">
						<el-input
							v-if="row.isEditing"
							v-model="row.module"
							placeholder="Enter external module name"
							@blur="handleModuleBlur(row)"
						/>
						<span v-else class="font-medium text-sm">
							{{ row.module || 'Enter external module name' }}
						</span>
					</template>
				</el-table-column>

				<el-table-column label="System ID" width="200">
					<template #default="{ row }">
						<span class="text-sm text-text-secondary">
							{{ row.systemId || 'Auto-generated on save' }}
						</span>
					</template>
				</el-table-column>

				<el-table-column label="Available in Workflows" min-width="250">
					<template #default="{ row }">
						<el-select
							v-model="row.workflows"
							multiple
							placeholder="Select workflows..."
							collapse-tags
							@change="handleAttachmentSharingChange"
						>
							<el-option
								v-for="workflow in workflowOptions"
								:key="workflow.workflowId"
								:label="workflow.workflowName"
								:value="String(workflow.workflowId)"
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

				<el-table-column label="Actions" width="180" align="center">
					<template #default="{ row, $index }">
						<div class="flex items-center justify-center gap-1">
							<el-button
								v-if="row.isEditing || !row.id"
								type="primary"
								@click="handleSaveModule(row, $index)"
							>
								Save
							</el-button>
							<el-button type="danger" link @click="handleDeleteModule($index)">
								<el-icon><Delete /></el-icon>
							</el-button>
						</div>
					</template>
				</el-table-column>
			</el-table>

			<div class="flex justify-start">
				<el-button type="primary" @click="handleAddModule">
					<el-icon><Plus /></el-icon>
					Add Module
				</el-button>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import { Delete, Search, Plus } from '@element-plus/icons-vue';
import { ElMessage } from 'element-plus';
import type { IFieldMapping, IAttachmentSharing } from '#/integration';

interface Props {
	integrationId: string | number;
	inboundSettings?: {
		fieldMappings?: IFieldMapping[];
		attachmentSharing?: IAttachmentSharing[];
	};
	workflows?: any[];
}

// 字段映射显示类型（包含 action 信息）
interface IFieldMappingDisplay extends IFieldMapping {
	actionId?: string | number;
	actionName?: string;
	wfeFieldDisplayName?: string;
}

// 附件共享扩展类型
interface IAttachmentSharingExtended extends IAttachmentSharing {
	systemId?: string;
	isEditing?: boolean;
}

const props = defineProps<Props>();

// 搜索过滤
const externalFieldSearch = ref('');
const wfeFieldSearch = ref('');

// 本地数据
const fieldMappings = ref<IFieldMappingDisplay[]>([]);
const attachmentSharing = ref<IAttachmentSharingExtended[]>([]);

/**
 * 将 workflows 转换为 IWorkflowOption 格式
 */
const workflowOptions = computed(() => {
	return (props.workflows || []).map((workflow) => ({
		workflowId: workflow.id,
		workflowName: workflow.name,
		isActive: workflow.isActive !== false,
		id: workflow.id,
		name: workflow.name,
	}));
});

// WFE 字段显示名称映射（实际应该从 API 获取）
const wfeFieldDisplayNameMap: Record<string | number, string> = {
	name: 'Name',
	email: 'Email Address',
	phone: 'Phone Number',
	status: 'Status',
	description: 'Description',
	customer_id: 'Customer Id',
	email_address: 'Email Address',
	phone_number: 'Phone Number',
	company_name: 'Company Name',
	lead_id: 'Lead Id',
	first_name: 'First Name',
	last_name: 'Last Name',
	lead_source: 'Lead Source',
};

// Action 信息映射（实际应该从 API 获取）
const actionInfoMap: Record<string | number, { id: string; name: string }> = {
	'ACT-001': { id: 'ACT-001', name: 'Import Customer Data' },
	'ACT-002': { id: 'ACT-002', name: 'Sync Lead Information' },
};

/**
 * 过滤后的字段映射列表
 */
const filteredFieldMappings = computed(() => {
	let filtered = [...fieldMappings.value];

	if (externalFieldSearch.value) {
		const search = externalFieldSearch.value.toLowerCase();
		filtered = filtered.filter(
			(item) => item.externalFieldName?.toLowerCase().includes(search)
		);
	}

	if (wfeFieldSearch.value) {
		const search = wfeFieldSearch.value.toLowerCase();
		filtered = filtered.filter(
			(item) => item.wfeFieldDisplayName?.toLowerCase().includes(search)
		);
	}

	return filtered;
});

/**
 * 初始化字段映射数据（添加 action 信息和 WFE 字段显示名称）
 */
function initializeFieldMappings() {
	const mappings = props.inboundSettings?.fieldMappings || [];
	fieldMappings.value = mappings.map((mapping) => {
		// 根据 entityMappingId 或其他逻辑确定 actionId（这里使用模拟数据）
		const actionId = mapping.entityMappingId || 'ACT-001';
		const actionInfo = actionInfoMap[actionId] || { id: actionId, name: 'Unknown Action' };

		return {
			...mapping,
			actionId: actionInfo.id,
			actionName: actionInfo.name,
			wfeFieldDisplayName: mapping.wfeFieldId
				? wfeFieldDisplayNameMap[mapping.wfeFieldId] || String(mapping.wfeFieldId)
				: undefined,
		};
	});
}

/**
 * 初始化附件共享数据
 */
function initializeAttachmentSharing() {
	const sharing = props.inboundSettings?.attachmentSharing || [];
	attachmentSharing.value = sharing.map((item) => ({
		...item,
		systemId: item.id ? String(item.id) : undefined,
		isEditing: false,
	}));
}

/**
 * 添加模块
 */
function handleAddModule() {
	attachmentSharing.value.push({
		module: '',
		workflows: [],
		isEditing: true,
	});
}

/**
 * 模块输入框失焦
 */
function handleModuleBlur(row: IAttachmentSharingExtended) {
	if (!row.module || row.module.trim() === '') {
		// 如果为空，保持编辑状态
		return;
	}
	// 可以在这里添加验证逻辑
}

/**
 * 附件共享变更
 */
function handleAttachmentSharingChange() {
	// 数据变更处理（不自动保存）
}

/**
 * 保存模块
 */
function handleSaveModule(row: IAttachmentSharingExtended, index: number) {
	if (!row.module || row.module.trim() === '') {
		ElMessage.warning('Please enter a module name');
		return;
	}

	row.isEditing = false;
	// 如果是新模块，生成 systemId（实际应该由后端生成）
	if (!row.systemId) {
		row.systemId = `SYS-${Date.now()}`;
	}
}

/**
 * 删除模块
 */
function handleDeleteModule(index: number) {
	attachmentSharing.value.splice(index, 1);
}

/**
 * 上移模块
 */

// 监听 props 变化
watch(
	() => props.inboundSettings,
	(newSettings) => {
		if (newSettings) {
			initializeFieldMappings();
			initializeAttachmentSharing();
		}
	},
	{ immediate: true, deep: true }
);
</script>
