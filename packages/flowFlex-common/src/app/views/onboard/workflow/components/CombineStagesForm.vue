<template>
	<div class="combine-stages-form">
		<div class="selected-stages">
			<h4>Selected Stages to Combine:</h4>
			<ul>
				<li v-for="stage in selectedStages" :key="stage.id">
					{{ stage.name }}
				</li>
			</ul>
		</div>

		<el-form
			ref="formRef"
			:model="formData"
			:rules="rules"
			label-position="top"
			@submit.prevent="submitForm"
		>
			<el-form-item label="New Stage Name" prop="name">
				<el-input v-model="formData.name" placeholder="Enter new stage name" />
			</el-form-item>

			<el-form-item label="Description" prop="description">
				<el-input
					v-model="formData.description"
					type="textarea"
					:rows="3"
					placeholder="Enter stage description"
				/>
			</el-form-item>

			<el-form-item label="Assigned User Group" prop="defaultAssignedGroup">
				<el-select
					v-model="formData.defaultAssignedGroup"
					placeholder="Select user group"
					style="width: 100%"
				
				:teleported="false">
					<el-option label="Account Management" value="Account Management" />
					<el-option label="Sales" value="Sales" />
					<el-option label="Customer" value="Customer" />
					<el-option label="Legal" value="Legal" />
					<el-option label="IT" value="IT" />
					<!-- 实际应用中这里会从API获取 -->
				</el-select>
				<div class="help-text">Select the group responsible for this combined stage</div>
			</el-form-item>

			<el-form-item label="Default Assignee" prop="defaultAssignee">
				<el-input
					v-model="formData.defaultAssignee"
					placeholder="Enter default assignee role"
				/>
				<div class="help-text">
					Enter the role or title of the person who will be assigned this stage by default
				</div>
			</el-form-item>

			<el-form-item label="Estimated Duration" prop="estimatedDuration">
				<el-input-number
					v-model="formData.estimatedDuration"
					:min="1"
					:max="30"
					style="width: 100%"
					placeholder="e.g., 3 days"
				/>
				<div class="help-text">
					Enter the expected number of days to complete this stage
				</div>
			</el-form-item>

			<el-form-item label="Stage Color" prop="color">
				<div class="color-picker-container">
					<div class="color-grid">
						<div
							v-for="color in colorOptions"
							:key="color"
							class="color-option"
							:class="{ selected: formData.color === color }"
							:style="{ backgroundColor: color }"
							@click="formData.color = color"
						></div>
					</div>
				</div>
			</el-form-item>

			<div class="form-actions">
				<el-button @click="$emit('cancel')">Cancel</el-button>
				<el-button
					type="primary"
					native-type="submit"
					:class="{
						'disabled-btn':
							!formData.name ||
							!formData.defaultAssignedGroup ||
							!formData.estimatedDuration,
					}"
				>
					Combine Stages
				</el-button>
			</div>
		</el-form>
	</div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed, PropType } from 'vue';
import type { FormInstance, FormRules } from 'element-plus';

// 接口定义
interface Stage {
	id: string;
	name: string;
	description?: string;
	defaultAssignedGroup: string;
	defaultAssignee: string;
	estimatedDuration: number;
	requiredFields: string[];
	order: number;
	color?: string;
}

// 颜色选项
const colorOptions = [
	'#4F46E5', // Indigo
	'#0EA5E9', // Sky
	'#10B981', // Emerald
	'#F59E0B', // Amber
	'#EC4899', // Pink
	'#8B5CF6', // Violet
	'#06B6D4', // Cyan
	'#14B8A6', // Teal
	'#F43F5E', // Rose
	'#22C55E', // Green
	'#3B82F6', // Blue
	'#A855F7', // Purple
];

// Props
const props = defineProps({
	selectedStages: {
		type: Array as PropType<Stage[]>,
		required: true,
	},
	workflowId: {
		type: String,
		required: true,
	},
});

// 表单数据
const formData = reactive({
	name: '',
	description: '',
	defaultAssignedGroup: '',
	defaultAssignee: '',
	estimatedDuration: 1,
	color: colorOptions[Math.floor(Math.random() * colorOptions.length)], // 随机默认颜色
});

// 表单验证规则 - 与StageForm保持一致
const rules = reactive<FormRules>({
	name: [
		{ required: true, message: 'Please enter stage name', trigger: 'blur' },
		{ min: 3, max: 50, message: 'Length should be 3 to 50 characters', trigger: 'blur' },
	],
	defaultAssignedGroup: [
		{ required: true, message: 'Please select user group', trigger: 'change' },
	],
	estimatedDuration: [
		{ required: true, message: 'Please enter estimated duration', trigger: 'change' },
	],
});

// 表单引用
const formRef = ref<FormInstance>();

// 计算总估计时间
const totalEstimatedDuration = computed(() => {
	return props.selectedStages.reduce((total, stage) => total + stage.estimatedDuration, 0);
});

// 初始化表单数据
onMounted(() => {
	if (props.selectedStages.length > 0) {
		// 使用第一个选中的阶段作为默认值
		const firstStage = props.selectedStages[0];
		formData.name = `Combined: ${firstStage.name}`;
		formData.description = firstStage.description || '';
		formData.defaultAssignedGroup = firstStage.defaultAssignedGroup;
		formData.defaultAssignee = firstStage.defaultAssignee;
		formData.estimatedDuration = totalEstimatedDuration.value;
		// 使用第一个阶段的颜色或选择随机颜色
		formData.color =
			firstStage.color || colorOptions[Math.floor(Math.random() * colorOptions.length)];
	} else {
		// 选择随机颜色
		formData.color = colorOptions[Math.floor(Math.random() * colorOptions.length)];
	}
});

// 提交表单
const submitForm = async () => {
	if (!formRef.value) return;

	await formRef.value.validate((valid, fields) => {
		if (valid) {
			emit('submit', { ...formData });
		}
	});
};

// 定义事件
const emit = defineEmits(['submit', 'cancel']);
</script>

<style scoped>
.combine-stages-form {
	padding: 10px;
}

.selected-stages {
	margin-bottom: 20px;
	padding: 10px;
	background-color: #f5f7fa;
	border-radius: 4px;
}

.selected-stages h4 {
	margin-top: 0;
	margin-bottom: 10px;
	color: #606266;
}

.selected-stages ul {
	list-style-type: none;
	padding-left: 10px;
	margin: 0;
}

.selected-stages li {
	padding: 5px 0;
	color: #303133;
}

.form-actions {
	display: flex;
	justify-content: flex-end;
	gap: 10px;
	margin-top: 20px;
}

.help-text {
	font-size: 12px;
	color: #909399;
	margin-top: 4px;
	font-style: italic;
}

.color-picker-container {
	width: 100%;
}

.color-grid {
	display: grid;
	grid-template-columns: repeat(6, 1fr);
	gap: 8px;
	margin-top: 4px;
}

.color-option {
	width: 32px;
	height: 32px;
	border-radius: 50%;
	cursor: pointer;
	transition: all 0.2s;
	border: 2px solid transparent;
}

.color-option:hover {
	transform: scale(1.1);
}

.color-option.selected {
	border-color: #333;
	transform: scale(1.1);
}

.disabled-btn {
	opacity: 0.6;
	cursor: not-allowed;
}
</style>
