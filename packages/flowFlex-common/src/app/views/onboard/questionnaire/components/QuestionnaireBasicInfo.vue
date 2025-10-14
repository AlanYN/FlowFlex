<template>
	<div class="config-section">
		<h3 class="section-title">Basic Information</h3>
		<el-form :model="questionnaire" label-position="top" @submit.prevent>
			<el-form-item label="Questionnaire Title" required>
				<el-input
					:model-value="questionnaire.name"
					placeholder="Enter questionnaire title"
					@input="updateName"
				/>
			</el-form-item>
			<el-form-item label="Description">
				<el-input
					:model-value="questionnaire.description"
					type="textarea"
					:rows="3"
					placeholder="Enter questionnaire description"
					@input="updateDescription"
				/>
			</el-form-item>
		</el-form>
	</div>
</template>

<script setup lang="ts">
interface QuestionnaireBasicInfo {
	name: string;
	description: string;
}

interface Props {
	questionnaire: QuestionnaireBasicInfo;
}

const props = defineProps<Props>();

const emits = defineEmits<{
	'update-questionnaire': [questionnaire: QuestionnaireBasicInfo];
}>();

const updateName = (value: string) => {
	emits('update-questionnaire', { ...props.questionnaire, name: value });
};

const updateDescription = (value: string) => {
	emits('update-questionnaire', { ...props.questionnaire, description: value });
};
</script>

<style scoped lang="scss">
.config-section {
	margin-bottom: 1.5rem;
}

.section-title {
	font-size: 1.125rem;
	font-weight: 600;
	color: var(--primary-700);
	margin: 0 0 1rem 0;
}

/* 深色模式支持 */
.dark .section-title {
	color: var(--primary-200);
}
</style>
