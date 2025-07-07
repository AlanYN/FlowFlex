import { ref } from 'vue';
import { FilterType } from '@/enums/appEnum';
import { assginToOprions } from '@/hooks/searchAssginTo';
import { Filter } from '#/golbal';

export function useFilterOptions(defaultFilters: Filter[]) {
	// 存储所有选项数据
	const optionsMap = ref<Record<string, any[]>>({});
	const { assignOptions, optionsLoading, remoteMethod, initSearchAssgin } = assginToOprions();

	// 获取指定筛选器的选项
	const getFilterOptions = (filter: Filter) => {
		if (filter.filterType !== FilterType.SelectInput) {
			return [];
		}

		// 如果是assignedTo，使用远程搜索
		if (filter.filterKey === 'assignedTo') {
			return optionsMap.value[filter.filterKey] || [];
		}

		// 从defaultFilters中获取选项
		const defaultFilter = defaultFilters.find((f) => f.filterKey === filter.filterKey);
		return defaultFilter?.filterOptions || [];
	};

	// 更新选项数据
	const updateOptions = (key: string, options: any[]) => {
		optionsMap.value[key] = options;
	};

	// 初始化选项数据
	const initOptions = async () => {
		// 处理assignedTo选项
		if (defaultFilters.some((f) => f.filterKey === 'assignedTo')) {
			updateOptions('assignedTo', assignOptions.value);
		}

		// 处理其他选项
		defaultFilters.forEach((filter) => {
			if (filter.filterType === FilterType.SelectInput && filter.filterOptions) {
				updateOptions(filter.filterKey, filter.filterOptions);
			}
		});
	};

	// 远程搜索处理
	const handleRemoteSearch = async (query: string, filter: Filter) => {
		if (filter.filterKey === 'assignedTo') {
			await remoteMethod(query);
			updateOptions('assignedTo', assignOptions.value);
		} else if (filter.remoteMethod) {
			// 如果有自定义的remoteMethod，使用它
			await filter.remoteMethod(query);
			// 更新选项数据
			if (filter.filterOptions) {
				updateOptions(filter.filterKey, filter.filterOptions);
			}
		}
	};

	return {
		getFilterOptions,
		updateOptions,
		initOptions,
		optionsLoading,
		remoteMethod,
		initSearchAssgin,
		assignOptions,
		handleRemoteSearch,
	};
}
