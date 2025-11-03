import { ref, onMounted } from 'vue';
import { Options } from '#/setting';
import { useUserStore } from '@/stores/modules/user';
import { findUserList } from '@/apis/global';

interface UrlExtension {
	headUrl?: string;
	value: string;
	email?: string;
}

type PeopleOptions = Options & UrlExtension;

const userStore = useUserStore();

export function useInternalNoteUsers(id: string) {
	const initAssign = {
		key: userStore.getUserInfo.userId as string,
		value: `${
			userStore.getUserInfo.userName || userStore.getUserInfo.realName || ''
		}` as string,
		email: `${userStore.getUserInfo.email ? ` ${userStore.getUserInfo.email}` : ''}`,
		headUrl: userStore.getUserInfo?.avatarUrl || '',
	};
	const assignOptions = ref<PeopleOptions[]>([initAssign]);
	const allAssignOptions = ref<PeopleOptions[]>([initAssign]);
	const optionsLoading = ref(false);

	const fetchOptions = async (text?: string) => {
		optionsLoading.value = true;
		try {
			const finUser = await findUserList(id);
			if (finUser.code === '200') {
				const data = finUser.data.map((item) => ({
					key: item.id,
					value: item.name,
					email: item.email,
				}));
				allAssignOptions.value = data;
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
		assignOptions.value = allAssignOptions.value.filter((item) => item.value.includes(text));
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
	};
}
