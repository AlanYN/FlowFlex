<template>
	<div class="condition-fallback-form">
		<div class="fallback-description">
			<el-icon class="info-icon"><InfoFilled /></el-icon>
			<span>Fallback determines what happens when the condition rules are not met.</span>
		</div>

		<el-form-item label="Fallback Behavior" class="fallback-field">
			<el-radio-group :model-value="modelValue.type" @change="onTypeChange">
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

		<el-form-item v-if="modelValue.type === 'specified'" label="Fallback Stage">
			<el-select
				:model-value="modelValue.fallbackStageId"
				placeholder="Select fallback stage"
				@update:model-value="handleStageChange"
			>
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

// 可选的 Stage（当前之后的 Stage）
const availableStages = computed(() => {
	return props.stages.slice(props.currentStageIndex + 1);
});

// 处理类型变化
const onTypeChange = (val: string | number | boolean | undefined) => {
	if (val === 'default' || val === 'specified') {
		emit('update:modelValue', {
			type: val,
			fallbackStageId: val === 'default' ? undefined : props.modelValue.fallbackStageId,
		});
	}
};

// 处理 Stage 变化
const handleStageChange = (val: string) => {
	emit('update:modelValue', {
		...props.modelValue,
		fallbackStageId: val,
	});
};
</script>

<style lang="scss" scoped>
.condition-fallback-form {
	@apply flex flex-col gap-4;
}

.fallback-description {
	@apply flex items-start gap-2 p-3 rounded-lg text-sm bg-black-400;
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
