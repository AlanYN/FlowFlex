<template>
	<div class="portal-access-page">
		<div class="container">
			<div class="header">
				<h1>FlowFlex Portal</h1>
				<p>Please verify your email to access the flowflex portal</p>
			</div>

		<div v-if="verificationState === 'form'" class="verification-form">
			<el-form ref="formRef" :model="form" :rules="rules" label-width="120px">
				<el-form-item label="Email" prop="email">
					<el-input v-model="form.email" placeholder="Enter your email address" />
				</el-form-item>
				<el-form-item>
					<el-button type="primary" @click="handleVerify" :loading="loading">
						Verify Access
					</el-button>
				</el-form-item>
			</el-form>
		</div>

		<!-- Success State -->
		<div v-else-if="verificationState === 'success'" class="success-message">
				<el-result icon="success" title="Access Verified!" :sub-title="successMessage">
					<template #extra>
						<el-button type="primary" @click="redirectToOnboarding">
							Continue to Onboarding
						</el-button>
					</template>
				</el-result>
			</div>

			<!-- Expired State -->
			<div v-else-if="verificationState === 'expired'" class="expired-message">
				<el-result icon="warning" title="Invitation Expired" sub-title="This invitation link has expired.">
					<template #extra>
						<div class="space-y-4">
							<p class="text-gray-600">
								Your invitation link has expired. Please contact the organization that sent you this invitation to request a new link.
							</p>
							<div class="flex justify-center space-x-4">
								<el-button @click="requestNewInvitation">
									Request New Invitation
								</el-button>
								<el-button type="primary" @click="contactSupport">
									Contact Support
								</el-button>
							</div>
						</div>
					</template>
				</el-result>
			</div>

			<!-- Error State -->
			<div v-else-if="verificationState === 'error'" class="error-message">
				<el-result icon="error" title="Access Denied" :sub-title="errorMessage">
					<template #extra>
						<div class="space-y-4">
							<p class="text-gray-600">
								{{ errorDescription }}
							</p>
							<div class="flex justify-center space-x-4">
								<el-button @click="retryVerification">
									Try Again
								</el-button>
								<el-button type="primary" @click="contactSupport">
									Contact Support
								</el-button>
							</div>
						</div>
					</template>
				</el-result>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, ElForm } from 'element-plus';
import * as userInvitationApi from '@/apis/ow/userInvitation';

const route = useRoute();
const router = useRouter();

// Reactive data
const formRef = ref<InstanceType<typeof ElForm>>();
const loading = ref(false);
const isVerified = ref(false);
const successMessage = ref('');
const verificationState = ref<'form' | 'success' | 'expired' | 'error'>('form');
const errorMessage = ref('');
const errorDescription = ref('');

const form = ref({
	email: '',
});

const rules = {
	email: [
		{ required: true, message: 'Please enter your email address', trigger: 'blur' },
		{ type: 'email' as const, message: 'Please enter a valid email address', trigger: 'blur' },
	],
};

// Methods
const handleVerify = async () => {
	if (!formRef.value) return;

	try {
		await formRef.value.validate();
		loading.value = true;

		const token = route.query.token as string;
		if (!token) {
			ElMessage.error('Invalid invitation link');
			return;
		}

		const response = await userInvitationApi.verifyPortalAccess({
			token,
			email: form.value.email,
		});

		// Extract data from wrapped response
		const verificationData = response?.data || response;

		if (verificationData.isValid) {
			isVerified.value = true;
			verificationState.value = 'success';
			successMessage.value = `Welcome! You now have access to the onboarding portal.`;
			
			// Store access token for future API calls
			localStorage.setItem('portal_access_token', verificationData.accessToken);
			localStorage.setItem('onboarding_id', verificationData.onboardingId.toString());
		} else {
			if (verificationData.isExpired) {
				verificationState.value = 'expired';
			} else {
				verificationState.value = 'error';
				errorMessage.value = verificationData.errorMessage || 'Verification failed';
				errorDescription.value = getErrorDescription(verificationData.errorMessage);
			}
		}
	} catch (error) {
		verificationState.value = 'error';
		errorMessage.value = 'Connection Error';
		errorDescription.value = 'Unable to verify your invitation. Please check your internet connection and try again.';
		console.error('Verification error:', error);
	} finally {
		loading.value = false;
	}
};

const redirectToOnboarding = () => {
	const onboardingId = localStorage.getItem('onboarding_id');
	if (onboardingId) {
		// 跳转到onboard详情页面，传递onboardingId作为查询参数
		router.push(`/onboard/onboardDetail?onboardingId=${onboardingId}`);
	} else {
		router.push('/onboard/onboardList');
	}
};

// Get user-friendly error description
const getErrorDescription = (errorMessage?: string): string => {
	if (!errorMessage) return 'Please try again or contact support if the problem persists.';
	
	if (errorMessage.includes('Email address does not match')) {
		return 'Please make sure you are using the correct email address that received the invitation.';
	}
	if (errorMessage.includes('deactivated')) {
		return 'Your access has been temporarily disabled. Please contact the organization for assistance.';
	}
	if (errorMessage.includes('corrupted')) {
		return 'The invitation link appears to be damaged. Please request a new invitation link.';
	}
	return 'Please try again or contact support if the problem persists.';
};

// Request new invitation
const requestNewInvitation = () => {
	// Could implement email sending or redirect to a request form
	ElMessage.info('Please contact the organization that sent you the original invitation to request a new link.');
};

// Contact support
const contactSupport = () => {
	// Could open email client or redirect to support page
	const supportEmail = 'support@flowflex.com'; // This should be configurable
	window.location.href = `mailto:${supportEmail}?subject=Portal Access Issue&body=I need help with my portal access invitation.`;
};

// Retry verification
const retryVerification = () => {
	verificationState.value = 'form';
	errorMessage.value = '';
	errorDescription.value = '';
	form.value.email = '';
};

// Load data on mount
onMounted(() => {
	const token = route.query.token as string;
	if (!token) {
		ElMessage.error('Invalid invitation link');
		router.push('/');
	}
});
</script>

<style scoped lang="scss">
.portal-access-page {
	min-height: 100vh;
	display: flex;
	align-items: center;
	justify-content: center;
	background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.container {
	max-width: 500px;
	width: 100%;
	padding: 2rem;
	background: white;
	border-radius: 12px;
	box-shadow: 0 10px 30px rgba(0, 0, 0, 0.1);
}

.header {
	text-align: center;
	margin-bottom: 2rem;

	h1 {
		color: #333;
		margin-bottom: 0.5rem;
		font-size: 2rem;
		font-weight: 600;
	}

	p {
		color: #666;
		font-size: 1rem;
	}
}

.verification-form {
	.el-form-item {
		margin-bottom: 1.5rem;
	}

	.el-button {
		width: 100%;
	}
}

.success-message {
	:deep(.el-result__title) {
		color: #333;
	}

	:deep(.el-result__subtitle) {
		color: #666;
	}
}
</style> 