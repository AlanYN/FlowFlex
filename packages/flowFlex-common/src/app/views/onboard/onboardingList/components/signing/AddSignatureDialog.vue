<script setup lang="ts">
import { ref, nextTick } from 'vue';
import FromProfileTab from './FromProfileTab.vue';
import DrawTab from './DrawTab.vue';

// ========================= Props / Emits =========================

interface Props {
	visible: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	visible: false,
});

const emit = defineEmits<{
	'update:visible': [value: boolean];
	signatureSelected: [imageBase64: string];
}>();

// ========================= State =========================

/** Currently active tab: 'fromProfile' | 'draw' */
const activeTab = ref<'fromProfile' | 'draw'>('fromProfile');
const drawTabRef = ref<{ resizePad: () => Promise<void>; clearPad: () => Promise<void> } | null>(
	null
);

// ========================= Helpers =========================

/**
 * Closes the dialog by emitting the v-model update.
 */
function close() {
	emit('update:visible', false);
}

/**
 * Called when the user selects a saved signature from the From Profile tab.
 * Emits signatureSelected and closes the dialog.
 * Validates: Requirements 11.2, 11.3
 */
function handleSignatureSelectedFromProfile(imageBase64: string) {
	emit('signatureSelected', imageBase64);
	close();
}

/**
 * Called when the user clicks "Use Signature" in the Draw tab.
 * The drawn signature is NOT saved to the user's profile (Requirement 11.8).
 * Emits signatureSelected and closes the dialog.
 * Validates: Requirements 11.6, 11.7, 11.8
 */
function handleSignatureUsedFromDraw(imageBase64: string) {
	emit('signatureSelected', imageBase64);
	close();
}

/**
 * Resets tab to From Profile and clears the draw canvas whenever the dialog opens.
 * Ensures no previous signature strokes are visible when the user re-opens the dialog.
 */
async function handleOpen() {
	activeTab.value = 'fromProfile';
	await nextTick();
	drawTabRef.value?.clearPad();
}

/**
 * When user switches to the Draw tab, resize the signature pad so it
 * occupies the correct dimensions (canvas may have been zero-sized at mount).
 */
async function handleTabChange(tab: string) {
	if (tab === 'draw') {
		await nextTick();
		drawTabRef.value?.resizePad();
	}
}
</script>

<template>
	<!-- Validates: Requirement 11.1 (Add Signature dialog with two tabs) -->
	<el-dialog
		:model-value="props.visible"
		title="Add Signature"
		width="480px"
		:close-on-click-modal="false"
		@update:model-value="(v) => emit('update:visible', v)"
		@open="handleOpen"
	>
		<el-tabs v-model="activeTab" class="signature-tabs" @tab-change="handleTabChange">
			<!-- Tab 1: From Profile
                 Validates: Requirements 11.1, 11.2, 11.3, 11.4 -->
			<el-tab-pane label="From Profile" name="fromProfile">
				<FromProfileTab @signature-selected="handleSignatureSelectedFromProfile" />
			</el-tab-pane>

			<!-- Tab 2: Draw
                 mode='signing' ensures drawn signatures are NOT saved to profile.
                 Validates: Requirements 11.1, 11.5, 11.6, 11.7, 11.8 -->
			<el-tab-pane label="Draw" name="draw">
				<DrawTab ref="drawTabRef" mode="signing" @use="handleSignatureUsedFromDraw" />
			</el-tab-pane>
		</el-tabs>

		<!-- Dialog footer with a plain Close action -->
		<template #footer>
			<el-button @click="close">Close</el-button>
		</template>
	</el-dialog>
</template>

<style scoped lang="scss">
.signature-tabs {
	// Ensure tab panes have consistent minimum height
	:deep(.el-tabs__content) {
		min-height: 220px;
	}
}
</style>
