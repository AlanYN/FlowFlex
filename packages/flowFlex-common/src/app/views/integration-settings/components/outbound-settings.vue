<template>
	<div class="space-y-8">
		<!-- Master Data to Share -->
		<div class="space-y-4">
			<div>
				<h3 class="text-lg font-semibold text-text-primary m-0">Master Data to Share</h3>
				<p class="text-sm text-text-secondary mt-1">
					Select WFE master data to make available for BNP
				</p>
			</div>

			<el-checkbox-group v-model="selectedMasterData" @change="handleMasterDataChange">
				<el-checkbox value="cases" class="w-full mb-3">
					<div class="flex flex-col">
						<span class="font-medium text-sm">Cases</span>
						<span class="text-xs text-text-secondary">
							All WFE cases available to BNP
						</span>
					</div>
				</el-checkbox>
			</el-checkbox-group>
		</div>

		<!-- Fields to Share -->
		<div class="space-y-4">
			<div>
				<h3 class="text-lg font-semibold text-text-primary m-0">Fields to Share</h3>
				<p class="text-sm text-text-secondary mt-1">
					Select WFE fields to make available for BNP
				</p>
			</div>

			<transfer-panel
				v-model="selectedFields"
				:data="allFieldsData"
				left-title="Available Fields"
				right-title="Fields to Share"
				left-empty-text="No fields available"
				right-empty-text="No fields selected"
				@change="handleFieldsChange"
			/>
		</div>

		<!-- Attachments to Share -->
		<div class="space-y-4">
			<div>
				<h3 class="text-lg font-semibold text-text-primary m-0">Attachments to Share</h3>
				<p class="text-sm text-text-secondary mt-1">
					Configure which workflow attachments can be shared with BNP
				</p>
			</div>

			<div class="space-y-3">
				<div class="text-sm font-medium text-text-primary">Workflows</div>
				<div class="text-sm text-text-secondary mb-2">
					Select workflows whose attachments can be shared
				</div>
				<el-select
					v-model="selectedWorkflows"
					multiple
					placeholder="Select workflows..."
					collapse-tags
					class="w-full"
					@change="handleWorkflowsChange"
				>
					<el-option
						v-for="workflow in workflowOptions"
						:key="workflow.id"
						:label="workflow.name"
						:value="workflow.id"
					/>
				</el-select>
			</div>
		</div>

		<!-- Sync Configuration -->
		<div class="space-y-4">
			<div>
				<h3 class="text-lg font-semibold text-text-primary m-0">Sync Configuration</h3>
				<p class="text-sm text-text-secondary mt-1">
					Configure how data is synchronized to BNP
				</p>
			</div>

			<div class="space-y-4">
				<div class="flex items-start gap-3">
					<el-switch v-model="syncConfig.realTimeSync" @change="handleSyncConfigChange" />
					<div class="flex-1">
						<div class="text-sm font-medium text-text-primary">Real-time Sync</div>
						<div class="text-xs text-text-secondary mt-1">
							Automatically sync changes to BNP
						</div>
					</div>
				</div>

				<div class="space-y-2">
					<div class="text-sm font-medium text-text-primary">Webhook URL</div>
					<div class="text-xs text-text-secondary">
						Optional webhook endpoint for notifications
					</div>
					<el-input
						v-model="syncConfig.webhookUrl"
						placeholder="https://example.com/webhook"
						@change="handleSyncConfigChange"
					/>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import TransferPanel from '@/components/transfer-panel/index.vue';
import type { IIntegrationConfig, IWorkflowOption } from '#/integration';

// 穿梭框数据项接口
interface ITransferItem {
	key: string;
	label: string;
	description?: string;
	group?: string;
	disabled?: boolean;
}

interface Props {
	integration: IIntegrationConfig;
}

interface Emits {
	(e: 'update', data: Partial<IIntegrationConfig>): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

// Master Data
const selectedMasterData = ref<string[]>(props.integration.outboundSettings?.masterData || []);

// Fields to Share
const selectedFields = ref<string[]>([]);

// 所有字段数据（转换为 TransferPanel 格式）
const allFieldsData: ITransferItem[] = [
	// Static Fields
	{ key: 'case_id', label: 'Case ID', description: 'text', group: 'Static Fields' },
	{ key: 'case_title', label: 'Case Title', description: 'text', group: 'Static Fields' },
	{ key: 'case_status', label: 'Case Status', description: 'text', group: 'Static Fields' },
	{ key: 'created_date', label: 'Created Date', description: 'date', group: 'Static Fields' },
	{ key: 'updated_date', label: 'Updated Date', description: 'date', group: 'Static Fields' },
	{ key: 'assigned_to', label: 'Assigned To', description: 'text', group: 'Static Fields' },
	{ key: 'priority', label: 'Priority', description: 'text', group: 'Static Fields' },
	// Dynamic Fields
	{ key: 'customer_name', label: 'Customer Name', description: 'text', group: 'Dynamic Fields' },
	{
		key: 'customer_email',
		label: 'Contact Email',
		description: 'email',
		group: 'Dynamic Fields',
	},
	{ key: 'customer_phone', label: 'Phone Number', description: 'phone', group: 'Dynamic Fields' },
	{ key: 'company_name', label: 'Company Name', description: 'text', group: 'Dynamic Fields' },
	{ key: 'payment_terms', label: 'Payment Terms', description: 'text', group: 'Dynamic Fields' },
];

// Workflows
const selectedWorkflows = ref<string[]>(
	props.integration.outboundSettings?.attachmentWorkflows || []
);

const workflowOptions = ref<IWorkflowOption[]>([
	{ id: 'wf-1', name: 'Onboarding Workflow' },
	{ id: 'wf-2', name: 'Customer Service Workflow' },
	{ id: 'wf-3', name: 'Sales Workflow' },
]);

// Sync Configuration
const syncConfig = ref({
	realTimeSync: false,
	webhookUrl: '',
});

/**
 * 处理 Master Data 变更
 */
function handleMasterDataChange() {
	emitUpdate();
}

/**
 * 处理字段变更
 */
function handleFieldsChange() {
	emitUpdate();
}

/**
 * 处理 Workflows 变更
 */
function handleWorkflowsChange() {
	emitUpdate();
}

/**
 * 处理 Sync Config 变更
 */
function handleSyncConfigChange() {
	emitUpdate();
}

/**
 * 发送更新事件
 */
function emitUpdate() {
	emit('update', {
		outboundSettings: {
			masterData: selectedMasterData.value,
			fields: selectedFields.value,
			attachmentWorkflows: selectedWorkflows.value,
		},
	});
}

// 监听 props 变化
watch(
	() => props.integration.outboundSettings,
	(newSettings) => {
		if (newSettings) {
			selectedMasterData.value = newSettings.masterData || [];
			selectedWorkflows.value = newSettings.attachmentWorkflows || [];
		}
	},
	{ deep: true }
);
</script>

<style scoped lang="scss">
// Element Plus Transfer 组件已在全局样式中统一处理
// 这里只保留必要的局部样式覆盖
</style>
