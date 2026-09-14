<template>
	<el-dialog
		v-model="visible"
		title="Send Reminder"
		width="420px"
		draggable
		:append-to-body="true"
		:destroy-on-close="true"
		:close-on-click-modal="false"
		:before-close="handleClose"
	>
		<div class="space-y-3 text-sm">
			<!-- Signing Order + Message -->
			<div
				class="space-y-1.5 text-xs p-3 rounded-lg bg-[var(--el-fill-color-light)] border border-[var(--el-border-color-lighter)]"
			>
				<div class="flex items-center gap-2">
					<span class="text-[var(--el-text-color-placeholder)] w-24 flex-shrink-0">
						Signing Order
					</span>
					<span class="text-[var(--el-text-color-regular)]">
						{{
							signingOrder === 'Sequential'
								? 'Sequential (one by one)'
								: 'Parallel (all at once)'
						}}
					</span>
				</div>
				<div v-if="message" class="flex items-start gap-2">
					<span class="text-[var(--el-text-color-placeholder)] w-24 flex-shrink-0 pt-0.5">
						Message
					</span>
					<span class="text-[var(--el-text-color-regular)] break-words">
						{{ message }}
					</span>
				</div>
			</div>

			<p class="text-[var(--el-text-color-secondary)]">Select signers to remind:</p>

			<div
				v-if="awaitingSigners.length === 0"
				class="py-4 text-center text-[var(--el-text-color-placeholder)]"
			>
				No pending signers to remind.
			</div>

			<div v-else class="space-y-1.5 max-h-60 overflow-y-auto pr-0.5">
				<div
					v-for="{ signer, originalIndex } in awaitingSigners"
					:key="originalIndex"
					class="flex items-center gap-2.5 px-3 py-2 border border-[var(--el-border-color-lighter)] rounded-lg cursor-pointer hover:bg-[var(--el-fill-color-light)] transition-colors select-none"
					:class="{
						'bg-[var(--el-color-primary-light-9)] border-[var(--el-color-primary-light-5)]':
							selectedIndices.includes(originalIndex),
					}"
					@click="toggleSigner(originalIndex)"
				>
					<el-checkbox
						:model-value="selectedIndices.includes(originalIndex)"
						@click.stop="toggleSigner(originalIndex)"
					/>
					<div class="flex-1 min-w-0">
						<p class="font-medium text-[var(--el-text-color-primary)] truncate">
							{{ signer.name || '—' }}
						</p>
						<p class="text-xs text-[var(--el-text-color-secondary)] truncate">
							{{ signer.email || '—' }}
						</p>
					</div>
					<el-tag type="warning" size="small" effect="light" class="flex-shrink-0">
						Awaiting
					</el-tag>
				</div>
			</div>

			<!-- 全选 / 取消全选 -->
			<div v-if="awaitingSigners.length > 1" class="flex justify-end">
				<el-button link type="primary" size="small" @click="toggleAll">
					{{ isAllSelected ? 'Deselect All' : 'Select All' }}
				</el-button>
			</div>
		</div>

		<template #footer>
			<el-button @click="handleClose">Cancel</el-button>
			<el-button
				type="primary"
				:loading="submitting"
				:disabled="selectedIndices.length === 0"
				@click="handleSend"
			>
				Send Reminder
				<span v-if="selectedIndices.length > 0" class="ml-1 opacity-70">
					({{ selectedIndices.length }})
				</span>
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
const signingOrder = ref<string>('Sequential');
const message = ref<string>('');
// 用原始下标作为唯一标识，避免重复邮箱时的歧义
const selectedIndices = ref<number[]>([]);
const submitting = ref(false);

// 待签署的 signer，保留原始 index 用于唯一标识和发送时取邮箱
const awaitingSigners = computed(() =>
	signers.value
		.map((signer, originalIndex) => ({ signer, originalIndex }))
		.filter(
			({ signer }) =>
				!signer.signedAt && signer.status !== 'Declined' && signer.status !== 'Cancelled'
		)
);

const isAllSelected = computed(
	() =>
		awaitingSigners.value.length > 0 &&
		selectedIndices.value.length === awaitingSigners.value.length
);

const open = (params: {
	agreementId: string | number;
	signers: AdobeSigner[];
	signingOrder?: string;
	message?: string;
}) => {
	agreementId.value = params.agreementId;
	signers.value = params.signers;
	signingOrder.value = params.signingOrder ?? 'Sequential';
	message.value = params.message ?? '';
	// 默认全选
	selectedIndices.value = awaitingSigners.value.map((s) => s.originalIndex);
	visible.value = true;
};

defineExpose({ open });

const handleClose = () => {
	agreementId.value = '';
	signers.value = [];
	signingOrder.value = 'Sequential';
	message.value = '';
	selectedIndices.value = [];
	submitting.value = false;
	visible.value = false;
};

const toggleSigner = (index: number) => {
	const idx = selectedIndices.value.indexOf(index);
	if (idx >= 0) {
		selectedIndices.value.splice(idx, 1);
	} else {
		selectedIndices.value.push(index);
	}
};

const toggleAll = () => {
	if (isAllSelected.value) {
		selectedIndices.value = [];
	} else {
		selectedIndices.value = awaitingSigners.value.map((s) => s.originalIndex);
	}
};

const handleSend = async () => {
	if (selectedIndices.value.length === 0) return;
	// 按选中的 index 取对应邮箱（可能有重复，后端会去重或照单全收）
	const emails = selectedIndices.value
		.map((i) => signers.value[i]?.email)
		.filter(Boolean) as string[];
	submitting.value = true;
	try {
		const res = await sendReminder(agreementId.value, emails);
		if (res.code == 200) {
			ElMessage.success('Reminder sent successfully');
			emit('sent');
			visible.value = false;
		} else {
			res?.msg && ElMessage.error(res.msg);
		}
	} finally {
		submitting.value = false;
	}
};
</script>
