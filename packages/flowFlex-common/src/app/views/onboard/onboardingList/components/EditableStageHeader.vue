<template>
	<div class="">
		<div class="case-header rounded-xl p-2.5">
			<!-- 显示状态 -->
			<div v-if="!isEditing">
				<!-- 头部标题和操作按钮 -->
				<div class="flex items-center justify-between">
					<h2 class="font-bold text-xl">{{ displayTitle }}</h2>
					<div class="flex items-center gap-2">
						<template v-if="!disabled">
							<el-button
								type="primary"
								size="small"
								plain
								@click="openReassignDialog"
							>
								Reassign
							</el-button>
							<el-button
								type="primary"
								size="small"
								plain
								:icon="Plus"
								@click="openAddCoassigneeDialog"
							>
								Add Co-assignee
							</el-button>
						</template>
						<el-button
							link
							type="primary"
							@click="handleEdit"
							:icon="Edit"
							:disabled="disabled || !currentStage?.startTime"
						/>
					</div>
				</div>
				<div class="my-2 space-y-2">
					<!-- Assigned to 行 -->
					<div class="assignees-row">
						<div class="flex items-center gap-x-2 flex-shrink-0">
							<Icon icon="lucide-user" class="text-gray-500" />
							<span class="text-gray-600">Assigned to:</span>
						</div>
						<template v-if="displayAssignees.length > 0">
							<div
								ref="assigneesTagsRef"
								class="assignees-tags"
								:class="{ 'assignees-collapsed': !isAssigneesExpanded }"
							>
								<el-tag
									v-for="userId in displayAssignees"
									:key="userId"
									:closable="!disabled"
									size="small"
									type="primary"
									@close="handleRemoveAssignee(userId)"
								>
									{{ getUserDisplayName(userId) }}
								</el-tag>
							</div>
							<el-button
								v-if="showAssigneesExpandButton || isAssigneesExpanded"
								link
								type="primary"
								size="small"
								class="flex-shrink-0"
								@click="toggleAssigneesExpand"
							>
								{{
									isAssigneesExpanded
										? 'Less'
										: `Show all ${displayAssignees.length}`
								}}
							</el-button>
						</template>
						<span v-else class="text-gray-400">--</span>
					</div>
					<!-- Co-assignees 行 -->
					<div class="co-assignees-row">
						<div class="flex items-center gap-x-2 flex-shrink-0">
							<Icon icon="lucide-users" class="text-gray-500" />
							<span class="text-gray-600">Co-assignees:</span>
						</div>
						<template v-if="displayCoAssignees.length > 0">
							<div
								ref="coAssigneesTagsRef"
								class="co-assignees-tags mt-1"
								:class="{ 'co-assignees-collapsed': !isCoAssigneesExpanded }"
							>
								<el-tag
									v-for="userId in displayCoAssignees"
									:key="userId"
									:closable="!disabled"
									size="small"
									@close="handleRemoveCoassignee(userId)"
								>
									{{ getUserDisplayName(userId) }}
								</el-tag>
							</div>
							<el-button
								v-if="showExpandButton || isCoAssigneesExpanded"
								link
								type="primary"
								size="small"
								class="flex-shrink-0"
								@click="toggleCoAssigneesExpand"
							>
								{{
									isCoAssigneesExpanded
										? 'Less'
										: `Show all ${displayCoAssignees.length}`
								}}
							</el-button>
						</template>
						<span v-else class="text-gray-400">--</span>
					</div>
				</div>
				<div
					v-if="currentStage?.stageDescription"
					class="text-sm text-[var(--el-text-color-secondary)]"
				>
					{{ currentStage?.stageDescription }}
				</div>
				<el-divider class="my-4" />
				<!-- 信息网格 -->
				<div class="grid grid-cols-1 md:grid-cols-3 gap-4 text-sm">
					<div>
						<div class="mb-1">Start Date</div>
						<div class="font-medium">{{ displayStartDate }}</div>
					</div>
					<div>
						<div class="mb-1">Est. Duration</div>
						<div class="font-medium">{{ displayEstimatedDuration }}</div>
					</div>
					<div>
						<div class="mb-1">ETA</div>
						<div class="font-medium">{{ displayETA }}</div>
					</div>
				</div>
			</div>

			<!-- 编辑状态 -->
			<div v-else>
				<!-- 编辑头部标题和操作按钮 -->
				<div class="flex items-center justify-between mb-4">
					<h2 class="text-xl font-semibold">Edit Stage Information</h2>
					<div class="flex items-center gap-2">
						<el-button link type="info" @click="handleCancel" :disabled="saving">
							Cancel
						</el-button>
						<el-button link type="primary" @click="handleSave" :loading="saving">
							Save
						</el-button>
					</div>
				</div>

				<!-- 编辑表单网格 -->
				<div class="grid grid-cols-1 md:grid-cols-3 gap-4 text-sm">
					<!-- Start Date - 只读显示 -->
					<div>
						<div class="mb-2">Start Date</div>
						<el-input
							v-model="displayStartDate"
							class="w-full stage-edit-input"
							disabled
						/>
					</div>
					<!-- Est. Duration - 可编辑 -->
					<div>
						<div class="mb-2">Est. Duration (days)</div>
						<InputNumber
							v-model="editForm.customEstimatedDays as number"
							placeholder="Enter days"
							class="w-full stage-edit-input"
							:disabled="saving"
							:isFoloat="false"
							@change="handleEstimatedDaysChange"
						/>
					</div>
					<!-- End Time - 可编辑 -->
					<div>
						<div class="mb-2">End Time</div>
						<el-date-picker
							v-model="editForm.customEndTime as string"
							type="date"
							placeholder="Select end date"
							class="w-full stage-edit-input"
							:disabled="saving"
							:format="projectDate"
							:value-format="projectDate"
							:disabledDate="disabledEndDate"
							@change="handleEndTimeChange"
						/>
					</div>
				</div>
			</div>
		</div>

		<!-- Reassign 弹窗 -->
		<el-dialog
			v-model="reassignDialogVisible"
			title="Reassign Stage"
			:width="dialogWidth"
			:close-on-click-modal="false"
			append-to-body
		>
			<p class="text-gray-600 mb-4">
				Select assignees for the "{{ currentStage?.stageName }}" stage.
			</p>
			<!-- 负责人选择 -->
			<div>
				<div class="text-sm text-gray-500 mb-1">Select Assignees</div>
				<el-select
					v-model="reassignForm.selectedAssignees"
					placeholder="Select users..."
					class="w-full"
					multiple
					filterable
					:loading="optionsLoading"
					tag-type="primary"
					collapse-tags
					collapse-tags-tooltip
					:max-collapse-tags="3"
					clearable
				>
					<el-option
						v-for="user in canSelectAssignOptions"
						:key="String(user.key)"
						:label="user.value"
						:value="String(user.key)"
					>
						<span>{{ user.value }}</span>
						<span v-if="user.email" class="text-gray-400 text-xs ml-2">
							{{ user.email }}
						</span>
					</el-option>
				</el-select>
			</div>
			<template #footer>
				<el-button @click="handleReassignCancel">Cancel</el-button>
				<el-button
					type="primary"
					:disabled="reassignForm.selectedAssignees.length === 0"
					@click="handleReassignConfirm"
				>
					Confirm
				</el-button>
			</template>
		</el-dialog>

		<!-- Add Co-assignee 弹窗 -->
		<el-dialog
			v-model="addCoassigneeDialogVisible"
			title="Add Co-assignee"
			:width="dialogWidth"
			:close-on-click-modal="false"
			append-to-body
		>
			<p class="text-gray-600 mb-4">
				Select one or more users to add as co-assignees to this stage.
			</p>
			<!-- 多选用户列表 -->
			<div>
				<div class="text-sm text-gray-500 mb-1">Select Co-assignees</div>
				<el-select
					v-model="addCoassigneeForm.selectedUsers"
					placeholder="Select users..."
					class="w-full"
					multiple
					filterable
					:loading="optionsLoading"
					tag-type="primary"
					collapse-tags
					collapse-tags-tooltip
					:max-collapse-tags="3"
					clearable
				>
					<el-option
						v-for="user in availableCoAssignees"
						:key="String(user.key)"
						:label="user.value"
						:value="String(user.key)"
					>
						<span>{{ user.value }}</span>
						<span v-if="user.email" class="text-gray-400 text-xs ml-2">
							{{ user.email }}
						</span>
					</el-option>
				</el-select>
			</div>
			<template #footer>
				<el-button @click="handleAddCoassigneeCancel">Cancel</el-button>
				<el-button type="primary" @click="handleAddCoassigneeConfirm">
					Add Co-assignee
				</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, watch, nextTick, onMounted, toRaw } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Edit, Plus } from '@element-plus/icons-vue';
import { Icon } from '@iconify/vue';
import { timeZoneConvert } from '@/hooks/time';
import {
	defaultStr,
	dialogWidth,
	projectTenMinutesSsecondsDate,
	projectDate,
} from '@/settings/projectSetting';
import InputNumber from '@/components/form/InputNumber/index.vue';
import { getAllUser } from '@/apis/global';
import type { Stage } from '#/onboard';
import { useInternalNoteUsers } from '@/hooks/useInternalNoteUsers';

// Props 定义
interface Props {
	currentStage?: Stage | null;
	disabled?: boolean;
	onboardingId?: string;
}

const props = withDefaults(defineProps<Props>(), {
	currentStage: null,
	disabled: false,
	onboardingId: '',
});

// 用户列表数据
interface UserOption {
	key: string;
	value: string;
	email?: string;
}

const { allAssignOptions: canSelectAllAssignOption } = useInternalNoteUsers(props.onboardingId);
const allAssignOptions = ref<UserOption[]>([]);
const optionsLoading = ref(false);

// 获取所有用户列表
const fetchAllUsers = async () => {
	optionsLoading.value = true;
	try {
		const res = await getAllUser();
		if (res?.data && Array.isArray(res.data)) {
			allAssignOptions.value = res.data
				.filter((item) => item?.userType == 3)
				.map((user: any) => ({
					key: String(user?.id),
					value: user?.name,
					email: user?.email,
				}));
		}
	} catch (error) {
		console.error('Failed to fetch users:', error);
		allAssignOptions.value = [];
	} finally {
		optionsLoading.value = false;
	}
};

// 组件挂载后检测溢出
onMounted(() => {
	fetchAllUsers();
	nextTick(() => {
		checkOverflow();
	});
});

// Emits 定义
const emit = defineEmits(['update:stage-data']);

// 响应式数据
const isEditing = ref(false);
const saving = ref(false);

// Co-assignees 展开/收起控制
const isCoAssigneesExpanded = ref(false);
const coAssigneesTagsRef = ref<HTMLElement | null>(null);
const showExpandButton = ref(false);

// Assignees 展开/收起控制
const isAssigneesExpanded = ref(false);
const assigneesTagsRef = ref<HTMLElement | null>(null);
const showAssigneesExpandButton = ref(false);

// 检测是否需要显示展开按钮（内容是否溢出）
const checkOverflow = () => {
	if (coAssigneesTagsRef.value) {
		const el = coAssigneesTagsRef.value;
		showExpandButton.value = el.scrollHeight > el.clientHeight;
	}
	if (assigneesTagsRef.value) {
		const el = assigneesTagsRef.value;
		showAssigneesExpandButton.value = el.scrollHeight > el.clientHeight;
	}
};

// 弹窗控制
const reassignDialogVisible = ref(false);
const addCoassigneeDialogVisible = ref(false);

// 弹窗表单数据
const reassignForm = ref({
	selectedAssignees: [] as string[],
});

const addCoassigneeForm = ref({
	selectedUsers: [] as string[],
});

// 编辑表单数据
const editForm = ref({
	customEstimatedDays: null as number | null,
	customEndTime: null as string | null,
	assignee: [] as string[],
	coAssignees: [] as string[],
});

// 计算属性 - 显示标题
const displayTitle = computed(() => {
	return props.currentStage?.stageName || defaultStr;
});

// 计算属性 - 显示负责人列表
const displayAssignees = computed(() => {
	return props.currentStage?.assignee || [];
});

// 计算属性 - 显示协作负责人列表
const displayCoAssignees = computed(() => {
	return props.currentStage?.coAssignees || [];
});

// 根据 key 去重的工具函数
const deduplicateByKey = (
	items: Array<{ key: string | number | boolean; value: string; email?: string }>
): UserOption[] => {
	const uniqueMap = new Map<string, UserOption>();
	items.forEach((item) => {
		const rawItem = toRaw(item);
		const key = String(rawItem.key);
		if (!uniqueMap.has(key)) {
			uniqueMap.set(key, {
				key,
				value: rawItem.value,
				email: rawItem.email,
			});
		}
	});
	return Array.from(uniqueMap.values());
};

// 计算属性 - 可选择的协作负责人列表 (排除当前负责人，但保留已选的协作负责人用于回显)
const availableCoAssignees = computed(() => {
	const currentAssigneeOptions =
		addCoassigneeForm.value.selectedUsers.length > 0
			? allAssignOptions.value.filter((item) =>
					addCoassigneeForm.value.selectedUsers.includes(item.key)
			  )
			: [];
	const currentAssignee = props.currentStage?.assignee || [];
	const canselectAssigneeOption = canSelectAllAssignOption.value.filter(
		(user) => !currentAssignee.includes(String(user.key))
	);
	const combined = [...currentAssigneeOptions, ...canselectAssigneeOption];
	return deduplicateByKey(combined);
});

const canSelectAssignOptions = computed(() => {
	const currentAssigneeOptions =
		reassignForm.value.selectedAssignees.length > 0
			? allAssignOptions.value.filter((item) =>
					reassignForm.value.selectedAssignees.includes(item.key)
			  )
			: [];
	const combined = [...currentAssigneeOptions, ...canSelectAllAssignOption.value];
	return deduplicateByKey(combined);
});

// 获取用户显示名称
const getUserDisplayName = (userId: string): string => {
	const user = allAssignOptions.value.find((u) => String(u.key) === userId);
	return user?.value || userId;
};

// 计算属性 - 显示开始日期
const displayStartDate = computed(() => {
	if (!props.currentStage?.startTime) return defaultStr;
	return timeZoneConvert(props.currentStage.startTime, false, projectDate);
});

// 计算属性 - 显示预估时长
const displayEstimatedDuration = computed(() => {
	if (!props.currentStage?.estimatedDays) return defaultStr;
	const days = props.currentStage.estimatedDays;
	if (days === 1) return '1 day';
	if (days < 30) return `${days} days`;
	if (days < 365) {
		const months = Math.round(days / 30);
		return months === 1 ? '1 month' : `${months} months`;
	}
	const years = Math.round(days / 365);
	return years === 1 ? '1 year' : `${years} years`;
});

// 计算属性 - 显示ETA
const displayETA = computed(() => {
	if (!props.currentStage?.startTime || !props.currentStage?.estimatedDays) {
		return defaultStr;
	}

	try {
		return (
			timeZoneConvert(
				props.currentStage?.customEndTime || props.currentStage?.endTime || '',
				false,
				projectDate
			) || defaultStr
		);
	} catch (error) {
		console.error('Error calculating ETA:', error);
		return defaultStr;
	}
});

// 初始化编辑表单
const initEditForm = () => {
	if (!props.currentStage) return;
	editForm.value = {
		assignee: props.currentStage.assignee || [],
		coAssignees: props.currentStage.coAssignees || [],
		customEstimatedDays: props.currentStage?.estimatedDays || null,
		customEndTime: timeZoneConvert(props.currentStage?.endTime || '') || null, // 可以直接编辑结束时间
	};
};

// 监听当前阶段变化，更新编辑表单
watch(
	() => props.currentStage,
	() => {
		isEditing.value = false;
		isCoAssigneesExpanded.value = false;
		isAssigneesExpanded.value = false;
		showExpandButton.value = false;
		showAssigneesExpandButton.value = false;
		initEditForm();
		// 等待 DOM 更新后检测溢出
		nextTick(() => {
			checkOverflow();
		});
	},
	{ immediate: true }
);

// 监听 assignees 和 co-assignees 变化，重新检测溢出
watch(
	() => [props.currentStage?.assignee, props.currentStage?.coAssignees],
	() => {
		nextTick(() => {
			checkOverflow();
		});
	}
);

// 切换展开/收起状态
const toggleCoAssigneesExpand = () => {
	isCoAssigneesExpanded.value = !isCoAssigneesExpanded.value;
};

// 切换 Assignees 展开/收起状态
const toggleAssigneesExpand = () => {
	isAssigneesExpanded.value = !isAssigneesExpanded.value;
};

// 预估天数变化时，自动计算结束时间
const handleEstimatedDaysChange = (estimatedDays: number | null) => {
	if (props.currentStage?.startTime && estimatedDays && estimatedDays > 0) {
		try {
			// 直接使用原始的ISO时间字符串创建Date对象
			const startDate = new Date(props.currentStage.startTime);

			// 使用毫秒计算支持小数天数，保持原始时分秒
			const millisecondsToAdd = estimatedDays * 24 * 60 * 60 * 1000;
			const endDate = new Date(startDate.getTime() + millisecondsToAdd);

			// 将计算出的结束时间转换为 projectTenMinutesSsecondsDate 格式
			const endTimeFormatted = timeZoneConvert(endDate.toString(), false, projectDate);
			editForm.value.customEndTime = endTimeFormatted;
		} catch (error) {
			console.error('Error calculating end time from estimated days:', error);
			editForm.value.customEndTime = null;
		}
	} else if (!estimatedDays) {
		editForm.value.customEndTime = null;
	}
};

// 禁用结束日期选择器中开始日期之前的日期
const disabledEndDate = (time: Date) => {
	if (!props.currentStage?.startTime) {
		return false;
	}

	try {
		// 将开始时间转换为本地时区的格式化字符串
		const startTimeFormatted = timeZoneConvert(
			props.currentStage.startTime,
			false,
			projectTenMinutesSsecondsDate
		);

		const startDate = new Date(startTimeFormatted);
		const startDateOnly = new Date(
			startDate.getFullYear(),
			startDate.getMonth(),
			startDate.getDate()
		);
		const timeOnly = new Date(time.getFullYear(), time.getMonth(), time.getDate());

		// 禁用早于开始日期的所有日期
		return timeOnly < startDateOnly;
	} catch (error) {
		console.error('Error in disabledEndDate:', error);
		return false;
	}
};

// 结束时间变化时，自动计算预估天数
const handleEndTimeChange = (endTime: string | Date | null) => {
	if (props.currentStage?.startTime && endTime) {
		try {
			// 直接使用原始的ISO时间字符串创建Date对象
			const startDate = new Date(timeZoneConvert(displayStartDate.value, true));
			const endDate = new Date(timeZoneConvert(endTime as string, true));
			// 验证结束时间不能小于开始时间
			if (endDate < startDate) {
				ElMessage.error('End time cannot be earlier than start time');
				// 重置为之前的有效值或null
				editForm.value.customEndTime = null;
				editForm.value.customEstimatedDays = null;
				return;
			}

			// 计算天数差，支持小数
			const timeDiff = endDate.getTime() - startDate.getTime();
			const daysDiff = timeDiff / (1000 * 60 * 60 * 24);

			// 更新预估天数，保留两位小数
			editForm.value.customEstimatedDays =
				daysDiff > 0 ? Math.round(daysDiff * 100) / 100 : 0.01;
		} catch (error) {
			console.error('Error calculating estimated days from end time:', error);
			editForm.value.customEstimatedDays = null;
		}
	} else if (!endTime) {
		editForm.value.customEstimatedDays = null;
	}
};

// 事件处理函数
const handleEdit = () => {
	if (props.disabled) return;
	initEditForm();
	isEditing.value = true;
};

const handleCancel = () => {
	isEditing.value = false;
	initEditForm(); // 重置表单数据
};

const handleSave = async () => {
	if (!props.currentStage?.stageId) {
		ElMessage.error('Invalid stage information');
		return;
	}

	// 表单验证
	if (!editForm.value.customEstimatedDays || editForm.value.customEstimatedDays < 0.01) {
		ElMessage.error('Estimated duration must be at least 0.01 day');
		return;
	}

	if (!editForm.value.customEndTime) {
		ElMessage.error('End time is required');
		return;
	}

	// 验证结束时间不能小于开始时间
	if (props.currentStage.startTime) {
		try {
			// 直接使用原始的ISO时间字符串创建Date对象
			const startDate = new Date(props.currentStage.startTime);
			const endDate = new Date(editForm.value.customEndTime);

			if (endDate < startDate) {
				ElMessage.error('End time cannot be earlier than start time');
				return;
			}
		} catch (error) {
			console.error('Error validating dates:', error);
			ElMessage.error('Invalid date format');
			return;
		}
	}

	saving.value = true;

	try {
		// 准备更新数据，使用 timeZoneConvert 处理时间格式
		// 将格式化的时间字符串转换为Date对象，再转为ISO字符串，最后转换为UTC格式
		const customEndTimeStr = timeZoneConvert(editForm.value.customEndTime, true);
		const updateData = {
			stageId: props.currentStage.stageId,
			customEstimatedDays: editForm.value.customEstimatedDays,
			customEndTime: customEndTimeStr,
			assignee: editForm.value.assignee,
			coAssignees: editForm.value.coAssignees,
		};

		// 发送更新事件给父组件
		emit('update:stage-data', updateData);

		// 退出编辑模式
		isEditing.value = false;
	} finally {
		saving.value = false;
	}
};

// ========== Reassign 弹窗相关方法 ==========
const openReassignDialog = () => {
	// 回显已添加的负责人
	reassignForm.value.selectedAssignees = [...(props.currentStage?.assignee || [])];
	reassignDialogVisible.value = true;
};

const handleReassignConfirm = () => {
	if (!props.currentStage?.stageId) return;

	const updateData = {
		stageId: props.currentStage.stageId,
		assignee: reassignForm.value.selectedAssignees,
	};

	emit('update:stage-data', updateData);
	reassignDialogVisible.value = false;
};

const handleReassignCancel = () => {
	reassignDialogVisible.value = false;
	reassignForm.value.selectedAssignees = [];
};

// ========== 删除负责人 ==========
const handleRemoveAssignee = async (userId: string) => {
	if (!props.currentStage?.stageId) return;

	const userName = getUserDisplayName(userId);

	ElMessageBox.confirm(
		`Are you sure you want to remove "${userName}" from assignees?`,
		'⚠️ Confirm Remove Assignee',
		{
			confirmButtonText: 'Remove',
			cancelButtonText: 'Cancel',
			confirmButtonClass: 'warning-confirm-btn',
			cancelButtonClass: 'cancel-confirm-btn',
			distinguishCancelAndClose: true,
			customClass: 'remove-confirmation-dialog',
			showCancelButton: true,
			showConfirmButton: true,
			beforeClose: async (action, instance, done) => {
				if (action === 'confirm') {
					instance.confirmButtonLoading = true;
					instance.confirmButtonText = 'Removing...';

					const currentAssignees = props.currentStage?.assignee || [];
					const newAssignees = currentAssignees.filter((id) => id !== userId);

					const updateData = {
						stageId: props.currentStage?.stageId,
						assignee: newAssignees,
					};

					emit('update:stage-data', updateData);
					done();
				} else {
					done();
				}
			},
		}
	);
};

// ========== Add Co-assignee 弹窗相关方法 ==========
const openAddCoassigneeDialog = () => {
	// 回显已添加的协作负责人
	addCoassigneeForm.value.selectedUsers = [...(props.currentStage?.coAssignees || [])];
	addCoassigneeDialogVisible.value = true;
};

const handleAddCoassigneeConfirm = () => {
	if (!props.currentStage?.stageId) return;

	const updateData = {
		stageId: props.currentStage.stageId,
		coAssignees: addCoassigneeForm.value.selectedUsers,
	};

	emit('update:stage-data', updateData);
	addCoassigneeDialogVisible.value = false;
};

const handleAddCoassigneeCancel = () => {
	addCoassigneeDialogVisible.value = false;
	addCoassigneeForm.value.selectedUsers = [];
};

// ========== 删除协作负责人 ==========
const handleRemoveCoassignee = async (userId: string) => {
	if (!props.currentStage?.stageId) return;

	const userName = getUserDisplayName(userId);

	ElMessageBox.confirm(
		`Are you sure you want to remove "${userName}" from co-assignees?`,
		'⚠️ Confirm Remove Co-assignee',
		{
			confirmButtonText: 'Remove',
			cancelButtonText: 'Cancel',
			confirmButtonClass: 'warning-confirm-btn',
			cancelButtonClass: 'cancel-confirm-btn',
			distinguishCancelAndClose: true,
			customClass: 'remove-confirmation-dialog',
			showCancelButton: true,
			showConfirmButton: true,
			beforeClose: async (action, instance, done) => {
				if (action === 'confirm') {
					instance.confirmButtonLoading = true;
					instance.confirmButtonText = 'Removing...';

					const currentCoAssignees = props.currentStage?.coAssignees || [];
					const newCoAssignees = currentCoAssignees.filter((id) => id !== userId);

					const updateData = {
						stageId: props.currentStage?.stageId,
						coAssignees: newCoAssignees,
					};

					emit('update:stage-data', updateData);
					done();
				} else {
					done();
				}
			},
		}
	);
};
</script>

<style scoped lang="scss">
/* Assignees 行样式 */
.assignees-row {
	display: flex;
	align-items: flex-start;
	gap: 0.5rem;
	flex-wrap: wrap;
}

.assignees-tags {
	display: flex;
	flex-wrap: wrap;
	gap: 0.25rem;
	flex: 1;
	min-width: 0;
	max-height: 500px;
	overflow: hidden;
	transition: max-height 0.3s ease-in-out;
}

.assignees-collapsed {
	max-height: 24px;
}

/* Co-assignees 行样式 */
.co-assignees-row {
	display: flex;
	align-items: flex-start;
	gap: 0.5rem;
	flex-wrap: wrap;
}

.co-assignees-tags {
	display: flex;
	flex-wrap: wrap;
	gap: 0.25rem;
	flex: 1;
	min-width: 0;
	max-height: 500px;
	overflow: hidden;
	transition: max-height 0.3s ease-in-out;
}

.co-assignees-collapsed {
	max-height: 24px;
}

/* 响应式设计 */
@media (max-width: 768px) {
	:deep(.grid) {
		grid-template-columns: 1fr;
		gap: 1rem;
	}
}
</style>
