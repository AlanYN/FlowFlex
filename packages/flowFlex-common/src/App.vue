<template>
	<div class="flowflex-app" id="flowflex-root">
		<ElConfigProvider :popup-container="getPopupContainer">
			<router-view :key="routeKey" />
		</ElConfigProvider>
	</div>
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
const getPopupContainer = () => document.querySelector('#app-root') as HTMLElement | null;

// const chatDialogRef = ref<InstanceType<typeof ChatDialog>>();

// const openChatDialog = (show: boolean, title: string, ...arg) => {
// 	chatDialogRef.value?.openDialog(show, title, arg);
// };

// provide('openChatDialog', openChatDialog);
</script>

<style scoped lang="scss"></style>
