<template>
	<el-dialog v-model="visible" title="Code Generator" width="800px">
		<div>
			<!-- Model Selection -->
			<div class="flex items-center gap-2 mb-4">
				<el-icon class="text-blue-500"><Star /></el-icon>
				<span class="font-medium text-gray-900">Model:</span>
				<el-select
					v-model="selectedModelId"
					placeholder="Select AI Model"
					clearable
					style="width: 260px"
					@change="onModelChange"
					:disabled="loadingModels"
				>
					<el-option
						v-for="model in modelOptions"
						:key="model.id"
						:label="getModelDisplayName(model)"
						:value="String(model.id)"
					/>
				</el-select>
			</div>

			<!-- Content Area -->
			<div class="flex flex-col lg:flex-row gap-4 lg:h-96">
				<!-- Left: Instructions -->
				<div class="flex-1 flex flex-col h-80 lg:h-auto">
					<div class="font-medium text-gray-900 mb-2">Instructions</div>
					<div class="flex-1 border-gray-200 rounded overflow-hidden">
						<el-input
							v-model="aiInstructions"
							type="textarea"
							:rows="10"
							placeholder="Please enter a detailed description of the code you want to generate."
							class="h-full instructions-textarea"
						/>
					</div>
				</div>

				<!-- Right: Preview -->
				<div class="flex-1 flex flex-col h-80 lg:h-auto">
					<div class="font-medium text-gray-900 mb-2">Code Preview</div>
					<div
						class="flex-1 bg-gray-50 border border-gray-200 rounded p-3 overflow-auto"
						v-loading="generating"
					>
						<div
							v-if="!generatedCode && !generating"
							class="flex flex-col items-center justify-center h-full text-gray-500 text-center"
						>
							<el-icon size="48" color="#909399"><Star /></el-icon>
							<p class="mt-2 text-sm">
								Describe your use case on the left, and the code preview will be
								displayed here.
							</p>
						</div>
						<pre
							v-else-if="generatedCode"
							class="font-mono text-xs text-gray-800 whitespace-pre-wrap break-words m-0"
							>{{ generatedCode }}</pre
						>
					</div>
				</div>
			</div>
		</div>

		<template #footer>
			<div class="flex justify-between">
				<div>
					<el-button v-if="generatedCode" @click="clearGeneratedCode">Clear</el-button>
				</div>
				<div class="flex gap-2">
					<el-button v-if="generatedCode" type="success" @click="applyGeneratedCode">
						Apply Code
					</el-button>
					<el-button
						type="primary"
						@click="generateCode"
						:loading="generating"
						:icon="Star"
						:disabled="!selectedModelId"
					>
						Generate
					</el-button>
				</div>
			</div>
		</template>
	</el-dialog>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { Star } from '@element-plus/icons-vue';

interface Props {
	modelValue: boolean;
	selectedModelId: string;
	modelOptions: any[];
	loadingModels: boolean;
	aiInstructions: string;
	generatedCode: string;
	generating: boolean;
}

const props = defineProps<Props>();
const emit = defineEmits<{
	'update:modelValue': [value: boolean];
	'update:selectedModelId': [value: string];
	'update:aiInstructions': [value: string];
	'update:generatedCode': [value: string];
	'generate-code': [];
	'apply-code': [];
	'clear-code': [];
	'model-change': [value: string];
}>();

// Computed properties for two-way binding
const visible = computed({
	get: () => props.modelValue,
	set: (value) => emit('update:modelValue', value),
});

const selectedModelId = computed({
	get: () => props.selectedModelId,
	set: (value) => emit('update:selectedModelId', value),
});

const aiInstructions = computed({
	get: () => props.aiInstructions,
	set: (value) => emit('update:aiInstructions', value),
});

const generatedCode = computed({
	get: () => props.generatedCode,
	set: (value) => emit('update:generatedCode', value),
});

// Method proxies
const generateCode = () => emit('generate-code');
const applyGeneratedCode = () => emit('apply-code');
const clearGeneratedCode = () => emit('clear-code');
const onModelChange = (value: string) => emit('model-change', value);

// Get model display name
const getModelDisplayName = (model: any) => {
	return `${model.name} (${model.provider})`;
};
</script>

<style scoped lang="scss">
.instructions-textarea {
	:deep(.el-textarea__inner) {
		height: 100% !important;
		min-height: 100% !important;
	}
}
</style>
