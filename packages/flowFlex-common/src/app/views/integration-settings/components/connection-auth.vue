<template>
	<div class="wfe-global-block-bg p-4">
		<!-- 标题和描述 -->
		<div>
			<h3 class="text-xl font-semibold text-text-primary m-0">Connection & Authentication</h3>
			<p class="text-sm text-text-secondary my-2">
				Configure connection details and authentication method
			</p>
		</div>

		<el-form ref="formRef" :model="formData" :rules="rules" label-position="top">
			<!-- System Name & Endpoint URL (两列布局) -->
			<div class="grid grid-cols-2 gap-6">
				<el-form-item label="System Name" prop="systemName">
					<el-input
						v-model="formData.systemName"
						placeholder="BNP"
						@change="handleFormChange"
					/>
				</el-form-item>

				<el-form-item label="Endpoint URL" prop="endpointUrl">
					<el-input
						v-model="formData.endpointUrl"
						placeholder="https://api.example.com"
						@change="handleFormChange"
					/>
				</el-form-item>
			</div>

			<el-form-item label="Description" prop="description" class="w-full">
				<el-input
					v-model="formData.description"
					placeholder="Enter description"
					type="textarea"
					maxlength="500"
					:autosize="inputTextraAutosize"
					show-word-limit
					@change="handleFormChange"
				/>
			</el-form-item>

			<!-- Authentication Method -->
			<el-form-item
				label="Authentication Method"
				prop="authMethod"
				placeholder="Select authentication method"
			>
				<el-select v-model="formData.authMethod" @change="handleAuthMethodChange">
					<el-option :value="AuthMethod.ApiKey" label="API Key" />
					<el-option :value="AuthMethod.OAuth2" label="OAuth 2.0" />
					<el-option :value="AuthMethod.BasicAuth" label="Basic Auth" />
					<el-option :value="AuthMethod.BearerToken" label="Bearer Token" />
				</el-select>
			</el-form-item>

			<!-- Credentials (两列布局) -->
			<div class="grid grid-cols-2 gap-6">
				<!-- API Key -->
				<template v-if="formData.authMethod === AuthMethod.ApiKey">
					<el-form-item label="API Key" prop="credentials.apiKey">
						<el-input
							v-model="formData.credentials.apiKey"
							type="password"
							placeholder="Enter API key"
							show-password
							@change="handleFormChange"
						/>
					</el-form-item>
				</template>

				<!-- Basic Auth -->
				<template v-else-if="formData.authMethod === AuthMethod.BasicAuth">
					<el-form-item label="Username" prop="credentials.username">
						<el-input
							v-model="formData.credentials.username"
							placeholder="Enter username"
							@change="handleFormChange"
						/>
					</el-form-item>
					<el-form-item label="Password" prop="credentials.password">
						<el-input
							v-model="formData.credentials.password"
							type="password"
							placeholder="Enter password"
							show-password
							@change="handleFormChange"
						/>
					</el-form-item>
				</template>

				<!-- Bearer Token -->
				<template v-else-if="formData.authMethod === AuthMethod.BearerToken">
					<el-form-item label="Bearer Token" prop="credentials.token">
						<el-input
							v-model="formData.credentials.token"
							type="textarea"
							placeholder="Enter bearer token"
							:autosize="inputTextraAutosize"
							@change="handleFormChange"
						/>
					</el-form-item>
				</template>

				<!-- OAuth 2.0 -->
				<template v-else-if="formData.authMethod === AuthMethod.OAuth2">
					<el-form-item label="Client ID" prop="credentials.clientId">
						<el-input
							v-model="formData.credentials.clientId"
							placeholder="Enter client ID"
							@change="handleFormChange"
						/>
					</el-form-item>
					<el-form-item label="Client Secret" prop="credentials.clientSecret">
						<el-input
							v-model="formData.credentials.clientSecret"
							type="password"
							placeholder="Enter client secret"
							show-password
							@change="handleFormChange"
						/>
					</el-form-item>
				</template>
			</div>

			<!-- 操作按钮 (右对齐) -->
			<div class="flex justify-end gap-3">
				<el-button
					v-if="props.integrationId === 'new'"
					type="primary"
					size="large"
					:loading="isSaving"
					:disabled="!isFormValid"
					@click="handleSave"
				>
					Create Integration
				</el-button>
				<template v-if="props.integrationId !== 'new'">
					<el-button
						type="primary"
						size="large"
						:loading="isUpdating"
						:disabled="!isFormValid"
						@click="() => handleUpdate(true)"
					>
						Update
					</el-button>
					<el-button
						type="primary"
						size="large"
						:loading="isTesting"
						:disabled="!isFormValid"
						@click="handleTestConnection"
					>
						<el-icon class="mr-2"><Connection /></el-icon>
						Test Connection
					</el-button>
				</template>
			</div>
		</el-form>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import type { FormInstance, FormRules } from 'element-plus';
import { Connection } from '@element-plus/icons-vue';
import { ElMessage } from 'element-plus';
import { createIntegration, updateIntegration } from '@/apis/integration';
import type { IConnectionConfig, IIntegrationConfig } from '#/integration';
import { AuthMethod } from '@/enums/integration';
import { inputTextraAutosize } from '@/settings/projectSetting';

interface Props {
	integrationId: string | number;
	connectionData?: IIntegrationConfig;
}

interface Emits {
	(e: 'created', id: string | number, name: string): void;
	(e: 'updated'): void;
	(e: 'test'): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

// 表单引用
const formRef = ref<FormInstance>();

// 状态
const isSaving = ref(false);
const isUpdating = ref(false);
const isTesting = ref(false);
const integrationName = ref<string>('New Integration');

// 表单数据
const formData = ref<IConnectionConfig & { authMethod: string | number }>({
	systemName: '',
	endpointUrl: '',
	authMethod: AuthMethod.ApiKey,
	credentials: {},
	description: '',
});

// 表单验证规则
const rules = ref<FormRules>({
	systemName: [{ required: true, message: 'Please enter system name', trigger: 'blur' }],
	endpointUrl: [
		{ required: true, message: 'Please enter endpoint URL', trigger: 'blur' },
		{ type: 'url', message: 'Please enter a valid URL', trigger: 'blur' },
	],
	authMethod: [{ required: true, message: 'Please select auth method', trigger: 'change' }],
	'credentials.apiKey': [
		{
			required: computed(() => formData.value.authMethod === AuthMethod.ApiKey).value,
			message: 'Please enter API key',
			trigger: 'blur',
		},
	],
	'credentials.username': [
		{
			required: computed(() => formData.value.authMethod === AuthMethod.BasicAuth).value,
			message: 'Please enter username',
			trigger: 'blur',
		},
	],
	'credentials.password': [
		{
			required: computed(() => formData.value.authMethod === AuthMethod.BasicAuth).value,
			message: 'Please enter password',
			trigger: 'blur',
		},
	],
	'credentials.token': [
		{
			required: computed(() => formData.value.authMethod === AuthMethod.BearerToken).value,
			message: 'Please enter bearer token',
			trigger: 'blur',
		},
	],
});

// 表单是否有效
const isFormValid = computed(() => {
	return (
		formData.value.systemName &&
		formData.value.endpointUrl &&
		`${formData.value.authMethod}` &&
		hasValidCredentials()
	);
});

/**
 * 检查凭证是否有效
 */
function hasValidCredentials(): boolean {
	switch (formData.value.authMethod) {
		case AuthMethod.ApiKey:
			return !!formData.value.credentials.apiKey;
		case AuthMethod.BasicAuth:
			return !!formData.value.credentials.username && !!formData.value.credentials.password;
		case AuthMethod.BearerToken:
			return !!formData.value.credentials.token;
		case AuthMethod.OAuth2:
			return (
				!!formData.value.credentials.clientId && !!formData.value.credentials.clientSecret
			);
		default:
			return false;
	}
}

/**
 * 处理认证方式变更
 */
function handleAuthMethodChange() {
	// 清空凭证
	formData.value.credentials = {};
}

/**
 * 初始化表单数据
 */
function initFormData() {
	if (props.integrationId === 'new') {
		// 新建模式，使用默认值
		formData.value.systemName = '';
		formData.value.endpointUrl = '';
		formData.value.authMethod = AuthMethod.ApiKey;
		formData.value.credentials = {};
		return;
	}

	// 使用传入的数据
	if (props.connectionData) {
		formData.value = {
			...props.connectionData,
			systemName: props.connectionData.systemName || '',
			endpointUrl: props.connectionData.endpointUrl || '',
			authMethod: props.connectionData.authMethod || AuthMethod.ApiKey,
			credentials: props.connectionData.credentials || {},
		};
	}
}

/**
 * 处理表单变更（仅标记数据已变更，不自动保存）
 */
function handleFormChange() {
	// 表单数据变更，但不自动保存，等待用户点击 Update 按钮
}

/**
 * 保存集成（新建时）
 */
async function handleSave() {
	if (!formRef.value) return;

	try {
		await formRef.value.validate();
		isSaving.value = true;

		const res = await createIntegration({
			...formData.value,
			systemName: formData.value.systemName,
			endpointUrl: formData.value.endpointUrl,
			authMethod: formData.value.authMethod,
			credentials: formData.value.credentials,
			name: formData.value.systemName,
		});

		if (res.success && res.data) {
			ElMessage.success('Integration created successfully');
			emit('created', res.data, integrationName.value);
		} else {
			ElMessage.error(res.msg || 'Failed to create integration');
		}
	} finally {
		isSaving.value = false;
	}
}

/**
 * 更新集成连接配置
 */
const handleUpdate = async (informParams: boolean = true) => {
	if (!formRef.value) return;

	try {
		await formRef.value.validate();
		isUpdating.value = true;

		const res = await updateIntegration(props.integrationId, {
			...formData.value,
			systemName: formData.value.systemName,
			endpointUrl: formData.value.endpointUrl,
			authMethod: formData.value.authMethod,
			credentials: formData.value.credentials,
			name: formData.value.systemName,
		});

		if (res.success) {
			ElMessage.success('Connection settings updated successfully');
			informParams && emit('updated');
		} else {
			ElMessage.error(res.msg || 'Failed to update connection settings');
		}
	} finally {
		isUpdating.value = false;
	}
};

/**
 * 测试连接（触发事件，由父组件处理）
 */
const handleTestConnection = async () => {
	if (!formRef.value) return;

	try {
		await formRef.value.validate();
		isTesting.value = true;
		// 触发事件，由父组件调用 test 接口
		await handleUpdate(false);
		emit('test');
	} catch (error) {
		console.error('Form validation failed:', error);
	} finally {
		setTimeout(() => {
			isTesting.value = false;
		}, 1000);
	}
};

// 监听 props 变化
watch(
	() => [props.integrationId, props.connectionData],
	() => {
		initFormData();
	},
	{ immediate: true, deep: true }
);

// 初始化
onMounted(() => {
	initFormData();
});
</script>
