<template>
	<div class="grid grid-cols-1 lg:grid-cols-2 gap-6 p-1">
		<!-- 左侧：View Permission -->
		<div class="space-y-4 w-full">
			<div class="space-y-2">
				<label class="text-base font-bold">View Permission</label>

				<el-select
					v-model="localPermissions.viewPermissionMode"
					placeholder="Select permission type"
					class="w-full"
					@change="leftTypeChange"
				>
					<el-option
						v-for="option in permissionTypeOptions"
						:key="option.value"
						:label="option.label"
						:value="option.value"
					/>
				</el-select>
			</div>

			<!-- Team（仅在 VisibleToTeams 或 InvisibleToTeams 时显示）-->
			<div v-if="shouldShowTeamSelector" class="space-y-2">
				<label class="text-base font-bold">Team</label>
				<FlowflexUserSelector
					ref="viewTeamSelectorRef"
					v-model="localPermissions.viewTeams"
					selectionType="team"
					:clearable="true"
					@change="leftChange"
				/>
			</div>
		</div>

		<!-- 右侧：Operate Permission -->
		<div class="space-y-4 w-full">
			<div class="space-y-2">
				<label class="text-base font-bold">Operate Permission</label>

				<el-checkbox v-model="localPermissions.useSameGroups">
					{{ checkboxLabel }}
				</el-checkbox>
			</div>

			<!-- Team（仅在不勾选 useSameGroups 时显示）-->
			<div v-if="!localPermissions.useSameGroups" class="space-y-2">
				<label class="text-base font-bold">Team</label>
				<FlowflexUserSelector
					v-model="localPermissions.operateTeams"
					selectionType="team"
					:clearable="true"
					:choosable-tree-data="operateChoosableTreeData"
				/>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { reactive, computed, ref, toRaw } from 'vue';
import { ViewPermissionModeEnum } from '@/enums/permissionEnum';
import FlowflexUserSelector from '@/components/form/flowflexUser/index.vue';
import { menuRoles } from '@/stores/modules/menuFunction';
import type { FlowflexUser } from '#/golbal';

// Props
interface Props {
	modelValue?: {
		viewPermissionMode: number;
		viewTeams: string[];
		useSameGroups: boolean;
		operateTeams: string[];
	};
}

const props = withDefaults(defineProps<Props>(), {
	modelValue: () => ({
		viewPermissionMode: ViewPermissionModeEnum.Public,
		viewTeams: [],
		useSameGroups: true,
		operateTeams: [],
	}),
});

// Emits
// const emit = defineEmits(['update:modelValue']);

// 权限类型选项
const permissionTypeOptions = [
	{ label: 'Public', value: ViewPermissionModeEnum.Public },
	{ label: 'Visible to', value: ViewPermissionModeEnum.VisibleToTeams },
	{ label: 'Invisible to', value: ViewPermissionModeEnum.InvisibleToTeams },
];

// 是否需要显示 Team 选择器 - 只有 VisibleToTeams 和 InvisibleToTeams 需要选择团队
const shouldShowTeamSelector = computed(() => {
	const mode = localPermissions.viewPermissionMode;
	// Public 模式下不显示
	return (
		mode === ViewPermissionModeEnum.VisibleToTeams ||
		mode === ViewPermissionModeEnum.InvisibleToTeams
	);
});

// 动态checkbox文案 - Invisible模式显示特殊文案
const checkboxLabel = computed(() => {
	const mode = localPermissions.viewPermissionMode;
	if (mode === ViewPermissionModeEnum.InvisibleToTeams) {
		return 'Use same team that have view permission (not editable)';
	}
	return 'Use same team that have view permission';
});

// 本地权限数据
const localPermissions = reactive({
	viewPermissionMode: props.modelValue.viewPermissionMode ?? ViewPermissionModeEnum.Public,
	viewTeams: [...(props.modelValue.viewTeams || [])],
	useSameGroups: props.modelValue.useSameGroups ?? true,
	operateTeams: [...(props.modelValue.operateTeams || [])],
});

// 使用一个 ref 来跟踪是否正在处理内部更新
// const isProcessingInternalUpdate = ref(false);

// 跟踪上一次的viewPermissionMode和useSameGroups状态
// const previousViewPermissionMode = ref(localPermissions.viewPermissionMode);
// const previousUseSameGroups = ref(localPermissions.useSameGroups);

// 左侧 Team 选择器的 ref
const viewTeamSelectorRef = ref<InstanceType<typeof FlowflexUserSelector> | null>(null);

// 获取 menuStore 实例
const menuStore = menuRoles();

// 右侧可选的树形数据
const operateChoosableTreeData = ref<FlowflexUser[] | undefined>(undefined);

// 从树形数据中提取所有 team ID（递归）
// const extractAllTeamIds = (data: FlowflexUser[]): string[] => {
// 	const ids: string[] = [];
// 	const extract = (nodes: FlowflexUser[]) => {
// 		for (const node of nodes) {
// 			if (node.type === 'team') {
// 				ids.push(node.id);
// 			}
// 			if (node.children && node.children.length > 0) {
// 				extract(node.children);
// 			}
// 		}
// 	};
// 	extract(data);
// 	return ids;
// };

// 从完整的树中排除指定的节点（Invisible to 模式用）
// const excludeNodesFromTree = (
// 	fullTree: FlowflexUser[],
// 	excludeTree: FlowflexUser[]
// ): FlowflexUser[] => {
// 	// 先提取要排除的所有 team ID
// 	const excludeIds = new Set(extractAllTeamIds(excludeTree));

// 	// 递归过滤树形数据
// 	const filterTree = (nodes: FlowflexUser[]): FlowflexUser[] => {
// 		const result: FlowflexUser[] = [];

// 		for (const node of nodes) {
// 			// 如果当前节点是要排除的 team，跳过
// 			if (node.type === 'team' && excludeIds.has(node.id)) {
// 				continue;
// 			}

// 			// 保留当前节点，但需要递归处理子节点
// 			const newNode: FlowflexUser = { ...node };
// 			if (newNode.children && newNode.children.length > 0) {
// 				newNode.children = filterTree(newNode.children);
// 			}

// 			result.push(newNode);
// 		}

// 		return result;
// 	};

// 	return filterTree(fullTree);
// };

// 更新右侧可选的树形数据
// const updateOperateChoosableTreeData = async () => {
// 	const mode = localPermissions.viewPermissionMode;

// 	// Public 模式：不限制
// 	if (mode === ViewPermissionModeEnum.Public) {
// 		operateChoosableTreeData.value = undefined;
// 		return;
// 	}

// 	// 获取左侧选中的树形数据
// 	const selectedData = viewTeamSelectorRef.value?.getSelectedData?.() || [];

// 	// Visible to 模式：直接使用左侧选中的树形数据
// 	if (mode === ViewPermissionModeEnum.VisibleToTeams) {
// 		operateChoosableTreeData.value = selectedData.length > 0 ? selectedData : [];
// 		return;
// 	}

// 	// Invisible to 模式：从完整树中排除左侧选中的节点
// 	if (mode === ViewPermissionModeEnum.InvisibleToTeams) {
// 		// 获取完整的 team 树数据
// 		const fullTreeData = await menuStore.getFlowflexUserDataWithCache('');

// 		// 排除左侧选中的节点
// 		if (selectedData.length > 0) {
// 			operateChoosableTreeData.value = excludeNodesFromTree(fullTreeData, selectedData);
// 		} else {
// 			// 如果左侧没有选择，右侧可以选择所有
// 			operateChoosableTreeData.value = undefined;
// 		}
// 		return;
// 	}

// 	operateChoosableTreeData.value = undefined;
// };

// 过滤右侧已选数据，移除不在可选范围内的 team
// const filterOperateTeams = (): string[] => {
// 	if (localPermissions.operateTeams.length === 0) {
// 		return [];
// 	}

// 	// Public 模式或没有限制时，不过滤
// 	if (!operateChoosableTreeData.value) {
// 		return localPermissions.operateTeams;
// 	}

// 	// 从可选树形数据中提取所有可选的 team ID
// 	const availableIds = new Set(extractAllTeamIds(operateChoosableTreeData.value));

// 	// 只保留在可选范围内的 team
// 	return localPermissions.operateTeams.filter((teamId) => availableIds.has(teamId));
// };

// 处理右侧team选择器获得焦点事件
// const handleOperateFocus = () => {
// 	// 如果左侧未选择team，显示提示
// 	if (shouldShowTeamSelector.value && localPermissions.viewTeams.length === 0) {
// 		ElMessage.warning('Please select View Permission teams first');
// 	}
// };

// 统一的数据处理函数
// const processPermissionChanges = async () => {
// 	if (isProcessingInternalUpdate.value) return;

// 	// 先设置标志位，防止内部修改触发 watch
// 	isProcessingInternalUpdate.value = true;

// 	// 使用 nextTick 确保在下一个事件循环中处理
// 	await nextTick();

// 	// 处理 viewPermissionMode 的变化 - Public 模式下清空 viewTeams
// 	if (!shouldShowTeamSelector.value && localPermissions.viewTeams.length > 0) {
// 		localPermissions.viewTeams = [];
// 	}

// 	// 检测viewPermissionMode在VisibleToTeams和InvisibleToTeams之间切换，清空operateTeams
// 	const currentMode = localPermissions.viewPermissionMode;
// 	const previousMode = previousViewPermissionMode.value;
// 	const isModeSwitchBetweenVisibleAndInvisible =
// 		(currentMode === ViewPermissionModeEnum.VisibleToTeams &&
// 			previousMode === ViewPermissionModeEnum.InvisibleToTeams) ||
// 		(currentMode === ViewPermissionModeEnum.InvisibleToTeams &&
// 			previousMode === ViewPermissionModeEnum.VisibleToTeams);

// 	if (isModeSwitchBetweenVisibleAndInvisible) {
// 		localPermissions.operateTeams = [];
// 	}

// 	// 更新previousViewPermissionMode
// 	previousViewPermissionMode.value = currentMode;

// 	// 更新右侧可选的树形数据
// 	await updateOperateChoosableTreeData();

// 	// 检测useSameGroups变化
// 	const currentUseSameGroups = localPermissions.useSameGroups;
// 	const previousSameGroups = previousUseSameGroups.value;

// 	// 处理 operateTeams 的同步
// 	if (currentUseSameGroups) {
// 		// 勾选"使用相同团队"时，同步 viewTeams 到 operateTeams
// 		const newOperateTeams = shouldShowTeamSelector.value ? [...localPermissions.viewTeams] : [];
// 		if (JSON.stringify(newOperateTeams) !== JSON.stringify(localPermissions.operateTeams)) {
// 			localPermissions.operateTeams = newOperateTeams;
// 		}
// 	} else if (previousSameGroups && !currentUseSameGroups) {
// 		// 从勾选变为取消勾选时，清空operateTeams
// 		localPermissions.operateTeams = [];
// 	} else if (!currentUseSameGroups && localPermissions.operateTeams.length > 0) {
// 		// 当 useSameGroups 未勾选且 operateTeams 有数据时，过滤无效的 team
// 		const filteredOperateTeams = filterOperateTeams();

// 		// 如果过滤后数量减少，静默移除无效选项
// 		if (filteredOperateTeams.length < localPermissions.operateTeams.length) {
// 			localPermissions.operateTeams = filteredOperateTeams;
// 		}
// 	}

// 	// 更新previousUseSameGroups
// 	previousUseSameGroups.value = currentUseSameGroups;

// 	// emit 更新到父组件
// 	emit('update:modelValue', {
// 		viewPermissionMode: localPermissions.viewPermissionMode,
// 		viewTeams: [...localPermissions.viewTeams],
// 		useSameGroups: localPermissions.useSameGroups,
// 		operateTeams: [...localPermissions.operateTeams],
// 	});

// 	// 重置标志位
// 	await nextTick();
// 	isProcessingInternalUpdate.value = false;
// };

// 只监听 localPermissions 的变化，统一处理
// watch(
// 	localPermissions,
// 	() => {
// 		processPermissionChanges();
// 	},
// 	{ deep: true }
// );

const leftTypeChange = () => {
	const treedata = viewTeamSelectorRef.value?.getSelectedData() || [];
	leftChange(localPermissions.viewTeams, treedata);
};

const leftChange = async (value, selectData) => {
	console.log('=== leftChange called ===');
	console.log('value (selected IDs):', value);
	console.log('selectData:', selectData);
	console.log('viewPermissionMode:', localPermissions.viewPermissionMode);

	const mode = localPermissions.viewPermissionMode;

	// Convert selectData to array if it's not already
	const selectedNodes = Array.isArray(selectData) ? toRaw(selectData) : [toRaw(selectData)];
	console.log('selectedNodes:', selectedNodes);

	// Build a map of selected IDs for quick lookup
	const selectedIdSet = new Set<string>(value as string[]);

	// Build parent-child relationship map
	const nodeMap = new Map<string, FlowflexUser>();
	const childToParentMap = new Map<string, string>();

	function buildMaps(nodes: FlowflexUser[], parentId?: string) {
		nodes.forEach((node) => {
			nodeMap.set(node.id, node);
			if (parentId) {
				childToParentMap.set(node.id, parentId);
			}
			if (node.children && node.children.length > 0) {
				buildMaps(node.children, node.id);
			}
		});
	}

	buildMaps(selectedNodes);

	// Whitelist mode: only keep selected nodes (avoid parent-child duplicates)
	if (mode === ViewPermissionModeEnum.VisibleToTeams) {
		console.log('Whitelist mode: filtering selected nodes');

		// Filter logic: avoid duplicates (parent + children both selected)
		const resultIds = new Set<string>();

		selectedIdSet.forEach((nodeId: string) => {
			const node = nodeMap.get(nodeId);
			if (!node) {
				console.log(`Node ${nodeId} not found in map`);
				return;
			}

			// Check if any parent is also selected
			let currentId: string = nodeId;
			let hasSelectedParent = false;

			while (childToParentMap.has(currentId)) {
				const parentId: string = childToParentMap.get(currentId)!;
				if (selectedIdSet.has(parentId)) {
					console.log(
						`Node ${nodeId} (${node.name}) has selected parent ${parentId}, skipping`
					);
					hasSelectedParent = true;
					break;
				}
				currentId = parentId;
			}

			// Only add if no parent is selected
			if (!hasSelectedParent) {
				console.log(`Adding node ${nodeId} (${node.name})`);
				resultIds.add(nodeId);
			}
		});

		// Build final result array
		const newTreeData = Array.from(resultIds)
			.map((id: string) => nodeMap.get(id)!)
			.filter(Boolean);
		console.log('Final newTreeData (whitelist):', newTreeData);
		operateChoosableTreeData.value = newTreeData.length > 0 ? newTreeData : [];
		return;
	}

	// Blacklist mode: exclude selected nodes from full tree
	if (mode === ViewPermissionModeEnum.InvisibleToTeams) {
		console.log('Blacklist mode: excluding selected nodes from full tree');

		// Get full tree data
		const fullTreeData = await menuStore.getFlowflexUserDataWithCache('');
		console.log('Full tree data:', fullTreeData);

		// Extract all selected IDs (including children if parent is selected)
		const excludeIds = new Set<string>();

		const collectNodeAndChildren = (nodeId: string) => {
			excludeIds.add(nodeId);
			const node = nodeMap.get(nodeId);
			if (node && node.children && node.children.length > 0) {
				node.children.forEach((child) => {
					collectNodeAndChildren(child.id);
				});
			}
		};

		selectedIdSet.forEach((nodeId: string) => {
			collectNodeAndChildren(nodeId);
		});

		console.log('Exclude IDs:', Array.from(excludeIds));

		// Recursively filter tree to exclude selected nodes
		const filterTree = (nodes: FlowflexUser[]): FlowflexUser[] => {
			const result: FlowflexUser[] = [];

			for (const node of nodes) {
				// Skip excluded nodes
				if (excludeIds.has(node.id)) {
					console.log(`Excluding node: ${node.id} (${node.name})`);
					continue;
				}

				// Keep node but filter children
				const newNode: FlowflexUser = { ...node };
				if (newNode.children && newNode.children.length > 0) {
					newNode.children = filterTree(newNode.children);
				}

				result.push(newNode);
			}

			return result;
		};

		const filteredTreeData = filterTree(fullTreeData);
		console.log('Final filtered tree (blacklist):', filteredTreeData);
		operateChoosableTreeData.value = filteredTreeData.length > 0 ? filteredTreeData : [];
		return;
	}

	// Public mode or other modes
	console.log('Public or other mode: no filtering');
	operateChoosableTreeData.value = undefined;
};
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
