<template>
	<el-form ref="formRef" :model="formData" :rules="formRules" label-position="top">
		<!-- 基础信息 -->
		<el-form-item label="Field Name" prop="fieldName">
			<el-input
				v-model="formData.fieldName"
				placeholder="e.g., Customer Email"
				clearable
				class="w-full"
			/>
		</el-form-item>

		<el-form-item label="Description" prop="description">
			<el-input
				v-model="formData.description"
				type="textarea"
				:rows="2"
				placeholder="Describe what this field is used for..."
				clearable
				class="w-full"
			/>
		</el-form-item>

		<el-form-item label="Field Type" prop="dataType">
			<el-select
				v-model="formData.dataType"
				placeholder="Select field type"
				class="w-full"
				filterable
				:disabled="isEdit"
				@change="handleDataTypeChange"
			>
				<el-option
					v-for="type in fieldsTypeEnum"
					:key="type.key"
					:label="type.value"
					:value="type.key"
				/>
			</el-select>
		</el-form-item>

		<el-form-item label="Required Field Message">
			<el-input
				v-model="formData.fieldValidate!.message"
				placeholder="e.g., Please enter your email address"
			/>
		</el-form-item>

		<!-- 数字类型配置 -->
		<template v-if="formData.dataType === propertyTypeEnum.Number">
			<div class="section-title">Number Settings</div>
			<div class="grid grid-cols-3 gap-4">
				<el-form-item label="Allow Decimal">
					<el-switch v-model="formData.additionalInfo!.isFloat" />
				</el-form-item>
				<el-form-item label="Allow Negative">
					<el-switch v-model="formData.additionalInfo!.allowNegative" />
				</el-form-item>
				<el-form-item label="Financial Format">
					<el-switch v-model="formData.additionalInfo!.isFinancial" />
				</el-form-item>
			</div>
			<el-form-item v-if="formData.additionalInfo!.isFloat" label="Decimal Places">
				<InputNumber
					v-model="formData.format!.decimalPlaces"
					:is-foloat="false"
					:min-number="0"
					class="w-full"
				/>
			</el-form-item>
		</template>

		<!-- 日期时间类型配置 -->
		<template v-if="formData.dataType === propertyTypeEnum.DatePicker">
			<div class="section-title">DateTime Settings</div>
			<div class="grid grid-cols-2 gap-4">
				<el-form-item label="Date Format">
					<el-select v-model="formData.format!.dateFormat" class="w-full">
						<el-option :label="projectDate" :value="projectDate" />
						<el-option :label="projectTenMinuteDate" :value="projectTenMinuteDate" />
						<el-option
							:label="projectTenMinutesSsecondsDate"
							:value="projectTenMinutesSsecondsDate"
						/>
					</el-select>
				</el-form-item>
			</div>
		</template>

		<!-- 布尔类型配置 (Switch) -->
		<template v-if="formData.dataType === propertyTypeEnum.Switch">
			<div class="section-title">Boolean Settings</div>
			<div class="grid grid-cols-2 gap-4">
				<el-form-item label="True Label">
					<el-input v-model="formData.additionalInfo!.trueLabel" placeholder="Yes" />
				</el-form-item>
				<el-form-item label="False Label">
					<el-input v-model="formData.additionalInfo!.falseLabel" placeholder="No" />
				</el-form-item>
			</div>
			<el-form-item label="Display Style">
				<el-radio-group v-model="formData.additionalInfo!.displayStyle">
					<el-radio value="switch">Switch</el-radio>
					<el-radio value="checkbox">Checkbox</el-radio>
					<el-radio value="radio">Radio</el-radio>
				</el-radio-group>
			</el-form-item>
		</template>

		<!-- 单行/多行文本配置 -->
		<template
			v-if="
				formData.dataType === propertyTypeEnum.SingleLineText ||
				formData.dataType === propertyTypeEnum.MultilineText
			"
		>
			<div class="section-title">Text Settings</div>
			<div class="grid grid-cols-2 gap-4">
				<el-form-item label="Max Length">
					<InputNumber
						v-model="formData.fieldValidate!.maxLength"
						:is-foloat="false"
						:min-number="1"
						class="w-full"
						:property="{
							clearable: true,
						}"
					/>
				</el-form-item>
				<template v-if="formData.dataType === propertyTypeEnum.MultilineText">
					<el-form-item label="Rows">
						<InputNumber
							v-model="formData.additionalInfo!.rows"
							:is-foloat="false"
							:min-number="2"
							class="w-full"
						/>
					</el-form-item>
				</template>
			</div>
		</template>

		<!-- 下拉选项配置 -->
		<template v-if="formData.dataType === propertyTypeEnum.DropdownSelect">
			<div class="section-title">Dropdown Settings</div>
			<el-form-item>
				<el-checkbox v-model="formData.additionalInfo!.allowMultiple">
					Allow Multiple Selection
				</el-checkbox>
				<el-checkbox v-model="formData.additionalInfo!.allowSearch" class="ml-4">
					Allow Search
				</el-checkbox>
			</el-form-item>
			<el-form-item label="Options">
				<div class="w-full space-y-2">
					<div
						v-for="(item, index) in formData.dropdownItems"
						:key="index"
						class="flex items-center gap-2"
					>
						<div class="flex-1">
							<el-input
								v-model="item.value"
								placeholder="Value"
								:class="{ 'value-duplicate': isValueDuplicate(index, item.value) }"
								@input="handleValueChange"
							/>
							<div
								v-if="isValueDuplicate(index, item.value)"
								class="text-xs text-red-500 mt-1"
							>
								Duplicate value
							</div>
						</div>
						<!-- <el-checkbox
							v-model="item.isDefault"
							title="Default"
							@change="handleDefaultChange(index, $event as boolean)"
						/> -->
						<el-radio
							:model-value="item.isDefault"
							:value="true"
							@change="handleDefaultChange(index)"
							size="small"
							class="mr-2"
							title="Default"
						/>
						<el-button
							type="danger"
							:icon="Delete"
							link
							@click="removeDropdownItem(index)"
						/>
					</div>
					<el-button type="primary" link :icon="Plus" @click="addDropdownItem">
						Add Option
					</el-button>
					<div v-if="dropdownError" class="text-xs text-red-500">
						{{ dropdownError }}
					</div>
				</div>
			</el-form-item>
		</template>

		<!-- 文件类型配置 -->
		<template v-if="formData.dataType === propertyTypeEnum.File">
			<div class="section-title">File Settings</div>
			<div class="grid grid-cols-2 gap-4">
				<el-form-item label="Max Size (MB)">
					<InputNumber
						v-model="maxSizeMB"
						:is-foloat="false"
						:min-number="1"
						class="w-full"
						@field-blur="updateMaxSize"
					/>
				</el-form-item>
				<el-form-item label="Max Count">
					<InputNumber
						v-model="formData.additionalInfo!.maxCount"
						:is-foloat="false"
						:min-number="1"
						class="w-full"
					/>
				</el-form-item>
			</div>
			<el-form-item label="Allowed Extensions">
				<el-select
					v-model="formData.additionalInfo!.allowedExtensions"
					multiple
					filterable
					allow-create
					class="w-full"
					placeholder="Select or type extensions"
				>
					<el-option
						v-for="ext in defaultExtensions"
						:key="ext"
						:label="ext"
						:value="ext"
					/>
				</el-select>
			</el-form-item>
		</template>

		<!-- 人员类型配置 -->
		<template v-if="formData.dataType === propertyTypeEnum.Pepole">
			<div class="section-title">People Settings</div>
			<el-form-item>
				<el-checkbox v-model="formData.additionalInfo!.allowMultiple">
					Allow Multiple Selection
				</el-checkbox>
			</el-form-item>
		</template>
	</el-form>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue';
import { Plus, Delete } from '@element-plus/icons-vue';
import { fieldsTypeEnum, propertyTypeEnum } from '@/enums/appEnum';
import {
	projectDate,
	projectTenMinutesSsecondsDate,
	projectTenMinuteDate,
} from '@/settings/projectSetting';
import InputNumber from '@/components/form/InputNumber/index.vue';
import type { CreateDynamicFieldParams, DynamicList } from '#/dynamic';

defineProps<{
	isEdit?: boolean;
}>();

const formRef = ref();

const getDefaultFormData = (): CreateDynamicFieldParams => ({
	fieldName: '',
	displayName: '',
	description: '',
	dataType: propertyTypeEnum.SingleLineText,
	format: {},
	fieldValidate: { maxLength: 100 },
	additionalInfo: {},
	dropdownItems: [],
});

const formData = reactive<CreateDynamicFieldParams>(getDefaultFormData());

const formRules = {
	fieldName: [
		{
			required: true,
			validator: (_rule: any, value: string, callback: any) => {
				if (!value || !value.trim()) {
					callback(new Error('Field Name is required'));
				} else {
					callback();
				}
			},
			trigger: 'blur',
		},
	],
	dataType: [{ required: true, message: 'Field Type is required', trigger: 'change' }],
};

// 下拉选项错误信息
const dropdownError = ref('');

// 文件大小 MB 转换
const maxSizeMB = ref(10);
watch(
	() => formData.additionalInfo?.maxSize,
	(val) => {
		if (val) maxSizeMB.value = Math.round(val / 1024 / 1024);
	},
	{ immediate: true }
);

const updateMaxSize = () => {
	if (formData.additionalInfo && maxSizeMB.value) {
		formData.additionalInfo.maxSize = Number(maxSizeMB.value) * 1024 * 1024;
	}
};

// 默认文件扩展名
const defaultExtensions = [
	'.pdf',
	'.doc',
	'.docx',
	'.xls',
	'.xlsx',
	'.ppt',
	'.pptx',
	'.txt',
	'.jpg',
	'.jpeg',
	'.png',
	'.gif',
	'.zip',
	'.rar',
];

// 类型切换时重置配置
const handleDataTypeChange = () => {
	formData.format = {};
	formData.fieldValidate = { message: formData.fieldValidate?.message }; // 保留 message
	formData.additionalInfo = {};
	formData.dropdownItems = [];

	switch (formData.dataType) {
		case propertyTypeEnum.Number:
			formData.format = { decimalPlaces: 2 };
			formData.additionalInfo = {
				isFloat: false,
				allowNegative: false,
				isFinancial: false,
			};
			break;
		case propertyTypeEnum.DatePicker:
			formData.format = { dateFormat: projectDate };
			break;
		case propertyTypeEnum.Switch:
			formData.additionalInfo = {
				trueLabel: 'Yes',
				falseLabel: 'No',
				displayStyle: 'switch',
			};
			break;
		case propertyTypeEnum.SingleLineText:
			formData.fieldValidate = { ...formData.fieldValidate, maxLength: 100 };
			break;
		case propertyTypeEnum.MultilineText:
			formData.fieldValidate = { ...formData.fieldValidate, maxLength: 5000 };
			formData.additionalInfo = { rows: 4 };
			break;
		case propertyTypeEnum.DropdownSelect:
			formData.additionalInfo = { allowMultiple: false, allowSearch: true };
			formData.dropdownItems = [];
			break;
		case propertyTypeEnum.File:
			formData.additionalInfo = {
				maxSize: 10 * 1024 * 1024,
				maxCount: 1,
				allowedExtensions: ['.pdf', '.doc', '.docx'],
			};
			break;
		case propertyTypeEnum.Pepole:
			formData.additionalInfo = { allowMultiple: false };
			break;
	}
};

// 下拉选项操作
const addDropdownItem = () => {
	if (!formData.dropdownItems) formData.dropdownItems = [];
	formData.dropdownItems.push({
		id: Date.now(),
		value: '',
		sort: formData.dropdownItems.length + 1,
		isDefault: false,
	});
};

const removeDropdownItem = (index: number) => {
	formData.dropdownItems?.splice(index, 1);
	// 删除后重新验证
	formRef.value?.validateField('dropdownItems');
};

// 检查 value 是否重复
const isValueDuplicate = (index: number, value: string) => {
	if (!value?.trim()) return false;
	return formData.dropdownItems?.some(
		(item, i) => i !== index && item.value?.trim() === value?.trim()
	);
};

// value 输入变化时触发验证
const handleValueChange = () => {
	validateDropdownOptions();
};

// 验证下拉选项
const validateDropdownOptions = (): boolean => {
	if (formData.dataType !== propertyTypeEnum.DropdownSelect) {
		dropdownError.value = '';
		return true;
	}

	// 过滤空值后检查是否有有效选项
	const validItems = formData.dropdownItems?.filter((item) => item.value?.trim()) || [];
	if (validItems.length === 0) {
		dropdownError.value = 'Please add at least one option with a valid value';
		return false;
	}

	// 检查是否有空的 value
	const hasEmptyValue = formData.dropdownItems?.some((item) => !item.value?.trim());
	if (hasEmptyValue) {
		dropdownError.value = 'Option value cannot be empty';
		return false;
	}

	// 检查 value 是否重复
	const values = formData.dropdownItems?.map((item) => item.value?.trim()) || [];
	const uniqueValues = new Set(values);
	if (values.length !== uniqueValues.size) {
		dropdownError.value = 'Option values must be unique';
		return false;
	}

	dropdownError.value = '';
	return true;
};

// 处理默认值变更，确保只有一个默认值
const handleDefaultChange = (index: number) => {
	if (formData.dropdownItems) {
		formData.dropdownItems.forEach((item, i) => {
			item.isDefault = i === index;
		});
	}
};

// 获取表单数据
const getFormData = (): CreateDynamicFieldParams => ({
	...formData,
	displayName: formData.fieldName,
});

// 设置表单数据（编辑时回显）
const setFormData = (data: DynamicList) => {
	Object.assign(formData, {
		fieldName: data.fieldName,
		displayName: data.displayName,
		description: data.description,
		dataType: data.dataType,
		format: data.format || {},
		fieldValidate: data.fieldValidate || {},
		additionalInfo: data.additionalInfo || {},
		dropdownItems: data.dropdownItems || [],
	});
};

// 重置表单
const resetForm = () => {
	const defaultData = getDefaultFormData();
	formData.fieldName = defaultData.fieldName;
	formData.displayName = defaultData.displayName;
	formData.description = defaultData.description;
	formData.dataType = defaultData.dataType;
	formData.format = { ...defaultData.format };
	formData.fieldValidate = { ...defaultData.fieldValidate };
	formData.additionalInfo = { ...defaultData.additionalInfo };
	formData.dropdownItems = [];
	dropdownError.value = '';
	formRef.value?.clearValidate();
};

// 验证表单
const validate = async () => {
	await formRef.value?.validate();
	// 额外验证下拉选项
	if (!validateDropdownOptions()) {
		return Promise.reject(new Error(dropdownError.value));
	}
	return true;
};

defineExpose({ getFormData, setFormData, resetForm, validate });
</script>

<style scoped lang="scss">
.section-title {
	font-size: 13px;
	font-weight: 500;
	color: var(--el-text-color-secondary);
	margin: 16px 0 12px;
	padding-bottom: 8px;
	border-bottom: 1px solid var(--el-border-color-lighter);
}
</style>
