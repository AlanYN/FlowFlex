<template>
	<div class="min-h-screen">
		<PageHeader
			:title="integrationName || 'Integration Details'"
			description="Configure connection and data exchange settings"
			:show-back-button="true"
			@go-back="handleBack"
		>
			<template #actions>
				<div class="flex gap-x-2 items-center">
					<el-tag
						v-if="integrationStatus !== undefined"
						:type="integrationStatus === 1 ? 'success' : 'info'"
					>
						{{ integrationStatus === 1 ? 'Connected' : 'Disconnected' }}
					</el-tag>
					<el-button type="danger" :icon="Delete" @click="handleDelete" />
					<div></div>
				</div>
			</template>
		</PageHeader>

		<!-- 加载状态 -->
		<div v-if="isLoading" v-loading="true" class="min-h-[400px]"></div>

		<!-- 详情内容 -->
		<div v-else class="space-y-6 pb-8">
			<connection-auth
				:integration-id="integrationId"
				:connection-data="integrationData || undefined"
				@created="handleIntegrationCreated"
				@updated="handleIntegrationUpdated"
				@test="handleTestConnection"
			/>

			<!-- 只有当集成已保存（有真实 ID）时才显示其他模块 -->
			<template v-if="integrationId && integrationId !== 'new' && integrationData">
				<!-- Entity Type Mapping -->
				<entity-type
					v-if="integrationData"
					:integration-id="integrationId"
					:entity-mappings="integrationData.entityMappings || []"
					@refresh="loadIntegrationData"
				/>

				<!-- Tabs -->
				<div class="wfe-global-block-bg p-4">
					<PrototypeTabs v-model="activeTab" :tabs="tabsConfig" class="">
						<TabPane value="inbound">
							<inbound-settings
								:integration-id="integrationId"
								:workflows="workflows"
								@refresh="loadIntegrationData"
							/>
						</TabPane>

						<TabPane value="outbound">
							<outbound-settings
								:integration-id="integrationId"
								:workflows="workflows"
								@refresh="loadIntegrationData"
							/>
						</TabPane>

						<TabPane value="actions">
							<actions-list
								:integration-id="String(integrationId)"
								:integration-name="integrationName"
							/>
						</TabPane>

						<TabPane value="quickLinks">
							<quick-links :integration-id="String(integrationId)" />
						</TabPane>
					</PrototypeTabs>
				</div>

				<!-- 底部操作按钮 -->
			</template>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted, nextTick } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Delete } from '@element-plus/icons-vue';
import { PrototypeTabs, TabPane } from '@/components/PrototypeTabs';
import ConnectionAuth from './components/connection-auth.vue';
import EntityType from './components/entityType.vue';
import InboundSettings from './components/inbound-settings.vue';
import OutboundSettings from './components/outbound-settings.vue';
import ActionsList from './components/actions-list.vue';
import QuickLinks from './components/quick-links.vue';
import { getIntegrationDetails, deleteIntegration, testConnection } from '@/apis/integration';
import { getWorkflowList } from '@/apis/ow';
import type { IIntegrationConfig } from '#/integration';
import PageHeader from '@/components/global/PageHeader/index.vue';

const route = useRoute();
const router = useRouter();

// 状态管理
const integrationId = ref<string | number>('new');
const integrationName = ref<string>('New Integration');
const integrationStatus = ref<number>(0);
const integrationData = ref<IIntegrationConfig | null>(null);
const isLoading = ref(false);
const activeTab = ref('inbound');
const workflows = ref<any[]>([]);

// Tab 配置
const tabsConfig = [
	{
		value: 'inbound',
		label: 'Inbound Settings',
	},
	{
		value: 'outbound',
		label: 'Outbound Settings',
	},
	{
		value: 'actions',
		label: 'Actions',
	},
	{
		value: 'quickLinks',
		label: 'Quick Links',
	},
];

/**
 * 加载集成数据
 */
async function loadIntegrationData() {
	const id = route.params.id as string;
	if (!id) {
		ElMessage.error('Invalid integration ID');
		handleBack();
		return;
	}

	integrationId.value = id;

	// 如果是新建，使用默认值
	if (id === 'new') {
		integrationName.value = 'New Integration';
		integrationStatus.value = 0;
		integrationData.value = null;
		return;
	}

	// 加载完整数据
	isLoading.value = true;
	try {
		await loadWorkflows();
		const response = await getIntegrationDetails(id);
		if (response.success && response.data) {
			integrationData.value = response.data;
			integrationName.value = response.data.name || 'Integration Details';
			integrationStatus.value = response.data.status || 0;
		} else {
			ElMessage.error(response.msg || 'Failed to load integration');
		}
	} finally {
		isLoading.value = false;
	}
}

/**
 * 处理集成创建成功
 */
const handleIntegrationCreated = async (id: string | number, name: string) => {
	integrationId.value = id;
	integrationName.value = name;
	// 更新路由并重新加载数据
	await router.replace({
		name: 'IntegrationDetail',
		params: { id: String(id) },
	});
	nextTick(() => {
		loadIntegrationData();
	});
};

/**
 * 处理集成更新
 */
function handleIntegrationUpdated() {
	nextTick(() => {
		loadIntegrationData();
	});
}

/**
 * 测试连接（由父组件调用 test 接口）
 */
async function handleTestConnection() {
	if (!integrationId.value || integrationId.value === 'new') return;

	try {
		const result = await testConnection(integrationId.value);
		if (result.success && result.data) {
			ElMessage.success('Connection test successful');
			integrationStatus.value = 1;
			// 重新加载数据以更新状态
			loadIntegrationData();
		} else {
			ElMessage.error(result.msg || 'Connection test failed');
		}
	} catch (error) {
		console.error('Connection test failed:', error);
	}
}

/**
 * 删除集成
 */
async function handleDelete() {
	if (!integrationId.value || integrationId.value === 'new') return;

	try {
		await ElMessageBox.confirm(
			`Are you sure you want to delete the integration "${integrationName.value}"? This action cannot be undone.`,
			'Confirm Deletion',
			{
				confirmButtonText: 'Delete',
				cancelButtonText: 'Cancel',
				type: 'warning',
			}
		);

		const res = await deleteIntegration(integrationId.value);
		if (res.success) {
			ElMessage.success('Integration deleted successfully');
		} else {
			ElMessage.error(res.msg || 'Failed to delete integration');
		}
	} catch (error) {
		console.log('Failed to delete integration:', error);
	}
}

/**
 * 返回列表
 */
function handleBack() {
	router.push({ name: 'Integration' });
}

/**
 * 获取工作流列表
 */
async function loadWorkflows() {
	try {
		const response = await getWorkflowList();
		if (response.code === '200') {
			const workflowList = response.data || [];
			// 处理默认工作流显示
			const processedWorkflows = workflowList.map((workflow: any) => {
				if (workflow.isDefault) {
					return {
						...workflow,
						name: '⭐ ' + workflow.name,
					};
				}
				return workflow;
			});
			workflows.value = processedWorkflows;
		}
	} catch (error) {
		console.error('Failed to load workflows:', error);
		workflows.value = [];
	}
}

// 初始化
onMounted(async () => {
	loadIntegrationData();
});
</script>
