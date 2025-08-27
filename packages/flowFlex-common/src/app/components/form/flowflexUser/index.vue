<template>
	<div class="flowflex-user-selector">
		<el-tree-select
			v-model="selectedValue"
			:data="treeData"
			:props="treeProps"
			:placeholder="computedPlaceholder"
			:disabled="disabled"
			:clearable="clearable"
			:multiple="multiple"
			:show-checkbox="multiple"
			:check-strictly="checkStrictly"
			:filterable="true"
			:filter-node-method="filterNode"
			:loading="loading"
			:popper-class="popperClass"
			:teleported="teleported"
			node-key="id"
			class="w-full"
			@clear="handleClear"
			@visible-change="handleVisibleChange"
		>
			<template #default="{ data }">
				<div class="flex items-center space-x-2">
					<el-icon v-if="data.type === 'team'" class="text-blue-500">
						<UserFilled />
					</el-icon>
					<el-icon v-else-if="data.type === 'user'" class="text-green-500">
						<User />
					</el-icon>
					<el-icon v-else class="text-gray-400">
						<Folder />
					</el-icon>

					<span class="flex-1 truncate" :title="data.name">{{ data.name }}</span>

					<span
						v-if="data.type === 'team' && data.memberCount"
						class="text-xs text-gray-400"
					>
						({{ data.memberCount }})
					</span>

					<span
						v-if="data.userDetails?.email && showEmail"
						class="text-xs text-gray-400 truncate max-w-[150px]"
						:title="data.userDetails.email"
					>
						{{ data.userDetails.email }}
					</span>
				</div>
			</template>
		</el-tree-select>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, nextTick } from 'vue';
import { User, UserFilled, Folder } from '@element-plus/icons-vue';
import { ElMessage } from 'element-plus';
import { getFlowflexUser } from '@/apis/global';
import type { FlowflexUser } from '#/golbal';

interface Props {
	modelValue?: string | string[];
	placeholder?: string;
	disabled?: boolean;
	multiple?: boolean;
	checkStrictly?: boolean;
	clearable?: boolean;
	popperClass?: string;
	teleported?: boolean;
	pageSize?: number;
	showEmail?: boolean;
	teamOnly?: boolean; // 仅允许选择团队
}

interface Emits {
	(e: 'update:modelValue', value: string | string[]): void;
	(e: 'change', value: string | string[], selectedData?: FlowflexUser | FlowflexUser[]): void;
	(e: 'clear'): void;
	(e: 'visible-change', visible: boolean): void;
}

const props = withDefaults(defineProps<Props>(), {
	modelValue: undefined,
	placeholder: '',
	disabled: false,
	multiple: false,
	checkStrictly: false,
	clearable: true,
	popperClass: '',
	teleported: true,
	pageSize: 50,
	showEmail: true,
	teamOnly: false,
});

// 计算 placeholder
const computedPlaceholder = computed(() => {
	if (props.placeholder) return props.placeholder;
	return props.teamOnly ? 'Please select team' : 'Please select user or team';
});

const emit = defineEmits<Emits>();

// 数据相关
const treeData = ref<FlowflexUser[]>([]);
const loading = ref(false);
const userDataMap = ref<Map<string, FlowflexUser>>(new Map());
const searchText = ref('');

// 树形配置
const treeProps = {
	value: 'id',
	label: 'name',
	children: 'children',
};

// 使用 computed 实现双向绑定
const selectedValue = computed({
	get: () => props.modelValue,
	set: (value: string | string[]) => {
		emit('update:modelValue', value);

		// 获取选中的数据对象
		const selectedData = getSelectedData(value);
		emit('change', value, selectedData);
	},
});

// 获取选中数据的详细信息
const getSelectedData = (value: string | string[]): FlowflexUser | FlowflexUser[] | undefined => {
	if (!value || (Array.isArray(value) && value.length === 0)) return undefined;

	if (Array.isArray(value)) {
		return value.map((id) => userDataMap.value.get(id)).filter(Boolean) as FlowflexUser[];
	} else {
		return userDataMap.value.get(value);
	}
};

// 构建用户数据映射
const buildUserDataMap = (data: FlowflexUser[], clear = false) => {
	if (clear) {
		userDataMap.value.clear();
	}

	const traverse = (items: FlowflexUser[]) => {
		items.forEach((item) => {
			userDataMap.value.set(item.id, item);
			if (item.children && item.children.length > 0) {
				traverse(item.children);
			}
		});
	};

	traverse(data);
};

// 过滤节点方法，支持搜索
const filterNode = (value: string, data: FlowflexUser) => {
	if (!value) return true;
	const searchValue = value.toLowerCase();
	return (
		data.name.toLowerCase().includes(searchValue) ||
		(data.userDetails?.email && data.userDetails.email.toLowerCase().includes(searchValue))
	);
};

// 清空事件
const handleClear = () => {
	searchText.value = '';
	emit('clear');
};

// 下拉框显示隐藏事件
const handleVisibleChange = (visible: boolean) => {
	emit('visible-change', visible);
	// 改为懒加载策略：只在首次打开且没有数据时加载
	if (visible && treeData.value.length === 0 && !loading.value) {
		initializeData();
	}
};

// 初始化加载数据（优化版）
const initializeData = async (searchQuery = '') => {
	if (loading.value) return; // 防止重复加载

	try {
		loading.value = true;

		const response = await getFlowflexUser({
			searchText: searchQuery,
		});

		if (response.code === '200' && response.data) {
			// 确保数据结构正确，为没有子节点的项目添加空数组
			const processedData = processTreeData(response.data);
			treeData.value = processedData;
			buildUserDataMap(processedData, true);
		} else {
			ElMessage.warning('Failed to load user data');
			treeData.value = [];
		}
	} catch (error) {
		console.error('Failed to initialize user data:', error);
		ElMessage.error('Failed to load user data, please try again later');
		treeData.value = [];
	} finally {
		loading.value = false;
	}
};

// 处理树形数据，确保结构正确
const processTreeData = (data: FlowflexUser[]): FlowflexUser[] => {
	if (!Array.isArray(data)) return [];

	return data
		.filter((item) => {
			// 如果设置了 teamOnly，过滤掉用户节点
			if (props.teamOnly && item.type === 'user') {
				return false;
			}
			return true;
		})
		.map((item) => ({
			...item,
			// 递归处理子节点，确保每个节点都有 children 数组
			children:
				item.children && Array.isArray(item.children) ? processTreeData(item.children) : [],
		}));
};

// 根据ID获取用户名称（用于后期ID转换为名称的功能）
const getUserNameById = (id: string): string => {
	const user = userDataMap.value.get(id);
	return user ? user.name : id;
};

// 根据ID数组获取用户名称数组
const getUserNamesByIds = (ids: string[]): string[] => {
	return ids.map((id) => getUserNameById(id));
};

// 获取用户详细信息
const getUserById = (id: string): FlowflexUser | undefined => {
	return userDataMap.value.get(id);
};

// 获取多个用户详细信息
const getUsersByIds = (ids: string[]): FlowflexUser[] => {
	return ids.map((id) => getUserById(id)).filter(Boolean) as FlowflexUser[];
};

// 组件挂载时初始化
onMounted(() => {
	// 延迟初始化，避免首次加载时的重复请求
	// 只在没有禁用且没有数据时初始化
	if (!props.disabled && treeData.value.length === 0) {
		// 使用 nextTick 确保组件完全渲染后再加载数据
		nextTick(() => {
			initializeData();
		});
	}
});

// 组件卸载时清理
onUnmounted(() => {
	userDataMap.value.clear();
	treeData.value = [];
	searchText.value = '';
});

// 暴露方法供外部使用
defineExpose({
	getUserNameById,
	getUserNamesByIds,
	getUserById,
	getUsersByIds,
	refreshData: initializeData,
	clearCache: () => {
		userDataMap.value.clear();
		treeData.value = [];
		searchText.value = '';
	},
	getSelectedData: () => {
		const value = selectedValue.value;
		return value ? getSelectedData(value) : undefined;
	},
});
</script>

<style scoped lang="scss">
.flowflex-user-selector {
	width: 100%;
	.el-tree-select {
		width: 100%;
	}

	:deep(.el-tree-node__content) {
		height: auto;
		min-height: 32px;
		padding: 6px 0;
		align-items: center;

		&:hover {
			background-color: #f5f7fa;
		}
	}

	:deep(.el-tree-node__label) {
		flex: 1;
		overflow: hidden;
	}

	:deep(.el-select-dropdown__item) {
		height: auto;
		line-height: normal;
		padding: 8px 12px;
	}

	:deep(.el-tree) {
		.el-tree-node__expand-icon {
			padding: 2px;
		}

		.is-leaf .el-tree-node__expand-icon {
			color: transparent;
		}
	}
}
</style>
