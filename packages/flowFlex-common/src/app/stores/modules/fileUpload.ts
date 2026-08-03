import { defineStore } from 'pinia';
import { ref } from 'vue';

export const useFileUploadStore = defineStore('item-wfe-app-file-upload', () => {
    // 当前正在上传中的文件数量
    const uploadingCount = ref(0);

    const increment = () => {
        uploadingCount.value++;
    };

    const decrement = () => {
        if (uploadingCount.value > 0) {
            uploadingCount.value--;
        }
    };

    const isUploading = () => uploadingCount.value > 0;

    // 组件卸载时调用，防止路由切换后 count 残留
    const reset = () => {
        uploadingCount.value = 0;
    };

    return { uploadingCount, increment, decrement, isUploading, reset };
});
