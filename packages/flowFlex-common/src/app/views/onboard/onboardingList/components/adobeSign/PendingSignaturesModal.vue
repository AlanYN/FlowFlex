<template>
	<el-dialog
		v-model="visible"
		title="Pending Signature Documents"
		width="480px"
		draggable
		:append-to-body="true"
		:destroy-on-close="true"
		:close-on-click-modal="false"
		:before-close="handleClose"
	>
		<div class="space-y-4">
			<!-- Warning banner -->
			<div
				class="flex items-start gap-2.5 p-3 bg-amber-50 dark:bg-amber-900/20 rounded-lg border border-amber-200 dark:border-amber-700"
			>
				<el-icon class="text-amber-500 mt-0.5 flex-shrink-0 text-base">
					<WarningFilled />
				</el-icon>
				<p class="text-sm text-amber-700 dark:text-amber-300 leading-relaxed">
					This case has
					<strong>{{ items.length }}</strong>
					document{{ items.length > 1 ? 's' : '' }} still waiting for signatures. The
					signature process will continue after the case is force completed.
				</p>
			</div>

			<!-- File list -->
			<div class="space-y-1.5 max-h-52 overflow-y-auto">
				<div
					v-for="item in items"
					:key="item.agreementId"
					class="flex items-start gap-2.5 px-3 py-2.5 rounded-lg border border-[var(--el-border-color-lighter)] bg-[var(--el-fill-color-light)]"
				>
					<el-icon class="text-[var(--el-text-color-secondary)] mt-0.5 flex-shrink-0">
						<Document />
					</el-icon>
					<div class="flex-1 min-w-0">
						<p
							class="text-sm font-medium text-[var(--el-text-color-primary)] truncate"
							:title="item.fileName"
						>
							{{ item.fileName }}
						</p>
						<p class="text-xs text-[var(--el-text-color-secondary)] mt-0.5">
							{{ item.stageName }}
							<span class="mx-1 opacity-50">·</span>
							{{ item.pendingSignerCount }} of {{ item.totalSignerCount }} signer{{
								item.totalSignerCount > 1 ? 's' : ''
							}}
							pending
						</p>
					</div>
				</div>
			</div>

			<!-- Confirm question -->
			<p class="text-sm text-[var(--el-text-color-secondary)]">
				Are you sure you want to force complete this case?
			</p>
		</div>

		<template #footer>
			<el-button @click="handleClose">Cancel</el-button>
			<el-button type="primary" @click="handleConfirm">Yes, Continue</el-button>
		</template>
	</el-dialog>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { Document, WarningFilled } from '@element-plus/icons-vue';
import type { PendingSignatureItem } from '@/apis/ow/adobeSign';

const emit = defineEmits<{
	confirmed: [];
}>();

const visible = ref(false);
const items = ref<PendingSignatureItem[]>([]);

// resolve/reject for the promise-based API
let _resolve: (() => void) | null = null;
let _reject: (() => void) | null = null;

/**
 * Open the modal and return a Promise that resolves on confirm, rejects on cancel.
 */
const open = (pendingItems: PendingSignatureItem[]): Promise<void> => {
	items.value = pendingItems;
	visible.value = true;
	return new Promise((resolve, reject) => {
		_resolve = resolve;
		_reject = reject;
	});
};

defineExpose({ open });

const handleConfirm = () => {
	visible.value = false;
	_resolve?.();
	_resolve = null;
	_reject = null;
	emit('confirmed');
};

const handleClose = () => {
	visible.value = false;
	_reject?.();
	_resolve = null;
	_reject = null;
};
</script>
