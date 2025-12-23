<template>
	<div class="grid grid-cols-[1fr_auto_1fr] gap-6">
		<!-- 左侧：Available Items -->
		<div class="bg-bg-overlay border border-border-lighter rounded-lg overflow-hidden">
			<div class="px-4 py-3 border-b border-border-lighter">
				<h4 class="text-sm font-semibold text-text-primary m-0">
					{{ leftTitle }}
				</h4>
			</div>
			<div class="p-4 space-y-3 max-h-96 overflow-y-auto">
				<!-- 分组显示 -->
				<template v-if="groupedLeftItems.length > 0">
					<div v-for="group in groupedLeftItems" :key="group.label" class="space-y-2">
						<div
							v-if="group.label"
							class="text-xs font-medium text-text-secondary mb-2"
						>
							{{ group.label }}
						</div>
						<div class="space-y-2">
							<div
								v-for="item in group.items"
								:key="item.key"
								class="flex items-center justify-between px-3 py-2 bg-bg-page rounded hover:bg-fill-color cursor-pointer transition-colors"
							>
								<div class="flex-1 min-w-0">
									<div class="text-sm font-medium text-text-primary truncate">
										{{ item.label }}
									</div>
									<div
										v-if="item.description"
										class="text-xs text-text-secondary"
									>
										{{ item.description }}
									</div>
								</div>
							</div>
						</div>
					</div>
				</template>

				<!-- 空状态 -->
				<div v-else class="text-center py-8 text-sm text-text-secondary">
					{{ leftEmptyText }}
				</div>
			</div>
		</div>

		<!-- 中间：控制按钮 -->
		<div class="flex flex-col justify-center items-center gap-3">
			<div>
				<el-button :disabled="rightItems.length === 0" @click="handleMoveToLeft">
					<el-icon>
						<Icon icon="lucide:chevrons-left" class="w-4 h-4" />
					</el-icon>
				</el-button>
			</div>
			<div>
				<el-button :disabled="leftItems.length === 0" @click="handleMoveToRight">
					<el-icon>
						<Icon icon="lucide:chevrons-right" class="w-4 h-4" />
					</el-icon>
				</el-button>
			</div>
		</div>

		<!-- 右侧：Selected Items -->
		<div class="bg-bg-overlay border border-border-lighter rounded-lg overflow-hidden">
			<div class="px-4 py-3 border-b border-border-lighter">
				<h4 class="text-sm font-semibold text-text-primary m-0">
					{{ rightTitle }}
				</h4>
			</div>
			<div class="p-4 space-y-2 max-h-96 overflow-y-auto">
				<div
					v-for="item in rightItems"
					:key="item.key"
					class="flex items-center justify-between px-3 py-2 bg-bg-page rounded hover:bg-fill-color cursor-pointer transition-colors"
				>
					<div class="flex-1 min-w-0">
						<div class="text-sm font-medium text-text-primary truncate">
							{{ item.label }}
						</div>
						<div v-if="item.description" class="text-xs text-text-secondary">
							{{ item.description }}
						</div>
					</div>
				</div>

				<!-- 空状态 -->
				<div
					v-if="rightItems.length === 0"
					class="text-center py-8 text-sm text-text-secondary"
				>
					{{ rightEmptyText }}
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { ITransferItem } from './types';

/**
 * 分组数据接口
 */
interface IGroupedItems {
	label: string;
	items: ITransferItem[];
}

interface Props {
	/** 所有可用数据 */
	data: ITransferItem[];
	/** 已选中的 key 列表 */
	modelValue: string[];
	/** 左侧标题 */
	leftTitle?: string;
	/** 右侧标题 */
	rightTitle?: string;
	/** 左侧空状态文本 */
	leftEmptyText?: string;
	/** 右侧空状态文本 */
	rightEmptyText?: string;
}

interface Emits {
	(e: 'update:modelValue', value: string[]): void;
	(e: 'change', value: string[], direction: 'left' | 'right'): void;
}

const props = withDefaults(defineProps<Props>(), {
	leftTitle: 'Available',
	rightTitle: 'Selected',
	leftEmptyText: 'No items available',
	rightEmptyText: 'No items selected',
});

const emit = defineEmits<Emits>();

/**
 * 左侧可用项（未选中的）
 */
const leftItems = computed(() => {
	return props.data.filter((item) => !props.modelValue.includes(item.key));
});

/**
 * 右侧已选项
 */
const rightItems = computed(() => {
	return props.modelValue
		.map((key) => props.data.find((item) => item.key === key))
		.filter((item): item is ITransferItem => item !== undefined);
});

/**
 * 左侧分组数据
 */
const groupedLeftItems = computed<IGroupedItems[]>(() => {
	const groups = new Map<string, ITransferItem[]>();

	leftItems.value.forEach((item) => {
		const groupLabel = item.group || '';
		if (!groups.has(groupLabel)) {
			groups.set(groupLabel, []);
		}
		groups.get(groupLabel)!.push(item);
	});

	return Array.from(groups.entries()).map(([label, items]) => ({
		label,
		items,
	}));
});

/**
 * 移动所有项到右侧
 */
function handleMoveToRight() {
	const newValue = props.data.map((item) => item.key);
	emit('update:modelValue', newValue);
	emit('change', newValue, 'right');
}

/**
 * 移动所有项到左侧
 */
function handleMoveToLeft() {
	emit('update:modelValue', []);
	emit('change', [], 'left');
}
</script>

<style scoped lang="scss">
// 所有样式都使用 Tailwind CSS，无需额外样式
</style>
