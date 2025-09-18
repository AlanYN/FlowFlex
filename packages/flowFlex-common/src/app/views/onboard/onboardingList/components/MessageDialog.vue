<template>
	<el-dialog
		v-model="dialogVisible"
		title="Send Message"
		width="600px"
		:before-close="handleClose"
	>
		<el-form ref="messageFormRef" :model="messageForm" label-width="100px" class="space-y-4">
			<el-form-item label="To" required>
				<el-select
					v-model="messageForm.recipients"
					multiple
					placeholder="Select recipients"
					class="w-full"
				>
					<el-option
						v-for="user in users"
						:key="user.id"
						:label="user.name"
						:value="user.email"
					>
						<div class="flex justify-between">
							<span>{{ user.name }}</span>
							<span class="text-sm text-gray-500">{{ user.email }}</span>
						</div>
					</el-option>
				</el-select>
			</el-form-item>

			<el-form-item label="Subject" required>
				<el-input
					v-model="messageForm.subject"
					placeholder="Enter message subject"
					class="w-full"
				/>
			</el-form-item>

			<el-form-item label="Message" required>
				<el-input
					v-model="messageForm.message"
					type="textarea"
					:rows="6"
					placeholder="Enter your message..."
					class="w-full"
				/>
			</el-form-item>

			<el-form-item label="Priority">
				<el-radio-group v-model="messageForm.priority">
					<el-radio label="High">High</el-radio>
					<el-radio label="Normal">Normal</el-radio>
					<el-radio label="Low">Low</el-radio>
				</el-radio-group>
			</el-form-item>

			<el-form-item label="Type">
				<el-radio-group v-model="messageForm.type">
					<el-radio label="notification">Notification</el-radio>
					<el-radio label="reminder">Reminder</el-radio>
					<el-radio label="update">Update</el-radio>
				</el-radio-group>
			</el-form-item>
		</el-form>

		<template #footer>
			<div class="flex justify-end space-x-2">
				<el-button @click="handleClose">Cancel</el-button>
				<el-button
					type="primary"
					@click="handleSend"
					:loading="sending"
					:disabled="!isFormValid"
				>
					Send Message
				</el-button>
			</div>
		</template>
	</el-dialog>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch } from 'vue';
import { ElMessage } from 'element-plus';
import { OnboardingItem } from '#/onboard';

// 组件属性
interface Props {
	modelValue: boolean;
	onboardingData?: OnboardingItem | null;
}

const props = defineProps<Props>();

// 组件事件
const emit = defineEmits<{
	'update:modelValue': [value: boolean];
}>();

// 响应式数据
const sending = ref(false);
const messageFormRef = ref();
const messageForm = reactive({
	recipients: [] as string[],
	subject: '',
	message: '',
	priority: 'Normal',
	type: 'notification',
});

// 用户列表
const users = [
	{ id: '1', name: 'John Smith', email: 'john.smith@example.com', role: 'Sales Manager' },
	{
		id: '2',
		name: 'Sarah Williams',
		email: 'sarah.williams@example.com',
		role: 'Account Manager',
	},
	{
		id: '3',
		name: 'Mike Johnson',
		email: 'mike.johnson@example.com',
		role: 'Operations Manager',
	},
	{ id: '4', name: 'Emily Davis', email: 'emily.davis@example.com', role: 'Customer Success' },
];

// 计算属性
const dialogVisible = computed({
	get: () => props.modelValue,
	set: (value) => emit('update:modelValue', value),
});

const isFormValid = computed(() => {
	return (
		messageForm.recipients.length > 0 &&
		messageForm.subject.trim() !== '' &&
		messageForm.message.trim() !== ''
	);
});

// 监听onboarding数据变化，自动填充主题
watch(
	() => props.onboardingData,
	(newData) => {
		if (newData) {
			messageForm.subject = `Onboarding Update: ${newData.leadName} - ${newData.currentStageName}`;
		}
	},
	{ immediate: true }
);

// 事件处理函数
const handleClose = () => {
	dialogVisible.value = false;
	resetForm();
};

const resetForm = () => {
	messageForm.recipients = [];
	messageForm.subject = '';
	messageForm.message = '';
	messageForm.priority = 'Normal';
	messageForm.type = 'notification';
};

const handleSend = async () => {
	if (!isFormValid.value) {
		ElMessage.warning('Please fill in all required fields');
		return;
	}

	try {
		sending.value = true;

		// 模拟发送消息的API调用
		await new Promise((resolve) => setTimeout(resolve, 1000));

		ElMessage.success('Message sent successfully');
		handleClose();
	} catch (error) {
		console.error('Failed to send message:', error);
		ElMessage.error('Failed to send message');
	} finally {
		sending.value = false;
	}
};
</script>

<style scoped lang="scss">
:deep(.el-dialog__header) {
	border-bottom: 1px solid #e0e7ff;
	padding: 20px;
}

:deep(.el-dialog__body) {
	padding: 20px;
}

:deep(.el-dialog__footer) {
	background-color: #f8fafc;
	border-top: 1px solid #e5e7eb;
	padding: 16px 20px;
}

:deep(.el-form-item) {
	margin-bottom: 20px;
}

/* 暗色主题 */
html.dark {
	:deep(.el-dialog) {
		background-color: var(--black-400) !important;
		border: 1px solid var(--black-200) !important;
	}

	:deep(.el-dialog__header) {
		background-color: #003c76 !important;
		border-bottom: 1px solid #00509d !important;
		color: #cce8d0 !important;
	}

	:deep(.el-dialog__body) {
		background-color: var(--black-400) !important;
		color: var(--white-100) !important;
	}

	:deep(.el-dialog__footer) {
		background-color: var(--black-200) !important;
		border-top: 1px solid var(--black-100) !important;
	}

	:deep(.el-input__wrapper) {
		background-color: var(--black-200) !important;
		border: 1px solid var(--black-200) !important;
	}

	:deep(.el-input__inner),
	:deep(.el-textarea__inner) {
		@apply text-white-100;
		background-color: var(--black-200) !important;
	}

	:deep(.el-select .el-input__wrapper) {
		background-color: var(--black-200) !important;
	}
}
</style>
