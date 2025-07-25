import { ElMessage } from 'element-plus';
import { useI18n } from '@/hooks/useI18n';
// import router from '@/router';
// import { PageEnum } from '@/enums/pageEnum';
import { useUserStoreWithOut } from '@/stores/modules/user';
import { SingleCaseMessage } from '@/utils/singleCaseMessage';

const singleCaseMessage = new SingleCaseMessage();

export type ErrorMessageMode = 'none' | 'modal' | 'message' | 'error-page' | undefined;

export function checkStatus(
	status: number,
	msg: string,
	errorMessageMode: ErrorMessageMode = 'message',
	requestInfo?: {
		url: string;
		method: string;
		params?: any;
	}
): void {
	const userStore = useUserStoreWithOut();
	let errMessage = '';
	const { t } = useI18n();
	switch (status) {
		case 400:
			errMessage = `${msg}`;
			break;
		// 401: Not logged in
		// Jump to the login page if not logged in, and carry the path of the current page
		// Return to the current page after successful login. This step needs to be operated on the login page.
		case 401:
			// errMessage = msg || t('sys.api.errMsg401');
			userStore.setTokenobj(undefined);
			userStore.logout(true, 'logout');
			singleCaseMessage.showMessage(t('sys.api.tokenExpired'), 'info');
			break;
		case 403:
			errMessage = t('sys.api.errMsg403');
			break;
		// 404请求不存在
		case 404:
			errMessage = t('sys.api.errMsg404');
			break;
		case 405:
			errMessage = t('sys.api.errMsg405');
			break;
		case 408:
			errMessage = t('sys.api.errMsg408');
			break;
		case 500:
			errMessage = t('sys.api.errMsg500');
			break;
		case 501:
			errMessage = t('sys.api.errMsg501');
			break;
		case 502:
			errMessage = t('sys.api.errMsg502');
			break;
		case 503:
			errMessage = t('sys.api.errMsg503');
			break;
		case 504:
			errMessage = t('sys.api.errMsg504');
			break;
		case 505:
			errMessage = t('sys.api.errMsg505');
			break;
		default:
			break;
	}

	if (errMessage) {
		if (errorMessageMode === 'modal') {
			ElMessage.error(t('sys.api.errorTip'));
		} else if (errorMessageMode === 'message') {
			ElMessage.error(errMessage);
		}
	}
}
