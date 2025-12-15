<template>
	<div>
		<PageHeader
			title="Message Center"
			description="Manage and organize all your communications in one place"
		>
			<template #actions>
				<el-button
					:icon="Plus"
					type="primary"
					class="page-header-btn page-header-btn-secondary"
					@click="handleCompose"
				>
					New Message
				</el-button>
			</template>
		</PageHeader>
		<div class="grid grid-cols-12 gap-6">
			<div class="col-span-12 md:col-span-3 lg:col-span-2 bg-black-400 p-4 rounded-xl">
				<el-button type="primary" :icon="Plus" class="w-full my-4" @click="handleCompose">
					Compose
				</el-button>
				<div class="flex flex-col gap-y-2">
					<template v-for="item in messageType" :key="item.key">
						<div
							class="flex justify-between items-center p-2 rounded-lg cursor-pointer transition-all duration-200 hover:bg-black-300"
							:class="{
								'text-primary border': selectedType === item.key,
							}"
							@click="handleSelectType(item.key)"
						>
							<div class="flex items-center gap-x-2">
								<Icon :icon="item.icon" />
								<div class="font-medium">{{ item.name }}</div>
							</div>
							<div
								class="px-2 py-0.5 border rounded-md text-xs font-medium"
								:class="
									selectedType === item.key
										? 'text-primary border-primary'
										: 'text-gray-300'
								"
							>
								{{ item.count }}
							</div>
						</div>
					</template>
					<div class="text-gray-400 text-sm mt-4 mb-2 px-2">Labels</div>
					<template v-for="item in messageLabels" :key="item.key">
						<div
							class="flex items-center gap-x-2 p-2 rounded-lg cursor-pointer transition-all duration-200 hover:bg-black-300"
							:class="{
								'bg-black-300': selectedType === item.key,
							}"
							@click="handleSelectLabel(item.key)"
						>
							<div :class="`bg-${item.color}`" class="w-2 h-2 rounded-full"></div>
							<div
								class="font-medium"
								:class="{ 'text-primary': selectedType === item.key }"
							>
								{{ item.name }}
							</div>
						</div>
					</template>
				</div>
			</div>
			<div
				class="col-span-12 bg-black-400 rounded-xl"
				:class="{
					'md:col-span-9 lg:col-span-4': isDetailPanelVisible,
					'md:col-span-9 lg:col-span-10': !isDetailPanelVisible,
				}"
			>
				<div class="flex items-center justify-between p-4">
					<div class="text-xl font-bold capitalize">{{ Message[selectedType] }}</div>
					<el-button circle :icon="Refresh" link />
				</div>
				<div class="flex items-center gap-x-2 my-2 mx-4">
					<el-input
						v-model="searchMessage"
						:prefix-icon="Search"
						placeholder="Search messages..."
					/>
					<el-button :icon="Filter" />
				</div>
				<el-empty :image-size="50" v-if="messageList.length == 0 && !requiredLoading">
					<template #image>
						<Icon icon="lucide-mail" />
					</template>
					<template #description>
						<div class="">
							<h3 class="text-lg">No messages found</h3>
							<div class="text-sm text-gray-400">Your starred folder is empty</div>
						</div>
					</template>
				</el-empty>

				<template v-for="item in messageList" :key="item.id">
					<div
						class="flex items-start p-4 hover:bg-black-300 cursor-pointer transition-colors border-b"
						:class="{
							'bg-black-300 border-primary': selectedMessageId === item.id,
						}"
						@click="handleSelectMessage(item.id)"
					>
						<div class="flex items-start space-x-2 w-full min-w-0">
							<el-button
								:icon="Star"
								link
								size="large"
								class="mt-1"
								:class="{ 'text-yellow-400': item.isStarred }"
							/>
							<div class="flex-1 min-w-0">
								<div class="flex items-center justify-between gap-2">
									<div class="flex items-center space-x-2 min-w-0 flex-1">
										<span
											class="relative flex size-8 shrink-0 overflow-hidden rounded-full h-8 w-8 bg-primary-500 flex items-center justify-center text-white font-medium"
										>
											{{ getInitials(item.from.name) }}
										</span>
										<span class="font-medium truncate min-w-0">
											{{ item.from.name }}
										</span>
									</div>
									<div class="flex items-center space-x-2 shrink-0">
										<el-button
											v-if="item.attachments.length > 0"
											:icon="Link"
											link
										/>
										<span class="text-xs text-gray-400 whitespace-nowrap">
											{{ formatMessageTime(item.timestamp) }}
										</span>
									</div>
								</div>
								<h3 class="text-sm my-2 truncate">{{ item.subject }}</h3>
								<p class="text-xs text-gray-400 truncate">
									{{ item.body }}
								</p>
								<div class="mt-2 flex items-center space-x-2">
									<el-tag
										v-for="label in item.labels"
										:key="label.id"
										:type="getTagType(label.type)"
										size="small"
									>
										{{ label.name }}
									</el-tag>
									<el-tag v-if="item.relatedEntity" type="info" size="small">
										{{ item.relatedEntity.displayName }}
									</el-tag>
								</div>
							</div>
						</div>
					</div>
				</template>
			</div>
			<!-- Message detail panel -->
			<div
				v-if="isDetailPanelVisible && selectedMessage"
				class="col-span-12 md:col-span-12 lg:col-span-6"
			>
				<MessageDetailPanel
					:message="selectedMessage"
					:visible="isDetailPanelVisible"
					@close="handleCloseDetailPanel"
					@reply="handleReply"
					@forward="handleForward"
					@star="handleStar"
					@archive="handleArchive"
					@delete="handleDelete"
					@download-attachment="handleDownloadAttachment"
				/>
			</div>
		</div>

		<!-- Message Composer Dialog -->
		<MessageComposer
			v-model="showComposer"
			:mode="composerMode"
			:original-message="composerOriginalMessage"
			@send="handleSendMessage"
			@save-draft="handleSaveDraft"
		/>
	</div>
</template>

<script lang="ts" setup>
import { onMounted, ref, computed } from 'vue';
import { Message } from '@/enums/appEnum';
import { Filter, Plus, Refresh, Search, Star, Link } from '@element-plus/icons-vue';
import { MessageInfo } from '#/message';

import PageHeader from '@/components/global/PageHeader/index.vue';
import MessageDetailPanel from './components/MessageDetailPanel.vue';
import MessageComposer from './components/MessageComposer.vue';

// 当前选中的消息类型和标签
const selectedType = ref<Message>(Message.Inbox);

const messageType = ref([
	{
		icon: 'lucide-inbox',
		name: 'Inbox',
		key: Message.Inbox,
		count: 0,
	},
	{
		icon: 'lucide-send',
		name: 'Sent',
		key: Message.Sent,
		count: 0,
	},
	{
		icon: 'lucide-star',
		name: 'Starred',
		key: Message.Starred,
		count: 0,
	},
	{
		icon: 'lucide-archive',
		name: 'Archive',
		key: Message.Archive,
		count: 0,
	},
	{
		icon: 'lucide-trash-2',
		name: 'Trash',
		key: Message.Trash,
		count: 0,
	},
]);

const messageLabels = ref([
	{
		color: 'primary-200',
		name: 'Internal',
		key: Message.Internal,
	},
	{
		color: '[var(--el-color-success)]',
		name: 'External',
		key: Message.External,
	},
	{
		color: '[var(--el-color-warning)]',
		name: 'Important',
		key: Message.Important,
	},
	{
		color: 'primary-500',
		name: 'Portal',
		key: Message.Portal,
	},
]);

// Select message type
const handleSelectType = (type: Message) => {
	selectedType.value = type;
	// TODO: Load corresponding message list
	console.log('Selected type:', type);
};

// Select label
const handleSelectLabel = (label: Message) => {
	selectedType.value = label;
	// TODO: Load corresponding message list
	console.log('Selected label:', label);
};

const searchMessage = ref('');
const messageList = ref<MessageInfo[]>([]);
const requiredLoading = ref(false);

// Message selection and detail panel state management
const selectedMessageId = ref<string | null>(null);
const isDetailPanelVisible = ref(false);

// Computed property: get full data of currently selected message
const selectedMessage = computed(() => {
	if (!selectedMessageId.value) return null;
	return messageList.value.find((msg) => msg.id === selectedMessageId.value) || null;
});

// Handle message selection
const handleSelectMessage = (messageId: string) => {
	selectedMessageId.value = messageId;
	isDetailPanelVisible.value = true;
};

// Close detail panel
const handleCloseDetailPanel = () => {
	selectedMessageId.value = null;
	isDetailPanelVisible.value = false;
};

// Get name initials
const getInitials = (name: string): string => {
	if (!name) return '?';
	const parts = name.split(' ');
	if (parts.length >= 2) {
		return (parts[0][0] + parts[1][0]).toUpperCase();
	}
	return name.substring(0, 2).toUpperCase();
};

// Format message time
const formatMessageTime = (timestamp: string | Date): string => {
	const date = typeof timestamp === 'string' ? new Date(timestamp) : timestamp;
	const now = new Date();
	const diff = now.getTime() - date.getTime();
	const days = Math.floor(diff / (1000 * 60 * 60 * 24));

	if (days === 0) return 'Today';
	if (days === 1) return 'Yesterday';
	if (days < 7) return `${days} days ago`;

	return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
};

// Get tag type
const getTagType = (type: Message): 'primary' | 'success' | 'warning' | 'info' => {
	switch (type) {
		case Message.Internal:
			return 'primary';
		case Message.External:
			return 'success';
		case Message.Important:
			return 'warning';
		default:
			return 'info';
	}
};

const getMessageList = () => {
	try {
		requiredLoading.value = true;
		messageList.value = [
			{
				id: '111',
				subject: 'RE: Questionnaire completion',
				from: {
					id: 'user1',
					name: 'Global Logistics',
					email: 'contact@globallogistics.com',
				},
				to: [
					{
						id: 'user2',
						name: 'John Smith',
						email: 'john.smith@example.com',
					},
				],
				body: 'Thank you for the reminder. We have completed the questionnaire survey, please check the detailed report in the attachment.',
				timestamp: new Date(Date.now() - 86400000), // Yesterday
				labels: [
					{
						id: 'label1',
						name: 'Customer',
						color: 'primary',
						type: Message.External,
					},
				],
				relatedEntity: {
					id: 'lead003',
					type: 'LEAD',
					displayName: 'LEAD-003',
				},
				attachments: [
					{
						id: 'att1',
						filename: 'ChangeLog.vue',
						size: '4096',
						mimeType: 'text/plain',
						url: '/attachments/changelog.vue',
					},
				],
				isStarred: false,
				isRead: true,
			},
			{
				id: '222',
				subject: 'Welcome to our onboarding process',
				from: {
					id: 'user3',
					name: 'Dear Manufacturing Pro Team',
					email: 'hr@manufacturing.com',
				},
				to: [
					{
						id: 'user2',
						name: 'John Smith',
						email: 'john.smith@example.com',
					},
				],
				body: 'Welcome to our team! Please review the onboarding documents in the attachment and complete the relevant training this week.',
				bodyHtml:
					'<p>Welcome to our team!</p><p>Please review the onboarding documents in the attachment and complete the relevant training this week.</p>',
				timestamp: new Date(Date.now() - 259200000), // 3 days ago
				labels: [
					{
						id: 'label2',
						name: 'Internal',
						color: 'success',
						type: Message.Internal,
					},
				],
				relatedEntity: {
					id: 'lead005',
					type: 'LEAD',
					displayName: 'LEAD-005',
				},
				attachments: [
					{
						id: 'att2',
						filename: 'Onboarding_Guide.pdf',
						size: '4718592',
						mimeType: 'application/pdf',
						url: '/attachments/onboarding.pdf',
					},
				],
				isStarred: true,
				isRead: false,
			},
			{
				id: '333',
				subject: 'Customer account setup for Healthcare Solutions',
				from: {
					id: 'user4',
					name: 'Hi Emily',
					email: 'emily@healthcare.com',
				},
				to: [
					{
						id: 'user2',
						name: 'John Smith',
						email: 'john.smith@example.com',
					},
				],
				body: 'Hello, we have set up your customer account for Healthcare Solutions. Please use the following credentials to log in to the system.',
				timestamp: new Date(Date.now() - 432000000), // 5 days ago
				labels: [
					{
						id: 'label3',
						name: 'Internal',
						color: 'primary',
						type: Message.Internal,
					},
				],
				relatedEntity: {
					id: 'lead006',
					type: 'LEAD',
					displayName: 'LEAD-006',
				},
				attachments: [],
				isStarred: true,
				isRead: true,
			},
		];
	} finally {
		requiredLoading.value = false;
	}
};

// Message composer state
const showComposer = ref(false);
const composerMode = ref<'compose' | 'reply' | 'forward'>('compose');
const composerOriginalMessage = ref<MessageInfo | null>(null);

// Message action handlers
const handleReply = (messageId: string) => {
	const message = messageList.value.find((msg) => msg.id === messageId);
	if (message) {
		composerOriginalMessage.value = message;
		composerMode.value = 'reply';
		showComposer.value = true;
	}
};

const handleForward = (messageId: string) => {
	const message = messageList.value.find((msg) => msg.id === messageId);
	if (message) {
		composerOriginalMessage.value = message;
		composerMode.value = 'forward';
		showComposer.value = true;
	}
};

const handleStar = (messageId: string) => {
	console.log('Star message:', messageId);
	// TODO: Implement star functionality
};

const handleArchive = (messageId: string) => {
	console.log('Archive message:', messageId);
	// TODO: Implement archive functionality
};

const handleDelete = (messageId: string) => {
	console.log('Delete message:', messageId);
	// TODO: Implement delete functionality
};

const handleDownloadAttachment = (attachmentId: string) => {
	console.log('Download attachment:', attachmentId);
	// TODO: Implement attachment download functionality
};

const handleCompose = () => {
	composerOriginalMessage.value = null;
	composerMode.value = 'compose';
	showComposer.value = true;
};

const handleSendMessage = (data: any) => {
	console.log('Sending message:', data);
	// TODO: Implement send message API call
};

const handleSaveDraft = (data: any) => {
	console.log('Saving draft:', data);
	// TODO: Implement save draft functionality
};

onMounted(() => {
	getMessageList();
});
</script>
