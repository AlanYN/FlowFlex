export enum PageEnum {
	// basic login path
	BASE_LOGIN = '/login',
	// basic home path
	BASE_HOME = import.meta.env.VITE_HOME_URL,
	// error page path
	ERROR_PAGE = '/exception',
	// error log page path
	ERROR_LOG_PAGE = '/error-log/list',
}

export const PageWrapperFixedHeightKey = 'PageWrapperFixedHeight';
