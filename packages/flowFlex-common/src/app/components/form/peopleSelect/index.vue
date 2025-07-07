<template>
	<el-select
		ref="selectRef"
		:model-value="modelValue"
		:placeholder="t('sys.placeholder.searchPlaceholder')"
		clearable
		filterable
		remote-show-suffix
		remote
		:remote-method="onSearch"
		:loading="loading"
		@update:model-value="emit('update:modelValue', $event)"
		@blur="emit('blur')"
		@change="emit('change', $event)"
	>
		<el-option
			v-for="item in options as StringKeyOptions[]"
			:key="item.key"
			:label="item.value"
			:value="item.key"
		>
			<div class="flex items-center gap-x-2 ml-[-10px]">
				<peopleTags :value="item.key" :options="options" />
				{{ item.value }} {{ item?.email }}
			</div>
		</el-option>
		<template #loading>
			<svg class="circular" viewBox="0 0 50 50">
				<circle class="path" cx="25" cy="25" r="20" fill="none" />
			</svg>
		</template>
	</el-select>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useI18n } from '@/hooks/useI18n';
import peopleTags from '@/components/form/peopleTags/index.vue';
import { ElSelect } from 'element-plus';
import { Options } from '#/setting';

const { t } = useI18n();

type StringKeyOptions = {
	[K in keyof Options]: K extends 'key' ? string : Options[K];
};

interface Props {
	modelValue: any;
	options: Options[];
	loading?: boolean;
}

interface Emits {
	(e: 'update:modelValue', value: string | number): void;
	(e: 'blur'): void;
	(e: 'change', value: string | number): void;
	(e: 'search', value: string): void;
}

defineProps<Props>();
const emit = defineEmits<Emits>();

const onSearch = (query: string) => {
	emit('search', query);
};

const selectRef = ref<InstanceType<typeof ElSelect>>();

const focus = () => {
	selectRef.value?.focus();
};

defineExpose({
	focus,
});
</script>
