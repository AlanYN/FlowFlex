import { ref, computed } from 'vue';
import { ElMessage } from 'element-plus';
import { useI18n } from '@/hooks/useI18n';

const { t } = useI18n();

export function createCardModule(
	sourceModule: number,
	targetModule: number,
	addRrelation: (
		businessId: string,
		sourceModule: number,
		targetModule: number,
		params: any
	) => Promise<any>
) {
	const cardRef = ref<{
		addCrad: () => void;
		refersh: () => void;
		cardTotal: number;
	}>();

	const formRef = ref<{
		submitForm: (onSuccess: (any) => void, onError: (error: Error) => void) => void;
		formFields: () => void;
		presentId: string;
	} | null>(null);

	const selectTableRef = ref<{
		submitSelectTable: () => void;
	}>();

	const cardTotal = computed(() => {
		return cardRef.value?.cardTotal || 0;
	});

	const dialogFormloading = ref(false);

	/**
	 * 保存卡片数据
	 * @returns Promise 当异步操作完成后解决
	 */
	const cardSave = (presentId, type, closeDialog, callBack) => {
		// 返回Promise，确保调用者可以使用await等待操作完成
		return new Promise((resolve, reject) => {
			// 处理表单提交
			if (type === 'form') {
				// 检查表单引用是否存在
				if (!formRef.value) {
					reject(new Error(t('sys.api.operationFailed')));
					return;
				}

				// 提交表单
				formRef.value.submitForm(
					// 表单验证成功回调
					async (form) => {
						dialogFormloading.value = true;

						try {
							// 等待关联添加完成
							const res = await addRrelation(
								presentId,
								sourceModule,
								targetModule,
								form
							);

							// 处理响应
							if (res.code === '200') {
								// 成功回调
								callBack && callBack();
								// 关闭对话框
								closeDialog();
								// 重置表单
								formRef.value && formRef.value.formFields();
								// 解决Promise
								resolve(res);
							} else {
								// 显示错误消息
								ElMessage.error(res?.msg || t('sys.api.operationFailed'));
								// 拒绝Promise
								reject(new Error(res?.msg || t('sys.api.operationFailed')));
							}
						} catch (error) {
							reject(error);
						} finally {
							// 无论成功失败，都关闭加载状态
							dialogFormloading.value = false;
						}
					},
					// 表单验证失败回调
					(errorFields) => {
						reject(new Error(t('sys.api.operationFailed')));
					}
				);
			} else {
				// 处理表格选择提交
				(async () => {
					try {
						if (selectTableRef.value) {
							// 提交表格选择
							const res = await selectTableRef.value.submitSelectTable();
							// 成功回调
							callBack && callBack(res);
							// 解决Promise
							resolve(res);
						} else {
							reject(new Error(t('sys.api.operationFailed')));
							return;
						}
						// 关闭对话框
						closeDialog();
					} catch (error) {
						// 处理异常
						reject(error);
					}
				})();
			}
		});
	};

	const addCard = (type = false) => {
		console.log('type', type);
		cardRef.value?.addCrad();
	};

	return {
		formRef,
		cardRef,
		cardTotal,
		dialogFormloading,
		selectTableRef,
		cardSave,
		addCard,
	};
}
