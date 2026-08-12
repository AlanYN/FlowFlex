<template>
	<!-- FAB replay button — fixed bottom-right, rendered via Teleport -->
	<Teleport :to="fabTarget">
		<Transition name="ff-tour-fab">
			<el-tooltip
				v-if="showFab"
				content="View guided tour"
				placement="left"
				:show-after="300"
			>
				<button
					class="ff-tour-fab"
					:class="{ 'ff-tour-fab--running': isRunning }"
					aria-label="View guided tour"
					@click="handleFabClick"
				>
					<span class="ff-tour-fab__icon">?</span>
				</button>
			</el-tooltip>
		</Transition>
	</Teleport>
</template>

<script setup lang="ts">
import { nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useTourGuide, type TourStep } from '@/hooks/useTourGuide';

// ─── Props ─────────────────────────────────────────────────────────────────────

interface Props {
	/**
	 * Unique key for localStorage persistence.
	 * Should include user + context identifiers so each context (user × stage × onboarding)
	 * has its own completion state.
	 * Example: `${userId}_${onboardingId}_${stageId}`
	 */
	persistKey: string;
	/** Tour steps to render. Built externally so the parent controls content. */
	steps: TourStep[];
	/**
	 * Auto-start the tour when mounted (or when persistKey changes).
	 * The tour only fires if the current persistKey has NOT been completed.
	 * @default true
	 */
	autoStart?: boolean;
	/** Show the floating "?" replay button at bottom-right. @default true */
	showFab?: boolean;
	/**
	 * Teleport target for the "?" FAB — CSS selector, element, or a getter
	 * function returning either. Defaults to `body`. Pass a getter that
	 * resolves to the specific dialog's overlay so the FAB renders inside
	 * that dialog and stays below the dialog's own z-index layer.
	 */
	fabContainer?: string | HTMLElement | (() => string | HTMLElement | null);
	/** Custom label for the FAB hover tooltip. Falls back to i18n key. */
	fabTooltip?: string;
	/**
	 * Optional getter for the scrollable content container (el-scrollbar's inner wrap).
	 * When provided, Driver.js auto-scroll is disabled and we scroll manually,
	 * preventing window-level scroll issues when content is inside el-scrollbar.
	 * Example: `() => leftScrollbarRef?.$el?.querySelector('.el-scrollbar__wrap')`
	 */
	getScrollContainer?: () => HTMLElement | null;
	/**
	 * Optional async function to check whether the current user has already
	 * seen the tour on the backend (account-level).
	 * If it resolves to true the tour will NOT auto-start.
	 */
	checkSeenRemote?: () => Promise<boolean>;
	/**
	 * Optional async function to mark the tour as seen on the backend.
	 * Called when the tour completes or is skipped.
	 */
	markSeenRemote?: () => Promise<void>;
}

const props = withDefaults(defineProps<Props>(), {
	autoStart: true,
	showFab: true,
	fabTooltip: '',
	fabContainer: 'body',
});

// Resolve the Teleport target for the "?" FAB. Getter-based targets are
// resolved after mount (and re-resolved a tick later) so the FAB lands in the
// dialog's overlay even if the dialog content mounts slightly after this
// component; non-function targets are used as-is.
const fabTarget = ref<string | HTMLElement | null>(
	typeof props.fabContainer === 'function' ? 'body' : props.fabContainer
);

function _resolveFabTarget() {
	if (typeof props.fabContainer !== 'function') return;
	fabTarget.value = props.fabContainer() ?? 'body';
}

// ─── Emits ─────────────────────────────────────────────────────────────────────

const emit = defineEmits<{
	/** Tour completed — user clicked Done on the last step */
	complete: [];
	/** Tour skipped — user closed/skipped before finishing */
	skip: [];
}>();

// ─── Internal state ────────────────────────────────────────────────────────────

// We keep a reactive reference to the active tour instance.
// When persistKey changes (e.g. user switches stage), we re-create the instance.
const isRunning = ref(false);
const isCompleted = ref(false);

// Current useTourGuide instance — recreated when persistKey changes
let _tourInstance = _createInstance(props.persistKey);

function _createInstance(key: string) {
	const instance = useTourGuide({
		persistKey: key,
		onComplete: () => {
			isCompleted.value = true;
			emit('complete');
		},
		onSkip: () => emit('skip'),
		getScrollContainer: props.getScrollContainer,
		checkSeenRemote: props.checkSeenRemote,
		markSeenRemote: props.markSeenRemote,
	});
	// Sync reactive refs
	isRunning.value = instance.isRunning.value;
	isCompleted.value = instance.isCompleted.value;
	return instance;
}

// Re-create when persistKey changes, then conditionally auto-start
watch(
	() => props.persistKey,
	(newKey) => {
		_tourInstance = _createInstance(newKey);
		if (props.autoStart) {
			_autoStart();
		}
	},
	{ immediate: true }
);
// ─── Auto-start with retry ─────────────────────────────────────────────────

/**
 * Auto-start the tour once the async-rendered content (questionnaire /
 * checklist / fields) has mounted. Retries a few times in case the anchors
 * appear slightly later than the tour component itself.
 */
async function _autoStart() {
	const instance = _tourInstance;
	// Give late-rendering async content a moment to mount.
	await new Promise((resolve) => setTimeout(resolve, 300));
	let attempts = 0;
	const tryStart = async () => {
		if (instance.isCompleted.value || instance.isRunning.value) return;
		const started = await instance.startTour(props.steps);
		if (!started && attempts < 4) {
			attempts++;
			await new Promise((resolve) => setTimeout(resolve, 400));
			await tryStart();
		}
	};
	await tryStart();
}

// Stop a running tour when the component unmounts (e.g. switching stages)
onBeforeUnmount(() => {
	_tourInstance.stopTour();
});

// Re-resolve getter-based FAB targets after mount: the dialog content may
// mount one tick later, and the FAB must still land inside its overlay.
onMounted(() => {
	if (typeof props.fabContainer === 'function') {
		nextTick(_resolveFabTarget);
	}
});

// ─── Handlers ──────────────────────────────────────────────────────────────────

function handleFabClick() {
	_tourInstance.replayTour(props.steps).then(() => {
		isRunning.value = _tourInstance.isRunning.value;
	});
}

// ─── Expose ────────────────────────────────────────────────────────────────────

defineExpose({
	/** Manually start the tour (respects completion state) */
	startTour: () =>
		_tourInstance.startTour(props.steps).then(() => {
			isRunning.value = _tourInstance.isRunning.value;
		}),
	/** Force-replay the tour regardless of completion state */
	replayTour: () =>
		_tourInstance.replayTour(props.steps).then(() => {
			isRunning.value = _tourInstance.isRunning.value;
		}),
	/** Clear completion state for current persistKey */
	resetCompleted: () => _tourInstance.resetCompleted(),
	/** Whether the tour is currently running */
	isRunning,
	/** Whether the tour has been completed for the current persistKey */
	isCompleted,
});
</script>

<style scoped lang="scss">
// ─── FAB Button ────────────────────────────────────────────────────────────────

.ff-tour-fab {
	position: fixed;
	bottom: 24px;
	right: 68px;
	// 保持低于 Element Plus 弹窗/抽屉遮罩（useZIndex 默认从 2000 起递增），
	// 弹窗打开时 "?" 被遮罩盖住，符合"弹窗层级更高"的交互原则；
	// 同时低于 driver.js overlay（z-index: 10000），不会遮挡引导层。
	z-index: 2000;

	display: flex;
	align-items: center;
	justify-content: center;

	width: 36px;
	height: 36px;
	border-radius: 50%;
	border: none;
	cursor: pointer;

	background: var(--el-color-primary);
	color: #fff;
	box-shadow: 0 4px 14px rgba(0, 0, 0, 0.2);

	transition:
		transform 0.2s ease,
		box-shadow 0.2s ease,
		background 0.2s ease;

	// Accessibility focus ring
	&:focus-visible {
		outline: 2px solid var(--el-color-primary);
		outline-offset: 3px;
	}

	&:hover {
		transform: scale(1.1);
		box-shadow: 0 6px 20px rgba(0, 0, 0, 0.25);
		background: var(--el-color-primary-dark-2);
	}

	&:active {
		transform: scale(0.97);
	}

	// Pulse animation while tour is running
	&--running {
		animation: ff-tour-pulse 1.5s ease-in-out infinite;
	}

	&__icon {
		font-size: 16px;
		font-weight: 700;
		line-height: 1;
		user-select: none;
	}
}

// Mobile: shift FAB above bottom navigation bars
@media (max-width: 768px) {
	.ff-tour-fab {
		bottom: 16px;
		right: 60px;
		width: 36px;
		height: 36px;

		&__icon {
			font-size: 16px;
		}
	}
}

// ─── FAB entrance/exit transition ──────────────────────────────────────────────

.ff-tour-fab-enter-active,
.ff-tour-fab-leave-active {
	transition:
		opacity 0.25s ease,
		transform 0.25s ease;
}

.ff-tour-fab-enter-from,
.ff-tour-fab-leave-to {
	opacity: 0;
	transform: scale(0.5);
}

// ─── Pulse keyframes ───────────────────────────────────────────────────────────

@keyframes ff-tour-pulse {
	0%,
	100% {
		box-shadow: 0 4px 14px rgba(0, 0, 0, 0.2);
	}

	50% {
		box-shadow: 0 4px 20px var(--el-color-primary-light-3);
	}
}
</style>
