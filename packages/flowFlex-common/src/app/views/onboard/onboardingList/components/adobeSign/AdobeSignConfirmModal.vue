<template>
	<el-dialog
		v-model="visible"
		title="Confirm Signature Request"
		width="480px"
		draggable
		:append-to-body="true"
		:destroy-on-close="true"
		:close-on-click-modal="false"
		:before-close="handleClose"
	>
		<div class="space-y-3 text-sm">
			<div class="flex items-center gap-2 p-3 bg-gray-50 dark:bg-black-400 rounded-lg">
				<el-icon class="text-gray-500"><Document /></el-icon>
				<span class="font-medium truncate">{{ fileName }}</span>
			</div>

			<div>
				<p class="text-gray-500 mb-1">Signers:</p>
				<ol class="list-decimal list-inside space-y-1">
					<li
						v-for="(signer, i) in requestData?.signers"
						:key="i"
						class="text-gray-700 dark:text-gray-300"
					>
						{{ signer.name }} ({{ signer.email }})
					</li>
				</ol>
			</div>

			<div class="text-gray-600 dark:text-gray-400 space-y-1">
				<p>
					Signing Order:
					<span class="font-medium">{{ requestData?.signingOrder }}</span>
				</p>
				<p>
					Expires in:
					<span class="font-medium">{{ requestData?.expirationDays }} days</span>
				</p>
			</div>

			<div
				class="flex items-center gap-1.5 text-xs text-[var(--el-color-warning)] border-t border-[var(--el-border-color)] pt-3"
			>
				<el-icon><WarningFilled /></el-icon>
				<span>Signers will receive an email from Adobe Sign.</span>
			</div>
		</div>

		<template #footer>
			<el-button @click="handleClose">Back</el-button>
			<el-button type="primary" @click="handleConfirm">Confirm &amp; Send</el-button>
		</template>
	</el-dialog>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { Document, WarningFilled } from '@element-plus/icons-vue';
import type { RequestAdobeSignInput } from '#/adobeSign';

const emit = defineEmits<{
	confirmed: [];
	back: [];
}>();

const visible = ref(false);
const fileName = ref('');
const requestData = ref<RequestAdobeSignInput | null>(null);

const open = (params: { fileName: string; requestData: RequestAdobeSignInput }) => {
	fileName.value = params.fileName;
	requestData.value = params.requestData;
	visible.value = true;
};

defineExpose({ open, requestData });

const handleClose = () => {
	emit('back');
	fileName.value = '';
	requestData.value = null;
	visible.value = false;
};

const handleConfirm = () => {
	emit('confirmed');
	handleClose();
};
</script>
