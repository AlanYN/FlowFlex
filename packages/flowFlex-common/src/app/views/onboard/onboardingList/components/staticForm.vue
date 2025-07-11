<template>
	<div class="customer-block">
		<h2 class="text-lg font-semibold">Request fields</h2>
		<el-divider />
		<el-form
			ref="formRef"
			:model="formData"
			:rules="formRules"
			label-position="top"
			class="form-grid"
		>
			<!-- Lead Basic Info -->
			<el-form-item
				label="Lead ID"
				prop="leadId"
				class="form-item"
				v-if="staticFields.includes('LEADID')"
			>
				<el-input
					v-model="formData.leadId"
					placeholder="Search or enter Lead ID"
					clearable
					class="text-sm min-w-[250px]"
				/>
			</el-form-item>

			<el-form-item
				label="Lead Company Name"
				prop="companyName"
				v-if="staticFields.includes('COMPANYNAME')"
			>
				<el-input
					v-model="formData.companyName"
					placeholder="Input Company Name"
					class="text-sm min-w-[250px]"
				/>
			</el-form-item>

			<el-form-item
				label="Contact Name"
				prop="contactName"
				v-if="staticFields.includes('CONTACTNAME')"
			>
				<el-input
					v-model="formData.contactName"
					placeholder="Input Contact Name"
					class="text-sm min-w-[250px]"
				/>
			</el-form-item>

			<el-form-item
				label="Contact Email"
				prop="contactEmail"
				v-if="staticFields.includes('CONTACTEMAIL')"
			>
				<el-input
					v-model="formData.contactEmail"
					placeholder="Input Contact Email"
					class="text-sm min-w-[250px]"
				/>
			</el-form-item>

			<el-form-item
				label="Contact Phone"
				prop="contactPhone"
				v-if="staticFields.includes('CONTACTPHONE')"
			>
				<el-input
					v-model="formData.contactPhone"
					placeholder="Input Contact Phone"
					class="text-sm min-w-[250px]"
				/>
			</el-form-item>

			<el-form-item
				label="Life Cycle Stage"
				prop="lifeCycleStage"
				v-if="staticFields.includes('LIFECYCLESTAGE')"
			>
				<el-select
					v-model="formData.lifeCycleStage"
					placeholder="Select Life Cycle Stage"
					clearable
					class="text-sm min-w-[250px]"
				>
					<el-option
						v-for="stage in lifeCycleStage"
						:key="stage.id"
						:label="stage.name"
						:value="stage.id"
					/>
				</el-select>
			</el-form-item>

			<el-form-item label="Priority" prop="priority" v-if="staticFields.includes('PRIORITY')">
				<el-select
					v-model="formData.priority"
					placeholder="Select Priority"
					clearable
					class="text-sm min-w-[250px]"
				>
					<el-option label="Urgent" value="Urgent" />
					<el-option label="High" value="High" />
					<el-option label="Medium" value="Medium" />
					<el-option label="Low" value="Low" />
				</el-select>
			</el-form-item>

			<!-- Credit and Payment Info -->
			<el-form-item
				label="Requested Credit Limit"
				prop="requestedCreditLimit"
				v-if="staticFields.includes('REQUESTEDCREDITLIMIT')"
			>
				<InputNumber
					v-model="formData.requestedCreditLimit"
					:isFinancial="true"
					placeholder="Input Credit Limit"
					class="text-sm min-w-[250px]"
				/>
			</el-form-item>

			<el-form-item
				label="Approved Credit Limit"
				prop="approvedCreditLimit"
				v-if="staticFields.includes('APPROVEDCREDITLIMIT')"
			>
				<InputNumber
					v-model="formData.approvedCreditLimit"
					:isFinancial="true"
					placeholder="Input Credit Limit"
					class="text-sm min-w-[250px]"
				/>
			</el-form-item>

			<el-form-item
				label="Sales Approved Credit Limit"
				prop="salesApprovedCreditLimit"
				v-if="staticFields.includes('SALESAPPROVEDCREDITLIMIT')"
			>
				<InputNumber
					v-model="formData.salesApprovedCreditLimit"
					:isFinancial="true"
					placeholder="Input Sales Approved Credit Limit"
					class="text-sm min-w-[250px]"
				/>
			</el-form-item>

			<el-form-item
				label="Sales Approval Notes"
				prop="salesApprovalNotes"
				class="form-item full-width"
				v-if="staticFields.includes('SALESAPPROVALNOTES')"
			>
				<el-input
					v-model="formData.salesApprovalNotes"
					type="textarea"
					:rows="3"
					placeholder="Input Sales Approval Notes"
					:maxlength="textraTwoHundredLength"
					show-word-limit
					class="text-sm w-full"
				/>
			</el-form-item>

			<el-form-item
				label="Negative Balance Allowance (Prepaid)"
				prop="negativeBalanceAllowance"
				v-if="staticFields.includes('NEGATIVEBALANCEALLOWANCE')"
			>
				<InputNumber
					v-model="formData.negativeBalanceAllowance"
					:isFinancial="true"
					placeholder="Input Balance Allowance"
					class="text-sm min-w-[250px]"
				/>
			</el-form-item>

			<el-form-item
				label="Low Balance Notification (Prepaid)"
				prop="lowBalanceNotification"
				v-if="staticFields.includes('LOWBALANCENOTIFICATION')"
			>
				<InputNumber
					v-model="formData.lowBalanceNotification"
					:isFinancial="true"
					placeholder="Input Balance Notification"
					class="text-sm min-w-[250px]"
				/>
			</el-form-item>

			<el-form-item
				label="Payment Term"
				prop="paymentTerm"
				v-if="staticFields.includes('PAYMENTTERM')"
			>
				<el-select
					v-model="formData.paymentTerm"
					placeholder="Select Payment Term"
					clearable
					class="text-sm min-w-[250px]"
				>
					<el-option label="Net 30" value="Net 30" />
					<el-option label="Net 15" value="Net 15" />
					<el-option label="Net 45" value="Net 45" />
					<el-option label="Net 60" value="Net 60" />
					<el-option label="Prepaid" value="Prepaid" />
				</el-select>
			</el-form-item>

			<el-form-item
				label="Credit Score"
				prop="creditScore"
				v-if="staticFields.includes('CREDITSCORE')"
			>
				<el-input
					v-model="formData.creditScore"
					placeholder="Input Credit Score"
					class="text-sm min-w-[250px]"
				/>
			</el-form-item>

			<el-form-item
				label="Approval Notes"
				prop="approvalNotes"
				class="form-item full-width"
				v-if="staticFields.includes('APPROVALNOTES')"
			>
				<el-input
					v-model="formData.approvalNotes"
					type="textarea"
					:rows="4"
					placeholder="Input Approval Notes"
					:maxlength="textraTwoHundredLength"
					show-word-limit
					class="text-sm w-full"
				/>
			</el-form-item>

			<!-- Customer Account Info -->
			<el-form-item
				label="Customer Code"
				prop="customerCode"
				v-if="staticFields.includes('CUSTOMERCODE')"
			>
				<el-input
					v-model="formData.customerCode"
					placeholder="Input Customer Code"
					class="text-sm min-w-[250px]"
				/>
			</el-form-item>

			<el-form-item
				label="Customer Name"
				prop="customerName"
				v-if="staticFields.includes('CUSTOMERNAME')"
			>
				<el-input
					v-model="formData.customerName"
					placeholder="Input Customer Name"
					class="text-sm min-w-[250px]"
				/>
			</el-form-item>

			<el-form-item label="Status" prop="status" v-if="staticFields.includes('STATUS')">
				<el-select
					v-model="formData.status"
					placeholder="Select Status"
					class="text-sm min-w-[250px]"
				>
					<el-option label="Draft" value="Draft" />
					<el-option label="Active" value="Active" />
					<el-option label="Inactive" value="Inactive" />
				</el-select>
			</el-form-item>

			<!-- Account Holder Information -->

			<el-form-item
				label="Account Holder's Category"
				prop="accountHolderCategory"
				v-if="staticFields.includes('ACCOUNTHOLDERCATEGORY')"
			>
				<el-select
					v-model="formData.accountHolderCategory"
					placeholder="Select Category"
					clearable
					class="text-sm min-w-[250px]"
				>
					<el-option label="AR/Collector" value="AR/Collector" />
					<el-option label="CSR" value="CSR" />
					<el-option label="Sales" value="Sales" />
					<el-option label="Account Manager" value="Account Manager" />
					<el-option label="Warehouse Manager" value="Warehouse Manager" />
					<el-option label="Office Manager" value="Office Manager" />
				</el-select>
			</el-form-item>

			<el-form-item label="Assignee" prop="assignee" v-if="staticFields.includes('ASSIGNEE')">
				<el-select
					v-model="formData.assignee"
					placeholder="Select Assignee"
					clearable
					:loading="assigneeLoading"
					:disabled="!formData.accountHolderCategory"
					class="text-sm min-w-[250px]"
				>
					<el-option
						v-for="user in assigneeOptions"
						:key="user.id"
						:label="user.fullName"
						:value="user.id"
					/>
				</el-select>
			</el-form-item>

			<el-form-item
				label="Assignee's Email"
				prop="assigneeEmail"
				v-if="staticFields.includes('ASSIGNEEEMAIL')"
			>
				<el-input
					v-model="formData.assigneeEmail"
					placeholder="Assignee's Email"
					class="text-sm min-w-[250px]"
				/>
			</el-form-item>

			<el-form-item
				label="Assignee's Phone Number"
				prop="assigneePhone"
				v-if="staticFields.includes('ASSIGNEEPHONENUMBER')"
			>
				<el-input
					v-model="formData.assigneePhone"
					placeholder="Assignee's Phone Number"
					class="text-sm min-w-[250px]"
				/>
			</el-form-item>

			<el-form-item
				label="Assignee's Responsible Location"
				prop="assigneeLocation"
				v-if="staticFields.includes('ASSIGNEELOCATION')"
			>
				<el-select
					v-model="formData.assigneeLocation"
					placeholder="Select Location"
					clearable
					:loading="locationLoading"
					class="text-sm min-w-[250px]"
				>
					<el-option
						v-for="location in locationOptions"
						:key="location.id"
						:label="location.name"
						:value="location.id"
					/>
				</el-select>
			</el-form-item>

			<el-form-item
				label="Note"
				prop="accountHolderNote"
				class="form-item full-width"
				v-if="staticFields.includes('NOTE')"
			>
				<el-input
					v-model="formData.accountHolderNote"
					type="textarea"
					:rows="2"
					placeholder="Enter note"
					maxlength="100"
					show-word-limit
					class="text-sm w-full"
				/>
			</el-form-item>
		</el-form>
		<div class="flex justify-end space-x-2">
			<el-button @click="handleSave" :loading="saving">
				<el-icon class="mr-1"><Document /></el-icon>
				Save
			</el-button>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, watch } from 'vue';
import type { FormInstance, FormRules } from 'element-plus';
import InputNumber from '@/components/form/InputNumber/index.vue';
import { textraTwoHundredLength } from '@/settings/projectSetting';
import { saveQuestionnaireStatic } from '@/apis/ow/onboarding';
import { ElMessage } from 'element-plus';

const props = defineProps<{
	staticFields: string[]; // 需要显示的字段
	onboardingId: string;
	stageId: string;
}>();

// 表单引用
const formRef = ref<FormInstance>();

// Assignee相关状态
const assigneeLoading = ref(false);
const assigneeOptions = ref<Array<{ id: string; fullName: string; email: string; phone: string }>>(
	[]
);

// Location相关状态
const locationLoading = ref(false);
const locationOptions = ref<Array<{ id: string; name: string }>>([]);

// Life Cycle Stage options
const lifeCycleStage = ref<any[]>([]);
const getLifeCycleStage = async () => {
	lifeCycleStage.value = [
		{
			id: '1',
			name: 'Lead',
		},
		{
			id: '2',
			name: 'Qualified',
		},
		{
			id: '3',
			name: 'Proposal',
		},
		{
			id: '4',
			name: 'Negotiation',
		},
		{
			id: '5',
			name: 'Closed Won',
		},
	];
};

// 表单数据
const formData = reactive({
	// Lead Basic Info
	leadId: '',
	companyName: '',
	contactName: '',
	contactEmail: '',
	contactPhone: '',
	lifeCycleStage: '',
	priority: 'Medium', // 默认值为Medium

	// Credit and Payment Info
	requestedCreditLimit: '500.00', // 默认值$500.00
	approvedCreditLimit: '500.00', // 默认填入与Requested Credit Limit相同的数值
	salesApprovedCreditLimit: '', // 新增字段
	salesApprovalNotes: '', // 新增字段
	negativeBalanceAllowance: '0', // 默认值0
	lowBalanceNotification: '500.00', // 默认值$500.00
	paymentTerm: 'Net 30', // 默认值Net 30
	creditScore: '',
	approvalNotes: '',

	// Customer Account Info
	customerCode: '',
	customerName: '',
	status: 'Draft', // 默认状态

	// Account Holder Information
	accountHolderCategory: '',
	assignee: '',
	assigneeEmail: '',
	assigneePhone: '',
	assigneeLocation: '',
	accountHolderNote: '',
});

// 表单验证规则
const formRules: FormRules = {
	leadId: [{ required: true, message: 'Please select lead', trigger: 'change' }],
	companyName: [{ required: true, message: 'Please enter company name', trigger: 'blur' }],
	priority: [{ required: true, message: 'Please select priority', trigger: 'change' }],
	requestedCreditLimit: [
		{ required: true, message: 'Please enter requested credit limit', trigger: 'blur' },
	],
	approvedCreditLimit: [
		{ required: true, message: 'Please enter approved credit limit', trigger: 'blur' },
	],
	salesApprovedCreditLimit: [
		{ required: true, message: 'Please enter sales approved credit limit', trigger: 'blur' },
	],
	salesApprovalNotes: [
		{ required: true, message: 'Please enter sales approval notes', trigger: 'blur' },
		{
			max: textraTwoHundredLength,
			message: `Notes cannot exceed ${textraTwoHundredLength} characters`,
			trigger: 'blur',
		},
	],
	paymentTerm: [{ required: true, message: 'Please select payment term', trigger: 'change' }],
	status: [{ required: true, message: 'Please select status', trigger: 'change' }],

	// Account Holder Rules
	accountHolderCategory: [
		{ required: true, message: 'Please select account holder category', trigger: 'change' },
	],
	assignee: [{ required: true, message: 'Please select assignee', trigger: 'change' }],
	assigneeLocation: [{ required: false, message: 'Please select location', trigger: 'change' }],
	accountHolderNote: [
		{ max: 100, message: 'Note cannot exceed 100 characters', trigger: 'blur' },
	],
};

// 表单验证方法
const validateForm = () => {
	return new Promise((resolve) => {
		formRef.value?.validate((valid) => {
			resolve(valid);
		});
	});
};

// 获取表单数据 - 返回新的格式
const getFormData = () => {
	// 字段映射关系 - 从表单字段名映射到API字段名
	const formToApiFieldsMap = {
		leadId: 'LEADID',
		companyName: 'COMPANYNAME',
		contactName: 'CONTACTNAME',
		contactEmail: 'CONTACTEMAIL',
		contactPhone: 'CONTACTPHONE',
		lifeCycleStage: 'LIFECYCLESTAGE',
		priority: 'PRIORITY',
		requestedCreditLimit: 'REQUESTEDCREDITLIMIT',
		approvedCreditLimit: 'APPROVEDCREDITLIMIT',
		salesApprovedCreditLimit: 'SALESAPPROVEDCREDITLIMIT',
		salesApprovalNotes: 'SALESAPPROVALNOTES',
		negativeBalanceAllowance: 'NEGATIVEBALANCEALLOWANCE',
		lowBalanceNotification: 'LOWBALANCENOTIFICATION',
		paymentTerm: 'PAYMENTTERM',
		creditScore: 'CREDITSCORE',
		approvalNotes: 'APPROVALNOTES',
		customerCode: 'CUSTOMERCODE',
		customerName: 'CUSTOMERNAME',
		status: 'STATUS',
		accountHolderCategory: 'ACCOUNTHOLDERCATEGORY',
		assignee: 'ASSIGNEE',
		assigneeEmail: 'ASSIGNEEEMAIL',
		assigneePhone: 'ASSIGNEEPHONENUMBER',
		assigneeLocation: 'ASSIGNEELOCATION',
		accountHolderNote: 'NOTE',
	};

	// 字段类型映射
	const fieldTypeMap = {
		leadId: 'text',
		companyName: 'text',
		contactName: 'text',
		contactEmail: 'email',
		contactPhone: 'tel',
		lifeCycleStage: 'select',
		priority: 'select',
		requestedCreditLimit: 'number',
		approvedCreditLimit: 'number',
		salesApprovedCreditLimit: 'number',
		salesApprovalNotes: 'textarea',
		negativeBalanceAllowance: 'number',
		lowBalanceNotification: 'number',
		paymentTerm: 'select',
		creditScore: 'text',
		approvalNotes: 'textarea',
		customerCode: 'text',
		customerName: 'text',
		status: 'select',
		accountHolderCategory: 'select',
		assignee: 'select',
		assigneeEmail: 'email',
		assigneePhone: 'tel',
		assigneeLocation: 'select',
		accountHolderNote: 'textarea',
	};

	// 必填字段定义
	const requiredFields = new Set([
		'leadId',
		'companyName',
		'priority',
		'requestedCreditLimit',
		'approvedCreditLimit',
		'salesApprovedCreditLimit',
		'salesApprovalNotes',
		'paymentTerm',
		'status',
		'accountHolderCategory',
		'assignee',
		'assigneeLocation',
	]);

	const fieldLabelMap = {
		LEADID: 'Lead ID',
		COMPANYNAME: 'Lead Company Name',
		CONTACTNAME: 'Contact Name',
		CONTACTEMAIL: 'Contact Email',
		CONTACTPHONE: 'Contact Phone',
		LIFECYCLESTAGE: 'Life Cycle Stage',
		PRIORITY: 'Priority',
		REQUESTEDCREDITLIMIT: 'Requested Credit Limit',
		APPROVEDCREDITLIMIT: 'Approved Credit Limit',
		SALESAPPROVEDCREDITLIMIT: 'Sales Approved Credit Limit',
		SALESAPPROVALNOTES: 'Sales Approval Notes',
		NEGATIVEBALANCEALLOWANCE: 'Negative Balance Allowance (Prepaid)',
		LOWBALANCENOTIFICATION: 'Low Balance Notification (Prepaid)',
		PAYMENTTERM: 'Payment Term',
		CREDITSCORE: 'Credit Score',
		APPROVALNOTES: 'Approval Notes',
		CUSTOMERCODE: 'Customer Code',
		CUSTOMERNAME: 'Customer Name',
		STATUS: 'Status',
		ACCOUNTHOLDERCATEGORY: "Account Holder's Category",
		ASSIGNEE: 'Assignee',
		ASSIGNEEEMAIL: "Assignee's Email",
		ASSIGNEEPHONENUMBER: "Assignee's Phone Number",
		ASSIGNEELOCATION: "Assignee's Responsible Location",
		NOTE: 'Note',
	};

	const result: Array<{
		fieldName: string;
		fieldValueJson: any;
		fieldType: string;
		isRequired: boolean;
		fieldLabel: string;
	}> = [];

	// 只处理在 staticFields 中指定的字段
	props.staticFields.forEach((apiFieldName) => {
		// 找到对应的表单字段名
		const formFieldName = Object.keys(formToApiFieldsMap).find(
			(key) => formToApiFieldsMap[key] === apiFieldName
		);

		if (formFieldName && formData[formFieldName] !== undefined) {
			console.log('formData[formFieldName]:', formData[formFieldName]);
			result.push({
				fieldName: apiFieldName,
				fieldValueJson:
					typeof formData[formFieldName] === 'string'
						? formData[formFieldName]
						: JSON.stringify(formData[formFieldName]),
				fieldType: fieldTypeMap[formFieldName] || 'text',
				isRequired: requiredFields.has(formFieldName),
				fieldLabel: fieldLabelMap[apiFieldName] || apiFieldName,
			});
		}
	});

	return result;
};

// 设置表单字段值的方法
const setFieldValues = (fieldValues: Record<string, any> | any[]) => {
	// 处理新的数组格式
	if (Array.isArray(fieldValues)) {
		const convertedValues = {};
		fieldValues.forEach((field) => {
			if (field.fieldName && field.fieldValueJson !== undefined) {
				try {
					// fieldValueJson 是一个 JSON 字符串，需要解析
					convertedValues[field.fieldName] = field.fieldValueJson;
				} catch (error) {
					// 如果解析失败，直接使用原值
					convertedValues[field.fieldName] = field.fieldValueJson;
				}
			}
		});
		setFieldValues(convertedValues);
		return;
	}

	if (!fieldValues || typeof fieldValues !== 'object') return;

	// 字段映射关系
	const fieldsMap = {
		// 原有的大写格式
		LEADID: 'leadId',
		COMPANYNAME: 'companyName',
		CONTACTNAME: 'contactName',
		CONTACTEMAIL: 'contactEmail',
		CONTACTPHONE: 'contactPhone',
		LIFECYCLESTAGE: 'lifeCycleStage',
		PRIORITY: 'priority',
		REQUESTEDCREDITLIMIT: 'requestedCreditLimit',
		APPROVEDCREDITLIMIT: 'approvedCreditLimit',
		SALESAPPROVEDCREDITLIMIT: 'salesApprovedCreditLimit',
		SALESAPPROVALNOTES: 'salesApprovalNotes',
		NEGATIVEBALANCEALLOWANCE: 'negativeBalanceAllowance',
		LOWBALANCENOTIFICATION: 'lowBalanceNotification',
		PAYMENTTERM: 'paymentTerm',
		CREDITSCORE: 'creditScore',
		APPROVALNOTES: 'approvalNotes',
		CUSTOMERCODE: 'customerCode',
		CUSTOMERNAME: 'customerName',
		STATUS: 'status',
		ACCOUNTHOLDERCATEGORY: 'accountHolderCategory',
		ASSIGNEE: 'assignee',
		ASSIGNEEEMAIL: 'assigneeEmail',
		ASSIGNEEPHONENUMBER: 'assigneePhone',
		ASSIGNEELOCATION: 'assigneeLocation',
		NOTE: 'accountHolderNote',
	};

	// 遍历字段值并设置到表单数据中
	Object.keys(fieldValues).forEach((fieldKey) => {
		const propName = fieldsMap[fieldKey];
		if (propName && Object.prototype.hasOwnProperty.call(formData, propName)) {
			formData[propName] = JSON.parse(fieldValues[fieldKey]);
		}
	});
};

// 暴露给父组件的方法
defineExpose({
	validateForm,
	getFormData,
	setFieldValues,
});

// 监听Requested Credit Limit变化，自动更新Approved Credit Limit
watch(
	() => formData.requestedCreditLimit,
	(newValue) => {
		if (newValue && !formData.approvedCreditLimit) {
			formData.approvedCreditLimit = newValue;
		}
	}
);

const saving = ref(false);
const handleSave = async () => {
	try {
		saving.value = true;

		// 验证静态表单
		const staticFormValid = await validateForm();
		if (!staticFormValid || !props?.onboardingId) {
			return;
		}

		const staticFormData = getFormData();
		const res = await saveQuestionnaireStatic({
			fieldValues: staticFormData,
			onboardingId: props?.onboardingId,
			stageId: props.stageId,
		});
		if (res.code == '200') {
			return true;
		} else {
			ElMessage.error(res.msg || 'Failed to save stage data');
			return false;
		}
	} catch {
		return false;
	} finally {
		saving.value = false;
	}
};

onMounted(() => {
	getLifeCycleStage();
	console.log('static form');
});
</script>

<style lang="scss" scoped>
.form-grid {
	display: grid;
	grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
	gap: 16px;
	width: 100%;

	// 对于需要占满整行的元素
	.full-width {
		grid-column: 1 / -1;
	}

	// 确保表单项内部元素也是100%宽度
	.form-item {
		width: 100%;
		:deep(.el-select),
		:deep(.el-input),
		:deep(.el-input-number) {
			width: 100%;
		}
	}

	:deep(.el-form-item) {
		margin-bottom: 0; // 移除默认的底部间距，因为我们使用gap来控制间距

		.el-form-item__label {
			font-weight: 500;
			color: #374151;
			margin-bottom: 4px;
		}
	}
}
</style>
