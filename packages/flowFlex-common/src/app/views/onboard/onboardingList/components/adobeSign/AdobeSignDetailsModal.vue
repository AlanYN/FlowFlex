<template>
	<el-dialog
		v-model="visible"
		title="Signature Details"
		width="520px"
		draggable
		:append-to-body="true"
		:destroy-on-close="true"
		:close-on-click-modal="false"
		:before-close="handleClose"
	>
		<div v-if="loading" class="py-6 text-center">
			<el-icon class="text-2xl animate-spin"><Loading /></el-icon>
			<p class="text-[var(--el-text-color-secondary)] mt-2 text-sm">Loading details...</p>
		</div>

		<div v-else-if="agreement" class="space-y-4 text-sm">
			<!-- Document + Status -->
			<div
				class="flex items-center gap-3 p-3 rounded-lg bg-[var(--el-fill-color-light)] border border-[var(--el-border-color-lighter)]"
			>
				<el-icon class="text-[var(--el-text-color-secondary)] flex-shrink-0">
					<Document />
				</el-icon>
				<span
					class="font-medium flex-1 truncate text-[var(--el-text-color-primary)]"
					:title="fileName"
				>
					{{ fileName }}
				</span>
				<el-tag
					:type="ADOBE_SIGN_TAG_TYPES[agreement.status]"
					effect="light"
					size="small"
					class="flex-shrink-0"
				>
					{{ agreement.status }}
				</el-tag>
			</div>

			<!-- Agreement ID + Signing Order + Message -->
			<div class="space-y-1.5 text-xs text-[var(--el-text-color-secondary)]">
				<div class="flex items-center gap-2">
					<span class="text-[var(--el-text-color-placeholder)] w-24 flex-shrink-0">
						Agreement ID
					</span>
					<span class="font-mono text-[var(--el-text-color-regular)] select-all">
						{{ agreement.agreementId }}
					</span>
				</div>
				<div class="flex items-center gap-2">
					<span class="text-[var(--el-text-color-placeholder)] w-24 flex-shrink-0">
						Signing Order
					</span>
					<div class="flex items-center gap-1.5">
						<el-icon
							v-if="agreement.signingOrder === 'Sequential'"
							class="text-[var(--el-color-primary)]"
						>
							<Sort />
						</el-icon>
						<el-icon v-else class="text-[var(--el-color-success)]">
							<Grid />
						</el-icon>
						<span class="text-[var(--el-text-color-regular)]">
							{{
								agreement.signingOrder === 'Sequential'
									? 'Sequential (one by one)'
									: 'Parallel (all at once)'
							}}
						</span>
					</div>
				</div>
				<div v-if="agreement.message" class="flex items-start gap-2">
					<span class="text-[var(--el-text-color-placeholder)] w-24 flex-shrink-0 pt-0.5">
						Message
					</span>
					<span class="text-[var(--el-text-color-regular)] break-words">
						{{ agreement.message }}
					</span>
				</div>
			</div>

			<!-- Signers -->
			<div>
				<p
					class="text-xs font-semibold text-[var(--el-text-color-secondary)] uppercase tracking-wide mb-2"
				>
					Signers ({{ agreement.signers?.length ?? 0 }})
				</p>
				<div class="space-y-1.5 max-h-52 overflow-y-auto pr-0.5">
					<div
						v-for="(signer, i) in agreement.signers"
						:key="i"
						class="flex items-center gap-2.5 px-3 py-2 border border-[var(--el-border-color-lighter)] rounded-lg bg-[var(--el-bg-color)]"
					>
						<!-- 序号 -->
						<span
							class="text-xs text-[var(--el-text-color-placeholder)] w-4 flex-shrink-0 text-center"
						>
							{{ i + 1 }}
						</span>
						<!-- 信息 -->
						<div class="flex-1 min-w-0">
							<p class="font-medium text-[var(--el-text-color-primary)] truncate">
								{{ signer.name || '—' }}
							</p>
							<p class="text-xs text-[var(--el-text-color-secondary)] truncate">
								{{ signer.email || '—' }}
								<span
									v-if="signer.role"
									class="text-[var(--el-text-color-placeholder)]"
								>
									· {{ signer.role }}
								</span>
							</p>
						</div>
						<!-- 签署状态 -->
						<div class="flex-shrink-0 text-xs">
							<span
								v-if="signer.signedAt || signer.status === 'Signed'"
								class="text-[var(--el-color-success)]"
							>
								✓
								{{ signer.signedAt ? timeZoneConvert(signer.signedAt) : 'Signed' }}
							</span>
							<span
								v-else-if="signer.status === 'Declined'"
								class="text-[var(--el-color-danger)]"
							>
								Declined
							</span>
							<span
								v-else-if="signer.status === 'Cancelled'"
								class="text-[var(--el-text-color-placeholder)]"
							>
								Cancelled
							</span>
							<span v-else class="text-[var(--el-color-warning)]">Awaiting</span>
						</div>
					</div>
				</div>
			</div>

			<!-- Timeline -->
			<div>
				<p
					class="text-xs font-semibold text-[var(--el-text-color-secondary)] uppercase tracking-wide mb-2"
				>
					Timeline
				</p>
				<div class="space-y-1.5 text-xs text-[var(--el-text-color-regular)]">
					<div class="flex items-center gap-2">
						<span class="text-[var(--el-text-color-placeholder)] w-36 flex-shrink-0">
							{{ timeZoneConvert(agreement.createDate) }}
						</span>
						<span>
							Requested by
							<strong>{{ agreement.requestedByName || 'User' }}</strong>
						</span>
					</div>
					<div v-if="agreement.completedDate" class="flex items-center gap-2">
						<span class="text-[var(--el-text-color-placeholder)] w-36 flex-shrink-0">
							{{ timeZoneConvert(agreement.completedDate) }}
						</span>
						<span>Signing completed</span>
					</div>
				</div>
			</div>
		</div>

		<div v-else class="py-6 text-center text-[var(--el-text-color-secondary)] text-sm">
			Failed to load agreement details.
		</div>

		<template #footer>
			<el-button @click="handleRefresh" :loading="loading" :icon="RefreshRight">
				Refresh
			</el-button>
			<el-button @click="handleClose">Close</el-button>
		</template>
	</el-dialog>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { Document, Loading, Sort, Grid, RefreshRight } from '@element-plus/icons-vue';
import { ElMessage } from 'element-plus';
import { getAgreement } from '@/apis/ow/adobeSign';
import type { AdobeSignAgreement } from '#/adobeSign';
import { ADOBE_SIGN_TAG_TYPES } from '@/enums/adobeSignConstants';
import { timeZoneConvert } from '@/hooks/time';

const emit = defineEmits<{
	/** Fired when closing a Completed agreement — parent should refresh the file list */
	completed: [];
}>();

const visible = ref(false);
const fileName = ref('');
const loading = ref(false);
const agreement = ref<AdobeSignAgreement | null>(null);
const currentAgreementId = ref<string | number>('');

const open = async (params: { agreementId: string | number; fileName?: string }) => {
	currentAgreementId.value = params.agreementId;
	fileName.value = params.fileName || '';
	agreement.value = null;
	visible.value = true;
	loading.value = true;
	try {
		const res = await getAgreement(params.agreementId);
		if (res.code == 200) {
			agreement.value = (res as any)?.data ?? res;
		} else {
			res?.msg && ElMessage.error(res.msg);
		}
	} finally {
		loading.value = false;
	}
};

const handleClose = () => {
	const wasCompleted = agreement.value?.status === 'Completed';
	fileName.value = '';
	agreement.value = null;
	currentAgreementId.value = '';
	loading.value = false;
	visible.value = false;
	if (wasCompleted) emit('completed');
};

const handleRefresh = async () => {
	if (!currentAgreementId.value) return;
	loading.value = true;
	try {
		const res = await getAgreement(currentAgreementId.value);
		if (res.code == 200) {
			agreement.value = (res as any)?.data ?? res;
		}
	} finally {
		loading.value = false;
	}
};

defineExpose({ open });
</script>
