<template>
	<div class="mt-6">
		<div class="flex flex-col gap-4">
			<div class="flex justify-between items-center mb-2">
				<label class="">Rating Configuration</label>
			</div>

			<!-- 评分设置区域 -->
			<div class="flex gap-8">
				<!-- 左侧：评分数量设置 -->
				<div class="flex-1 min-w-0">
					<div class="mb-4">
						<h4 class="text-base font-medium m-0">Rating Scale</h4>
					</div>
					<div class="flex flex-col gap-3">
						<div class="flex items-center gap-3">
							<label class="text-sm font-medium min-w-[100px]">Maximum Score</label>
							<el-input-number
								:model-value="max"
								:min="2"
								:max="10"
								:step="1"
								@change="updateMax"
								class="w-[120px]"
							/>
						</div>
						<div class="p-2 bg-primary rounded-xl">
							<span class="text-sm font-medium">Scale: 1 - {{ max }}</span>
						</div>
					</div>
				</div>

				<!-- 右侧：图标类型设置 -->
				<div class="flex-1 min-w-0">
					<div class="mb-4">
						<h4 class="text-base font-medium m-0">Icon Type</h4>
					</div>
					<div class="flex flex-col gap-3">
						<div class="flex gap-3 flex-wrap">
							<div
								v-for="iconOption in iconOptions"
								:key="iconOption.value"
								class="flex flex-col items-center gap-2 p-3 rounded-xl cursor-pointer transition-all bg-white dark:bg-black min-w-[80px] hover:border-primary-400 hover:bg-primary-25 dark:hover:border-primary-400 dark:hover:bg-primary-600"
								:class="{
									'border-primary-600 bg-primary-50 dark:border-primary-400 dark:bg-primary-600':
										iconType === iconOption.value,
								}"
								@click="updateIconType(iconOption.value)"
							>
								<el-icon class="text-primary-600 dark:text-primary-300" :size="20">
									<component :is="iconOption.icon" />
								</el-icon>
								<span
									class="text-xs font-medium text-primary-700 dark:text-primary-200 text-center"
								>
									{{ iconOption.label }}
								</span>
							</div>
						</div>
					</div>
				</div>
			</div>

			<!-- 预览区域 -->
			<div class="mt-6 pt-4 border-t border-primary-100 dark:border-primary-600">
				<div class="mb-4">
					<h4 class="text-base font-medium text-primary-800 dark:text-primary-200 m-0">
						Preview
					</h4>
				</div>
				<div class="flex items-center gap-3 p-4 rounded-xl">
					<el-rate
						v-model="previewValue"
						:max="max"
						:icons="getSelectedFilledIcon()"
						:void-icon="getSelectedVoidIcon()"
						:colors="ratingColors"
					/>
					<span class="text-sm text-primary-600 dark:text-primary-200 font-medium">
						({{ max }} {{ getIconLabel() }})
					</span>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
// 使用 MDI 图标库
import IconStar from '~icons/mdi/star';
import IconStarOutline from '~icons/mdi/star-outline';
import IconHeart from '~icons/mdi/heart';
import IconHeartOutline from '~icons/mdi/heart-outline';
import IconThumbUp from '~icons/mdi/thumb-up';
import IconThumbUpOutline from '~icons/mdi/thumb-up-outline';

interface Props {
	max: number;
	iconType: string;
}

const props = defineProps<Props>();
const previewValue = ref(Math.ceil(props.max / 2)); // 设置为最大值的一半，确保能看到图标效果

const emit = defineEmits<{
	'update-max': [value: number];
	'update-icon-type': [type: string];
}>();

// 动态获取 Element Plus 颜色
const ratingColors = computed(() => {
	const primary = getComputedStyle(document.documentElement)
		.getPropertyValue('--el-color-primary')
		.trim();
	const success = getComputedStyle(document.documentElement)
		.getPropertyValue('--el-color-success')
		.trim();
	const warning = getComputedStyle(document.documentElement)
		.getPropertyValue('--el-color-warning')
		.trim();
	return [primary, success, warning];
});

// 图标选项
const iconOptions = [
	{
		value: 'star',
		label: 'Star',
		icon: IconStar,
		filledIcon: [IconStar, IconStar, IconStar],
		voidIcon: IconStarOutline,
	},
	{
		value: 'heart',
		label: 'Heart',
		icon: IconHeart,
		filledIcon: [IconHeart, IconHeart, IconHeart],
		voidIcon: IconHeartOutline,
	},
	{
		value: 'thumbs',
		label: 'Thumbs',
		icon: IconThumbUp,
		filledIcon: [IconThumbUp, IconThumbUp, IconThumbUp],
		voidIcon: IconThumbUpOutline,
	},
];

const updateMax = (value: number) => {
	emit('update-max', value);
};

const updateIconType = (type: string) => {
	emit('update-icon-type', type);
};

const getSelectedFilledIcon = () => {
	const selectedOption =
		iconOptions.find((option) => option.value === props.iconType) || iconOptions[0];
	return selectedOption?.filledIcon;
};

const getSelectedVoidIcon = () => {
	const selectedOption = iconOptions.find((option) => option.value === props.iconType);
	return selectedOption ? selectedOption.voidIcon : IconStarOutline;
};

const getIconLabel = () => {
	const selectedOption = iconOptions.find((option) => option.value === props.iconType);
	return selectedOption ? selectedOption.label.toLowerCase() + 's' : 'stars';
};
</script>
