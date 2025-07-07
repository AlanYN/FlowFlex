import { ref } from 'vue';
import { getDynamicTableData } from '@/apis/global';
import type { DynamicTableRow } from '#/dynamic';

export function useDynamicTableData(moduleType: number) {
	const loading = ref(false);
	const tableData = ref<DynamicTableRow[]>([]);
	const total = ref(0);
	const paginationPages = ref({
		pageIndex: 1,
		pageSize: 15,
	});

	// 加载数据
	const loadData = async (params?: any) => {
		if (loading.value) return;
		loading.value = true;
		tableData.value = [];
		try {
			// 合并分页和筛选参数
			const requestParams = {
				...paginationPages.value,
				...params,
			};
			const res = await getDynamicTableData(moduleType, requestParams);
			if (res.code == '200') {
				tableData.value = res?.data?.data || [];
				total.value = +res?.data?.total || 0;
			}
		} catch (error) {
			console.error('Failed to load table data:', error);
		} finally {
			loading.value = false;
		}
	};

	const handlePageUpdate = (val: number) => {
		paginationPages.value.pageIndex = val;
	};

	const handleLimitUpdate = (val: number) => {
		paginationPages.value.pageSize = val;
	};

	return {
		loading,
		tableData,
		total,
		paginationPages,
		loadData,
		handlePageUpdate,
		handleLimitUpdate,
	};
}
