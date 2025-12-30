<template>
	<el-card class="mb-6">
		<el-form :model="searchParams" @submit.prevent="handleSearch">
			<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
				<div class="space-y-2">
					<label class="text-sm font-medium">Field Name</label>
					<InputTag
						v-model="fieldNameTags"
						placeholder="Enter Field Name and press enter"
						style-type="normal"
						:limit="10"
						clearable
						@change="handleFieldNameTagsChange"
						class="w-full"
					/>
				</div>

				<div class="space-y-2">
					<label class="text-sm font-medium">Field Type</label>
					<el-select
						v-model="searchParams.dataType"
						placeholder="Select Field Type"
						clearable
						class="w-full filter-select"
						@change="handleAutoSearch"
					>
						<el-option
							v-for="type in fieldsTypeEnum"
							:key="type.key"
							:label="type.value"
							:value="type.key"
						/>
					</el-select>
				</div>

				<div class="space-y-2">
					<label class="text-sm font-medium">Created By</label>
					<InputTag
						v-model="createdByTags"
						placeholder="Enter User Name and press enter"
						style-type="normal"
						:limit="10"
						clearable
						@change="handleCreatedByTagsChange"
						class="w-full"
					/>
				</div>

				<div class="space-y-2">
					<label class="text-sm font-medium">Updated By</label>
					<InputTag
						v-model="updatedByTags"
						placeholder="Enter User Name and press enter"
						style-type="normal"
						:limit="10"
						clearable
						@change="handleUpdatedByTagsChange"
						class="w-full"
					/>
				</div>
			</div>
		</el-form>
	</el-card>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import InputTag from '@/components/global/u-input-tags/index.vue';
import { fieldsTypeEnum } from '@/enums/appEnum';

// 搜索参数接口
interface DynamicFieldSearchParams {
	fieldName: string;
	dataType: string;
	createBy: string;
	updateBy: string;
}

// Props
interface Props {
	loading?: boolean;
}

withDefaults(defineProps<Props>(), {
	loading: false,
});

// Emits
const emit = defineEmits<{
	search: [params: DynamicFieldSearchParams];
	export: [];
}>();

// 搜索参数
const searchParams = reactive<DynamicFieldSearchParams>({
	fieldName: '',
	dataType: '',
	createBy: '',
	updateBy: '',
});

// 标签数组
const fieldNameTags = ref<string[]>([]);
const createdByTags = ref<string[]>([]);
const updatedByTags = ref<string[]>([]);

// 防抖搜索
let searchTimeout: any = null;

// 自动搜索函数
const handleAutoSearch = () => {
	// 清除之前的定时器
	if (searchTimeout) {
		clearTimeout(searchTimeout);
	}
	// 设置新的定时器，实现防抖
	searchTimeout = setTimeout(() => {
		handleSearch();
	}, 300);
};

// 标签变化处理函数
const handleFieldNameTagsChange = (tags: string[]) => {
	searchParams.fieldName = tags.join(',');
	handleAutoSearch();
};

const handleCreatedByTagsChange = (tags: string[]) => {
	searchParams.createBy = tags.join(',');
	handleAutoSearch();
};

const handleUpdatedByTagsChange = (tags: string[]) => {
	searchParams.updateBy = tags.join(',');
	handleAutoSearch();
};

// 事件处理函数
const handleSearch = () => {
	// 将标签数组转换为搜索参数
	const searchParamsWithTags = {
		...searchParams,
	};
	emit('search', searchParamsWithTags);
};

const handleExport = () => {
	emit('export');
};

// 暴露给父组件的方法
defineExpose({
	handleSearch,
	handleExport,
});
</script>
