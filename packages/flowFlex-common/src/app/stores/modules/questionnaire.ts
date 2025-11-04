import { defineStore } from 'pinia';
import { store } from '@/stores';
import { ref, watch, onBeforeUnmount, toRaw, computed } from 'vue';
import { ElMessageBox } from 'element-plus';
import { onBeforeRouteLeave, useRouter } from 'vue-router';

interface IUnsavedGuardOptions<T extends object> {
	initialState: T;
	currentState: T;
}

interface IUnsavedGuard<T extends object> {
	resetDirty(nextInitial: T): void;
	guardNavigation(targetPath: string): Promise<void>;
}

export const useQuestionnaireStore = defineStore('item-wfe-app-questionnaire', () => {
	const unSavedQuestionnaire = ref<IUnsavedGuardOptions<any> | null>(null);
	const formDirty = ref(false);
	const externalDirtyFlags = ref<Record<string, boolean>>({});
	const hasExternalDirty = computed(() =>
		Object.values(externalDirtyFlags.value).some((flag) => flag)
	);
	const isDirty = computed(() => formDirty.value || hasExternalDirty.value);
	const dialogMessage = 'You have unsaved changes. Are you sure you want to leave?';
	/**
	 * 统一处理“未保存变更”提示：
	 * 1. 深度监听 currentState 与 initialState 的差异，得到 isDirty。
	 * 2. 拦截路由离开、浏览器刷新、外部导航（菜单按钮等）。
	 * 3. confirmed 离开后可以通过 resetDirty 刷新初始快照。
	 */
	const cloneDeep = <S extends object>(source: S): S => {
		try {
			return structuredClone(toRaw(source) as S);
		} catch {
			return JSON.parse(JSON.stringify(source)) as S;
		}
	};

	const confirmLeave = async () => {
		if (!isDirty.value) return true;
		try {
			await ElMessageBox.confirm(dialogMessage, 'Confirmation', {
				type: 'warning',
				confirmButtonText: 'Leave',
				cancelButtonText: 'Stay',
				closeOnClickModal: false,
				closeOnPressEscape: false,
			});
			clearStatusData();
			return true;
		} catch {
			return false;
		}
	};

	// 清理数据状态
	const resetExternalDirtyFlags = () => {
		externalDirtyFlags.value = {};
	};

	const clearStatusData = () => {
		formDirty.value = false;
		resetExternalDirtyFlags();
		unSavedQuestionnaire.value = null;
	};

	const updateExternalDirtyFlag = (key: string, value: boolean) => {
		const nextFlags = { ...externalDirtyFlags.value };
		if (value) {
			nextFlags[key] = true;
		} else {
			delete nextFlags[key];
		}
		externalDirtyFlags.value = nextFlags;
	};

	const useUnsavedChangesGuard = <T extends object>({
		initialState,
		currentState,
	}: IUnsavedGuardOptions<T>): IUnsavedGuard<T> => {
		unSavedQuestionnaire.value = {
			initialState,
			currentState,
		};
		const router = useRouter();

		let initialSnapshot: T = cloneDeep(initialState);
		let bypassNextLeaveGuard = false;

		watch(
			() => currentState,
			(value) => {
				const rawValue = toRaw(value) as T;
				formDirty.value = JSON.stringify(rawValue) !== JSON.stringify(initialSnapshot);
			},
			{ deep: true }
		);

		const resetDirty = (nextInitial: T) => {
			initialSnapshot = cloneDeep(nextInitial);
			formDirty.value = false;
			resetExternalDirtyFlags();
		};

		onBeforeRouteLeave(async () => {
			if (bypassNextLeaveGuard) {
				bypassNextLeaveGuard = false;
				return true;
			}
			return await confirmLeave();
		});

		const handleBeforeUnload = (event: BeforeUnloadEvent) => {
			if (!isDirty.value) return;
			event.preventDefault();
			event.returnValue = '';
		};

		const guardNavigation = async (targetPath: string) => {
			if (!(await confirmLeave())) return;
			bypassNextLeaveGuard = true;
			try {
				await router.push(targetPath);
			} catch (error) {
				bypassNextLeaveGuard = false;
				throw error;
			}
		};

		window.addEventListener('beforeunload', handleBeforeUnload);
		onBeforeUnmount(() => {
			window.removeEventListener('beforeunload', handleBeforeUnload);
		});

		return {
			resetDirty,
			guardNavigation,
		};
	};

	return {
		useQuestionnaireUnsavedChangesGuard: useUnsavedChangesGuard,
		confirmLeave,
		clearStatusData,
		updateExternalDirtyFlag,
	};
});

export function useQuestionnaireStoreWithOut() {
	return useQuestionnaireStore(store);
}
