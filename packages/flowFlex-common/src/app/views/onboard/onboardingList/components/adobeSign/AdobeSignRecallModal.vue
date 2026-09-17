<template>
	<el-dialog
		v-model="visible"
		title="Recall Signature Request"
		width="440px"
		draggable
		:append-to-body="true"
		:destroy-on-close="true"
		:close-on-click-modal="false"
		:before-close="handleClose"
	>
		<div class="space-y-3">
			<div
				class="flex items-start gap-2 p-3 bg-amber-50 dark:bg-amber-900/20 rounded-lg border border-amber-200 dark:border-amber-700"
			>
				<el-icon class="text-amber-500 mt-0.5 flex-shrink-0"><WarningFilled /></el-icon>
				<p class="text-sm text-amber-700 dark:text-amber-300">
					Are you sure you want to recall this request?
				</p>
			</div>

			<div class="flex items-center gap-2 p-3 bg-gray-50 dark:bg-black-400 rounded-lg">
				<el-icon class="text-gray-500"><Document /></el-icon>
				<span class="text-sm font-medium truncate">{{ fileName }}</span>
			</div>

			<div class="text-sm text-gray-600 dark:text-gray-400 space-y-1">
				<p class="font-medium text-gray-700 dark:text-gray-300">If recalled:</p>
				<ul class="list-disc list-inside space-y-1 text-gray-500">
					<li>All signers will no longer be able to sign</li>
					<li>Completed signatures will be invalidated</li>
					<li>This action cannot be undone</li>
				</ul>
			</div>
		</div>

		<template #footer>
			<el-button @click="handleClose">Cancel</el-button>
			<el-button type="danger" :loading="submitting" @click="handleRecall">Recall</el-button>
		</template>
	</el-dialog>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { Document, WarningFilled } from '@element-plus/icons-vue';
import { ElMessage } from 'element-plus';
import { recallAgreement } from '@/apis/ow/adobeSign';

const emit = defineEmits<{
	recalled: [];
}>();

const visible = ref(false);
const agreementId = ref<string | number>('');
const fileName = ref('');
const submitting = ref(false);

const open = (params: { agreementId: string | number; fileName: string }) => {
	agreementId.value = params.agreementId;
	fileName.value = params.fileName;
	visible.value = true;
};

defineExpose({ open });

const handleClose = () => {
	agreementId.value = '';
	fileName.value = '';
	submitting.value = false;
	visible.value = false;
};

const handleRecall = async () => {
	submitting.value = true;
	try {
		const res = await recallAgreement(agreementId.value);
		if (res.code == 200) {
			ElMessage.success('Signature request recalled');
			emit('recalled');
			visible.value = false;
		} else {
			res?.msg && ElMessage.error(res.msg);
		}
	} finally {
		submitting.value = false;
	}
};
</script>
