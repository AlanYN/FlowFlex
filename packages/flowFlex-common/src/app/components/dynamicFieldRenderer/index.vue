<template>
	<el-form
		ref="formRef"
		:model="formValues"
		:label-position="labelPosition"
		:label-width="labelWidth"
		:disabled="disabled"
		class="dynamic-form-grid"
	>
		<template v-for="field in fields" :key="field.id">
			<el-form-item
				:label="field.displayName"
				:prop="field.fieldName"
				:rules="getFieldRules(field)"
				:class="getFieldClass(field)"
			>
				<template #label="{ label }">
					<span class="inline-flex items-center gap-x-1">
						{{ label }}
						<el-tooltip
							v-if="field.description"
							:content="field.description"
							placement="top"
						>
							<Icon
								icon="mdi:information-outline"
								class="text-gray-400 cursor-help"
							/>
						</el-tooltip>
					</span>
				</template>

				<!-- Number 数字 -->
				<InputNumber
					v-if="field.dataType === propertyTypeEnum.Number"
					v-model="formValues[field.fieldName]"
					:is-foloat="field.additionalInfo?.isFloat ?? false"
					:minus-number="field.additionalInfo?.allowNegative ?? false"
					:is-financial="field.additionalInfo?.isFinancial ?? false"
					:decimal-places="Number(field.format?.decimalPlaces) || 2"
					class="w-full"
					:property="{
						placeholder: `Please enter ${field.displayName}`,
						disabled: readonly,
					}"
				/>

				<!-- DatePicker 日期 -->
				<el-date-picker
					v-else-if="field.dataType === propertyTypeEnum.DatePicker"
					v-model="formValues[field.fieldName]"
					:type="getDateType(field)"
					:format="field.format?.dateFormat || projectDate"
					:value-format="field.format?.dateFormat || projectDate"
					:placeholder="`Select ${field.displayName}`"
					:disabled="readonly"
					class="w-full"
					:clearable="true"
				/>

				<!-- Switch 布尔/开关 -->
				<template v-else-if="field.dataType === propertyTypeEnum.Switch">
					<el-switch
						v-if="field.additionalInfo?.displayStyle === 'switch'"
						v-model="formValues[field.fieldName]"
						:active-text="field.additionalInfo?.trueLabel || 'Yes'"
						:inactive-text="field.additionalInfo?.falseLabel || 'No'"
						:disabled="readonly"
						inline-prompt
					/>
					<el-checkbox
						v-else-if="field.additionalInfo?.displayStyle === 'checkbox'"
						v-model="formValues[field.fieldName]"
						:disabled="readonly"
					>
						{{ field.additionalInfo?.trueLabel || 'Yes' }}
					</el-checkbox>
					<el-radio-group
						v-else
						v-model="formValues[field.fieldName]"
						:disabled="readonly"
					>
						<el-radio :value="true">
							{{ field.additionalInfo?.trueLabel || 'Yes' }}
						</el-radio>
						<el-radio :value="false">
							{{ field.additionalInfo?.falseLabel || 'No' }}
						</el-radio>
					</el-radio-group>
				</template>

				<!-- SingleLineText 单行文本 -->
				<el-input
					v-else-if="field.dataType === propertyTypeEnum.SingleLineText"
					v-model="formValues[field.fieldName]"
					:maxlength="field.fieldValidate?.maxLength || 100"
					:placeholder="`Please enter ${field.displayName}`"
					:disabled="readonly"
					:show-word-limit="true"
					clearable
				/>

				<!-- MultilineText 多行文本 -->
				<el-input
					v-else-if="field.dataType === propertyTypeEnum.MultilineText"
					v-model="formValues[field.fieldName]"
					type="textarea"
					:rows="Number(field.additionalInfo?.rows) || 4"
					:maxlength="field.fieldValidate?.maxLength || 5000"
					:placeholder="`Please enter ${field.displayName}`"
					:disabled="readonly"
					:resize="Number(field.additionalInfo?.rows) > 1 ? 'none' : 'vertical'"
					:show-word-limit="true"
				/>

				<!-- DropdownSelect 下拉选择 -->
				<el-select
					v-else-if="field.dataType === propertyTypeEnum.DropdownSelect"
					v-model="formValues[field.fieldName]"
					:multiple="field.additionalInfo?.allowMultiple ?? false"
					:filterable="field.additionalInfo?.allowSearch ?? true"
					:placeholder="`Select ${field.displayName}`"
					:disabled="readonly"
					class="w-full"
					clearable
					tag-type="primary"
				>
					<el-option
						v-for="item in field.dropdownItems"
						:key="item.id"
						:label="`${item.value}`"
						:value="item.value"
					/>
				</el-select>

				<!-- File 文件 -->
				<div v-else-if="field.dataType === propertyTypeEnum.File" class="w-full">
					<el-upload
						:key="`upload-${field.fieldName}-${
							getUploadedFiles(field.fieldName).length
						}`"
						:auto-upload="false"
						:show-file-list="false"
						:limit="Number(field.additionalInfo?.maxCount) || 1"
						:accept="getAcceptExtensions(field)"
						:disabled="readonly"
						:on-change="(file: any) => handleFileChange(file, field)"
						:on-exceed="handleExceed"
					>
						<el-button type="primary" :disabled="readonly">Upload File</el-button>
					</el-upload>
					<div class="el-upload__tip text-gray-400 mt-1">
						Max {{ formatFileSize(field.additionalInfo?.maxSize) }},
						{{ field.additionalInfo?.maxCount || 1 }} file(s)
						<span v-if="field.additionalInfo?.allowedExtensions?.length">
							({{ field.additionalInfo.allowedExtensions.join(', ') }})
						</span>
					</div>
					<!-- 上传进度 -->
					<div
						v-if="getUploadProgress(field.fieldName).length > 0"
						class="mt-2 space-y-2"
					>
						<div
							v-for="progress in getUploadProgress(field.fieldName)"
							:key="progress.uid"
							class="flex items-center gap-2 p-2 bg-gray-50 dark:bg-black-200 rounded"
						>
							<Icon icon="mdi:file-upload" class="text-blue-500" />
							<div class="flex-1 min-w-0">
								<div class="text-sm truncate">{{ progress.name }}</div>
								<el-progress
									:percentage="progress.percentage"
									:status="progress.error ? 'exception' : undefined"
									:show-text="false"
									:stroke-width="4"
								/>
							</div>
							<span class="text-xs text-gray-500 flex-shrink-0">
								{{ progress.percentage }}%
							</span>
						</div>
					</div>
					<!-- 已上传文件列表 -->
					<div v-if="getUploadedFiles(field.fieldName).length > 0" class="mt-2 space-y-2">
						<div
							v-for="file in getUploadedFiles(field.fieldName)"
							:key="file.id"
							class="flex items-center gap-2 p-2 bg-gray-50 dark:bg-black-200 rounded"
						>
							<Icon icon="mdi:file-document" class="text-primary flex-shrink-0" />
							<span class="text-sm flex-1 truncate" :title="file.fileName">
								{{ file.fileName }}
							</span>
							<span class="text-xs text-gray-400 flex-shrink-0">
								{{ formatFileSizeDisplay(file.fileSize) }}
							</span>
							<el-button
								type="danger"
								link
								size="small"
								:disabled="readonly"
								@click="handleRemoveFile(field.fieldName, file?.id)"
							>
								<Icon icon="mdi:close" />
							</el-button>
						</div>
					</div>
				</div>

				<!-- People 人员 -->
				<FlowflexUser
					v-else-if="field.dataType === propertyTypeEnum.Pepole"
					v-model="formValues[field.fieldName]"
					:max-count="field.additionalInfo?.allowMultiple ? 0 : 1"
					:disabled="readonly"
					selection-type="user"
					class="w-full"
				/>

				<!-- Email 邮箱 -->
				<el-input
					v-else-if="field.dataType === propertyTypeEnum.Email"
					v-model="formValues[field.fieldName]"
					type="email"
					:placeholder="`Please enter ${field.displayName}`"
					:disabled="readonly"
					clearable
				/>

				<!-- Phone 电话 -->
				<MergedArea
					v-else-if="field.dataType === propertyTypeEnum.Phone"
					v-model="formValues[field.fieldName]"
					:disabled="readonly"
				/>

				<!-- 默认文本输入 -->
				<el-input
					v-else
					v-model="formValues[field.fieldName]"
					:placeholder="`Please enter ${field.displayName}`"
					:disabled="readonly"
					clearable
				/>
			</el-form-item>
		</template>
	</el-form>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { ElMessage } from 'element-plus';
import { propertyTypeEnum } from '@/enums/appEnum';
import { projectDate } from '@/settings/projectSetting';
import InputNumber from '@/components/form/InputNumber/index.vue';
import FlowflexUser from '@/components/form/flowflexUser/index.vue';
import MergedArea from '@/components/form/inputPhone/mergedArea.vue';
import { uploadQuestionFile } from '@/apis/ow/questionnaire';
import { timeZoneConvert } from '@/hooks/time';
import type { DynamicList } from '#/dynamic';
import type { UploadProps } from 'element-plus';

// 上传文件信息接口
interface UploadedFile {
	id: string;
	fileName: string;
	fileSize: number | string;
}

// 上传进度接口
interface UploadProgress {
	uid: string;
	name: string;
	percentage: number;
	error?: string;
	fieldName: string;
}

interface Props {
	fields: DynamicList[];
	labelPosition?: 'top' | 'left' | 'right';
	labelWidth?: string;
	disabled?: boolean;
	readonly?: boolean;
	showDescription?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	labelPosition: 'top',
	labelWidth: 'auto',
	disabled: false,
	readonly: false,
	showDescription: true,
});

const formRef = ref();
const formValues = reactive<Record<string, any>>({});

// 上传进度列表
const uploadProgressList = ref<UploadProgress[]>([]);

// 已上传文件映射 (fieldName -> UploadedFile[])
const uploadedFilesMap = reactive<Record<string, UploadedFile[]>>({});

// 获取指定字段的上传进度
const getUploadProgress = (fieldName: string) => {
	return uploadProgressList.value.filter((p) => p.fieldName === fieldName);
};

// 获取指定字段的已上传文件
const getUploadedFiles = (fieldName: string) => {
	return uploadedFilesMap[fieldName] || [];
};

// 初始化表单值（可传入初始数据）
const initFormValues = (initialData?: Record<string, any>) => {
	props.fields.forEach((field) => {
		const defaultValue = getDefaultValue(field);
		const value = initialData?.[field.fieldName] ?? defaultValue;
		formValues[field.fieldName] = value;

		// 如果是文件类型，初始化已上传文件列表
		if (field.dataType === propertyTypeEnum.File && Array.isArray(value)) {
			// value 可能是文件对象数组
			if (value.length > 0 && typeof value[0] === 'object') {
				// 文件对象数组
				uploadedFilesMap[field.fieldName] = value.map((f: any) => ({
					id: f?.id,
					fileName: f.originalFileName || f.name || 'Unknown',
					fileSize: f.fileSize || f.size || 0,
				}));
				// formValues 也存储完整的文件对象数组
				formValues[field.fieldName] = [...uploadedFilesMap[field.fieldName]];
			} else {
				// 空数组或其他情况
				uploadedFilesMap[field.fieldName] = [];
				formValues[field.fieldName] = [];
			}
		}
	});
};

// 获取字段默认值
const getDefaultValue = (field: DynamicList) => {
	switch (field.dataType) {
		case propertyTypeEnum.Number:
			return null;
		case propertyTypeEnum.Switch:
			return false;
		case propertyTypeEnum.DropdownSelect:
			// 查找默认选项
			const defaultItem = field.dropdownItems?.find((item) => item.isDefault);
			if (field.additionalInfo?.allowMultiple) {
				return defaultItem ? [defaultItem.value] : [];
			}
			return defaultItem?.value ?? null;
		case propertyTypeEnum.File:
			return [];
		case propertyTypeEnum.Pepole:
			return field.additionalInfo?.allowMultiple ? [] : undefined;
		case propertyTypeEnum.Phone:
			return null;
		default:
			return '';
	}
};

// 获取字段验证规则
const getFieldRules = (field: DynamicList) => {
	const rules: any[] = [];

	// 优先使用 isRequired 字段（由 stage 设置），其次检查 fieldValidate.message
	if (field.isRequired || field.fieldValidate?.message) {
		rules.push({
			required: field.isRequired,
			message: field.fieldValidate?.message || `${field.displayName} is required`,
			trigger: field.dataType === propertyTypeEnum.DropdownSelect ? 'change' : 'blur',
		});
	}

	// Email 类型添加格式校验
	if (field.dataType === propertyTypeEnum.Email) {
		rules.push({
			type: 'email',
			message: 'Please enter a valid email address',
			trigger: 'blur',
		});
	}

	return rules;
};

// 获取字段布局类名
const getFieldClass = (field: DynamicList) => {
	// 文件、多行文本 占满整行
	if (
		field.dataType === propertyTypeEnum.File ||
		field.dataType === propertyTypeEnum.MultilineText ||
		field.dataType === propertyTypeEnum.Pepole
	) {
		return 'full-width';
	}
	return '';
};

// 获取日期类型
const getDateType = (field: DynamicList) => {
	const format = field.format?.dateFormat || '';
	if (format.includes('HH:mm:ss')) return 'datetime';
	if (format.includes('HH:mm')) return 'datetime';
	return 'date';
};

// 获取文件接受的扩展名
const getAcceptExtensions = (field: DynamicList) => {
	return field.additionalInfo?.allowedExtensions?.join(',') || '';
};

// 格式化文件大小
const formatFileSize = (bytes?: number) => {
	if (!bytes) return '10MB';
	const mb = bytes / 1024 / 1024;
	return `${mb}MB`;
};

// 格式化文件大小显示
const formatFileSizeDisplay = (bytes: number | string): string => {
	const numBytes = typeof bytes === 'string' ? parseInt(bytes, 10) : bytes;
	if (!numBytes || isNaN(numBytes)) return '0 B';
	const k = 1024;
	const sizes = ['B', 'KB', 'MB', 'GB'];
	const i = Math.floor(Math.log(numBytes) / Math.log(k));
	return parseFloat((numBytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
};

// 文件上传前验证
const validateFile = (file: File, field: DynamicList): boolean => {
	const maxSize = Number(field.additionalInfo?.maxSize) || 10 * 1024 * 1024;
	if (file.size > maxSize) {
		ElMessage.error(`File size cannot exceed ${formatFileSize(maxSize)}`);
		return false;
	}

	const allowedExtensions = field.additionalInfo?.allowedExtensions;
	if (allowedExtensions?.length) {
		const ext = '.' + file.name.split('.').pop()?.toLowerCase();
		if (!allowedExtensions.includes(ext)) {
			ElMessage.error(`Only ${allowedExtensions.join(', ')} files are allowed`);
			return false;
		}
	}

	return true;
};

// 处理文件选择变化 - 立即上传
const handleFileChange = async (file: any, field: DynamicList) => {
	if (!file.raw) return;

	// 验证文件
	if (!validateFile(file.raw, field)) {
		return;
	}

	// 检查文件数量限制
	const maxCount = Number(field.additionalInfo?.maxCount) || 1;
	const currentFiles = uploadedFilesMap[field.fieldName] || [];
	if (currentFiles.length >= maxCount) {
		ElMessage.warning(`Maximum ${maxCount} file(s) allowed`);
		return;
	}

	try {
		// 添加到进度列表
		uploadProgressList.value.push({
			uid: file.uid,
			name: file.name,
			percentage: 0,
			fieldName: field.fieldName,
		});

		// 构建上传参数
		const uploadParams = {
			name: 'formFile',
			file: file.raw,
			filename: file.raw.name,
			data: {
				category: 'Document',
				description: `${file.raw.name} uploaded via dynamic field`,
			},
		};

		// 调用上传 API
		const response = await uploadQuestionFile(uploadParams, (progressEvent: any) => {
			const existingIndex = uploadProgressList.value.findIndex((p) => p.uid === file.uid);
			if (existingIndex >= 0 && progressEvent.total > 0) {
				uploadProgressList.value[existingIndex].percentage = Math.round(
					(progressEvent.loaded * 100) / progressEvent.total
				);
			}
		});

		// 从进度列表中移除
		uploadProgressList.value = uploadProgressList.value.filter((p) => p.uid !== file.uid);

		if (response?.data?.code === '200') {
			ElMessage.success(`${file.name} uploaded successfully`);
			// 添加到已上传文件列表
			const uploadedFile: UploadedFile = {
				id: response?.data?.data?.id,
				fileName: response?.data?.data?.originalFileName || file.name,
				fileSize: response?.data?.data?.fileSize || file.raw?.size || 0,
			};

			if (!uploadedFilesMap[field.fieldName]) {
				uploadedFilesMap[field.fieldName] = [];
			}
			uploadedFilesMap[field.fieldName].push(uploadedFile);
			// 更新 formValues 中的文件 ID 列表
			updateFileIds(field.fieldName);
		} else {
			ElMessage.error(response?.data?.msg || `Failed to upload ${file.name}`);
		}
	} catch (error) {
		console.error('Upload error:', error);
		ElMessage.error(`Failed to upload ${file.name}`);
		// 从进度列表中移除
		uploadProgressList.value = uploadProgressList.value.filter((p) => p.uid !== file.uid);
	}
};

// 移除已上传文件
const handleRemoveFile = (fieldName: string, fileId: string) => {
	if (uploadedFilesMap[fieldName] && fileId) {
		uploadedFilesMap[fieldName] = uploadedFilesMap[fieldName].filter((f) => f?.id !== fileId);
		updateFileIds(fieldName);
	}
};

// 更新 formValues 中的文件对象列表
const updateFileIds = (fieldName: string) => {
	const files = uploadedFilesMap[fieldName] || [];
	// 存储完整的文件对象数组，包含 id, fileName, fileSize
	formValues[fieldName] = files.map((f) => ({
		id: f.id,
		fileName: f.fileName,
		fileSize: f.fileSize,
	}));
};

// 文件超出限制
const handleExceed: UploadProps['onExceed'] = () => {
	ElMessage.warning('File limit exceeded');
};

// 获取表单数据（日期转换为 UTC 时间）
const getFormData = () => {
	const result: Record<string, any> = {};
	Object.keys(formValues).forEach((key) => {
		const field = props.fields.find((f) => f?.fieldName === key);
		const value = formValues[key];
		// 日期类型转换为 UTC 时间
		if (field?.dataType === propertyTypeEnum.DatePicker && value) {
			result[key] = timeZoneConvert(value, true);
		} else {
			result[key] = value;
		}
	});
	return result;
};

// 设置表单数据（日期从 UTC 转换为项目时区）
const setFormData = (data: Record<string, any>) => {
	initFormValues();
	Object.keys(data).forEach((key) => {
		// 查找对应的字段配置
		const field = props.fields.find((f) => f?.fieldName === key);
		let value = data[key];
		// 日期类型转换为项目时区时间
		if (field?.dataType === propertyTypeEnum.DatePicker && value) {
			const format = field.format?.dateFormat || projectDate;
			formValues[key] = timeZoneConvert(value, false, format);
		} else if (field?.dataType === propertyTypeEnum.File && Array.isArray(value)) {
			if (value.length > 0 && typeof value[0] === 'object') {
				// 文件对象数组
				uploadedFilesMap[key] = value.map((f: any) => ({
					id: f?.id,
					fileName: f?.fileName || f?.name || 'Unknown',
					fileSize: f?.fileSize || f?.size || 0,
				}));
				// formValues 存储完整的文件对象数组
				formValues[key] = [...uploadedFilesMap[key]];
			} else {
				// 空数组或其他情况
				uploadedFilesMap[key] = [];
				formValues[key] = [];
			}
		} else if (field?.dataType == propertyTypeEnum.DropdownSelect && !value) {
			formValues[key] = getDefaultValue(field);
		} else {
			formValues[key] = value;
		}
	});
};

// 重置表单
const resetForm = () => {
	initFormValues();
	// 清空已上传文件
	Object.keys(uploadedFilesMap).forEach((key) => {
		uploadedFilesMap[key] = [];
	});
	uploadProgressList.value = [];
	formRef.value?.clearValidate();
};

// 验证表单
const validate = async () => {
	return formRef.value?.validate();
};

// 清除验证
const clearValidate = () => {
	formRef.value?.clearValidate();
};

defineExpose({
	initFormValues,
	getFormData,
	setFormData,
	resetForm,
	validate,
	clearValidate,
});
</script>

<style lang="scss" scoped>
.dynamic-form-grid {
	display: grid;
	grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
	gap: 16px;
	width: 100%;

	// 占满整行
	.full-width {
		grid-column: 1 / -1;
	}
}
</style>
