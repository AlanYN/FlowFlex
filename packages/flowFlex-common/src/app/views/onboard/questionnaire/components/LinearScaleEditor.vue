<template>
	<div class="linear-scale-editor">
		<div class="linear-scale-section">
			<div class="linear-scale-header">
				<label class="linear-scale-label">Linear Scale Configuration</label>
			</div>

			<!-- 范围配置 -->
			<div class="scale-range-config">
				<div class="range-selectors">
					<div class="range-item">
						<label class="range-label">From</label>
						<el-select
							:model-value="min"
							placeholder="Select minimum"
							class="range-select"
							@change="updateMin"
						>
							<el-option
								v-for="num in [0, 1]"
								:key="num"
								:label="num.toString()"
								:value="num"
							/>
						</el-select>
					</div>
					<div class="range-separator">to</div>
					<div class="range-item">
						<label class="range-label">To</label>
						<el-select
							:model-value="max"
							placeholder="Select maximum"
							class="range-select"
							@change="updateMax"
						>
							<el-option
								v-for="num in [2, 3, 4, 5, 6, 7, 8, 9, 10]"
								:key="num"
								:label="num.toString()"
								:value="num"
							/>
						</el-select>
					</div>
				</div>
			</div>

			<!-- 标签配置 -->
			<div class="scale-labels-config">
				<div class="labels-grid">
					<div class="label-item">
						<label class="label-title">{{ min }}</label>
						<el-input
							:model-value="minLabel"
							placeholder="Left label (optional)"
							class="label-input"
							@input="updateMinLabel"
						/>
					</div>
					<div class="label-item">
						<label class="label-title">{{ max }}</label>
						<el-input
							:model-value="maxLabel"
							placeholder="Right label (optional)"
							class="label-input"
							@input="updateMaxLabel"
						/>
					</div>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
interface Props {
	min: number;
	max: number;
	minLabel: string;
	maxLabel: string;
}

defineProps<Props>();

const emits = defineEmits<{
	'update-min': [value: number];
	'update-max': [value: number];
	'update-min-label': [label: string];
	'update-max-label': [label: string];
}>();

const updateMin = (value: number) => {
	emits('update-min', value);
};

const updateMax = (value: number) => {
	emits('update-max', value);
};

const updateMinLabel = (label: string) => {
	emits('update-min-label', label);
};

const updateMaxLabel = (label: string) => {
	emits('update-max-label', label);
};
</script>

<style scoped lang="scss">
.linear-scale-editor {
	margin-top: 1.5rem;
	padding: 1rem;
	border: 1px solid var(--primary-200);
	background-color: var(--primary-25);
	@apply dark:bg-primary-700 dark:border-primary-600 rounded-xl;
}

.linear-scale-section {
	display: flex;
	flex-direction: column;
	gap: 1rem;
}

.linear-scale-header {
	margin-bottom: 0.5rem;
}

.linear-scale-label {
	font-size: 0.875rem;
	font-weight: 600;
	color: var(--primary-800);
	@apply dark:text-primary-200;
}

.scale-range-config {
	margin-bottom: 1rem;
}

.range-selectors {
	display: flex;
	align-items: center;
	gap: 1rem;
}

.range-item {
	display: flex;
	flex-direction: column;
	gap: 0.5rem;
	min-width: 100px;
}

.range-label {
	font-size: 0.875rem;
	font-weight: 500;
	color: var(--primary-700);
	@apply dark:text-primary-300;
}

.range-select {
	width: 100%;
}

.range-separator {
	font-size: 0.875rem;
	color: var(--primary-600);
	margin-top: 1.5rem;
	@apply dark:text-primary-400;
}

.scale-labels-config {
	border-top: 1px solid var(--primary-100);
	padding-top: 1rem;
	@apply dark:border-primary-600;
}

.labels-grid {
	display: grid;
	grid-template-columns: 1fr 1fr;
	gap: 1rem;
}

.label-item {
	display: flex;
	flex-direction: column;
	gap: 0.5rem;
}

.label-title {
	font-size: 0.875rem;
	font-weight: 500;
	color: var(--primary-700);
	text-align: center;
	@apply dark:text-primary-300;
}

.label-input {
	width: 100%;
}
</style>
