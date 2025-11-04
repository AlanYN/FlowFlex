// System default cache time, in seconds
// import { isDevMode } from '@/utils/env';

export const DEFAULT_CACHE_TIME = 60 * 60 * 24 * 7;

// aes encryption key
export const cacheCipher = {
	key: '_11111000001111@',
	iv: '@11111000001111_',
};

export const getCommonStoragePrefix = 'ITEMWFE_';

// Whether the system cache is encrypted using aes
export const SHOULD_ENABLE_STORAGE_ENCRYPTION = false;
