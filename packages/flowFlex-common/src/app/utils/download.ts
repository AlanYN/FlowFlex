import { ElNotification, ElMessage, ElProgress } from 'element-plus';
import { h, reactive } from 'vue';

interface DownloadProgressState {
	progress: number;
	status: string;
}

interface FileItem {
	id: string | number;
	name: string;
	realName: string;
}

/**
 * 下载文件并显示进度
 * @param item 文件信息
 * @param fetchFn 获取文件的函数
 */
export const downloadWithProgress = async (
	item: FileItem,
	fetchFn: (id: string | number, onProgress?: (progressEvent: any) => void) => Promise<any>
) => {
	const downloadState = reactive<DownloadProgressState>({
		progress: 0,
		status: '',
	});

	const notification = ElNotification({
		title: 'File Download',
		message: () =>
			h('div', { class: '' }, [
				h('p', { class: '' }, item.name),
				h(ElProgress, {
					percentage: downloadState.progress,
					strokeWidth: 6,
					textInside: false,
					format: (percentage) => `${percentage}%`,
				}),
			]),
		duration: 0,
		position: 'top-right',
		customClass: 'notification-download',
	});

	try {
		const res = await fetchFn(item.id, (progressEvent) => {
			downloadState.progress = Math.round((progressEvent.loaded * 100) / progressEvent.total);
		});

		setTimeout(() => {
			notification.close();
		}, 1000);

		downloadFile(res, item);
	} catch (err) {
		setTimeout(() => {
			notification.close();
		}, 2000);
		err && ElMessage.error(err);
	}
};

/**
 * 下载文件到本地
 * @param res 文件内容
 * @param file 文件信息
 */
export const downloadFile = (res: BlobPart, file: { name: string }) => {
	const blob = new Blob([res], { type: 'application/octet-stream' });
	const link = document.createElement('a');
	link.download = file.name;
	link.href = URL.createObjectURL(blob);
	document.body.appendChild(link);
	link.click();
	window.URL.revokeObjectURL(link.href);
	document.body.removeChild(link);
};
