<template>
	<div class="min-h-screen">
		<PageHeader
			description="Configure connection and data exchange settings"
			:title="integrationName || 'Integration Details'"
			:show-back-button="true"
			@go-back="handleBack"
		/>

		<!-- 加载状态 - 骨架屏 -->
		<div v-if="isLoading" class="space-y-6 pb-8">
			<!-- Connection Auth 骨架屏 -->
			<div class="wfe-global-block-bg p-6">
				<el-skeleton :rows="6" animated />
			</div>

			<!-- Entity Type Mapping 骨架屏 -->
			<div class="wfe-global-block-bg p-6">
				<el-skeleton :rows="4" animated />
			</div>

			<!-- Tabs 骨架屏 -->
			<div class="wfe-global-block-bg p-6">
				<el-skeleton :rows="8" animated />
			</div>
		</div>

		<!-- 详情内容 -->
		<div v-else class="space-y-6 pb-8">
			<connection-auth
				:integration-id="integrationId"
				:connection-data="integrationData || undefined"
				:integrationStatus="integrationStatus"
				:testErrorMsg="testErrorMsg"
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
					:all-workflows="workflows"
					@refresh="loadIntegrationData"
				/>

				<!-- Tabs -->
				<div class="wfe-global-block-bg p-4">
					<PrototypeTabs v-model="activeTab" :tabs="tabsConfig" class="">
						<TabPane value="inbound">
							<inbound-settings
								:integration-id="integrationId"
								:attachmentApiMd="attachmentInboundApiMd"
								:loading="isLoadingAttachmentApiMd"
								:workflows="workflows"
								:inboundFieldMappings="integrationData?.inboundFieldMappings || []"
								:actions="actions"
								@refresh="loadIntegrationData"
							/>
						</TabPane>

						<TabPane value="outbound">
							<outbound-settings
								:integration-id="integrationId"
								:attachmentApiMd="attachmentOutboundApiMd"
								:loading="isLoadingAttachmentApiMd"
								:workflows="workflows"
								:outboundFieldMappings="
									integrationData?.outboundFieldMappings || []
								"
								@refresh="loadIntegrationData"
							/>
						</TabPane>

						<TabPane value="actions">
							<actions-list
								:integration-id="String(integrationId)"
								:integration-name="integrationName"
								:all-workflows="workflows"
								:actions="actions"
								:is-loading="actionsLoading"
								@refresh="loadIntegrationData"
							/>
						</TabPane>

						<TabPane value="quickLinks">
							<quick-links :integration-id="String(integrationId)" />
						</TabPane>
					</PrototypeTabs>
				</div>
			</template>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted, nextTick } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage } from 'element-plus';
import { PrototypeTabs, TabPane } from '@/components/PrototypeTabs';
import ConnectionAuth from './components/connection-auth.vue';
import EntityType from './components/entityType.vue';
import InboundSettings from './components/inbound-settings.vue';
import OutboundSettings from './components/outbound-settings.vue';
import ActionsList from './components/actions-list.vue';
import QuickLinks from './components/quick-links.vue';
import { getIntegrationDetails, testConnection, getAttachmentApiMd } from '@/apis/integration';
import { getWorkflowList } from '@/apis/ow';
import { getActionDefinitions } from '@/apis/action';
import type { IIntegrationConfig } from '#/integration';
import PageHeader from '@/components/global/PageHeader/index.vue';

const route = useRoute();
const router = useRouter();

// 状态管理
const integrationId = ref<string>('new');
const integrationName = ref<string>('New Integration');
const integrationStatus = ref<number>(0);
const integrationData = ref<IIntegrationConfig | null>(null);
const isLoading = ref(false);
const activeTab = ref('inbound');
const workflows = ref<any[]>([]);
const actions = ref<any[]>([]);
const actionsLoading = ref(false);

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
		ElMessage.info('Invalid integration ID');
		handleBack();
		return;
	}

	integrationId.value = id;

	// 如果是新建，使用默认值
	if (id === 'new') {
		integrationName.value = 'New Integration';
		integrationStatus.value = 0;
		integrationData.value = null;
		isLoading.value = false;
		return;
	}

	// 加载完整数据
	isLoading.value = true;
	try {
		await loadWorkflows();
		const response = await getIntegrationDetails(id);
		if (response.code == '200' && response.data) {
			integrationData.value = response.data;
			integrationName.value = response.data.name || 'Integration Details';
			integrationStatus.value = response.data.status || 0;
			// 加载 actions 列表
			await loadActions();
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
const handleIntegrationCreated = async (id: string, name: string) => {
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
const testErrorMsg = ref<string>('');
const handleTestConnection = async () => {
	if (!integrationId.value || integrationId.value === 'new') return;

	try {
		const result = await testConnection(integrationId.value);
		if (result.code == '200') {
			ElMessage.success('Connection test successful');
			integrationStatus.value = 1;
			testErrorMsg.value = '';
		} else {
			testErrorMsg.value = result.data?.msg || 'Connection test failed';
			integrationStatus.value = 0;
		}
	} catch (error) {
		console.error('Connection test failed:', error);
		integrationStatus.value = 0;
	}
};

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

/**
 * 加载动作列表
 */
async function loadActions() {
	if (!integrationId.value || integrationId.value === 'new') {
		actions.value = [];
		return;
	}

	actionsLoading.value = true;
	try {
		const res = await getActionDefinitions({
			integrationId: String(integrationId.value),
		});
		if (res.code === '200') {
			actions.value = res.data.data || [];
		} else {
			actions.value = [];
			ElMessage.error(res.msg || 'Failed to load actions');
		}
	} catch (error) {
		console.error('Failed to load actions:', error);
		actions.value = [];
	} finally {
		actionsLoading.value = false;
	}
}

const attachmentInboundApiMd = ref('');
const attachmentOutboundApiMd = ref('');
const isLoadingAttachmentApiMd = ref(false);
const loadAttachmentApiMd = async () => {
	try {
		isLoadingAttachmentApiMd.value = true;
		const res = await getAttachmentApiMd();
		if (res.code == '200') {
			attachmentInboundApiMd.value = res?.data?.inbound || '';
			attachmentOutboundApiMd.value = res?.data?.outbound || '';
		}
	} finally {
		isLoadingAttachmentApiMd.value = false;
	}
};

// 初始化
onMounted(async () => {
	loadIntegrationData();
	loadAttachmentApiMd();
});
</script>
