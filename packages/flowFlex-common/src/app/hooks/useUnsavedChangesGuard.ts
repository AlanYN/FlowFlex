import { ref, watch, onBeforeUnmount, type Ref, toRaw } from 'vue';
import { ElMessageBox } from 'element-plus';
import { onBeforeRouteLeave, useRouter } from 'vue-router';

interface IUnsavedGuardOptions<T extends object> {
	initialState: T;
	currentState: T;
	dialogMessage?: string;
}

interface IUnsavedGuard<T extends object> {
	isDirty: Ref<boolean>;
	resetDirty(nextInitial: T): void;
	confirmLeave(): Promise<boolean>;
	guardNavigation(targetPath: string): Promise<void>;
}

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

export function useUnsavedChangesGuard<T extends object>({
	initialState,
	currentState,
	dialogMessage = 'You have unsaved changes. Are you sure you want to leave?',
}: IUnsavedGuardOptions<T>): IUnsavedGuard<T> {
	const router = useRouter();
	const isDirty = ref(false);
	let initialSnapshot: T = cloneDeep(initialState);
	let bypassNextLeaveGuard = false;

	watch(
		() => currentState,
		(value) => {
			const rawValue = toRaw(value) as T;
			isDirty.value = JSON.stringify(rawValue) !== JSON.stringify(initialSnapshot);
		},
		{ deep: true }
	);

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
			return true;
		} catch {
			return false;
		}
	};

	const resetDirty = (nextInitial: T) => {
		initialSnapshot = cloneDeep(nextInitial);
		isDirty.value = false;
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
		if (await confirmLeave()) {
			bypassNextLeaveGuard = true;
			router.push(targetPath);
		}
	};

	window.addEventListener('beforeunload', handleBeforeUnload);
	onBeforeUnmount(() => {
		window.removeEventListener('beforeunload', handleBeforeUnload);
	});

	return {
		isDirty,
		resetDirty,
		confirmLeave,
		guardNavigation,
	};
}
