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
					label="Field (Display Name)"
					prop="fieldDisplayName"
					min-width="220"
				>
					<template #default="{ row }">
						<span class="text-sm">{{ row.fieldDisplayName }}</span>
					</template>
				</el-table-column>

				<el-table-column label="Field (API Name)" prop="fieldApiName" min-width="220">
					<template #default="{ row }">
						<span class="font-medium text-sm">{{ row.fieldApiName }}</span>
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

			<div class="space-y-3">
				<div class="text-sm font-medium text-text-primary">Workflows</div>
				<el-select
					v-model="selectedWorkflows"
					v-loading="isLoading"
					multiple
					placeholder="Select workflows..."
					collapse-tags
					class="w-full"
					:max-collapse-tags="5"
					clearable
					@blur="handleSaveWorkflows"
				>
					<el-option
						v-for="workflow in workflows || []"
						:key="workflow.id"
						:label="workflow.name"
						:value="String(workflow.id)"
						:disabled="!workflow.isActive"
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
				<div class="text-xs text-text-secondary">
					Attachments from selected workflows will be shared with IAM System.
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import { Search } from '@element-plus/icons-vue';
import { ElMessage } from 'element-plus';
import { useDebounceFn } from '@vueuse/core';
import {
	createOutboundSettingsAttachment,
	getOutboundSettingsAttachment,
} from '@/apis/integration';

// 字段映射显示类型
interface IFieldMappingDisplay {
	actionId: string;
	actionName: string;
	fieldDisplayName: string;
	fieldApiName: string;
}

interface Props {
	integrationId: string | number;
	outboundSettings?: {
		masterData?: string[];
		fields?: string[];
		attachmentWorkflows?: string[];
	};
	workflows?: any[];
}

const props = defineProps<Props>();

// 搜索过滤
const fieldSearch = ref('');

// Workflows
const workflows = computed(() => props.workflows || []);
const selectedWorkflows = ref<string[]>([]);
const isLoading = ref(false);
const isSaving = ref(false);

// 字段映射数据（暂时为空数组，数据来源待确定）
const fieldMappings = ref<IFieldMappingDisplay[]>([]);

/**
 * 过滤后的字段映射列表
 */
const filteredFieldMappings = computed(() => {
	if (!fieldSearch.value) {
		return fieldMappings.value;
	}

	const search = fieldSearch.value.toLowerCase();
	return fieldMappings.value.filter(
		(item) =>
			item.fieldDisplayName.toLowerCase().includes(search) ||
			item.fieldApiName.toLowerCase().includes(search) ||
			item.actionName.toLowerCase().includes(search)
	);
});

/**
 * 加载附件工作流配置
 */
async function loadAttachmentWorkflows() {
	if (!props.integrationId || props.integrationId === 'new') {
		selectedWorkflows.value = [];
		return;
	}

	isLoading.value = true;
	try {
		const response = await getOutboundSettingsAttachment(props.integrationId);
		if (response.success && response.data) {
			// 将 workflowIds 转换为字符串数组（因为 el-select 的 value 需要是字符串）
			const workflowIds = response.data.workflowIds || [];
			selectedWorkflows.value = workflowIds.map((id) => String(id));
		} else {
			selectedWorkflows.value = [];
		}
	} catch (error) {
		console.error('Failed to load attachment workflows:', error);
		selectedWorkflows.value = [];
	} finally {
		isLoading.value = false;
	}
}

/**
 * 保存附件工作流配置（内部实现）
 */
async function _handleSaveWorkflows() {
	if (!props.integrationId || props.integrationId === 'new') {
		ElMessage.warning('Please save the integration first');
		return;
	}

	isSaving.value = true;
	try {
		// 将字符串数组转换为字符串数组（接口需要 string[]）
		const workflowIds = selectedWorkflows.value.map((id) => String(id));

		const response = await createOutboundSettingsAttachment(
			String(props.integrationId),
			workflowIds
		);
		if (response.success) {
			ElMessage.success('Workflows saved successfully');
		} else {
			ElMessage.error(response.msg || 'Failed to save workflows');
		}
	} catch (error) {
		console.error('Failed to save workflows:', error);
		ElMessage.error('Failed to save workflows');
	} finally {
		isSaving.value = false;
	}
}

/**
 * 保存附件工作流配置（带防抖）
 */
const handleSaveWorkflows = useDebounceFn(_handleSaveWorkflows, 500);

// 监听 integrationId 变化（包括初始化）
watch(
	() => props.integrationId,
	(newId) => {
		if (newId && newId !== 'new') {
			loadAttachmentWorkflows();
		} else {
			selectedWorkflows.value = [];
		}
	},
	{ immediate: true }
);
</script>
