<template>
	<div>
		<PageHeader
			title="What's New Management"
			description="Create and manage product updates for all users"
		>
			<template #actions>
				<el-button
					type="primary"
					class="page-header-btn page-header-btn-primary"
					@click="handleCreate"
				>
					+ New update
				</el-button>
			</template>
		</PageHeader>

		<!-- 统计卡片 -->
		<div class="stats-row mb-4">
			<div
				class="stat-card"
				:class="{ 'stat-card--active': activeFilter === 1, 'stat-card--clickable': true }"
				@click="handleCardFilter(1)"
			>
				<div class="stat-label">Published</div>
				<div class="stat-value text-green-600 dark:text-green-400">
					{{ publishedCount }}
				</div>
			</div>
			<div
				class="stat-card"
				:class="{ 'stat-card--active': activeFilter === 0, 'stat-card--clickable': true }"
				@click="handleCardFilter(0)"
			>
				<div class="stat-label">Drafts</div>
				<div class="stat-value text-gray-500 dark:text-gray-400">{{ draftCount }}</div>
			</div>
		</div>

		<!-- 条目列表 -->
		<div class="list-container">
			<!-- 加载骨架屏 -->
			<template v-if="loading">
				<div v-for="i in 5" :key="i" class="list-item-skeleton">
					<el-skeleton animated>
						<template #template>
							<div class="flex items-center gap-3 px-5 py-4">
								<el-skeleton-item
									variant="text"
									style="width: 80px; height: 22px"
								/>
								<el-skeleton-item
									variant="text"
									style="width: 60px; height: 22px"
								/>
								<div class="flex-1">
									<el-skeleton-item
										variant="text"
										style="width: 40%; height: 16px"
									/>
									<el-skeleton-item
										variant="text"
										style="width: 70%; height: 13px; margin-top: 6px"
									/>
								</div>
								<el-skeleton-item
									variant="text"
									style="width: 100px; height: 13px"
								/>
							</div>
						</template>
					</el-skeleton>
				</div>
			</template>

			<!-- 空状态 -->
			<div v-else-if="listData.length === 0" class="empty-state">
				<el-empty description="No updates yet. Create your first one!" />
			</div>

			<!-- 列表条目 -->
			<template v-else>
				<div v-for="item in listData" :key="item.id" class="list-item">
					<!-- 左侧标签区 -->
					<div class="item-tags flex items-center gap-2 flex-shrink-0">
						<!-- Category Tag -->
						<el-tag
							:type="getCategoryTagType(item.category)"
							:style="item.category === 'Announcement' ? announcementStyle : {}"
							size="small"
							effect="light"
						>
							{{ getCategoryLabel(item.category) }}
						</el-tag>
						<!-- Status Tag -->
						<el-tag
							:type="item.status === 1 ? 'success' : 'info'"
							size="small"
							effect="light"
						>
							{{ item.status === 1 ? 'Published' : 'Draft' }}
						</el-tag>
					</div>

					<!-- 中间内容区 -->
					<div class="item-content flex-1 min-w-0">
						<div
							class="item-title font-semibold text-sm text-gray-800 dark:text-gray-100 truncate"
						>
							{{ item.title }}
						</div>
						<div
							class="item-summary text-xs text-gray-500 dark:text-gray-400 mt-0.5 summary-clamp"
						>
							{{ item.summary }}
						</div>
					</div>

					<!-- 发布时间 -->
					<div
						class="item-time text-xs text-gray-400 dark:text-gray-500 flex-shrink-0 whitespace-nowrap"
					>
						{{ item.publishTime ? formatDate(item.publishTime) : 'Not published' }}
					</div>

					<!-- 操作按钮 -->
					<div class="item-actions flex items-center gap-1 flex-shrink-0">
						<el-tooltip content="Edit" placement="top">
							<el-button
								link
								size="small"
								:icon="Edit"
								class="action-btn"
								@click="handleEdit(item)"
							/>
						</el-tooltip>
						<el-tooltip content="Delete" placement="top">
							<el-button
								link
								size="small"
								:icon="Delete"
								class="action-btn action-btn-danger"
								@click="handleDeleteClick(item)"
							/>
						</el-tooltip>
					</div>
				</div>
			</template>
		</div>

		<!-- 删除确认弹窗 -->
		<el-dialog
			v-model="deleteDialogVisible"
			title="Delete Update"
			width="480px"
			:close-on-click-modal="false"
			:before-close="handleDeleteCancel"
		>
			<div class="delete-dialog-body">
				<div class="flex items-start gap-3 mb-4">
					<el-icon class="text-red-500 mt-0.5 flex-shrink-0" :size="20">
						<WarningFilled />
					</el-icon>
					<div>
						<p class="text-sm text-gray-700 dark:text-gray-300 mb-1">
							You are about to delete:
						</p>
						<p class="font-semibold text-sm text-gray-800 dark:text-gray-100 mb-3">
							"{{ itemToDelete?.title }}"
						</p>
						<p class="text-sm text-gray-600 dark:text-gray-400">
							This update has been viewed by
							<span class="font-semibold">{{ itemToDelete?.readCount ?? 0 }}</span>
							{{ (itemToDelete?.readCount ?? 0) === 1 ? 'user' : 'users' }}. Deleting
							it will remove it from all users' What's New panel.
						</p>
					</div>
				</div>
			</div>
			<template #footer>
				<div class="flex justify-end gap-2">
					<el-button @click="handleDeleteCancel">Cancel</el-button>
					<el-button type="danger" :loading="deleting" @click="handleDeleteConfirm">
						Delete
					</el-button>
				</div>
			</template>
		</el-dialog>

		<!-- 创建/编辑弹窗 -->
		<WhatsNewFormModal
			v-if="modalVisible"
			:mode="modalMode"
			:item="modalItem"
			@success="handleModalSuccess"
			@close="handleModalClose"
		/>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { ElMessage } from 'element-plus';
import { Edit, Delete, WarningFilled } from '@element-plus/icons-vue';
import dayjs from 'dayjs';
import { useUserStore } from '@/stores/modules/user';
import PageHeader from '@/components/global/PageHeader/index.vue';
import WhatsNewFormModal from './components/WhatsNewFormModal.vue';
import { getAdminList, deleteWhatsNew } from '@/apis/whatsNew';
import type { WhatsNewAdminItem } from '#/whatsNew';

// ========================= Auth Guard =========================

const userStore = useUserStore();
const router = useRouter();

onMounted(() => {
	if (userStore.getUserInfo?.userType !== 1) {
		router.replace('/');
		return;
	}
	loadList();
});

// ========================= List State =========================

const listData = ref<WhatsNewAdminItem[]>([]);
const publishedCount = ref<number>(0);
const draftCount = ref<number>(0);
const loading = ref<boolean>(false);

const activeFilter = ref<0 | 1 | undefined>(undefined);

const handleCardFilter = (status: 0 | 1) => {
	if (activeFilter.value === status) {
		activeFilter.value = undefined;
	} else {
		activeFilter.value = status;
	}
	loadList(activeFilter.value);
};

const loadList = async (status?: number) => {
	loading.value = true;
	try {
		const res = await getAdminList(status);
		listData.value = res.items ?? [];
		publishedCount.value = res.publishedCount ?? 0;
		draftCount.value = res.draftCount ?? 0;
	} catch (e) {
		console.warn('[WhatsNewManagement] Failed to load list', e);
	} finally {
		loading.value = false;
	}
};

// ========================= Delete =========================

const deleteDialogVisible = ref<boolean>(false);
const itemToDelete = ref<WhatsNewAdminItem | null>(null);
const deleting = ref<boolean>(false);

const handleDeleteClick = (item: WhatsNewAdminItem) => {
	itemToDelete.value = item;
	deleteDialogVisible.value = true;
};

const handleDeleteCancel = () => {
	if (deleting.value) return;
	deleteDialogVisible.value = false;
	itemToDelete.value = null;
};

const handleDeleteConfirm = async () => {
	if (!itemToDelete.value || deleting.value) return;
	deleting.value = true;
	try {
		await deleteWhatsNew(itemToDelete.value.id);
		// 从本地列表移除并更新统计卡片
		const deleted = itemToDelete.value;
		listData.value = listData.value.filter((i) => i.id !== deleted.id);
		if (deleted.status === 1) {
			publishedCount.value = Math.max(0, publishedCount.value - 1);
		} else {
			draftCount.value = Math.max(0, draftCount.value - 1);
		}
		deleteDialogVisible.value = false;
		itemToDelete.value = null;
		ElMessage.success('Update deleted successfully.');
	} catch (e) {
		ElMessage.error('Failed to delete update. Please try again.');
	} finally {
		deleting.value = false;
	}
};

// ========================= Create / Edit Modal =========================

const modalVisible = ref<boolean>(false);
const modalMode = ref<'create' | 'edit'>('create');
const modalItem = ref<WhatsNewAdminItem | null>(null);

const handleCreate = () => {
	modalMode.value = 'create';
	modalItem.value = null;
	modalVisible.value = true;
};

const handleEdit = (item: WhatsNewAdminItem) => {
	modalMode.value = 'edit';
	modalItem.value = item;
	modalVisible.value = true;
};

const handleModalSuccess = () => {
	modalVisible.value = false;
	modalItem.value = null;
	loadList();
};

const handleModalClose = () => {
	modalVisible.value = false;
	modalItem.value = null;
};

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

const announcementStyle = {
	'--el-tag-text-color': '#722ED1',
	'--el-tag-border-color': '#d3adf7',
	'--el-tag-bg-color': '#f9f0ff',
};

const getCategoryTagType = (category: string) => {
	return CATEGORY_TAG_TYPE[category as CategoryType] ?? '';
};

const getCategoryLabel = (category: string) => {
	return CATEGORY_LABEL[category as CategoryType] ?? category;
};

const formatDate = (dateStr: string): string => {
	if (!dateStr) return '';
	return dayjs(dateStr).format('MMM D, YYYY');
};
</script>

<style scoped lang="scss">
/* 统计卡片行 */
.stats-row {
	display: flex;
	gap: 12px;
}

.stat-card {
	flex: 0 0 auto;
	min-width: 140px;
	padding: 16px 20px;
	border-radius: 12px;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
	box-shadow: 0 1px 4px rgba(0, 0, 0, 0.06);

	.stat-label {
		font-size: 12px;
		color: var(--el-text-color-secondary);
		margin-bottom: 6px;
		font-weight: 500;
		text-transform: uppercase;
		letter-spacing: 0.5px;
	}

	.stat-value {
		font-size: 28px;
		font-weight: 700;
		line-height: 1.2;
	}

	&--clickable {
		cursor: pointer;
		transition:
			border-color 0.15s ease,
			box-shadow 0.15s ease,
			background-color 0.15s ease;

		&:hover {
			border-color: var(--el-color-primary-light-5);
			box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
		}
	}

	&--active {
		border-color: var(--el-color-primary);
		box-shadow: 0 0 0 2px var(--el-color-primary-light-7);
		background-color: var(--el-color-primary-light-9);
	}
}

/* 列表容器 */
.list-container {
	border-radius: 12px;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
	overflow: hidden;
	box-shadow: 0 1px 4px rgba(0, 0, 0, 0.06);
}

/* 加载骨架 */
.list-item-skeleton {
	border-bottom: 1px solid var(--el-border-color-lighter);

	&:last-child {
		border-bottom: none;
	}
}

/* 空状态 */
.empty-state {
	padding: 48px 0;
	display: flex;
	justify-content: center;
}

/* 列表条目 */
.list-item {
	display: flex;
	align-items: center;
	gap: 16px;
	padding: 14px 20px;
	border-bottom: 1px solid var(--el-border-color-lighter);
	transition: background-color 0.15s ease;

	&:last-child {
		border-bottom: none;
	}

	&:hover {
		background-color: var(--el-fill-color-light);
	}

	.item-tags {
		min-width: 190px;
	}

	.item-time {
		min-width: 100px;
		text-align: right;
	}

	.item-actions {
		min-width: 64px;
		justify-content: flex-end;
	}
}

/* 摘要最多2行截断 */
.summary-clamp {
	display: -webkit-box;
	-webkit-line-clamp: 2;
	-webkit-box-orient: vertical;
	overflow: hidden;
	line-height: 1.4;
}

/* 操作按钮 */
.action-btn {
	color: var(--el-text-color-secondary);

	&:hover {
		color: var(--el-color-primary);
	}

	&.action-btn-danger:hover {
		color: var(--el-color-danger);
	}
}

/* 删除弹窗 */
.delete-dialog-body {
	padding: 4px 0;
}
</style>
