import { createStorage as create, CreateStorageParams } from './storageCache';
import {
	getCommonStoragePrefix,
	SHOULD_ENABLE_STORAGE_ENCRYPTION,
	DEFAULT_CACHE_TIME,
} from '@/settings/encryptionSetting';

export type Options = Partial<CreateStorageParams>;

const createOptions = (storage: Storage, options: Options = {}): Options => {
	return {
		// No encryption in debug mode
		hasEncrypt: SHOULD_ENABLE_STORAGE_ENCRYPTION,
		storage,
		prefixKey: getCommonStoragePrefix,
		...options,
	};
};

export const WebStorage = create(createOptions(sessionStorage));

export const createStorage = (storage: Storage = sessionStorage, options: Options = {}) => {
	return create(createOptions(storage, options));
};

export const createSessionStorage = (options: Options = {}) => {
	return createStorage(sessionStorage, { ...options, timeout: DEFAULT_CACHE_TIME });
};

export const createLocalStorage = (options: Options = {}) => {
	return createStorage(localStorage, { ...options, timeout: DEFAULT_CACHE_TIME });
};

export default WebStorage;
