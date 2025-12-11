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
				v-loading="isLoading"
				:data="filteredFieldMappings"
				class="w-full"
				empty-text="No field mappings configured"
				:border="true"
			>
				<el-table-column label="Action ID" prop="actionCode	" width="120">
					<template #default="{ row }">
						<span class="font-medium text-sm action-id">{{ row.actionCode }}</span>
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
					prop="wfeFieldName"
					min-width="220"
				>
					<template #default="{ row }">
						<span class="text-sm">{{ row.wfeFieldName || '-' }}</span>
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
				<el-button
					type="primary"
					link
					:icon="Document"
					:loading="loading"
					@click="showMdDialog"
				>
					API Docs
				</el-button>
			</div>

			<el-table
				v-loading="isLoading"
				:data="localAttachmentSharing"
				class="w-full"
				empty-text="No attachment sharing configured"
				:border="true"
			>
				<el-table-column label="External Module" min-width="200">
					<template #default="{ row }">
						<span v-if="isSaved(row)" class="font-medium text-sm">
							{{ row.moduleName || defaultStr }}
						</span>
						<el-input
							v-else
							v-model="row.moduleName"
							placeholder="Enter external module name"
						/>
					</template>
				</el-table-column>

				<el-table-column label="Workflow" min-width="250">
					<template #default="{ row }">
						<span v-if="isSaved(row)" class="text-sm">
							{{ getWorkflowName(row.workflowId) || defaultStr }}
						</span>
						<el-select v-else v-model="row.workflowId" placeholder="Select workflow...">
							<el-option
								v-for="workflow in workflows"
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

				<el-table-column label="Action" min-width="250">
					<template #default="{ row }">
						<span v-if="isSaved(row)" class="text-sm">
							{{ getActionName(row.actionId) || defaultStr }}
						</span>
						<el-select
							v-else
							v-model="row.actionId"
							placeholder="Select action"
							:disabled="!row.workflowId"
						>
							<el-option
								v-for="action in actions || []"
								:key="action.id"
								:label="action.name"
								:value="String(action.id)"
							/>
						</el-select>
					</template>
				</el-table-column>

				<el-table-column
					label="Actions"
					width="80"
					align="center"
					v-if="ProjectPermissionEnum.integration.delete"
				>
					<template #default="{ $index }">
						<div class="flex items-center justify-center gap-1">
							<el-button
								type="danger"
								link
								@click="handleDeleteModule($index)"
								:icon="Delete"
							/>
						</div>
					</template>
				</el-table-column>
			</el-table>

			<div class="flex justify-start justify-between">
				<el-button
					@click="handleAddModule"
					v-permission="ProjectPermissionEnum.integration.create"
					:icon="Plus"
				>
					Add Module
				</el-button>
				<el-button type="primary" :loading="saving" @click="handleSaveModule">
					Save
				</el-button>
			</div>
		</div>

		<!-- API Documentation Dialog -->
		<el-dialog
			v-model="showApiDocDialog"
			title="API Docs"
			:width="bigDialogWidth"
			:close-on-click-modal="false"
			append-to-body
			draggable
		>
			<el-scrollbar max-height="70vh">
				<MarkdownRenderer :content="attachmentApiMd" />
			</el-scrollbar>
			<template #footer>
				<el-button
					type="primary"
					:icon="DocumentCopy"
					@click="copyApiDoc"
					:loading="isCopying"
				>
					Copy
				</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import { Delete, Search, Plus, Document, DocumentCopy } from '@element-plus/icons-vue';
import { ElMessage } from 'element-plus';
import { getInboundSettingsAttachment, createInboundSettingsAttachment } from '@/apis/integration';
import type { FieldMapping, InboundAttachmentIteml } from '#/integration';
import { defaultStr, bigDialogWidth } from '@/settings/projectSetting';
import MarkdownRenderer from '@/components/common/MarkdownRenderer.vue';
import { ProjectPermissionEnum } from '@/enums/permissionEnum';

interface Props {
	integrationId: string;
	workflows?: any[];
	inboundFieldMappings?: FieldMapping[];
	attachmentApiMd: string;
	loading?: boolean;
	actions: {
		actionCode: string;
		name: string;
		id: string;
	}[];
}

interface Emits {
	(e: 'refresh'): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

// 搜索过滤
const externalFieldSearch = ref('');
const wfeFieldSearch = ref('');

// 本地数据（本地编辑状态）
const localAttachmentSharing = ref<InboundAttachmentIteml[]>([]);
const isLoading = ref(false);
const saving = ref(false);

/**
 * 过滤后的字段映射列表
 */
const filteredFieldMappings = computed(() => {
	let filtered = [...(props.inboundFieldMappings || [])];

	if (externalFieldSearch.value) {
		const search = externalFieldSearch.value.toLowerCase();
		filtered = filtered.filter(
			(item) => item.externalFieldName?.toLowerCase().includes(search)
		);
	}

	if (wfeFieldSearch.value) {
		const search = wfeFieldSearch.value.toLowerCase();
		filtered = filtered.filter((item) => item.wfeFieldName?.toLowerCase().includes(search));
	}

	return filtered;
});

/**
 * 判断是否已保存
 */
function isSaved(row: InboundAttachmentIteml): boolean {
	return !!row?.id;
}

/**
 * 判断是否为空行（未填写必填字段）
 */
function isEmptyRow(item: InboundAttachmentIteml): boolean {
	return !item.moduleName || item.moduleName.trim() === '' || !item.workflowId || !item.actionId;
}

/**
 * 添加模块
 */
function handleAddModule() {
	localAttachmentSharing.value.push({
		moduleName: '',
		workflowId: '',
		actionId: '',
	});
}

/**
 * 删除模块（仅从本地列表移除，不立即调用API）
 */
function handleDeleteModule(index: number) {
	localAttachmentSharing.value.splice(index, 1);
}

/**
 * 统一保存所有附件共享配置
 */
async function handleSaveModule() {
	// 过滤掉空行数据
	const validItems = localAttachmentSharing.value.filter((item) => !isEmptyRow(item));

	if (validItems.length === 0) {
		ElMessage.warning('Please add at least one valid module configuration');
		return;
	}

	// 验证每一行数据
	for (let i = 0; i < validItems.length; i++) {
		const item = validItems[i];
		if (!item.moduleName || item.moduleName.trim() === '') {
			ElMessage.warning(`Row ${i + 1}: Please enter module name`);
			return;
		}
		if (!item.workflowId) {
			ElMessage.warning(`Row ${i + 1}: Please select workflow`);
			return;
		}
		if (!item.actionId) {
			ElMessage.warning(`Row ${i + 1}: Please select action`);
			return;
		}
	}

	if (!props.integrationId || props.integrationId === 'new') {
		ElMessage.warning('Please save the integration first');
		return;
	}

	saving.value = true;

	try {
		// 准备批量保存的数据
		const items = validItems.map((item) => ({
			id: item.id, // 如果有id则为更新，否则为新增
			integrationId: props.integrationId,
			moduleName: item.moduleName.trim(),
			workflowId: item.workflowId,
			actionId: item.actionId,
		}));

		// 调用批量保存接口
		const res = await createInboundSettingsAttachment({
			integrationId: props.integrationId,
			items,
		});

		if (res.code == '200') {
			ElMessage.success('Attachment sharing configurations saved successfully');
			// 通知父组件刷新数据
			emit('refresh');
			loadAttachmentSharing();
		} else {
			ElMessage.error(res.msg || 'Failed to save configurations');
		}
	} catch (error) {
		console.error('Failed to save configurations:', error);
		ElMessage.error('Failed to save configurations');
	} finally {
		saving.value = false;
	}
}

/**
 * 获取 Workflow 名称
 */
function getWorkflowName(workflowId: string | number | undefined): string {
	if (!workflowId || !props.workflows) return '';
	const workflow = props.workflows.find((w) => String(w.id) === String(workflowId));
	return workflow?.name || '';
}

/**
 * 获取 Action 名称
 */
function getActionName(actionId: string | number | undefined): string {
	if (!actionId || !props.actions) return '';
	const action = props.actions.find((a) => String(a.id) === String(actionId));
	return action?.name || '';
}

const showApiDocDialog = ref(false);
const isCopying = ref(false);
const showMdDialog = () => {
	if (props.attachmentApiMd) {
		showApiDocDialog.value = true;
	} else {
		ElMessage.warning('No API documentation available');
	}
};

/**
 * 复制 API 文档内容
 */
async function copyApiDoc() {
	if (!props.attachmentApiMd) {
		ElMessage.warning('No content to copy');
		return;
	}

	isCopying.value = true;
	try {
		await navigator.clipboard.writeText(props.attachmentApiMd);
		ElMessage.success('API documentation copied to clipboard');
	} finally {
		isCopying.value = false;
	}
}

/**
 * 加载附件共享数据
 */
async function loadAttachmentSharing() {
	if (!props.integrationId || props.integrationId === 'new') {
		localAttachmentSharing.value = [];
		return;
	}

	isLoading.value = true;
	try {
		const response = await getInboundSettingsAttachment(props.integrationId);
		if (response.code == '200') {
			localAttachmentSharing.value = response?.data || [];
		} else {
			localAttachmentSharing.value = [];
		}
	} catch (error) {
		console.error('Failed to load attachment sharing:', error);
		localAttachmentSharing.value = [];
	} finally {
		isLoading.value = false;
	}
}

// 监听 integrationId 变化（包括初始化）
watch(
	() => props.integrationId,
	(newId) => {
		if (newId && newId !== 'new') {
			loadAttachmentSharing();
		} else {
			// 如果是新建或无效 ID，清空数据
			localAttachmentSharing.value = [];
		}
	},
	{ immediate: true }
);
</script>
