<template>
	<div class="w-full h-[188px] cred-credit gap-3 flex-none order-1 self-stretch grow">
		<div class="green-zone relative">
			<div class="yellow-zone">
				<div class="red-zone">
					<div class="score">
						<CountTo :startVal="0" :endVal="creditScore || 0" class="text-3xl" />
						<div class="text-xs mt-2" style="color: rgba(94, 95, 95, 1)">
							{{ operateDate ? `AS OF ${operateDate}` : defaultStr }}
						</div>
						<div id="indicator" class="w-[114px] indicator_block">
							<div class="indicator" id="inner">
								<div class="inner"></div>
							</div>
						</div>
					</div>
				</div>
			</div>
		</div>
		<div class="flex items-center whitespace-nowrap">
			<div>Credit Score is</div>
			<div class="fair ml-2">
				<text>{{ determineCreditType(creditScore) }}</text>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import CountTo from '@/components/CountTo/CountTo.vue';
import { defaultStr } from '@/settings/projectSetting';

defineProps({
	creditScore: {
		type: Number,
		default: 0,
	},
	operateDate: {
		type: String,
		default: '',
	},
});

const determineCreditType = (creditScore) => {
	if (!creditScore) return defaultStr;
	if (creditScore > 1 && creditScore <= 29) {
		return 'High Risk';
	} else if (creditScore > 30 && creditScore <= 50) {
		return 'Moderate Risk';
	} else if (creditScore > 51 && creditScore <= 71) {
		return 'Low Risk';
	} else if (creditScore > 71 && creditScore <= 100) {
		return 'Very Low Risk';
	} else {
		return 'Not Rated';
	}
};

const rotateToAngle = (creditScore) => {
	const angle = (creditScore / 100) * 180;
	var indicator = document.getElementById('indicator') as HTMLDivElement;
	indicator.style.transform = `rotate(${angle}deg)`;
	let formBack = '#E52628';
	if (0 <= angle && angle < 70) {
		formBack = '#E52628';
	} else if (60 < angle && angle < 130) {
		formBack = '#f7be00';
	} else {
		formBack = '#018A16';
	}
	(document.getElementById('inner') as HTMLDivElement).style.background = formBack;
};

defineExpose({
	rotateToAngle,
});
</script>

<style lang="scss" scoped>
.cred-credit {
	display: flex;
	flex-direction: column;
	justify-content: center;
	align-items: center;
	padding: 20px;
	gap: 12px;
	width: 380px;
	height: 188px;
	flex: none;
	order: 1;
	align-self: stretch;
	flex-grow: 1;
	@apply rounded-xl;
}

.green-zone {
	box-sizing: border-box;
	display: flex;
	flex-direction: column;
	justify-content: center;
	align-items: center;
	padding: 0px;
	width: 212px;
	height: 116px;
	border-right: 4px solid #018a16;
	border-radius: 9999px 9999px 0px 0px;
	flex: none;
	order: 0;
	flex-grow: 0;
}

.yellow-zone {
	box-sizing: border-box;
	display: flex;
	flex-direction: column;
	justify-content: center;
	align-items: center;
	padding: 0px;
	gap: 10px;
	width: 212px;
	height: 116px;
	border-top: 4px solid #f7be00;
	border-radius: 9999px 9999px 0px 0px;
	flex: none;
	order: 0;
	flex-grow: 0;
}

.red-zone {
	box-sizing: border-box;
	display: flex;
	flex-direction: column;
	justify-content: center;
	align-items: center;
	padding: 16px 16px 0px;
	gap: 10px;
	width: 212px;
	height: 116px;
	border-left: 4px solid #e52628;
	border-radius: 9999px 9999px 0px 0px;
	flex: none;
	order: 0;
	flex-grow: 0;
}

.score {
	box-sizing: border-box;
	display: flex;
	flex-direction: column;
	justify-content: center;
	align-items: center;
	padding: 32px 32px 0px;
	isolation: isolate;
	width: 180px;
	height: 100px;
	border-top: 1px dashed #989a9c;
	border-radius: 9999px 9999px 0px 0px;
	flex: none;
	order: 0;
	flex-grow: 0;
}

.indicator_block {
	position: absolute;
	right: 103px;
	bottom: 0;
	transform-origin: right center;
	transition: all 2s ease;
}

.indicator {
	display: flex;
	flex-direction: row;
	align-items: flex-start;
	padding: 4px;
	width: 16px;
	height: 16px;
	border-radius: 9999px;
	background: #e52628;
	flex: none;
	order: 2;
	flex-grow: 0;
	z-index: 2;
	transition: all 2s ease;
}

.inner {
	width: 8px;
	height: 8px;
	background: #ffffff;
	flex: none;
	order: 0;
	flex-grow: 0;
	border-radius: 50%;
}

.fair {
	display: flex;
	flex-direction: row;
	justify-content: center;
	align-items: center;
	padding: 0px 10px;
	gap: 10px;
	height: 20px;
	background: #fefce8;
	flex: none;
	order: 1;
	flex-grow: 0;
	@apply rounded-xl;

	text {
		height: 20px;
		font-style: normal;
		font-weight: 700;
		font-size: 14px;
		line-height: 20px;
		display: flex;
		align-items: center;
		text-align: center;
		color: #ff9900;
		flex: none;
		order: 0;
		flex-grow: 0;
	}
}
</style>
