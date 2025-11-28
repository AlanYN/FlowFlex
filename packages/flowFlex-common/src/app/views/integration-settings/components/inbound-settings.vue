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
							v-if="row.isEditing"
							v-model="row.moduleName"
							placeholder="Enter external module name"
							@blur="handleModuleBlur(row)"
						/>
						<span v-else class="font-medium text-sm">
							{{ row.moduleName || 'Enter external module name' }}
						</span>
					</template>
				</el-table-column>

				<el-table-column label="Workflow" min-width="250">
					<template #default="{ row }">
						<el-select
							v-model="row.workflowId"
							placeholder="Select workflow..."
							@change="(val) => handleWorkflowChange(row, val)"
						>
							<el-option
								v-for="workflow in workflows"
								:key="workflow.id"
								:label="workflow.name"
								:value="String(workflow.id)"
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

				<el-table-column label="Stage" min-width="250">
					<template #default="{ row }">
						<el-select
							v-model="row.stageId"
							placeholder="Select stage..."
							:disabled="!row.workflowId"
							:loading="stagesLoadingMap[row.workflowId] || false"
							@change="handleAttachmentSharingChange"
							@focus="() => loadStagesForWorkflow(row.workflowId)"
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
					<template #default="{ row, $index }">
						<div class="flex items-center justify-center gap-1">
							<el-button
								v-if="row.isEditing || !row.id"
								type="primary"
								:loading="isSaving"
								@click="handleSaveModule(row, $index)"
							>
								Save
							</el-button>
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
import { getStagesByWorkflow } from '@/apis/ow';
import type { FieldMapping, InboundAttachmentIteml } from '#/integration';

interface Props {
	integrationId: string | number;
	workflows?: any[];
	inboundFieldMappings?: FieldMapping[];
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

// Stages 数据缓存
const stagesCache = ref<Record<string, Array<{ id: string; name: string }>>>({});
// Stages 加载状态（按 workflowId）
const stagesLoadingMap = ref<Record<string, boolean>>({});

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
 * 处理 workflow 变化
 */
const handleWorkflowChange = async (row: IAttachmentSharingExtended, workflowId: string) => {
	row.stageId = '';
	if (workflowId && workflowId !== '0') {
		await loadStagesForWorkflow(workflowId);
	}
	handleAttachmentSharingChange();
};

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

	// 并行加载所有需要的 stages
	await Promise.all(
		Array.from(workflowIds).map((workflowId) => loadStagesForWorkflow(workflowId))
	);
}

/**
 * 添加模块
 */
function handleAddModule() {
	attachmentSharing.value.push({
		id: '',
		moduleName: '',
		workflowId: '',
		stageId: '',
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
 * 附件共享变更
 */
function handleAttachmentSharingChange() {
	// 数据变更处理（不自动保存）
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
			stageId: row.stageId,
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

					const response = await deleteInboundSettingsAttachment(row.id);
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
					ElMessage.error('Failed to delete module');
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
 * 上移模块
 */

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
