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
					v-permission="ProjectPermissionEnum.messageCenter.create"
				>
					New Message
				</el-button>
			</template>
		</PageHeader>

		<!-- Loading Skeleton -->
		<div v-if="initLoading" class="grid grid-cols-12 gap-6">
			<!-- Sidebar Skeleton -->
			<div class="col-span-12 md:col-span-3 lg:col-span-2 bg-black-400 p-4 rounded-xl">
				<el-skeleton animated>
					<template #template>
						<!-- Compose Button Skeleton -->
						<el-skeleton-item variant="button" class="w-full h-10 mb-4" />
						<!-- Menu Items Skeleton -->
						<div class="space-y-2">
							<el-skeleton-item v-for="i in 5" :key="i" variant="text" class="h-10" />
						</div>
						<!-- Labels Section -->
						<el-skeleton-item variant="text" class="h-6 mt-4 mb-2 w-20" />
						<div class="space-y-2">
							<el-skeleton-item v-for="i in 4" :key="i" variant="text" class="h-8" />
						</div>
					</template>
				</el-skeleton>
			</div>

			<!-- MessageFolder List Skeleton -->
			<div class="col-span-12 md:col-span-9 lg:col-span-10 bg-black-400 rounded-xl p-4">
				<el-skeleton animated>
					<template #template>
						<!-- Header Skeleton -->
						<div class="flex items-center justify-between mb-4">
							<el-skeleton-item variant="h1" class="w-32" />
							<el-skeleton-item variant="circle" class="w-8 h-8" />
						</div>
						<!-- Search Bar Skeleton -->
						<el-skeleton-item variant="text" class="h-10 mb-4" />
						<!-- MessageFolder Items Skeleton -->
						<div class="space-y-4">
							<div v-for="i in 6" :key="i" class="border-b border-gray-700 pb-4">
								<div class="flex items-start space-x-3">
									<el-skeleton-item variant="circle" class="w-8 h-8 mt-1" />
									<div class="flex-1 space-y-2">
										<div class="flex items-center justify-between">
											<el-skeleton-item variant="text" class="w-40" />
											<el-skeleton-item variant="text" class="w-20" />
										</div>
										<el-skeleton-item variant="text" class="w-3/4" />
										<el-skeleton-item variant="text" class="w-full" />
									</div>
								</div>
							</div>
						</div>
					</template>
				</el-skeleton>
			</div>
		</div>

		<!-- MessageFolder Center Content -->
		<div v-if="!initLoading" class="grid grid-cols-12 gap-6 items-start">
			<div
				class="col-span-12 md:col-span-3 lg:col-span-2 bg-black-400 p-4 rounded-xl flex flex-col man-h-0"
			>
				<el-scrollbar class="max-h-full">
					<!-- Fixed Compose Button -->
					<div class="flex-shrink-0 mb-4">
						<el-button
							type="primary"
							:icon="Plus"
							class="w-full"
							@click="handleCompose"
							v-permission="ProjectPermissionEnum.messageCenter.create"
						>
							Compose
						</el-button>
					</div>

					<!-- Scrollable Menu -->

					<div class="flex flex-col gap-y-2">
						<template v-for="item in messageType" :key="item.key">
							<div
								class="flex justify-between items-center p-2 rounded-lg transition-all duration-200"
								:class="{
									'text-primary border': selectedType === item.key,
									'cursor-pointer hover:bg-black-300':
										!requiredLoading && !loadingMore,
									'opacity-50 cursor-not-allowed': requiredLoading || loadingMore,
								}"
								@click="handleSelectType(item.key)"
							>
								<div class="flex items-center gap-x-2">
									<Icon :icon="item.icon" />
									<div class="font-medium">{{ item.name }}</div>
								</div>
								<div
									v-if="item.key == MessageFolder.Inbox"
									class="px-2 py-0.5 border rounded-md text-xs font-medium"
									:class="
										selectedType === item.key
											? 'text-primary border-primary'
											: 'text-gray-300'
									"
								>
									{{ messageCenterCount }}
								</div>
							</div>
						</template>
						<div class="text-gray-400 text-sm pt-4 mb-2 px-2 border-t">Labels</div>
						<template v-for="item in messageLabels" :key="item.key">
							<div
								class="flex items-center gap-x-2 p-2 rounded-lg transition-all duration-200"
								:class="{
									'bg-black-300': selectedTag === item.key,
									'cursor-pointer hover:bg-black-300':
										!requiredLoading && !loadingMore,
									'opacity-50 cursor-not-allowed': requiredLoading || loadingMore,
								}"
								@click="handleSelectLabel(item.key)"
							>
								<div :class="`bg-${item.name}`" class="w-2 h-2 rounded-full"></div>
								<div
									class="font-medium"
									:class="{ 'text-primary': selectedTag === item.key }"
								>
									{{ item.name }}
								</div>
							</div>
						</template>

						<!-- Connected Account -->
						<div class="pt-3 border-t flex items-center justify-between gap-2">
							<template v-if="isBinding">
								<div class="flex items-center gap-2 min-w-0 flex-1">
									<Icon
										icon="lucide-mail-check"
										class="text-primary-500 shrink-0"
									/>
									<span class="text-xs text-gray-400 truncate" :title="isBinding">
										{{ isBinding }}
									</span>
								</div>
								<el-button
									text
									size="small"
									class="text-gray-400 hover:text-red-400 shrink-0 px-1"
									:loading="unbindLoading"
									@click="handleUnbind"
								>
									<Icon icon="lucide-unlink" />
								</el-button>
							</template>
							<template v-else>
								<el-button
									type="primary"
									class="w-full"
									:loading="authLoading"
									:disabled="!authEmailUrl"
									@click="handleConnectEmail"
								>
									<Icon icon="lucide-link" class="mr-2" />
									Connect Email
								</el-button>
							</template>
						</div>
					</div>
				</el-scrollbar>
			</div>
			<div
				class="col-span-12 bg-black-400 rounded-xl flex flex-col max-h-full min-h-0"
				:class="{
					'md:col-span-9 lg:col-span-4': isDetailPanelVisible,
					'md:col-span-9 lg:col-span-10': !isDetailPanelVisible,
				}"
			>
				<!-- <template v-if="!isBinding && selectedType == MessageFolder.Inbox">
					<EmailBindingPrompt
						:auth-url="authEmailUrl"
						:loading="authLoading"
						@connect="handleConnectEmail"
					/>
				</template> -->
				<!-- Fixed Header -->
				<div class="flex-shrink-0">
					<div class="flex items-center justify-between p-4">
						<div class="text-xl font-bold capitalize">
							{{
								selectedType != null
									? MessageFolder[selectedType]
									: selectedTag != null && MessageTag[selectedTag]
							}}
						</div>
						<el-button
							v-permission="ProjectPermissionEnum.messageCenter.read"
							circle
							:icon="Refresh"
							:disabled="requiredLoading"
							:loading="syncMessageLoading"
							link
							@click="syncMessageList"
						/>
					</div>
					<div
						class="flex items-center gap-x-2 my-2 mx-4"
						v-permission="ProjectPermissionEnum.messageCenter.read"
					>
						<el-input
							v-model="searchMessage"
							:prefix-icon="Search"
							placeholder="Search messages..."
							clearable
							:disabled="requiredLoading"
							@keyup.enter="refreshMessageList"
						/>
						<el-button
							:icon="Filter"
							:disabled="requiredLoading"
							@click="refreshMessageList"
						/>
					</div>
				</div>

				<!-- Scrollable MessageFolder List -->
				<el-scrollbar ref="gridContainerRef" @scroll="handleScroll">
					<!-- MessageFolder List Loading Skeleton -->
					<div v-if="requiredLoading && currentPage === 1" class="space-y-4 p-4">
						<div v-for="i in 5" :key="i" class="border-b border-gray-700 pb-4">
							<el-skeleton animated>
								<template #template>
									<div class="flex items-start space-x-3">
										<el-skeleton-item variant="circle" class="w-8 h-8 mt-1" />
										<div class="flex-1 space-y-2">
											<div class="flex items-center justify-between">
												<el-skeleton-item variant="text" class="w-40" />
												<el-skeleton-item variant="text" class="w-20" />
											</div>
											<el-skeleton-item variant="text" class="w-3/4" />
											<el-skeleton-item variant="text" class="w-full" />
										</div>
									</div>
								</template>
							</el-skeleton>
						</div>
					</div>

					<!-- Empty State -->
					<div v-else-if="messageList.length == 0 && !requiredLoading" class="p-4">
						<el-empty :image-size="50">
							<template #image>
								<Icon icon="lucide-mail" />
							</template>
							<template #description>
								<div class="">
									<h3 class="text-lg">No messages found</h3>
									<div class="text-sm text-gray-400">
										{{
											selectedType === MessageFolder.Starred
												? 'Your starred folder is empty'
												: 'No messages in this folder'
										}}
									</div>
								</div>
							</template>
						</el-empty>
					</div>

					<!-- MessageFolder List -->
					<div v-else>
						<div v-for="item in messageList" :key="item.id">
							<div
								class="flex items-start p-4 hover:bg-black-300 cursor-pointer transition-colors border-b"
								:class="{
									'bg-black-500': selectedMessageId === item.id,
								}"
								@click="handleSelectMessage(item.id)"
							>
								<div class="flex items-start space-x-2 w-full min-w-0">
									<el-button
										link
										size="large"
										class="mt-1"
										v-if="
											functionPermission(
												ProjectPermissionEnum.messageCenter.update
											)
										"
										@click.stop="handleStar(item.id, item.isStarred)"
									>
										<el-tooltip
											:content="item.isStarred ? 'Unstar' : 'Star'"
											placement="bottom"
										>
											<Icon
												v-if="starLoadingId == item.id"
												icon="lucide-loader-2"
												class="animate-spin"
											/>
											<Icon
												v-else
												:class="{ 'text-yellow-400': item.isStarred }"
												:icon="
													item.isStarred
														? 'solar:star-bold'
														: 'solar:star-outline'
												"
											/>
										</el-tooltip>
									</el-button>
									<div class="flex-1 min-w-0">
										<div class="flex items-center justify-between gap-2">
											<div class="flex items-center space-x-2 min-w-0 flex-1">
												<span
													class="relative flex size-8 shrink-0 overflow-hidden rounded-full h-8 w-8 flex items-center justify-center text-white font-medium"
													:style="{
														backgroundColor: getAvatarColor(
															item.senderName
														),
													}"
												>
													{{ getInitials(item.senderName) }}
												</span>
												<div class="flex items-center gap-x-2">
													<div
														v-if="!item.isRead"
														class="w-2 h-2 bg-primary rounded-full"
													></div>
													<span class="font-medium truncate min-w-0">
														{{ item.senderName }}
													</span>
												</div>
											</div>
											<div class="flex items-center space-x-2 shrink-0">
												<el-button
													v-if="item.hasAttachments"
													:icon="Link"
													link
												/>
												<span
													class="text-xs text-gray-400 whitespace-nowrap"
												>
													{{ formatMessageTime(item.receivedDate) }}
												</span>
											</div>
										</div>
										<h3 class="text-sm my-2 truncate">
											{{ item.subject }}
										</h3>
										<p class="text-xs text-gray-400 truncate">
											{{ item.bodyPreview }}
										</p>
										<div class="mt-2 flex items-center space-x-2">
											<el-tag type="primary" size="small">
												{{ MessageTypeEnum[item.messageType] }}
											</el-tag>
											<template v-if="item.labels.length > 0">
												<el-tag
													v-for="label in item.labels"
													:key="label"
													size="small"
													:type="getLabelType(label)"
												>
													{{ MessageTag[label] }}
												</el-tag>
											</template>
										</div>
									</div>
								</div>
							</div>
						</div>

						<!-- Loading More Indicator -->
						<div v-if="loadingMore" class="p-4 text-center">
							<Icon icon="lucide-loader-2" class="animate-spin inline-block" />
							<span class="ml-2 text-sm text-gray-400">Loading more messages...</span>
						</div>

						<!-- No More Messages -->
						<div v-if="!hasMore && messageList.length > 0" class="p-4 text-center">
							<span class="text-sm text-gray-400">No more messages</span>
						</div>
					</div>
				</el-scrollbar>
			</div>
			<!-- Email Binding Prompt -->

			<!-- MessageFolder detail panel -->
			<div
				v-if="isDetailPanelVisible && selectedMessageId"
				class="col-span-12 md:col-span-12 lg:col-span-6"
			>
				<MessageDetailPanel
					ref="messageDetail"
					:visible="isDetailPanelVisible"
					:starLoadingId="starLoadingId"
					:archiveLoadingId="archiveLoadingId"
					:unreadLoadingId="unreadLoadingId"
					:attachmentLoadingId="attachmentLoadingId"
					@close="handleCloseDetailPanel"
					@reply="handleReply"
					@forward="handleForward"
					@star="handleStar"
					@archive="handleArchive"
					@delete="handleDelete"
					@unread="handleUnread"
					@download-attachment="handleDownloadAttachment"
				/>
			</div>
		</div>

		<!-- MessageFolder Composer Dialog -->
		<MessageComposer
			ref="messageComposer"
			:isBinding="isBinding"
			@send="handleSendMessage"
			@save-draft="handleSaveDraft"
		/>
	</div>
</template>

<script lang="ts" setup>
import { h, onMounted, ref, useTemplateRef, nextTick } from 'vue';
import { MessageFolder, MessageTag, MessageTypeEnum } from '@/enums/appEnum';
import { Filter, Plus, Refresh, Search, Link } from '@element-plus/icons-vue';
import { MessageList, MessageInfo } from '#/message';
import PageHeader from '@/components/global/PageHeader/index.vue';
import MessageDetailPanel from './components/MessageDetailPanel.vue';
import MessageComposer from './components/MessageComposer.vue';
// import EmailBindingPrompt from './components/EmailBindingPrompt.vue';
import {
	messageCenterList,
	getMessageUnreadCount,
	getEmailAuth,
	getIsBindIng,
	starMessage,
	unstarMessage,
	deleteMessage,
	permanentDeleteMessage,
	archiveMessage,
	unarchiveMessage,
	unreadMessage,
	downLoadFile,
	syncMessage,
	syncMessageFull,
	unbindMessageCenter,
} from '@/apis/messageCenter';
import { Icon } from '@iconify/vue';
import { ElMessageBox } from 'element-plus';
import { useAdaptiveScrollbar } from '@/hooks/useAdaptiveScrollbar';
import { timeZoneConvert } from '@/hooks/time';
import { getAvatarColor } from '@/utils';
import { projectTenMinutesSsecondsDate } from '@/settings/projectSetting';
import { functionPermission } from '@/hooks/index';
import { ProjectPermissionEnum } from '@/enums/permissionEnum';

// Use adaptive scrollbar for the main grid container
const { scrollbarRef: gridContainerRef, updateScrollbarHeight: updateGridHeight } =
	useAdaptiveScrollbar(20);

// 当前选中的消息类型和标签
const selectedType = ref<MessageFolder | null>(MessageFolder.Inbox);
const selectedTag = ref<MessageTag | null>();

const messageType = ref([
	{
		icon: 'lucide-inbox',
		name: 'Inbox',
		key: MessageFolder.Inbox,
	},
	{
		icon: 'lucide-send',
		name: 'Sent',
		key: MessageFolder.Sent,
	},
	{
		icon: 'lucide-star',
		name: 'Starred',
		key: MessageFolder.Starred,
	},
	{
		icon: 'lucide-archive',
		name: 'Archive',
		key: MessageFolder.Archive,
	},
	{
		icon: 'lucide-trash-2',
		name: 'Trash',
		key: MessageFolder.Trash,
	},
]);

const messageLabels = ref([
	{
		name: 'Internal',
		key: MessageTag.Internal,
	},
	{
		name: 'External',
		key: MessageTag.External,
	},
	{
		name: 'Important',
		key: MessageTag.Important,
	},
	{
		name: 'Portal',
		key: MessageTag.Portal,
	},
]);

// Select message type
const handleSelectType = (type: MessageFolder) => {
	// 如果正在加载，禁止切换
	if (requiredLoading.value || loadingMore.value) return;

	handleCloseDetailPanel();
	selectedTag.value = null;
	selectedType.value = type;
	refreshMessageList();
};

// Select label
const handleSelectLabel = (label: MessageTag) => {
	// 如果正在加载，禁止切换
	if (requiredLoading.value || loadingMore.value) return;

	handleCloseDetailPanel();
	selectedType.value = null;
	selectedTag.value = label;
	refreshMessageList();
};

const searchMessage = ref('');
const messageList = ref<MessageList[]>([]);
const requiredLoading = ref(false);

// Pagination state
const currentPage = ref(1);
const pageSize = ref(20);
const hasMore = ref(true);
const loadingMore = ref(false);

// MessageFolder selection and detail panel state management
const selectedMessageId = ref<string | null>(null);
const isDetailPanelVisible = ref(false);

// Handle message selection
const currentMessageCenter = useTemplateRef('messageDetail');
const handleSelectMessage = (messageId: string) => {
	if (!functionPermission(ProjectPermissionEnum.messageCenter.read)) return;

	selectedMessageId.value = messageId;
	isDetailPanelVisible.value = true;

	// 前端直接标记为已读
	const message = messageList.value.find((msg) => msg.id === messageId);
	if (message && !message.isRead) {
		message.isRead = true;
		// 更新未读数量(减1)
		if (messageCenterCount.value > 0) {
			messageCenterCount.value--;
		}
	}

	nextTick(() => {
		currentMessageCenter.value?.getMessageInfo(messageId);
	});
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
const formatMessageTime = (timestamp: string): string => {
	const time = timeZoneConvert(timestamp, false, projectTenMinutesSsecondsDate);
	const date = typeof time === 'string' ? new Date(time) : time;
	const now = new Date();
	const diff = now.getTime() - date.getTime();
	const days = Math.floor(diff / (1000 * 60 * 60 * 24));

	if (days === 0) return time;
	if (days === 1) return 'Yesterday';
	if (days < 7) return `${days} days ago`;

	return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
};

const messageCenterCount = ref(0);

/**
 * Get message list with pagination
 * @param append - Whether to append to existing list (for load more)
 */
const getMessageList = async (append = false) => {
	try {
		// if (!isBinding.value && selectedType.value == MessageFolder.Inbox) return;
		if (append) {
			if (loadingMore.value || !hasMore.value) return;
			loadingMore.value = true;
		} else {
			if (requiredLoading.value) return;
			requiredLoading.value = true;
		}

		const res = await messageCenterList({
			folder:
				((selectedType.value != null && MessageFolder?.[selectedType.value]) as any) ||
				undefined,
			label:
				((selectedTag.value != null && MessageTag?.[selectedTag.value]) as any) ||
				undefined,
			searchTerm: searchMessage.value,
			pageIndex: currentPage.value,
			pageSize: pageSize.value,
		});

		if (res.code == '200') {
			const newMessages = res?.data?.data || [];

			if (append) {
				// Append new messages to existing list
				messageList.value = [...messageList.value, ...newMessages];
			} else {
				// Replace list with new messages
				messageList.value = newMessages;
			}

			// Check if there are more messages
			hasMore.value = newMessages.length === pageSize.value;
		}

		// Get unread count (only on initial load)
		if (!append) {
			getMessageUnreadCount().then((res) => {
				if (res.code == '200') {
					messageCenterCount.value = res?.data;
				}
			});
		}
	} finally {
		if (append) {
			loadingMore.value = false;
		} else {
			requiredLoading.value = false;
		}
		nextTick(() => {
			updateGridHeight();
			if (selectedMessageId.value && !append) {
				currentMessageCenter.value?.getMessageInfo(selectedMessageId.value);
			}
		});
	}
};

const syncMessageLoading = ref(false);
const syncMessageList = async () => {
	try {
		syncMessageLoading.value = true;
		isBinding.value && (await syncMessage());
		refreshMessageList();
	} finally {
		syncMessageLoading.value = false;
	}
};

/**
 * Refresh message list (reset to page 1)
 */
const refreshMessageList = async () => {
	currentPage.value = 1;
	hasMore.value = true;
	getMessageList(false);
};

/**
 * Load more messages (next page)
 */
const loadMoreMessages = () => {
	if (hasMore.value && !loadingMore.value) {
		currentPage.value++;
		getMessageList(true);
	}
};

/**
 * Handle scroll event for infinite loading
 */
const handleScroll = (event) => {
	const { scrollTop } = event;
	const scrollbarWrap = (gridContainerRef.value as any).wrapRef;

	if (!scrollbarWrap) return;

	const scrollHeight = scrollbarWrap.scrollHeight;
	const clientHeight = scrollbarWrap.clientHeight;
	if (scrollHeight - scrollTop - clientHeight < 10) {
		console.log('scrollTop:', scrollTop);
		console.log('gridContainerRef.value:', clientHeight);
		console.log('gridContainerRef.value:', scrollHeight);
		console.log('滑动到底部');
		loadMoreMessages();
	}
};

// MessageFolder composer state
const messageComposer = useTemplateRef('messageComposer');

// MessageFolder action handlers
const handleReply = (message: MessageInfo) => {
	if (message) {
		messageComposer.value?.openVisible(message, true);
	}
};

const handleForward = (message: MessageInfo) => {
	if (message) {
		messageComposer.value?.openVisible(message, false);
	}
};

const starLoadingId = ref('');
const handleStar = async (messageId: string, isStarred: boolean) => {
	try {
		starLoadingId.value = messageId;
		const res = isStarred ? await unstarMessage(messageId) : await starMessage(messageId);
		if (res.code == '200') {
			// 本地更新收藏状态
			const message = messageList.value.find((msg) => msg.id === messageId);
			if (message) {
				message.isStarred = !isStarred;
			}
			// 如果在 Starred 文件夹取消收藏，从列表中移除
			if (selectedType.value === MessageFolder.Starred && isStarred) {
				messageList.value = messageList.value.filter((msg) => msg.id !== messageId);
				// 如果删除的是当前选中的消息，关闭详情面板
				if (selectedMessageId.value === messageId) {
					handleCloseDetailPanel();
				}
			}
		}
	} finally {
		starLoadingId.value = '';
	}
};

const archiveLoadingId = ref('');
const handleArchive = async (messageId: string, isArchived: boolean) => {
	try {
		archiveLoadingId.value = messageId;
		const res = isArchived
			? await unarchiveMessage(messageId)
			: await archiveMessage(messageId);
		if (res.code == '200') {
			// 归档/取消归档后从当前列表中移除
			// 因为归档后消息会移动到 Archive 文件夹，取消归档后会移回原文件夹
			messageList.value = messageList.value.filter((msg) => msg.id !== messageId);
			// 如果移除的是当前选中的消息，关闭详情面板
			if (selectedMessageId.value === messageId) {
				handleCloseDetailPanel();
			}
		}
	} finally {
		archiveLoadingId.value = '';
	}
};

const handleDelete = async (messageId: string, permanent: boolean = false) => {
	const title = permanent ? '⚠️ Permanently Delete' : '⚠️ Delete Message';
	const confirmMessage = permanent
		? 'This message will be permanently deleted and cannot be recovered. Are you sure you want to continue?'
		: 'This message will be moved to Trash. Are you sure you want to delete it?';
	const confirmButtonText = permanent ? 'Delete Permanently' : 'Delete';

	try {
		await ElMessageBox.confirm(confirmMessage, title, {
			confirmButtonText,
			cancelButtonText: 'Cancel',
			confirmButtonClass: 'danger-confirm-btn',
			cancelButtonClass: 'cancel-confirm-btn',
			distinguishCancelAndClose: true,
			customClass: 'delete-confirmation-dialog',
			showCancelButton: true,
			showConfirmButton: true,
			beforeClose: async (action, instance, done) => {
				if (action === 'confirm') {
					instance.confirmButtonLoading = true;
					instance.confirmButtonText = 'Deleting...';
					try {
						const res = permanent
							? await permanentDeleteMessage(messageId)
							: await deleteMessage(messageId);
						if (res.code == '200') {
							// 本地移除删除的消息
							messageList.value = messageList.value.filter(
								(msg) => msg.id !== messageId
							);
							// 如果删除的是当前选中的消息，关闭详情面板
							if (selectedMessageId.value === messageId) {
								handleCloseDetailPanel();
							}
							done();
						} else {
							instance.confirmButtonLoading = false;
							instance.confirmButtonText = confirmButtonText;
						}
					} catch {
						instance.confirmButtonLoading = false;
						instance.confirmButtonText = confirmButtonText;
					}
				} else {
					done();
				}
			},
		});
	} catch {
		// User cancelled, do nothing
	}
};

const unreadLoadingId = ref('');
const handleUnread = async (messageId: string, folder: MessageFolder) => {
	try {
		unreadLoadingId.value = messageId;
		const res = await unreadMessage(messageId);
		if (res.code == '200') {
			// 前端直接更新列表中的未读状态
			const message = messageList.value.find((msg) => msg.id === messageId);
			if (message && message.isRead) {
				message.isRead = false;
				// 更新未读数量(加1)
				messageCenterCount.value++;
			}
		}
	} finally {
		unreadLoadingId.value = '';
	}
};

const attachmentLoadingId = ref('');
const handleDownloadAttachment = async (attachmentId: string, name: string) => {
	console.log('Download attachment:', attachmentId);
	try {
		attachmentLoadingId.value = attachmentId;
		const response = await downLoadFile(attachmentId);
		// 创建下载链接
		const blob = new Blob([response], {
			type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
		});
		const url = window.URL.createObjectURL(blob);
		const link = document.createElement('a');
		link.href = url;

		link.download = name;

		// 触发下载
		document.body.appendChild(link);
		link.click();
		document.body.removeChild(link);
		window.URL.revokeObjectURL(url);
	} finally {
		attachmentLoadingId.value = '';
	}
};

const handleCompose = () => {
	messageComposer.value?.openVisible();
};

const handleSendMessage = (data: any) => {
	refreshMessageList();
};

const handleSaveDraft = (data: any) => {
	console.log('Saving draft:', data);
	// TODO: Implement save draft functionality
};

// Email binding state
const isBinding = ref('');
const initLoading = ref(false);
const authLoading = ref(false);
const authEmailUrl = ref('');
const unbindLoading = ref(false);

/**
 * Get user email binding status
 * If bound, load message list
 * If not bound, get authorization URL
 */
const getUserBinding = async () => {
	try {
		initLoading.value = true;
		const res = await getIsBindIng();
		if (res.code == '200' && res.data) {
			isBinding.value = res.data.email;
			if (!res.data?.lastSyncTime) {
				// 显示同步加载弹窗
				ElMessageBox({
					title: 'Syncing Emails',
					message: h('div', { class: 'sync-loading-container' }, [
						h('div', { class: 'wave-loading' }, [
							h('div', { class: 'bar', style: 'animation-delay: -1.1s' }),
							h('div', { class: 'bar', style: 'animation-delay: -1s' }),
							h('div', { class: 'bar', style: 'animation-delay: -0.9s' }),
							h('div', { class: 'bar', style: 'animation-delay: -0.8s' }),
							h('div', { class: 'bar', style: 'animation-delay: -0.7s' }),
						]),
						h(
							'p',
							{ class: 'sync-loading-text' },
							'Syncing your emails, please wait...'
						),
					]),
					showClose: false,
					showConfirmButton: false,
					closeOnClickModal: false,
					closeOnPressEscape: false,
					customClass: 'sync-email-dialog',
				});
				try {
					await syncMessageFull();
				} finally {
					ElMessageBox.close();
				}
			}
			refreshMessageList();
		} else {
			isBinding.value = '';
			const authEmailRes = await getEmailAuth();
			if (authEmailRes.code == '200') {
				authEmailUrl.value = authEmailRes.data.authorizationUrl;
			}
			// selectedType.value = MessageFolder.Sent;
			refreshMessageList();
		}
	} finally {
		initLoading.value = false;
		ElMessageBox.close();
		nextTick(() => {
			updateGridHeight();
		});
	}
};

/**
 * Open authorization window and handle auth flow
 */
const openAuthWindow = () => {
	if (!authEmailUrl.value) {
		return;
	}

	authLoading.value = true;

	// Open authorization window
	const width = 600;
	const height = 700;
	const left = window.screen.width / 2 - width / 2;
	const top = window.screen.height / 2 - height / 2;

	const authWindow = window.open(
		authEmailUrl.value,
		'Email Authorization',
		`width=${width},height=${height},left=${left}px,top=${top}px,toolbar=no,menubar=no,scrollbars=yes,resizable=yes`
	);

	// Poll to check if window is closed
	const checkWindowClosed = setInterval(() => {
		if (authWindow?.closed) {
			clearInterval(checkWindowClosed);
			nextTick(async () => {
				authLoading.value = false;
				// Re-check binding status
				await getUserBinding();
			});
		}
	}, 500);
};

/**
 * Handle connect email button click (from EmailBindingPrompt or sidebar)
 */
const handleConnectEmail = () => {
	openAuthWindow();
};

/**
 * Handle unbind email
 */
const handleUnbind = async () => {
	try {
		await ElMessageBox.confirm(
			'Are you sure you want to disconnect your email account? You will need to reconnect to access your messages.',
			'Disconnect Email',
			{
				confirmButtonText: 'Disconnect',
				cancelButtonText: 'Cancel',
				type: 'warning',
				confirmButtonClass: 'el-button--danger',
				beforeClose: async (action, instance, done) => {
					if (action === 'confirm') {
						instance.confirmButtonLoading = true;
						instance.confirmButtonText = 'Disconnecting...';
						try {
							const res = await unbindMessageCenter();
							if (res.code == '200') {
								// 清空本地状态
								isBinding.value = '';
								messageList.value = [];
								selectedMessageId.value = null;
								isDetailPanelVisible.value = false;
								messageCenterCount.value = 0;
								// 重新获取授权 URL
								const authEmailRes = await getEmailAuth();
								if (authEmailRes.code == '200') {
									authEmailUrl.value = authEmailRes.data.authorizationUrl;
								}
								done();
							} else {
								instance.confirmButtonLoading = false;
								instance.confirmButtonText = 'Disconnect';
							}
						} catch {
							instance.confirmButtonLoading = false;
							instance.confirmButtonText = 'Disconnect';
						}
					} else {
						done();
					}
				},
			}
		);
	} finally {
		unbindLoading.value = false;
	}
};

const getLabelType = (label: MessageTag) => {
	switch (label) {
		case MessageTag.Internal:
			return 'info';
		case MessageTag.External:
			return 'success';
		case MessageTag.Important:
			return 'warning';
		case MessageTag.Portal:
			return 'primary';
		default:
			return 'info';
	}
};

onMounted(() => {
	getUserBinding();
});
</script>

<style scoped>
.bg-Internal {
	background-color: var(--el-color-info);
}
.bg-External {
	background-color: var(--el-color-success);
}
.bg-Important {
	background-color: var(--el-color-warning);
}
.bg-Portal {
	background-color: var(--primary-500);
}
</style>

<style>
/* Sync Email Dialog Styles */
.sync-email-dialog .el-message-box__header {
	padding-bottom: 0;
}

.sync-email-dialog .el-message-box__content {
	padding-top: 20px;
}

.sync-email-dialog .el-message-box__message {
	width: 100%;
}

.sync-loading-container {
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	padding: 20px 0;
	width: 100%;
}

.sync-loading-container .wave-loading {
	display: flex;
	justify-content: center;
	align-items: center;
	gap: 4px;
}

.sync-loading-container .wave-loading .bar {
	width: 5px;
	height: 28px;
	background: var(--el-color-primary);
	border-radius: 3px;
	animation: wave-sync 1.2s ease-in-out infinite;
}

.sync-loading-container .sync-loading-text {
	margin-top: 20px;
	color: var(--el-text-color-secondary);
	font-size: 14px;
	text-align: center;
}

@keyframes wave-sync {
	0%,
	40%,
	100% {
		transform: scaleY(0.4);
	}
	20% {
		transform: scaleY(1);
	}
}
</style>
