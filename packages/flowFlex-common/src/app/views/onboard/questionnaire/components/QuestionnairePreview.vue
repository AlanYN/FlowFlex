<template>
	<el-dialog
		v-model="dialogVisible"
		:title="dialogTitle"
		:width="bigDialogWidth"
		:before-close="handleClose"
		class="questionnaire-preview-dialog"
		destroy-on-close
		draggable
	>
		<template #header>
			<div class="flex flex-col gap-1">
				<span class="text-xl font-semibold text-primary-500">Questionnaire Preview</span>
				<span class="text-sm preview-subtitle">
					This is how the questionnaire will appear to users.
				</span>
			</div>
		</template>
		<el-scrollbar max-height="70vh">
			<PreviewContent
				:questionnaire="questionnaire"
				:loading="loading"
				:workflows="workflows"
				:all-stages="allStages"
			/>
		</el-scrollbar>

		<template #footer>
			<div class="flex justify-end w-full">
				<el-button @click="handleClose" type="primary">Close Preview</el-button>
			</div>
		</template>
	</el-dialog>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { ElMessage, ElScrollbar } from 'element-plus';
import { getQuestionnaireDetail } from '@/apis/ow/questionnaire';
import { bigDialogWidth } from '@/settings/projectSetting';
import PreviewContent from './PreviewContent.vue';
import { Workflow } from '#/onboard';

// 定义组件属性
interface Props {
	questionnaireId?: string;
	questionnaireData?: any;
	visible?: boolean;
	workflows: Workflow[];
	allStages: any[];
}

const props = withDefaults(defineProps<Props>(), {
	questionnaireId: '',
	questionnaireData: null,
	visible: false,
});

// 定义事件
const emit = defineEmits<{
	'update:visible': [value: boolean];
}>();

// 响应式数据
const questionnaire = ref<any>(null);
const loading = ref(false);

// 计算属性
const dialogVisible = computed({
	get: () => props.visible,
	set: (value: boolean) => emit('update:visible', value),
});

const dialogTitle = computed(() => {
	if (loading.value) return 'Loading Preview...';
	return 'Questionnaire Preview';
});

// 只监听弹窗显示状态，在打开时设置数据
watch(
	() => props.visible,
	async (visible) => {
		if (visible) {
			// 弹窗打开时设置数据
			if (props.questionnaireData) {
				// 优先使用传入的数据
				questionnaire.value = props.questionnaireData;
			} else if (props.questionnaireId) {
				// 如果没有传入数据但有ID，则调用API获取
				await fetchQuestionnairePreview(props.questionnaireId);
			}
		}
	}
);

// 获取问卷预览数据
const fetchQuestionnairePreview = async (id: string) => {
	if (!id) return;

	try {
		loading.value = true;
		const response = await getQuestionnaireDetail(id);
		if (response.success && response.data) {
			questionnaire.value = response.data;
		} else {
			ElMessage.error('Failed to load questionnaire preview');
		}
	} catch (error) {
		console.error('Error fetching questionnaire preview:', error);
		ElMessage.error('Error loading questionnaire preview');
	} finally {
		loading.value = false;
	}
};

// 关闭对话框
const handleClose = () => {
	dialogVisible.value = false;
	questionnaire.value = null;
};
</script>

<style scoped lang="scss">
.preview-subtitle {
	color: var(--el-text-color-secondary);
}

.questionnaire-preview-dialog {
	:deep(.el-dialog__body) {
		padding: 0;
	}

	:deep(.el-dialog__footer) {
		padding: 16px 20px;
		border-top: 1px solid var(--primary-100);
		@apply dark:border-black-200;
	}
}
</style>
