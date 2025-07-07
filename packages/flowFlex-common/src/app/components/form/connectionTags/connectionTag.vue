<template>
	<div class="w-full">
		<el-popover
			v-if="showPopover"
			ref="popoverRef"
			placement="right"
			:width="200"
			:hide-after="0"
			trigger="hover"
		>
			<template #reference>
				<div class="cursor-pointer truncate" @click="clickPopover">
					{{ valueName }}
				</div>
			</template>
			<div class="w-full">
				<el-link @click="viewAssociatedCompanies" class="flex w-full max-w-full truncate">
					{{ valueName || defaultStr }}
				</el-link>
			</div>
		</el-popover>
		<div v-else class="connection-tag">
			{{ valueName }}
		</div>
	</div>
</template>

<script lang="ts" setup>
import { computed, ref } from 'vue';
import { defaultStr } from '@/settings/projectSetting';
import { ElPopover } from 'element-plus';
import { useRouter } from 'vue-router';

const router = useRouter();

interface ConnectionTagProps {
	value: any;
	valueName: any;
	module: any;
}

const props = withDefaults(defineProps<ConnectionTagProps>(), {
	value: '',
	connectionData: () => [],
});

// 判断是否显示弹窗
const showPopover = computed(() => {
	return props.value && props.value !== '' && props.value !== undefined && props.value !== null;
});

const viewAssociatedCompanies = () => {
	if (!props.value) return;

	// 定义模块类型与URL路径的映射
	let path = '';

	// 只处理Contact、Company、Deal和Product这四种类型
	switch (Number(props.module)) {
		case 2: // Contact
			path = '/leads/contactDetails';
			break;
		case 3: // Company
			path = '/leads/companiesdetails';
			break;
		case 4: // Deal
			path = '/deals/details';
			break;
		case 5: // Product
			path = '/products';
			break;
		default:
			console.log('未支持的module类型:', props.module);
			return; // 不处理其他类型
	}

	// 统一执行路由跳转
	router.push({
		path,
		query: {
			id: props.value,
		},
	});
};

const popoverRef = ref<InstanceType<typeof ElPopover>>();
const clickPopover = () => {
	popoverRef.value?.hide();
};
</script>

<style lang="scss" scoped>
:deep(.connection-popover) {
	padding: 0;
	border-radius: 8px;
	box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
}

:deep(.el-link) {
	@apply block;
}
</style>
