<template>
	<div v-if="username" class="w-full flex items-center gap-2">
		<el-avatar :size="size" :src="src" :icon="UserFilled" />
		<span>{{ username }}</span>
	</div>
</template>

<script lang="ts" setup>
import { UserFilled } from '@element-plus/icons-vue';

defineProps<{
	size: number;
	src: string;
	username: string;
}>();
</script>
