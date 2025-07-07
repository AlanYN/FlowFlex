import { reactive, readonly } from 'vue';

const state = reactive({
	theme: localStorage.theme || 'light', // 默认主题为 dark
	primary: localStorage.primary || 'blue', // 默认主题为 dark
});

export function useTheme() {
	localStorage.getItem('theme') && (state.theme = localStorage.getItem('theme')!);
	return readonly(state);
}

export function setTheme(theme?: string | null, isSet?: boolean) {
	if (theme || isSet) {
		state.theme = theme !== 'null' ? theme : 'light';
	} else {
		// 没有设置值,则从内存中去取 或者直接取当前浏览器的默认值
		if (
			!state.theme &&
			!('theme' in localStorage) &&
			window.matchMedia('(prefers-color-scheme: dark)').matches
		) {
			state.theme = 'dark';
		} else {
			state.theme = localStorage.getItem('theme') == 'dark' ? 'light' : 'dark';
		}
	}
	localStorage.setItem('theme', state.theme!);

	function changeDarkMode() {
		if (state.theme === 'dark') {
			document.documentElement.classList.add('dark');
		} else {
			document.documentElement.classList.remove('dark');
		}

		// 通知嵌套的权限页面切换主题
		const permissionIframe = document.getElementById('permission-iframe') as HTMLIFrameElement;
		if (permissionIframe) {
			// 从 iframe 的 src 中获取目标域名
			const targetOrigin = new URL(permissionIframe.src).origin;
			permissionIframe.contentWindow?.postMessage(
				{
					type: 'themeChange',
					data: {
						theme: state.theme,
					},
				},
				targetOrigin
			);
		}
	}

	changeDarkMode();
}

export function setPrimary(primary?: string | null) {
	const primaryColor = localStorage.getItem('primary');
	if (primary) {
		if (primaryColor) {
			document.documentElement.classList.remove(primaryColor);
		}
		state.primary = primary;
	} else {
		if (!primaryColor) {
			state.primary = 'blue';
		} else {
			state.primary = primaryColor;
		}
	}

	document.documentElement.classList.add(state.primary);
	localStorage.setItem('primary', state.primary);
}
