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
					multiple
					placeholder="Select workflows..."
					collapse-tags
					class="w-full"
				>
					<el-option
						v-for="workflow in workflows || []"
						:key="workflow.id"
						:label="workflow.name"
						:value="workflow.id"
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
const selectedWorkflows = ref<string[]>(props.outboundSettings?.attachmentWorkflows || []);

// 字段映射数据（模拟数据，实际应该从 API 获取）
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

// 监听 props 变化
watch(
	() => props.outboundSettings,
	(newSettings) => {
		if (newSettings) {
			selectedWorkflows.value = newSettings.attachmentWorkflows || [];
		}
	},
	{ immediate: true, deep: true }
);
</script>
