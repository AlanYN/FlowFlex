import { Persistent, BasicKeys } from '@/utils/cache/persistent';
import { TOKENOBJ_KEY, USER_INFO_KEY } from '@/enums/cacheEnum';

import type { TokenObj } from '@/apis/axios/axiosTransform';

const isLocal = true;

export function getTokenobj(): TokenObj {
	return getAuthCache(TOKENOBJ_KEY);
}

export function getLocalUserInfo() {
	return getAuthCache(USER_INFO_KEY);
}

export function getAuthCache<T>(key: BasicKeys) {
	const fn = isLocal ? Persistent.getLocal : Persistent.getSession;
	return fn(key) as T;
}

export function setAuthCache(key: BasicKeys, value) {
	const fn = isLocal ? Persistent.setLocal : Persistent.setSession;
	return fn(key, value, true);
}

export function clearAuthCache(immediate = true) {
	const fn = isLocal ? Persistent.clearLocal : Persistent.clearSession;
	return fn(immediate);
}
