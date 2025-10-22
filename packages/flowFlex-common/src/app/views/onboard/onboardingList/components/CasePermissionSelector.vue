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
				<el-radio-group v-model="viewSelectionType" @change="handleViewSelectionTypeChange">
					<el-radio value="team">User Teams</el-radio>
					<el-radio value="user">Individual Users</el-radio>
				</el-radio-group>

				<!-- 选择器 -->
				<FlowflexUserSelector
					v-model="currentViewSelection"
					:selection-type="viewSelectionType"
					:clearable="true"
				/>
			</div>
		</div>

		<!-- 右侧：Operate Permission -->
		<div class="space-y-4 w-full">
			<div class="space-y-2">
				<label class="text-base font-bold">Operate Permission</label>
				<p class="text-sm">Controls who can operate on this case</p>

				<el-checkbox v-if="shouldShowSelector" v-model="localPermissions.useSameGroups">
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
			<div v-if="!localPermissions.useSameGroups && shouldShowSelector" class="space-y-2">
				<label class="text-base font-bold">Available Teams</label>

				<!-- 单选按钮：User Groups / Individual Users -->
				<el-radio-group
					v-model="operateSelectionType"
					@change="handleOperateSelectionTypeChange"
				>
					<el-radio value="team">User Teams</el-radio>
					<el-radio value="user">Individual Users</el-radio>
				</el-radio-group>

				<!-- 选择器 -->
				<FlowflexUserSelector
					v-model="currentOperateSelection"
					:selection-type="operateSelectionType"
					:placeholder="operateSelectionType === 'team' ? 'Select teams' : 'Select users'"
					:clearable="true"
				/>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { reactive, computed, watch, nextTick, ref } from 'vue';
import { CasePermissionModeEnum, PermissionSubjectTypeEnum } from '@/enums/permissionEnum';
import FlowflexUserSelector from '@/components/form/flowflexUser/index.vue';

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

// 左侧选择类型：team 或 user（根据 props.modelValue 的 SubjectType 初始化）
const viewSelectionType = ref<'team' | 'user'>(
	props.modelValue.viewPermissionSubjectType === PermissionSubjectTypeEnum.User ? 'user' : 'team'
);

// 右侧选择类型：team 或 user（根据 props.modelValue 的 SubjectType 初始化）
const operateSelectionType = ref<'team' | 'user'>(
	props.modelValue.operatePermissionSubjectType === PermissionSubjectTypeEnum.User
		? 'user'
		: 'team'
);

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

// 当前 View 选择的数据（根据 viewSelectionType 动态绑定）
const currentViewSelection = computed({
	get: () => {
		return viewSelectionType.value === 'team'
			? localPermissions.viewTeams
			: localPermissions.viewUsers;
	},
	set: (value) => {
		if (viewSelectionType.value === 'team') {
			localPermissions.viewTeams = value;
		} else {
			localPermissions.viewUsers = value;
		}
	},
});

// 当前 Operate 选择的数据（根据 operateSelectionType 动态绑定）
const currentOperateSelection = computed({
	get: () => {
		return operateSelectionType.value === 'team'
			? localPermissions.operateTeams
			: localPermissions.operateUsers;
	},
	set: (value) => {
		if (operateSelectionType.value === 'team') {
			localPermissions.operateTeams = value;
		} else {
			localPermissions.operateUsers = value;
		}
	},
});

// 处理 View 选择类型切换（切换时清空对应的另一个字段）
const handleViewSelectionTypeChange = () => {
	if (viewSelectionType.value === 'team') {
		localPermissions.viewPermissionSubjectType = PermissionSubjectTypeEnum.Team;
	} else {
		// 切换到 user，保持不变
		localPermissions.viewPermissionSubjectType = PermissionSubjectTypeEnum.User;
	}
};

// 处理 Operate 选择类型切换（切换时清空对应的另一个字段）
const handleOperateSelectionTypeChange = () => {
	if (operateSelectionType.value === 'team') {
		// 切换到 team，保持不变
		localPermissions.operatePermissionSubjectType = PermissionSubjectTypeEnum.Team;
	} else {
		// 切换到 user，保持不变
		localPermissions.operatePermissionSubjectType = PermissionSubjectTypeEnum.User;
	}
};

// 使用一个 ref 来跟踪是否正在处理内部更新
const isProcessingInternalUpdate = ref(false);

// 统一的数据处理函数
const processPermissionChanges = () => {
	if (isProcessingInternalUpdate.value) return;

	// 先设置标志位，防止内部修改触发 watch
	isProcessingInternalUpdate.value = true;

	// 使用 nextTick 确保在下一个事件循环中处理
	nextTick(() => {
		// 处理 viewPermissionMode 的变化 - Public 和 Private 模式下清空所有选择
		if (!shouldShowSelector.value) {
			// 清空 View 相关的选择
			if (localPermissions.viewTeams.length > 0) {
				localPermissions.viewTeams = [];
			}
			if (localPermissions.viewUsers.length > 0) {
				localPermissions.viewUsers = [];
			}
			// 清空 Operate 相关的选择
			if (localPermissions.operateTeams.length > 0) {
				localPermissions.operateTeams = [];
			}
			if (localPermissions.operateUsers.length > 0) {
				localPermissions.operateUsers = [];
			}
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

			// 同步选择类型和 SubjectType 枚举值
			operateSelectionType.value = viewSelectionType.value;
			localPermissions.operatePermissionSubjectType =
				localPermissions.viewPermissionSubjectType;
		}
		// 注意：不勾选时，左右完全独立，无需任何过滤或限制

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

				// 根据 SubjectType 枚举值恢复 radio 选择
				viewSelectionType.value =
					newVal.viewPermissionSubjectType === PermissionSubjectTypeEnum.User
						? 'user'
						: 'team';
				operateSelectionType.value =
					newVal.operatePermissionSubjectType === PermissionSubjectTypeEnum.User
						? 'user'
						: 'team';

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
