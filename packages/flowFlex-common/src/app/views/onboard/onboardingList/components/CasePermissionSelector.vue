<template>
	<div class="grid grid-cols-1 lg:grid-cols-2 gap-6 p-1">
		<!-- 左侧：View Permission -->
		<div class="space-y-4 w-full">
			<div class="space-y-2">
				<label class="text-base font-bold">View Permission</label>
				<p class="text-sm">Controls who can view this case</p>

				<el-select
					v-model="localPermissions.viewPermissionMode"
					placeholder="Select permission type"
					class="w-full"
				>
					<el-option
						v-for="option in permissionTypeOptions"
						:key="option.value"
						:label="option.label"
						:value="option.value"
					/>
				</el-select>

				<!-- Public/Private 提示文本 -->
				<p
					v-if="localPermissions.viewPermissionMode === CasePermissionModeEnum.Public"
					class="text-sm text-gray-500 mt-2"
				>
					Public (Inherit from workflow)
				</p>
				<p
					v-else-if="
						localPermissions.viewPermissionMode === CasePermissionModeEnum.Private
					"
					class="text-sm text-gray-500 mt-2"
				>
					Private (Only owner can view)
				</p>
			</div>

			<!-- Team/User 选择（仅在 VisibleToTeams 或 InvisibleToTeams 时显示）-->
			<div v-if="shouldShowSelector" class="space-y-2">
				<label class="text-base font-bold">Team</label>

				<!-- 单选按钮：User Groups / Individual Users -->
				<el-radio-group v-model="localPermissions.viewPermissionSubjectType">
					<el-radio :value="PermissionSubjectTypeEnum.Team">User Teams</el-radio>
					<el-radio :value="PermissionSubjectTypeEnum.User">Individual Users</el-radio>
				</el-radio-group>

				<!-- Team 选择器 -->
				<FlowflexUserSelector
					v-show="
						localPermissions.viewPermissionSubjectType ===
						PermissionSubjectTypeEnum.Team
					"
					ref="viewTeamSelectorRef"
					v-model="localPermissions.viewTeams"
					selection-type="team"
					clearable
					@change="handleLeftChange"
				/>

				<!-- User 选择器 -->
				<FlowflexUserSelector
					v-show="
						localPermissions.viewPermissionSubjectType ===
						PermissionSubjectTypeEnum.User
					"
					ref="viewUserSelectorRef"
					v-model="localPermissions.viewUsers"
					selection-type="user"
					:clearable="true"
					@change="handleLeftChange"
				/>
			</div>
		</div>

		<!-- 右侧：Operate Permission -->
		<div class="space-y-4 w-full">
			<div class="space-y-2">
				<label class="text-base font-bold">Operate Permission</label>
				<p class="text-sm">Controls who can operate on this case</p>

				<el-checkbox
					v-if="shouldShowSelector"
					v-model="localPermissions.useSameGroups"
					:class="{
						invisible:
							localPermissions.viewPermissionMode ===
							CasePermissionModeEnum.InvisibleToTeams,
					}"
				>
					Use same teams and users that have view permission
				</el-checkbox>

				<!-- Public/Private 提示文本 -->
				<p
					v-if="localPermissions.viewPermissionMode === CasePermissionModeEnum.Public"
					class="text-sm text-gray-500 mt-2"
				>
					Public (Inherit from workflow)
				</p>
				<p
					v-else-if="
						localPermissions.viewPermissionMode === CasePermissionModeEnum.Private
					"
					class="text-sm text-gray-500 mt-2"
				>
					Private (Only owner can operate)
				</p>
			</div>

			<!-- Available Teams/Users（仅在不勾选 useSameGroups 时显示）-->
			<div
				v-if="
					(!localPermissions.useSameGroups && shouldShowSelector) ||
					localPermissions.viewPermissionMode === CasePermissionModeEnum.InvisibleToTeams
				"
				class="space-y-2"
			>
				<label class="text-base font-bold">Teams</label>

				<!-- 单选按钮：User Groups / Individual Users -->
				<el-radio-group v-model="localPermissions.operatePermissionSubjectType">
					<el-radio
						:value="PermissionSubjectTypeEnum.Team"
						:disabled="
							localPermissions.viewPermissionSubjectType ===
							PermissionSubjectTypeEnum.User
						"
					>
						User Teams
					</el-radio>
					<el-radio :value="PermissionSubjectTypeEnum.User">Individual Users</el-radio>
				</el-radio-group>

				<!-- Team 选择器 -->
				<FlowflexUserSelector
					v-show="
						localPermissions.operatePermissionSubjectType ===
						PermissionSubjectTypeEnum.Team
					"
					v-model="localPermissions.operateTeams"
					selection-type="team"
					:clearable="true"
					:choosable-tree-data="operateChoosableTreeData"
				/>

				<!-- User 选择器 -->
				<FlowflexUserSelector
					v-show="
						localPermissions.operatePermissionSubjectType ===
						PermissionSubjectTypeEnum.User
					"
					v-model="localPermissions.operateUsers"
					selection-type="user"
					:clearable="true"
					:choosable-tree-data="operateChoosableTreeData"
				/>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { reactive, computed, watch, nextTick, ref, onMounted } from 'vue';
import { CasePermissionModeEnum, PermissionSubjectTypeEnum } from '@/enums/permissionEnum';
import FlowflexUserSelector from '@/components/form/flowflexUser/index.vue';
import { menuRoles } from '@/stores/modules/menuFunction';
import type { FlowflexUser } from '#/golbal';

// Props
interface Props {
	modelValue?: {
		viewPermissionMode: number;
		viewTeams: string[];
		viewUsers: string[];
		viewPermissionSubjectType: number;
		useSameGroups: boolean;
		operateTeams: string[];
		operateUsers: string[];
		operatePermissionSubjectType: number;
	};
}

const props = withDefaults(defineProps<Props>(), {
	modelValue: () => ({
		viewPermissionMode: CasePermissionModeEnum.Public,
		viewTeams: [],
		viewUsers: [],
		viewPermissionSubjectType: PermissionSubjectTypeEnum.Team,
		useSameGroups: true,
		operateTeams: [],
		operateUsers: [],
		operatePermissionSubjectType: PermissionSubjectTypeEnum.Team,
	}),
});

// Emits
const emit = defineEmits(['update:modelValue']);

// 权限类型选项
const permissionTypeOptions = [
	{ label: 'Public', value: CasePermissionModeEnum.Public },
	{ label: 'Visible to', value: CasePermissionModeEnum.VisibleToTeams },
	{ label: 'Invisible to', value: CasePermissionModeEnum.InvisibleToTeams },
	{ label: 'Private', value: CasePermissionModeEnum.Private },
];

// 是否需要显示选择器 - 只有 VisibleToTeams 和 InvisibleToTeams 需要选择
const shouldShowSelector = computed(() => {
	const mode = localPermissions.viewPermissionMode;
	return (
		mode === CasePermissionModeEnum.VisibleToTeams ||
		mode === CasePermissionModeEnum.InvisibleToTeams
	);
});

// 本地权限数据
const localPermissions = reactive({
	viewPermissionMode: props.modelValue.viewPermissionMode ?? CasePermissionModeEnum.Public,
	viewTeams: [...(props.modelValue.viewTeams || [])],
	viewUsers: [...(props.modelValue.viewUsers || [])],
	viewPermissionSubjectType:
		props.modelValue.viewPermissionSubjectType ?? PermissionSubjectTypeEnum.Team,
	useSameGroups: props.modelValue.useSameGroups ?? true,
	operateTeams: [...(props.modelValue.operateTeams || [])],
	operateUsers: [...(props.modelValue.operateUsers || [])],
	operatePermissionSubjectType:
		props.modelValue.operatePermissionSubjectType ?? PermissionSubjectTypeEnum.Team,
});

// 添加左侧选择器的 ref
const viewTeamSelectorRef = ref<InstanceType<typeof FlowflexUserSelector> | null>(null);
const viewUserSelectorRef = ref<InstanceType<typeof FlowflexUserSelector> | null>(null);

// 获取 menuStore 实例
const menuStore = menuRoles();

// 右侧可选的树形数据
const operateChoosableTreeData = ref<FlowflexUser[] | undefined>(undefined);

// 使用一个 ref 来跟踪是否正在处理内部更新
const isProcessingInternalUpdate = ref(false);

// 处理左侧选择变化的核心过滤逻辑
const handleLeftChange = async () => {
	const mode = localPermissions.viewPermissionMode;
	const leftSubjectType = localPermissions.viewPermissionSubjectType;

	// 获取左侧选中的 ID
	let selectedIds: string[] = [];

	if (leftSubjectType === PermissionSubjectTypeEnum.Team) {
		selectedIds = localPermissions.viewTeams;
	} else {
		localPermissions.operatePermissionSubjectType = PermissionSubjectTypeEnum.User;
		selectedIds = localPermissions.viewUsers;
	}

	// Public/Private 模式：不过滤
	if (mode === CasePermissionModeEnum.Public || mode === CasePermissionModeEnum.Private) {
		operateChoosableTreeData.value = undefined;
		return;
	}

	// 如果没有选中数据，直接返回
	if (selectedIds.length === 0) {
		operateChoosableTreeData.value = undefined;
		return;
	}

	// 获取完整树数据（不依赖 ref）
	const fullTreeData = await menuStore.getFlowflexUserDataWithCache('');

	// 构建节点映射和父子关系（使用完整树）
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

	buildMaps(fullTreeData);
	const selectedIdSet = new Set<string>(selectedIds);

	// Visible 模式：白名单（右侧只能从左侧选中的子集中选择）
	if (mode === CasePermissionModeEnum.VisibleToTeams) {
		const resultIds = new Set<string>();

		selectedIdSet.forEach((nodeId: string) => {
			const node = nodeMap.get(nodeId);
			if (!node) return;

			// 检查是否有已选中的父节点
			let currentId: string = nodeId;
			let hasSelectedParent = false;

			while (childToParentMap.has(currentId)) {
				const parentId: string = childToParentMap.get(currentId)!;
				if (selectedIdSet.has(parentId)) {
					hasSelectedParent = true;
					break;
				}
				currentId = parentId;
			}

			if (!hasSelectedParent) {
				resultIds.add(nodeId);
			}
		});

		const newTreeData = Array.from(resultIds)
			.map((id: string) => nodeMap.get(id)!)
			.filter(Boolean);
		operateChoosableTreeData.value = newTreeData.length > 0 ? newTreeData : [];
		return;
	}

	// Invisible 模式：黑名单（右侧排除左侧选中的）
	if (mode === CasePermissionModeEnum.InvisibleToTeams) {
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

		const filterTree = (nodes: FlowflexUser[]): FlowflexUser[] => {
			const result: FlowflexUser[] = [];
			for (const node of nodes) {
				if (excludeIds.has(node.id)) continue;

				const newNode: FlowflexUser = { ...node };
				if (newNode.children && newNode.children.length > 0) {
					newNode.children = filterTree(newNode.children);

					// 如果是 Team 节点且所有 children 都被过滤掉了，不保留这个空 Team
					if (newNode.type === 'team' && newNode.children.length === 0) {
						continue;
					}
				}
				result.push(newNode);
			}
			return result;
		};

		const filteredTreeData = filterTree(fullTreeData);
		operateChoosableTreeData.value = filteredTreeData.length > 0 ? filteredTreeData : [];
		return;
	}
};

onMounted(() => {
	nextTick(() => {
		if (shouldShowSelector.value && localPermissions.viewTeams.length > 0) {
			handleLeftChange();
		}
	});
});

// 统一的数据处理函数
const processPermissionChanges = () => {
	if (isProcessingInternalUpdate.value) return;

	// 先设置标志位，防止内部修改触发 watch
	isProcessingInternalUpdate.value = true;

	// 使用 nextTick 确保在下一个事件循环中处理
	nextTick(() => {
		// 处理 viewPermissionMode 的变化 - 切换模式时保留选择，只更新过滤
		if (!shouldShowSelector.value) {
			// Public/Private 模式清空
			if (localPermissions.viewTeams.length > 0) {
				localPermissions.viewTeams = [];
			}
			if (localPermissions.viewUsers.length > 0) {
				localPermissions.viewUsers = [];
			}
			if (localPermissions.operateTeams.length > 0) {
				localPermissions.operateTeams = [];
			}
			if (localPermissions.operateUsers.length > 0) {
				localPermissions.operateUsers = [];
			}
			operateChoosableTreeData.value = undefined;
		} else {
			// VisibleToTeams/InvisibleToTeams 模式：更新过滤逻辑
			handleLeftChange();
		}

		// 处理 operateTeams/operateUsers 的同步
		if (localPermissions.useSameGroups) {
			// 勾选"使用相同"时，同步 view 的选择到 operate
			const newOperateTeams = shouldShowSelector.value ? [...localPermissions.viewTeams] : [];
			const newOperateUsers = shouldShowSelector.value ? [...localPermissions.viewUsers] : [];

			if (JSON.stringify(newOperateTeams) !== JSON.stringify(localPermissions.operateTeams)) {
				localPermissions.operateTeams = newOperateTeams;
			}
			if (JSON.stringify(newOperateUsers) !== JSON.stringify(localPermissions.operateUsers)) {
				localPermissions.operateUsers = newOperateUsers;
			}

			// 同步 SubjectType 枚举值
			localPermissions.operatePermissionSubjectType =
				localPermissions.viewPermissionSubjectType;
		}

		// emit 更新到父组件
		emit('update:modelValue', {
			viewPermissionMode: localPermissions.viewPermissionMode,
			viewTeams: [...localPermissions.viewTeams],
			viewUsers: [...localPermissions.viewUsers],
			viewPermissionSubjectType: localPermissions.viewPermissionSubjectType,
			useSameGroups: localPermissions.useSameGroups,
			operateTeams: [...localPermissions.operateTeams],
			operateUsers: [...localPermissions.operateUsers],
			operatePermissionSubjectType: localPermissions.operatePermissionSubjectType,
		});

		// 重置标志位
		nextTick(() => {
			isProcessingInternalUpdate.value = false;
		});
	});
};

// 监听左侧 SubjectType 变化
watch(
	() => localPermissions.viewPermissionSubjectType,
	() => {
		handleLeftChange();
	}
);

// 监听 viewPermissionMode 变化
watch(
	() => localPermissions.viewPermissionMode,
	() => {
		handleLeftChange();
	}
);

// 只监听 localPermissions 的变化，统一处理
watch(
	localPermissions,
	() => {
		processPermissionChanges();
	},
	{ deep: true }
);

// 监听外部数据变化（从父组件接收更新）
watch(
	() => props.modelValue,
	(newVal) => {
		if (newVal && !isProcessingInternalUpdate.value) {
			// 检查是否真的有变化，避免不必要的更新
			const hasChanges =
				localPermissions.viewPermissionMode !== newVal.viewPermissionMode ||
				JSON.stringify(localPermissions.viewTeams) !== JSON.stringify(newVal.viewTeams) ||
				JSON.stringify(localPermissions.viewUsers) !== JSON.stringify(newVal.viewUsers) ||
				localPermissions.viewPermissionSubjectType !== newVal.viewPermissionSubjectType ||
				localPermissions.useSameGroups !== newVal.useSameGroups ||
				JSON.stringify(localPermissions.operateTeams) !==
					JSON.stringify(newVal.operateTeams) ||
				JSON.stringify(localPermissions.operateUsers) !==
					JSON.stringify(newVal.operateUsers) ||
				localPermissions.operatePermissionSubjectType !==
					newVal.operatePermissionSubjectType;

			if (hasChanges) {
				isProcessingInternalUpdate.value = true;
				localPermissions.viewPermissionMode =
					newVal.viewPermissionMode ?? CasePermissionModeEnum.Public;
				localPermissions.viewTeams = [...(newVal.viewTeams || [])];
				localPermissions.viewUsers = [...(newVal.viewUsers || [])];
				localPermissions.viewPermissionSubjectType =
					newVal.viewPermissionSubjectType ?? PermissionSubjectTypeEnum.Team;
				localPermissions.useSameGroups = newVal.useSameGroups ?? true;
				localPermissions.operateTeams = [...(newVal.operateTeams || [])];
				localPermissions.operateUsers = [...(newVal.operateUsers || [])];
				localPermissions.operatePermissionSubjectType =
					newVal.operatePermissionSubjectType ?? PermissionSubjectTypeEnum.Team;

				nextTick(() => {
					isProcessingInternalUpdate.value = false;
				});
			}
		}
	},
	{ deep: true }
);
</script>

<style scoped>
/* 确保 checkbox label 换行显示 */
:deep(.el-checkbox__label) {
	white-space: normal;
	line-height: 1.5;
}

/* 确保 radio label 正常显示 */
:deep(.el-radio__label) {
	white-space: normal;
	line-height: 1.5;
}
</style>
