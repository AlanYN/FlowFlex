<template>
	<div class="flex items-center">
		<el-tooltip
			:content="
				timeZoneList.find((item) => item.timeZoneId == initalizeTomeZone)?.timeZoneName ||
				getTimeZoneInfo().timeZone
			"
		>
			<TimeZone :class="{ hidden: props.setting }" />
		</el-tooltip>
		<el-select
			v-model="currentTimeZone"
			:placeholder="t('sys.placeholder.selectPlaceholder')"
			class="rounded-lg"
			:class="{ 'w-[400px] ': props.setting, 'w-[200px] ml-4': !props.setting }"
			@blur="!props.setting ? saveTimeZone() : null"
			filterable
			clearable
		>
			<el-option
				v-for="item in timeZoneList"
				:key="item.timeZoneId"
				:label="item.timeZoneName"
				:value="item.timeZoneId"
			/>
		</el-select>
	</div>
</template>

<script lang="ts" setup>
import { ref, h, nextTick, onMounted } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useI18n } from '@/hooks/useI18n';
import { useUserStore } from '@/stores/modules/user';
import { getTimeZoneList, updateTimeZone } from '@/apis/global/timeZone';
import { getTimeZoneInfo } from '@/hooks/time';

import TimeZone from '@assets/svg/global/timeZone.svg';

const { t } = useI18n();
const userStore = useUserStore();

interface TimeZoneOption {
	timeZoneId: string;
	timeZoneName: string;
}

const props = defineProps<{
	setting?: boolean;
}>();

// 时区
const initalizeTomeZone = ref('');
const currentTimeZone = ref('');
const timeZoneList = ref<TimeZoneOption[]>([]);

const getTimeList = () => {
	getTimeZoneList({
		userId: userStore.getUserInfo.userId,
	}).then((res) => {
		if (res.code == '200' || res.code == 1) {
			timeZoneList.value = res.data.timeZoneList;
			if (res.data.defaultTimeZone) {
				initalizeTomeZone.value = res.data.defaultTimeZone;
				currentTimeZone.value = res.data.defaultTimeZone;

				userStore.setUserInfo({
					...userStore.getUserInfo,
					defaultTimeZone: currentTimeZone.value,
				});
			} else {
				const TimeZoneObj = getTimeZoneInfo();
				initalizeTomeZone.value = TimeZoneObj.timeZone;
				currentTimeZone.value = TimeZoneObj.timeZone;
				if (
					timeZoneList.value.filter((item) => item.timeZoneId == TimeZoneObj.timeZone)
						.length > 0
				)
					return;
				timeZoneList.value.push({
					timeZoneId: TimeZoneObj.timeZone,
					timeZoneName: `(${TimeZoneObj.offset}) ${TimeZoneObj.timeZone}`,
				});
			}
		}
	});
};

const saveTimeZone = async () => {
	if (initalizeTomeZone.value == currentTimeZone.value || currentTimeZone.value == '') return;

	if (props.setting) {
		try {
			const res = await updateTimeZone({
				timeZone: currentTimeZone.value,
				userId: userStore.getUserInfo.userId,
			});
			if (res.code == '200' || res.code == 1) {
				// userStore.setUserInfo({
				// 	...userStore.getUserInfo,
				// 	defaultTimeZone: currentTimeZone.value,
				// });
				window.location.reload();
			}
		} catch (error) {
			ElMessage.error(t('sys.api.operationFailed'));
		}
		return;
	}

	ElMessageBox({
		title: 'Title',
		message: h('p', null, [
			h('span', null, 'It will be switched to the new time zone for you.'),
		]),
		showCancelButton: true,
		confirmButtonText: t('sys.app.logoutTip'),
		cancelButtonText: t('sys.app.cancelText'),
		beforeClose: async (action, instance, done) => {
			if (action === 'confirm') {
				instance.confirmButtonLoading = true;
				instance.confirmButtonText = 'Loading...';
				try {
					const res = await updateTimeZone({
						timeZone: currentTimeZone.value,
						userId: userStore.getUserInfo.userId,
					});
					instance.confirmButtonText = t('sys.app.yesText');
					instance.confirmButtonLoading = false;
					if (res.code == '200' || res.code == 1) {
						ElMessage.success(res.msg || t('sys.api.operationSuccess'));
						nextTick(async () => {
							userStore.setUserInfo({
								...userStore.getUserInfo,
								defaultTimeZone: currentTimeZone.value,
							});
							window.location.reload();
							done();
						});
					}
				} catch {
					instance.confirmButtonText = t('sys.app.yesText');
					instance.confirmButtonLoading = false;
					done();
				}
			} else {
				currentTimeZone.value = initalizeTomeZone.value;
				done();
			}
		},
	});
};

defineExpose({
	saveTimeZone,
});

onMounted(() => {
	getTimeList();
});
</script>
