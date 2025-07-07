<template>
	<div class="flex gap-4">
		<div class="w-2 h-full bg-gray-200 relative ml-2">
			<el-icon :size="25" class="bg-white-100 pt-5 pb-5 absolute left-[-7px] top-5">
				<Tickets v-if="activity.tagerType === 4" />
				<ChatDotRound v-else-if="activity.tagerType === 6" />
				<InfoFilled v-else />
			</el-icon>
		</div>
		<div class="border-b-2 w-full pb-5 pt-5">
			<div class="flex justify-between">
				<h4 class="font-bold max-w-60 break-normal">{{ activity.title }}</h4>
				<p class="text-sm text-gray-400">
					{{ timeZoneConvert(activity.modifyDate, false, projectTenMinutesSsecondsDate) }}
				</p>
			</div>
			<p class="text-sm text-gray-400 my-2">{{ activity.createBy }}</p>
			<div>
				<p>{{ activity.notes }}</p>
			</div>
			<div
				v-show="activity.tagerType !== 5"
				class="flex gap-2 items-center hover:bg-gray-200 pl-1 pr-1 mt-1 w-fit cursor-pointer select-none"
				@click="() => (hideComments = !hideComments)"
			>
				<el-icon>
					<ChatDotSquare />
				</el-icon>
				<p v-if="hideComments" class="text-[13px] text-primary-500">
					{{ `${commentTotalCount} comment${commentTotalCount > 1 ? 's' : ''}` }}
				</p>
				<p v-else class="text-[13px] text-primary-500">Hide Comments</p>
			</div>
			<el-collapse-transition>
				<div
					v-loading="loading"
					v-show="hideComments === false"
					class="flex flex-col gap-2 min-h-10 pt-2"
				>
					<!-- <el-empty
						v-show="loading === false && commentList.length === 0"
						description="No comments"
					/> -->
					<el-divider class="mt-2 mb-1" />
					<ul class="infinite-list" @scroll="load">
						<li v-for="item in commentList" :key="item.id" class="group">
							<div class="flex gap-3 w-full">
								<el-avatar :icon="UserFilled" class="min-w-[40px]" />
								<div
									v-if="commentIdToEdit !== item.id"
									class="flex flex-col gap-1 w-full"
								>
									<div class="flex justify-between">
										<p class="font-bold text-[13px]">{{ item.createBy }}</p>
										<div class="flex gap-3 items-center">
											<el-button-group
												v-show="userName === item.createBy"
												class="invisible group-hover:visible"
											>
												<el-button
													size="small"
													type="primary"
													:icon="Edit"
													@click="handleEditComment(item)"
													:disabled="
														savingComment || savingUpdatedComment
													"
												/>
												<el-button
													size="small"
													type="primary"
													:icon="Delete"
													@click="
														handleDisplayDeleteConfirmation(item.id)
													"
													:disabled="
														savingComment || savingUpdatedComment
													"
												/>
											</el-button-group>
											<p class="text-[13px]">
												{{
													timeZoneConvert(
														item.createDate,
														false,
														projectTenMinutesSsecondsDate
													)
												}}
											</p>
										</div>
									</div>
									<p class="text-wrap text-[13px]" v-html="item.content"></p>
								</div>
								<div
									v-else-if="commentIdToEdit === item.id"
									class="flex flex-col gap-1 w-full"
								>
									<comment
										:options="props.userOptions"
										placeholder="Leave a comment"
										:value="editCommentText"
										@on-input="handleEditCommentText"
									/>
									<div class="flex">
										<el-button
											type="primary"
											@click="handlePostComment('edit')"
											:loading="savingUpdatedComment"
											class="text-[13px]"
										>
											Save
										</el-button>
										<el-button
											type="info"
											@click="handleCancelEdit"
											:disabled="savingUpdatedComment"
											class="text-[13px]"
										>
											Cancel
										</el-button>
									</div>
								</div>
							</div>
						</li>
					</ul>
					<p v-if="loadMore" class="text-center text-[13px]">Loading...</p>
					<p v-if="noNewComments" class="text-center text-[13px]">No more comments</p>
					<div>
						<comment
							ref="commentRef"
							:options="props.userOptions"
							placeholder="Leave a comment"
							:value="commentText"
							:disabled="savingComment"
							@on-input="handleUpdateCommentText"
						/>
						<div class="flex">
							<el-button
								type="primary"
								@click="handlePostComment('save')"
								:loading="savingComment"
							>
								Save
							</el-button>
						</div>
					</div>
				</div>
			</el-collapse-transition>
		</div>
	</div>
</template>

<script lang="ts" setup>
import { ref, onMounted, watch } from 'vue';

import { useUserStore } from '@/stores/modules/user';

import { timeZoneConvert } from '@/hooks/time';
import { projectTenMinutesSsecondsDate } from '@/settings/projectSetting';

import comment from '@/components/commentField/comment.vue';
import { editComment, getComment, postComment, updateCommentCount } from '@/apis/comments/index';

import {
	ChatDotSquare,
	UserFilled,
	Edit,
	Delete,
	Tickets,
	ChatDotRound,
	InfoFilled,
} from '@element-plus/icons-vue';

import { ElMessage } from 'element-plus';
import { CommentList, ActivityCard } from '#/leadAndDeal';

interface Props {
	info: ActivityCard;
	selectedActivity: string;
	userOptions: any;
}

const commentRef = ref<{
	handleClearContent: () => void;
}>();
const userStore = useUserStore();
const props = defineProps<Props>();
const emits = defineEmits(['openCommentField', 'openDeleteDialog']);
const activityItemInfo = ref(props.info);
const userName = ref(userStore.getUserInfo.userName);

let activity = ref<ActivityCard>(props.info);
let showCommentField = ref(false);
let hideComments = ref(true);
let commentTotalCount = ref(props.info.commentCount);
let commentText = ref('');
let loading = ref(false);
let loadMore = ref(false);
let noNewComments = ref(false);
let commentIdToEdit = ref('');
let commentList = ref<CommentList[]>([]);
let savingComment = ref(false);
let savingUpdatedComment = ref(false);
let editCommentText = ref('');
let currentPageLength = ref(10);

const load = (event) => {
	const tracker = event.target;

	// Check if scrolled to the bottom
	if (
		tracker.scrollTop + tracker.clientHeight >= tracker.scrollHeight - 1 &&
		savingComment.value === false &&
		savingUpdatedComment.value === false &&
		loadMore.value === false
	) {
		currentPageLength.value = currentPageLength.value + 10;
		loadMore.value = true;

		handleGetCommentsList(false);
	}
};

const handleGetCommentsList = (backdropLoad: boolean) => {
	if (backdropLoad) loading.value = true;
	getComment({
		TargetId: activityItemInfo.value.tagerId,
		TargetType: activityItemInfo.value.tagerType,
		IsPC: true,
		PageIndex: 0,
		PageSize: currentPageLength.value,
	})
		.then((res) => {
			if (res.code == '200') {
				const { data } = res.data;

				data.forEach((obj: any) => {
					obj.content =
						typeof obj.content === 'string'
							? obj.content
									.replace(/\n/g, '<br>')
									.replace(/\[~(.*?)\]/g, '<strong>@$1</strong>')
							: '';
				});

				if (backdropLoad === false && commentList.value.length === data.length) {
					noNewComments.value = true;
				}

				commentList.value = data;

				commentTotalCount.value = res.data.dataCount;
			}
		})
		.finally(() => {
			loading.value = false;
			loadMore.value = false;
			setTimeout(() => {
				noNewComments.value = false;
			}, 1000);
		});
};

// const handleShowCommentField = () => {
// 	showCommentField.value = true;
// 	emits('openCommentField', activity.value.id);
// };

// const handleCloseCommentField = () => {
// 	showCommentField.value = false;
// 	commentText.value = '';
// 	emits('openCommentField', '');
// };

const handleUpdateCommentText = (str: string) => {
	commentText.value = str;
};

const handleEditCommentText = (str: string) => {
	editCommentText.value = str;
};

const handlePostComment = async (type: string) => {
	const convertComment = (str: string) => {
		// Create a regex pattern from our mentioned usernames
		const userPattern = Array.from(props.userOptions)
			.map(
				(user: any) =>
					// Escape special regex characters in usernames
					`@${user.value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}`
			)
			.join('|');

		if (!userPattern) return; // No mentions to convert

		const mentionRegex = new RegExp(`(${userPattern})`, 'g');

		const convertedText = str.replace(mentionRegex, (match) => {
			// Remove the @ and convert to [~username] format
			const username = match.substring(1);
			return `[~${username}]`;
		});

		return convertedText;
	};

	let res;

	switch (type) {
		case 'save':
			if (commentText.value === '') {
				ElMessage({
					type: 'error',
					message: 'Please leave a comment',
				});
				return;
			}
			try {
				savingComment.value = true;
				res = await postComment({
					targetId: activityItemInfo.value.tagerId,
					targetType: activityItemInfo.value.tagerType,
					content: convertComment(commentText.value) || '',
				});
			} finally {
				savingComment.value = false;
			}
			break;
		case 'edit':
			if (editCommentText.value === '') {
				ElMessage({
					type: 'error',
					message: 'Please leave a comment',
				});
				return;
			}
			savingUpdatedComment.value = true;
			try {
				res = await editComment({
					id: commentIdToEdit.value,
					targetId: activityItemInfo.value.tagerId,
					targetType: activityItemInfo.value.tagerType,
					content: convertComment(editCommentText.value) || '',
				});
			} finally {
				savingUpdatedComment.value = false;
			}
			break;
	}

	if (res.code == '200') {
		let msg = '';
		switch (type) {
			case 'save':
				handleGetUpdatedCommentCount();
				msg = 'Comment posted';
				break;
			case 'edit':
				msg = 'Comment edited';
				break;
		}

		ElMessage({
			type: 'success',
			message: msg,
		});

		// clear the text area
		commentText.value = '';
		commentRef.value && commentRef.value.handleClearContent();

		// clear the edit text area
		handleCancelEdit();

		// reload the list
		handleGetCommentsList(true);
	}
};

const handleEditComment = (comment) => {
	const { id, content } = comment;
	commentIdToEdit.value = id;
	editCommentText.value =
		typeof content === 'string'
			? content.replace(/<br>/g, '\n').replace(/<strong>@(.*?)<\/strong>/g, '@$1')
			: '';
};

const handleCancelEdit = () => {
	commentIdToEdit.value = '';
	editCommentText.value = '';
};

const handleDisplayDeleteConfirmation = (id: string) => {
	emits('openDeleteDialog', id, handleGetCommentsList, activityItemInfo.value.id);
};

const handleGetUpdatedCommentCount = () => {
	updateCommentCount({
		tagerId: activityItemInfo.value.tagerId,
		tagerType: activityItemInfo.value.tagerType,
		sourceId: activityItemInfo.value.sourceId,
	}).then((res) => {
		if (res.code == '200') {
			commentTotalCount.value = res.data.commentCount;
		}
	});
};

onMounted(() => {
	activity.value = props.info;
});

watch(props, () => {
	if (props.selectedActivity !== activity.value.id) {
		showCommentField.value = false;
	}
});

watch(hideComments, () => {
	if (hideComments.value === false) {
		// reset the page index
		currentPageLength.value = 10;
		handleGetCommentsList(true);
	}
});
</script>

<style scoped>
.infinite-list {
	max-height: 200px;
	padding: 0;
	margin: 0;
	list-style: none;
	display: flex;
	flex-direction: column;
	gap: 10px;
	overflow: auto;
}
</style>
