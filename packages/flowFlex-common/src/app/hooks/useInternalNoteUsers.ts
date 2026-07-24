import { ref, onMounted } from 'vue';
import { useUserStore } from '@/stores/modules/user';
import { findUserList } from '@/apis/global';
import { UserType } from '@/enums/permissionEnum';

export interface MentionOption {
	value: string;
	label: string;
	userId?: string;
	email?: string;
	username?: string;
	isExternal?: boolean;
	/** @deprecated 使用 userId 替代，保留用于向后兼容 */
	key?: string;
}

const userStore = useUserStore();

export function useInternalNoteUsers(id: string) {
	const userInfo = userStore.getUserInfo;
	const initAssign: MentionOption = {
		value: userInfo.realName || userInfo.userName || '',
		label: userInfo.realName || userInfo.userName || '',
		userId: userInfo.userId as string,
		username: userInfo.userName || '',
		key: userInfo.userId as string,
	};
	const assignOptions = ref<MentionOption[]>([initAssign]);
	const allAssignOptions = ref<MentionOption[]>([initAssign]);
	const optionsLoading = ref(false);
	const mentionUserMap = ref<
		Map<string, { email: string; username: string; displayName: string }>
	>(new Map());

	const fetchOptions = async (text?: string) => {
		optionsLoading.value = true;
		try {
			const findUser = await findUserList(id);
			if (findUser.code === '200') {
				const data: MentionOption[] = findUser.data
					.filter(
						(item) =>
							item.userType != UserType.SystemAdmin && (item.name || item.username)
					)
					.map((item) => ({
						value: item.name,
						label: item.name,
						userId: item.id,
						email: item.email,
						username: item.username,
						key: item.id,
					}));
				allAssignOptions.value = data;
				assignOptions.value = data;

				// 填充 mentionUserMap（displayName / username → { email, username, displayName }）
				const map = new Map<
					string,
					{ email: string; username: string; displayName: string }
				>();
				findUser.data
					.filter(
						(item) =>
							item.userType != UserType.SystemAdmin && (item.name || item.username)
					)
					.forEach((item) => {
						const info = {
							email: item.email,
							username: item.username,
							displayName: item.name,
						};
						// Map by displayName (primary - for set() when el-mention inserts @displayName)
						if (item.name) {
							map.set(item.name, info);
						}
						// Also map by username (fallback - for editing old data that might have @username)
						if (item.username && item.username !== item.name) {
							map.set(item.username, info);
						}
					});
				mentionUserMap.value = map;
			}
		} catch (error) {
			console.error('Error fetching options:', error);
		} finally {
			optionsLoading.value = false;
		}
	};

	const initAssignId = () => {
		return assignOptions.value.length > 0 ? userStore.getUserInfo.userId : '';
	};

	const remoteMethod = (text?: string) => {
		if (!text) {
			assignOptions.value = allAssignOptions.value;
			return;
		}

		// 过滤匹配的内部用户（按 value/displayName 和 username 匹配）
		const filtered = allAssignOptions.value.filter(
			(item) =>
				item.value.toLowerCase().includes(text.toLowerCase()) ||
				(item.username && item.username.toLowerCase().includes(text.toLowerCase()))
		);

		assignOptions.value = filtered;
	};

	onMounted(() => {
		fetchOptions();
	});

	return {
		assignOptions,
		allAssignOptions,
		optionsLoading,
		remoteMethod,
		initAssignId,
		mentionUserMap,
	};
}
