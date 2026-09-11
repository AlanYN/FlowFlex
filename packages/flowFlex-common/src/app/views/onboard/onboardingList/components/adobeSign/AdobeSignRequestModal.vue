<template>
	<el-dialog
		v-model="visible"
		:title="step === 'edit' ? 'Request Legal Signature' : 'Confirm Signature Request'"
		width="600px"
		draggable
		:append-to-body="true"
		:destroy-on-close="true"
		:close-on-click-modal="false"
		:before-close="handleClose"
	>
		<!-- ============ Step 1: 编辑表单 ============ -->
		<el-form
			v-if="step === 'edit'"
			:model="{ signers, signingOrder, expirationDays, message }"
			label-position="top"
			class="space-y-4 p-1"
			@submit.prevent
		>
			<!-- Document -->
			<el-form-item label="Document">
				<div
					class="flex items-center gap-2 w-full px-3 py-2 border border-[var(--el-border-color)] rounded-[var(--el-border-radius-base)] bg-[var(--el-fill-color-light)]"
				>
					<el-icon class="text-[var(--el-text-color-secondary)]"><Document /></el-icon>
					<span class="text-sm truncate text-[var(--el-text-color-primary)]">
						{{ fileName }}
					</span>
				</div>
			</el-form-item>

			<!-- Signers -->
			<el-form-item label="Signers">
				<div class="w-full space-y-3">
					<div
						v-for="(signer, index) in signers"
						:key="index"
						class="border border-[var(--el-border-color)] rounded-[var(--el-border-radius-base)] p-3"
					>
						<div class="flex-1 space-y-2">
							<!-- Email -->
							<div>
								<el-input
									v-model="signer.email"
									placeholder="Email *"
									:class="{ 'is-error': emailErrors[index] }"
									@blur="validateEmail(index)"
									@input="clearEmailError(index)"
								/>
								<p
									v-if="emailErrors[index]"
									class="text-xs text-[var(--el-color-danger)] mt-1"
								>
									{{ emailErrors[index] }}
								</p>
							</div>
							<!-- Name + Role + Remove -->
							<div class="grid grid-cols-2 gap-2">
								<el-input v-model="signer.name" placeholder="Name *" />
								<div class="flex gap-2">
									<el-select v-model="signer.role" class="flex-1">
										<el-option
											v-for="role in ADOBE_SIGNER_ROLES"
											:key="role"
											:label="role"
											:value="role"
										/>
									</el-select>
									<el-button
										link
										type="danger"
										:disabled="signers.length <= 1"
										@click="removeSigner(index)"
									>
										Remove
									</el-button>
								</div>
							</div>
						</div>
					</div>

					<!-- Add Signer 按钮放在列表右下方 -->
					<div class="flex justify-end">
						<el-button
							link
							type="primary"
							:disabled="signers.length >= 10"
							@click="addSigner"
						>
							+ Add Signer
						</el-button>
					</div>

					<p v-if="signers.length >= 10" class="text-xs text-[var(--el-color-warning)]">
						Maximum 10 signers allowed.
					</p>
				</div>
			</el-form-item>

			<!-- Signing Order -->
			<el-form-item label="Signing Order">
				<el-radio-group v-model="signingOrder" class="flex flex-col items-start gap-1">
					<el-radio value="Sequential">Sequential (1 signs, then 2, then 3...)</el-radio>
					<el-radio value="Parallel">Parallel (All receive at the same time)</el-radio>
				</el-radio-group>
			</el-form-item>

			<!-- Expiration -->
			<el-form-item label="Expiration">
				<el-select v-model="expirationDays" style="width: 140px">
					<el-option
						v-for="days in ADOBE_SIGN_EXPIRATION_OPTIONS"
						:key="days"
						:label="`${days} days`"
						:value="days"
					/>
				</el-select>
			</el-form-item>

			<!-- Message -->
			<el-form-item label="Message to Signers">
				<el-input
					v-model="message"
					type="textarea"
					:rows="3"
					placeholder="Please review and sign this document."
					maxlength="1000"
					show-word-limit
				/>
			</el-form-item>

			<!-- Hint -->
			<div class="flex items-center gap-1.5 text-xs text-[var(--el-color-warning)]">
				<el-icon><WarningFilled /></el-icon>
				<span>Signers will receive an email from Adobe Sign.</span>
			</div>
		</el-form>

		<!-- ============ Step 2: 确认摘要 ============ -->
		<div v-else class="space-y-4 p-1 text-sm">
			<!-- Document -->
			<div
				class="flex items-center gap-2 px-3 py-2 border border-[var(--el-border-color)] rounded-[var(--el-border-radius-base)] bg-[var(--el-fill-color-light)]"
			>
				<el-icon class="text-[var(--el-text-color-secondary)]"><Document /></el-icon>
				<span class="truncate text-[var(--el-text-color-primary)]">{{ fileName }}</span>
			</div>

			<!-- Signers list -->
			<div>
				<p class="text-[var(--el-text-color-secondary)] mb-2">Signers:</p>
				<ol class="space-y-1 pl-4 list-decimal">
					<li
						v-for="(signer, i) in signers"
						:key="i"
						class="text-[var(--el-text-color-primary)]"
					>
						{{ signer.name }} ({{ signer.email }})
						<span class="text-[var(--el-text-color-placeholder)] ml-1">
							· {{ signer.role }}
						</span>
					</li>
				</ol>
			</div>

			<!-- Options summary -->
			<div class="space-y-1 text-[var(--el-text-color-regular)]">
				<p>
					Signing Order:
					<span class="font-medium text-[var(--el-text-color-primary)]">
						{{ signingOrder }}
					</span>
				</p>
				<p>
					Expires in:
					<span class="font-medium text-[var(--el-text-color-primary)]">
						{{ expirationDays }} days
					</span>
				</p>
				<p v-if="message">
					Message:
					<span class="text-[var(--el-text-color-primary)]">{{ message }}</span>
				</p>
			</div>

			<div
				class="flex items-center gap-1.5 border-t border-[var(--el-border-color)] pt-3 text-xs text-[var(--el-color-warning)]"
			>
				<el-icon><WarningFilled /></el-icon>
				<span>Signers will receive an email from Adobe Sign.</span>
			</div>
		</div>

		<!-- Footer -->
		<template #footer>
			<!-- Step 1 footer -->
			<template v-if="step === 'edit'">
				<el-button @click="handleClose">Cancel</el-button>
				<el-button type="primary" @click="handleSubmit">Send Request</el-button>
			</template>
			<!-- Step 2 footer -->
			<template v-else>
				<el-button @click="step = 'edit'" :disabled="submitting">Back</el-button>
				<el-button type="primary" :loading="submitting" @click="handleConfirm">
					Confirm &amp; Send
				</el-button>
			</template>
		</template>
	</el-dialog>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { Document, WarningFilled } from '@element-plus/icons-vue';
import { ElMessage } from 'element-plus';
import type { RequestAdobeSignInput, AdobeSigner, AdobeSigningOrder } from '#/adobeSign';
import { ADOBE_SIGNER_ROLES, ADOBE_SIGN_EXPIRATION_OPTIONS } from '@/enums/adobeSignConstants';
import { requestAdobeSign } from '@/apis/ow/adobeSign';

const emit = defineEmits<{
	confirmed: [];
}>();

const visible = ref(false);
const step = ref<'edit' | 'confirm'>('edit');
const submitting = ref(false);

const fileId = ref<string>('');
const fileName = ref<string>('');
const onboardingId = ref<string>('');
const stageId = ref<string>('');

const signers = ref<AdobeSigner[]>([{ email: '', name: '', role: 'Signer', order: 1 }]);
const signingOrder = ref<AdobeSigningOrder>('Sequential');
const expirationDays = ref(30);
const message = ref('');
const emailErrors = ref<string[]>(['']);

const open = (params: {
	fileId: string;
	fileName: string;
	onboardingId: string;
	stageId: string;
	initialData?: RequestAdobeSignInput;
}) => {
	fileId.value = params.fileId;
	fileName.value = params.fileName;
	onboardingId.value = params.onboardingId;
	stageId.value = params.stageId;

	if (params.initialData) {
		// Re-send：回填之前的表单数据
		const d = params.initialData;
		signers.value = d.signers.map((s) => ({
			email: s.email,
			name: s.name,
			role: s.role,
			order: s.order,
		}));
		signingOrder.value = d.signingOrder;
		expirationDays.value = d.expirationDays;
		message.value = d.message || '';
		emailErrors.value = d.signers.map(() => '');
		step.value = 'edit';
	} else {
		resetForm();
	}
	visible.value = true;
};

defineExpose({ open });

const resetForm = () => {
	step.value = 'edit';
	signers.value = [{ email: '', name: '', role: 'Signer', order: 1 }];
	signingOrder.value = 'Sequential';
	expirationDays.value = 30;
	message.value = '';
	emailErrors.value = [''];
};

const handleClose = () => {
	resetForm();
	visible.value = false;
};

const addSigner = () => {
	if (signers.value.length >= 10) return;
	signers.value.push({ email: '', name: '', role: 'Signer', order: signers.value.length + 1 });
	emailErrors.value.push('');
};

const removeSigner = (index: number) => {
	signers.value.splice(index, 1);
	emailErrors.value.splice(index, 1);
	signers.value.forEach((s, i) => {
		s.order = i + 1;
	});
};

const validateEmail = (index: number) => {
	const email = signers.value[index].email;
	if (!email) {
		emailErrors.value[index] = 'Email is required';
	} else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
		emailErrors.value[index] = 'Please enter a valid email address.';
	} else {
		emailErrors.value[index] = '';
	}
};

const clearEmailError = (index: number) => {
	emailErrors.value[index] = '';
};

// Send Request 按钮：校验通过后切到确认页
const handleSubmit = () => {
	let hasError = false;
	signers.value.forEach((_, index) => {
		validateEmail(index);
		if (emailErrors.value[index]) hasError = true;
		if (!signers.value[index].name) {
			ElMessage.error(`Signer ${index + 1}: Name is required`);
			hasError = true;
		}
	});
	if (hasError) return;

	// 切换到确认页，不关闭弹窗，内容保留
	step.value = 'confirm';
};

// Confirm & Send 按钮：调用 API，成功后关闭
const handleConfirm = async () => {
	submitting.value = true;
	try {
		const res = await requestAdobeSign({
			onboardingId: onboardingId.value,
			stageId: stageId.value,
			sourceFileId: fileId.value,
			signers: signers.value.map((s) => ({ ...s })),
			signingOrder: signingOrder.value,
			expirationDays: expirationDays.value,
			message: message.value || undefined,
		});
		if (res.code == 200) {
			ElMessage.success('Signing request sent successfully');
			emit('confirmed');
			visible.value = false;
		} else {
			res?.msg && ElMessage.error(res?.msg);
		}
	} finally {
		submitting.value = false;
	}
};
</script>
