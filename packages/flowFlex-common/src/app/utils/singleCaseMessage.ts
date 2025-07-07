import { ElMessage } from 'element-plus';

export class SingleCaseMessage {
	private isShowMessage: boolean = false;

	public showMessage(message: string, type: 'success' | 'warning' | 'info' | 'error') {
		if (this.isShowMessage === false) {
			this.isShowMessage = true;
			ElMessage({
				message: message,
				type: type,
			});
			setTimeout(() => {
				this.isShowMessage = false;
			}, 2000);
		}
	}
}
