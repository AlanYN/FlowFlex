<template>
	<div v-if="visible && message" class="h-full flex flex-col bg-black-400 rounded-xl">
		<!-- Header section -->
		<div class="flex items-center justify-between p-4 border-b border-gray-700">
			<h2 class="text-xl font-bold truncate flex-1">{{ message.subject }}</h2>
			<div class="flex items-center gap-2 ml-4">
				<el-button :icon="Star" circle link @click="$emit('star', message.id)" />
				<el-button circle link @click="$emit('archive', message.id)">
					<Icon icon="lucide-archive" />
				</el-button>
				<el-button :icon="Delete" circle link @click="$emit('delete', message.id)" />
				<el-button :icon="Close" circle link @click="$emit('close')" />
			</div>
		</div>

		<!-- Content area -->
		<div class="flex-1 overflow-y-auto p-4">
			<!-- Sender/Recipient information -->
			<div class="mb-6">
				<div class="flex items-start gap-3 mb-3">
					<div
						class="w-10 h-10 rounded-full bg-primary-500 flex items-center justify-center text-white font-medium"
					>
						{{ getInitials(message.from.name) }}
					</div>
					<div class="flex-1">
						<div class="flex items-center justify-between">
							<div>
								<div class="font-medium">{{ message.from.name }}</div>
								<div class="text-sm text-gray-400">{{ message.from.email }}</div>
							</div>
							<div class="text-sm text-gray-400">
								{{ formatTimestamp(message.timestamp) }}
							</div>
						</div>
						<div class="mt-2 text-sm">
							<span class="text-gray-400 mr-2">To:</span>
							<span>{{ message.to.map((t) => t.email).join(', ') }}</span>
						</div>
					</div>
				</div>
			</div>

			<!-- Metadata section -->
			<div v-if="message.labels.length > 0 || message.relatedEntity" class="mb-6">
				<div v-if="message.labels.length > 0" class="flex items-center gap-2 mb-2">
					<el-tag
						v-for="label in message.labels"
						:key="label.id"
						:type="getTagType(label.type)"
						size="small"
					>
						{{ label.name }}
					</el-tag>
				</div>
				<div v-if="message.relatedEntity" class="text-sm">
					<span class="text-gray-400 mr-2">Related to:</span>
					<span class="text-primary">{{ message.relatedEntity.displayName }}</span>
				</div>
			</div>

			<!-- Message content -->
			<div class="mb-6">
				<div
					v-if="message.bodyHtml"
					v-html="message.bodyHtml"
					class="prose prose-invert max-w-none"
				></div>
				<div v-else class="whitespace-pre-wrap">{{ message.body }}</div>
			</div>

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
								<div class="font-medium truncate">{{ attachment.filename }}</div>
								<div class="text-xs text-gray-400">
									{{ formatFileSize(attachment.size) }}
								</div>
							</div>
						</div>
						<el-button
							type="primary"
							size="small"
							@click="$emit('download-attachment', attachment.id)"
						>
							Download
						</el-button>
					</div>
				</div>
			</div>
		</div>

		<!-- Action footer -->
		<div class="flex items-center gap-3 p-4 border-t border-gray-700">
			<el-button @click="$emit('reply', message.id)">
				<Icon icon="lucide-reply" />
				Reply
			</el-button>
			<el-button type="primary" @click="$emit('forward', message.id)">
				<Icon icon="lucide-forward" />
				Forward
			</el-button>
		</div>
	</div>
</template>

<script lang="ts" setup>
import { MessageInfo } from '#/message';
import { Star, Delete, Close } from '@element-plus/icons-vue';
import { Message } from '@/enums/appEnum';
import { formatFileSize } from '@/utils/format';

// 定义 props
interface Props {
	message: MessageInfo | null;
	visible: boolean;
}

defineProps<Props>();

// 定义 emits
defineEmits<{
	close: [];
	reply: [messageId: string];
	forward: [messageId: string];
	star: [messageId: string];
	archive: [messageId: string];
	delete: [messageId: string];
	'download-attachment': [attachmentId: string];
}>();

// Get name initials
const getInitials = (name: string): string => {
	if (!name) return '?';
	const parts = name.split(' ');
	if (parts.length >= 2) {
		return (parts[0][0] + parts[1][0]).toUpperCase();
	}
	return name.substring(0, 2).toUpperCase();
};

// Format timestamp
const formatTimestamp = (timestamp: string | Date): string => {
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
</script>
