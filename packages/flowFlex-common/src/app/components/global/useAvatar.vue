<template>
	<div class="h-full bg-primary-500 w-full rounded-full m-auto flex items-center justify-center">
		<img
			v-if="userStore?.getUserInfo?.avatarUrl"
			:src="userStore?.getUserInfo?.avatarUrl"
			alt="avatar"
			class="h-full w-full"
		/>
		<slot v-else name="default"></slot>
	</div>
</template>

<script setup lang="ts">
import { useUserStore } from '@/stores/modules/user';

const userStore = useUserStore();
</script>
