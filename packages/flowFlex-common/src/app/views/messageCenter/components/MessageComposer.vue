<template>
	<el-dialog
		v-model="visible"
		title="New Message"
		:width="bigDialogWidth"
		:before-close="handleClose"
		class="message-composer-dialog"
	>
		<!-- Description -->
		<div class="text-sm text-gray-500 dark:text-gray-400 mb-6">
			Compose a new message to send to team members or customers.
		</div>

		<!-- Message Type Tabs -->
		<PrototypeTabs v-model="messageType" :tabs="messageTypeTabs" class="mb-6">
			<!-- Internal Message -->
			<TabPane value="internal">
				<el-form :model="form" label-position="top" @submit.prevent="handleSend">
					<el-form-item label="Recipient">
						<el-select
							v-model="form.recipient"
							placeholder="John Smith"
							class="w-full"
							filterable
						>
							<el-option label="John Smith" value="john.smith" />
							<el-option label="Jane Doe" value="jane.doe" />
						</el-select>
					</el-form-item>

					<el-form-item label="Subject">
						<el-input v-model="form.subject" placeholder="Enter message subject" />
					</el-form-item>

					<el-form-item label="Related To">
						<el-select
							v-model="form.relatedTo"
							placeholder="Select related lead (optional)"
							class="w-full"
							filterable
							clearable
						>
							<el-option label="Lead 1" value="lead1" />
							<el-option label="Lead 2" value="lead2" />
						</el-select>
					</el-form-item>

					<el-form-item label="Message" class="w-full">
						<RichTextEditor
							v-model="form.body"
							placeholder="Type your message here..."
							min-height="200px"
							max-height="300px"
						/>
					</el-form-item>

					<el-form-item label="Attachments">
						<el-upload
							ref="uploadRef"
							class="w-full"
							drag
							:auto-upload="false"
							:on-change="handleFileChange"
							:on-remove="handleFileRemove"
							:file-list="fileList"
							multiple
							:limit="10"
						>
							<div class="flex flex-col items-center justify-center py-6">
								<Icon
									icon="lucide-paperclip"
									class="w-10 h-10 text-gray-400 mb-3"
								/>
								<div class="text-sm text-gray-600 dark:text-gray-300">
									Click to upload or drag and drop files
								</div>
							</div>
						</el-upload>
					</el-form-item>
				</el-form>
			</TabPane>

			<!-- Customer Email -->
			<TabPane value="customer">
				<el-form :model="form" label-position="top" @submit.prevent="handleSend">
					<el-form-item label="Customer Email">
						<el-select
							v-model="form.customerEmail"
							placeholder="Select customer"
							class="w-full"
							filterable
						>
							<el-option label="customer@example.com" value="customer@example.com" />
						</el-select>
					</el-form-item>

					<el-form-item label="Subject">
						<el-input v-model="form.subject" placeholder="Enter message subject" />
					</el-form-item>

					<el-form-item label="Related To">
						<el-select
							v-model="form.relatedTo"
							placeholder="Select related lead (optional)"
							class="w-full"
							filterable
							clearable
						>
							<el-option label="Lead 1" value="lead1" />
							<el-option label="Lead 2" value="lead2" />
						</el-select>
					</el-form-item>

					<el-form-item label="Message" class="w-full">
						<RichTextEditor
							v-model="form.body"
							placeholder="Type your message here..."
							min-height="200px"
							max-height="300px"
						/>
					</el-form-item>

					<el-form-item label="Attachments">
						<el-upload
							ref="uploadRef"
							class="w-full"
							drag
							:auto-upload="false"
							:on-change="handleFileChange"
							:on-remove="handleFileRemove"
							:file-list="fileList"
							multiple
							:limit="10"
						>
							<div class="flex flex-col items-center justify-center py-6">
								<Icon
									icon="lucide-paperclip"
									class="w-10 h-10 text-gray-400 mb-3"
								/>
								<div class="text-sm text-gray-600 dark:text-gray-300">
									Click to upload or drag and drop files
								</div>
							</div>
						</el-upload>
					</el-form-item>
				</el-form>
			</TabPane>

			<!-- Portal Message -->
			<TabPane value="portal">
				<el-form :model="form" label-position="top" @submit.prevent="handleSend">
					<el-form-item label="Customer Portal">
						<el-select
							v-model="form.customerPortal"
							placeholder="Select portal"
							class="w-full"
							filterable
						>
							<el-option label="Portal 1" value="portal1" />
							<el-option label="Portal 2" value="portal2" />
						</el-select>
					</el-form-item>

					<el-form-item label="Subject">
						<el-input v-model="form.subject" placeholder="Enter message subject" />
					</el-form-item>

					<el-form-item label="Related To">
						<el-select
							v-model="form.relatedTo"
							placeholder="Select related lead (optional)"
							class="w-full"
							filterable
							clearable
						>
							<el-option label="Lead 1" value="lead1" />
							<el-option label="Lead 2" value="lead2" />
						</el-select>
					</el-form-item>

					<el-form-item label="Message" class="w-full">
						<RichTextEditor
							v-model="form.body"
							placeholder="Type your message here..."
							min-height="200px"
							max-height="300px"
						/>
					</el-form-item>

					<el-form-item label="Attachments">
						<el-upload
							ref="uploadRef"
							class="w-full"
							drag
							:auto-upload="false"
							:on-change="handleFileChange"
							:on-remove="handleFileRemove"
							:file-list="fileList"
							multiple
							:limit="10"
						>
							<div class="flex flex-col items-center justify-center py-6">
								<Icon
									icon="lucide-paperclip"
									class="w-10 h-10 text-gray-400 mb-3"
								/>
								<div class="text-sm text-gray-600 dark:text-gray-300">
									Click to upload or drag and drop files
								</div>
							</div>
						</el-upload>
					</el-form-item>
				</el-form>
			</TabPane>
		</PrototypeTabs>

		<template #footer>
			<div class="flex justify-end gap-3">
				<el-button @click="handleClose" size="large">Cancel</el-button>
				<el-button type="primary" @click="handleSend" size="large">
					<Icon icon="lucide-send" class="w-4 h-4 mr-2" />
					Send Message
				</el-button>
			</div>
		</template>
	</el-dialog>
</template>

<script lang="ts" setup>
import { ref, computed, watch, useTemplateRef } from 'vue';
import { ElMessage, type UploadFile, type UploadUserFile } from 'element-plus';
import RichTextEditor from '@/components/RichTextEditor/index.vue';
import { PrototypeTabs, TabPane } from '@/components/PrototypeTabs';
import { bigDialogWidth } from '@/settings/projectSetting';

interface Props {
	modelValue: boolean;
}

const props = defineProps<Props>();

const emit = defineEmits<{
	'update:modelValue': [value: boolean];
	send: [data: ComposerFormData];
}>();

type MessageType = 'internal' | 'customer' | 'portal';

interface ComposerFormData {
	messageType?: MessageType;
	recipient?: string;
	customerEmail?: string;
	customerPortal?: string;
	subject: string;
	relatedTo?: string;
	body: string;
	attachments: File[];
}

const visible = computed({
	get: () => props.modelValue,
	set: (value) => emit('update:modelValue', value),
});

const messageType = ref<MessageType>('internal');

const messageTypeTabs = [
	{
		value: 'internal',
		label: 'Internal Message',
		icon: 'lucide-message-square',
	},
	{
		value: 'customer',
		label: 'Customer Email',
		icon: 'lucide-at-sign',
	},
	{
		value: 'portal',
		label: 'Portal Message',
		icon: 'lucide-layout-dashboard',
	},
];

const form = ref<ComposerFormData>({
	subject: '',
	body: '',
	attachments: [],
});

const uploadRef = useTemplateRef('uploadRef');
const fileList = ref<UploadUserFile[]>([]);

// Reset form when dialog closes
watch(visible, (isVisible) => {
	if (!isVisible) {
		setTimeout(() => {
			messageType.value = 'internal';
			form.value = {
				subject: '',
				body: '',
				attachments: [],
			};
			fileList.value = [];
		}, 300);
	}
});

const handleClose = () => {
	visible.value = false;
};

const handleSend = () => {
	// Validate based on message type
	if (messageType.value === 'internal' && !form.value.recipient) {
		ElMessage.error('Please select a recipient');
		return;
	}
	if (messageType.value === 'customer' && !form.value.customerEmail) {
		ElMessage.error('Please select a customer email');
		return;
	}
	if (messageType.value === 'portal' && !form.value.customerPortal) {
		ElMessage.error('Please select a customer portal');
		return;
	}
	if (!form.value.subject.trim()) {
		ElMessage.error('Please enter subject');
		return;
	}
	if (!form.value.body.trim()) {
		ElMessage.error('Please enter message body');
		return;
	}

	emit('send', { ...form.value, messageType: messageType.value });
	ElMessage.success('Message sent successfully');
	visible.value = false;
};

const handleFileChange = (file: UploadFile, files: UploadFile[]) => {
	fileList.value = files;
	form.value.attachments = files.map((f) => f.raw as File).filter(Boolean);
};

const handleFileRemove = (file: UploadFile, files: UploadFile[]) => {
	fileList.value = files;
	form.value.attachments = files.map((f) => f.raw as File).filter(Boolean);
};
</script>

<style scoped></style>

<style scoped lang="scss">
.message-composer-dialog {
	:deep(.el-dialog__body) {
		padding-top: 20px;
	}
}
</style>
