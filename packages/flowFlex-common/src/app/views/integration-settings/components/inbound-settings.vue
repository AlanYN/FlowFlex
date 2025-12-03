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
			</div>

			<el-table
				v-loading="isLoading"
				:data="attachmentSharing"
				class="w-full"
				empty-text="No attachment sharing configured"
				:border="true"
			>
				<el-table-column label="External Module" min-width="200">
					<template #default="{ row }">
						<el-input
							v-if="!row.id && row.isEditing"
							v-model="row.moduleName"
							placeholder="Enter external module name"
							@blur="handleModuleBlur(row)"
						/>
						<span v-else class="font-medium text-sm">
							{{ row.moduleName || defaultStr }}
						</span>
					</template>
				</el-table-column>

				<el-table-column label="Workflow" min-width="250">
					<template #default="{ row }">
						<span v-if="row.id" class="text-sm">
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
						<span v-if="row.id" class="text-sm">
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

				<el-table-column label="Actions" width="80" align="center">
					<template #default="{ row, $index }">
						<div class="flex items-center justify-center gap-1">
							<el-button
								v-if="row.isEditing || !row.id"
								type="primary"
								:loading="isSaving"
								@click="handleSaveModule(row, $index)"
								:icon="SaveChangeIcon"
								link
							/>
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
import { ElMessage, ElMessageBox } from 'element-plus';
import {
	getInboundSettingsAttachment,
	createInboundSettingsAttachment,
	deleteInboundSettingsAttachment,
} from '@/apis/integration';
import type { FieldMapping, InboundAttachmentIteml } from '#/integration';
import SaveChangeIcon from '@assets/svg/publicPage/saveChange.svg';
import { defaultStr } from '@/settings/projectSetting';

interface Props {
	integrationId: string | number;
	workflows?: any[];
	inboundFieldMappings?: FieldMapping[];
	actions: {
		actionCode: string;
		name: string;
		id: string;
	}[];
}

// 附件共享扩展类型
interface IAttachmentSharingExtended extends InboundAttachmentIteml {
	systemId?: string;
	isEditing?: boolean;
}

const props = defineProps<Props>();

// 搜索过滤
const externalFieldSearch = ref('');
const wfeFieldSearch = ref('');

// 本地数据
const attachmentSharing = ref<IAttachmentSharingExtended[]>([]);
const isLoading = ref(false);
const isSaving = ref(false);

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
 * 加载附件共享数据
 */
async function loadAttachmentSharing() {
	if (!props.integrationId || props.integrationId === 'new') {
		attachmentSharing.value = [];
		return;
	}

	isLoading.value = true;
	try {
		const response = await getInboundSettingsAttachment(props.integrationId);
		if (response.success && response.data) {
			// 处理返回的附件共享数据
			const data = response.data;
			// 如果返回的是数组，直接使用；如果是对象，取 attachmentSharing 字段
			const attachmentList = Array.isArray(data) ? data : data.attachmentSharing || [];
			initializeAttachmentSharing(attachmentList);
		} else {
			attachmentSharing.value = [];
		}
	} catch (error) {
		console.error('Failed to load attachment sharing:', error);
		attachmentSharing.value = [];
	} finally {
		isLoading.value = false;
	}
}

/**
 * 初始化附件共享数据
 */
async function initializeAttachmentSharing(sharing?: InboundAttachmentIteml[]) {
	const sharingList = sharing || [];
	attachmentSharing.value = sharingList.map((item) => ({
		...item,
		systemId: item.id ? String(item.id) : undefined,
		isEditing: false,
	}));

	// 加载所有有 workflowId 的 stages（用于回显）
	const workflowIds = new Set<string>();
	sharingList.forEach((item) => {
		if (item.workflowId && item.workflowId !== '0') {
			workflowIds.add(String(item.workflowId));
		}
	});
}

/**
 * 添加模块
 */
function handleAddModule() {
	attachmentSharing.value.push({
		id: '',
		moduleName: '',
		workflowId: '',
		actionId: '',
		isEditing: true,
	});
}

/**
 * 模块输入框失焦
 */
function handleModuleBlur(row: IAttachmentSharingExtended) {
	if (!row.moduleName || row.moduleName.trim() === '') {
		// 如果为空，保持编辑状态
		return;
	}
	// 可以在这里添加验证逻辑
}

/**
 * 保存模块
 */
async function handleSaveModule(row: IAttachmentSharingExtended, index: number) {
	if (!row.moduleName || row.moduleName.trim() === '') {
		ElMessage.warning('Please enter a module name');
		return;
	}

	if (!props.integrationId || props.integrationId === 'new') {
		ElMessage.warning('Please save the integration first');
		return;
	}

	isSaving.value = true;
	try {
		// 将 IAttachmentSharingExtended 转换为 IInboundConfiguration
		const configData: InboundAttachmentIteml = {
			integrationId: String(props.integrationId),
			moduleName: row.moduleName.trim(),
			workflowId: row.workflowId,
			actionId: row.actionId,
		};

		const response = await createInboundSettingsAttachment(configData);
		if (response.success) {
			ElMessage.success('Module saved successfully');
			row.isEditing = false;
			// 如果有返回的 ID，更新本地数据
			if (response.data) {
				row.id = String(response.data);
			}
			// 重新加载数据
			await loadAttachmentSharing();
		} else {
			ElMessage.error(response.msg || 'Failed to save module');
		}
	} finally {
		isSaving.value = false;
	}
}

/**
 * 删除模块
 */
function handleDeleteModule(index: number) {
	const row = attachmentSharing.value[index];
	if (!row) return;

	// 如果是新添加的未保存项，直接删除
	if (!row.id) {
		attachmentSharing.value.splice(index, 1);
		return;
	}

	ElMessageBox({
		title: `Are you sure you want to delete the module "${row.moduleName}"?`,
		message: `Are you sure you want to delete the module "${row.moduleName}"?`,
		showCancelButton: true,
		confirmButtonText: 'Delete',
		cancelButtonText: 'Cancel',
		type: 'warning',
		beforeClose: async (action, instance, done) => {
			if (action === 'confirm') {
				// 显示 loading 状态
				instance.confirmButtonLoading = true;
				instance.confirmButtonText = 'Deleting...';

				try {
					if (!row.id) {
						ElMessage.warning('Invalid module ID');
						instance.confirmButtonLoading = false;
						instance.confirmButtonText = 'Delete';
						return;
					}

					if (!props.integrationId || props.integrationId === 'new') {
						ElMessage.warning('Invalid integration ID');
						instance.confirmButtonLoading = false;
						instance.confirmButtonText = 'Delete';
						return;
					}

					const response = await deleteInboundSettingsAttachment(
						row.id,
						props.integrationId
					);
					if (response.success) {
						ElMessage.success('Module deleted successfully');
						attachmentSharing.value.splice(index, 1);
						// 重新加载数据以确保数据同步
						await loadAttachmentSharing();
						done(); // 关闭对话框
					} else {
						ElMessage.error(response.msg || 'Failed to delete module');
						// 恢复按钮状态
						instance.confirmButtonLoading = false;
						instance.confirmButtonText = 'Delete';
					}
				} catch (error) {
					console.error('Failed to delete module:', error);
					// 恢复按钮状态
					instance.confirmButtonLoading = false;
					instance.confirmButtonText = 'Delete';
				}
			} else {
				done(); // 取消或关闭时直接关闭对话框
			}
		},
	});
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

// 监听 integrationId 变化（包括初始化）
watch(
	() => props.integrationId,
	(newId) => {
		if (newId && newId !== 'new') {
			loadAttachmentSharing();
		} else {
			// 如果是新建或无效 ID，清空数据
			attachmentSharing.value = [];
		}
	},
	{ immediate: true }
);
</script>
