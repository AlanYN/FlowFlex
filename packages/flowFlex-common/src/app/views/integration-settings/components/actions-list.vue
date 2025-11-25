<template>
	<div class="">
		<div class="flex flex-col gap-2 my-2">
			<div class="font-bold">Integration Actions</div>
			<div class="text-gray-500">
				All actions related to {{ integrationName }} System integration
			</div>
		</div>

		<el-table
			v-loading="isLoading"
			:data="actions"
			border
			stripe
			empty-text="No actions configured for this integration"
		>
			<el-table-column label="Action ID" prop="id" width="180">
				<template #default="{ row }">
					<span class="action-id">{{ row.id }}</span>
				</template>
			</el-table-column>

			<el-table-column label="Action Name" prop="name" min-width="200">
				<template #default="{ row }">
					<el-link type="primary" :underline="false" @click="handleActionClick(row.id)">
						{{ row.name }}
					</el-link>
				</template>
			</el-table-column>

			<el-table-column label="Type" prop="type" width="150">
				<template #default="{ row }">
					<el-tag size="small" type="info">{{ row.type }}</el-tag>
				</template>
			</el-table-column>

			<el-table-column label="Status" prop="status" width="120" align="center">
				<template #default="{ row }">
					<el-tag
						:type="row.status === 'active' ? 'success' : 'info'"
						size="small"
						:style="{
							background:
								row.status === 'active' ? 'var(--success-color)' : '#909399',
							borderColor:
								row.status === 'active' ? 'var(--success-color)' : '#909399',
							color: 'white',
						}"
					>
						{{ row.status === 'active' ? 'Active' : 'Inactive' }}
					</el-tag>
				</template>
			</el-table-column>

			<el-table-column label="Workflows" prop="workflows" min-width="250">
				<template #default="{ row }">
					<el-tag v-for="workflowId in row.workflows" :key="workflowId">
						{{ getWorkflowName(workflowId) }}
					</el-tag>
				</template>
			</el-table-column>
		</el-table>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';

interface Props {
	integrationId: string;
	integrationName: string;
}

defineProps<Props>();
const router = useRouter();

// 状态管理
const isLoading = ref(false);
const actions = ref<[]>([]);

// 模拟工作流名称映射（实际应该从 API 获取）
const workflowNameMap: Record<string, string> = {
	'wf-1': 'Onboarding Workflow',
	'wf-2': 'Customer Service Workflow',
	'wf-3': 'Sales Workflow',
};

/**
 * 获取工作流名称
 */
function getWorkflowName(workflowId: string): string {
	return workflowNameMap[workflowId] || workflowId;
}

/**
 * 加载动作列表
 */
async function loadActions() {
	isLoading.value = true;
	try {
		actions.value = [];
	} catch (error) {
		console.error('Failed to load actions:', error);
	} finally {
		isLoading.value = false;
	}
}

/**
 * 点击动作名称
 */
function handleActionClick(actionId: string) {
	// 跳转到动作详情页
	router.push(`/actions/${actionId}`);
}

// 初始化
onMounted(() => {
	loadActions();
});
</script>
