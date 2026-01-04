<template>
	<div class="message-center">
		<div class="message-center__header">
			<div class="message-center__title">
				<Icon icon="lucide-inbox" />
				<div>Message Center</div>
			</div>
			<span class="message-center__subtitle">Recent messages and notifications</span>
		</div>
		<div class="message-center__content">
			<!-- Loading State -->
			<template v-if="loading">
				<div class="message-list">
					<div v-for="i in 4" :key="i" class="message-item">
						<el-skeleton animated>
							<template #template>
								<div class="flex items-start space-x-3">
									<el-skeleton-item
										variant="circle"
										style="width: 32px; height: 32px"
									/>
									<div class="flex-1 space-y-2">
										<div class="flex items-center justify-between">
											<el-skeleton-item variant="text" style="width: 40%" />
											<el-skeleton-item variant="text" style="width: 60px" />
										</div>
										<el-skeleton-item variant="text" style="width: 75%" />
										<el-skeleton-item variant="text" style="width: 100%" />
									</div>
								</div>
							</template>
						</el-skeleton>
					</div>
				</div>
			</template>

			<!-- Empty State -->
			<div v-else-if="messages.length === 0" class="message-center__empty">
				<el-empty :image-size="50">
					<template #image>
						<Icon icon="lucide-mail" />
					</template>
					<template #description>
						<div>
							<h3 class="text-base">No messages found</h3>
							<div class="text-sm text-gray-400">Your inbox is empty</div>
						</div>
					</template>
				</el-empty>
			</div>

			<!-- Message List -->
			<div v-else class="message-list">
				<div
					v-for="message in messages"
					:key="message.id"
					class="message-item"
					:class="{ 'message-item--unread': !message.isRead }"
				>
					<div class="flex items-start space-x-2 w-full min-w-0" @click="clickMessage">
						<div class="flex-1 min-w-0">
							<div class="flex items-center justify-between gap-2">
								<div class="flex items-center space-x-2 min-w-0 flex-1">
									<span
										class="avatar"
										:style="{
											backgroundColor: getAvatarColor(message.senderName),
										}"
									>
										{{ getInitials(message.senderName) }}
									</span>
									<div class="flex items-center gap-x-2">
										<div
											v-if="!message.isRead"
											class="w-2 h-2 bg-primary rounded-full flex-shrink-0"
										></div>
										<span class="font-medium truncate min-w-0 text-sm">
											{{ message.senderName }}
										</span>
									</div>
								</div>
								<div class="flex items-center space-x-2 shrink-0">
									<span class="text-xs text-gray-400 whitespace-nowrap">
										{{ formatMessageTime(message.receivedDate) }}
									</span>
								</div>
							</div>
							<h3 class="text-sm my-2 truncate">{{ message.subject }}</h3>
							<p class="text-xs text-gray-400 truncate">{{ message.bodyPreview }}</p>
							<div class="mt-2 flex items-center space-x-2">
								<el-tag type="primary" size="small">
									{{ MessageTypeEnum[message.messageType] }}
								</el-tag>
								<template v-if="message.labels.length > 0">
									<el-tag
										v-for="label in message.labels"
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
		</div>
		<div class="message-center__footer">
			<div class="flex items-center space-x-2">
				<div
					class="px-2 py-0.5 border rounded-md text-xs font-medium text-primary border-primary"
				>
					{{ messageUnreadCount }}
				</div>
				<div>Unread messages</div>
			</div>
			<el-button @click="viewAllMessage">View All Message</el-button>
		</div>
	</div>
</template>

<script setup lang="ts">
import type { IDashboardMessage } from '#/dashboard';
import { MessageTypeEnum, MessageTag } from '@/enums/appEnum';
import { getAvatarColor } from '@/utils';
import { Icon } from '@iconify/vue';
import { useRouter } from 'vue-router';
import { formatMessageTime } from '@/hooks/time';

interface Props {
	messages: IDashboardMessage[];
	loading?: boolean;
	messageUnreadCount: number;
}
const router = useRouter();

defineProps<Props>();

const getInitials = (name: string): string => {
	if (!name) return '?';
	const parts = name.split(' ');
	if (parts.length >= 2) {
		return (parts[0][0] + parts[1][0]).toUpperCase();
	}
	return name.substring(0, 2).toUpperCase();
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

const clickMessage = () => {
	router.push(`/message/messageCenter`);
};

const viewAllMessage = () => {
	router.push(`/message/messageCenter`);
};
</script>

<style scoped lang="scss">
.message-center {
	@apply rounded-xl overflow-hidden flex flex-col;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
	box-shadow: var(--el-box-shadow-light);
	height: 100%;

	&__header {
		@apply px-4 border-b flex-shrink-0;
		border-color: var(--el-border-color-lighter);
		height: 72px;
		display: flex;
		flex-direction: column;
		justify-content: center;
	}

	&__title {
		@apply text-lg font-semibold m-0 flex items-center space-x-2;
		color: var(--el-text-color-primary);
	}

	&__subtitle {
		@apply text-sm;
		color: var(--el-text-color-secondary);
	}

	&__content {
		@apply overflow-y-auto flex-1;
		min-height: 200px;
		max-height: 320px;
	}

	&__empty {
		@apply p-4 text-center;
		color: var(--el-text-color-secondary);
	}

	&__footer {
		@apply px-4 py-3 text-center text-sm border-t flex-shrink-0;
		color: var(--el-text-color-secondary);
		border-color: var(--el-border-color-lighter);
		height: 44px;
		display: flex;
		align-items: center;
		justify-content: space-between;
	}
}

.message-list {
	@apply divide-y;
	border-color: var(--el-border-color-lighter);
}

.message-item {
	@apply flex items-start p-4 cursor-pointer transition-colors border-b;
	border-color: var(--el-border-color-lighter);

	&:hover {
		background: var(--el-fill-color-light);
	}

	&--unread {
		background: var(--el-color-primary-light-9);
	}
}

.avatar {
	@apply relative flex size-8 shrink-0 overflow-hidden rounded-full h-8 w-8 items-center justify-center text-white font-medium text-xs;
}
</style>
