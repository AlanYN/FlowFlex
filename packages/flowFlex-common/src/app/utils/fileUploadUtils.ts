import { ElMessage, ElNotification } from 'element-plus';
import { uploadQuestionFile } from '@/apis/ow/questionnaire';
import { nextTick } from 'vue';

// 文件类型定义
export type FileUploadType = 'video' | 'image';

// 上传结果接口
export interface UploadResult {
	success: boolean;
	fileUrl?: string;
	fileName?: string;
	error?: string;
}

// 上传参数接口
export interface UploadParams {
	file: File;
	type: FileUploadType;
	onProgress?: (progress: number) => void;
	onCancel?: () => void;
}

// 文件配置常量
export const FILE_CONFIG = {
	validVideoExtensions: ['.mp4', '.avi', '.mov', '.webm'],
	validImageExtensions: ['.jpg', '.jpeg', '.png', '.gif', '.webp'],
	maxVideoSize: 500 * 1024 * 1024, // 500MB
	maxImageSize: 10 * 1024 * 1024, // 10MB
} as const;

/**
 * 验证文件类型
 */
export function validateFileType(file: File, type: FileUploadType): boolean {
	const isValidType =
		type === 'video' ? file.type.startsWith('video/') : file.type.startsWith('image/');

	if (!isValidType) {
		ElMessage.error(`Please select a valid ${type} file. Selected file type: ${file.type}`);
		return false;
	}

	return true;
}

/**
 * 验证文件扩展名
 */
export function validateFileExtension(file: File, type: FileUploadType): boolean {
	const fileName = file.name.toLowerCase();
	const validExtensions =
		type === 'video' ? FILE_CONFIG.validVideoExtensions : FILE_CONFIG.validImageExtensions;

	const hasValidExtension = validExtensions.some((ext) => fileName.endsWith(ext));

	if (!hasValidExtension) {
		ElMessage.error(
			`Please select a valid ${type} file with supported extension. Supported: ${validExtensions.join(
				', '
			)}`
		);
		return false;
	}

	return true;
}

/**
 * 验证文件大小
 */
export function validateFileSize(file: File, type: FileUploadType): boolean {
	const maxSize = type === 'video' ? FILE_CONFIG.maxVideoSize : FILE_CONFIG.maxImageSize;

	if (file.size > maxSize) {
		ElMessage.error(`File size exceeds the maximum limit for ${type}`);
		return false;
	}

	return true;
}

/**
 * 验证文件（综合验证）
 */
export function validateFile(file: File, type: FileUploadType): boolean {
	return (
		validateFileType(file, type) &&
		validateFileExtension(file, type) &&
		validateFileSize(file, type)
	);
}

/**
 * 创建文件输入元素
 */
export function createFileInput(type: FileUploadType): HTMLInputElement {
	const input = document.createElement('input');
	input.type = 'file';
	input.style.display = 'none';
	input.accept = type === 'video' ? 'video/*' : 'image/*';
	input.multiple = false;
	return input;
}

/**
 * 文件上传管理器类
 */
export class FileUploadManager {
	private uploadNotification: any = null;
	private uploadAbortController: AbortController | null = null;
	private shouldSuppressCloseMessage = false; // 是否应该阻止关闭消息

	/**
	 * 创建上传进度通知
	 */
	private createUploadNotification(fileName: string, type: FileUploadType) {
		this.uploadNotification = ElNotification({
			title: `Uploading ${type}: ${fileName}`,
			message: `<div style="display: flex; align-items: center;">
          <div style="flex: 1; background-color: #f0f2f5; border-radius: 4px; overflow: hidden; margin-right: 8px;">
            <div id="upload-progress-bar" style="width: 0%; height: 8px; background: linear-gradient(90deg, #409eff 0%, #67c23a 100%); transition: width 0.3s ease;"></div>
          </div>
          <span id="upload-progress-text" style="font-weight: bold; font-size: 14px; color: #409eff;">0%</span>
        </div>
        <div style="color: #909399; font-size: 12px;">Click X to cancel upload</div>`,
			dangerouslyUseHTMLString: true,
			type: 'warning',
			duration: 0,
			showClose: true,
			onClose: () => {
				// 只有在不需要阻止关闭消息时才显示取消消息
				if (!this.shouldSuppressCloseMessage) {
					this.cancelUpload();
					ElMessage.warning('Upload cancelled');
				}
			},
		});
	}

	/**
	 * 更新上传进度
	 */
	private updateProgress(progress: number) {
		const progressTextElement = document.getElementById('upload-progress-text');
		if (progressTextElement) {
			progressTextElement.textContent = `${progress}%`;
		}

		const progressBarElement = document.getElementById('upload-progress-bar');
		if (progressBarElement) {
			progressBarElement.style.width = `${progress}%`;
		}
	}

	/**
	 * 取消上传
	 */
	private cancelUpload() {
		if (this.uploadAbortController) {
			this.uploadAbortController.abort();
			this.uploadAbortController = null;
		}
	}

	/**
	 * 清理资源
	 */
	private cleanup() {
		if (this.uploadNotification) {
			// 设置标记以阻止关闭消息
			this.shouldSuppressCloseMessage = true;
			this.uploadNotification.close();
			this.uploadNotification = null;
		}
		if (this.uploadAbortController) {
			this.uploadAbortController = null;
		}
	}

	/**
	 * 上传文件
	 */
	async uploadFile(params: UploadParams): Promise<UploadResult> {
		const { file, type, onProgress, onCancel } = params;

		// 验证文件
		if (!validateFile(file, type)) {
			return { success: false, error: 'File validation failed' };
		}

		// 创建上传控制器
		this.uploadAbortController = new AbortController();

		// 创建进度通知
		this.createUploadNotification(file.name, type);

		const uploadParams = {
			name: 'formFile',
			file: file,
			filename: file.name,
			data: {
				category: 'QuestionnaireQuestion',
			},
		};

		try {
			const res = await uploadQuestionFile(
				uploadParams,
				(progressEvent: any) => {
					const progress = Math.round((progressEvent.loaded * 100) / progressEvent.total);
					this.updateProgress(progress);
					onProgress?.(progress);
				},
				this.uploadAbortController.signal
			);

			if (res?.data?.code === '200' && res.data?.data?.fullAccessUrl) {
				this.shouldSuppressCloseMessage = true;
				ElMessage.success(`${type} uploaded successfully`);
				return {
					success: true,
					fileUrl: res.data?.data?.fullAccessUrl,
					fileName: file.name,
				};
			} else {
				return { success: false, error: 'Upload failed' };
			}
		} catch (error: any) {
			if (error.name === 'AbortError') {
				onCancel?.();
				return { success: false, error: 'Upload cancelled' };
			} else {
				return { success: false, error: error.message || 'Upload failed' };
			}
		} finally {
			nextTick(() => {
				this.cleanup();
			});
		}
	}
}

/**
 * 触发文件选择并上传
 */
export async function triggerFileUpload(
	type: FileUploadType,
	onSuccess?: (result: UploadResult) => void,
	onCancel?: () => void
): Promise<void> {
	return new Promise((resolve) => {
		const input = createFileInput(type);
		const uploadManager = new FileUploadManager();

		input.addEventListener('change', async (event: Event) => {
			const target = event.target as HTMLInputElement;
			const files = target.files;

			if (files && files.length > 0) {
				const file = files[0];

				const result = await uploadManager.uploadFile({
					file,
					type,
					onCancel,
				});

				if (result.success) {
					onSuccess?.(result);
				}
			}

			// 清理DOM元素
			document.body.removeChild(input);
			resolve();
		});

		// 添加到DOM并触发点击
		document.body.appendChild(input);
		input.click();
	});
}
