<template>
	<div class="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center p-4">
		<div class="max-w-md w-full bg-white rounded-lg shadow-lg overflow-hidden">
			<!-- Header -->
			<div class="bg-gradient-to-r from-blue-500 to-indigo-600 px-6 py-4">
				<h1 class="text-xl font-semibold text-white text-center">
					Portal Access Verification
				</h1>
			</div>

			<!-- Content -->
			<div class="p-6">
				<!-- Form State -->
				<div v-if="verificationState === 'form'" class="space-y-4">
					<div class="text-center mb-6">
						<div class="w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center mx-auto mb-3">
							<svg class="w-8 h-8 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"></path>
							</svg>
						</div>
						<p class="text-gray-600">
							Please enter your email address to verify your invitation and access the customer portal.
						</p>
					</div>

					<el-form ref="formRef" :model="form" :rules="rules" label-position="top">
						<el-form-item label="Email Address" prop="email">
							<el-input
								v-model="form.email"
								type="email"
								placeholder="Enter your email address"
								size="large"
								:disabled="loading"
							/>
					</el-form-item>

						<el-button
							type="primary"
							size="large"
							class="w-full"
							@click="handleVerify"
							:loading="loading"
						>
							Verify Access
						</el-button>
					</el-form>
				</div>

				<!-- Login State -->
				<div v-if="verificationState === 'login'" class="space-y-4">
					<div class="text-center mb-6">
						<div class="w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center mx-auto mb-3">
							<svg class="w-8 h-8 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"></path>
							</svg>
						</div>
						<h3 class="text-lg font-semibold text-gray-800 mb-2">Welcome Back!</h3>
						<p class="text-gray-600">
							Please enter your password to access the customer portal.
						</p>
					</div>

					<el-form ref="loginFormRef" :model="loginForm" :rules="loginRules" label-position="top">
						<el-form-item label="Email Address">
							<el-input
								v-model="form.email"
								type="email"
								size="large"
								disabled
								class="disabled-input"
							/>
						</el-form-item>

						<el-form-item label="Password" prop="password">
							<el-input
								v-model="loginForm.password"
								type="password"
								placeholder="Enter your password"
								size="large"
								show-password
								:disabled="loggingIn"
							/>
						</el-form-item>

						<el-button
							type="primary"
							size="large"
							class="w-full"
							@click="handleLogin"
							:loading="loggingIn"
						>
							Login & Continue
						</el-button>
					</el-form>
				</div>

				<!-- Registration State -->
				<div v-if="verificationState === 'registration'" class="space-y-4">
					<div class="text-center mb-6">
						<div class="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-3">
							<svg class="w-8 h-8 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18 9v3m0 0v3m0-3h3m-3 0h-3m-2-5a4 4 0 11-8 0 4 4 0 018 0zM3 20a6 6 0 0112 0v1H3v-1z"></path>
							</svg>
						</div>
						<h3 class="text-lg font-semibold text-gray-800 mb-2">Welcome! Set Your Password</h3>
						<p class="text-gray-600">
							Your email address is not registered yet. Please set a password to create your account and access the customer portal.
						</p>
					</div>

					<el-form ref="registrationFormRef" :model="registrationForm" :rules="registrationRules" label-position="top">
						<el-form-item label="Email Address">
							<el-input
								v-model="form.email"
								type="email"
								size="large"
								disabled
								class="disabled-input"
							/>
						</el-form-item>

						<el-form-item label="Password" prop="password">
							<el-input
								v-model="registrationForm.password"
								type="password"
								placeholder="Enter your password (minimum 6 characters)"
								size="large"
								show-password
								:disabled="registering"
							/>
						</el-form-item>

						<el-form-item label="Confirm Password" prop="confirmPassword">
							<el-input
								v-model="registrationForm.confirmPassword"
								type="password"
								placeholder="Confirm your password"
								size="large"
								show-password
								:disabled="registering"
							/>
					</el-form-item>

						<el-button
							type="primary"
							size="large"
							class="w-full"
							@click="handleRegisterAndLogin"
							:loading="registering"
						>
							Create Account & Continue
						</el-button>
				</el-form>
			</div>

				<!-- Success State -->
				<div v-if="verificationState === 'success'" class="text-center space-y-4">
					<div class="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto">
						<svg class="w-8 h-8 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"></path>
						</svg>
					</div>
					<h3 class="text-lg font-semibold text-gray-800">{{ successMessage }}</h3>
					<p class="text-gray-600">
						You will be redirected to the customer portal shortly.
					</p>
					<el-button
						type="primary"
						size="large"
						@click="redirectToCustomerPortal"
						class="w-full"
					>
						Continue to Customer Portal
					</el-button>
				</div>

				<!-- Error State -->
				<div v-if="verificationState === 'error'" class="text-center space-y-4">
					<div class="w-16 h-16 bg-red-100 rounded-full flex items-center justify-center mx-auto">
						<svg class="w-8 h-8 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.082 14.5c-.77.833.192 2.5 1.732 2.5z"></path>
						</svg>
					</div>
					<h3 class="text-lg font-semibold text-red-800">{{ errorMessage }}</h3>
					<p class="text-gray-600">{{ errorDescription }}</p>
					<div class="flex gap-3">
						<el-button @click="retryVerification" class="flex-1">
							Try Again
						</el-button>
						<el-button type="primary" @click="contactSupport" class="flex-1">
							Contact Support
						</el-button>
					</div>
				</div>

				<!-- Expired State -->
				<div v-if="verificationState === 'expired'" class="text-center space-y-4">
					<div class="w-16 h-16 bg-orange-100 rounded-full flex items-center justify-center mx-auto">
						<svg class="w-8 h-8 text-orange-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path>
						</svg>
					</div>
					<h3 class="text-lg font-semibold text-orange-800">Invitation Expired</h3>
					<p class="text-gray-600">
						This invitation link has expired. Please contact the organization to request a new invitation.
					</p>
					<div class="flex gap-3">
						<el-button @click="requestNewInvitation" class="flex-1">
							Request New Invitation
						</el-button>
						<el-button type="primary" @click="contactSupport" class="flex-1">
							Contact Support
						</el-button>
					</div>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, type FormInstance } from 'element-plus';
import * as userInvitationApi from '@/apis/ow/userInvitation';
import * as userApi from '@/apis/ow/user';

// Router and route
const route = useRoute();
const router = useRouter();

// Refs
const formRef = ref<FormInstance>();
const registrationFormRef = ref<FormInstance>();
const loginFormRef = ref<FormInstance>();

// States
const loading = ref(false);
const registering = ref(false);
const loggingIn = ref(false);
const verificationState = ref<'form' | 'login' | 'registration' | 'success' | 'error' | 'expired'>('form');
const isVerified = ref(false);
const successMessage = ref('');
const errorMessage = ref('');
const errorDescription = ref('');

// Form data
const form = ref({
	email: '',
});

const loginForm = ref({
	password: '',
});

const registrationForm = ref({
	password: '',
	confirmPassword: '',
});

// Validation rules
const rules = {
	email: [
		{ required: true, message: 'Please enter your email address', trigger: 'blur' },
		{ type: 'email' as const, message: 'Please enter a valid email address', trigger: 'blur' },
	],
};

const loginRules = {
	password: [
		{ required: true, message: 'Please enter your password', trigger: 'blur' },
	],
};

const registrationRules = {
	password: [
		{ required: true, message: 'Please enter your password', trigger: 'blur' },
		{ min: 6, message: 'Password must be at least 6 characters long', trigger: 'blur' },
	],
	confirmPassword: [
		{ required: true, message: 'Please confirm your password', trigger: 'blur' },
		{
			validator: (_rule: any, value: string, callback: any) => {
				if (value !== registrationForm.value.password) {
					callback(new Error('Passwords do not match'));
				} else {
					callback();
				}
			},
			trigger: 'blur',
		},
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

		// First verify the portal access
		const response = await userInvitationApi.verifyPortalAccess({
			token,
			email: form.value.email,
		});

		const verificationData = response?.data || response;

		if (verificationData.isValid) {
			// Store portal access token
			localStorage.setItem('portal_access_token', verificationData.accessToken);
			localStorage.setItem('onboarding_id', verificationData.onboardingId.toString());

			// Check if user exists
			const emailExistsResponse = await userApi.checkEmailExists(form.value.email);
			const emailExists = emailExistsResponse?.data || emailExistsResponse;

			if (emailExists) {
				// User exists - we need to prompt for password to login
				verificationState.value = 'login';
			} else {
				// User doesn't exist, show registration form
				verificationState.value = 'registration';
			}
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

const handleRegisterAndLogin = async () => {
	if (!registrationFormRef.value) return;

	try {
		await registrationFormRef.value.validate();
		registering.value = true;

		const onboardingId = localStorage.getItem('onboarding_id');
		if (!onboardingId) {
			throw new Error('Onboarding ID not found');
		}

		// Register the user
		await userApi.portalAutoRegisterAndLogin({
			email: form.value.email,
			password: registrationForm.value.password,
			onboardingId,
		});

		// Login the user
		const loginResponse = await userApi.loginUser({
			email: form.value.email,
			password: registrationForm.value.password,
		});

		const loginData = loginResponse?.data || loginResponse;
		
		// Store user token for future use (this is different from portal_access_token)
		localStorage.setItem('user_access_token', loginData.accessToken);
		localStorage.setItem('user_info', JSON.stringify(loginData.user));

		// Show success
		verificationState.value = 'success';
		successMessage.value = 'Account created successfully! Welcome to the customer portal.';
		
		// Auto redirect after 2 seconds
		setTimeout(() => {
			redirectToCustomerPortal();
		}, 2000);

	} catch (error: any) {
		console.error('Registration error:', error);
		ElMessage.error(error?.response?.data?.msg || error?.message || 'Failed to create account. Please try again.');
	} finally {
		registering.value = false;
	}
};

const handleLogin = async () => {
	if (!loginFormRef.value) return;

	try {
		await loginFormRef.value.validate();
		loggingIn.value = true;

		const loginResponse = await userApi.loginUser({
			email: form.value.email,
			password: loginForm.value.password,
		});

		const loginData = loginResponse?.data || loginResponse;

		// Store user token for future use (this is different from portal_access_token)
		localStorage.setItem('user_access_token', loginData.accessToken);
		localStorage.setItem('user_info', JSON.stringify(loginData.user));

		// Show success
		verificationState.value = 'success';
		successMessage.value = 'Login successful! Welcome to the customer portal.';
		
		// Auto redirect after 2 seconds
		setTimeout(() => {
			redirectToCustomerPortal();
		}, 2000);

	} catch (error: any) {
		console.error('Login error:', error);
		ElMessage.error(error?.response?.data?.msg || error?.message || 'Failed to login. Please check your password and try again.');
	} finally {
		loggingIn.value = false;
	}
};

const redirectToCustomerPortal = () => {
	const onboardingId = localStorage.getItem('onboarding_id');
	if (onboardingId) {
		router.push(`/customer-portal?onboardingId=${onboardingId}`);
	} else {
		router.push('/customer-portal');
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
	ElMessage.info('Please contact the organization that sent you the original invitation to request a new link.');
};

// Contact support
const contactSupport = () => {
	const supportEmail = 'support@flowflex.com';
	window.location.href = `mailto:${supportEmail}?subject=Portal Access Issue&body=I need help with my portal access invitation.`;
};

// Retry verification
const retryVerification = () => {
	verificationState.value = 'form';
	errorMessage.value = '';
	errorDescription.value = '';
	form.value.email = '';
	loginForm.value.password = '';
	registrationForm.value.password = '';
	registrationForm.value.confirmPassword = '';
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
.disabled-input {
	:deep(.el-input__inner) {
		background-color: #f5f7fa;
		color: #909399;
	}
}
</style> 