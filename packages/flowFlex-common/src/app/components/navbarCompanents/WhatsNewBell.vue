<template>
	<div class="whats-new-bell-wrapper">
		<WhatsNewDetail ref="detailRef" :item="selectedItem" />
		<el-popover
			placement="bottom-end"
			:width="360"
			trigger="click"
			popper-class="whats-new-popover"
			@show="onPanelShow"
			@hide="onPanelHide"
		>
			<!-- 铃铛触发器 -->
			<template #reference>
				<div class="relative cursor-pointer flex items-center justify-center w-8 h-8">
					<el-icon
						:size="20"
						class="text-gray-600 dark:text-gray-300 hover:text-primary transition-colors"
					>
						<Bell />
					</el-icon>
					<!-- 红点：仅在 unreadCount > 0 时渲染 -->
					<span v-if="unreadCount > 0" class="unread-dot" />
				</div>
			</template>

			<!-- 面板内容 -->
			<div class="whats-new-panel">
				<!-- 面板头部 -->
				<div
					class="panel-header flex items-center justify-between px-4 py-3 border-b border-gray-100 dark:border-gray-700"
				>
					<span class="font-semibold text-sm text-gray-800 dark:text-gray-100">
						What's New
					</span>
					<el-link
						v-if="unreadCount > 0"
						type="primary"
						:underline="false"
						class="text-xs"
						:disabled="markingAll"
						@click="handleMarkAllRead"
					>
						{{ markingAll ? 'Marking...' : 'Mark all as read' }}
					</el-link>
				</div>

				<!-- 加载状态 -->
				<div v-if="panelLoading" class="flex items-center justify-center py-10">
					<el-icon class="is-loading text-gray-400" :size="20">
						<Loading />
					</el-icon>
				</div>

				<!-- 空状态 -->
				<div
					v-else-if="panelItems.length === 0"
					class="flex flex-col items-center justify-center py-10 text-gray-400"
				>
					<el-icon :size="32" class="mb-2">
						<Bell />
					</el-icon>
					<span class="text-sm">No updates yet</span>
				</div>

				<!-- 更新列表 -->
				<div v-else class="panel-list overflow-y-auto" style="max-height: 400px">
					<div
						v-for="item in panelItems"
						:key="item.id"
						class="panel-item flex gap-3 px-4 py-3 cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors border-b border-gray-50 dark:border-gray-800 last:border-b-0"
						:class="{ 'bg-blue-50/30 dark:bg-blue-900/10': !item.isRead }"
						@click="handleItemClick(item)"
					>
						<!-- 未读蓝点 -->
						<div class="flex-shrink-0 flex items-start pt-1.5">
							<span v-if="!item.isRead" class="unread-item-dot" />
							<span v-else class="unread-item-dot-placeholder" />
						</div>

						<!-- 内容区 -->
						<div class="flex-1 min-w-0">
							<div class="flex items-center gap-2 mb-1">
								<!-- Category Tag -->
								<el-tag
									:type="getCategoryTagType(item.category)"
									:style="
										item.category === 'Announcement'
											? {
													'--el-tag-text-color': '#722ED1',
													'--el-tag-border-color': '#d3adf7',
													'--el-tag-bg-color': '#f9f0ff',
											  }
											: {}
									"
									size="small"
									effect="light"
									class="flex-shrink-0"
								>
									{{ getCategoryLabel(item.category) }}
								</el-tag>
							</div>
							<!-- 标题 -->
							<div
								class="font-semibold text-sm text-gray-800 dark:text-gray-100 leading-snug truncate"
							>
								{{ item.title }}
							</div>
							<!-- 摘要（2 行截断） -->
							<div
								class="text-xs text-gray-500 dark:text-gray-400 mt-0.5 summary-clamp"
							>
								{{ item.summary }}
							</div>
							<!-- 相对时间 -->
							<div class="text-xs text-gray-400 dark:text-gray-500 mt-1">
								{{ formatRelativeTime(item.publishTime) }}
							</div>
						</div>
					</div>
				</div>
			</div>
		</el-popover>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted, provide } from 'vue';
import { ElMessage } from 'element-plus';
import { Bell, Loading } from '@element-plus/icons-vue';
import dayjs from 'dayjs';
import relativeTime from 'dayjs/plugin/relativeTime';
import { getUnreadCount, getPanel, markAllRead } from '@/apis/whatsNew';
import type { WhatsNewPanelItem } from '#/whatsNew';
import WhatsNewDetail from './WhatsNewDetail.vue';

// 注册 dayjs relativeTime 插件
dayjs.extend(relativeTime);

// ========================= State =========================

const unreadCount = ref<number>(0);
const panelVisible = ref<boolean>(false);
const panelItems = ref<WhatsNewPanelItem[]>([]);
const panelLoading = ref<boolean>(false);
const markingAll = ref<boolean>(false);

// 选中条目（供 WhatsNewDetail 使用）
const selectedItem = ref<WhatsNewPanelItem | null>(null);
const detailRef = ref();

// ========================= Lifecycle =========================

onMounted(async () => {
	try {
		unreadCount.value = await getUnreadCount();
	} catch (e) {
		// 静默失败，不影响页面加载
		console.warn('[WhatsNewBell] Failed to fetch unread count', e);
	}
});

// ========================= Panel =========================

const togglePanel = () => {
	panelVisible.value = !panelVisible.value;
};

const onPanelShow = async () => {
	if (panelLoading.value) return;
	panelLoading.value = true;
	try {
		const res = await getPanel();
		panelItems.value = res.items ?? [];
	} catch (e) {
		console.warn('[WhatsNewBell] Failed to fetch panel', e);
		panelItems.value = [];
	} finally {
		panelLoading.value = false;
	}
};

const onPanelHide = () => {
	// 面板关闭后不重置列表，保持上次数据避免闪烁
};

// ========================= Mark All Read =========================

const handleMarkAllRead = async () => {
	if (markingAll.value) return;
	markingAll.value = true;
	try {
		await markAllRead();
		// 成功：本地更新状态
		clearAllUnread();
	} catch (e) {
		ElMessage.error('Failed to mark all as read. Please try again.');
	} finally {
		markingAll.value = false;
	}
};

// ========================= Item Click =========================

const handleItemClick = (item: WhatsNewPanelItem) => {
	selectedItem.value = item;
	detailRef.value?.open();
};

// ========================= Provide/Inject Contract =========================

const decrementUnread = () => {
	unreadCount.value = Math.max(0, unreadCount.value - 1);
};

const clearAllUnread = () => {
	unreadCount.value = 0;
	panelItems.value.forEach((i) => {
		i.isRead = true;
	});
};

const markItemAsRead = (id: string) => {
	const item = panelItems.value.find((i) => i.id === id);
	if (item) {
		item.isRead = true;
	}
};

interface WhatsNewStateContext {
	unreadCount: typeof unreadCount;
	panelItems: typeof panelItems;
	selectedItem: typeof selectedItem;
	decrementUnread: () => void;
	clearAllUnread: () => void;
	markItemAsRead: (id: string) => void;
}

provide<WhatsNewStateContext>('whatsNewState', {
	unreadCount,
	panelItems,
	selectedItem,
	decrementUnread,
	clearAllUnread,
	markItemAsRead,
});

// ========================= Helpers =========================

type CategoryType = 'NewFeature' | 'Improvement' | 'BugFix' | 'Announcement';

const CATEGORY_TAG_TYPE: Record<
	CategoryType,
	'' | 'primary' | 'success' | 'warning' | 'danger' | 'info'
> = {
	NewFeature: 'primary',
	Improvement: 'warning',
	BugFix: 'danger',
	Announcement: '',
};

const CATEGORY_LABEL: Record<CategoryType, string> = {
	NewFeature: 'New Feature',
	Improvement: 'Improvement',
	BugFix: 'Bug Fix',
	Announcement: 'Announcement',
};

const getCategoryTagType = (category: string) => {
	return CATEGORY_TAG_TYPE[category as CategoryType] ?? '';
};

const getCategoryLabel = (category: string) => {
	return CATEGORY_LABEL[category as CategoryType] ?? category;
};

const formatRelativeTime = (publishTime: string): string => {
	if (!publishTime) return '';
	return dayjs(publishTime).fromNow();
};
</script>

<style scoped lang="scss">
.whats-new-bell-wrapper {
	display: flex;
	align-items: center;
}

/* 铃铛右上角红点（无数字，纯圆点） */
.unread-dot {
	position: absolute;
	top: 2px;
	right: 2px;
	width: 8px;
	height: 8px;
	border-radius: 50%;
	background-color: #f56c6c;
	border: 1.5px solid #ffffff;
	pointer-events: none;
}

/* 条目左侧未读蓝点 */
.unread-item-dot {
	display: block;
	width: 7px;
	height: 7px;
	border-radius: 50%;
	background-color: #409eff;
	flex-shrink: 0;
}

/* 占位，保持对齐 */
.unread-item-dot-placeholder {
	display: block;
	width: 7px;
	height: 7px;
	flex-shrink: 0;
}

/* 摘要最多2行 */
.summary-clamp {
	display: -webkit-box;
	-webkit-line-clamp: 2;
	-webkit-box-orient: vertical;
	overflow: hidden;
	line-height: 1.4;
}

.panel-item {
	transition: background-color 0.15s ease;
}
</style>
