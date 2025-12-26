<template>
	<div v-if="visible" class="h-full flex flex-col bg-black-400 rounded-xl">
		<!-- Loading Skeleton -->
		<div v-if="messageLoading" class="h-full flex flex-col">
			<!-- Header Skeleton -->
			<div class="flex items-center justify-between p-4 border-b border-gray-700">
				<el-skeleton animated class="flex-1">
					<template #template>
						<el-skeleton-item variant="h1" class="w-3/4" />
					</template>
				</el-skeleton>
				<div class="flex items-center gap-2 ml-4">
					<el-skeleton-item variant="circle" class="w-8 h-8" />
					<el-skeleton-item variant="circle" class="w-8 h-8" />
					<el-skeleton-item variant="circle" class="w-8 h-8" />
					<el-skeleton-item variant="circle" class="w-8 h-8" />
				</div>
			</div>

			<!-- Content Skeleton -->
			<div class="flex-1 overflow-y-auto p-4">
				<el-skeleton animated>
					<template #template>
						<!-- Sender Info Skeleton -->
						<div class="flex items-start gap-3 mb-6">
							<el-skeleton-item variant="circle" class="w-10 h-10" />
							<div class="flex-1 space-y-2">
								<el-skeleton-item variant="text" class="w-40" />
								<el-skeleton-item variant="text" class="w-60" />
								<el-skeleton-item variant="text" class="w-80" />
							</div>
						</div>

						<!-- Labels Skeleton -->
						<div class="flex items-center gap-2 mb-6">
							<el-skeleton-item variant="text" class="w-20 h-6" />
							<el-skeleton-item variant="text" class="w-24 h-6" />
						</div>

						<!-- Message Body Skeleton -->
						<div class="space-y-3 mb-6">
							<el-skeleton-item variant="text" class="w-full" />
							<el-skeleton-item variant="text" class="w-full" />
							<el-skeleton-item variant="text" class="w-3/4" />
							<el-skeleton-item variant="text" class="w-full" />
							<el-skeleton-item variant="text" class="w-5/6" />
						</div>

						<!-- Attachments Skeleton -->
						<div class="space-y-2">
							<el-skeleton-item variant="text" class="w-32 h-5" />
							<div
								v-for="i in 2"
								:key="i"
								class="flex items-center gap-3 p-3 bg-black-300 rounded-lg"
							>
								<el-skeleton-item variant="circle" class="w-6 h-6" />
								<div class="flex-1 space-y-1">
									<el-skeleton-item variant="text" class="w-48" />
									<el-skeleton-item variant="text" class="w-20" />
								</div>
								<el-skeleton-item variant="button" class="w-24 h-8" />
							</div>
						</div>
					</template>
				</el-skeleton>
			</div>

			<!-- Footer Skeleton -->
			<div class="flex items-center gap-3 p-4 border-t border-gray-700">
				<el-skeleton-item variant="button" class="w-24 h-9" />
				<el-skeleton-item variant="button" class="w-24 h-9" />
			</div>
		</div>

		<!-- Actual Content -->
		<template v-else-if="message">
			<!-- Header section -->
			<div class="flex items-center justify-between p-4 border-b border-gray-700">
				<h2 class="text-xl font-bold truncate flex-1">{{ message.subject }}</h2>
				<div class="flex items-center gap-2 ml-4">
					<!-- Reply -->
					<el-tooltip
						content="Reply"
						placement="bottom"
						v-if="functionPermission(ProjectPermissionEnum.messageCenter.create)"
					>
						<el-button link @click="$emit('reply', message)">
							<Icon icon="lucide-reply" />
						</el-button>
					</el-tooltip>
					<!-- Forward -->
					<el-tooltip
						content="Forward"
						placement="bottom"
						v-if="functionPermission(ProjectPermissionEnum.messageCenter.create)"
					>
						<el-button link @click="$emit('forward', message)">
							<Icon icon="lucide-forward" />
						</el-button>
					</el-tooltip>
					<!-- More actions dropdown -->
					<el-dropdown
						trigger="click"
						hide-on-click
						class="ml-2"
						@command="handleMoreCommand"
						v-if="
							functionPermission(ProjectPermissionEnum.messageCenter.update) ||
							functionPermission(ProjectPermissionEnum.messageCenter.delete)
						"
					>
						<el-button link>
							<Icon icon="lucide-more-horizontal" />
						</el-button>
						<template #dropdown>
							<el-dropdown-menu>
								<el-dropdown-item
									command="star"
									v-if="
										functionPermission(
											ProjectPermissionEnum.messageCenter.update
										)
									"
								>
									<Icon
										v-if="starLoadingId == message.id"
										icon="lucide-loader-2"
										class="animate-spin mr-2"
									/>
									<Icon
										v-else
										:icon="
											message.isStarred
												? 'solar:star-bold'
												: 'solar:star-outline'
										"
										:class="{ 'text-yellow-400': message.isStarred }"
										class="mr-2"
									/>
									{{ message.isStarred ? 'Unstar' : 'Star' }}
								</el-dropdown-item>
								<el-dropdown-item
									command="archive"
									v-if="
										functionPermission(
											ProjectPermissionEnum.messageCenter.update
										) && message.folder != MessageFolder.Trash
									"
								>
									<Icon
										v-if="archiveLoadingId == message.id"
										icon="lucide-loader-2"
										class="animate-spin mr-2"
									/>
									<Icon
										v-else
										:icon="
											message.isArchived
												? 'lucide-archive-restore'
												: 'lucide-archive'
										"
										class="mr-2"
									/>
									{{ message.isArchived ? 'Unarchive' : 'Archive' }}
								</el-dropdown-item>
								<el-dropdown-item
									command="unread"
									v-if="
										functionPermission(
											ProjectPermissionEnum.messageCenter.update
										) && message.folder != MessageFolder.Trash
									"
								>
									<Icon
										v-if="unreadLoadingId == message.id"
										icon="lucide-loader-2"
										class="animate-spin mr-2"
									/>
									<Icon v-else icon="lucide-mail" class="mr-2" />
									Mark as unread
								</el-dropdown-item>
								<el-dropdown-item
									command="restore"
									v-if="
										functionPermission(
											ProjectPermissionEnum.messageCenter.update
										) && message.folder == MessageFolder.Trash
									"
								>
									<Icon
										v-if="restoreLoadingId == message.id"
										icon="lucide-loader-2"
										class="animate-spin mr-2"
									/>
									<Icon v-else icon="lucide-archive-restore" class="mr-2" />
									Restore message
								</el-dropdown-item>
								<el-dropdown-item
									command="delete"
									divided
									v-if="
										functionPermission(
											ProjectPermissionEnum.messageCenter.delete
										)
									"
								>
									<Icon icon="lucide-trash-2" class="mr-2 text-red-500" />
									<span class="text-red-500">Delete</span>
								</el-dropdown-item>
							</el-dropdown-menu>
						</template>
					</el-dropdown>
					<!-- Close -->
					<el-button :icon="Close" class="ml-2" link @click="$emit('close')" />
				</div>
			</div>

			<!-- Content area -->
			<el-scrollbar ref="emailContainerRef">
				<div class="p-4">
					<!-- Sender/Recipient information -->
					<div class="mb-6">
						<div class="flex items-start gap-3 mb-3">
							<div
								class="w-10 h-10 rounded-full flex items-center justify-center text-white font-medium"
								:style="{
									backgroundColor: getAvatarColor(message.senderName),
								}"
							>
								{{ getInitials(message.senderName) }}
							</div>
							<div class="flex-1">
								<div class="flex items-center justify-between">
									<div>
										<div class="font-medium">{{ message.senderName }}</div>
										<div class="text-sm text-gray-400">
											{{ message.senderEmail }}
										</div>
									</div>
									<div class="text-sm text-gray-400">
										{{ formatMessageTime(message.receivedDate) }}
									</div>
								</div>
								<div v-if="message.recipients.length > 0" class="mt-2 text-sm">
									<span class="text-gray-400 mr-2">To:</span>
									<span>
										{{ message.recipients.map((t) => t.email).join(', ') }}
									</span>
								</div>
							</div>
						</div>
					</div>

					<!-- Metadata section -->
					<div v-if="message.labels.length > 0 || message.relatedEntityId" class="mb-6">
						<div v-if="message.labels.length > 0" class="flex items-center gap-2 mb-2">
							<el-tag
								v-for="label in message.labels"
								:key="label"
								size="small"
								:type="getLabelType(label)"
							>
								{{ MessageTag[label] }}
							</el-tag>
						</div>
						<div v-if="message.relatedEntityId" class="text-sm">
							<span class="text-gray-400 mr-2">Related to:</span>
							<span class="text-primary">#{{ message.relatedEntityType }}</span>
						</div>
					</div>

					<!-- Message content (使用 iframe 隔离显示，保留原始样式并防止 XSS) -->
					<iframe
						ref="emailIframeRef"
						class="email-iframe w-full border-0 rounded-lg overflow-hidden"
						sandbox="allow-same-origin allow-scripts allow-popups allow-popups-to-escape-sandbox"
						scrolling="no"
						title="Email Content"
					></iframe>

					<!-- Attachments section -->
					<div v-if="message.attachments.length > 0" class="mb-6">
						<div class="text-sm font-medium mb-3">Attachments</div>
						<div class="space-y-2">
							<div
								v-for="attachment in message.attachments"
								:key="attachment.id"
								class="flex items-center justify-between p-3 bg-black-300 rounded-lg"
							>
								<div class="flex items-center gap-3 flex-1 min-w-0">
									<Icon icon="lucide-file" class="text-gray-400" />
									<div class="flex-1 min-w-0">
										<div class="font-medium truncate">
											{{ attachment.fileName }}
										</div>
										<div class="text-xs text-gray-400">
											{{ formatFileSize(attachment.fileSize) }}
										</div>
									</div>
								</div>
								<el-button
									type="primary"
									size="small"
									:loading="attachmentLoadingId == attachment.id"
									@click="
										$emit(
											'download-attachment',
											attachment.id,
											attachment.fileName
										)
									"
								>
									Download
								</el-button>
							</div>
						</div>
					</div>
				</div>
			</el-scrollbar>
		</template>
	</div>
</template>

<script lang="ts" setup>
import { ref, nextTick, watch } from 'vue';
import { MessageInfo } from '#/message';
import { Close } from '@element-plus/icons-vue';
import { formatFileSize } from '@/utils/format';
import { messageCenterInfo } from '@/apis/messageCenter';
import { formatMessageTime } from '@/hooks/time';
import { getAvatarColor } from '@/utils';
import { useTheme } from '@/utils/theme';
import {
	renderEmailToIframe,
	applyDarkModeToElementsWithDarkreader,
	adjustIframeHeight,
} from '@/utils/emailDarkMode';

import { MessageTag, MessageFolder } from '@/enums/appEnum';
import { useAdaptiveScrollbar } from '@/hooks/useAdaptiveScrollbar';
import { functionPermission } from '@/hooks/index';
import { ProjectPermissionEnum } from '@/enums/permissionEnum';

const { scrollbarRef: emailContainerRef, updateScrollbarHeight: updateEmailHeight } =
	useAdaptiveScrollbar(20);
// 保持对 ref 的引用，避免未使用警告
void emailContainerRef;

// 定义 props
interface Props {
	visible: boolean;
	starLoadingId: string;
	archiveLoadingId: string;
	unreadLoadingId: string;
	attachmentLoadingId: string;
	restoreLoadingId: string;
}

defineProps<Props>();

// 定义 emits
const emit = defineEmits<{
	close: [];
	reply: [message: MessageInfo];
	forward: [message: MessageInfo];
	star: [messageId: string, isStarred: boolean];
	archive: [messageId: string, isArchived: boolean];
	delete: [messageId: string, permanent: boolean];
	unread: [messageId: string, folder: MessageFolder];
	restore: [messageId: string];
	'download-attachment': [attachmentId: string, name: string];
}>();

// Handle more actions dropdown command
const handleMoreCommand = (command: string) => {
	if (!message.value) return;
	switch (command) {
		case 'star':
			emit('star', message.value.id, message.value.isStarred);
			break;
		case 'archive':
			emit('archive', message.value.id, message.value.isArchived);
			break;
		case 'unread':
			emit('unread', message.value.id, message.value.folder);
			break;
		case 'restore':
			emit('restore', message.value.id);
			break;
		case 'delete':
			emit('delete', message.value.id, message.value.folder === MessageFolder.Trash);
			break;
	}
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

// Theme state
const theme = useTheme();

const message = ref<MessageInfo>();
const messageLoading = ref(false);
const emailIframeRef = ref<HTMLIFrameElement | null>(null);

/**
 * 渲染邮件内容到 iframe
 */
const renderEmailInIframe = async () => {
	if (!emailIframeRef.value || !message.value?.body) return;

	const iframe = emailIframeRef.value;
	const iframeDoc = iframe.contentDocument || iframe.contentWindow?.document;
	if (!iframeDoc) return;

	const isDark = theme.theme === 'dark';

	// 获取处理好的 HTML
	const fullHtml = renderEmailToIframe(message.value.body);
	// 写入 iframe
	iframeDoc.open();
	iframeDoc.write(fullHtml);
	iframeDoc.close();

	await applyDarkModeToElementsWithDarkreader(iframe, isDark);

	// 使用 Darkreader 处理暗黑模式

	// 设置所有链接在新窗口打开
	const links = iframeDoc.querySelectorAll('a');
	links.forEach((link) => {
		link.setAttribute('target', '_blank');
		link.setAttribute('rel', 'noopener noreferrer');
	});

	// 等待内容加载完成后调整高度
	nextTick(() => {
		adjustIframeHeight(iframe);
	});
};

// 监听主题变化，重新渲染 iframe 内容以应用正确的主题样式
watch(
	() => theme.theme,
	async () => {
		if (message.value?.body) {
			nextTick(() => {
				if ('startViewTransition' in document) {
					(document as any).startViewTransition(renderEmailInIframe);
				} else {
					renderEmailInIframe();
				}
			});
		}
	}
);

const getMessageInfo = async (id: string) => {
	try {
		messageLoading.value = true;
		const res = await messageCenterInfo(id);
		if (res.code == '200') {
			message.value = res.data;
		}
	} finally {
		messageLoading.value = false;
		// 使用 nextTick 确保 DOM 更新后再渲染 iframe 内容
		await nextTick();
		await renderEmailInIframe();
		await nextTick();
		updateEmailHeight();
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

defineExpose({
	getMessageInfo,
});
</script>
