import type { RouteLocationNormalized } from 'vue-router';

import { createLocalStorage, createSessionStorage } from '@/utils/cache';
import { Memory } from './memory';
import {
	USER_INFO_KEY,
	ROLES_KEY,
	LOCK_INFO_KEY,
	PROJ_CFG_KEY,
	APP_LOCAL_CACHE_KEY,
	APP_SESSION_CACHE_KEY,
	MULTIPLE_TABS_KEY,
	TOKENOBJ_KEY,
	ENUM_SELECT_KEY,
	MENU_MENUTYPE,
	ISLOGIN_KEY,
} from '@/enums/cacheEnum';
import { DEFAULT_CACHE_TIME, getCommonStoragePrefix } from '@/settings/encryptionSetting';
import { toRaw } from 'vue';
import { pick, omit } from 'lodash-es';
import { useUserStore } from '@/stores/modules/user';

export interface LockInfo {
	// Password required
	pwd?: string | undefined;
	// Is it locked?
	isLock?: boolean;
}
export interface UserInfo {
	userId: string | number;
	userName: string;
	realName: string;
	avatar: string;
	desc?: string;
	homePath?: string;
	roles: any[];
	companyIds: [string | number];
}

interface BasicStore {
	[USER_INFO_KEY]: UserInfo;
	[ROLES_KEY]: string[];
	[LOCK_INFO_KEY]: LockInfo;
	[PROJ_CFG_KEY]: any;
	[MULTIPLE_TABS_KEY]: RouteLocationNormalized[];
	[TOKENOBJ_KEY]: TokenObj | undefined;
	[ENUM_SELECT_KEY]: EnumList;
	[MENU_MENUTYPE]: string;
	[ISLOGIN_KEY]: boolean;
}

type TokenObj = {
	accessToken: {
		expire: string;
		token: string;
	};
	refreshToken: {
		expire: string;
		token: string;
	};
};

interface EnumList {
	peopleClass: any[];
	position: any[];
	status: any[];
	shift: any[];
	experience: any[];
	terminal: any[];
}

type LocalStore = BasicStore;

type SessionStore = BasicStore;

export type BasicKeys = keyof BasicStore;
type LocalKeys = keyof LocalStore;
type SessionKeys = keyof SessionStore;

const ls = createLocalStorage();
const ss = createSessionStorage();

const localMemory = new Memory(DEFAULT_CACHE_TIME);
const sessionMemory = new Memory(DEFAULT_CACHE_TIME);

function initPersistentMemory() {
	const localCache = ls.get(APP_LOCAL_CACHE_KEY);
	const sessionCache = ss.get(APP_SESSION_CACHE_KEY);
	localCache && localMemory.resetCache(localCache);
	sessionCache && sessionMemory.resetCache(sessionCache);
}

export class Persistent {
	static getLocal<T>(key: LocalKeys): T {
		return localMemory.get(key)?.value;
	}

	static setLocal(key: LocalKeys, value: LocalStore[LocalKeys], immediate = false): void {
		localMemory.set(key, toRaw(value));
		immediate && ls.set(APP_LOCAL_CACHE_KEY, localMemory.getCache);
	}

	static removeLocal(key: LocalKeys, immediate = false): void {
		localMemory.remove(key);
		immediate && ls.set(APP_LOCAL_CACHE_KEY, localMemory.getCache);
	}

	static clearLocal(immediate = false): void {
		localMemory.clear();
		immediate && ls.clear();
	}

	static getSession<T>(key: SessionKeys): T {
		return sessionMemory.get(key)?.value;
	}

	static setSession(key: SessionKeys, value: SessionStore[SessionKeys], immediate = false): void {
		sessionMemory.set(key, toRaw(value));
		immediate && ss.set(APP_SESSION_CACHE_KEY, sessionMemory.getCache);
	}

	static removeSession(key: SessionKeys, immediate = false): void {
		sessionMemory.remove(key);
		immediate && ss.set(APP_SESSION_CACHE_KEY, sessionMemory.getCache);
	}
	static clearSession(immediate = false): void {
		sessionMemory.clear();
		immediate && ss.clear();
	}

	static clearAll(immediate = false) {
		sessionMemory.clear();
		localMemory.clear();
		if (immediate) {
			ls.clear();
			ss.clear();
		}
	}
}

window.addEventListener('beforeunload', function () {
	// TOKEN_KEY 在登录或注销时已经写入到storage了，此处为了解决同时打开多个窗口时token不同步的问题
	// LOCK_INFO_KEY 在锁屏和解锁时写入，此处也不应修改
	ls.set(APP_LOCAL_CACHE_KEY, {
		...omit(localMemory.getCache, LOCK_INFO_KEY),
		...pick(ls.get(APP_LOCAL_CACHE_KEY), [USER_INFO_KEY, LOCK_INFO_KEY]),
	});
	ss.set(APP_SESSION_CACHE_KEY, {
		...omit(sessionMemory.getCache, LOCK_INFO_KEY),
		...pick(ss.get(APP_SESSION_CACHE_KEY), [USER_INFO_KEY, LOCK_INFO_KEY]),
	});
});

function storageChange(e: any) {
	const { key, newValue } = e;

	if (!key) {
		Persistent.clearAll();
		return;
	}

	if (key === `${getCommonStoragePrefix}${APP_LOCAL_CACHE_KEY}`) {
		try {
			if (newValue) {
				const cacheData = JSON.parse(newValue);
				const cacheValues = cacheData?.value || {};

				try {
					const userStore = useUserStore();

					if (cacheValues[TOKENOBJ_KEY]?.value) {
						userStore.$patch({ tokenObj: cacheValues[TOKENOBJ_KEY].value });
						// console.log('✅ Token 已从其他标签页同步');
					}

					if (cacheValues[USER_INFO_KEY]?.value) {
						userStore.$patch({ userInfo: cacheValues[USER_INFO_KEY].value });
						// console.log('✅ 用户信息已从其他标签页同步');
					}

					if (cacheValues[ISLOGIN_KEY]?.value !== undefined) {
						userStore.$patch({ isLogin: cacheValues[ISLOGIN_KEY].value });
					}
				} catch (storeError) {
					console.warn('Pinia store 未就绪，仅更新内存缓存:', storeError);
				}

				localMemory.resetCache(cacheValues);
			} else {
				Persistent.clearLocal();
				try {
					const userStore = useUserStore();
					userStore.$patch({
						tokenObj: undefined,
						userInfo: null,
						isLogin: false,
					});
				} catch (storeError) {
					console.warn('Pinia store 未就绪:', storeError);
				}
			}
		} catch (error) {
			console.error('同步缓存失败:', error);
		}
		return;
	}
}

window.addEventListener('storage', storageChange);

initPersistentMemory();
