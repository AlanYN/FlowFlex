<template>
	<div class="w-full h-full">
		<draggable
			style="padding-bottom: 50px"
			:list="list"
			chosen-class="chosenClass"
			:item-key="itemKey"
			:component-data="{
				tag: 'ul',
				type: 'transition-group',
				name: !drag ? 'flip-list' : null,
			}"
			:move="checkMove"
			@start="moveStart"
			@end="draggableEnd"
			v-bind="dragOptions"
			@change="handleDrag"
		>
			<template #item="{ element }">
				<div class="deal-card">
					<div class="flex flex-col gap-y-2 items-start">
						<div class="flex justify-between w-full min-h-[32px]">
							<div>
								<el-link class="text-primary-500" @click="changeItem(element)">
									<a :href="`/deals/details?id=${element.id}`">
										{{ element.deal_name }}
									</a>
								</el-link>
							</div>
							<el-checkbox
								v-if="
									element?.deal_name &&
									element?.userPermissions?.currentUserCanEdit
								"
								:model-value="isChecked(element.id)"
								@change="() => toggleCheckbox(element)"
								:class="!isChecked(element.id) ? 'checkbox' : 'block'"
							/>
						</div>
						<div v-if="element?.amount" class="flex items-center gap-x-2">
							<div class="font-bold">Amount:</div>
							<div class="text-[#606266]">
								$ {{ formatToFinancial(element.amount) }}
							</div>
						</div>
						<div
							v-if="isItemTenant && element?.one_time_charge_fee"
							class="flex items-center gap-x-2"
						>
							<div class="font-bold">One-time charge fee:</div>
							<div class="text-[#606266]">
								$ {{ formatToFinancial(element.one_time_charge_fee) }}
							</div>
						</div>
						<div
							v-if="isItemTenant && element?.monthly_charge_fee"
							class="flex items-center gap-x-2"
						>
							<div class="font-bold">Monthly charge fee:</div>
							<div class="text-[#606266]">
								$ {{ formatToFinancial(element.monthly_charge_fee) }}
							</div>
						</div>
					</div>
					<el-divider class="my-4 mx-0 h-[2px]" />
					<!--<el-tag>other Date</el-tag> -->
					<div v-if="props.infoData && props.infoData[element.id]">
						<div v-if="props.infoData[element.id].leads" style="display: flex">
							<template
								v-for="item in props.infoData[element.id].leads"
								:key="item.leadId"
							>
								<a
									:href="`/leads/${
										item.leadsType == 2 ? 'contactDetails' : 'companiesDetails'
									}?id=${item.leadsId}`"
								>
									<img
										v-if="item.headUrl"
										style="width: 20px; border-radius: 10px"
										:src="item.headUrl"
									/>
									<div
										v-else
										style="width: 20px; height: 20px"
										class="w-[60px] h-[60px] font-extrabold bg-primary-200 flex justify-center rounded-full select-none items-center"
									>
										{{
											item?.name?.trimStart()?.substring(0, 1)?.toUpperCase()
										}}
									</div>
								</a>
							</template>
						</div>
						<div style="margin-top: 5px">
							{{
								`${t('sys.app.note')}: ` +
								(props.infoData[element.id].notesUpdateTime
									? getReadableDateDifference(
											props.infoData[element.id].notesUpdateTime
									  )
									: t('sys.deal.noActivityInAMonth'))
							}}
						</div>
						<div style="margin-top: 5px">
							{{
								`${t('sys.app.task')}: ` +
								(props.infoData[element.id].tasksUpdateTime
									? getReadableDateDifference(
											props.infoData[element.id].tasksUpdateTime
									  )
									: t('sys.deal.noActivityInAMonth'))
							}}
						</div>
					</div>
					<div v-else>{{ t('sys.deal.noActivityInAMonth') }}</div>
				</div>
			</template>
		</draggable>
		<div class="total-footer bg-white dark:bg-black-200" style="margin-top: 100px">
			<div>Total: $ {{ calculateTotal(list) }}</div>
			<template v-if="isItemTenant">
				<div>One-time charge fee: $ {{ calculateOneTimeTotal(list) }}</div>
				<div>Monthly charge fee: $ {{ calculateMonthlyTotal(list) }}</div>
			</template>
		</div>
	</div>
</template>

<script lang="ts" setup>
import { ref, reactive, watchEffect, computed } from 'vue';
import { ElMessage } from 'element-plus';
import { debounce } from 'lodash-es';
import draggable from 'vuedraggable';
import { DealGroup } from '#/setting';
import { useI18n } from '@/hooks/useI18n';
import { IBaseDataPermission } from '#/permission';
import { formatToFinancial } from '@/utils';
import { useUserStore } from '@/stores/modules/user';

interface Drag {
	animation: number;
	group: string;
	disabled: boolean;
	ghostClass: string;
}

interface Props {
	list: any[];
	infoData?: DealGroup;
	id: string;
	dragOptions: Drag;
	itemKey?: string; // 使itemKey可配置
	moneryKey?: string;
	dueDateKey?: string;
	noAllowSkipStage?: boolean;
}
const { t } = useI18n();

defineOptions({
	name: 'DraggableCardList',
});

const props = withDefaults(defineProps<Props>(), {
	list: () => [],
	dragOptions: () => ({
		animation: 200,
		group: 'description',
		disabled: false,
		ghostClass: 'ghost',
	}),
	itemKey: 'id', // 默认使用id作为key
	moneryKey: 'amount',
	dueDate: 'dueDate',
	id: '',
});

const drag = ref(false);
const checkedStates = reactive({});

const emit = defineEmits(['handleDrag', 'update:checkedState', 'changeItem']);

interface IEvent {
	draggedRect: {
		bottom: number;
		height: number;
		left: number;
		right: number;
		top: number;
		width: number;
	};
	draggedContext: {
		element: { userPermissions?: IBaseDataPermission['userPermissions']; [key: string]: any };
	};
	relatedContext: {
		element: { userPermissions?: IBaseDataPermission['userPermissions']; [key: string]: any };
	};
	relatedRect: {
		bottom: number;
		height: number;
		left: number;
		right: number;
		top: number;
		width: number;
	};
}

const initialLeft = ref<number | null>(null);

const moveStart = (evt) => {
	drag.value = true;
	initialLeft.value = evt.from.getBoundingClientRect().left; // Correctly record the initial position using the `from` property
};

const debounceNoPermissionMessage = debounce(() => {
	// ElMessage.closeAll();
	ElMessage.warning('You do not have permission to edit this item');
}, 200);
/**
 * check move by userPermissions and determine if dragging forward
 * 根据 userPermissions 检查移动并确定是否向前拖动
 * @param event
 * @returns boolean
 */
const checkMove = (evt: IEvent, ...args): boolean => {
	// console.log('evt:', evt);
	const hasDragPermission =
		evt?.draggedContext?.element?.userPermissions?.currentUserCanEdit === true;
	const isDraggingForward = evt?.relatedRect?.left > evt?.draggedRect?.left;
	if (!hasDragPermission) {
		debounceNoPermissionMessage();
		return false;
	}

	if (isDraggingForward && props.noAllowSkipStage) {
		return true;
	} else {
		if (initialLeft.value && evt.relatedRect.left - initialLeft.value > 500) {
			return false;
		}

		// Allow dragging only backward or back to the original column
		if (isDraggingForward || evt.relatedRect.left === initialLeft.value) {
			return true;
		}
	}

	return false;
};

const handleDrag = (event) => {
	const id = props?.id || '';
	emit('handleDrag', event, id);
};

const calculateTotal = (list) => {
	const total = list.reduce((acc, item) => acc + parseFloat(item[props.moneryKey] || 0), 0);
	return formatToFinancial(total);
};

// 判断是否为ITEM租户
const isItemTenant = computed(() => {
	const userStore = useUserStore();
	return userStore.getUserInfo?.clientShortName === 'ItemCom';
});

// 计算one-time charge fee总额
const calculateOneTimeTotal = (list) => {
	const total = list.reduce((acc, item) => acc + parseFloat(item['one_time_charge_fee'] || 0), 0);
	return formatToFinancial(total);
};

// 计算monthly charge fee总额
const calculateMonthlyTotal = (list) => {
	const total = list.reduce((acc, item) => acc + parseFloat(item['monthly_charge_fee'] || 0), 0);
	return formatToFinancial(total);
};

const draggableEnd = () => {
	drag.value = false;
	initialLeft.value = null;
	// 新增代码：移除不在list中的checkedStates条目
	for (const key in checkedStates) {
		if (!props.list.some((item) => item[props.itemKey] === key)) {
			delete checkedStates[key];
			emit('update:checkedState', checkedStates); // 发射更新事件
		}
	}
};

function getReadableDateDifference(startDate: string) {
	// 将输入转换为Date对象
	const start = new Date(startDate) as any;
	const end = new Date() as any;

	// 计算毫秒差
	const diffMs = Math.abs(end - start);

	// 计算各单位的差值
	const minutes = Math.floor(diffMs / (1000 * 60));
	const hours = Math.floor(diffMs / (1000 * 60 * 60));
	const days = Math.floor(diffMs / (1000 * 60 * 60 * 24));
	const months = Math.abs(
		(end.getFullYear() - start.getFullYear()) * 12 + (end.getMonth() - start.getMonth())
	);

	// 根据差值大小选择合适的单位
	if (months >= 1) {
		if (months == 1) return `${months} ${t('sys.app.monthAgo').replace('s', '')}`;
		return `${months} ${t('sys.app.monthAgo')}`;
	} else if (days >= 1) {
		if (days == 1) return `${days} ${t('sys.app.dayAgo').replace('s', '')}`;
		return `${days} ${t('sys.app.dayAgo')}`;
	} else if (hours >= 1) {
		if (hours == 1) return `${hours} ${t('sys.app.hourAgo').replace('s', '')}`;
		return `${hours} ${t('sys.app.hourAgo')}`;
	} else if (minutes >= 1) {
		if (minutes == 1) return `${minutes} ${t('sys.app.minuteAgo').replace('s', '')}`;
		return `${minutes} ${t('sys.app.minuteAgo')}`;
	} else {
		return t('sys.app.lessThanAMinute');
	}
}

// 检查是否勾选
const isChecked = (id) => {
	return checkedStates[id]?.isChecked;
};

// 修改toggleCheckbox函数
const toggleCheckbox = (event) => {
	console.log('event:', event);
	checkedStates[event.id] = {
		isChecked: !checkedStates[event.id]?.isChecked,
		canEdit: event.userPermissions?.currentUserCanEdit,
		canView: event.userPermissions?.currentUserCanView,
		canDelete: event.userPermissions?.currentUserCanDelete,
		name: event.deal_name,
	};
	emit('update:checkedState', checkedStates); // 发射事件
};

// 监听list变化并初始化checkedStates
watchEffect(() => {
	props.list.forEach((item) => {
		if (checkedStates[item[props.itemKey]] === undefined) {
			checkedStates[item[props.itemKey]] = {
				isChecked: false,
				canEdit: item.userPermissions?.currentUserCanEdit,
				canView: item.userPermissions?.currentUserCanView,
				canDelete: item.userPermissions?.currentUserCanDelete,
				name: item.deal_name,
			};
			emit('update:checkedState', checkedStates); // 发射事件
		}
	});
});

const changeItem = (item) => {
	emit('changeItem', item);
};
</script>

<style lang="scss" scoped>
.deal-card {
	width: 100%;
	border: 1px solid var(--black-500);
	border-radius: 8px;
	margin-bottom: 10px;
	padding: 15px;
	box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);

	@apply text-sm;
}

div[name='flip-list'] {
	min-height: 200px;
}

.total-footer {
	/* background-color: #f8f9fa; */
	padding: 10px 15px;
	position: absolute;
	bottom: 0;
	left: 0;
	right: 0;
	border-top: 1px solid var(--black-500);
	border-right: 1px solid var(--black-500);
}

.flip-list-move {
	transition: transform 0.5s;
}

.no-move {
	transition: transform 0s;
}

.ghost {
	opacity: 0.5;

	@apply bg-primary-200;
}

.checkbox {
	display: none; // 默认隐藏checkbox
}

.deal-card:hover .checkbox,
.deal-card .checkbox:checked {
	// 当checkbox被勾选时也显示
	display: block;
}
</style>
