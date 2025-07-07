import { ref, computed } from 'vue';
import type { TableColumn, TableAndFilter, Filters, Column } from '#/dynamic';
import { getTableViewInfo, getModuleTypeColumns } from '@/apis/global';
import { DynamicFieldsList } from '#/leadAndDeal';
import { useUserStoreWithOut } from '@/stores/modules/user';

export function useDynamicTableConfig() {
	const config = ref<TableAndFilter | null>(null);
	const loading = ref(false);
	const userStore = useUserStoreWithOut();

	// 列配置相关
	const userColumn = computed<Column[] & DynamicFieldsList[]>(
		() => config.value?.columns.sort((a, b) => a.displayOrder - b.displayOrder) || []
	);
	const filters = computed<Filters[] & DynamicFieldsList[]>(
		() => config.value?.filters.sort((a, b) => a.displayOrder - b.displayOrder) || []
	);
	const allColums = ref<TableColumn[]>([]);
	const canEditView = computed(() => {
		return config.value?.createUserId === userStore.getUserInfo.userId;
	});

	// 加载配置
	const loadConfig = async (modeuleType: number, id: string) => {
		loading.value = true;
		try {
			await Promise.all([getViewInfor(id), getAllViewInfor(modeuleType)]);
		} catch (error) {
			console.error('Failed to load dynamic table config:', error);
		} finally {
			loading.value = false;
		}
	};

	const getViewInfor = async (id: string) => {
		try {
			const res = await getTableViewInfo(id);
			if (res.code == '200') {
				config.value = res.data;
			}
		} catch (error) {
			config.value = null;
		}
	};

	const getAllViewInfor = async (modeuleType: number) => {
		try {
			const res = await getModuleTypeColumns(modeuleType);
			if (res.code == '200') {
				allColums.value = res.data;
			}
		} catch (error) {
			allColums.value = [];
		}
	};

	return {
		loading,
		userColumn, // 用户选择的列
		canEditView, // 是否可以编辑视图信息
		filters, // 筛选项
		allColums, // 所有的列
		loadConfig,
		getViewInfor,
		getAllViewInfor,
	};
}
