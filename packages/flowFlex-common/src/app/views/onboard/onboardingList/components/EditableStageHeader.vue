<template>
	<div class="stage-header">
		<!-- ===== 只读展示状态 ===== -->
		<div v-show="!isEditing">
			<!-- 标题行 -->
			<div
				class="flex items-center justify-between cursor-pointer select-none"
				@click="toggleExpanded"
			>
				<h2 class="text-base font-semibold text-gray-800 truncate mr-2">
					{{ displayTitle }}
				</h2>
				<div class="flex items-center gap-1 flex-shrink-0">
					<el-tooltip v-if="!disabled" content="Edit stage info" placement="top">
						<button
							class="icon-btn"
							:class="{ 'opacity-30 cursor-not-allowed': !currentStage?.startTime }"
							:disabled="!currentStage?.startTime"
							@click.stop="handleEdit"
						>
							<Icon icon="lucide:pencil" class="w-3.5 h-3.5" />
						</button>
					</el-tooltip>
					<button class="icon-btn" @click.stop="toggleExpanded">
						<Icon
							icon="lucide:chevron-right"
							class="w-4 h-4 transition-transform duration-200"
							:class="{ 'rotate-90': isExpanded }"
						/>
					</button>
				</div>
			</div>

			<el-collapse-transition>
				<div v-show="isExpanded" class="mt-2">
					<!-- stage description -->
					<p
						v-if="currentStage?.stageDescription"
						class="text-xs text-gray-500 mb-2 leading-relaxed"
					>
						{{ currentStage.stageDescription }}
					</p>

					<!-- 信息卡片网格 -->
					<div class="meta-grid">
						<!-- Assigned to（合并展示 assignee + co-assignee） -->
						<div class="meta-item">
							<span class="meta-label">
								<Icon icon="lucide:user" class="w-3 h-3" />
								Assigned to
							</span>
							<div class="meta-value flex items-center gap-1 flex-wrap">
								<template v-if="allDisplayAssignees.length > 0">
									<el-avatar
										v-for="userId in allDisplayAssignees.slice(0, 1)"
										:key="userId"
										:size="20"
										class="avatar-primary shrink-0 text-xs"
										:title="getUserDisplayName(userId)"
									>
										{{ getInitials(userId) }}
									</el-avatar>
									<span class="text-xs font-medium text-gray-700 truncate">
										{{ getUserDisplayName(allDisplayAssignees[0]) || '—' }}
									</span>
									<el-popover
										v-if="allDisplayAssignees.length > 1"
										placement="bottom-start"
										:width="180"
										trigger="click"
										popper-class="assignees-popover"
									>
										<template #reference>
											<span class="more-badge">
												+{{ allDisplayAssignees.length - 1 }}
											</span>
										</template>
										<div class="py-1">
											<div
												v-for="userId in allDisplayAssignees.slice(1)"
												:key="userId"
												class="flex items-center gap-2 px-2 py-1.5 hover:bg-gray-50 rounded"
											>
												<el-avatar
													:size="20"
													class="avatar-primary shrink-0 text-xs"
												>
													{{ getInitials(userId) }}
												</el-avatar>
												<span class="text-xs text-gray-700">
													{{ getUserDisplayName(userId) || userId }}
												</span>
											</div>
										</div>
									</el-popover>
								</template>
								<span v-else class="text-xs text-gray-400">—</span>
							</div>
						</div>

						<!-- Start Date -->
						<div class="meta-item">
							<span class="meta-label">
								<Icon icon="lucide:calendar" class="w-3 h-3" />
								Start Date
							</span>
							<span class="meta-value">{{ displayStartDate }}</span>
						</div>

						<!-- Est. Duration -->
						<div class="meta-item">
							<span class="meta-label">
								<Icon icon="lucide:clock" class="w-3 h-3" />
								Est. Duration
							</span>
							<span class="meta-value">{{ displayEstimatedDuration }}</span>
						</div>

						<!-- ETA -->
						<div class="meta-item">
							<span class="meta-label">
								<Icon icon="lucide:flag" class="w-3 h-3" />
								ETA
							</span>
							<span class="meta-value">{{ displayETA }}</span>
						</div>
					</div>
				</div>
			</el-collapse-transition>
		</div>

		<!-- ===== 编辑状态 ===== -->
		<div v-show="isEditing">
			<div class="flex items-center justify-between mb-3">
				<span class="text-sm font-semibold text-gray-700">Edit Stage</span>
				<div class="flex items-center gap-2">
					<el-button size="small" @click="handleCancel" :disabled="props.saving">
						Cancel
					</el-button>
					<el-button
						size="small"
						type="primary"
						@click="handleSave"
						:loading="props.saving"
					>
						Save
					</el-button>
				</div>
			</div>

			<!-- 编辑表单：两行布局 -->
			<!-- 第一行：Assignee + Co-assignees -->
			<div class="grid grid-cols-2 gap-3 mb-3">
				<div class="form-field">
					<label class="form-label">Assignee</label>
					<el-select
						v-model="editForm.assignee"
						placeholder="Select assignees"
						class="w-full"
						multiple
						filterable
						tag-type="primary"
						:loading="optionsLoading"
						collapse-tags
						collapse-tags-tooltip
						:max-collapse-tags="2"
						:disabled="props.saving"
					>
						<el-option
							v-for="user in assigneeOptions"
							:key="user.key"
							:label="user.value"
							:value="user.key"
						>
							<div class="flex items-center justify-between w-full">
								<span class="text-sm">{{ user.value }}</span>
								<span v-if="user.email" class="text-gray-400 text-xs ml-3">
									{{ user.email }}
								</span>
							</div>
						</el-option>
					</el-select>
				</div>

				<div class="form-field">
					<label class="form-label">Co-assignees</label>
					<el-select
						v-model="editForm.coAssignees"
						placeholder="Select co-assignees"
						class="w-full"
						multiple
						filterable
						tag-type="primary"
						:loading="optionsLoading"
						collapse-tags
						collapse-tags-tooltip
						:max-collapse-tags="2"
						:disabled="props.saving"
					>
						<el-option
							v-for="user in coAssigneeOptions"
							:key="user.key"
							:label="user.value"
							:value="user.key"
						>
							<div class="flex items-center justify-between w-full">
								<span class="text-sm">{{ user.value }}</span>
								<span v-if="user.email" class="text-gray-400 text-xs ml-3">
									{{ user.email }}
								</span>
							</div>
						</el-option>
					</el-select>
				</div>
			</div>

			<!-- 第二行：Start Date（只读）+ Est. Duration + End Time -->
			<div class="grid grid-cols-3 gap-3">
				<div class="form-field">
					<label class="form-label">Start Date</label>
					<el-input :model-value="displayStartDate" class="w-full" disabled />
				</div>

				<div class="form-field">
					<label class="form-label">Est. Duration (days)</label>
					<InputNumber
						v-model="editForm.customEstimatedDays as number"
						placeholder="e.g. 3"
						class="w-full"
						:disabled="props.saving"
						:isFoloat="false"
						@change="handleEstimatedDaysChange"
					/>
				</div>

				<div class="form-field">
					<label class="form-label">End Time</label>
					<el-date-picker
						v-model="editForm.customEndTime as string"
						type="date"
						placeholder="Select end date"
						class="w-full"
						:disabled="props.saving"
						:format="projectDate"
						:value-format="projectDate"
						:disabledDate="disabledEndDate"
						@change="handleEndTimeChange"
					/>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, toRaw } from 'vue';
import { ElMessage } from 'element-plus';
import { Icon } from '@iconify/vue';
import { timeZoneConvert } from '@/hooks/time';
import { defaultStr, projectTenMinutesSsecondsDate, projectDate } from '@/settings/projectSetting';
import InputNumber from '@/components/form/InputNumber/index.vue';
import { getAllUser } from '@/apis/global';
import type { Stage } from '#/onboard';
import { UserType } from '@/enums/permissionEnum';

// ===== Props =====
interface Props {
	currentStage?: Stage | null;
	disabled?: boolean;
	onboardingId?: string;
	saving?: boolean;
}
const props = withDefaults(defineProps<Props>(), {
	currentStage: null,
	disabled: false,
	onboardingId: '',
	saving: false,
});

// ===== Emits =====
const emit = defineEmits(['update:stage-data', 'save:done']);

// ===== 用户列表 =====
interface UserOption {
	key: string;
	value: string;
	email?: string;
}

const allAssignOptions = ref<UserOption[]>([]);
const optionsLoading = ref(false);
let usersFetched = false;

const fetchAllUsers = async () => {
	// 已经请求过就不重复请求
	if (usersFetched || optionsLoading.value) return;
	optionsLoading.value = true;
	try {
		const res = await getAllUser();
		if (res?.data && Array.isArray(res.data)) {
			allAssignOptions.value = res.data
				.filter((item: any) => item?.userType != UserType.SystemAdmin)
				.map((user: any) => ({
					key: String(user?.id),
					value: user?.name ?? '',
					email: user?.email ?? '',
				}));
			usersFetched = true;
		}
	} catch {
		allAssignOptions.value = [];
	} finally {
		optionsLoading.value = false;
	}
};

const deduplicateByKey = (items: UserOption[]): UserOption[] => {
	const map = new Map<string, UserOption>();
	items.forEach((item) => {
		const raw = toRaw(item);
		if (!map.has(raw.key)) map.set(raw.key, raw);
	});
	return Array.from(map.values());
};

// 编辑时 assignee 可选（排除已选 co-assignees）
const assigneeOptions = computed(() => {
	const excluded = new Set(editForm.value.coAssignees);
	const base = allAssignOptions.value.filter((u) => !excluded.has(u.key));
	const selected = allAssignOptions.value.filter((u) => editForm.value.assignee.includes(u.key));
	return deduplicateByKey([...selected, ...base]);
});

// 编辑时 co-assignee 可选（排除已选 assignees）
const coAssigneeOptions = computed(() => {
	const excluded = new Set(editForm.value.assignee);
	const base = allAssignOptions.value.filter((u) => !excluded.has(u.key));
	const selected = allAssignOptions.value.filter((u) =>
		editForm.value.coAssignees.includes(u.key)
	);
	return deduplicateByKey([...selected, ...base]);
});

const getUserDisplayName = (userId: string) =>
	allAssignOptions.value.find((u) => u.key === userId)?.value ?? '';

const getInitials = (userId: string) => {
	const name = getUserDisplayName(userId);
	if (!name) return '?';
	const parts = name.trim().split(/\s+/);
	return parts.length >= 2
		? (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
		: name.slice(0, 2).toUpperCase();
};

// ===== UI 状态 =====
const isEditing = ref(false);
const isExpanded = ref(true);

const toggleExpanded = () => {
	isExpanded.value = !isExpanded.value;
};

// ===== 编辑表单 =====
const editForm = ref({
	assignee: [] as string[],
	coAssignees: [] as string[],
	customEstimatedDays: null as number | null,
	customEndTime: null as string | null,
});

const initEditForm = () => {
	if (!props.currentStage) return;
	editForm.value = {
		assignee: [...(props.currentStage.assignee ?? [])],
		coAssignees: [...(props.currentStage.coAssignees ?? [])],
		customEstimatedDays: props.currentStage.estimatedDays ?? null,
		customEndTime: timeZoneConvert(props.currentStage.endTime ?? '') || null,
	};
};

// ===== 展示计算属性 =====
const displayTitle = computed(() => props.currentStage?.stageName ?? defaultStr);

// 只读状态：合并 assignee + coAssignees 一行展示
const allDisplayAssignees = computed(() => {
	const assignees = props.currentStage?.assignee ?? [];
	const coAssignees = props.currentStage?.coAssignees ?? [];
	return [...new Set([...assignees, ...coAssignees])];
});

const displayStartDate = computed(() => {
	if (!props.currentStage?.startTime) return defaultStr;
	return timeZoneConvert(props.currentStage.startTime, false, projectDate);
});

const displayEstimatedDuration = computed(() => {
	const days = props.currentStage?.estimatedDays;
	if (!days) return defaultStr;
	if (days === 1) return '1 day';
	if (days < 30) return `${days} days`;
	if (days < 365) {
		const m = Math.round(days / 30);
		return m === 1 ? '1 month' : `${m} months`;
	}
	const y = Math.round(days / 365);
	return y === 1 ? '1 year' : `${y} years`;
});

const displayETA = computed(() => {
	if (!props.currentStage?.startTime || !props.currentStage?.estimatedDays) return defaultStr;
	try {
		return (
			timeZoneConvert(
				props.currentStage.customEndTime || props.currentStage.endTime || '',
				false,
				projectDate
			) || defaultStr
		);
	} catch {
		return defaultStr;
	}
});

// ===== 日期联动 =====
const handleEstimatedDaysChange = (days: number | null) => {
	if (props.currentStage?.startTime && days && days > 0) {
		try {
			const end = new Date(
				new Date(props.currentStage.startTime).getTime() + days * 86400000
			);
			editForm.value.customEndTime = timeZoneConvert(end.toString(), false, projectDate);
		} catch {
			editForm.value.customEndTime = null;
		}
	} else if (!days) {
		editForm.value.customEndTime = null;
	}
};

const disabledEndDate = (time: Date) => {
	if (!props.currentStage?.startTime) return false;
	try {
		const fmt = timeZoneConvert(
			props.currentStage.startTime,
			false,
			projectTenMinutesSsecondsDate
		);
		const s = new Date(fmt);
		const startDay = new Date(s.getFullYear(), s.getMonth(), s.getDate());
		return new Date(time.getFullYear(), time.getMonth(), time.getDate()) < startDay;
	} catch {
		return false;
	}
};

const handleEndTimeChange = (endTime: string | Date | null) => {
	if (props.currentStage?.startTime && endTime) {
		try {
			const startMs = new Date(timeZoneConvert(displayStartDate.value, true)).getTime();
			const endMs = new Date(timeZoneConvert(endTime as string, true)).getTime();
			if (endMs < startMs) {
				ElMessage.error('End time cannot be earlier than start time');
				editForm.value.customEndTime = null;
				editForm.value.customEstimatedDays = null;
				return;
			}
			const diff = (endMs - startMs) / 86400000;
			editForm.value.customEstimatedDays = diff > 0 ? Math.round(diff * 100) / 100 : 0.01;
		} catch {
			editForm.value.customEstimatedDays = null;
		}
	} else if (!endTime) {
		editForm.value.customEstimatedDays = null;
	}
};

// ===== 编辑操作 =====
const handleEdit = () => {
	if (props.disabled || !props.currentStage?.startTime) return;
	initEditForm();
	isEditing.value = true;
};

const handleCancel = () => {
	if (props.saving) return;
	isEditing.value = false;
	initEditForm();
};

const handleSave = async () => {
	if (!props.currentStage?.stageId) {
		ElMessage.error('Invalid stage information');
		return;
	}
	if (!editForm.value.customEstimatedDays || editForm.value.customEstimatedDays < 0.01) {
		ElMessage.error('Estimated duration must be at least 0.01 day');
		return;
	}
	if (!editForm.value.customEndTime) {
		ElMessage.error('End time is required');
		return;
	}
	if (props.currentStage.startTime) {
		const start = new Date(props.currentStage.startTime);
		const end = new Date(editForm.value.customEndTime);
		if (end < start) {
			ElMessage.error('End time cannot be earlier than start time');
			return;
		}
	}

	// emit 给父组件处理，不在这里关闭编辑态
	// 父组件完成后需调用 closeSaving() 或 emit('save:done')
	emit('update:stage-data', {
		stageId: props.currentStage.stageId,
		customEstimatedDays: editForm.value.customEstimatedDays,
		customEndTime: timeZoneConvert(editForm.value.customEndTime, true),
		assignee: editForm.value.assignee,
		coAssignees: editForm.value.coAssignees,
	});
};

// 父组件 API 完成后调用此方法关闭编辑态
const closeSaving = () => {
	isEditing.value = false;
};

defineExpose({ closeSaving });

// ===== 生命周期 =====
onMounted(fetchAllUsers);

watch(
	() => props.currentStage,
	() => {
		isEditing.value = false;
		initEditForm();
	},
	{ immediate: true }
);
</script>

<style scoped lang="scss">
.stage-header {
	padding: 0.625rem 0.75rem;
	background: #fff;
	border-radius: 0.75rem;
}

/* 图标按钮 */
.icon-btn {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 24px;
	height: 24px;
	border-radius: 4px;
	border: none;
	background: transparent;
	color: var(--el-text-color-secondary);
	cursor: pointer;
	transition:
		background 0.15s,
		color 0.15s;

	&:hover:not(:disabled) {
		background: var(--el-fill-color-light);
		color: var(--el-color-primary);
	}
}

/* 只读信息网格：responsive，最多 4 列 */
.meta-grid {
	display: grid;
	grid-template-columns: repeat(4, minmax(0, 1fr));
	gap: 0.5rem 1rem;
	width: 100%;

	@media (max-width: 900px) {
		grid-template-columns: repeat(2, minmax(0, 1fr));
	}
}

.meta-item {
	display: flex;
	flex-direction: column;
	gap: 0.2rem;
	min-width: 0;
}

/* 头像 primary 色 */
.avatar-primary {
	background-color: var(--el-color-primary) !important;
	color: #fff !important;
	font-size: 0.6rem !important;
	font-weight: 600;
}

.meta-label {
	display: inline-flex;
	align-items: center;
	gap: 0.25rem;
	font-size: 0.7rem;
	font-weight: 500;
	color: var(--el-text-color-placeholder);
	white-space: nowrap;
	letter-spacing: 0.02em;
}

.meta-value {
	font-size: 0.8rem;
	font-weight: 500;
	color: var(--el-text-color-primary);
	min-width: 0;
}

/* +n badge */
.more-badge {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	height: 20px;
	padding: 0 6px;
	border-radius: 10px;
	background: var(--el-fill-color);
	color: var(--el-text-color-secondary);
	font-size: 0.7rem;
	font-weight: 500;
	cursor: pointer;
	transition: background 0.15s;
	flex-shrink: 0;

	&:hover {
		background: var(--el-fill-color-dark);
	}
}

/* 编辑表单字段 */
.form-field {
	display: flex;
	flex-direction: column;
	gap: 0.3rem;
}

.form-label {
	font-size: 0.72rem;
	font-weight: 500;
	color: var(--el-text-color-secondary);
	white-space: nowrap;
}

/* date-picker 撑满父容器 */
:deep(.el-date-editor.el-input),
:deep(.el-date-editor.el-input__wrapper) {
	width: 100%;
}

/* popover 内部滚动区域 */
:global(.assignees-popover .el-popover__title) {
	display: none;
}
</style>
