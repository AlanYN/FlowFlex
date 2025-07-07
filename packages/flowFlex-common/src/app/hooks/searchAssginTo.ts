import { ref, toRaw } from 'vue';
import { Options } from '#/setting';
import { useUserStore } from '@/stores/modules/user';
import { appEnum } from '@/stores/modules/appEnum';

interface UrlExtension {
	headUrl?: string;
	value: string;
	email?: string;
}

type PeopleOptions = Options & UrlExtension;

const userStore = useUserStore();
const enumStore = appEnum();
const ASSIGN_OPTIONS_KEY = 'allAssignOptions'; // 新增统一的存储key

export function assginToOprions(needHeadUrl = false) {
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

	const updateOptions = (newData) => {
		const uniqueOptions = newData.filter((newItem) => initAssign.key !== newItem.value?.key);
		// 创建一个新的 Map 来确保所有项都是唯一的，基于 key 属性
		const allOptions = new Map();
		// 先添加初始分配
		allOptions.set(initAssign.key, initAssign);
		// 添加新的选项，如果 key 已存在则不会重复添加
		uniqueOptions.forEach((option) => {
			if (!allOptions.has(option.key)) {
				allOptions.set(option.key, option);
			}
		});
		// 将 Map 转换回数组赋值给 assignOptions
		assignOptions.value = Array.from(allOptions.values());
		allAssignOptions.value = [
			...new Set([...toRaw(allAssignOptions.value), ...Array.from(allOptions.values())]),
		];
	};

	// 更新本地缓存
	const updateLocalCache = (newData) => {
		// 获取当前缓存的所有选项
		const cachedOptions = enumStore.getAssignOptions(ASSIGN_OPTIONS_KEY);
		// 创建一个新的 Map 来合并现有缓存和新数据
		const mergedOptions = new Map();

		// 添加缓存的选项
		cachedOptions.forEach((option) => {
			mergedOptions.set(option.key, option);
		});

		// 添加新的选项，覆盖已存在的
		newData.forEach((option) => {
			mergedOptions.set(option.key, option);
		});

		// 将合并后的选项存储到缓存中
		enumStore.setAssignOptions(ASSIGN_OPTIONS_KEY, Array.from(mergedOptions.values()));
	};

	// 从缓存中查找匹配文本的选项
	const findMatchOptions = (text) => {
		if (!text) return [];

		const cachedOptions = enumStore.getAssignOptions(ASSIGN_OPTIONS_KEY);
		// 查找匹配文本的选项（值或键包含搜索文本）
		return cachedOptions.filter(
			(option) =>
				String(option.value).toLowerCase().includes(text.toLowerCase()) ||
				String(option.key).toLowerCase().includes(text.toLowerCase())
		);
	};

	const fetchOptions = async (text, limit = 1) => {
		if (!text) return;

		// 先从缓存中查找匹配的选项
		const matchedOptions = findMatchOptions(text);
		if (matchedOptions.length > 0) {
			// 使用缓存中匹配的数据更新选项
			updateOptions(matchedOptions);
			return;
		}

		// 如果缓存中没有匹配项，则调用接口
		optionsLoading.value = true;
		try {
			// 更新显示的选项
			updateOptions([]);
			// 更新本地缓存
			updateLocalCache([]);
			assignOptions.value = [];
		} catch (error) {
			console.error('Error fetching options:', error);
		} finally {
			optionsLoading.value = false;
		}
	};

	const initAssignId = () => {
		return assignOptions.value.length > 0 ? userStore.getUserInfo.userId : '';
	};

	const remoteMethod = (text) => fetchOptions(text);
	const initSearchAssgin = async (value) => {
		if (!value) return;

		const arr = assignOptions.value.map((item) => item.key);
		if (arr.indexOf(value) < 0) {
			// 判断缓存中是否已有匹配的选项
			const matchedOptions = findMatchOptions(value);
			if (matchedOptions.length > 0) {
				updateOptions(matchedOptions);
				return;
			}

			// 如果缓存中没有匹配项，则获取新数据
			await fetchOptions(value, 2);
		}
	};

	return {
		assignOptions,
		allAssignOptions,
		optionsLoading,
		remoteMethod,
		initSearchAssgin,
		initAssignId,
	};
}
