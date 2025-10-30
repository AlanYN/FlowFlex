<template>
	<div class="grid grid-cols-2 gap-6 p-1 divide-x divide-gray-300">
		<!-- 左侧：View Permission -->
		<div class="space-y-4 w-full">
			<div class="space-y-2">
				<label class="text-base font-bold">View Permission</label>

				<el-select
					v-model="localPermissions.viewPermissionMode"
					placeholder="Select permission type"
					class="w-full"
					@change="updateViewPermissionMode"
				>
					<el-option
						v-for="option in permissionTypeOptions"
						:key="option.value"
						:label="option.label"
						:value="option.value"
					/>
				</el-select>
			</div>

			<!-- Team（仅在 VisibleTo 或 InvisibleTo 时显示）-->
			<div v-if="shouldShowTeamSelector" class="space-y-2 flex flex-col">
				<label class="text-base font-bold">Team</label>
				<FlowflexUserSelector
					ref="viewTeamSelectorRef"
					v-model="localPermissions.viewTeams"
					selectionType="team"
					:clearable="true"
					:choosable-tree-data="viewChoosableTreeData"
					@change="updateOperateTeamOptions()"
				/>
			</div>
		</div>

		<!-- 右侧：Operate Permission -->
		<div class="space-y-4 w-full pl-4">
			<div class="space-y-2">
				<label class="text-base font-bold">Operate Permission</label>

				<el-checkbox
					:class="{
						invisible:
							localPermissions.viewPermissionMode ===
							ViewPermissionModeEnum.InvisibleTo,
					}"
					v-model="localPermissions.useSameTeamForOperate"
				>
					Use same team that have view permission
				</el-checkbox>
			</div>

			<!-- Team（仅在不勾选 useSameTeamForOperate 时显示）-->
			<div
				v-if="
					!localPermissions.useSameTeamForOperate ||
					localPermissions.viewPermissionMode === ViewPermissionModeEnum.InvisibleTo
				"
				class="space-y-2 flex flex-col"
			>
				<label class="text-base font-bold">Team</label>
				<FlowflexUserSelector
					v-model="localPermissions.operateTeams"
					selectionType="team"
					:clearable="true"
					:choosable-tree-data="operateChoosableTreeData"
					:before-open="handleBeforeOpen"
				/>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { reactive, computed, ref, watch, nextTick, onMounted } from 'vue';
import { ViewPermissionModeEnum } from '@/enums/permissionEnum';
import FlowflexUserSelector from '@/components/form/flowflexUser/index.vue';
import { menuRoles } from '@/stores/modules/menuFunction';
import type { FlowflexUser } from '#/golbal';
import { ElMessage } from 'element-plus';
// Props
interface Props {
	modelValue?: {
		viewPermissionMode: number;
		viewTeams: string[];
		useSameTeamForOperate: boolean;
		operateTeams: string[];
	};
	viewLimitData?: string[];
	operateLimitData?: string[];
	workFlowViewPermissionMode?: number;
	workFlowViewUseSameTeamForOperate?: boolean;
	isWorkflowLevel?: boolean; // 是否是 workflow 级别调用，true 时不使用 workflow 过滤
}

const props = withDefaults(defineProps<Props>(), {
	modelValue: () => ({
		viewPermissionMode: ViewPermissionModeEnum.Public,
		viewTeams: [],
		useSameTeamForOperate: true,
		operateTeams: [],
	}),
	viewLimitData: () => [],
	operateLimitData: () => [],
	workFlowViewPermissionMode: undefined,
	workFlowViewUseSameTeamForOperate: undefined,
	isWorkflowLevel: false,
});

// Emits
const emit = defineEmits(['update:modelValue']);

// 权限类型选项
const permissionTypeOptions = [
	{ label: 'Public', value: ViewPermissionModeEnum.Public },
	{ label: 'Visible to', value: ViewPermissionModeEnum.VisibleTo },
	{ label: 'Invisible to', value: ViewPermissionModeEnum.InvisibleTo },
];

// 是否需要显示 Team 选择器 - 只有 VisibleTo 和 InvisibleTo 需要选择团队
const shouldShowTeamSelector = computed(() => {
	const mode = localPermissions.viewPermissionMode;
	// Public 模式下不显示
	return mode === ViewPermissionModeEnum.VisibleTo || mode === ViewPermissionModeEnum.InvisibleTo;
});

// 本地权限数据
const localPermissions = reactive({
	viewPermissionMode: props.modelValue.viewPermissionMode ?? ViewPermissionModeEnum.Public,
	viewTeams: [...(props.modelValue.viewTeams || [])],
	useSameTeamForOperate: props.modelValue.useSameTeamForOperate ?? true,
	operateTeams: [...(props.modelValue.operateTeams || [])],
});

// 左侧 Team 选择器的 ref
const viewTeamSelectorRef = ref<InstanceType<typeof FlowflexUserSelector> | null>(null);

// 获取 menuStore 实例
const menuStore = menuRoles();

// 右侧可选的树形数据
const operateChoosableTreeData = ref<FlowflexUser[] | undefined>(undefined);
const viewChoosableTreeData = ref<FlowflexUser[] | undefined>(undefined);
const fullTreeDataCache = ref<FlowflexUser[] | null>(null);

// 使用一个 ref 来跟踪是否正在处理内部更新
const isProcessingInternalUpdate = ref(false);

interface TreeMaps {
	nodeMap: Map<string, FlowflexUser>;
	childToParentMap: Map<string, string>;
}

const handleBeforeOpen = async () => {
	if (
		localPermissions.viewPermissionMode !== ViewPermissionModeEnum.Public &&
		localPermissions.viewTeams.length === 0
	) {
		ElMessage.warning('Please select a team for view permission');
		return false;
	}
	return true;
};

const getFullTreeData = async (): Promise<FlowflexUser[]> => {
	if (!fullTreeDataCache.value) {
		const data = await menuStore.getFlowflexUserDataWithCache('');
		fullTreeDataCache.value = Array.isArray(data) ? data : [];
	}
	return fullTreeDataCache.value!;
};

// 将树形用户数据扁平化，方便做 ID 级别的集合运算
const buildTreeMaps = (nodes: FlowflexUser[]): TreeMaps => {
	const nodeMap = new Map<string, FlowflexUser>();
	const childToParentMap = new Map<string, string>();

	const walk = (items: FlowflexUser[], parentId?: string) => {
		items.forEach((node) => {
			nodeMap.set(node.id, node);
			if (parentId) {
				childToParentMap.set(node.id, parentId);
			}
			if (node.children && node.children.length > 0) {
				walk(node.children, node.id);
			}
		});
	};

	walk(nodes);
	return { nodeMap, childToParentMap };
};

// 第一层过滤：根据 view 限制确定左侧可选的团队树
const updateViewChoosableTreeData = async () => {
	// 如果是 workflow 级别调用，左侧显示全部数据
	if (props.isWorkflowLevel) {
		viewChoosableTreeData.value = undefined; // undefined 表示不限制，显示全部数据
		return;
	}

	// 第一层过滤：使用 viewLimitData 过滤全部数据，得到左侧可选数据 A
	const limitIds = props.viewLimitData ?? [];
	const mode =
		props.workFlowViewPermissionMode !== undefined
			? props.workFlowViewPermissionMode
			: undefined;

	if (mode === undefined || mode === ViewPermissionModeEnum.Public) {
		viewChoosableTreeData.value = undefined;
		return;
	}

	const fullTreeData = await getFullTreeData();
	if (!fullTreeData || fullTreeData.length === 0) {
		viewChoosableTreeData.value = [];
		return;
	}

	const { nodeMap, childToParentMap } = buildTreeMaps(fullTreeData);

	if (mode === ViewPermissionModeEnum.VisibleTo) {
		if (!limitIds.length) {
			viewChoosableTreeData.value = [];
			return;
		}

		const limitSet = new Set<string>(limitIds);
		const resultIds = new Set<string>();

		limitIds.forEach((nodeId) => {
			if (!nodeMap.has(nodeId)) {
				return;
			}

			let currentId = nodeId;
			let hasSelectedParent = false;

			while (childToParentMap.has(currentId)) {
				const parentId = childToParentMap.get(currentId)!;
				if (limitSet.has(parentId)) {
					hasSelectedParent = true;
					break;
				}
				currentId = parentId;
			}

			if (!hasSelectedParent) {
				resultIds.add(nodeId);
			}
		});

		const cloneNode = (node: FlowflexUser): FlowflexUser => {
			const newNode: FlowflexUser = { ...node };
			if (newNode.children && newNode.children.length > 0) {
				// 过滤掉 user 类型的子节点，只保留 team
				newNode.children = newNode.children
					.filter((child) => child.type === 'team')
					.map((child) => cloneNode(child));
			}
			return newNode;
		};

		const whitelistTree = Array.from(resultIds)
			.map((id) => nodeMap.get(id))
			.filter(Boolean)
			.map((node) => cloneNode(node!));

		viewChoosableTreeData.value = whitelistTree.length > 0 ? whitelistTree : [];
		return;
	}

	// Blacklist (InvisibleTo)
	if (!limitIds.length) {
		viewChoosableTreeData.value = undefined;
		return;
	}

	const excludeIds = new Set<string>();
	const collectNodeAndChildren = (nodeId: string) => {
		if (excludeIds.has(nodeId)) return;
		excludeIds.add(nodeId);
		const node = nodeMap.get(nodeId);
		if (node && node.children && node.children.length > 0) {
			node.children.forEach((child) => collectNodeAndChildren(child.id));
		}
	};

	limitIds.forEach((nodeId) => {
		if (nodeMap.has(nodeId)) {
			collectNodeAndChildren(nodeId);
		}
	});

	const filterTree = (nodes: FlowflexUser[]): FlowflexUser[] => {
		const result: FlowflexUser[] = [];
		nodes.forEach((node) => {
			// 跳过被排除的节点
			if (excludeIds.has(node.id)) {
				return;
			}
			// 只保留 team 类型的节点，过滤掉 user
			if (node.type !== 'team') {
				return;
			}
			const newNode: FlowflexUser = { ...node };
			if (newNode.children && newNode.children.length > 0) {
				newNode.children = filterTree(newNode.children);
			}
			result.push(newNode);
		});
		return result;
	};

	const filteredTree = filterTree(fullTreeData);
	viewChoosableTreeData.value = filteredTree.length > 0 ? filteredTree : [];
};
//
const updateViewPermissionMode = async () => {
	// 清空右侧已选的 operateTeams
	localPermissions.operateTeams = [];
	// 先更新左侧可选数据（第一层过滤）
	await updateViewChoosableTreeData();
	// 再根据左侧已选数据过滤右侧（第二层过滤）
	await updateOperateTeamOptions();
};

onMounted(() => {
	nextTick(() => {
		updateViewChoosableTreeData().then(() => {
			updateOperateTeamOptions(false);
		});
	});
});

const updateOperateTeamOptions = async (needEditLocalPermissions: boolean = true) => {
	const mode = localPermissions.viewPermissionMode;

	// InvisibleTo 模式下不能复用“查看”team
	if (mode === ViewPermissionModeEnum.InvisibleTo) {
		localPermissions.useSameTeamForOperate = false;
	}

	const fullTreeData = await getFullTreeData();
	const { nodeMap, childToParentMap } = buildTreeMaps(fullTreeData);

	const collectTeamIds = (nodes?: FlowflexUser[]): Set<string> => {
		const ids = new Set<string>();
		if (!nodes) return ids;

		const traverse = (items: FlowflexUser[]) => {
			items.forEach((item) => {
				if (item.type === 'team') {
					ids.add(item.id);
				}
				if (item.children && item.children.length > 0) {
					traverse(item.children);
				}
			});
		};

		traverse(nodes);
		return ids;
	};

	// 第一层数据A：根据 view 支持的数据权限集
	const availableViewTeams =
		viewChoosableTreeData.value === undefined
			? collectTeamIds(fullTreeData)
			: collectTeamIds(viewChoosableTreeData.value);

	if (availableViewTeams.size === 0) {
		operateChoosableTreeData.value = [];
		return;
	}

	const selectedViewTeams = new Set<string>(localPermissions.viewTeams);

	// VisibleTo 但左边未选时直接清空右边
	if (selectedViewTeams.size === 0 && mode === ViewPermissionModeEnum.VisibleTo) {
		operateChoosableTreeData.value = undefined;
		return;
	}

	const subtractSets = (source: Set<string>, toRemove: Set<string>) => {
		const result = new Set<string>();
		source.forEach((id) => {
			if (!toRemove.has(id)) {
				result.add(id);
			}
		});
		return result;
	};

	const intersectSets = (a: Set<string>, b: Set<string>) => {
		const result = new Set<string>();
		a.forEach((id) => {
			if (b.has(id)) {
				result.add(id);
			}
		});
		return result;
	};

	let baseAvailableIds = new Set<string>();

	if (props.isWorkflowLevel) {
		// 工作流级别：右边仅取决于左边勾选
		if (mode === ViewPermissionModeEnum.VisibleTo) {
			baseAvailableIds = selectedViewTeams;
			if (baseAvailableIds.size === 0) {
				operateChoosableTreeData.value = undefined;
				return;
			}
		} else if (mode === ViewPermissionModeEnum.Public) {
			baseAvailableIds = availableViewTeams;
		} else {
			baseAvailableIds = subtractSets(availableViewTeams, selectedViewTeams);
		}
	} else {
		// Stage 级别：叠加 operateLimitData 限制
		const operateLimitData = new Set<string>(props.operateLimitData || []);

		if (mode === ViewPermissionModeEnum.VisibleTo) {
			baseAvailableIds =
				operateLimitData.size === 0
					? selectedViewTeams
					: intersectSets(selectedViewTeams, operateLimitData);

			if (selectedViewTeams.size === 0 && operateLimitData.size > 0) {
				baseAvailableIds = operateLimitData;
			}

			if (baseAvailableIds.size === 0 && selectedViewTeams.size > 0) {
				operateChoosableTreeData.value = [];
				return;
			}
		} else if (mode === ViewPermissionModeEnum.Public) {
			const shouldInheritWorkflow =
				props.workFlowViewPermissionMode === ViewPermissionModeEnum.Public &&
				props.workFlowViewUseSameTeamForOperate;
			baseAvailableIds = shouldInheritWorkflow ? availableViewTeams : operateLimitData;
		} else {
			const remainingTeams = subtractSets(availableViewTeams, selectedViewTeams);
			baseAvailableIds =
				operateLimitData.size === 0
					? remainingTeams
					: intersectSets(remainingTeams, operateLimitData);
		}
	}

	// 构建树：保证父子不重复
	const resultIds = new Set<string>();
	baseAvailableIds.forEach((nodeId) => {
		const node = nodeMap.get(nodeId);
		if (!node) return;

		let currentId: string | undefined = nodeId;
		let hasSelectedParent = false;

		while (currentId && childToParentMap.has(currentId)) {
			const parentId = childToParentMap.get(currentId)!;
			if (baseAvailableIds.has(parentId)) {
				hasSelectedParent = true;
				break;
			}
			currentId = parentId;
		}

		if (!hasSelectedParent) {
			resultIds.add(nodeId);
		}
	});

	const buildFilteredNode = (node: FlowflexUser): FlowflexUser => {
		const cloned: FlowflexUser = { ...node };
		if (cloned.children && cloned.children.length > 0) {
			cloned.children = cloned.children
				.filter((child) => baseAvailableIds.has(child.id))
				.map((child) => buildFilteredNode(child));
		}
		return cloned;
	};

	const newTreeData = Array.from(resultIds)
		.map((id) => nodeMap.get(id))
		.filter(Boolean)
		.map((node) => buildFilteredNode(node!));

	operateChoosableTreeData.value = newTreeData.length > 0 ? newTreeData : [];

	if (localPermissions.operateTeams.length > 0 && needEditLocalPermissions) {
		const validOperateTeams = localPermissions.operateTeams.filter((teamId) =>
			baseAvailableIds.has(teamId)
		);
		if (validOperateTeams.length !== localPermissions.operateTeams.length) {
			localPermissions.operateTeams = validOperateTeams;
		}
	}
};

// 统一的数据处理函数
const processPermissionChanges = () => {
	if (isProcessingInternalUpdate.value) return;

	// 先设置标志位，防止内部修改触发 watch
	isProcessingInternalUpdate.value = true;

	// 使用 nextTick 确保在下一个事件循环中处理
	nextTick(() => {
		//处理 viewPermissionMode 的变化
		if (!shouldShowTeamSelector.value) {
			//Public 模式清空
			if (localPermissions.viewTeams.length > 0) {
				localPermissions.viewTeams = [];
			}
		}

		// 处理 operateTeams 的同步
		// InvisibleTo 模式下不同步，因为右侧是左侧的反选

		if (
			localPermissions.useSameTeamForOperate &&
			localPermissions.viewPermissionMode !== ViewPermissionModeEnum.InvisibleTo
		) {
			// 勾选"使用相同"时，同步 view 的选择到 operate
			const newOperateTeams = shouldShowTeamSelector.value
				? [...localPermissions.viewTeams]
				: [];

			if (JSON.stringify(newOperateTeams) !== JSON.stringify(localPermissions.operateTeams)) {
				localPermissions.operateTeams = newOperateTeams;
			}
		}

		// emit 更新到父组件
		emit('update:modelValue', {
			viewPermissionMode: localPermissions.viewPermissionMode,
			viewTeams: [...localPermissions.viewTeams],
			useSameTeamForOperate: localPermissions.useSameTeamForOperate,
			operateTeams: [...localPermissions.operateTeams],
		});

		// 重置标志位
		nextTick(() => {
			isProcessingInternalUpdate.value = false;
		});
	});
};

// 监听 localPermissions 的变化，统一处理
watch(
	localPermissions,
	() => {
		processPermissionChanges();
	},
	{ deep: true }
);

// 同步外部 v-model 到本地状态，避免始终回退到 Public
watch(
	() => props.modelValue,
	(newVal) => {
		if (!newVal || isProcessingInternalUpdate.value) return;

		const hasChanges =
			localPermissions.viewPermissionMode !== newVal.viewPermissionMode ||
			JSON.stringify(localPermissions.viewTeams) !== JSON.stringify(newVal.viewTeams) ||
			localPermissions.useSameTeamForOperate !== newVal.useSameTeamForOperate ||
			JSON.stringify(localPermissions.operateTeams) !== JSON.stringify(newVal.operateTeams);

		if (hasChanges) {
			isProcessingInternalUpdate.value = true;
			localPermissions.viewPermissionMode =
				newVal.viewPermissionMode ?? ViewPermissionModeEnum.Public;
			localPermissions.viewTeams = [...(newVal.viewTeams || [])];
			localPermissions.useSameTeamForOperate = newVal.useSameTeamForOperate ?? true;
			localPermissions.operateTeams = [...(newVal.operateTeams || [])];

			nextTick(() => {
				isProcessingInternalUpdate.value = false;
				updateViewChoosableTreeData();
			});
		}
	},
	{ deep: true }
);

// 限制数据或工作流可见模式变化时，刷新可选树数据
watch(
	() => props.viewLimitData,
	() => {
		updateViewChoosableTreeData();
	},
	{ deep: true, immediate: true }
);

watch(
	() => props.workFlowViewPermissionMode,
	() => {
		updateViewChoosableTreeData();
	},
	{ immediate: true }
);

watch(
	() => localPermissions.viewPermissionMode,
	() => {
		updateViewChoosableTreeData();
	}
);
</script>

<style scoped>
/* 确保 checkbox label 换行显示 */
:deep(.el-checkbox__label) {
	white-space: normal;
	line-height: 1.5;
}
/* :deep(.el-tag.el-tag--default) { */
/* } */
</style>
