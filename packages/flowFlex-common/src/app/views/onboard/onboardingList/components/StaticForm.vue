<template>
	<div class="wfe-global-block-bg">
		<!-- 统一的头部卡片 -->
		<div
			class="case-component-header rounded-xl"
			:class="{ expanded: isExpanded }"
			@click="toggleExpanded"
		>
			<div class="flex justify-between">
				<div>
					<div class="flex items-center">
						<el-icon
							class="case-component-expand-icon text-lg mr-2"
							:class="{ rotated: isExpanded }"
						>
							<ArrowRight />
						</el-icon>
						<h3 class="case-component-title">Fields</h3>
					</div>
				</div>
			</div>
		</div>

		<!-- 可折叠表单内容 -->
		<el-collapse-transition>
			<div v-show="isExpanded" class="p-4">
				<!-- 骨架屏加载状态 -->
				<div v-if="loading" class="form-skeleton">
					<div v-for="i in 4" :key="i" class="skeleton-item">
						<el-skeleton :rows="1" animated>
							<template #template>
								<div class="skeleton-field">
									<el-skeleton-item
										variant="text"
										style="width: 30%; height: 14px; margin-bottom: 8px"
									/>
									<el-skeleton-item
										variant="rect"
										style="width: 100%; height: 32px; border-radius: 8px"
									/>
								</div>
							</template>
						</el-skeleton>
					</div>
				</div>

				<!-- 表单内容 -->
				<template v-else>
					<DynamicFieldRenderer
						ref="dynamicFormRef"
						:fields="dynamicFields"
						:disabled="disabled"
						:readonly="disabled"
						label-position="top"
					/>
					<el-empty
						v-if="dynamicFields.length === 0"
						description="No fields configured"
						:image-size="50"
					/>
				</template>
			</div>
		</el-collapse-transition>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted, useTemplateRef, nextTick } from 'vue';
import { ArrowRight } from '@element-plus/icons-vue';
import { ElMessage, ElNotification } from 'element-plus';
import DynamicFieldRenderer from '@/components/dynamicFieldRenderer/index.vue';
import { batchIdsDynamicFields } from '@/apis/global/dyanmicField';
import { saveQuestionnaireStatic } from '@/apis/ow/onboarding';
import type { DynamicList } from '#/dynamic';

const props = defineProps<{
	staticFields: {
		id: string;
		isRequired: boolean;
		order: number;
	}[]; // 需要显示的字段名列表
	onboardingId: string;
	stageId: string;
	disabled?: boolean;
}>();

const emit = defineEmits(['save-success']);

// 动态表单组件引用
const dynamicFormRef = useTemplateRef('dynamicFormRef');

// 折叠状态
const isExpanded = ref(true);

// 加载状态
const loading = ref(false);

// 动态字段列表
const dynamicFields = ref<DynamicList[]>([]);

// 切换展开状态
const toggleExpanded = () => {
	isExpanded.value = !isExpanded.value;
};

// 加载动态字段
const loadDynamicFields = async () => {
	if (!props.staticFields || props.staticFields.length === 0) {
		dynamicFields.value = [];
		return;
	}

	try {
		loading.value = true;
		const res = await batchIdsDynamicFields({
			ids: props.staticFields.map((item) => item.id),
		});

		if (res.code === '200' && Array.isArray(res.data)) {
			// 按 staticFields 顺序获取字段
			dynamicFields.value = res?.data
				.map((item) => {
					const field = props.staticFields.find((field) => field.id === item.id);
					return {
						...item,
						order: field?.order || item.sort,
						isRequired: field?.isRequired || false,
					};
				})
				.sort((a, b) => a.order - b.order);
		}
	} finally {
		loading.value = false;
		// 加载完成后，检查是否有待设置的数据
		if (pendingFormData.value) {
			await nextTick();
			dynamicFormRef.value?.setFormData(pendingFormData.value);
			pendingFormData.value = null;
		}
	}
};

// 初始化表单（供外部调用）
const initFormValues = (initialData?: Record<string, any>) => {
	dynamicFormRef.value?.initFormValues(initialData);
};

// 表单验证方法
const validateForm = async () => {
	try {
		await dynamicFormRef.value?.validate();
		return true;
	} catch {
		return false;
	}
};

// 获取表单数据
const getFormData = () => {
	const formData = dynamicFormRef.value?.getFormData() || {};

	// 转换为 API 需要的格式
	return dynamicFields.value.map((field) => ({
		fieldName: field.fieldName,
		fieldValueJson: JSON.stringify(formData[field.fieldName] ?? null),
		fieldType: String(field.dataType),
		isRequired: field.isRequired || false,
		fieldLabel: field.displayName,
	}));
};

// 待设置的表单数据（用于组件未就绪时缓存）
const pendingFormData = ref<Record<string, any> | null>(null);

// 等待组件就绪并设置数据
const waitForRefAndSetData = async (dataToSet: Record<string, any>, maxRetries = 10) => {
	for (let i = 0; i < maxRetries; i++) {
		await nextTick();
		if (dynamicFormRef.value && !loading.value) {
			dynamicFormRef.value.setFormData(dataToSet);
			pendingFormData.value = null;
			return;
		}
		// 等待 100ms 后重试
		await new Promise((resolve) => setTimeout(resolve, 100));
	}
	// 超过重试次数，缓存数据等待后续处理
	pendingFormData.value = dataToSet;
};

// 设置表单字段值
const setFieldValues = (fieldValues: Record<string, any> | any[]) => {
	let dataToSet: Record<string, any> = {};

	if (Array.isArray(fieldValues)) {
		// 处理数组格式
		fieldValues.forEach((field) => {
			if (field.fieldName && field.fieldValueJson !== undefined) {
				try {
					dataToSet[field.fieldName] = JSON.parse(field.fieldValueJson);
				} catch {
					dataToSet[field.fieldName] = field.fieldValueJson;
				}
			}
		});
	} else if (fieldValues && typeof fieldValues === 'object') {
		// 处理对象格式
		Object.keys(fieldValues).forEach((key) => {
			try {
				dataToSet[key] =
					typeof fieldValues[key] === 'string'
						? JSON.parse(fieldValues[key])
						: fieldValues[key];
			} catch {
				dataToSet[key] = fieldValues[key];
			}
		});
	}

	// 如果组件已就绪且不在加载中，直接设置
	if (dynamicFormRef.value && !loading.value) {
		nextTick(() => {
			dynamicFormRef.value?.setFormData(dataToSet);
		});
	} else {
		// 否则等待组件就绪
		waitForRefAndSetData(dataToSet);
	}
};

// 保存表单
const saving = ref(false);
const handleSave = async (isValidate: boolean = true) => {
	try {
		saving.value = true;

		if (isValidate) {
			const isValid = await validateForm();
			if (!isValid || !props.onboardingId) {
				ElNotification({
					title: 'Please complete all required fields',
					type: 'warning',
				});
				return false;
			}
		}

		const formData = getFormData();
		const res = await saveQuestionnaireStatic({
			fieldValues: formData,
			onboardingId: props.onboardingId,
			stageId: props.stageId,
		});

		if (res.code === '200') {
			isValidate && emit('save-success');
			return true;
		} else {
			isValidate && ElMessage.error(res.msg || 'Failed to save stage data');
			return false;
		}
	} catch (error) {
		console.error('Save error:', error);
		return false;
	} finally {
		saving.value = false;
	}
};

// 刷新字段列表
const refreshFields = async () => {
	await loadDynamicFields();
};

onMounted(() => {
	loadDynamicFields();
});

// 暴露给父组件的方法
defineExpose({
	initFormValues,
	validateForm,
	getFormData,
	setFieldValues,
	handleSave,
	refreshFields,
});
</script>

<style lang="scss" scoped>
.form-skeleton {
	display: grid;
	grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
	gap: 16px;
	width: 100%;

	.skeleton-item {
		.skeleton-field {
			display: flex;
			flex-direction: column;
		}
	}
}
</style>
