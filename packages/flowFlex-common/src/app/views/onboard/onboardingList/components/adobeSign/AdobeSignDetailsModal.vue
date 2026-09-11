<template>
	<el-dialog
		v-model="visible"
		title="Signature Details"
		width="560px"
		draggable
		:append-to-body="true"
		:destroy-on-close="true"
		:close-on-click-modal="false"
		:before-close="handleClose"
	>
		<div v-if="loading" class="py-8 text-center">
			<el-icon class="text-2xl animate-spin"><Loading /></el-icon>
			<p class="text-gray-500 mt-2 text-sm">Loading details...</p>
		</div>

		<div v-else-if="agreement" class="space-y-4 text-sm">
			<div class="space-y-1">
				<div class="flex items-center gap-2">
					<el-icon class="text-gray-500"><Document /></el-icon>
					<span class="font-medium">{{ fileName }}</span>
				</div>
				<div class="flex items-center gap-2">
					<span class="text-gray-500">Status:</span>
					<el-tag
						:type="ADOBE_SIGN_TAG_TYPES[agreement.status]"
						effect="light"
						size="small"
					>
						{{ agreement.status }}
					</el-tag>
				</div>
				<p class="text-xs text-gray-400">Agreement ID: {{ agreement.agreementId }}</p>
			</div>

			<div>
				<p class="font-semibold mb-2">SIGNERS</p>
				<div
					v-for="(signer, i) in agreement.signers"
					:key="i"
					class="flex items-start gap-2 p-2 border rounded-lg mb-2"
				>
					<span class="text-gray-400 w-4 flex-shrink-0">{{ i + 1 }}</span>
					<div class="flex-1">
						<p class="font-medium">{{ signer.name }}</p>
						<p class="text-gray-500 text-xs">{{ signer.email }} · {{ signer.role }}</p>
						<p class="text-xs mt-1">
							<span v-if="signer.signedAt" class="text-green-600">
								✅ Signed {{ formatDate(signer.signedAt) }}
							</span>
							<span v-else-if="signer.status === 'Declined'" class="text-red-500">
								❌ Declined
							</span>
							<span v-else-if="signer.status === 'Cancelled'" class="text-gray-500">
								⚫ Cancelled
							</span>
							<span v-else class="text-amber-500">⏳ Awaiting signature</span>
						</p>
					</div>
				</div>
			</div>

			<div>
				<p class="font-semibold mb-2">TIMELINE</p>
				<div class="space-y-1">
					<div class="flex gap-3 text-xs text-gray-600 dark:text-gray-400">
						<span class="w-36 flex-shrink-0">
							{{ formatDate(agreement.createDate) }}
						</span>
						<span>
							🚀 Request initiated by {{ agreement.requestedByName || 'User' }}
						</span>
					</div>
					<div
						v-if="agreement.completedDate"
						class="flex gap-3 text-xs text-gray-600 dark:text-gray-400"
					>
						<span class="w-36 flex-shrink-0">
							{{ formatDate(agreement.completedDate) }}
						</span>
						<span>✅ Signing completed, files archived</span>
					</div>
				</div>
			</div>
		</div>

		<div v-else class="py-8 text-center text-gray-500">Failed to load agreement details.</div>

		<template #footer>
			<el-button @click="handleClose">Close</el-button>
		</template>
	</el-dialog>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { Document, Loading } from '@element-plus/icons-vue';
import { ElMessage } from 'element-plus';
import { getAgreement } from '@/apis/ow/adobeSign';
import type { AdobeSignAgreement } from '#/adobeSign';
import { ADOBE_SIGN_TAG_TYPES } from '@/enums/adobeSignConstants';

const visible = ref(false);
const fileName = ref('');
const loading = ref(false);
const agreement = ref<AdobeSignAgreement | null>(null);

const open = async (params: { agreementId: string | number; fileName?: string }) => {
	fileName.value = params.fileName || '';
	agreement.value = null;
	visible.value = true;
	loading.value = true;
	try {
		const res = await getAgreement(params.agreementId);
		agreement.value = (res as any)?.data ?? res;
	} catch {
		ElMessage.error('Failed to load signature details');
	} finally {
		loading.value = false;
	}
};

const handleClose = () => {
	fileName.value = '';
	agreement.value = null;
	loading.value = false;
	visible.value = false;
};

defineExpose({ open });

const formatDate = (date?: string | null) => {
	if (!date) return '';
	return new Date(date).toLocaleString();
};
</script>
