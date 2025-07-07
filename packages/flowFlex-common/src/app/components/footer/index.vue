<template>
	<div class="w-full footer p-8">
		<div>© 2024 {{ environment }}</div>
		<div class="flex items-center">
			<el-button text>Help</el-button>
			<div class="w-px h-3 bg-zinc-400"></div>
			<el-button text>Api</el-button>
			<div class="w-px h-3 bg-zinc-400"></div>
			<el-button text>Feedback</el-button>
			<div class="w-px h-3 bg-zinc-400"></div>
			<el-button text>Download</el-button>
		</div>
	</div>
</template>

<script lang="ts" setup>
import { useGlobSetting } from '@/settings';

const { environment } = useGlobSetting();
</script>

<style lang="scss" scoped>
.footer {
	position: absolute;
	left: 0;
	bottom: 0;
	right: 0;
	width: 100%;
	display: flex;
	justify-content: space-between;
	align-items: center;
	transition: all 0.3s;
	z-index: 100;
}
</style>
