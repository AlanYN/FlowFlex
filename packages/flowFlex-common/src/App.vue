<template>
	<div class="flowflex-app flex flex-col h-full" id="flowflex-root">
		<el-alert
			class="flex justify-between items-center"
			type="warning"
			effect="dark"
			center
			@close="handleClose"
			v-if="showTip"
		>
			<div>
				🔔 Domain Update : On March 31st,
				<span>{{ getCurrentBaseUrl() }}</span>
				will be migrated to
				<span>{{ getCurrentBaseUrl().replace('flowflex', 'workflow') }}</span>
				. Please update your bookmarks accordingly.
			</div>
		</el-alert>
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

const showTip = ref(
	window.location.origin.includes('flowflex') && localStorage.getItem('showTip') !== 'false'
);
const handleClose = () => {
	showTip.value = false;
	localStorage.setItem('showTip', 'false');
};

const getCurrentBaseUrl = () => {
	return window.location.origin;
};
</script>

<style scoped lang="scss"></style>
