<template>
	<div class="condition-fallback-form">
		<div class="fallback-description">
			<el-icon class="info-icon"><InfoFilled /></el-icon>
			<span>Fallback determines what happens when the condition rules are not met.</span>
		</div>

		<el-form-item label="Fallback Behavior" class="fallback-field">
			<el-radio-group v-model="fallbackType">
				<el-radio value="default">
					<div class="option-content">
						<span class="option-label">Continue to next stage</span>
						<span class="option-desc">Proceed to the next stage in order</span>
					</div>
				</el-radio>
				<el-radio value="specified">
					<div class="option-content">
						<span class="option-label">Go to specific stage</span>
						<span class="option-desc">Jump to a designated stage</span>
					</div>
				</el-radio>
			</el-radio-group>
		</el-form-item>

		<el-form-item v-if="fallbackType === 'specified'" label="Fallback Stage">
			<el-select v-model="fallbackStageId" placeholder="Select fallback stage">
				<el-option
					v-for="stage in availableStages"
					:key="stage.id"
					:label="stage.name"
					:value="stage.id"
				/>
			</el-select>
		</el-form-item>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { InfoFilled } from '@element-plus/icons-vue';
import type { FallbackConfig } from '#/condition';
import type { Stage } from '#/onboard';

// Props
const props = defineProps<{
	modelValue: FallbackConfig;
	stages: Stage[];
	currentStageIndex: number;
}>();

// Emits
const emit = defineEmits<{
	(e: 'update:modelValue', value: FallbackConfig): void;
}>();

// 计算属性处理 fallback type
const fallbackType = computed({
	get: () => props.modelValue.type,
	set: (val: 'default' | 'specified') => {
		emit('update:modelValue', { ...props.modelValue, type: val });
	},
});

// 计算属性处理 fallback stage id
const fallbackStageId = computed({
	get: () => props.modelValue.fallbackStageId,
	set: (val: string | undefined) => {
		emit('update:modelValue', { ...props.modelValue, fallbackStageId: val });
	},
});

// 可选的 Stage（当前之后的 Stage）
const availableStages = computed(() => {
	return props.stages.slice(props.currentStageIndex + 1);
});
</script>

<style lang="scss" scoped>
.condition-fallback-form {
	@apply flex flex-col gap-4;
}

.fallback-description {
	@apply flex items-start gap-2 p-3 rounded-lg text-sm;
	background-color: var(--el-fill-color-lighter);
	color: var(--el-text-color-regular);
}

.info-icon {
	@apply flex-shrink-0 mt-0.5;
	color: var(--el-color-info);
}

.option-content {
	@apply flex flex-col;
}

.option-label {
	@apply text-sm font-medium;
	color: var(--el-text-color-primary);
}

.option-desc {
	@apply text-xs;
	color: var(--el-text-color-secondary);
}
</style>
