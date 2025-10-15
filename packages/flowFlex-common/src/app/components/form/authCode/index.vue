<template>
	<div class="w-full flex flex-col items-center">
		<div class="flex gap-2 justify-between mb-4 w-full">
			<input
				v-for="(_, index) in authCodeLength"
				:key="index"
				type="text"
				maxlength="1"
				v-model="verificationCode[index]"
				:disabled="disabled"
				@input="handleCodeInput(index, $event)"
				@keydown="handleCodeKeyDown(index, $event)"
				@focus="handleCodeFocus(index)"
				@compositionstart="handleCompositionStart"
				@compositionend="handleCompositionEnd(index, $event)"
				ref="codeInputs"
				class="w-10 h-10 rounded-xl auth-code-input text-center text-lg focus:outline-none"
				:class="{ 'bg-gray-500/50 cursor-not-allowed': disabled }"
			/>
		</div>
		<div v-if="isSendEmail" class="text-sm text-white/80 cursor-not-allowed">
			{{ t('sys.login.resend') }}
			<text class="text-gray-500 ml-4">{{ time }}s</text>
		</div>
		<div
			v-else
			@click="startTimer"
			class="text-sm text-white/80 cursor-pointer flex items-center gap-x-2"
			:class="{ 'cursor-not-allowed': sendLoading }"
		>
			{{ t('sys.login.sendEmail') }}
			<el-icon v-if="sendLoading" class="is-loading">
				<Loading />
			</el-icon>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useI18n } from '@/hooks/useI18n';
import { checkEmail } from '@/utils/check';
import { VALIDATOR_EMAIL } from '@/utils/formValidate';
import { ElMessage } from 'element-plus';
import { sendEmail } from '@/apis/global';
import { Loading } from '@element-plus/icons-vue';

const { t } = useI18n();

const codeInputs = ref<HTMLInputElement[]>([]);

const props = withDefaults(
	defineProps<{
		modelValue: string[];
		authCodeLength?: number;
		disabled?: boolean;
		email: string;
	}>(),
	{
		authCodeLength: 6,
		disabled: false,
	}
);

const emit = defineEmits(['update:modelValue', 'inputEnd', 'sendEmailSuccess']);

// 标记当前是否处于输入法合成状态
let isComposing = false;

const verificationCode = computed({
	get: () => {
		return props?.modelValue;
	},
	set: (value) => {
		console.log('value:', value);
		emit('update:modelValue', value);
	},
});

const handleCodeKeyDown = (index: number, event: KeyboardEvent) => {
	const key = event.key;
	const input = event.target as HTMLInputElement;

	// 如果按下退格键，且当前框为空
	if (key === 'Backspace' && !input.value && index > 0) {
		codeInputs.value[index - 1]?.focus();
	}
};

const handleCodeFocus = (index: number) => {
	if (index > 0 && !verificationCode.value[index] && !verificationCode.value[index - 1]) {
		const i = verificationCode.value.findIndex((v) => !v);
		if (i >= 0) codeInputs.value[i]?.focus();
	}
};
// 输入法合成事件处理
const handleCompositionStart = () => {
	isComposing = true;
};
const handleCompositionEnd = (index: number, event: Event) => {
	isComposing = false;
	// 合成结束后手动触发一次 input 处理
	handleCodeInput(index, event);
};

const handleCodeInput = (index: number, event: Event) => {
	if (isComposing) return; // 输入法合成中不处理
	const input = event.target as HTMLInputElement;
	verificationCode.value[index] = input.value;
	if (input.value && index < props.authCodeLength - 1) {
		codeInputs.value[index + 1]?.focus();
	}

	if (
		verificationCode.value.length === props.authCodeLength &&
		verificationCode.value.every((v) => v)
	) {
		emit('inputEnd');
	}
};

const time = ref(60);

const sendLoading = ref(false);
const isSendEmail = ref(false);
const timer = ref<any>(null);
const startTimer = async () => {
	if (!props.email) {
		ElMessage.info(t('sys.login.emailEmpty'));
		return;
	}
	if (timer.value || !props.email || sendLoading.value) return;
	if (!checkEmail(props?.email)) {
		ElMessage.error(VALIDATOR_EMAIL);
		return;
	}
	try {
		sendLoading.value = true;
		const res = await sendEmail({ email: props.email });
		if (res.code == '200') {
			time.value = 60;
			isSendEmail.value = true;
			clearInterval(timer.value);
			ElMessage.success(t('sys.login.successEmail'));
			emit('sendEmailSuccess');
			timer.value = setInterval(() => {
				time.value--;
				if (time.value <= 0) {
					time.value = 60;
					isSendEmail.value = false;
					timer.value = null;
				}
			}, 1000);
		} else {
			ElMessage.error(res?.msg || t('sys.api.apiRequestFailed'));
		}
	} finally {
		sendLoading.value = false;
	}
};
</script>

<style scoped>
.auth-code-input {
	color: var(--el-color-white);
	background: rgba(255, 255, 255, 0.1);
	border: 1px solid rgba(255, 255, 255, 0.2);
}

.auth-code-input:focus {
	border-color: var(--el-color-primary);
	box-shadow: 0 0 15px rgba(0, 0, 0, 0.2);
}
</style>
