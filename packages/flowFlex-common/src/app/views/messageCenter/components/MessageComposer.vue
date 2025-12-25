<template>
	<el-dialog
		v-model="visible"
		title="New Message"
		:width="moreDialogWidth"
		:before-close="handleClose"
		class="message-composer-dialog"
		draggable
		append-to-body
		destroy-on-close
	>
		<!-- Description -->
		<div class="text-sm text-gray-500 dark:text-gray-400 mb-6">
			Compose a new message to send to team members or customers.
		</div>

		<!-- Message Type Tabs -->
		<PrototypeTabs v-model="messageType" :tabs="messageTypeTabs" class="mb-6">
			<!-- Internal Message -->
			<TabPane :value="MessageType.Internal">
				<el-form :model="form" label-position="top" @submit.prevent="handleSend">
					<el-form-item label="Recipient">
						<FlowflexUserSelect
							v-model="selectedRecipient"
							placeholder="Select default assignee"
							:multiple="false"
							:clearable="true"
							selection-type="user"
							showEmail
							is-show-admin-user
							@change="selectRecipient"
						/>
					</el-form-item>

					<!-- Common Form Fields -->
					<MessageFormFields
						v-model:subject="form.subject"
						v-model:body="form.body"
						v-model:relatedTo="selectedRelatedTo"
						:upload-progress="uploadProgress"
						:uploaded-attachments="uploadedAttachments"
						@file-change="handleFileChange"
						@remove-file="handleRemoveUploadedFile"
					/>
				</el-form>
			</TabPane>

			<!-- Customer Email -->
			<TabPane :value="MessageType.Email">
				<el-form :model="form" label-position="top" @submit.prevent="handleSend">
					<el-form-item label="Customer Email">
						<div class="flex w-full items-center userSelect">
							<el-select
								v-model="customerEmailTags"
								multiple
								filterable
								allow-create
								default-first-option
								:reserve-keyword="false"
								placeholder="Enter email addresses"
								class="flex-1"
								collapse-tags
								collapse-tags-tooltip
								:max-collapse-tags="5"
								clearable
								@change="handleCustomerEmailTagsChange"
							/>
							<FlowflexUserSelect
								v-model="tempSelectedUsers"
								:multiple="true"
								selection-type="user"
								showEmail
								is-show-admin-user
								trigger-only
								class="!w-auto -mt-[2px]"
								@change="handleUserSelectChange"
							>
								<el-button>
									<Icon icon="lucide-users" />
								</el-button>
							</FlowflexUserSelect>
						</div>
					</el-form-item>

					<!-- Common Form Fields -->
					<MessageFormFields
						v-model:subject="form.subject"
						v-model:body="form.body"
						v-model:relatedTo="selectedRelatedTo"
						:upload-progress="uploadProgress"
						:uploaded-attachments="uploadedAttachments"
						@file-change="handleFileChange"
						@remove-file="handleRemoveUploadedFile"
					/>
				</el-form>
			</TabPane>

			<!-- Portal Message -->
			<TabPane :value="MessageType.Portal">
				<el-form :model="form" label-position="top" @submit.prevent="handleSend">
					<el-form-item label="Customer Portal">
						<el-select
							v-model="selectedCustomerPortal"
							placeholder="Select portal"
							class="w-full"
							filterable
						/>
					</el-form-item>

					<!-- Common Form Fields -->
					<MessageFormFields
						v-model:subject="form.subject"
						v-model:body="form.body"
						v-model:relatedTo="selectedRelatedTo"
						:upload-progress="uploadProgress"
						:uploaded-attachments="uploadedAttachments"
						@file-change="handleFileChange"
						@remove-file="handleRemoveUploadedFile"
					/>
				</el-form>
			</TabPane>
		</PrototypeTabs>

		<template #footer>
			<div class="flex justify-end gap-3">
				<el-button @click="handleClose">Cancel</el-button>
				<el-button
					type="primary"
					@click="handleSend"
					:loading="sendLoading"
					:disabled="uploadingCount > 0"
				>
					<Icon icon="lucide-send" class="w-4 h-4 mr-2" />
					{{
						uploadingCount > 0
							? `Uploading ${uploadingCount} file(s)...`
							: 'Send Message'
					}}
				</el-button>
			</div>
		</template>
	</el-dialog>
</template>

<script lang="ts" setup>
import { ref, computed } from 'vue';
import { ElMessage } from 'element-plus';
import type { UploadFile, UploadUserFile } from 'element-plus';
import { PrototypeTabs, TabPane } from '@/components/PrototypeTabs';
import { moreDialogWidth, projectTenMinutesSsecondsDate } from '@/settings/projectSetting';
import MessageFormFields from './MessageFormFields.vue';
import { MessageType } from '@/enums/appEnum';

import { MessageCenterForm, MessageInfo } from '#/message';
import { sendMessageCenter, uploadMessageFile } from '@/apis/messageCenter';
import FlowflexUserSelect from '@/components/form/flowflexUser/index.vue';
import { Icon } from '@iconify/vue';
import { timeZoneConvert } from '@/hooks/time';

import { FlowflexUser } from '#/golbal';

interface Props {
	isBinding: string;
}

const props = defineProps<Props>();

const emit = defineEmits<{
	send: [data: MessageCenterForm];
}>();

const forwardBodyTemplate = (originalMessage: MessageInfo) => {
	return `
		<pre>---------- Forwarded Message ----------<br />From: ${originalMessage.senderEmail}<${
			originalMessage.senderEmail
		}><br />Date: ${timeZoneConvert(
			originalMessage.sentDate,
			false,
			projectTenMinutesSsecondsDate
		)}<br />To: ${originalMessage.recipients
			.map((item) => item.email)
			.join(',')}<br />Subject: ${originalMessage.subject}</pre><br />
	
		${originalMessage.body}
	`;
};

const replyBodyTemplate = (originalMessage: MessageInfo) => {
	return `
		<pre>${originalMessage.senderEmail}<${originalMessage.senderEmail}> on ${timeZoneConvert(
			originalMessage.sentDate,
			false,
			projectTenMinutesSsecondsDate
		)} wrote:</pre>
		<br />
		${originalMessage.body}
	`;
};

const visible = ref(false);
const openVisible = (originalMessage?: MessageInfo, isReply: boolean = false) => {
	if (originalMessage) {
		const messageUser = isReply
			? originalMessage.senderEmail
				? [originalMessage.senderEmail]
				: originalMessage.recipients.map((item) => item.userId)
			: [];
		selectedRecipient.value = messageUser;
		form.value.body = isReply
			? replyBodyTemplate(originalMessage)
			: forwardBodyTemplate(originalMessage);
		form.value.subject = `${isReply ? 'Re: ' : 'Fwd: '}${originalMessage.subject}`;
		messageType.value = `${originalMessage.messageType}` as MessageType;
		uploadedAttachments.value = originalMessage.attachments;

		if (messageType.value === MessageType.Internal && selectedRecipient.value) {
			InternalRecipients.value = originalMessage.recipients;
		} else if (messageType.value === MessageType.Email) {
			// 回复/转发邮件时，设置收件人
			if (isReply && originalMessage.senderEmail) {
				// 回复时，将发件人邮箱添加到标签
				customerEmailTags.value = [originalMessage.senderEmail];
				handleCustomerEmailTagsChange(customerEmailTags.value);
			} else if (originalMessage.recipients?.length) {
				// 转发时，清空收件人
				customerEmailTags.value = [];
				CustomerRecipients.value = [];
			}
		}
	}
	visible.value = true;
};

const messageType = ref<MessageType>(MessageType.Internal);

const messageTypeTabs = computed(() => {
	return props.isBinding
		? [
				{
					value: MessageType.Internal,
					label: 'Internal Message',
					icon: 'lucide-message-square',
				},
				{
					value: MessageType.Email,
					label: 'Customer Email',
					icon: 'lucide-at-sign',
				},
				{
					value: MessageType.Portal,
					label: 'Portal Message',
					icon: 'lucide-layout-dashboard',
				},
		  ]
		: [
				{
					value: MessageType.Internal,
					label: 'Internal Message',
					icon: 'lucide-message-square',
				},
				{
					value: MessageType.Portal,
					label: 'Portal Message',
					icon: 'lucide-layout-dashboard',
				},
		  ];
});

const form = ref<MessageCenterForm>({
	subject: '',
	body: '',
	recipients: [],
	ccRecipients: [],
	bccRecipients: [],
	labels: [],
	relatedEntityType: '',
	relatedEntityId: 0,
	relatedEntityCode: '',
	portalId: null,
	attachments: [],
});

// UI-specific fields that map to the form
const selectedRecipient = ref<string[]>([]);
const selectedCustomerEmail = ref<string>('');
const customerEmailTags = ref<string[]>([]); // 邮箱标签
const selectedCustomerPortal = ref<string>('');
const selectedRelatedTo = ref<string>('');

// 用户选择相关
const tempSelectedUsers = ref<string[]>([]);
// 存储已选择的系统用户信息（用于同步 tempSelectedUsers）
const selectedSystemUsers = ref<Map<string, FlowflexUser>>(new Map());

// 处理用户选择变化（FlowflexUserSelect 的 change 事件）
const handleUserSelectChange = (
	value?: string | string[],
	userList?: FlowflexUser | FlowflexUser[]
) => {
	// 获取之前选择的系统用户邮箱列表
	const previousSystemEmails = Array.from(selectedSystemUsers.value.values())
		.map((u) => u.email)
		.filter(Boolean);

	if (!userList) {
		// 如果清空了选择，同步清空相关数据
		tempSelectedUsers.value = [];
		// 从 customerEmailTags 中移除系统用户的邮箱
		customerEmailTags.value = customerEmailTags.value.filter(
			(email) => !previousSystemEmails.includes(email)
		);
		selectedSystemUsers.value.clear();
		handleCustomerEmailTagsChange(customerEmailTags.value);
		return;
	}

	const users = Array.isArray(userList) ? userList : [userList];
	const selectedIds = Array.isArray(value) ? value : value ? [value] : [];

	// 过滤出有邮箱的用户
	const usersWithEmail = users
		.filter((user) => selectedIds.includes(user.id))
		.filter((user) => user.email && user.email.trim());

	// 更新已选择的系统用户映射（只保留有邮箱的用户）
	selectedSystemUsers.value.clear();
	usersWithEmail.forEach((user) => {
		selectedSystemUsers.value.set(user.id, user);
	});

	// 同步更新 tempSelectedUsers（只保留有邮箱的用户ID）
	tempSelectedUsers.value = usersWithEmail.map((user) => user.id);

	// 获取新选择的系统用户邮箱
	const newSystemUserEmails = usersWithEmail.map((u) => u.email);

	// 获取手动输入的邮箱（不在之前系统用户邮箱列表中的）
	const manualEmails = customerEmailTags.value.filter(
		(email) => !previousSystemEmails.includes(email)
	);

	// 合并新的系统用户邮箱和手动输入的邮箱
	customerEmailTags.value = [...new Set([...newSystemUserEmails, ...manualEmails])];

	// 同步更新 CustomerRecipients
	handleCustomerEmailTagsChange(customerEmailTags.value);
};

// 处理邮箱标签变化（用户手动删除标签时）
const handleCustomerEmailTagsChange = (emails: string[]) => {
	// 验证并过滤无效邮箱
	const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
	const validEmails = emails.filter((email) => emailRegex.test(email?.trim()));

	if (validEmails.length !== emails.length) {
		// 有无效邮箱，更新标签列表只保留有效的
		customerEmailTags.value = validEmails;
	}

	// 同步 tempSelectedUsers：移除不在 validEmails 中的系统用户
	const removedUserIds: string[] = [];
	selectedSystemUsers.value.forEach((user, id) => {
		if (!validEmails.includes(user.email)) {
			removedUserIds.push(id);
		}
	});
	removedUserIds.forEach((id) => selectedSystemUsers.value.delete(id));
	tempSelectedUsers.value = Array.from(selectedSystemUsers.value.keys());

	// 更新 CustomerRecipients
	CustomerRecipients.value = validEmails.map((email) => {
		// 查找是否是系统用户
		const systemUser = Array.from(selectedSystemUsers.value.values()).find(
			(u) => u.email === email
		);
		if (systemUser) {
			return {
				userId: systemUser.id,
				name: systemUser.name,
				email: systemUser.email,
			};
		}
		// 手动输入的邮箱
		return {
			userId: null as any,
			name: null as any,
			email: email?.trim(),
		};
	});
};

const fileList = ref<UploadUserFile[]>([]);
// 存储已上传文件的ID信息
const uploadedAttachments = ref<
	{
		id: string;
		fileName: string;
		fileSize: number;
		contentType: string;
	}[]
>([]);
// 上传进度列表
const uploadProgress = ref<
	{
		uid: string;
		name: string;
		percentage: number;
		error?: string;
	}[]
>([]);
// 跟踪正在上传的文件数量
const uploadingCount = ref(0);
// 存储上传请求的取消函数
const uploadCancelTokens = ref<Map<number, () => void>>(new Map());

const handleClose = () => {
	// 如果有文件正在上传,取消所有上传
	if (uploadingCount.value > 0) {
		uploadCancelTokens.value.forEach((cancel) => {
			cancel();
		});
		uploadCancelTokens.value.clear();
		uploadingCount.value = 0;
	}
	visible.value = false;
	initFormData();
};

const initFormData = () => {
	messageType.value = MessageType.Internal;
	form.value = {
		subject: '',
		body: '',
		recipients: [],
		ccRecipients: [],
		bccRecipients: [],
		labels: [],
		relatedEntityType: '',
		relatedEntityId: 0,
		relatedEntityCode: '',
		portalId: null,
		attachments: [],
	};
	selectedRecipient.value = [];
	selectedCustomerEmail.value = '';
	customerEmailTags.value = [];
	tempSelectedUsers.value = [];
	selectedSystemUsers.value.clear();
	selectedCustomerPortal.value = '';
	selectedRelatedTo.value = '';
	fileList.value = [];
	uploadedAttachments.value = [];
	uploadProgress.value = [];
	uploadingCount.value = 0;
	uploadCancelTokens.value.clear();
	InternalRecipients.value = [];
	CustomerRecipients.value = [];
};

const sendLoading = ref(false);
const handleSend = async () => {
	// Validate based on message type
	if (messageType.value === MessageType.Internal && !selectedRecipient.value) {
		ElMessage.error('Please select a recipient');
		return;
	}
	if (messageType.value === MessageType.Email && !CustomerRecipients.value.length) {
		ElMessage.error('Please select or enter at least one recipient email');
		return;
	}
	if (messageType.value === MessageType.Portal && !selectedCustomerPortal.value) {
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

	// Map UI fields to form structure based on message type
	if (messageType.value === MessageType.Internal && selectedRecipient.value) {
		form.value.recipients = InternalRecipients.value as any;
	} else if (messageType.value === MessageType.Email) {
		form.value.recipients = CustomerRecipients.value as any;
	} else if (messageType.value === MessageType.Portal && selectedCustomerPortal.value) {
		form.value.portalId = null;
	}

	// Map relatedTo to relatedEntity fields
	if (selectedRelatedTo.value) {
		form.value.relatedEntityType = 'lead';
		form.value.relatedEntityId = 0;
		form.value.relatedEntityCode = selectedRelatedTo.value;
	}
	try {
		sendLoading.value = true;

		// 提取附件ID列表
		const attachmentIds = uploadedAttachments.value.map((att) => att.id);

		const res = await sendMessageCenter({
			...form.value,
			messageType: messageType.value,
			attachmentIds: attachmentIds,
		});
		if (res.code == '200') {
			ElMessage.success('Message sent successfully');
			emit('send', form.value);
			handleClose();
		}
	} finally {
		sendLoading.value = false;
	}
};

const handleFileChange = async (file: UploadFile) => {
	// 上传新添加的文件
	if (file.raw) {
		// 添加到进度列表
		uploadProgress.value.push({
			uid: String(file.uid),
			name: file.name,
			percentage: 0,
		});

		// 创建 AbortController 用于取消上传
		const abortController = new AbortController();
		uploadCancelTokens.value.set(file.uid, () => abortController.abort());
		uploadingCount.value++;

		try {
			// 构建上传参数,参考 Documents.vue 的实现
			const uploadParams = {
				name: 'file',
				file: file.raw,
				filename: file.raw.name,
			};

			// 调用上传接口,传递进度回调
			const res = await uploadMessageFile(uploadParams, (progressEvent: any) => {
				// 实时更新上传进度
				const existingIndex = uploadProgress.value.findIndex(
					(p) => p.uid === String(file.uid)
				);
				if (existingIndex >= 0 && progressEvent.total > 0) {
					uploadProgress.value[existingIndex].percentage = Math.round(
						(progressEvent.loaded * 100) / progressEvent.total
					);
				}
			});

			// 从进度列表中移除
			uploadProgress.value = uploadProgress.value.filter((p) => p.uid !== String(file.uid));
			const responseData = res?.data;
			if (responseData.code === '200' && responseData.data) {
				// 上传成功,保存文件信息
				uploadedAttachments.value.push({
					id: responseData.data.id,
					fileName: responseData.data.fileName || file.name,
					fileSize: responseData.data.fileSize || file.size || 0,
					contentType: responseData.data.contentType || file.raw.type,
				});
				ElMessage.success(`${file.name} uploaded successfully`);
			} else {
				// 上传失败
				responseData?.msg && ElMessage.error(responseData?.msg);
			}
		} catch (error: any) {
			// 从进度列表中移除
			uploadProgress.value = uploadProgress.value.filter((p) => p.uid !== String(file.uid));

			// 检查是否是用户取消的上传
			if (error.name === 'AbortError' || error.name === 'CanceledError') {
				console.log('File upload cancelled:', file.name);
			} else {
				console.error('File upload error:', error);
				ElMessage.error(`Failed to upload ${file.name}`);
			}
		} finally {
			// 清理
			uploadCancelTokens.value.delete(file.uid);
			uploadingCount.value--;
		}
	}
};

// 移除已上传的文件
const handleRemoveUploadedFile = (fileId: string) => {
	const index = uploadedAttachments.value.findIndex((att) => att.id === fileId);
	if (index > -1) {
		uploadedAttachments.value.splice(index, 1);
	}
};

const InternalRecipients = ref<
	{
		userId: string | null;
		name: string | null;
		email: string;
	}[]
>([]);
const CustomerRecipients = ref<
	{
		userId: string | null;
		name: string | null;
		email: string;
	}[]
>([]);

const selectRecipient = (value?: string | string[], userList?: FlowflexUser | FlowflexUser[]) => {
	let arr = [] as {
		userId: string;
		name: string;
		email: string;
	}[];
	if (value && Array.isArray(value) && userList && Array.isArray(userList)) {
		arr = userList
			.filter((user) => value.includes(user.id))
			.map((item) => {
				return {
					userId: item.id,
					name: item.name,
					email: item.email,
				};
			});
	} else if (value && Array.isArray(value) && userList && !Array.isArray(userList)) {
		arr = [
			{
				userId: userList.id,
				name: userList.name,
				email: userList.email,
			},
		];
	} else {
		arr = [];
	}
	if (messageType.value === MessageType.Internal) {
		InternalRecipients.value = arr;
	} else {
		CustomerRecipients.value = arr;
	}
};

defineExpose({
	openVisible,
});
</script>

<style scoped lang="scss">
.userSelect {
	:deep(.el-select__wrapper) {
		border-top-right-radius: 0;
		border-bottom-right-radius: 0;
	}
	.el-button {
		border-top-left-radius: 0;
		border-bottom-left-radius: 0;
	}
}
</style>
