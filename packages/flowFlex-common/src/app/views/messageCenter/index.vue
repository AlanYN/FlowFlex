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
				>
					New Message
				</el-button>
			</template>
		</PageHeader>
		<div class="grid grid-cols-12 gap-6">
			<div class="col-span-12 md:col-span-3 lg:col-span-2 bg-black-400 p-4 rounded-xl">
				<el-button type="primary" :icon="Plus" class="w-full my-4">Compose</el-button>
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
			<div class="col-span-12 md:col-span-9 lg:col-span-10 bg-black-400 p-4 rounded-xl">
				<div class="flex items-center justify-between">
					<div class="text-xl font-bold capitalize">{{ Message[selectedType] }}</div>
					<el-button circle :icon="Refresh" link />
				</div>
				<div class="flex items-center gap-x-2 my-2">
					<el-input :prefix-icon="Search" placeholder="Search messages..." />
					<el-button :icon="Filter" />
				</div>
			</div>
		</div>
	</div>
</template>

<script lang="ts" setup>
import { ref } from 'vue';
import { Message } from '@/enums/appEnum';
import { Filter, Plus, Refresh, Search } from '@element-plus/icons-vue';

import PageHeader from '@/components/global/PageHeader/index.vue';

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

// 选择消息类型
const handleSelectType = (type: Message) => {
	selectedType.value = type;
	// TODO: 加载对应类型的消息列表
	console.log('Selected type:', type);
};

// 选择标签
const handleSelectLabel = (label: Message) => {
	selectedType.value = label; // 默认显示收件箱
	// TODO: 加载对应标签的消息列表
	console.log('Selected label:', label);
};
</script>
