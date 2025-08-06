<template>
	<ElConfigProvider :popup-container="(triggerNode) => triggerNode?.parentNode">
		<router-view :key="routeKey" />
	</ElConfigProvider>
</template>

<script lang="ts" setup>
import { ref, nextTick, provide } from 'vue';
import { useTitle } from '@/hooks/web/useTitle';
import { ElConfigProvider } from 'element-plus';
// import ChatDialog from '@/components/ChatWindow/ChatDialog.vue';

useTitle();

provide('refreshRoute', () => refreshRoute());

const routeKey = ref(Date.now());

// 无感刷新路由
const refreshRoute = () => {
	nextTick(() => {
		// 更改 key 值以刷新路由
		routeKey.value = Date.now();
	});
};

// const chatDialogRef = ref<InstanceType<typeof ChatDialog>>();

// const openChatDialog = (show: boolean, title: string, ...arg) => {
// 	chatDialogRef.value?.openDialog(show, title, arg);
// };

// provide('openChatDialog', openChatDialog);
</script>

<style scoped lang="scss"></style>
