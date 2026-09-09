<template>
	<el-dialog
		v-model="visible"
		title="Send Reminder"
		width="440px"
		draggable
		:append-to-body="true"
		:destroy-on-close="true"
		:close-on-click-modal="false"
		:before-close="handleClose"
	>
		<div class="space-y-3">
			<p class="text-sm text-gray-600 dark:text-gray-400">Select signers to remind:</p>

			<div v-if="awaitingSigners.length === 0" class="text-sm text-gray-500 py-4 text-center">
				No pending signers to remind.
			</div>

			<div v-else class="space-y-2">
				<div
					v-for="signer in awaitingSigners"
					:key="signer.email"
					class="flex items-start gap-3 p-3 border rounded-lg"
				>
					<el-checkbox
						:model-value="selectedEmails.includes(signer.email)"
						@change="toggleSigner(signer.email)"
					/>
					<div class="flex-1 min-w-0">
						<p class="text-sm font-medium truncate">
							{{ signer.name }} ({{ signer.email }})
						</p>
						<p class="text-xs text-amber-500 mt-0.5">Status: Awaiting signature</p>
					</div>
				</div>
			</div>
		</div>

		<template #footer>
			<el-button @click="handleClose">Cancel</el-button>
			<el-button
				type="primary"
				:loading="submitting"
				:disabled="selectedEmails.length === 0"
				@click="handleSend"
			>
				Send Reminder
			</el-button>
		</template>
	</el-dialog>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { ElMessage } from 'element-plus';
import { sendReminder } from '@/apis/ow/adobeSign';
import type { AdobeSigner } from '#/adobeSign';

const emit = defineEmits<{
	sent: [];
}>();

const visible = ref(false);
const agreementId = ref<string | number>('');
const signers = ref<AdobeSigner[]>([]);
const selectedEmails = ref<string[]>([]);
const submitting = ref(false);

const awaitingSigners = computed(() =>
	signers.value.filter((s) => !s.signedAt && s.status !== 'Declined' && s.status !== 'Cancelled')
);

const open = (params: { agreementId: string | number; signers: AdobeSigner[] }) => {
	agreementId.value = params.agreementId;
	signers.value = params.signers;
	selectedEmails.value = params.signers
		.filter((s) => !s.signedAt && s.status !== 'Declined' && s.status !== 'Cancelled')
		.map((s) => s.email);
	visible.value = true;
};

defineExpose({ open });

const handleClose = () => {
	agreementId.value = '';
	signers.value = [];
	selectedEmails.value = [];
	submitting.value = false;
	visible.value = false;
};

const toggleSigner = (email: string) => {
	const idx = selectedEmails.value.indexOf(email);
	if (idx >= 0) selectedEmails.value.splice(idx, 1);
	else selectedEmails.value.push(email);
};

const handleSend = async () => {
	if (selectedEmails.value.length === 0) return;
	submitting.value = true;
	try {
		await sendReminder(agreementId.value, selectedEmails.value);
		ElMessage.success('Reminder sent successfully');
		emit('sent');
		visible.value = false;
	} catch {
		ElMessage.error('Failed to send reminder');
	} finally {
		submitting.value = false;
	}
};
</script>
