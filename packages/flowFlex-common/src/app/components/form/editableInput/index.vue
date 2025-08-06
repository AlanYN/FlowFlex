<template>
	<div class="flex">
		<div v-show="blurInput" class="flex items-center group relative">
			<el-button
				:type="type"
				link
				:disabled="disabled"
				@click.stop="editableInput"
				class="whitespace-normal break-all leading-6 group relative"
				:class="computedProperty.disabled ? 'cursor-not-allowed' : ''"
			>
				<div
					v-if="fieldName == 'assigned_to'"
					class="group-hover:pr-6 flex items-center text-left tracking-wide leading-6 editText-hover"
				>
					<peopleTags :value="inputValue as string" :options="assignKeyOptions" />
				</div>
				<div
					v-else-if="textType == propertyTypeEnum.DropdownSelect"
					class="group-hover:pr-6 flex items-center text-left tracking-wide leading-6 editText-hover"
				>
					<div>
						{{
							!property?.remote
								? getOptionDisplayValue(inputValue) || inputValue || defaultStr
								: searchOptions?.find((i: any) => i.value == inputValue)?.label ||
								  inputValue ||
								  defaultStr
						}}
					</div>
					<el-icon class="ml-2">
						<CaretBottom />
					</el-icon>
				</div>
				<div
					v-else-if="textType == propertyTypeEnum.Pepole"
					class="flex items-center text-left tracking-wide leading-6 editText-hover group-hover:pr-6"
				>
					<peopleTags :value="inputValue as string" :options="assignOptions" />
					<el-icon class="ml-2">
						<CaretBottom />
					</el-icon>
				</div>
				<div
					v-else-if="textType == propertyTypeEnum.Connection"
					class="flex items-center text-left tracking-wide leading-6 editText-hover group-hover:pr-6"
				>
					<div>
						{{
							inputValue
								? `${getConnectionValue(inputValue, 'value') || ''}(${
										getConnectionValue(inputValue, 'businessValue') || ''
								  })`
								: defaultStr
						}}
					</div>
					<el-icon class="ml-2">
						<CaretBottom />
					</el-icon>
				</div>
				<div
					v-else-if="textType == propertyTypeEnum.TimeLine"
					class="flex items-center text-left tracking-wide leading-6 editText-hover group-hover:pr-6"
				>
					<time-line v-model="timeLineValue" :enable="false" :show-set-date="true" />
				</div>
				<div
					v-else
					class="flex items-center text-left tracking-wide leading-6 editText-hover group-hover:pr-6"
				>
					<div v-if="computedProperty?.isFinancial">
						$ {{ formatToFinancial(inputValue) || defaultStr }}
					</div>
					<div v-else-if="computedProperty.type == 'userTextarea'">
						{{
							inputValue
								? `${inputValue}`?.replace(/\[~(\S+?)\]/g, '@$1')
								: defaultStr
						}}
					</div>
					<div v-else>{{ inputValue || defaultStr }}</div>
				</div>
				<el-icon
					class="edit-icon"
					:class="{ 'group-hover:opacity-100': !computedProperty.disabled }"
				>
					<Edit />
				</el-icon>
			</el-button>
			<div class="flex items-center ml-2" v-if="isCopyText && inputValue">
				<el-icon class="text-primary-500" @click.stop="copyText(inputValue)">
					<DocumentCopy />
				</el-icon>
			</div>
		</div>
		<div v-show="!blurInput" @keydown.stop="handleKeydown" class="editable-input w-full">
			<el-select
				v-if="fieldName == 'assigned_to'"
				ref="inputRef"
				:style="{ minWidth: inputMinWidth }"
				v-model="inputValue"
				:placeholder="t('sys.placeholder.selectPlaceholder')"
				clearable
				filterable
				remote-show-suffix
				remote
				:remote-method="assignKeyRemoteMethod"
				:loading="assignKeyLoading"
				@blur="handleBlur"
				@change="handleSelectChange"
			>
				<el-option
					v-for="item in assignKeyOptions"
					:key="item.key"
					:label="item.value"
					:value="item.key"
				/>
				<template #loading>
					<svg class="circular" viewBox="0 0 50 50">
						<circle class="path" cx="25" cy="25" r="20" fill="none" />
					</svg>
				</template>
			</el-select>
			<el-date-picker
				v-else-if="textType == propertyTypeEnum.DatePicker"
				ref="inputRef"
				v-model="inputValue as string"
				:default-value="getTimeZoneOffsetForTimezone()"
				:value-format="computedProperty.valueFormat || projectDate"
				class="rounded-lg my-date-picker"
				:style="{ minWidth: inputMinWidth }"
				:type="computedProperty.dateType"
				:placeholder="computedProperty.placeholder || t('sys.placeholder.datePlaceholder')"
				:format="computedProperty.valueFormat || projectDate"
				:clearable="computedProperty.clearable"
				:disabled="computedProperty.disabled"
				@change="handChangeSelect"
				@blur="handleBlur"
			>
				<template #focus="{ focus }">
					<button @click="focus">Focus</button>
				</template>
			</el-date-picker>
			<el-select
				v-else-if="textType == propertyTypeEnum.DropdownSelect"
				ref="inputRef"
				:style="{ minWidth: inputMinWidth }"
				v-model="inputValue"
				:placeholder="
					computedProperty.placeholder || t('sys.placeholder.selectPlaceholder')
				"
				:type="computedProperty.type"
				:clearable="computedProperty.clearable"
				:maxlength="computedProperty.maxlength"
				:readonly="computedProperty.readonly"
				:disabled="computedProperty.disabled"
				:multiple="computedProperty.multiple"
				filterable
				:remote="computedProperty.remote"
				:loading="searchLoading"
				:remote-method="remoteMethod"
				@blur="handleBlur"
				@change="handleSelectChange"
			>
				<el-option
					v-for="item in !property?.remote
						? options.map((item) => ({
								label: item.value,
								value: item.key,
						  })) || []
						: searchOptions"
					:key="item.value"
					:label="item.label"
					:value="item.value"
				/>
				<template #loading>
					<svg class="circular" viewBox="0 0 50 50">
						<circle class="path" cx="25" cy="25" r="20" fill="none" />
					</svg>
				</template>
			</el-select>
			<input-number
				v-else-if="textType == propertyTypeEnum.Number"
				ref="inputRef"
				v-model="inputValue as string"
				@blur="handleBlur"
				:property="computedProperty"
				:isFinancial="computedProperty?.isFinancial"
				:isFoloat="computedProperty?.isFoloat"
				:minusNumber="computedProperty?.minusNumber"
			/>
			<PeopleSelect
				v-else-if="textType == propertyTypeEnum.Pepole"
				ref="inputRef"
				v-model="inputValue"
				:style="{ minWidth: inputMinWidth }"
				:options="assignOptions"
				:loading="optionsLoading"
				@blur="handleBlur"
				@change="handleSelectChange"
				@search="peopleRemoteMethod"
			/>
			<ConnectionInput
				v-else-if="textType == propertyTypeEnum.Connection"
				ref="connectionInputRef"
				v-model="inputValue as string"
				:property="{
					module: property?.connectionItems?.[0]?.[0],
					filedKey: property?.connectionItems?.[0]?.[1],
				}"
				:style="{ minWidth: inputMinWidth }"
				@blur="handleBlur"
				@change="handleSelectChange"
				@update-options="handleUpdateOptions"
			/>
			<div v-else-if="textType == propertyTypeEnum.TimeLine">
				<el-date-picker
					ref="inputRef"
					style="width: 200px"
					v-model="inputValue"
					type="daterange"
					range-separator="to"
					start-placeholder="Start date"
					end-placeholder="End date"
					format="MMM DD"
					:clearable="computedProperty.clearable"
					:disabled="computedProperty.disabled"
					@blur="handleBlur"
					:value-format="projectDate"
					class="custom-date-picker"
				>
					<template #focus="{ focus }">
						<button @click="focus">Focus</button>
					</template>
				</el-date-picker>
			</div>
			<MergedArea
				v-else-if="textType == propertyTypeEnum.Phone"
				ref="inputRef"
				v-model="inputValue as string"
				:style="{ width: '250px' }"
				@blur="handleBlur"
				@change="handleSelectChange"
			/>
			<div v-else>
				<mention
					ref="inputRef"
					v-if="computedProperty.type == 'userTextarea'"
					v-model="inputValue as string"
					@blur="handleBlur"
				/>
				<el-input
					v-else
					ref="inputRef"
					v-model="inputValue as string"
					@blur="handleBlur"
					:type="computedProperty.type"
					:clearable="computedProperty.clearable"
					:placeholder="
						computedProperty.placeholder || t('sys.placeholder.inputPlaceholder')
					"
					:maxlength="computedProperty.maxlength"
					:readonly="computedProperty.readonly"
					:disabled="computedProperty.disabled"
					:show-word-limit="computedProperty.showWordLimit"
					:autoSize="computedProperty.autoSize"
					:rows="computedProperty?.autoSize?.minRows"
					:style="{ minWidth: inputMinWidth }"
				/>
			</div>
		</div>
	</div>
</template>

<script lang="ts" setup>
import { ref, computed, nextTick, watch, onMounted, toRaw } from 'vue';
import { ElInput, ElDatePicker, ElButton, ElIcon, ElSelectV2, ElMessage } from 'element-plus';
import { DocumentCopy, Edit, CaretBottom } from '@element-plus/icons-vue';
import { defaultStr, projectDate } from '@/settings/projectSetting';
import { useI18n } from '@/hooks/useI18n';
import { getTimeZoneOffsetForTimezone, timeZoneConvert } from '@/hooks/time';
import InputNumber from '@/components/form/InputNumber/index.vue';
import ConnectionInput from '@/components/form/connectionInput/index.vue';
import TimeLine from '@/components/form/timeLine/index.vue';
import MergedArea from '@/components/form/inputPhone/mergedArea.vue';
import { Options } from '#/setting';
import { InputProperty } from '#/config';
import { useUserStore } from '@/stores/modules/user';
import { formatToFinancial } from '@/utils';
import { assginToOprions } from '@/hooks/searchAssginTo';
import peopleTags from '@/components/form/peopleTags/index.vue';
import PeopleSelect from '../peopleSelect/index.vue';
import { propertyTypeEnum } from '@/enums/appEnum';
import mention from '@/components/mention/mention.vue';

const userStore = useUserStore();
const {
	assignOptions,
	optionsLoading,
	remoteMethod: peopleRemoteMethod,
	initSearchAssgin,
} = assginToOprions(true);

const {
	assignOptions: assignKeyOptions,
	optionsLoading: assignKeyLoading,
	remoteMethod: assignKeyRemoteMethod,
	initSearchAssgin: assignKeyInitSearchAssgin,
} = assginToOprions();
const { t } = useI18n();

interface EditableInputProps {
	modelValue: string | number | boolean | null | undefined | any[] | any;
	type?: '' | 'default' | 'text' | 'primary' | 'success' | 'warning' | 'info' | 'danger';
	textType?: (typeof propertyTypeEnum)[keyof typeof propertyTypeEnum];
	fieldName?: string;
	isCopyText?: boolean;
	property?: InputProperty;
	options?: Options[];
	searchApi?: (text: string, type?: number) => Promise<any>;
	disabled?: boolean;
	inputMinWidth?: string;
}

const defaultProperty: InputProperty = {
	type: 'text',
	clearable: true,
	readonly: false,
	placeholder: '',
	maxlength: -1,
	disabled: false,
	dateType: 'date',
	valueFormat: projectDate,
};

const props = withDefaults(defineProps<EditableInputProps>(), {
	type: 'info',
	textType: propertyTypeEnum.SingleLineText,
	isCopyText: false,
	options: () => [],
	disabled: false,
	inputMinWidth: '250px',
});

const computedProperty = computed(() => {
	return { ...defaultProperty, ...props.property };
});

const emit = defineEmits(['update:modelValue', 'change', 'blur']);

const inputValue = computed({
	get: () => {
		if (props.textType == propertyTypeEnum.DatePicker) {
			return timeZoneConvert(
				props.modelValue as string,
				false,
				computedProperty.value.valueFormat
			);
		} else if (props.textType == propertyTypeEnum.TimeLine) {
			if (props.modelValue) {
				return [
					timeZoneConvert(props.modelValue.startDate),
					timeZoneConvert(props.modelValue.endDate),
				];
			}
			return [];
		} else {
			return props.modelValue;
		}
	},
	set: (value) => {
		let result = value;
		if (props.textType == propertyTypeEnum.DatePicker) {
			result = timeZoneConvert(value as string, true);
		} else if (props.textType == propertyTypeEnum.TimeLine) {
			if (value && value?.length == 2) {
				result = {
					startDate: timeZoneConvert(value[0], true),
					endDate: timeZoneConvert(value[1], true),
				};
			} else {
				result = null;
			}
		}
		emit('update:modelValue', result);
	},
});

const timeLineValue = computed(() => {
	return { startDate: props.modelValue?.startDate, endDate: props.modelValue?.endDate };
});

const blurInput = ref(true);
const connectionInputRef = ref<InstanceType<typeof ConnectionInput>>();
const connectionOptions = ref<any[]>([]);

const handleBlur = () => {
	if (connectionInputRef.value) {
		connectionOptions.value = connectionInputRef.value.options;
	}
	setTimeout(() => {
		blurInput.value = true;
	}, 100);
	emit('blur');
};

const handleUpdateOptions = () => {
	connectionOptions.value = connectionInputRef.value?.options || [];
};

const handleSelectChange = (value) => {
	emit('change', value);
};

const handChangeSelect = () => {
	nextTick(() => {
		inputRef.value?.$el.nextElementSibling.querySelector('input').focus();
	});
};

const focusInput = () => {
	nextTick(() => {
		if (
			props.textType == propertyTypeEnum.DatePicker ||
			props.textType == propertyTypeEnum.TimeLine
		) {
			inputRef.value?.$el.nextElementSibling.querySelector('input').focus();
		} else {
			inputRef.value?.focus();
			connectionInputRef.value?.focus();
		}
	});
};

const inputRef = ref<InstanceType<typeof ElInput | typeof ElSelectV2>>();
const editableInput = () => {
	nextTick(() => {
		if (!computedProperty.value.disabled) {
			blurInput.value = false;
			focusInput();
		}
	});
};

const copyText = async (text) => {
	if (!text) return;
	try {
		await navigator.clipboard.writeText(text);
		ElMessage.success(t('sys.title.copySucceed'));
	} catch {
		ElMessage.info(t('sys.title.copyFailed'));
	}
};

const initAssginToOptions = ref<any[]>(
	props.options.map((item) => ({ label: item.value, value: item.key }))
);

const searchOptions = computed<{ label: string; value: string | number }[]>(() => {
	const arr = [
		{
			label: userStore.getUserInfo.userName || userStore.getUserInfo.realName || '',
			value: userStore.getUserInfo.userId as string,
		},
		...toRaw(initAssginToOptions.value),
	];
	return Object.values(
		arr.reduce(
			(acc, curr) => ({
				...acc,
				[curr.value]: curr,
			}),
			{}
		)
	);
});

const searchLoading = ref(false);
const remoteMethod = (text) => {
	if (!props?.searchApi || !text || props.fieldName == 'assigned_to') return [];
	searchLoading.value = true;
	props
		.searchApi(text)
		.then((res) => {
			initAssginToOptions.value =
				res.data.map((item) => ({ label: item.value, value: item.key })) || [];
		})
		.finally(() => {
			searchLoading.value = false;
		});
};

const initSearchOptions = async (value) => {
	try {
		const arr = initAssginToOptions.value.map((item) => item.value);
		if (arr.indexOf(value) >= 0) {
			return;
		}
		searchLoading.value = true;
		const res = props.searchApi && (await props.searchApi(value, 2));
		initAssginToOptions.value =
			res.data.map((item) => ({ label: item.value, value: item.key })) || [];
	} finally {
		searchLoading.value = false;
	}
};

const shouldInitSearchOptions = (value) => {
	return (
		props.textType == propertyTypeEnum.DropdownSelect &&
		props.property?.remote &&
		value &&
		(searchOptions.value.length <= 1 ||
			!searchOptions.value.some((option) => option.value === value))
	);
};

onMounted(() => {
	if (shouldInitSearchOptions(props.modelValue)) {
		initSearchOptions(props.modelValue);
	}

	if (props.textType == propertyTypeEnum.Pepole) {
		initSearchAssgin(props.modelValue);
	}
	if (props.fieldName == 'assigned_to') {
		assignKeyInitSearchAssgin(props.modelValue);
	}
});

watch(
	() => props.modelValue,
	(newValue) => {
		if (shouldInitSearchOptions(newValue)) {
			initSearchOptions(newValue);
		}
		if (props.textType == propertyTypeEnum.Pepole) {
			initSearchAssgin(props.modelValue);
		}
		if (props.fieldName == 'assigned_to') {
			assignKeyInitSearchAssgin(props.modelValue);
		}
	}
);

const handleKeydown = (event) => {
	event.stopPropagation();
};

const getOptionDisplayValue = (value: any) => {
	// 尝试两种类型的匹配方式
	const optionItem = props.options?.find((i: any) => i.key == value || i.id == value);
	if (!optionItem) return null;

	if ('value' in optionItem) {
		// 是Options类型
		return (optionItem as unknown as Options).value;
	}
	return null;
};

const getConnectionValue = (value: any, field: 'value' | 'businessValue') => {
	const connectionItem = connectionOptions.value.find((i: any) => i.key == value);
	return connectionItem ? connectionItem[field] : '';
};
</script>

<style lang="scss" scoped>
:deep(.el-button--info) {
	background-color: initial;
	border-color: transparent;
}

.editText-hover {
	@apply px-2 transition-all duration-200 leading-6 box-border;
	border: 1px solid transparent;
}

.editText-hover:hover {
	@apply bg-black-70 border-primary-500 rounded text-primary-500;
}

.edit-icon {
	@apply absolute right-1 top-1/2 transform -translate-y-1/2 opacity-0 transition-opacity duration-200 text-primary-500;
}

.group:hover .edit-icon {
	@apply opacity-100;
}

:deep(.el-button--info.is-link) {
	@apply text-[#606266];
}
</style>
