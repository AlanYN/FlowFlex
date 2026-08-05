import { ref, type Ref } from 'vue';
import { TourStep, UseTourGuideOptions } from '#/config';

// ═══════════════════════════════════════════════════════════════════
// Constants
// ═══════════════════════════════════════════════════════════════════

const STORAGE_PREFIX = 'ff_tour_done_';

// ═══════════════════════════════════════════════════════════════════
// Helpers
// ═══════════════════════════════════════════════════════════════════

function _readCompleted(key: string): boolean {
	try {
		return !!localStorage.getItem(key);
	} catch {
		return false;
	}
}

/**
 * Return the first VISIBLE element matching a selector.
 * `querySelector` also matches elements hidden with `display:none` (collapsed
 * sections, disabled components, etc.). Driver.js cannot highlight those, so
 * we filter them out up-front.
 */
function _findVisibleElement(selector: string): Element | null {
	const nodes = Array.from(document.querySelectorAll<HTMLElement>(selector));
	for (const el of nodes) {
		if (el.getClientRects().length > 0) return el;
	}
	return null;
}

/**
 * Find the scrollable el-scrollbar wrap that contains the element.
 * The detail page has two independent el-scrollbar columns (left content /
 * right progress), so we must scroll the one that actually holds the target.
 */
function _findScrollContainer(el: Element): HTMLElement | null {
	let node = el.parentElement;
	while (node) {
		if (node.classList.contains('el-scrollbar__wrap')) return node;
		node = node.parentElement;
	}
	return null;
}

// ═══════════════════════════════════════════════════════════════════
// Hook
// ═══════════════════════════════════════════════════════════════════

export function useTourGuide(options: UseTourGuideOptions) {
	const { persistKey, onComplete, onSkip, getScrollContainer, checkSeenRemote, markSeenRemote } =
		options;
	const storageKey = `${STORAGE_PREFIX}${persistKey}`;

	const isCompleted: Ref<boolean> = ref(_readCompleted(storageKey));
	const isRunning = ref(false);
	// In-memory flag: remote check already done this session — skip subsequent requests.
	let _remoteCheckDone = false;

	// Current Driver.js instance so we can destroy it on unmount / stage switch.
	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	let activeDriver: any = null;
	// Restores scroll patching + frozen scrollers when the tour ends.
	let unlockScroll: (() => void) | null = null;
	// Cleanup for the current waitForUserClick listener (hoisted so stopTour can clear it).
	let _removeUserClickListenerOuter: (() => void) | null = null;

	async function _runTour(steps: TourStep[]): Promise<boolean> {
		// Never run two tours at the same time (e.g. auto-start racing replay).
		if (isRunning.value) return false;
		if (!steps || steps.length === 0) return false;

		isRunning.value = true;

		const [{ driver }] = await Promise.all([
			import('driver.js'),
			import('driver.js/dist/driver.css'),
		]);

		// Drop steps whose target is missing or not visible (collapsed/disabled).
		// disableHighlight steps only need a visible anchor to decide inclusion.
		// lazyElement steps bypass the filter entirely — their element appears
		// dynamically (e.g. after a dialog opens in beforeHighlight).
		const activeSteps = steps.filter(
			(step) =>
				step.disableHighlight ||
				step.lazyElement ||
				_findVisibleElement(step.element) !== null
		);

		if (activeSteps.length === 0) {
			console.warn('[useTourGuide] No tour elements found in DOM. Tour aborted.');
			isRunning.value = false;
			return false;
		}

		const total = activeSteps.length;

		// ═══ Scroll control ═══════════════════════════════════════════════
		// Root cause: Driver.js calls element.scrollIntoView() which walks up the
		// DOM tree and scrolls el-scrollbar containers, pushing the page layout.
		//
		// IMPORTANT: We must NOT set overflow:hidden on el-scrollbar__wrap because
		// Driver.js's SVG overlay uses getBoundingClientRect() of the highlighted
		// element. If the scrollbar container has overflow:hidden, the element's
		// visible rect gets clipped and the highlight box disappears.
		//
		// Strategy: patch scrollIntoView to no-op, freeze unrelated scrollers,
		// then manually scroll ONLY the target's own scroll container.

		const scrollY = window.scrollY;

		// Containers we are allowed to scroll = targets' own scroll ancestors.
		const scrollableContainers = new Set<HTMLElement>();
		activeSteps.forEach((step) => {
			if (step.disableHighlight) return;
			const el = _findVisibleElement(step.element);
			const container = el ? _findScrollContainer(el) : null;
			if (container) scrollableContainers.add(container);
		});

		// Freeze every OTHER el-scrollbar wrap so only the target's container moves.
		const frozenScrollers: Array<{
			el: HTMLElement;
			top: number;
			left: number;
			handler: () => void;
		}> = [];
		document.querySelectorAll<HTMLElement>('.el-scrollbar__wrap').forEach((el) => {
			if (scrollableContainers.has(el)) return;
			const top = el.scrollTop;
			const left = el.scrollLeft;
			const handler = () => {
				el.scrollTop = top;
				el.scrollLeft = left;
			};
			el.addEventListener('scroll', handler, { passive: true });
			frozenScrollers.push({ el, top, left, handler });
		});

		// Patch scrollIntoView — this is what Driver.js calls internally.
		const originalScrollIntoView = Element.prototype.scrollIntoView;
		// eslint-disable-next-line @typescript-eslint/no-empty-function
		Element.prototype.scrollIntoView = function () {};

		// Patch window scroll methods as a secondary safeguard.
		const origScrollTo = window.scrollTo.bind(window);
		const origScrollBy = window.scrollBy.bind(window);
		// eslint-disable-next-line @typescript-eslint/no-explicit-any
		(window as any).scrollTo = () => {};
		// eslint-disable-next-line @typescript-eslint/no-explicit-any
		(window as any).scrollBy = () => {};

		const _unlockScroll = () => {
			Element.prototype.scrollIntoView = originalScrollIntoView;
			// eslint-disable-next-line @typescript-eslint/no-explicit-any
			(window as any).scrollTo = origScrollTo;
			// eslint-disable-next-line @typescript-eslint/no-explicit-any
			(window as any).scrollBy = origScrollBy;
			frozenScrollers.forEach(({ el, handler, top, left }) => {
				el.removeEventListener('scroll', handler);
				el.scrollTop = top;
				el.scrollLeft = left;
			});
			origScrollTo(0, scrollY);
		};
		unlockScroll = _unlockScroll;

		// ═══ Helper: build dot indicators ══════════════════════════════════
		function buildDots(current: number, totalCount: number): string {
			return Array.from({ length: totalCount })
				.map((_, i) => {
					const active = i === current;
					return `<span class="ff-tour-dot ${
						active ? 'ff-tour-dot--active' : ''
					}"></span>`;
				})
				.join('');
		}

		// ═══ Helper: build popover description HTML ═════════════════════════
		// We inject the full footer (dots + nav + skip) inside the description
		// because Driver.js's built-in footer has limited customisation.
		function buildDescription(step: TourStep, index: number): string {
			const isFirst = index === 0;
			const isLast = index === total - 1;
			const dots = buildDots(index, total);
			// waitForUserClick: hide Next/Done, show a hint prompt instead
			const navHtml = step.waitForUserClick
				? `<span class="ff-tour-click-hint">👆 Click the highlighted button to continue</span>`
				: `<div class="ff-tour-nav">
							<button
								class="ff-tour-btn-prev"
								${isFirst ? 'disabled' : ''}
								data-action="prev"
							>←</button>
							<button
								class="ff-tour-btn-next"
								data-action="${isLast ? 'done' : 'next'}"
							>${isLast ? 'Done' : 'Next →'}</button>
						</div>`;

			return `
				<div class="ff-tour-body">${step.description}</div>
				<div class="ff-tour-footer">
					<div class="ff-tour-footer-row1">
						<div class="ff-tour-dots">${dots}</div>
						${navHtml}
					</div>
					<div class="ff-tour-footer-row2">
						<span class="ff-tour-counter">${index + 1} of ${total}</span>
						<button class="ff-tour-btn-skip" data-action="skip">Skip tour</button>
					</div>
				</div>
			`;
		}

		// eslint-disable-next-line @typescript-eslint/no-explicit-any
		let driverInstance: any = null;

		const _finish = () => {
			activeDriver = null;
			unlockScroll = null;
			isRunning.value = false;
			_unlockScroll();
			_removeUserClickListener?.();
			_removeUserClickListener = null;
			_removeUserClickListenerOuter = null;
			if (driverInstance?.isActive?.()) driverInstance.destroy();
		};

		// ═══ Drive ════════════════════════════════════════════════════════
		// Track current step index to invoke beforeHighlight callbacks
		// eslint-disable-next-line prefer-const
		let currentStepIdx = 0;
		// Cleanup handle for the current waitForUserClick listener
		let _removeUserClickListener: (() => void) | null = null;

		/**
		 * Set up a one-shot click listener on the highlighted element.
		 * When the user clicks it, advance to the next step (or finish if last).
		 */
		function _attachUserClickListener(targetEl: Element, stepIndex: number) {
			// Remove any previously registered listener first
			_removeUserClickListener?.();
			_removeUserClickListener = null;
			_removeUserClickListenerOuter = null;

			const handler = async (e: Event) => {
				// Don't intercept clicks on the popover itself
				const popover = document.querySelector('.ff-tour-popover');
				if (popover?.contains(e.target as Node)) return;

				// Clean up immediately so the real click fires through
				_removeUserClickListener?.();
				_removeUserClickListener = null;
				_removeUserClickListenerOuter = null;

				// Immediately hide the tour overlay + popover so the user doesn't
				// see the tour hovering while we wait for async work (dialog open/close).
				const popoverEl = document.querySelector<HTMLElement>('.driver-popover');
				const overlayEl = document.querySelector<SVGElement>('.driver-overlay');
				const stageEl = document.querySelector<SVGElement>(
					'.driver-stage-wrap, #driver-highlighted-element-stage'
				);
				if (popoverEl) popoverEl.style.visibility = 'hidden';
				if (overlayEl) (overlayEl as unknown as HTMLElement).style.opacity = '0';
				if (stageEl) (stageEl as unknown as HTMLElement).style.opacity = '0';

				// Run afterUserClick hook if defined
				const step = activeSteps[stepIndex];
				if (step?.afterUserClick) {
					await step.afterUserClick();
				}

				// Restore visibility before moveNext re-renders the popover
				if (popoverEl) popoverEl.style.visibility = '';
				if (overlayEl) (overlayEl as unknown as HTMLElement).style.opacity = '';
				if (stageEl) (stageEl as unknown as HTMLElement).style.opacity = '';

				// Advance or finish
				const isLast = stepIndex === activeSteps.length - 1;
				if (isLast) {
					markCompleted();
					onComplete?.();
					_finish();
				} else {
					currentStepIdx = stepIndex + 1;
					driverInstance?.moveNext();
				}
			};

			// Use capture phase so we react before other handlers (e.g. Element Plus dialog)
			targetEl.addEventListener('click', handler, { capture: false });
			const cleanup = () =>
				targetEl.removeEventListener('click', handler, { capture: false });
			_removeUserClickListener = cleanup;
			_removeUserClickListenerOuter = cleanup;
		}

		// Detect dark mode — the app adds `dark` class on <html>
		const isDark = document.documentElement.classList.contains('dark');

		driverInstance = driver({
			showProgress: false, // we render our own progress
			animate: true,
			overlayOpacity: isDark ? 0.75 : 0.4,
			// Disable Driver.js built-in scroll — the content area uses
			// el-scrollbar (a custom scroll container), not window. We handle
			// scrolling manually in onHighlightStarted below.
			smoothScroll: false,
			allowClose: false, // close only via our Skip button
			overlayColor: '#000',
			stagePadding: 4,
			stageRadius: 8,
			popoverClass: 'ff-tour-popover',
			nextBtnText: '',
			prevBtnText: '',
			doneBtnText: '',
			// Scroll the target element into view within ITS OWN scroll
			// container (left content column or right progress column).
			// Also invoke beforeHighlight if the current step defines one.
			onHighlightStarted: async (highlightedEl: Element | undefined) => {
				// Remove any leftover click listener from the previous step
				_removeUserClickListener?.();
				_removeUserClickListener = null;

				// Call beforeHighlight for the current step (if defined)
				const currentStep = activeSteps[currentStepIdx];
				if (currentStep?.beforeHighlight) {
					const waitFor = await currentStep.beforeHighlight();
					if (waitFor && typeof waitFor === 'string') {
						await new Promise<void>((resolve) => {
							const deadline = Date.now() + 2000;
							const check = () => {
								if (document.querySelector(waitFor)) {
									resolve();
								} else if (Date.now() < deadline) {
									requestAnimationFrame(check);
								} else {
									resolve();
								}
							};
							check();
						});
					}
				}

				if (!highlightedEl) return;

				const container =
					_findScrollContainer(highlightedEl) ??
					(getScrollContainer ? getScrollContainer() : null);

				if (container) {
					const containerRect = container.getBoundingClientRect();
					const elRect = highlightedEl.getBoundingClientRect();

					// Already fully visible — no need to scroll.
					const fullyVisible =
						elRect.top >= containerRect.top && elRect.bottom <= containerRect.bottom;
					if (!fullyVisible) {
						const relativeTop = elRect.top - containerRect.top + container.scrollTop;
						container.scrollTop = Math.max(0, relativeTop - 80);
					}
				}

				// waitForUserClick: attach a click listener so the user must
				// physically click the highlighted element to advance.
				if (currentStep?.waitForUserClick) {
					// Wait one frame so Driver.js finishes rendering the overlay
					// before we attach the listener.
					await new Promise<void>((resolve) => requestAnimationFrame(() => resolve()));
					_attachUserClickListener(highlightedEl, currentStepIdx);
				}
			},
			// eslint-disable-next-line @typescript-eslint/no-explicit-any
			onPopoverRender: (popoverEl: any) => {
				// popoverEl is Driver.js's PopoverDOM — access the wrapper HTMLElement.
				const el: HTMLElement = popoverEl.wrapper ?? popoverEl;

				// Remove any blue border on Driver.js SVG stage elements
				const svgStage = document.querySelector<SVGElement>(
					'#driver-highlighted-element-stage, .driver-stage'
				);
				if (svgStage) {
					svgStage.style.fill = 'transparent';
					svgStage.style.stroke = 'transparent';
				}

				// Hide Driver.js default navigation footer (we render our own).
				const nav = el.querySelector(
					'.driver-popover-navigation-btns'
				) as HTMLElement | null;
				if (nav) nav.style.display = 'none';
				const progress = el.querySelector(
					'.driver-popover-progress-text'
				) as HTMLElement | null;
				if (progress) progress.style.display = 'none';
				const footer = el.querySelector('.driver-popover-footer') as HTMLElement | null;
				if (footer) footer.style.display = 'none';

				// Scroll the active dot into view within the dots container
				// so it is always visible even when there are many steps.
				requestAnimationFrame(() => {
					const dotsContainer = el.querySelector<HTMLElement>('.ff-tour-dots');
					const activeDot = el.querySelector<HTMLElement>('.ff-tour-dot--active');
					if (dotsContainer && activeDot) {
						const containerLeft = dotsContainer.getBoundingClientRect().left;
						const dotLeft = activeDot.getBoundingClientRect().left;
						const offset = dotLeft - containerLeft;
						const center =
							offset - dotsContainer.offsetWidth / 2 + activeDot.offsetWidth / 2;
						dotsContainer.scrollLeft += center;
					}
				});

				// Bind click events on our custom buttons.
				el.addEventListener('click', (e: MouseEvent) => {
					const target = (e.target as HTMLElement).closest(
						'[data-action]'
					) as HTMLElement | null;
					if (!target) return;
					const action = target.dataset.action;

					if (action === 'next') {
						currentStepIdx = Math.min(currentStepIdx + 1, activeSteps.length - 1);
						driverInstance?.moveNext();
					} else if (action === 'prev') {
						currentStepIdx = Math.max(currentStepIdx - 1, 0);
						driverInstance?.movePrevious();
					} else if (action === 'done') {
						markCompleted();
						onComplete?.();
						_finish();
					} else if (action === 'skip') {
						markCompleted();
						onSkip?.();
						_finish();
					}
				});
			},
			steps: activeSteps.map((step, index) => {
				const stepConfig: Record<string, unknown> = {
					popover: {
						title: step.title ?? '',
						description: buildDescription(step, index),
						side: step.side ?? 'bottom',
						align: step.align ?? 'start',
					},
				};
				if (!step.disableHighlight) {
					stepConfig.element = step.element;
				}
				return stepConfig;
			}),
		});

		activeDriver = driverInstance;
		driverInstance.drive();
		return true;
	}

	async function startTour(steps: TourStep[]): Promise<boolean> {
		// Priority 1: If we have a remote check function, let the backend decide.
		// localStorage is only used as a cache AFTER the backend has confirmed seen.
		// This ensures the "once only" guarantee holds across devices/browsers.
		if (checkSeenRemote) {
			// Fast-path: localStorage already confirmed (backend was queried before).
			if (isCompleted.value) return false;
			// Fast-path 2: remote check already done this session (e.g. retry after DOM miss)
			if (!_remoteCheckDone) {
				try {
					const seen = await checkSeenRemote();
					if (seen) {
						markCompleted(); // cache in localStorage for subsequent same-session checks
						return false;
					}
				} catch {
					// Network error — fall back to localStorage state and let the tour run
					if (isCompleted.value) return false;
				}
				_remoteCheckDone = true;
			}
			return _runTour(steps);
		}

		// No remote check — rely solely on localStorage
		if (isCompleted.value) return false;
		return _runTour(steps);
	}

	async function replayTour(steps: TourStep[]): Promise<boolean> {
		return _runTour(steps);
	}

	/** Immediately stop a running tour and restore scroll behavior. */
	function stopTour(): void {
		_removeUserClickListenerOuter?.();
		_removeUserClickListenerOuter = null;
		if (activeDriver?.isActive?.()) {
			activeDriver.destroy();
		}
		activeDriver = null;
		unlockScroll?.();
		unlockScroll = null;
		isRunning.value = false;
	}

	function markCompleted(): void {
		try {
			localStorage.setItem(storageKey, new Date().toISOString());
		} catch {
			// fail silently
		}
		isCompleted.value = true;

		// Best-effort: persist to backend so the record survives browser/device changes.
		if (markSeenRemote) {
			markSeenRemote().catch(() => {
				// Swallow errors — tour marking is non-critical
			});
		}
	}

	function resetCompleted(): void {
		try {
			localStorage.removeItem(storageKey);
		} catch {
			// fail silently
		}
		isCompleted.value = false;
	}

	return {
		isCompleted,
		isRunning,
		startTour,
		replayTour,
		stopTour,
		markCompleted,
		resetCompleted,
	};
}
