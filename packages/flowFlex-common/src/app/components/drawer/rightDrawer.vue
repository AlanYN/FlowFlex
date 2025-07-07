<template>
	<el-drawer
		v-model="drawer"
		:title="title"
		direction="rtl"
		:before-close="handleCloseDrawer"
		:with-title="false"
		@closed="handleAfterDrawerisClosed"
		:size="dialogWidth"
		class="drawer"
	>
		<div
			v-loading="props.loading"
			class="flex flex-col h-full overflow-auto relative pr-2"
			@scroll="loadMoreActivity"
		>
			<el-empty
				v-show="props.loading === false && activityList.length === 0"
				description="No activity history"
			/>
			<activityCard
				ref="activityCardRef"
				v-for="activity in activityList"
				:key="activity.id"
				:info="activity"
				:selectedActivity="selectedActivity"
				:userOptions="props.userList"
				@open-comment-field="handleOpenComment"
				@open-delete-dialog="handleOpenDeleteConfirmation"
			/>
			<p :class="`sticky w-full text-center bottom-0 ${loadMore ? 'visible' : 'invisible'}`">
				{{ loadMoreText }}
			</p>
		</div>
	</el-drawer>
	<el-dialog
		v-model="displayDeleteConfirmation"
		:closed="() => (deleteCommentId = '')"
		:before-close="handleCloseDeleteConfirmation"
		:width="dialogWidth"
		:title="t('sys.app.logoutTip')"
	>
		<span>Are you sure you want to delete this comment?</span>
		<template #footer>
			<div class="dialog-footer">
				<el-button
					@click="() => (displayDeleteConfirmation = false)"
					:disabled="deleteLoading"
				>
					Cancel
				</el-button>
				<el-button
					class="capitalize"
					type="danger"
					@click="handleDeleteSelectedComment"
					:loading="deleteLoading"
				>
					Delete
				</el-button>
			</div>
		</template>
	</el-dialog>
</template>

<script lang="ts" setup>
import { ref, watch, defineExpose } from 'vue';
import activityCard from './components/activityCard.vue';
import { deleteComment, updateCommentCount } from '@/apis/comments/index';
import { dialogWidth } from '@/settings/projectSetting';

import { ElMessage } from 'element-plus';
import { useI18n } from '@/hooks/useI18n';

const { t } = useI18n();

interface Props {
	open: boolean;
	list: any;
	title: string;
	loading: boolean;
	userList: any;
}

const activityCardRef = ref(null);
const emit = defineEmits(['closeDrawer', 'clearActivityHistoryList', 'loadMoreActivityList']);
let props = defineProps<Props>();
let activityList = ref(props.list);
let drawer = ref(false);
let selectedActivity = ref('');
let displayDeleteConfirmation = ref(false);
let deleteCommentId = ref('');
let deleteLoading = ref(false);
let deleteCallback = ref();
let loadMore = ref(false);
let loadMoreText = ref('Loading more...');

const loadMoreActivity = (event) => {
	const tracker = event.target;
	// Check if scrolled to the bottom
	if (tracker.scrollTop + tracker.clientHeight >= tracker.scrollHeight - 0.6) {
		loadMore.value = true;

		console.info('more');
		emit('loadMoreActivityList');
	}
};

const handleCloseDrawer = () => {
	emit('closeDrawer');
};

const handleAfterDrawerisClosed = () => {
	selectedActivity.value = '';
	emit('clearActivityHistoryList');
};

const handleOpenComment = (id: string) => {
	selectedActivity.value = id;
};

const handleOpenDeleteConfirmation = (id: string, cb: any, activityId: string) => {
	selectedActivity.value = activityId;
	deleteCommentId.value = id;
	deleteCallback.value = cb;
	displayDeleteConfirmation.value = true;
};

const handleCloseDeleteConfirmation = (done: () => void) => {
	if (deleteLoading.value === false) {
		done();

		deleteCommentId.value = '';
		deleteCallback.value = undefined;
	}
};

const handleDeleteSelectedComment = () => {
	deleteLoading.value = true;
	deleteComment(deleteCommentId.value)
		.then((res) => {
			if (res.code == '200') {
				ElMessage({
					type: 'success',
					message: 'Comment deleted',
				});

				deleteCommentId.value = '';
				displayDeleteConfirmation.value = false;

				// reload the list
				deleteCallback.value(true);
				handleGetUpdatedCommentCount();
			}
		})
		.finally(() => {
			deleteLoading.value = false;
		});
};

const handleGetUpdatedCommentCount = () => {
	const row = activityList.value.find((n) => n.id === selectedActivity.value);
	if (row) {
		updateCommentCount({
			tagerId: row.tagerId,
			tagerType: row.tagerType,
			sourceId: row.sourceId,
		}).then((res) => {
			if (res.code == '200') {
				const { data } = res;
				activityList.value.forEach((item: any) => {
					if (item.id === data.id) {
						item.commentCount = data.commentCount;
					}
				});
			}
		});
	}
};

watch(props, () => {
	drawer.value = props.open;
	activityList.value = props.list;
});

const setLoadMoreText = (text: string) => {
	loadMoreText.value = text;
};

watch(activityList.value, () => {
	console.info(activityList.value);
});

defineExpose({ loadMore, setLoadMoreText });
</script>

<style lang="scss">
.drawer {
	.el-drawer__header {
		margin-bottom: 0px;
	}

	.el-drawer__body {
		padding-left: 12px;
		padding-right: 12px;
	}
}
</style>
