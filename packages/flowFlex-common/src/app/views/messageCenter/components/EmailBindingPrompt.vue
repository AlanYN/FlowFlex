<template>
	<div class="flex items-center justify-center min-h-[600px] bg-black-400 rounded-xl">
		<div class="max-w-md w-full p-8 text-center">
			<div class="mb-6">
				<div
					class="w-20 h-20 mx-auto bg-primary-500/10 rounded-full flex items-center justify-center mb-4"
				>
					<Icon icon="lucide-mail" class="w-10 h-10 text-primary-500" />
				</div>
				<h2 class="text-2xl font-bold mb-2">Connect Your Email</h2>
				<p class="text-gray-400">
					Connect your email account to unlock full email capabilities in the Message
					Center.
				</p>
			</div>

			<div class="space-y-4 mb-6">
				<div class="flex items-start gap-3 text-left">
					<div class="mt-1">
						<Icon icon="lucide-check-circle" class="w-5 h-5 text-primary-500" />
					</div>
					<div>
						<h3 class="font-medium mb-1">Send & Receive Emails</h3>
						<p class="text-sm text-gray-400">
							Compose and send emails directly from the Message Center
						</p>
					</div>
				</div>
				<div class="flex items-start gap-3 text-left">
					<div class="mt-1">
						<Icon icon="lucide-check-circle" class="w-5 h-5 text-primary-500" />
					</div>
					<div>
						<h3 class="font-medium mb-1">Real-time Sync</h3>
						<p class="text-sm text-gray-400">
							Automatically sync emails with your email provider
						</p>
					</div>
				</div>
				<div class="flex items-start gap-3 text-left">
					<div class="mt-1">
						<Icon icon="lucide-check-circle" class="w-5 h-5 text-primary-500" />
					</div>
					<div>
						<h3 class="font-medium mb-1">Secure Connection</h3>
						<p class="text-sm text-gray-400">
							Your email credentials are encrypted and secure
						</p>
					</div>
				</div>
			</div>

			<el-button
				type="primary"
				size="large"
				class="w-full"
				:loading="loading"
				:disabled="!authUrl"
				@click="handleConnect"
			>
				<Icon v-if="!loading" icon="lucide-link" class="w-5 h-5 mr-2" />
				{{ loading ? 'Waiting for authorization...' : 'Connect Email Account' }}
			</el-button>

			<p class="text-xs text-gray-500 mt-4">
				By connecting, you agree to allow access to your email for message management
			</p>
		</div>
	</div>
</template>

<script lang="ts" setup>
interface Props {
	authUrl: string;
	loading?: boolean;
}

const props = defineProps<Props>();

const emit = defineEmits<{
	connect: [];
}>();

const handleConnect = () => {
	if (!props.authUrl) {
		return;
	}
	emit('connect');
};
</script>
